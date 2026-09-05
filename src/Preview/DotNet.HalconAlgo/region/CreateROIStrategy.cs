using DotNet.Drawing;
using DotNet.Vision.Abstractions;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace DotNet.HalconAlgo
{
    public class CreateROIStrategy : ParaStrategyBase<CreateROI>, IRoiEditable
    {
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
                           .Node("角度", OutEnum.Array)
                       )
                       .Node("区域", OutEnum.Region)
                   );

            ClearResolvers();
            RegisterOutput("坐标系", () => inPara.Coord);
            RegisterOutput("坐标系/原点", () => inPara.Coord.Center);
            RegisterOutput("坐标系/原点/行", () => inPara.Coord.Y);
            RegisterOutput("坐标系/原点/列", () => inPara.Coord.X);
            RegisterOutput("坐标系/角度", () => inPara.Coord.Angle.Radians);
            RegisterOutput("区域", () => inPara.HoRect);

        }
        public override bool Fun_action(IHDisplay display, List<IParaStrategy> strategys)
        {
            HObject regionGet; HOperatorSet.GenEmptyObj(out regionGet);

            try
            {
                inPara.Coord = new CvCoord();
                var ho_ROI = inPara.HoRect;

                if (inPara.CoordIn == "默认")
                {
                    inPara.Coord = new CvCoord(ho_ROI.Center);
                    if (inPara.DispRegion) display.Disp(ho_ROI, DrawStyle.Of(HColor.Blue));
                }
                else
                {
                    var inCoord = strategys.ResolveFrom<CvCoord>(inPara.CoordIn);
                    var tmplPoint = strategys.ResolveFrom<Point2d>(inPara.CoordIn.ToTmplPoint());
                    HalconController.TransRegion(tmplPoint, inCoord.Center, ho_ROI.HoRegion, out regionGet);
                    HOperatorSet.AreaCenter(regionGet, out _, out HTuple row, out HTuple column);
                    inPara.Coord = new CvCoord(new Point2d(column, row));
                    if (inPara.DispRegion) display.Disp(regionGet, DrawStyle.Of(HColor.Blue));
                }

                if (inPara.DispText)
                {
                    string message = $"{Name} : 中心:({inPara.Coord.X:F2},{inPara.Coord.Y:F2}) 宽:{ho_ROI.Width:F0} 高:{ho_ROI.Height:F0} 跟随坐标:{inPara.CoordIn}";
                    display.DispText(message, new Point2d(inPara.FontX, inPara.FontY), DrawStyle.Of(HColor.Green, inPara.FontSize));
                }

                return true;
            }
            finally
            {
                regionGet.Dispose();
            }
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

        /// <summary> 显示区域 </summary>
        public bool DispRegion { set; get; } = true;
    }
}
