using DotNet.Drawing;
using DotNet.HalconUI;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Windows.Forms;


namespace DotNet.HalconAlgo
{
    public class LineRotImageStrategy : ParaStrategyBase<LineRotImage>
    {
        public override AlgoEnum Algorithm => AlgoEnum.LineRotImage;
        public override string Name { get; set; } = "直线图像";
        public override int RunIndex { get; set; }

        public override void GenTreeNode(TreeVisualizer tree)
        {
            tree.Branch(Name, branch => branch
                       .Node("图像", OutEnum.Image)
                   );

            ClearResolvers();
            RegisterOutput("图像", () => inPara.Image);
        }
        public override bool Fun_action(HObject ho_Image, IHDisplay display)
        {
            display.SetImage(ho_Image);
            return Fun_action(display, null);
        }
        public override bool Fun_action(IHDisplay display, List<IParaStrategy> strategys)
        {
            try
            {
                HObject ho_Image;
                if (inPara.ImageIn == "默认")
                    ho_Image = display.HoImage;
                else
                    ho_Image = strategys.ResolveFrom<HObject>(inPara.ImageIn);

                if (ho_Image == null || !ho_Image.NotNull())
                    throw new NullReferenceException("图像来源为空！");

                CvLine line = strategys.ResolveFrom<CvLine>(inPara.LineIn);
                if (line == null || line.IsDegenerate)
                    throw new NullReferenceException("直线数据为空！！");

                double dx = line.End.X - line.Start.X;
                double dy = line.End.Y - line.Start.Y;
                double lineAngle = Math.Atan2(dy, dx); // 弧度

                double rotateAngle;
                if (inPara.AlignAxis == "平行Y轴")
                    rotateAngle = lineAngle - Math.PI / 2;
                else
                    rotateAngle = lineAngle;

                // 归一化到 [-π/2, π/2]，取最小旋转角度（直线无方向性）
                while (rotateAngle > Math.PI / 2) rotateAngle -= Math.PI;
                while (rotateAngle < -Math.PI / 2) rotateAngle += Math.PI;

                HOperatorSet.GetImageSize(ho_Image, out HTuple imgWidth, out HTuple imgHeight);
                double centerRow = imgHeight.D / 2;
                double centerCol = imgWidth.D / 2;

                HOperatorSet.HomMat2dIdentity(out HTuple HomMat2D);
                HOperatorSet.HomMat2dRotate(HomMat2D, rotateAngle, centerRow, centerCol, out HTuple HomMat2DRotate);
                inPara.Image.Dispose();
                HOperatorSet.AffineTransImage(ho_Image, out inPara.Image, HomMat2DRotate, "constant", "false");
                double angleDeg = rotateAngle * 180.0 / Math.PI;
              
                display.DispImage(inPara.Image);

                if (inPara.DispText)
                {
                    string message = $"{Name} : 对齐:{inPara.AlignAxis} 旋转:{angleDeg:F2}°";
                    display.DispText(message, inPara.FontX, inPara.FontY, inPara.FontSize, HColor.Green);
                }

                return true;
            }
            catch
            {
                throw;
            }
        }
        public override void DispPara(Control form, Dictionary<string, VsControlModel> VsControls)
        {
            form.ShowTabs(TabPageEnum.Parameter, TabPageEnum.Display);

            VsControls.ShowLabel(form, "lbl_100", "图像来源");
            VsControls.ShowComboBox(form, "cmb_100", inPara.ImageIn, false);
            VsControls.ShowButton(form, "btn_100", true);

            VsControls.ShowLabel(form, "lbl_101", "直线来源");
            VsControls.ShowComboBox(form, "cmb_101", inPara.LineIn, false);
            VsControls.ShowButton(form, "btn_101", true);

            VsControls.ShowLabel(form, "lbl_102", "对齐方式");
            VsControls.ShowComboBoxList(form, "cmb_102", inPara.AlignAxis, new[] { "平行X轴", "平行Y轴" });
            VsControls.ShowButton(form, "btn_102", false);

            //------------------------------------------
            VsControls.ShowCheckBox(form, "ckb_disp0", "显示文本", inPara.DispText);

            VsControls.ShowComboBoxDropDown(form, "CB_FontX", inPara.FontX.ToString(), new[] { "20", "50" });
            VsControls.ShowComboBoxDropDown(form, "CB_FontY", inPara.FontY.ToString(), new[] { "20", "50" });
            VsControls.ShowComboBoxDropDown(form, "CB_FontSize", inPara.FontSize.ToString(), new[] { "15", "30" });
        }
        public override void SavePara(Control form, Dictionary<string, VsControlModel> VsControls)
        {
            inPara.ImageIn = VsControls["cmb_100"].AsString();
            inPara.LineIn = VsControls["cmb_101"].AsString();
            inPara.AlignAxis = VsControls["cmb_102"].AsString();

            //------------------------------------------
            inPara.DispText = VsControls["ckb_disp0"].AsBool();

            inPara.FontX = VsControls["CB_FontX"].AsInt();
            inPara.FontY = VsControls["CB_FontY"].AsInt();
            inPara.FontSize = VsControls["CB_FontSize"].AsInt();
        }

    }

    public class LineRotImage : AlgoFont
    {
        public LineRotImage()
        {
            HOperatorSet.GenEmptyObj(out Image);
        }

        /// <summary> 图像来源 </summary>
        public string ImageIn { set; get; } = "默认";

        /// <summary> 直线来源 </summary>
        public string LineIn { set; get; } = "默认";

        /// <summary> 输出图像 </summary>
        public HObject Image = new HObject();

        /// <summary> 对齐轴 </summary>
        public string AlignAxis { set; get; } = "平行X轴";
    }
}
