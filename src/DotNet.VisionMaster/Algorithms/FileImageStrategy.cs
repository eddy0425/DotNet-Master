using DotNet.HWindows;
using HalconDotNet;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;


namespace DotNet.VisionMaster
{
    public class FileImageStrategy : ParaStrategyBase<FileImage>
    {
        public override string Name => "加载图像";
        private int Index  = 0;       //图像下标
        private string[] ImagePaths;   //图像路径
        private string ThisFilePath => ImagePaths[Index];  //当前图像文件

        public Action TestImageRun { get; set; }

        public override void Init(DrawContext draw)
        {
            try
            {
                ImagePaths = GetPaths(inPara.ImageFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public override void Close(DrawContext draw)
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

        public override bool Fun_action(DisplayForm display, List<IParaStrategy> strategys)
        {
            HOperatorSet.GenEmptyObj(out inPara.Image);

            try
            {
                if (ImagePaths == null)
                {
                    ImagePaths = GetPaths(inPara.ImageFolder);
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

                var message = $"{Name} : W:{display.ho_Width} H:{display.ho_Height} Index:{Index}/{ImagePaths.Length}";
                
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

        public override void DispPara(ParaForm form, Dictionary<string, VsControlModel> VsControls)
        {
            form.ShowTabs(TabPageEnum.FileImage, TabPageEnum.Display);

            VsControls.ShowComboBoxList(form, "cmb_Rotate", inPara.Rotate.ToString(), new[] { "0", "90", "180", "270" });
            VsControls.ShowComboBoxList(form, "cmb_Mirror", inPara.Mirror, new[] { "无", "行镜像", "列镜像", "原点镜像" });
            VsControls.ShowComboBox(form, "cmb_ImageFolder", inPara.ImageFolder, true);


            VsControls.ShowComboBoxList(form, "CB_FontX", inPara.FontX.ToString(), new[] { "20", "50" });
            VsControls.ShowComboBoxList(form, "CB_FontY", inPara.FontY.ToString(), new[] { "20", "50" });
            VsControls.ShowComboBoxList(form, "CB_FontSize", inPara.FontSize.ToString(), new[] { "15", "30" });
        }

        public override void SavePara(ParaForm form, Dictionary<string, VsControlModel> VsControls)
        {
            inPara.Rotate = VsControls["cmb_Rotate"].Value;
            inPara.Mirror = VsControls["cmb_Mirror"].Text;
            inPara.ImageFolder = VsControls["cmb_ImageFolder"].Text;

            inPara.FontX = Convert.ToInt16(VsControls["CB_FontX"].Text);
            inPara.FontY = Convert.ToInt16(VsControls["CB_FontY"].Text);
            inPara.FontSize = Convert.ToInt16(VsControls["CB_FontSize"].Text);
        }

        private string[] GetPaths(string imageFolder)
        {
            var imagePaths = Directory.GetFiles(imageFolder);
            if (imagePaths.Length == 0)
                throw new InvalidOperationException("图片路径为空！");

            var numericFiles = new List<Tuple<int, string>>();
            var nonNumericFiles = new List<string>();

            foreach (var path in imagePaths)
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                int number;
                if (int.TryParse(fileName, out number))
                    numericFiles.Add(Tuple.Create(number, path));
                else
                    nonNumericFiles.Add(path);
            }

            var sorted = numericFiles.OrderBy(x => x.Item1)
                                    .Select(x => x.Item2)
                                    .Concat(nonNumericFiles)
                                    .ToArray();
            return sorted;
        }
    }

    public class FileImage
    {
        /// <summary> 指令类型 </summary>
        public readonly string Algorithm = "加载图像";

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
