using DotNet.Drawing;
using DotNet.HalconUI;
using HalconDotNet;
using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;


namespace DotNet.HalconAlgo
{
    public class GenericModelStrategy : ParaStrategyBase<GenericModel>
    {
        public override AlgoEnum Algorithm => AlgoEnum.GenericModel;
        public override string Name { get; set; } = "通用匹配";
        public override int RunIndex { get; set; }

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
                   );

            ClearResolvers();
            RegisterOutput("TmplPoint", () => inPara.TmplPoint);
            RegisterOutput("坐标系", () => inPara.Coord);
            RegisterOutput("坐标系/原点", () => inPara.Coord.Center);
            RegisterOutput("坐标系/原点/行", () => inPara.Coord.Y);
            RegisterOutput("坐标系/原点/列", () => inPara.Coord.X);
            RegisterOutput("坐标系/角度", () => inPara.Coord.Angle);
        }
        public override bool Fun_action(DisplayUI display, List<IParaStrategy> strategys)
        {
            if (inPara.ModelID == null || inPara.ModelID.Length == 0)
            {
                display.DispText("未建立模板，无法执行通用匹配！", 10, 10, HColor.Red);
                return false;
            }

            HObject imgReduced = new HObject(); HOperatorSet.GenEmptyObj(out imgReduced);
            HObject ho_SelRect = new HObject(); HOperatorSet.GenEmptyObj(out ho_SelRect);

            try
            {
                HObject ho_Image = (inPara.ImageIn == "默认")
                    ? display.HoImage
                    : strategys.ResolveFrom<HObject>(inPara.ImageIn);

                HObject ho_Rect = (inPara.RegionIn == "默认")
                    ? inPara.HoRect.HoRegion
                    : strategys.ResolveFrom<HObject>(inPara.RegionIn);

                if (inPara.DispRegion) display.DispRegion(ho_Rect, HColor.Blue);

                inPara.Results = new List<ModelResult>();

                for (int j = 0; j < ho_Rect.CountObj(); j++)
                {
                    // 释放上一次循环创建的对象，避免 HObject 句柄泄漏
                    imgReduced?.Dispose();
                    ho_SelRect?.Dispose();
                    HOperatorSet.SelectObj(ho_Rect, out ho_SelRect, j + 1);
                    HOperatorSet.ReduceDomain(ho_Image, ho_SelRect, out imgReduced);

                    #region 查找模板
                    // 注:CvHalconDotNet 22.11 未暴露 ClearGenericShapeModelResult,
                    // matchResultID 句柄由 HALCON 内部生命周期管理。
                    HOperatorSet.FindGenericShapeModel(imgReduced, inPara.ModelID, out HTuple matchResultID, out HTuple numMatchResult);

                    if (numMatchResult.I <= 0) continue;

                    HOperatorSet.GetGenericShapeModelResult(matchResultID, "all", "row", out HTuple row);
                    HOperatorSet.GetGenericShapeModelResult(matchResultID, "all", "column", out HTuple column);
                    HOperatorSet.GetGenericShapeModelResult(matchResultID, "all", "angle", out HTuple angle);
                    HOperatorSet.GetGenericShapeModelResult(matchResultID, "all", "score", out HTuple score);
                    #endregion

                    for (int i = 0; i < score.Length; i++)
                    {
                        var result = new ModelResult(row[i], column[i], angle[i], score[i]);
                        inPara.Results.Add(result);
                        inPara.Coord = new CvCoord(result.X, result.Y, result.Angle);

                        // 取该匹配实例对应的轮廓；先释放旧句柄，避免泄漏
                        inPara.HoContour?.Dispose();
                        HOperatorSet.GetGenericShapeModelResultObject(out inPara.HoContour, matchResultID, i, "contours");

                        if (inPara.DispContour) display.DispRegion(inPara.HoContour, HColor.Green);
                        if (inPara.DispPoint) display.DispCross(result.Coord, HColor.Red);
                    }
                }

                if (inPara.DispText)
                {
                    int cnt = inPara.Results.Count;
                    double bestScore = cnt > 0 ? inPara.Results[0].Score : 0;
                    string message = $"{Name} : 数量:{cnt} 最佳得分:{bestScore:F3} 缩放:[{inPara.ScaleMin},{inPara.ScaleMax}] 角度范围:[{inPara.AngleStart}°,{(inPara.AngleStart.D + inPara.AngleExtent.D)}°]";
                    display.DispText(message, inPara.FontX, inPara.FontY, inPara.FontSize, HColor.Green);
                }

                return true;
            }
            catch
            {
                throw;
            }
            finally
            {
                imgReduced?.Dispose();
                ho_SelRect?.Dispose();
            }
        }
        public override void DispPara(Form form, Dictionary<string, VsControlModel> VsControls)
        {
            form.ShowTabs(TabPageEnum.Parameter, TabPageEnum.Region, TabPageEnum.Matching, TabPageEnum.Display);

            CvRegion hRegion = inPara.HoRect;
            VsControls.ShowComboBox(form, "cmb_Width", hRegion.Width.ToString(), false);
            VsControls.ShowComboBox(form, "cmb_Height", hRegion.Height.ToString(), false);
            VsControls.ShowComboBox(form, "cmb_TopLeft", $"{hRegion.TopLeft.X};{hRegion.TopLeft.Y}", false);
            VsControls.ShowComboBox(form, "cmb_BottomRight", $"{hRegion.BottomRight.X};{hRegion.BottomRight.Y}", false);
            VsControls.ShowComboBox(form, "cmb_Center", $"{hRegion.Center.X};{hRegion.Center.Y}", false);

            VsControls.ShowComboBox(form, "cmb_CoordIn", inPara.CoordIn.ToString(), false);

            //基本参数
            VsControls.ShowLabel(form, "lbl_100", "图像来源");
            VsControls.ShowComboBox(form, "cmb_100", inPara.ImageIn.ToString(), false);
            VsControls.ShowButton(form, "btn_100", true);

            VsControls.ShowLabel(form, "lbl_101", "区域来源");
            VsControls.ShowComboBox(form, "cmb_101", inPara.RegionIn, false);
            VsControls.ShowButton(form, "btn_101", true);

            VsControls.ShowLabel(form, "lbl_102", "起始角度");
            VsControls.ShowComboBoxList(form, "cmb_102", inPara.AngleStart.ToString(), new[] { "-90", "-45" });
            VsControls.ShowButton(form, "btn_102", false);

            VsControls.ShowLabel(form, "lbl_103", "增量角度");
            VsControls.ShowComboBoxList(form, "cmb_103", inPara.AngleExtent.ToString(), new[] { "90", "180" });
            VsControls.ShowButton(form, "btn_103", false);

            VsControls.ShowLabel(form, "lbl_104", "最大重叠率");
            VsControls.ShowComboBoxList(form, "cmb_104", inPara.MaxOverlap.ToString(), new[] { "0", "0.3", "0.5" });
            VsControls.ShowButton(form, "btn_104", false);

            VsControls.ShowLabel(form, "lbl_110", "匹配数量");
            VsControls.ShowComboBoxList(form, "cmb_110", inPara.NumMatches == 0 ? "多个" : inPara.NumMatches.ToString(), new[] { "1", "2", "3", "多个" });
            VsControls.ShowButton(form, "btn_110", false);

            VsControls.ShowLabel(form, "lbl_111", "得分");
            VsControls.ShowComboBoxDropDown(form, "cmb_111", inPara.MinScore.ToString(), new[] { "0.5", "0.7" });

            VsControls.ShowLabel(form, "lbl_112", "金字塔");
            VsControls.ShowComboBoxList(form, "cmb_112", inPara.NumLevels.ToString(), new[] { "0", "2" });

            VsControls.ShowLabel(form, "lbl_113", "最小缩放");
            VsControls.ShowComboBoxDropDown(form, "cmb_113", inPara.ScaleMin.ToString(), new[] { "0.8", "0.7" });

            VsControls.ShowLabel(form, "lbl_114", "最大缩放");
            VsControls.ShowComboBoxDropDown(form, "cmb_114", inPara.ScaleMax.ToString(), new[] { "1.2", "1.5" });

            //------------------------------------------
            VsControls.ShowCheckBox(form, "ckb_disp0", "显示文本", inPara.DispText);
            VsControls.ShowCheckBox(form, "ckb_disp1", "查找区域", inPara.DispRegion);
            VsControls.ShowCheckBox(form, "ckb_disp2", "显示轮廓", inPara.DispContour);
            VsControls.ShowCheckBox(form, "ckb_disp3", "显示点", inPara.DispPoint);

            VsControls.ShowComboBoxDropDown(form, "CB_FontX", inPara.FontX.ToString(), new[] { "20", "50" });
            VsControls.ShowComboBoxDropDown(form, "CB_FontY", inPara.FontY.ToString(), new[] { "20", "50" });
            VsControls.ShowComboBoxDropDown(form, "CB_FontSize", inPara.FontSize.ToString(), new[] { "15", "30" });
        }
        public override void SavePara(Form form, Dictionary<string, VsControlModel> VsControls)
        {
            //基本参数
            inPara.CoordIn = VsControls["cmb_CoordIn"].Text;
            inPara.ImageIn = VsControls["cmb_100"].Text;
            inPara.RegionIn = VsControls["cmb_101"].Text;
            inPara.AngleStart = Convert.ToDouble(VsControls["cmb_102"].Text);
            inPara.AngleExtent = Convert.ToDouble(VsControls["cmb_103"].Text);
            inPara.MaxOverlap = Convert.ToDouble(VsControls["cmb_104"].Text);

            inPara.NumMatches = VsControls["cmb_110"].Text == "多个" ? 0 : Convert.ToInt32(VsControls["cmb_110"].Text);
            inPara.MinScore = Convert.ToDouble(VsControls["cmb_111"].Text);
            inPara.NumLevels = Convert.ToInt16(VsControls["cmb_112"].Text);
            inPara.ScaleMin = Convert.ToDouble(VsControls["cmb_113"].Text);
            inPara.ScaleMax = Convert.ToDouble(VsControls["cmb_114"].Text);

            // 仅当模板已建立时才更新模型参数；否则等下一次 SetTemplate 时统一应用
            if (inPara.ModelID != null && inPara.ModelID.Length > 0)
            {
                // 注意：iso_scale_*, num_levels 等属于"修改模型"参数，改动后必须重新训练。
                // 这里 SavePara 没有图像可重训，所以只更新"查找"类参数；
                // 如需修改 iso_scale / num_levels / optimization，请用户重新设置模板触发 SetTemplate。
                HTuple angleStartRad = ((HTuple)inPara.AngleStart.D).TupleRad();
                HTuple angleEndRad = ((HTuple)(inPara.AngleStart.D + inPara.AngleExtent.D)).TupleRad();

                HOperatorSet.SetGenericShapeModelParam(inPara.ModelID, "angle_start", angleStartRad);
                HOperatorSet.SetGenericShapeModelParam(inPara.ModelID, "angle_end", angleEndRad);
                HOperatorSet.SetGenericShapeModelParam(inPara.ModelID, "max_overlap", inPara.MaxOverlap);
                HOperatorSet.SetGenericShapeModelParam(inPara.ModelID, "num_matches", inPara.NumMatches);
                HOperatorSet.SetGenericShapeModelParam(inPara.ModelID, "min_score", inPara.MinScore);
                HOperatorSet.SetGenericShapeModelParam(inPara.ModelID, "greediness", inPara.Greediness);
                HOperatorSet.SetGenericShapeModelParam(inPara.ModelID, "subpixel", inPara.SubPixel);
            }

            //------------------------------------------
            inPara.DispText = VsControls["ckb_disp0"].Checked;
            inPara.DispRegion = VsControls["ckb_disp1"].Checked;
            inPara.DispContour = VsControls["ckb_disp2"].Checked;
            inPara.DispPoint = VsControls["ckb_disp3"].Checked;

            inPara.FontX = Convert.ToInt16(VsControls["CB_FontX"].Text);
            inPara.FontY = Convert.ToInt16(VsControls["CB_FontY"].Text);
            inPara.FontSize = Convert.ToInt16(VsControls["CB_FontSize"].Text);
        }
        public override void DrawROI(DisplayUI display, RectEnum type)
        {
            display.DrawRegion(type, out CvRegion hRegion);
            display.DispRegion(hRegion, HColor.Blue);
            inPara.HoRect.Dispose();
            inPara.HoRect = hRegion;
        }
        public override void DispROI(DisplayUI display)
        {
            display.SetDrawMode(Name, inPara.HoRect, DrawEnum.DispRect);
        }
        public override void SetTemplate(HDisplayForm display, RectEnum type)
        {
            HObject imgReduced = new HObject(); HOperatorSet.GenEmptyObj(out imgReduced);
            HObject ho_Contour = new HObject(); HOperatorSet.GenEmptyObj(out ho_Contour);

            try
            {
                display.DrawRegion(type, out CvRegion hRegion);
                inPara.ModeRect.Dispose();
                inPara.ModeRect = hRegion;

                inPara.ModelPath = Path.Combine(AlgoPaths.JobDir, RunIndex.ToString(), "matching.bmp");
                var hImage = display.HoImage;

                imgReduced.Dispose();
                HOperatorSet.ReduceDomain(hImage, inPara.ModeRect.HoRegion, out imgReduced);

                // 注:CvHalconDotNet 22.11 未暴露 ClearGenericShapeModel,
                // 旧模板句柄由 HALCON 内部生命周期管理；这里仅置空,让新 modelID 接管。
                inPara.ModelID = null;

                #region 1) 创建模板
                HOperatorSet.CreateGenericShapeModel(out HTuple modelID);
                #endregion

                #region 2) 训练前必须设置的“修改模型”类参数 (改动这些参数需要重训)
                HOperatorSet.SetGenericShapeModelParam(modelID, "num_levels", inPara.NumLevels);
                HOperatorSet.SetGenericShapeModelParam(modelID, "iso_scale_min", inPara.ScaleMin);
                HOperatorSet.SetGenericShapeModelParam(modelID, "iso_scale_max", inPara.ScaleMax);
                HOperatorSet.SetGenericShapeModelParam(modelID, "optimization", "auto");
                HOperatorSet.SetGenericShapeModelParam(modelID, "metric", "use_polarity");
                #endregion

                #region 3) 训练
                HOperatorSet.TrainGenericShapeModel(imgReduced, modelID);
                #endregion

                #region 4) 训练后设置的“查找”类参数 (角度需为弧度)
                HTuple angleStartRad = ((HTuple)inPara.AngleStart.D).TupleRad();
                HTuple angleEndRad = ((HTuple)(inPara.AngleStart.D + inPara.AngleExtent.D)).TupleRad();

                HOperatorSet.SetGenericShapeModelParam(modelID, "angle_start", angleStartRad);
                HOperatorSet.SetGenericShapeModelParam(modelID, "angle_end", angleEndRad);
                HOperatorSet.SetGenericShapeModelParam(modelID, "max_overlap", inPara.MaxOverlap);
                HOperatorSet.SetGenericShapeModelParam(modelID, "min_score", inPara.MinScore);
                HOperatorSet.SetGenericShapeModelParam(modelID, "greediness", inPara.Greediness);
                HOperatorSet.SetGenericShapeModelParam(modelID, "subpixel", inPara.SubPixel);

                // 试模板时只需要 1 个匹配
                HOperatorSet.SetGenericShapeModelParam(modelID, "num_matches", 1);
                #endregion

                inPara.ModelID = modelID;

                #region 5) 试匹配 (matchResultID 由 HALCON 内部生命周期管理)
                HOperatorSet.FindGenericShapeModel(hImage, inPara.ModelID, out HTuple matchResultID, out HTuple numMatchResult);
                #endregion

                // 试匹配完成后，恢复用户设定的匹配数量供 Fun_action 使用
                HOperatorSet.SetGenericShapeModelParam(inPara.ModelID, "num_matches", inPara.NumMatches);

                HalconHelper.SaveSmallestRectImage(hImage, imgReduced, inPara.ModelPath);

                display.ReDispImage();
                display.DispRegion(inPara.ModeRect, HColor.Red);

                if (numMatchResult.I > 0)
                {
                    HOperatorSet.GetGenericShapeModelResult(matchResultID, 0, "row", out HTuple row);
                    HOperatorSet.GetGenericShapeModelResult(matchResultID, 0, "column", out HTuple column);
                    HOperatorSet.GetGenericShapeModelResult(matchResultID, 0, "angle", out HTuple angle);
                    HOperatorSet.GetGenericShapeModelResult(matchResultID, 0, "score", out HTuple score);

                    inPara.Results = new List<ModelResult>();
                    var result = new ModelResult(row, column, angle, score);
                    result.ResultID = matchResultID;
                    inPara.Results.Add(result);

                    ho_Contour?.Dispose();
                    HOperatorSet.GetGenericShapeModelResultObject(out ho_Contour, matchResultID, 0, "contours");

                    display.DispRegion(ho_Contour, HColor.Green);
                    display.DispCross(result.Column, result.Row, result.Angle.ToDegrees(), HColor.Red, 50);
                    display.DispText("新建模板成功！", 10, 10, HColor.Green);

                    inPara.TmplPoint = new Point2d(result.X, result.Y);      //更改跟随坐标
                }
                else
                {
                    display.DispText("新建模板失败！", 10, 10, HColor.Red);
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
                // 注:CvHalconDotNet 22.11 未暴露 ClearGenericShapeModelResult,
                // 匹配结果句柄由 HALCON 内部生命周期管理。
            }
        }
        public override void Init(DisplayUI display)
        {
            display.DispModelEvent += DispModelEvent;
        }
        public override void Close(DisplayUI display)
        {
            display.DispModelEvent -= DispModelEvent;

            inPara.HoContour?.Dispose();
            inPara.HoRect?.Dispose();
            inPara.ModeRect?.Dispose();
            // 注: CvHalconDotNet 22.11 未暴露 ClearGenericShapeModel,
            // 旧模板句柄由 HALCON 内部生命周期管理。
            inPara.ModelID = null;
        }

        private void DispModelEvent(object sender, DrawDispModelArgs e)
        {
            if (e.Name == Name)
            {
                e.Display.DispRegion(inPara.HoRect, HColor.Blue);
                e.Display.DispRegion(inPara.HoContour, HColor.Green);
                e.Display.DispCross(inPara.Coord, HColor.OrangeRed);
            }
        }
    }

    public class GenericModel : AlgoFont
    {
        public GenericModel() 
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
        public HObject HoContour = new HObject();

        /// <summary> 锁定中心 </summary>
        public bool LockCenter { get; set; } = true;

        /// <summary> 模版路径 </summary>
        public string ModelPath { get; set; }

        /// <summary> 模板ID </summary>
        public HTuple ModelID { get; set; }

        /// <summary> 起始角度 </summary>
        public HTuple AngleStart { get; set; } = -90;

        /// <summary> 终点角度 </summary>
        public HTuple AngleEnd { get; set; } = 180;

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

        /// <summary> 最小缩放 (缩放匹配) </summary>
        public HTuple ScaleMin { get; set; } = 0.8;

        /// <summary> 最大缩放 (缩放匹配) </summary>
        public HTuple ScaleMax { get; set; } = 1.2;

        public List<ModelResult> Results { get; set; }

        /// <summary> 显示区域 </summary>
        public bool DispRegion { set; get; } = true;

        /// <summary> 显示轮廓 </summary>
        public bool DispContour { set; get; } = true;

        /// <summary> 显示点 </summary>
        public bool DispPoint { set; get; } = true;

    }
}
