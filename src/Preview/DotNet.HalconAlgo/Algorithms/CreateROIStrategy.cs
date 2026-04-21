using DotNet.Drawing;
using HalconDotNet;
using System.Windows.Forms;
using System.Collections.Generic;


namespace DotNet.HalconAlgo
{
    public class CreateROIStrategy : ParaStrategyBase<CreateROI>
    {
        public override AlgoEnum Algorithm => AlgoEnum.CreateROI;
        public override string Name { get; set; } = "创建ROI";
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
        public override bool Fun_action(DisplayUI display, List<IParaStrategy> strategys)
        {
            HObject regionGet = new HObject(); HOperatorSet.GenEmptyObj(out regionGet);
            HObject imgReduce = new HObject(); HOperatorSet.GenEmptyObj(out imgReduce);

            try
            {
                inPara.Coord = new CvCoord();
                var coord = strategys.ResolveFrom(inPara.CoordIn);

                var hcontext = inPara.HoRect;
                hcontext.GenRegion();
                inPara.Coord = new CvCoord(hcontext.Center);
                display.ReDispImage();
                display.DispRegion(hcontext, HColor.Blue);
                return true;
            }
            catch
            {
                throw;
            }
            finally
            {
                regionGet.Dispose();
                imgReduce.Dispose();
            }
        }
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
        }
        public override void SavePara(Form form, Dictionary<string, VsControlModel> VsControls)
        {
            inPara.CoordIn = VsControls["cmb_CoordIn"].Text;
        }
        public override void DispROI(DisplayUI display)
        {
            display.SetDrawMode(Name, inPara.HoRect, DrawEnum.DispRect);
        }

    }

    public class CreateROI
    {
        /// <summary> 指令类型 </summary>
        public readonly string Algorithm = "创建ROI";

        /// <summary> 跟随坐标 </summary>
        public string CoordIn { set; get; } = "默认";

        /// <summary> 坐标系 </summary>
        public CvCoord Coord { get; set; } = new CvCoord();

        /// <summary> 区域 </summary>
        public CvRegion HoRect { set; get; } = new CvRegion();

    }
}
