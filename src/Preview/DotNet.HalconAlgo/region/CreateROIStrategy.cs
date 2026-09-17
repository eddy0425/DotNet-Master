using DotNet.Drawing;
using DotNet.Vision.Abstractions;
using HalconDotNet;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace DotNet.HalconAlgo
{
    public class CreateROIStrategy : ParaStrategyBase<CreateROI>, IRoiEditable, IDisposable
    {
        private bool _disposed;

        public override AlgoEnum Algorithm => AlgoEnum.CreateROI;
        public override string Name { get; set; } = "创建ROI";
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
                   );

            ClearResolvers();
            // 模板协议: TmplPoint 是配置态(零角)参考原点, 下游 CoordIn 靠它算刚体变换.
            RegisterOutput("TmplPoint", () => inPara.HoRect.Center);
            RegisterOutput("坐标系", () => inPara.Coord);
            RegisterOutput("坐标系/原点", () => inPara.Coord.Center);
            RegisterOutput("坐标系/原点/行", () => inPara.Coord.Y);
            RegisterOutput("坐标系/原点/列", () => inPara.Coord.X);
            RegisterOutput("坐标系/角度", () => inPara.Coord.Angle.Radians);
            // 只给 HoRegion 而不给整个 CvRegion: Result 只有 HoRegion 一项是真的, Bounds / Type / Phi
            // 从未随本轮结果更新, 恒为默认值。对外只交付确实有效的那部分, 免得下游读到假数据。
            // TryResolveRegionFrom 两种形态都收, 解析侧无需改动。
            RegisterOutput("区域", () => inPara.Result.HoRegion);

        }
        public override bool Fun_action(IHDisplay display, List<IParaStrategy> strategys)
        {
            HObject regionGet = null;   // 两条分支都会赋值; 所有权转交 Result 时置 null

            try
            {
                inPara.Coord = new CvCoord();
                var ho_ROI = inPara.HoRect;

                // 未绘制 ROI 时 HoRegion 是 gen_empty_obj 的 0 长度元组: 两条分支 (CopyObj / TransRegion)
                // 都会让它一路流到下游, 报出与真实原因无关的 HALCON 错误.
                // 与 MergeRegion 的"无有效输入区域"同口径: 清结果 + 红字返回 false, 不抛异常 ——
                // CreateROIForm 的循环执行只在最外层包 try, 抛异常会让后续工具全部不执行;
                // 真正消费本输出的下游走 ResolveRegionFrom, 会抛出带完整路径的 AlgoOutputNotFoundException.
                if (!ho_ROI.HoRegion.IsUsableRegion())
                {
                    ClearResult();
                    // 不受 DispText 门控: 这是错误而不是装饰。"显示文本"是个显示偏好复选框, 关掉它
                    // 连报错一起消失是不对的 —— Fun_action 的返回值没有任何调用方检查, 静音就等于
                    // 工具默默什么都不做, 现场没有任何线索。
                    display.DispText($"{Name} : 尚未绘制 ROI", new Point2d(inPara.FontX, inPara.FontY), DrawStyle.Of(HColor.Red, inPara.FontSize));
                    return false;
                }

                if (inPara.CoordIn == "默认")
                {
                    regionGet = ho_ROI.HoRegion.CopyObj(1, -1);
                    inPara.Coord = new CvCoord(ho_ROI.Center);
                }
                else
                {
                    var inCoord = strategys.ResolveFrom<CvCoord>(inPara.CoordIn);
                    var tmplPoint = strategys.ResolveFrom<Point2d>(inPara.CoordIn.ToTmplPoint());
                    var tmplCoord = new CvCoord(tmplPoint);

                    HalconController.TransRegion(tmplCoord, inCoord, ho_ROI.HoRegion, out regionGet);
                    // 与 FitLine / FitArcMidpoint 同口径: 亚像素刚体变换, 不取区域光栅化重心
                    inPara.Coord = new CvCoord(
                        HalconController.TransPoint(tmplCoord, inCoord, ho_ROI.Center),
                        inCoord.Angle);
                }

                // 配置 ROI 不变；发布与显示共用本轮的区域句柄，下一轮才释放旧结果。
                var previous = inPara.Result.HoRegion;
                inPara.Result.HoRegion = regionGet;
                regionGet = null; // 所有权转交给 Result
                previous?.Dispose();
                if (inPara.DispRegion) display.Disp(inPara.Result.HoRegion, DrawStyle.Of(HColor.Blue));

                if (inPara.DispText)
                {
                    string message = $"{Name} : 中心:({inPara.Coord.X:F2},{inPara.Coord.Y:F2}) 宽:{ho_ROI.Width:F0} 高:{ho_ROI.Height:F0} 跟随坐标:{inPara.CoordIn}";
                    display.DispText(message, new Point2d(inPara.FontX, inPara.FontY), DrawStyle.Of(HColor.Green, inPara.FontSize));
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
                regionGet?.Dispose();
            }
        }

        private void ClearResult()
        {
            HOperatorSet.GenEmptyObj(out HObject empty);
            var previous = inPara.Result.HoRegion;
            inPara.Result.HoRegion = empty;
            previous?.Dispose();
            inPara.Coord = new CvCoord();
        }

        /// <summary>
        /// 只清理运行结果，重新打开工具页仍可复用配置 ROI。
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
        /// 这里<b>只</b>释放 <c>Result</c>: <c>HoRect</c> 是随 job 落盘的配置态 ROI, 一旦在此释放,
        /// 将来宿主真接上 Dispose 后, 保存配置 / 复制工具就会读到已释放的句柄。
        /// 配置态句柄的归属在 <c>CvRegion</c> 自己身上, 不由策略代管。
        /// </para>
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            inPara?.Result?.Dispose();
        }

        public override void DispPara(IParaUiHost ui)
        {
            ui.ShowTabs(TabPageEnum.Region, TabPageEnum.Display);

            ui.ShowComboBox("cmb_CoordIn", inPara.CoordIn.ToString(), false);

            CvRegion hRegion = inPara.HoRect;
            ui.ShowComboBox("cmb_Width", hRegion.Width.ToString(), false);
            ui.ShowComboBox("cmb_Height", hRegion.Height.ToString(), false);
            ui.ShowComboBox("cmb_TopLeft", $"{hRegion.TopLeft.X};{hRegion.TopLeft.Y}", false);
            ui.ShowComboBox("cmb_BottomRight", $"{hRegion.BottomRight.X};{hRegion.BottomRight.Y}", false);
            ui.ShowComboBox("cmb_Center", $"{hRegion.Center.X};{hRegion.Center.Y}", false);

            //------------------------------------------
            ui.ShowCheckBox("ckb_disp0", "显示文本", inPara.DispText);
            ui.ShowCheckBox("ckb_disp1", "查找区域", inPara.DispRegion);

            ui.ShowComboBoxDropDown("CB_FontX", inPara.FontX.ToString(), new[] { "20", "50" });
            ui.ShowComboBoxDropDown("CB_FontY", inPara.FontY.ToString(), new[] { "20", "50" });
            ui.ShowComboBoxDropDown("CB_FontSize", inPara.FontSize.ToString(), new[] { "15", "30" });
        }
        public override void SavePara(IParaUiHost ui)
        {
            inPara.CoordIn = ui.GetString("cmb_CoordIn");

            //------------------------------------------
            inPara.DispText = ui.GetBool("ckb_disp0");
            inPara.DispRegion = ui.GetBool("ckb_disp1");

            inPara.FontX = ui.GetInt("CB_FontX");
            inPara.FontY = ui.GetInt("CB_FontY");
            inPara.FontSize = ui.GetInt("CB_FontSize");
        }
        public async Task DrawROIAsync(IRoiHost host, RectEnum type, bool newROI)
        {
            if (newROI)
            {
                // Type 必须在绘制前写入(HDisplay 按它分发图元), 但取消时几何不会被回写,
                // 所以要连 Type 一起还原, 否则 Type 与 HoRegion / 外接框对不上, 还会存进 job 配置.
                var prevType = inPara.HoRect.Type;
                inPara.HoRect.Type = type;
                if (!await host.DrawRegionAsync(inPara.HoRect))
                    inPara.HoRect.Type = prevType;
            }
            else await host.DrawRegionModAsync(inPara.HoRect);

            // 这里故意不短路: 取消后仍要把原 ROI 重画回去(ParaForm 事先 ReDispImage 已清屏)
            host.Display.Disp(inPara.HoRect, DrawStyle.Of(HColor.Blue));
            host.SetRectPara(inPara.HoRect);
        }
        public void DispROI(IRoiHost host)
        {
            host.SetRectPara(inPara.HoRect);
        }
    }

    public class CreateROI : AlgoFont
    {
        /// <summary> 跟随坐标 </summary>
        public string CoordIn { set; get; } = "默认";

        /// <summary> 坐标系 </summary>
        public CvCoord Coord { get; set; } = new CvCoord();

        /// <summary> 区域 </summary>
        public CvRegion HoRect { set; get; } = new CvRegion();

        /// <summary>运行期区域输出；仅 HoRegion 表示实际形状，不用于编辑或重建。</summary>
        [JsonIgnore]
        public CvRegion Result { get; } = new CvRegion();

        /// <summary> 显示区域 </summary>
        public bool DispRegion { set; get; } = true;
    }
}
