using DotNet.HWindows;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace DotNet.VisionMaster
{
    public class CreateROIStrategy : ParaStrategyBase<CreateROI>
    {
        public override string Name => "创建ROI";

        public override bool Fun_action()
        {
            throw new NotImplementedException();
        }
        public override void Init(DrawContext draw)
        {
            draw.RectangleEvent += RectangleEvent;
        }
        public override void Close(DrawContext draw)
        {
            draw.RectangleEvent -= RectangleEvent;  //RemoveEvent
        }
        private void RectangleEvent(object sender, DrawContext.DrawRectangleArgs e)
        {
            if (e.Name == Name)
            {
                inPara.HContext.Re2Point(e.TopLeft, e.BottomRight);
                inPara.HContext.GenRegion();
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
        }
        public override void DispPara(ParaForm form, Dictionary<string, VsControlModel> VsControls)
        {
            form.ShowTabs(TabPageEnum.Region, TabPageEnum.Display);

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

        /// <summary> 区域 </summary>
        public HObjContext HContext { set; get; } = new HObjContext();

    }
}
