using DotNet.Drawing;
using DotNet.Vision.Abstractions;
using HalconDotNet;
using System;
using System.IO;
using System.Collections.Generic;


namespace DotNet.HalconAlgo
{
    public class ShapeModelStrategy : ParaStrategyBase<ShapeModel>, IRoiEditable, ITemplateEditable
    {
        public override AlgoEnum Algorithm => AlgoEnum.ShapeModel;
        public override string Name { get; set; } = "形状匹配";
        public override int RunIndex { get; set; }

        public override void GenTreeNode(ITreeVisualizer tree)
        {
            tree.Branch(Name, branch => branch
                       .Node("坐标系", OutEnum.Coord, line => line
                           .Branch("原点", pt => pt
                               .Node("行", OutEnum.Number)
                               .Node("列", OutEnum.Number)
                           )
                           .Node("角度", OutEnum.Array)
                       )
                   );

            ClearResolvers();
            RegisterOutput("TmplPoint", () => inPara.TmplPoint);
            RegisterOutput("坐标系", () => inPara.Coord);
            RegisterOutput("坐标系/原点", () => inPara.Coord.Center);
            RegisterOutput("坐标系/原点/行", () => inPara.Coord.Y);
            RegisterOutput("坐标系/原点/列", () => inPara.Coord.X);
            RegisterOutput("坐标系/角度", () => inPara.Coord.Angle.Radians);
        }
        public override bool Fun_action(HObject ho_Image, IHDisplay display)
        {
            display.SetImage(ho_Image);
            // 传空集合而不是 null: 另一重载内部会对 strategys 做 ResolveFrom, null 会直接 NRE.
            return Fun_action(display, StrategyExtensions.EmptyList());
        }
        public override bool Fun_action(IHDisplay display, List<IParaStrategy> strategys)
        {
            HObject imgReduced; HOperatorSet.GenEmptyObj(out imgReduced);
            HObject ho_SelRect; HOperatorSet.GenEmptyObj(out ho_SelRect);

            try
            {
                HObject ho_Image = (inPara.ImageIn == "默认")
                    ? display.HoImage
                    : strategys.ResolveFrom<HObject>(inPara.ImageIn);

                HObject ho_Rect = (inPara.RegionIn == "默认")
                    ? inPara.HoRect.HoRegion
                    : strategys.ResolveFrom<HObject>(inPara.RegionIn);

                if (inPara.DispRegion) display.Disp(ho_Rect, DrawStyle.Of(HColor.Blue));

                inPara.Results = new List<ModelResult>();

                for (int j = 0; j < ho_Rect.CountObj(); j++)
                {
                    // 释放上轮句柄，避免 SelectObj/ReduceDomain 反复 out 造成泄漏
                    ho_SelRect.Dispose();
                    HOperatorSet.SelectObj(ho_Rect, out ho_SelRect, j + 1);

                    imgReduced.Dispose();
                    HOperatorSet.ReduceDomain(ho_Image, ho_SelRect, out imgReduced);

                    //查找模板
                    HOperatorSet.FindShapeModel(imgReduced, inPara.ModelID, inPara.AngleStart.TupleRad(), inPara.AngleExtent.TupleRad(),
                                 inPara.MinScore, inPara.NumMatches, inPara.MaxOverlap, inPara.SubPixel, inPara.NumLevels, inPara.Greediness,
                                 out HTuple row, out HTuple column, out HTuple angle, out HTuple score);

                    for (int i = 0; i < score.Length; i++)
                    {
                        var result = new ModelResult(row[i], column[i], angle[i], score[i]);
                        inPara.Results.Add(result);
                        inPara.Coord = result.Coord;

                        inPara.HoContour.Dispose();
                        HOperatorSet.GetShapeModelContours(out inPara.HoContour, inPara.ModelID, 1);
                        HOperatorSet.VectorAngleToRigid(0, 0, 0, result.Row, result.Column, result.Angle, out HTuple hv_HomMat2D);
                        HOperatorSet.AffineTransContourXld(inPara.HoContour, out HObject contoursAffineTrans, hv_HomMat2D);
                        inPara.HoContour.Dispose();
                        inPara.HoContour = contoursAffineTrans;

                        if(inPara.DispContour) display.Disp(inPara.HoContour, DrawStyle.Of(HColor.Green));
                        if (inPara.DispPoint) display.Disp(result.Coord, DrawStyle.Of(HColor.Red));

                    }
                }

                if (inPara.DispText)
                {
                    int cnt = inPara.Results.Count;
                    double bestScore = cnt > 0 ? inPara.Results[0].Score : 0;
                    string message = $"{Name} : 数量:{cnt} 最佳得分:{bestScore:F3} 角度范围:[{inPara.AngleStart}°,{(inPara.AngleStart.D + inPara.AngleExtent.D)}°]";
                    display.DispText(message, new Point2d(inPara.FontX, inPara.FontY), DrawStyle.Of(HColor.Green, inPara.FontSize));
                }

                return true;
            }
            finally
            {
                imgReduced.Dispose();
                ho_SelRect.Dispose();
            }
        }
        public override void DispPara(IParaUiHost ui)
        {
            ui.ShowTabs(TabPageEnum.Parameter, TabPageEnum.Region, TabPageEnum.Matching, TabPageEnum.Display);

            CvRegion hRegion = inPara.HoRect;
            ui.ShowComboBox("cmb_Width", hRegion.Width.ToString(), false);
            ui.ShowComboBox("cmb_Height", hRegion.Height.ToString(), false);
            ui.ShowComboBox("cmb_TopLeft", $"{hRegion.TopLeft.X};{hRegion.TopLeft.Y}", false);
            ui.ShowComboBox("cmb_BottomRight", $"{hRegion.BottomRight.X};{hRegion.BottomRight.Y}", false);
            ui.ShowComboBox("cmb_Center", $"{hRegion.Center.X};{hRegion.Center.Y}", false);

            ui.ShowComboBox("cmb_CoordIn", inPara.CoordIn.ToString(), false);

            //基本参数
            ui.ShowLabel("lbl_100", "图像来源");
            ui.ShowComboBox("cmb_100", inPara.ImageIn.ToString(), false);
            ui.ShowButton("btn_100", true);

            ui.ShowLabel("lbl_101", "区域来源");
            ui.ShowComboBox("cmb_101", inPara.RegionIn, false);
            ui.ShowButton("btn_101", true);

            ui.ShowLabel("lbl_102", "起始角度");
            ui.ShowComboBoxList("cmb_102", inPara.AngleStart.ToString(), new[] { "-90", "-45" });
            ui.ShowButton("btn_102", false);

            ui.ShowLabel("lbl_103", "增量角度");
            ui.ShowComboBoxList("cmb_103", inPara.AngleExtent.ToString(), new[] { "90", "180" });
            ui.ShowButton("btn_103", false);

            ui.ShowLabel("lbl_104", "最大重叠率");
            ui.ShowComboBoxList("cmb_104", inPara.MaxOverlap.ToString(), new[] { "0", "0.3", "0.5" });
            ui.ShowButton("btn_104", false);

            ui.ShowLabel("lbl_110", "匹配数量");
            ui.ShowComboBoxList("cmb_110", inPara.NumMatches == 0 ? "多个" : inPara.NumMatches.ToString(), new[] { "1", "2", "3", "多个" });
            ui.ShowButton("btn_110", false);

            ui.ShowLabel("lbl_111", "得分");
            ui.ShowComboBoxDropDown("cmb_111", inPara.MinScore.ToString(), new[] { "0.5", "0.7" });

            ui.ShowLabel("lbl_112", "金字塔");
            ui.ShowComboBoxList("cmb_112", inPara.NumLevels.ToString(), new[] { "0", "2" });

            //------------------------------------------
            ui.ShowCheckBox("ckb_disp0", "显示文本", inPara.DispText);
            ui.ShowCheckBox("ckb_disp1", "查找区域", inPara.DispRegion);
            ui.ShowCheckBox("ckb_disp2", "显示轮廓", inPara.DispContour);
            ui.ShowCheckBox("ckb_disp3", "显示点", inPara.DispPoint);

            ui.ShowComboBoxDropDown("CB_FontX", inPara.FontX.ToString(), new[] { "20", "50" });
            ui.ShowComboBoxDropDown("CB_FontY", inPara.FontY.ToString(), new[] { "20", "50" });
            ui.ShowComboBoxDropDown("CB_FontSize", inPara.FontSize.ToString(), new[] { "15", "30" });
        }
        public override void SavePara(IParaUiHost ui)
        {
            //基本参数
            inPara.CoordIn = ui.GetString("cmb_CoordIn");
            inPara.ImageIn = ui.GetString("cmb_100");
            inPara.RegionIn = ui.GetString("cmb_101");
            inPara.AngleStart = ui.GetDouble("cmb_102");
            inPara.AngleExtent = ui.GetDouble("cmb_103");
            inPara.MaxOverlap = ui.GetDouble("cmb_104");

            inPara.NumMatches = ui.GetString("cmb_110") == "多个" ? 0 : ui.GetInt("cmb_110");
            inPara.MinScore = ui.GetDouble("cmb_111");
            inPara.NumLevels = ui.GetInt("cmb_112");

            //------------------------------------------
            inPara.DispText = ui.GetBool("ckb_disp0");
            inPara.DispRegion = ui.GetBool("ckb_disp1");
            inPara.DispContour = ui.GetBool("ckb_disp2");
            inPara.DispPoint = ui.GetBool("ckb_disp3");

            inPara.FontX = ui.GetInt("CB_FontX");
            inPara.FontY = ui.GetInt("CB_FontY");
            inPara.FontSize = ui.GetInt("CB_FontSize");
        }
        public void DrawROI(IRoiHost host, RectEnum type, bool newROI)
        {
            if (newROI)
            {
                inPara.HoRect.Type = type;
                host.DrawRegion(inPara.HoRect);
            } 
            else host.DrawRegionMod(inPara.HoRect);

            host.Display.Disp(inPara.HoRect, DrawStyle.Of(HColor.Blue));
            host.SetRectPara(inPara.HoRect);
        }
        public void DispROI(IRoiHost host)
        {
            host.SetModelPara(inPara.HoRect.HoRegion, inPara.HoContour, inPara.Coord);
        }
        public void SetTemplate(IRoiHost host, RectEnum type, bool newModel)
        {
            HObject imgReduced; HOperatorSet.GenEmptyObj(out imgReduced);
            HObject ho_Contour; HOperatorSet.GenEmptyObj(out ho_Contour);

            try
            {
                inPara.ModeRect.Type = type;
                if (newModel) host.DrawRegion(inPara.ModeRect);
                else host.DrawRegionMod(inPara.ModeRect);

                inPara.ModelPath = Path.Combine(AlgoPaths.JobDir, RunIndex.ToString(), "matching.bmp");
                var hImage = host.Display.HoImage;

                imgReduced.Dispose();
                HOperatorSet.ReduceDomain(hImage, inPara.ModeRect.HoRegion, out imgReduced);

                //制作模板
                HOperatorSet.CreateShapeModel(imgReduced, inPara.NumLevels, inPara.AngleStart.TupleRad(), inPara.AngleExtent.TupleRad(),
                                          "auto", "auto", "use_polarity", "auto", "auto", out HTuple modelID);

                // 立即转移所有权：先释放旧模板，再装入新模板，防止重复 SetTemplate 累计泄漏
                if (inPara.ModelID != null && inPara.ModelID.Length > 0)
                {
                    HOperatorSet.ClearShapeModel(inPara.ModelID);
                }
                inPara.ModelID = modelID;

                HOperatorSet.FindShapeModel(imgReduced, inPara.ModelID, inPara.AngleStart.TupleRad(), inPara.AngleExtent.TupleRad(),
                                            inPara.MinScore, 1, inPara.MaxOverlap, inPara.SubPixel, inPara.NumLevels, inPara.Greediness,
                                            out HTuple row, out HTuple column, out HTuple angle, out HTuple score);

                inPara.Results = new List<ModelResult>();
                var result = new ModelResult(row, column, angle, score);
                inPara.Results.Add(result);
                ho_Contour.Dispose();
                HOperatorSet.GetShapeModelContours(out ho_Contour, modelID, 1);
                HOperatorSet.VectorAngleToRigid(0, 0, 0, result.Row, result.Column, result.Angle, out HTuple hv_HomMat2D);
                HOperatorSet.AffineTransContourXld(ho_Contour, out HObject contoursAffineTrans, hv_HomMat2D);
                inPara.HoContour.Dispose();
                inPara.HoContour = contoursAffineTrans;
                inPara.Coord = result.Coord;

                HalconController.SaveSmallestRectImage(hImage, imgReduced, inPara.ModelPath);
                
                host.SetModelPara(inPara.HoRect.HoRegion, inPara.HoContour, inPara.Coord);
                host.Display.Disp(inPara.ModeRect, DrawStyle.Of(HColor.Orange));

                host.DrawDone(inPara.ModelPath, inPara.ModeRect.HoRegion, inPara.HoContour, result);
             
                if (score.Length > 0)
                {
                    host.Display.DispText("新建模板成功！", new Point2d(10, 10), DrawStyle.Of(HColor.Green));
                    inPara.TmplPoint = new Point2d(result.X, result.Y);      //更改跟随坐标
                }
                else
                {
                    host.Display.DispText("新建模板失败！", new Point2d(10, 10), DrawStyle.Of(HColor.Red));
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                imgReduced?.Dispose();
                ho_Contour?.Dispose();
            }
        }

    }

    public class ShapeModel : AlgoFont
    {
        public ShapeModel()
        {
            HOperatorSet.GenEmptyObj(out HoContour);
        }
        /// <summary> 图像来源 </summary>
        public string ImageIn { set; get; } = "默认";

        /// <summary> 区域来源 </summary>
        public string RegionIn { set; get; } = "默认";

        /// <summary> 跟随坐标 </summary>
        public string CoordIn { set; get; } = "默认";

        /// <summary> 坐标系 </summary>
        public CvCoord Coord { get; set; } = new CvCoord();

        /// <summary> 模版坐标 </summary>
        public Point2d TmplPoint { get; set; } = new Point2d();

        /// <summary> 区域 </summary>
        public CvRegion HoRect { set; get; } = new CvRegion();

        /// <summary> 模版区域 </summary>
        public CvRegion ModeRect { set; get; } = new CvRegion();

        /// <summary> 模版轮廓 </summary>
        /// <remarks>不加 = new HObject() 初始化器：句柄统一由构造函数的 GenEmptyObj 创建，否则初始化器创建的句柄会被覆盖且永不释放。</remarks>
        public HObject HoContour;

        /// <summary> 锁定中心 </summary>
        public bool LockCenter { get; set; } = true;

        /// <summary> 模版路径 </summary>
        public string ModelPath { get; set; }

        /// <summary> 模板ID </summary>
        public HTuple ModelID { get; set; }

        /// <summary> 起始角度 </summary>
        public HTuple AngleStart { get; set; } = -90;

        /// <summary> 增量角度 </summary>
        public HTuple AngleExtent { get; set; } = 180;

        /// <summary> 得分 </summary>
        public HTuple MinScore { get; set; } = 0.6;

        /// <summary> 匹配数量 </summary>
        public HTuple NumMatches { get; set; } = 1;

        /// <summary> 最大重叠率 </summary>
        public HTuple MaxOverlap { get; set; } = 0.5;

        /// <summary>
        /// 子像素： 如果不等于“none”，则为亚像素精度。默认值：“最小二乘” true
        /// </summary>
        public HTuple SubPixel { get; set; } = "least_squares";

        /// <summary> 金字塔层数 </summary>
        public HTuple NumLevels { get; set; } = 0;

        /// <summary>
        /// 贪婪系数 (形状匹配、缩放匹配)  
        /// 搜索启发式的“贪婪”（0：安全但缓慢；1：快速但匹配可能错过）。默认值：0.9
        /// </summary>
        public HTuple Greediness { get; set; } = 0.7;

        public List<ModelResult> Results { get; set; }

        /// <summary> 显示区域 </summary>
        public bool DispRegion { set; get; } = true;

        /// <summary> 显示轮廓 </summary>
        public bool DispContour { set; get; } = true;

        /// <summary> 显示点 </summary>
        public bool DispPoint { set; get; } = true;

    }

}
