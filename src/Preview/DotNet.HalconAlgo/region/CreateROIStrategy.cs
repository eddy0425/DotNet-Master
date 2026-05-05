using DotNet.Drawing;
using DotNet.HalconUI;
using HalconDotNet;
using System;
using System.Windows.Forms;
using System.Collections.Generic;


namespace DotNet.HalconAlgo
{
    public class CreateROIStrategy : ParaStrategyBase<CreateROI>
    {
        public override AlgoEnum Algorithm => AlgoEnum.CreateROI;
        public override string Name { get; set; } = "创建ROI";
        public override int RunIndex { get; set; }

        public override void GenTreeNode(TreeVisualizer tree)
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
            RegisterOutput("坐标系/角度", () => inPara.Coord.Angle);
            RegisterOutput("区域", () => inPara.HoRect);

        }
        public override bool Fun_action(DisplayUI display, List<IParaStrategy> strategys)
        {
            HObject regionGet = new HObject(); HOperatorSet.GenEmptyObj(out regionGet);

            try
            {
                inPara.Coord = new CvCoord();
                var ho_ROI = inPara.HoRect;

                if (inPara.CoordIn == "默认")
                {
                    inPara.Coord = new CvCoord(ho_ROI.Center);
                    if (inPara.DispRegion) display.DispRegion(ho_ROI, HColor.Blue);
                }
                else
                {
                    var inCoord = strategys.ResolveFrom<CvCoord>(inPara.CoordIn);
                    var tmplPoint = strategys.ResolveFrom<Point2d>(inPara.CoordIn.ToTmplPoint());
                    HalconHelper.TransRegion(tmplPoint, inCoord.Center, ho_ROI.HoRegion, out regionGet);
                    HOperatorSet.AreaCenter(regionGet, out _, out HTuple row, out HTuple column);
                    inPara.Coord = new CvCoord(new Point2d(column, row));
                    if (inPara.DispRegion) display.DispRegion(regionGet, HColor.Blue);
                }

                if (inPara.DispText)
                {
                    string message = $"{Name} : 中心:({inPara.Coord.X:F2},{inPara.Coord.Y:F2}) 宽:{ho_ROI.Width:F0} 高:{ho_ROI.Height:F0} 跟随坐标:{inPara.CoordIn}";
                    display.DispText(message, inPara.FontX, inPara.FontY, inPara.FontSize, HColor.Green);
                }

                return true;
            }
            catch
            {
                throw;
            }
            finally
            {
                regionGet.Dispose();
            }
        }
        public override void DispPara(Form form, Dictionary<string, VsControlModel> VsControls)
        {
            form.ShowTabs(TabPageEnum.Region, TabPageEnum.Display);

            VsControls.ShowComboBox(form, "cmb_CoordIn", inPara.CoordIn.ToString(), false);

            CvRegion hRegion = inPara.HoRect;
            VsControls.ShowComboBox(form, "cmb_Width", hRegion.Width.ToString(), false);
            VsControls.ShowComboBox(form, "cmb_Height", hRegion.Height.ToString(), false);
            VsControls.ShowComboBox(form, "cmb_TopLeft", $"{hRegion.TopLeft.X};{hRegion.TopLeft.Y}", false);
            VsControls.ShowComboBox(form, "cmb_BottomRight", $"{hRegion.BottomRight.X};{hRegion.BottomRight.Y}", false);
            VsControls.ShowComboBox(form, "cmb_Center", $"{hRegion.Center.X};{hRegion.Center.Y}", false);

            //------------------------------------------
            VsControls.ShowCheckBox(form, "ckb_disp0", "显示文本", inPara.DispText);
            VsControls.ShowCheckBox(form, "ckb_disp1", "查找区域", inPara.DispRegion);

            VsControls.ShowComboBoxDropDown(form, "CB_FontX", inPara.FontX.ToString(), new[] { "20", "50" });
            VsControls.ShowComboBoxDropDown(form, "CB_FontY", inPara.FontY.ToString(), new[] { "20", "50" });
            VsControls.ShowComboBoxDropDown(form, "CB_FontSize", inPara.FontSize.ToString(), new[] { "15", "30" });
        }
        public override void SavePara(Form form, Dictionary<string, VsControlModel> VsControls)
        {
            inPara.CoordIn = VsControls["cmb_CoordIn"].Text;

            //------------------------------------------
            inPara.DispText = VsControls["ckb_disp0"].Checked;
            inPara.DispRegion = VsControls["ckb_disp1"].Checked;

            inPara.FontX = Convert.ToInt16(VsControls["CB_FontX"].Text);
            inPara.FontY = Convert.ToInt16(VsControls["CB_FontY"].Text);
            inPara.FontSize = Convert.ToInt16(VsControls["CB_FontSize"].Text);
        }
        public override void DispROI(DisplayUI display)
        {
            display.SetDrawMode(Name, inPara.HoRect, DrawEnum.DispRect);
        }
        public override void Init(DisplayUI display)
        {
            display.RectangleEvent += RectEvent;
        }
        public override void Close(DisplayUI display)
        {
            display.RectangleEvent -= RectEvent;
        }
        private void RectEvent(object sender, DrawRectangleArgs e)
        {
            if (e.Name == Name)
            {
                inPara.HoRect.Update2Point(e.TopLeft, e.BottomRight);
                inPara.HoRect.GenRegion();
            }
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
