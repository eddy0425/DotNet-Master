using HalconDotNet;
using System;
using DotNet.Drawing;
using DotNet.Vision.Abstractions;
using System.Collections.Generic;


namespace DotNet.HalconAlgo
{
    public class FileImageStrategy : ParaStrategyBase<FileImage>
    {
        public override AlgoEnum Algorithm => AlgoEnum.FileImage;
        public override string Name { get; set; } = "文件图像";
        public override int RunIndex { get; set; }

        private int Index  = 0;       //图像下标
        private string[] ImagePaths;   //图像路径

        public override void GenTreeNode(ITreeVisualizer tree)
        {
            tree.Branch(Name, branch => branch
                       .Node("图像", OutEnum.Image)
                   );

            ClearResolvers();
            RegisterOutput("图像", () => inPara.Image);

        }
        public override bool Fun_action(IHDisplay display, List<IParaStrategy> strategys)
        {
            try
            {
                if (ImagePaths == null)
                {
                    ImagePaths = HalconController.GetPaths(inPara.ImageFolder);
                }

                if (Index >= ImagePaths.Length) Index = 0;

                inPara.Image.Dispose();
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
                    HOperatorSet.RotateImage(inPara.Image, out HObject imgRotated, pi, "constant");
                    inPara.Image.Dispose();
                    inPara.Image = imgRotated;
                }

                //镜像
                switch (inPara.Mirror)
                {
                    case "行镜像":
                        HOperatorSet.MirrorImage(inPara.Image, out HObject imgMirrored1, "row");
                        inPara.Image.Dispose();
                        inPara.Image = imgMirrored1;
                        break;
                    case "列镜像":
                        HOperatorSet.MirrorImage(inPara.Image, out HObject imgMirrored2, "column");
                        inPara.Image.Dispose();
                        inPara.Image = imgMirrored2;
                        break;
                    case "原点镜像":
                        HOperatorSet.MirrorImage(inPara.Image, out HObject imgMirrored3, "diagonal");
                        inPara.Image.Dispose();
                        inPara.Image = imgMirrored3;
                        break;
                    default: break;
                }

                display.DispImage(inPara.Image);

                if (inPara.DispText)
                {
                    string message = $"{Name} : W:{display.HoWidth} H:{display.HoHeight} 索引:{Index}/{ImagePaths.Length}";
                    display.DispText(message, new Point2d(inPara.FontX, inPara.FontY), DrawStyle.Of(HColor.Green, inPara.FontSize));
                }

                Index++;
                return true;
            }
            catch
            {
                // 捕捉异常并重新抛出
                throw;
            }
        }
        public override void DispPara(IParaUiHost ui)
        {
            ui.ShowTabs(TabPageEnum.FileImage, TabPageEnum.Display);

            ui.ShowComboBoxList("cmb_Rotate", inPara.Rotate.ToString(), new[] { "0", "90", "180", "270" });
            ui.ShowComboBoxList("cmb_Mirror", inPara.Mirror, new[] { "无", "行镜像", "列镜像", "原点镜像" });
            ui.ShowComboBox("cmb_ImageFolder", inPara.ImageFolder, true);

            //------------------------------------------
            ui.ShowCheckBox("ckb_disp0", "显示文本", inPara.DispText);

            ui.ShowComboBoxDropDown("CB_FontX", inPara.FontX.ToString(), new[] { "20", "50" });
            ui.ShowComboBoxDropDown("CB_FontY", inPara.FontY.ToString(), new[] { "20", "50" });
            ui.ShowComboBoxDropDown("CB_FontSize", inPara.FontSize.ToString(), new[] { "15", "30" });
        }
        public override void SavePara(IParaUiHost ui)
        {
            inPara.Rotate = ui.GetInt("cmb_Rotate");
            inPara.Mirror = ui.GetString("cmb_Mirror");
            inPara.ImageFolder = ui.GetString("cmb_ImageFolder");

            //------------------------------------------
            inPara.DispText = ui.GetBool("ckb_disp0");

            inPara.FontX = ui.GetInt("CB_FontX");
            inPara.FontY = ui.GetInt("CB_FontY");
            inPara.FontSize = ui.GetInt("CB_FontSize");
        }
        public override void Init(IRoiHost host)
        {
            try
            {
                // Init 可能被重复调用，先释放上一次的句柄再重建，避免累积泄漏
                inPara.Image?.Dispose();
                HOperatorSet.GenEmptyObj(out inPara.Image);
                ImagePaths = HalconController.GetPaths(inPara.ImageFolder);
            }
            catch (Exception ex)
            {
                // Init 在宿主窗体构造期被循环调用, 这里弹窗会卡住整个启动流程;
                // 图像目录无效属于可恢复配置问题, 记录后让其余策略继续初始化.
                ImagePaths = null;
                Log.Error(nameof(FileImageStrategy), $"初始化图像目录失败: {inPara.ImageFolder}", ex);
            }
        }
        public override void Close(IRoiHost host)
        {

        }

    }

    public class FileImage : AlgoFont
    {
        /// <summary> 图像 </summary>
        /// <remarks>不加 = new HObject() 初始化器：句柄由 <see cref="FileImageStrategy.Init"/> 创建，否则初始化器创建的句柄会被覆盖且永不释放。</remarks>
        public HObject Image;

        /// <summary> 旋转 </summary>
        public int Rotate { get; set; } = 0;

        /// <summary> 镜像 </summary>
        public string Mirror { get; set; } = "无";

        /// <summary> 图像文件夹 </summary>
        public string ImageFolder { get; set; }

    }
}
