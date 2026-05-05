using DotNet.Drawing;
using DotNet.HalconUI;
using HalconDotNet;
using System;
using System.Windows.Forms;
using System.Collections.Generic;


namespace DotNet.HalconAlgo
{
    public class NccModelStrategy : ParaStrategyBase<NccModel>
    {
        public override AlgoEnum Algorithm => AlgoEnum.NccModel;
        public override string Name { get; set; } = "灰度匹配";
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
                    ho_SelRect.Dispose();
                    HOperatorSet.SelectObj(ho_Rect, out ho_SelRect, j + 1);

                    imgReduced.Dispose();
                    HOperatorSet.ReduceDomain(ho_Image, ho_SelRect, out imgReduced);

                    //查找模板
                    HOperatorSet.FindNccModel(imgReduced, inPara.ModelID, inPara.AngleStart.TupleRad(), inPara.AngleExtent.TupleRad(),
                                 inPara.MinScore, inPara.NumMatches, inPara.MaxOverlap, inPara.SubPixel, inPara.NumLevels,
                                 out HTuple row, out HTuple column, out HTuple angle, out HTuple score);

                    for (int i = 0; i < score.Length; i++)
                    {
                        var result = new ModelResult(row[i], column[i], angle[i], score[i]);
                        inPara.Results.Add(result);
                        inPara.Coord = new CvCoord(result.X, result.Y, result.Angle);

                        inPara.HoContour.Dispose();
                        HOperatorSet.GetNccModelRegion(out inPara.HoContour, inPara.ModelID);
                        HOperatorSet.VectorAngleToRigid(0, 0, 0, result.Row, result.Column, result.Angle, out HTuple hv_HomMat2D);
                        HOperatorSet.AffineTransRegion(inPara.HoContour, out HObject regionAffineTrans, hv_HomMat2D, "nearest_neighbor");
                        inPara.HoContour.Dispose();
                        inPara.HoContour = regionAffineTrans;

                        if (inPara.DispContour) display.DispRegion(inPara.HoContour, HColor.Green);
                        if (inPara.DispPoint) display.DispCross(result.Coord, HColor.Red);
                    }
                }

                if (inPara.DispText)
                {
                    int cnt = inPara.Results.Count;
                    double bestScore = cnt > 0 ? inPara.Results[0].Score : 0;
                    string message = $"{Name} : 数量:{cnt} 最佳得分:{bestScore:F3} 角度范围:[{inPara.AngleStart}°,{(inPara.AngleStart.D + inPara.AngleExtent.D)}°]";
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
                imgReduced.Dispose();
                ho_SelRect.Dispose();
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

            //------------------------------------------
            inPara.DispText = VsControls["ckb_disp0"].Checked;
            inPara.DispRegion = VsControls["ckb_disp1"].Checked;
            inPara.DispContour = VsControls["ckb_disp2"].Checked;
            inPara.DispPoint = VsControls["ckb_disp3"].Checked;

            inPara.FontX = Convert.ToInt16(VsControls["CB_FontX"].Text);
            inPara.FontY = Convert.ToInt16(VsControls["CB_FontY"].Text);
            inPara.FontSize = Convert.ToInt16(VsControls["CB_FontSize"].Text);
        }
        public override void DispROI(DisplayUI display)
        {
            display.SetDrawMode(Name, inPara.HoRect, DrawEnum.DispRect);
        }
        private void SetTemplate(DisplayForm display)  //模板设置
        {
            HObject imgReduced = new HObject(); HOperatorSet.GenEmptyObj(out imgReduced);
            HObject ho_Contour = new HObject(); HOperatorSet.GenEmptyObj(out ho_Contour);

            try
            {
                var savelPath = AlgoPaths.JobDir + RunIndex + "\\matching.bmp";
                var hImage = display.HoImage;

                imgReduced.Dispose();
                HOperatorSet.ReduceDomain(hImage, inPara.ModeRect.HoRegion, out imgReduced);

                //制作模板
                HOperatorSet.CreateNccModel(imgReduced, inPara.NumLevels, inPara.AngleStart.TupleRad(), inPara.AngleExtent.TupleRad(),
                                 "auto", "use_polarity", out HTuple modelID);

                // 立即转移所有权：先释放旧模板，再装入新模板
                if (inPara.ModelID != null && inPara.ModelID.Length > 0)
                {
                    HOperatorSet.ClearNccModel(inPara.ModelID);
                }
                inPara.ModelID = modelID;

                HOperatorSet.FindNccModel(imgReduced, inPara.ModelID, inPara.AngleStart.TupleRad(), inPara.AngleExtent.TupleRad(),
                                            inPara.MinScore, 1, inPara.MaxOverlap, inPara.SubPixel, inPara.NumLevels,
                                            out HTuple row, out HTuple column, out HTuple angle, out HTuple score);

                var result = new ModelResult(row, column, angle, score);
                ho_Contour.Dispose();
                HOperatorSet.GetNccModelRegion(out ho_Contour, modelID);
                HOperatorSet.VectorAngleToRigid(0, 0, 0, result.Row, result.Column, result.Angle, out HTuple hv_HomMat2D);
                HOperatorSet.AffineTransRegion(ho_Contour, out HObject regionAffineTrans, hv_HomMat2D, "nearest_neighbor");
                ho_Contour.Dispose();
                ho_Contour = regionAffineTrans;

                HalconHelper.SaveSmallestRectImage(hImage, imgReduced, savelPath);

                display.ReDispImage();
                display.DispRegion(inPara.ModeRect, HColor.Red);
                display.DispRegion(ho_Contour, HColor.Green);
                display.DispCross(result.Column, result.Row, result.Angle.ToDegrees(), HColor.Red, 50);

                //m_dispModel.ShowModel(imgReduced, modeRect.HoRect, contourModel, result, type);

                if (score.Length > 0)
                {
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
            }
        }
        public override void Init(DisplayUI display)
        {
            display.RectangleEvent += RectEvent;
            display.SetModelEvent += SetModelEvent;
            display.DispModelEvent += DispModelEvent;
        }
        public override void Close(DisplayUI display)
        {
            display.RectangleEvent -= RectEvent;
            display.SetModelEvent -= SetModelEvent;
            display.DispModelEvent -= DispModelEvent;

            inPara.HoContour?.Dispose();
            inPara.HoRect?.Dispose();
            inPara.ModeRect?.Dispose();
            if (inPara.ModelID != null && inPara.ModelID.Length > 0)
            {
                HOperatorSet.ClearNccModel(inPara.ModelID);
                inPara.ModelID = null;
            }
        }
        private void RectEvent(object sender, DrawRectangleArgs e)
        {
            if (e.Name == Name)
            {
                inPara.HoRect.Update2Point(e.TopLeft, e.BottomRight);
                inPara.HoRect.GenRegion();
            }
        }
        private void SetModelEvent(object sender, DrawSetModelArgs e)
        {
            if (e.Name == Name)
            {
                inPara.ModeRect.Update2Point(e.TopLeft, e.BottomRight);
                inPara.ModeRect.GenRegion();

                SetTemplate(e.Display);
            }
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

    public class NccModel : AlgoFont
    {
        public NccModel()
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
        public HTuple SubPixel { get; set; } = "true";

        /// <summary> 金字塔层数 </summary>
        public HTuple NumLevels { get; set; } = 0;

        public List<ModelResult> Results { get; set; }

        /// <summary> 显示区域 </summary>
        public bool DispRegion { set; get; } = true;
     
        /// <summary> 显示轮廓 </summary>
        public bool DispContour { set; get; } = true;

        /// <summary> 显示点 </summary>
        public bool DispPoint { set; get; } = true;

    }
}
