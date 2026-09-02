using DotNet.Drawing;
using DotNet.HalconUI;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Windows.Forms;


namespace DotNet.HalconAlgo
{
    public class RotateImageStrategy : ParaStrategyBase<RotateImage>
    {
        public override AlgoEnum Algorithm => AlgoEnum.RotateImage;
        public override string Name { get; set; } = "旋转图像";
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
            // 传空集合而不是 null: 另一重载内部会对 strategys 做 ResolveFrom, null 会直接 NRE.
            return Fun_action(display, StrategyExtensions.EmptyList());
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

                string message;

                if (inPara.RotateType == "图像中心")
                {
                    if (inPara.RotateAngle != 0)
                    {
                        // HALCON rotate_image: Phi 单位为度
                        inPara.Image.Dispose();
                        HOperatorSet.RotateImage(ho_Image, out inPara.Image, inPara.RotateAngle, "constant");
                    }
                    else
                    {
                        inPara.Image.Dispose();
                        HOperatorSet.CopyObj(ho_Image, out inPara.Image, 1, 1);
                    }
                    message = $"{Name} : 方式:{inPara.RotateType} 角度:{inPara.RotateAngle:F2}°";
                }
                else
                {
                    CvCoord hCoord = strategys.ResolveFrom<CvCoord>(inPara.CoordIn);

                    double baseRow = hCoord.Y;
                    double baseCol = hCoord.X;
                    // CvCoord.Angle 已是弧度，统一以度数做归一化与逻辑处理
                    double baseAglDeg = hCoord.AngleDegrees;

                    // 统一走 MathHelper 的归一化, 不再手写 %360 三段式 (原写法在 ±180 处的取舍不明确)
                    baseAglDeg = MathHelper.NormalizeAngleDegrees(baseAglDeg);

                    switch (inPara.RotateType)
                    {
                        case "坐标系":
                        case "坐标系X轴":
                            baseAglDeg = -baseAglDeg;
                            break;
                        case "坐标系Y轴":
                            // 目标是把坐标系 Y 轴摆正, 所需旋转量为 (±90 - baseAglDeg).
                            // baseAglDeg == 0 时 +90 与 -90 在几何上等价(相差 180°, 都能让 Y 轴竖直),
                            // 这里明确归入 ">= 0" 分支取 +90, 与 baseAglDeg → 0⁺ 的极限保持连续;
                            // 原实现把 0 漏在两个分支之外, 结果退化成 "不旋转", 与两侧极限都不连续.
                            baseAglDeg = baseAglDeg >= 0 ? 90 - baseAglDeg : -90 - baseAglDeg;
                            break;
                        default:
                            // 未知旋转方式: 保持原角度不做换算, 但记录下来便于发现配置写错.
                            Log.Warn(nameof(RotateImageStrategy),
                                     $"未知的旋转方式 '{inPara.RotateType}', 按原角度处理.");
                            break;
                    }

                    HOperatorSet.HomMat2dIdentity(out HTuple HomMat2D);
                    HOperatorSet.HomMat2dRotate(HomMat2D, baseAglDeg.ToRadians(), baseRow, baseCol, out HTuple HomMat2DRotate);
                    inPara.Image.Dispose();
                    HOperatorSet.AffineTransImage(ho_Image, out inPara.Image, HomMat2DRotate, "constant", "false");

                    message = $"{Name} : 方式:{inPara.RotateType} 坐标:({baseCol:F2},{baseRow:F2}) 角度:{baseAglDeg:F2}°";
                }

                display.DispImage(inPara.Image);

                if (inPara.DispText)
                {
                    display.DispText(message, new Point2d(inPara.FontX, inPara.FontY), DrawStyle.Of(HColor.Green, inPara.FontSize));
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

            VsControls.ShowLabel(form, "lbl_101", "选择方式");
            VsControls.ShowComboBoxList(form, "cmb_101", inPara.RotateType, new[] { "图像中心", "坐标系", "坐标系X轴", "坐标系Y轴" });
            VsControls.ShowButton(form, "btn_101", false);

            if (inPara.RotateType == "图像中心")
            {
                VsControls.ShowLabel(form, "lbl_102", "旋转角度");
                VsControls.ShowComboBoxDropDown(form, "cmb_102", inPara.RotateAngle.ToString(), new[] { "0", "90", "180", "270" });
                VsControls.ShowButton(form, "btn_102", false);
            }
            else
            {
                VsControls.ShowLabel(form, "lbl_102", "坐标系");
                VsControls.ShowComboBox(form, "cmb_102", inPara.CoordIn, false);
                VsControls.ShowButton(form, "btn_102", true);
            }

            //------------------------------------------
            VsControls.ShowCheckBox(form, "ckb_disp0", "显示文本", inPara.DispText);

            VsControls.ShowComboBoxDropDown(form, "CB_FontX", inPara.FontX.ToString(), new[] { "20", "50" });
            VsControls.ShowComboBoxDropDown(form, "CB_FontY", inPara.FontY.ToString(), new[] { "20", "50" });
            VsControls.ShowComboBoxDropDown(form, "CB_FontSize", inPara.FontSize.ToString(), new[] { "15", "30" });
        }
        public override void SavePara(Control form, Dictionary<string, VsControlModel> VsControls)
        {
            inPara.ImageIn = VsControls["cmb_100"].AsString();
            inPara.RotateType = VsControls["cmb_101"].AsString();

            if (inPara.RotateType == "图像中心")
            {
                // 用户可能输入非数字, 这里保留 TryParse 的容错: 解析失败时不覆盖原值.
                if (float.TryParse(VsControls["cmb_102"].AsString(), out float angle))
                    inPara.RotateAngle = angle;
            }
            else
            {
                inPara.CoordIn = VsControls["cmb_102"].AsString();
            }

            //------------------------------------------
            inPara.DispText = VsControls["ckb_disp0"].AsBool();

            inPara.FontX = VsControls["CB_FontX"].AsInt();
            inPara.FontY = VsControls["CB_FontY"].AsInt();
            inPara.FontSize = VsControls["CB_FontSize"].AsInt();
        }

    }

    public class RotateImage : AlgoFont
    {
        public RotateImage()
        {
            HOperatorSet.GenEmptyObj(out Image);
        }

        /// <summary> 图像来源 </summary>
        public string ImageIn { set; get; } = "默认";

        /// <summary> 跟随坐标 </summary>
        public string CoordIn { set; get; } = "默认";

        /// <summary> 输出图像 </summary>
        /// <remarks>不加 = new HObject() 初始化器：句柄统一由构造函数的 GenEmptyObj 创建，否则初始化器创建的句柄会被覆盖且永不释放。</remarks>
        public HObject Image;

        /// <summary> 旋转方式 </summary>
        public string RotateType { set; get; } = "图像中心";

        /// <summary> 旋转角度（度） </summary>
        public float RotateAngle { set; get; } = 0;
    }
}
