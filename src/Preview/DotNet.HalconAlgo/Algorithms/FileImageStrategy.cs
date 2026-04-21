using HalconDotNet;
using System;
using DotNet.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;


namespace DotNet.HalconAlgo
{
    public class FileImageStrategy : ParaStrategyBase<FileImage>
    {
        public override AlgoEnum Algorithm => AlgoEnum.FileImage;
        public override string Name { get; set; } = "文件图像";
        private int Index  = 0;       //图像下标
        private string[] ImagePaths;   //图像路径

        public override void Init(DisplayUI display)
        {
            try
            {
                ImagePaths = HalconHelper.GetPaths(inPara.ImageFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public override void Close(DisplayUI displayw)
        {
           
        }

        public override void GenTreeNode(TreeVisualizer tree)
        {
            tree.Branch(Name, branch => branch
                       .Node("图像", OutEnum.Image)
                   );

            ClearResolvers();
            RegisterOutput("图像", () => inPara.Image);

        }

        public override bool Fun_action(DisplayUI display, List<IParaStrategy> strategys)
        {
            HOperatorSet.GenEmptyObj(out inPara.Image);

            try
            {
                if (ImagePaths == null)
                {
                    ImagePaths = HalconHelper.GetPaths(inPara.ImageFolder);
                }

                if (Index >= ImagePaths.Length) Index = 0;

                HOperatorSet.ReadImage(out inPara.Image, ImagePaths[Index]);

                //判断图像是否为空
                if (!inPara.Image.NotNull())
                {
                    throw new NullReferenceException("图像`imgTemp`变量为空，加载图像异常！");
                }

                //旋转
                if (inPara.Rotate != 0)
                {
                    double pi = Convert.ToDouble(inPara.Rotate);
                    HOperatorSet.RotateImage(inPara.Image, out inPara.Image, pi, "constant");
                }

                //镜像
                switch (inPara.Mirror)
                {
                    case "行镜像":
                        HOperatorSet.MirrorImage(inPara.Image, out inPara.Image, "row");
                        break;
                    case "列镜像":
                        HOperatorSet.MirrorImage(inPara.Image, out inPara.Image, "column");
                        break;
                    case "原点镜像":
                        HOperatorSet.MirrorImage(inPara.Image, out inPara.Image, "diagonal");
                        break;
                    default: break;
                }

                var message = $"{Name} : W:{display.HoWidth} H:{display.HoHeight} Index:{Index}/{ImagePaths.Length}";
                
                display.DispImage(inPara.Image);
                display.DispText(message, inPara.FontX, inPara.FontY, inPara.FontSize, HColor.Green);

                Index++;
                return true;
            }
            catch
            {
                // 捕捉异常并重新抛出
                throw;
            }
        }

        public override void DispPara(Form form, Dictionary<string, VsControlModel> VsControls)
        {
            form.ShowTabs(TabPageEnum.FileImage, TabPageEnum.Display);

            VsControls.ShowComboBoxList(form, "cmb_Rotate", inPara.Rotate.ToString(), new[] { "0", "90", "180", "270" });
            VsControls.ShowComboBoxList(form, "cmb_Mirror", inPara.Mirror, new[] { "无", "行镜像", "列镜像", "原点镜像" });
            VsControls.ShowComboBox(form, "cmb_ImageFolder", inPara.ImageFolder, true);


            VsControls.ShowComboBoxList(form, "CB_FontX", inPara.FontX.ToString(), new[] { "20", "50" });
            VsControls.ShowComboBoxList(form, "CB_FontY", inPara.FontY.ToString(), new[] { "20", "50" });
            VsControls.ShowComboBoxList(form, "CB_FontSize", inPara.FontSize.ToString(), new[] { "15", "30" });
        }

        public override void SavePara(Form form, Dictionary<string, VsControlModel> VsControls)
        {
            inPara.Rotate = VsControls["cmb_Rotate"].Value;
            inPara.Mirror = VsControls["cmb_Mirror"].Text;
            inPara.ImageFolder = VsControls["cmb_ImageFolder"].Text;

            inPara.FontX = Convert.ToInt16(VsControls["CB_FontX"].Text);
            inPara.FontY = Convert.ToInt16(VsControls["CB_FontY"].Text);
            inPara.FontSize = Convert.ToInt16(VsControls["CB_FontSize"].Text);
        }

    }

    public class FileImage
    {
        /// <summary> 图像 </summary>
        public HObject Image = new HObject();

        /// <summary> 旋转 </summary>
        public int Rotate { get; set; } = 0;

        /// <summary> 镜像 </summary>
        public string Mirror { get; set; } = "无";

        /// <summary> 图像文件夹 </summary>
        public string ImageFolder { get; set; }

        /// <summary> 字体X坐标 </summary>
        public int FontX { set; get; } = 50;

        /// <summary> 字体Y坐标 </summary>
        public int FontY { set; get; } = 50;

        /// <summary> 字体大小 </summary>
        public int FontSize { set; get; } = 15;

    }
}
