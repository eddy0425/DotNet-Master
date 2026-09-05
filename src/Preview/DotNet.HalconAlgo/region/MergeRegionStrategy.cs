using DotNet.Drawing;
using DotNet.Vision.Abstractions;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace DotNet.HalconAlgo
{
    public class MergeRegionStrategy : ParaStrategyBase<RegionMerge>, IRoiEditable
    {
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
                           .Node("角度", OutEnum.Array)
                       )
                       .Node("区域", OutEnum.Region)
                       .CommonNodes()
                   );

            ClearResolvers();
            RegisterOutput("坐标系", () => inPara.Coord);
            RegisterOutput("坐标系/原点", () => inPara.Coord.Center);
            RegisterOutput("坐标系/原点/行", () => inPara.Coord.Y);
            RegisterOutput("坐标系/原点/列", () => inPara.Coord.X);
            RegisterOutput("坐标系/角度", () => inPara.Coord.Angle.Radians);
            // 输出的是本次合并结果, 不是用户画的配置 ROI (inPara.HoRect).
            RegisterOutput("区域", () => inPara.Result);

        }

        /// <summary>
        /// 从上游策略解析一个区域. 上游可能注册的是 <see cref="CvRegion"/> (CreateROIStrategy 等),
        /// 也可能直接注册 <see cref="HObject"/>, 两种都接受; 解析不到返回 null (不抛异常, 由调用方按"来源无效"处理).
        /// 返回的句柄归上游所有, <b>不得</b>在这里释放.
        /// </summary>
        private static HObject ResolveRegion(List<IParaStrategy> strategys, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;

            CvRegion cvRegion;
            if (strategys.TryResolveFrom(path, out cvRegion))
                return cvRegion?.HoRegion;

            HObject hoRegion;
            if (strategys.TryResolveFrom(path, out hoRegion))
                return hoRegion;

            return null;
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

                        var src = ResolveRegion(strategys, path);
                        if (!src.NotNull()) { missCnt++; continue; }

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
                    if (inPara.DispText)
                    {
                        display.DispText($"{Name} : 无有效输入区域", new Point2d(inPara.FontX, inPara.FontY), DrawStyle.Of(HColor.Red, inPara.FontSize));
                    }
                    return false;
                }

                // Union1: 把元组里的全部区域并成一个区域, 正是"区域合并"的语义.
                HOperatorSet.Union1(collected, out merged);

                HObject result = merged;
                if (inPara.CoordIn != "默认")
                {
                    var inCoord = strategys.ResolveFrom<CvCoord>(inPara.CoordIn);
                    var tmplPoint = strategys.ResolveFrom<Point2d>(inPara.CoordIn.ToTmplPoint());
                    HalconController.TransRegion(tmplPoint, inCoord.Center, merged, out transformed);
                    result = transformed;
                }

                // 先完成深拷贝，再替换结果并释放旧句柄；复制失败时不暴露已释放对象。
                var copiedResult = result.CopyObj(1, -1);
                ReplaceResult(copiedResult);

                HOperatorSet.AreaCenter(result, out _, out HTuple row, out HTuple column);
                inPara.Coord = new CvCoord(new Point2d(column, row));

                if (inPara.DispRegion) display.Disp(result, DrawStyle.Of(HColor.Blue));

                if (inPara.DispText)
                {
                    string message = $"{Name} : 合并数量:{srcCnt}" + (missCnt > 0 ? $" 无效来源:{missCnt}" : string.Empty)
                                   + $" 中心:({inPara.Coord.X:F2},{inPara.Coord.Y:F2}) 跟随坐标:{inPara.CoordIn}";
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
                collected.Dispose();
                merged.Dispose();
                transformed.Dispose();
            }
        }

        private void ReplaceResult(HObject replacement)
        {
            var previous = inPara.Result.HoRegion;
            inPara.Result.HoRegion = replacement;
            previous?.Dispose();
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

            ui.ShowComboBox("cmb_CoordIn", inPara.CoordIn.ToString(), false);

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
        public override void SavePara(IParaUiHost ui)
        {
            //基本参数
            inPara.CoordIn = ui.GetString("cmb_CoordIn");

            for (int i = 0; i < inPara.RegionSources.Length; i++)
            {
                inPara.RegionSources[i] = ui.GetString($"cmb_{100 + i}");
            }

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

    public class RegionMerge : AlgoFont
    {
        /// <summary> 跟随坐标 </summary>
        public string CoordIn { set; get; } = "默认";

        /// <summary> 区域来源 </summary>
        public string RegionIn { set; get; } = "默认";

        /// <summary> 区域来源 </summary>
        public string[] RegionSources { get; set; } = new string[6];

        /// <summary> 坐标系 </summary>
        public CvCoord Coord { get; set; } = new CvCoord();

        /// <summary> 配置态 ROI (供 DrawROIAsync 绘制/编辑, 不参与合并) </summary>
        public CvRegion HoRect { set; get; } = new CvRegion();

        /// <summary> 合并结果, 由 Fun_action 每轮重建, 作为 "区域" 输出 </summary>
        public CvRegion Result { get; } = new CvRegion();

        /// <summary> 显示区域 </summary>
        public bool DispRegion { set; get; } = true;
    }

}
