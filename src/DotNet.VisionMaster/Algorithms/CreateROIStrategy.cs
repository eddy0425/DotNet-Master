using HalconDotNet;
using DotNet.HWindows;
using System.Collections.Generic;

namespace DotNet.VisionMaster
{
    public class CreateROIStrategy : ParaStrategyBase<CreateROI>
    {
        public override string Name => "创建ROI";
        public override void Init(DrawContext draw)
        {
            draw.RectangleEvent += RectEvent;
        }
        public override void Close(DrawContext draw)
        {
            draw.RectangleEvent -= RectEvent;
        }
        private void RectEvent(object sender, DrawContext.DrawRectangleArgs e)
        {
            if (e.Name == Name)
            {
                inPara.HContext.Re2Point(e.TopLeft, e.BottomRight);
                inPara.HContext.GenRegion();
            }
        }
        public override bool Fun_action(DisplayForm display, List<IParaStrategy> strategys)
        {
            HObject regionGet = new HObject(); HOperatorSet.GenEmptyObj(out regionGet);
            HObject imgReduce = new HObject(); HOperatorSet.GenEmptyObj(out imgReduce);

            try
            {
                inPara.Coord = new CvCoord();
                var coord = strategys.ResolveFrom(inPara.CoordIn);

                var hcontext = inPara.HContext;
                hcontext.GenRegion();
                inPara.Coord.center = hcontext.Center;
                display.DispRegion(hcontext.HoRect);
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
            ClearResolvers();
            RegisterOutput("坐标系", () => inPara.Coord);
            RegisterOutput("坐标系/原点", () => inPara.Coord.center);
            RegisterOutput("坐标系/原点/行", () => inPara.Coord.Y);
            RegisterOutput("坐标系/原点/列", () => inPara.Coord.X);
            RegisterOutput("坐标系/角度", () => inPara.Coord.angle);
            RegisterOutput("区域", () => inPara.HContext);

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
        }
        public override void DispPara(ParaForm form, Dictionary<string, VsControlModel> VsControls)
        {
            form.ShowTabs(TabPageEnum.Region, TabPageEnum.Display);

            VsControls.ShowComboBox(form, "cmb_CoordIn", inPara.CoordIn.ToString(), false);

            HObjContext hRegion = inPara.HContext;
            VsControls.ShowComboBox(form, "cmb_Width", hRegion.Width.ToString(), false);
            VsControls.ShowComboBox(form, "cmb_Height", hRegion.Height.ToString(), false);
            VsControls.ShowComboBox(form, "cmb_TopLeft", $"{hRegion.TopLeft.X};{hRegion.TopLeft.Y}", false);
            VsControls.ShowComboBox(form, "cmb_BottomRight", $"{hRegion.BottomRight.X};{hRegion.BottomRight.Y}", false);
            VsControls.ShowComboBox(form, "cmb_Center", $"{hRegion.Center.X};{hRegion.Center.Y}", false);
        }
        public override void SavePara(ParaForm form, Dictionary<string, VsControlModel> VsControls)
        {
            inPara.CoordIn = VsControls["cmb_CoordIn"].Text;
        }
        public override void DispROI(DisplayForm display)
        {
            display.SetDrawMode(Name, inPara.HContext, WinDrawType.DispRect);
        }

    }

    public class CreateROI
    {
        /// <summary> 指令类型 </summary>
        public readonly string Algorithm = "直线查找";

        /// <summary> 跟随坐标 </summary>
        public string CoordIn { set; get; } = "默认";

        /// <summary> 坐标系 </summary>
        public CvCoord Coord { get; set; } = new CvCoord();

        /// <summary> 区域 </summary>
        public HObjContext HContext { set; get; } = new HObjContext();

    }
}
