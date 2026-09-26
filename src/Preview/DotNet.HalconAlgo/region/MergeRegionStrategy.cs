using DotNet.Drawing;
using DotNet.Vision.Abstractions;
using HalconDotNet;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace DotNet.HalconAlgo
{
    /// <summary>
    /// 区域合并: 把多路上游区域输出并成一个区域。
    /// </summary>
    /// <remarks>
    /// 不实现 <c>IRoiEditable</c>: 本策略没有自己的配置 ROI —— 形状完全来自 <c>RegionSources</c>。
    /// 原先留着 <c>HoRect</c> + <c>DrawROIAsync</c> 是死代码(绘制按钮 btn_drawRegion 在 tabPage2,
    /// 本策略不开该页, 根本点不到), 而 <c>DispROI</c> 还有副作用: 选中本工具就会 SetRectPara 把显示
    /// 切进 DispRect 交互模式, 绑到一个 (0,0) 的空矩形上。
    /// </remarks>
    public class MergeRegionStrategy : ParaStrategyBase<RegionMerge>, IDisposable
    {
        private bool _disposed;

        public override AlgoEnum Algorithm => AlgoEnum.MergeRegion;
        public override string Name { get; set; } = "区域合并";
        public override int RunIndex { get; set; }

        public override void GenTreeNode(ITreeVisualizer tree)
        {
            tree.Branch(Name, branch => branch
                       .Node("坐标系", OutEnum.Coord, line => line
                           .Branch("原点", pt => pt
                               .Node("行", OutEnum.Number)
                               .Node("列", OutEnum.Number)
                           )
                           .Node("角度", OutEnum.Number)
                       )
                       .Node("区域", OutEnum.Region)
                       .CommonNodes()
                   );

            ClearResolvers();
            // 模板协议: TmplPoint 是"Coord.Center 在示教态取到的那个值", 下游 CoordIn 靠它算刚体变换.
            // 本策略没有配置 ROI, Coord 是每轮合并结果的重心, 所以零角参考原点只能在 Fun_action 里
            // 隐式示教(首轮全部来源有效时记一次), 不像匹配那样有显式的示教动作可挂.
            // 直接装箱 Nullable<T> 即可: 未示教 → null, 下游 ResolveFrom<Point2d> 抛
            // AlgoOutputNotFoundException (响亮失败好过静默算错); 已示教 → 装箱的是 Point2d 本身
            // (而非 Point2d?), 下游 is Point2d 判定成立.
            RegisterOutput("TmplPoint", () => (object)inPara.TmplPoint);
            RegisterOutput("坐标系", () => inPara.Coord);
            RegisterOutput("坐标系/原点", () => inPara.Coord.Center);
            RegisterOutput("坐标系/原点/行", () => inPara.Coord.Y);
            RegisterOutput("坐标系/原点/列", () => inPara.Coord.X);
            RegisterOutput("坐标系/角度", () => inPara.Coord.Angle.Radians);
            // 只给 HoRegion 而不给整个 CvRegion: Result 只有 HoRegion 一项是真的, Bounds / Type / Phi
            // 从未随合并结果更新, 恒为默认值。对外只交付确实有效的那部分, 免得下游读到假数据
            // (TmplPoint 当初就是这么踩的坑)。TryResolveRegionFrom 两种形态都收, 解析侧无需改动。
            RegisterOutput("区域", () => inPara.Result.HoRegion);

        }

        public override bool Fun_action(IHDisplay display, List<IParaStrategy> strategys)
        {
            // collected: 逐个 ConcatObj 累积的区域元组; merged: Union1 之后的单一区域.
            HObject collected; HOperatorSet.GenEmptyObj(out collected);
            HObject merged; HOperatorSet.GenEmptyObj(out merged);
            HObject transformed; HOperatorSet.GenEmptyObj(out transformed);

            try
            {
                int srcCnt = 0;
                int missCnt = 0;
                if (inPara.RegionSources != null)
                {
                    for (int i = 0; i < inPara.RegionSources.Length; i++)
                    {
                        var path = inPara.RegionSources[i];
                        if (string.IsNullOrWhiteSpace(path)) continue;

                        // 解析不到 / 空句柄都按"来源无效"计数, 不中断整轮合并.
                        // (空句柄的判断在 TryResolveRegionFrom 内部, 这里不必重复.)
                        if (!strategys.TryResolveRegionFrom(path, out HObject src)) { missCnt++; continue; }

                        HObject concat;
                        HOperatorSet.ConcatObj(collected, src, out concat);
                        collected.Dispose();
                        collected = concat;
                        srcCnt++;
                    }
                }

                if (srcCnt == 0)
                {
                    ClearResult();
                    // 不受 DispText 门控: 这是错误而不是装饰。"显示文本"是个显示偏好复选框, 关掉它
                    // 连报错一起消失是不对的 —— Fun_action 的返回值没有任何调用方检查, 静音就等于
                    // 工具默默什么都不做, 现场没有任何线索。
                    display.DispText($"{Name} : 无有效输入区域", new Point2d(inPara.FontX, inPara.FontY), DrawStyle.Of(HColor.Red, inPara.FontSize));
                    return false;
                }

                // Union1: 把元组里的全部区域并成一个区域, 正是"区域合并"的语义.
                merged.Dispose();
                HOperatorSet.Union1(collected, out merged);

                HObject result = merged;
                Angle rotation = default;
                if (inPara.CoordIn != "默认")
                {
                    var inCoord = strategys.ResolveFrom<CvCoord>(inPara.CoordIn);
                    var tmplPoint = strategys.ResolveFrom<Point2d>(inPara.CoordIn.ToTmplPoint());
                    transformed.Dispose();
                    HalconController.TransRegion(new CvCoord(tmplPoint), inCoord, merged, out transformed);
                    rotation = inCoord.Angle;
                    result = transformed;
                }

                // 先完成深拷贝，再替换结果并释放旧句柄；复制失败时不暴露已释放对象。
                var copiedResult = result.CopyObj(1, -1);
                ReplaceResult(copiedResult);

                HOperatorSet.AreaCenter(result, out _, out HTuple row, out HTuple column);
                // 角度只有跟随分支才有意义: "默认"分支恒为 0 —— 本策略只拿到上游的区域句柄,
                // 无法推断来源的朝向。所以当输入区域自己在跟随旋转时, 本策略的"坐标系"只能表达平移,
                // 下游选它当 CoordIn 会"跟着动但不跟着转"。需要旋转跟随的下游应直接跟最上游的匹配坐标系。
                inPara.Coord = new CvCoord(new Point2d(column, row), rotation);

                // 全部来源有效的首轮结果才算示教态: 记录一次作为下游 CoordIn 的零角参考原点.
                // 缺来源时的重心是残缺的, 不能当基准; 配置一经改动由 SavePara 清空本字段, 下一轮重新示教,
                // 否则改了输入来源 / 重画了上游 ROI 之后, TmplPoint 仍停在旧配置的重心, 下游平移量系统性偏移。
                // 重心必须取"变换前"的 merged: 有跟随时 result 已被搬到当前工件位姿, 拿它当零角参考会让
                // 下游平移量恒为 0 (跟随白做); "默认"分支下 result 就是 merged, 两者等价 —— 因此不能
                // 拿 CoordIn == "默认" 当示教前提, 否则跟随分支永远示教不出来, 下游选它当 CoordIn 永久抛异常。
                if (missCnt == 0 && !inPara.TmplPoint.HasValue)
                {
                    HOperatorSet.AreaCenter(merged, out _, out HTuple tmplRow, out HTuple tmplCol);
                    inPara.TmplPoint = new Point2d(tmplCol, tmplRow);
                }

                // 显示已发布的那一份, 与 CreateROI 口径一致: 现场看到的就是下游拿到的。
                if (inPara.DispRegion) display.Disp(inPara.Result.HoRegion, DrawStyle.Of(HColor.Blue));

                if (inPara.DispText)
                {
                    string message = $"{Name} : 合并数量:{srcCnt}" + (missCnt > 0 ? $" 无效来源:{missCnt}" : string.Empty)
                                   + $" 中心:({inPara.Coord.X:F2},{inPara.Coord.Y:F2}) 跟随坐标:{inPara.CoordIn}";
                    // 有无效来源时重心是残缺的(也因此不会示教 TmplPoint), 用红字区分:
                    // 绿字在本仓的惯例里就是"正常跑完", 降级结果不该长得跟成功一样。
                    display.DispText(message, new Point2d(inPara.FontX, inPara.FontY),
                        DrawStyle.Of(missCnt > 0 ? HColor.Red : HColor.Green, inPara.FontSize));
                }

                return true;
            }
            catch
            {
                ClearResult();
                throw;
            }
            finally
            {
                collected.Dispose();
                merged.Dispose();
                transformed.Dispose();
            }
        }

        private void ReplaceResult(HObject replacement)
        {
            inPara.Result.HoRegion = replacement; // setter 释放旧句柄
        }

        private void ClearResult()
        {
            HObject empty;
            HOperatorSet.GenEmptyObj(out empty);
            ReplaceResult(empty);
            inPara.Coord = new CvCoord();
        }

        public override void DispPara(IParaUiHost ui)
        {
            ui.ShowTabs(TabPageEnum.Parameter, TabPageEnum.Display);

            // 跟随坐标借参数页空闲的 110 号槽位, 而不是别的策略用的 cmb_CoordIn:
            // 那三个控件(lbl_CoordIn / cmb_CoordIn / btn_setCoordIn)都挂在 tabPage2(Region 页),
            // 本策略不开该页 → 控件永远不可见, 用户无从选择, CoordIn 只能永远停在"默认",
            // Fun_action 里的跟随分支就成了死路径。参数页的 100-105 与 110-115 是两排独立槽位
            // (拟合类算法同时用两排), 本策略只占了前一排, 借后一排的第一个不冲突。
            ui.ShowLabel("lbl_110", "跟随坐标");
            ui.ShowComboBox("cmb_110", inPara.CoordIn, false);
            ui.ShowButton("btn_110", true);

            for (int i = 0; i < inPara.RegionSources.Length; i++)
            {
                int id = 100 + i;
                ui.ShowLabel($"lbl_{id}", $"输入区域{i}");
                ui.ShowComboBox($"cmb_{id}", inPara.RegionSources[i], false);
                ui.ShowButton($"btn_{id}", true);
            }

            //------------------------------------------
            ui.ShowCheckBox("ckb_disp0", "显示文本", inPara.DispText);
            ui.ShowCheckBox("ckb_disp1", "查找区域", inPara.DispRegion);

            ui.ShowComboBoxDropDown("CB_FontX", inPara.FontX.ToString(), new[] { "20", "50" });
            ui.ShowComboBoxDropDown("CB_FontY", inPara.FontY.ToString(), new[] { "20", "50" });
            ui.ShowComboBoxDropDown("CB_FontSize", inPara.FontSize.ToString(), new[] { "15", "30" });
        }
        /// <summary>
        /// 配置项等值比较: 把 null 与 "" 视为同一个"未设置"。
        /// <c>RegionSources</c> 新建时是 <c>new string[6]</c> (全 null), 落盘再读回来也是 null,
        /// 而 <c>ui.GetString</c> 读空下拉框返回 <see cref="string.Empty"/> ——
        /// 不归一化就会把"什么都没改"判成改了。
        /// </summary>
        private static bool SameConfig(string a, string b)
        {
            return string.Equals(a ?? string.Empty, b ?? string.Empty, StringComparison.Ordinal);
        }

        public override void SavePara(IParaUiHost ui)
        {
            //基本参数
            // 只有配置真的变了才作废示教原点。
            // SavePara 的调用方全是各 Form 的"运行"按钮(but_Run_Click), 每点一次运行都会走到这里,
            // 并不是"确定配置"才触发, 所以无条件清空会导致每轮运行都清掉 →
            // 原点被当前画面里的工件位置重新示教, 下游整体偏移且毫无提示。
            bool configChanged = false;

            // 与 DispPara 对应: 跟随坐标在 110 号槽位, 不是 cmb_CoordIn(那个控件在本策略不显示的页上)。
            string coordIn = ui.GetString("cmb_110");
            if (!SameConfig(coordIn, inPara.CoordIn)) configChanged = true;
            inPara.CoordIn = coordIn;

            for (int i = 0; i < inPara.RegionSources.Length; i++)
            {
                string source = ui.GetString($"cmb_{100 + i}");
                if (!SameConfig(source, inPara.RegionSources[i])) configChanged = true;
                inPara.RegionSources[i] = source;
            }

            // 配置一经改动, 旧的示教原点即失效: 清空后由下一次成功合并重新记录。
            // 界面上没有别的入口能清它, 不在这里清就会一直沿用旧配置的重心。
            if (configChanged) inPara.TmplPoint = null;

            //------------------------------------------
            inPara.DispText = ui.GetBool("ckb_disp0");
            inPara.DispRegion = ui.GetBool("ckb_disp1");

            inPara.FontX = ui.GetInt("CB_FontX");
            inPara.FontY = ui.GetInt("CB_FontY");
            inPara.FontSize = ui.GetInt("CB_FontSize");
        }
        /// <summary>
        /// 只清理运行结果，重新打开工具页仍可复用配置与示教原点。
        /// 注意: 宿主目前只调用 <c>Init</c>, 尚未接线 <c>Close</c>。
        /// </summary>
        public override void Close(IRoiHost host)
        {
            if (_disposed) return;
            ClearResult();
        }

        /// <summary>
        /// 策略实例生命周期结束时释放运行态资源. 幂等.
        /// 注意: 宿主既未接线 <c>Close</c>, 也未对策略集合做 IDisposable 分发, 本方法目前<b>无调用方</b>,
        /// 句柄仍依赖 HObject 自身的 finalizer 回收 —— 属预留接口, 待宿主在移除工具 / 关闭 job 时接线.
        /// <para>
        /// <c>Result</c> 是本策略唯一持有的运行期句柄(没有配置 ROI), 释放它即可。
        /// </para>
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            inPara?.Result?.Dispose();
        }
    }

    public class RegionMerge : AlgoFont
    {
        /// <summary> 跟随坐标 </summary>
        public string CoordIn { set; get; } = "默认";

        /// <summary> 区域来源 </summary>
        public string[] RegionSources { get; set; } = new string[6];

        /// <summary> 坐标系 </summary>
        public CvCoord Coord { get; set; } = new CvCoord();

        /// <summary>
        /// 示教态合并重心(变换前): 首轮全部来源有效的合并记录一次, 作为下游 CoordIn 的零角参考原点。
        /// 随 job 落盘; 为 null 表示尚未示教, 此时不对外产出 TmplPoint。
        /// </summary>
        public Point2d? TmplPoint { get; set; }

        /// <summary> 合并结果, 由 Fun_action 每轮重建, 作为 "区域" 输出; 运行期状态不落盘 </summary>
        [JsonIgnore]
        public CvRegion Result { get; } = new CvRegion();

        /// <summary> 显示区域 </summary>
        public bool DispRegion { set; get; } = true;
    }

}
