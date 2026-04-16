using DotNet.Drawing;
using HalconDotNet;
using System;
using System.Collections.Generic;


namespace DotNet.VisionMaster.Algorithms
{
    public class FitArcMidpointStrategy : ParaStrategyBase<FitArcMidpoint>
    {
        public override string Name => "拟合圆弧中点";
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
                var image = strategys.ResolveFrom(inPara.ImageIn);
                var region = strategys.ResolveFrom(inPara.RegionIn);
                var coord = strategys.ResolveFrom(inPara.CoordIn);

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
                       .Node("直线", OutEnum.Line, line => line
                           .Branch("起点", pt => pt
                               .Node("行", OutEnum.Number)
                               .Node("列", OutEnum.Number)
                           )
                           .Branch("终点", pt => pt
                               .Node("行", OutEnum.Number)
                               .Node("列", OutEnum.Number)
                           )
                       )
                       .CommonNodes()
                   );

            ClearResolvers();
            RegisterOutput("直线", () => inPara.Line);
            RegisterOutput("直线/起点", () => inPara.Line.Start);
            RegisterOutput("直线/起点/行", () => inPara.Line.Start.Y);
            RegisterOutput("直线/起点/列", () => inPara.Line.Start.X);
            RegisterOutput("直线/终点", () => inPara.Line.End);
            RegisterOutput("直线/终点/行", () => inPara.Line.End.Y);
            RegisterOutput("直线/终点/列", () => inPara.Line.End.X);

        }
        public override void DispPara(ParaForm form, Dictionary<string, VsControlModel> VsControls)
        {
            form.ShowTabs(TabPageEnum.Parameter, TabPageEnum.Region, TabPageEnum.Display);

            VsControls.ShowComboBox(form, "cmb_CoordIn", inPara.CoordIn.ToString(), false);

            CvRegion hRegion = inPara.HoRect;
            VsControls.ShowComboBox(form, "cmb_Width", hRegion.Width.ToString(), false);
            VsControls.ShowComboBox(form, "cmb_Height", hRegion.Height.ToString(), false);
            VsControls.ShowComboBox(form, "cmb_TopLeft", $"{hRegion.TopLeft.X};{hRegion.TopLeft.Y}", false);
            VsControls.ShowComboBox(form, "cmb_BottomRight", $"{hRegion.BottomRight.X};{hRegion.BottomRight.Y}", false);
            VsControls.ShowComboBox(form, "cmb_Center", $"{hRegion.Center.X};{hRegion.Center.Y}", false);

            VsControls.ShowLabel(form, "lbl_100", "图像来源");
            VsControls.ShowComboBox(form, "cmb_100", inPara.ImageIn, false);
            VsControls.ShowButton(form, "btn_100", true);

            VsControls.ShowLabel(form, "lbl_101", "区域来源");
            VsControls.ShowComboBox(form, "cmb_101", inPara.RegionIn, false);
            VsControls.ShowButton(form, "btn_101", true);

            VsControls.ShowLabel(form, "lbl_102", "过渡方向");
            VsControls.ShowComboBoxList(form, "cmb_102", inPara.Transition, new[] { "由黑到白", "由白到黑", "全部" });
            VsControls.ShowButton(form, "btn_102", false);

            VsControls.ShowLabel(form, "lbl_103", "选择");
            VsControls.ShowComboBoxList(form, "cmb_103", inPara.ContourType, new[] { "第一条边", "第二条边", "最后一条", "全部" });
            VsControls.ShowButton(form, "btn_103", false);

            VsControls.ShowLabel(form, "lbl_104", "滤波");
            VsControls.ShowComboBoxDropDown(form, "cmb_104", inPara.Sigma.ToString(), new[] { "0", "1" });
            VsControls.ShowButton(form, "btn_104", false);

            VsControls.ShowLabel(form, "lbl_105", "阈值");
            VsControls.ShowComboBoxDropDown(form, "cmb_105", inPara.Threshold.ToString(), new[] { "30", "50" });
            VsControls.ShowButton(form, "btn_105", false);

            VsControls.ShowLabel(form, "lbl_110", "步距");
            VsControls.ShowComboBoxDropDown(form, "cmb_110", inPara.StepPace.ToString(), new[] { "2", "5", "10" });

            VsControls.ShowLabel(form, "lbl_111", "步宽");
            VsControls.ShowComboBoxDropDown(form, "cmb_111", inPara.StepWidth.ToString(), new[] { "2", "5", "10" });

            VsControls.ShowLabel(form, "lbl_112", "最大偏差");
            VsControls.ShowComboBoxDropDown(form, "cmb_112", inPara.MaxErr.ToString(), new[] { "1", "3", "5", "10" });
        }
        public override void SavePara(ParaForm form, Dictionary<string, VsControlModel> VsControls)
        {
            inPara.CoordIn = VsControls["cmb_CoordIn"].Text;
            inPara.ImageIn = VsControls["cmb_100"].Text;
            inPara.RegionIn = VsControls["cmb_101"].Text;
            inPara.Transition = VsControls["cmb_102"].Text;
            inPara.ContourType = VsControls["cmb_103"].Text;
            inPara.Sigma = Convert.ToInt16(VsControls["cmb_104"].Text);
            inPara.Threshold = Convert.ToInt16(VsControls["cmb_105"].Text);

            inPara.StepPace = Convert.ToInt16(VsControls["cmb_110"].Text);
            inPara.StepWidth = Convert.ToInt16(VsControls["cmb_111"].Text);
            inPara.MaxErr = Convert.ToInt16(VsControls["cmb_112"].Text);
        }
        public override void DispROI(DisplayUI display)
        {
            display.SetDrawMode(Name, inPara.HoRect, DrawEnum.DispRect);
        }

    }

    public class FitArcMidpoint
    {
        /// <summary> 指令类型 </summary>
        public readonly string Algorithm = "拟合圆弧中点";

        /// <summary> 图像来源 </summary>
        public string ImageIn { set; get; } = "默认";

        /// <summary> 区域来源 </summary>
        public string RegionIn { set; get; } = "默认";

        /// <summary> 跟随坐标 </summary>
        public string CoordIn { set; get; } = "默认";

        public CvLine Line { set; get; }

        /// <summary> 区域 </summary>
        public CvRegion HoRect { set; get; } = new CvRegion();

        /// <summary> 过渡方向 </summary>
        public string Transition { set; get; }

        /// <summary> 选择 </summary>
        public string ContourType { set; get; }

        /// <summary> 滤波 </summary>
        public int Sigma { set; get; }

        /// <summary>
        /// 阈值 val = 0: 自动阈值, val > 0: 手动阈值, val = -1: 能量最强, val 小于 -1: 百分比阈值
        /// </summary>
        public int Threshold { set; get; }

        /// <summary> 步距 </summary>
        public int StepPace { set; get; }

        /// <summary> 步宽 </summary>
        public int StepWidth { set; get; }

        /// <summary> 最大偏差 </summary>
        public int MaxErr { set; get; }
    }
}
