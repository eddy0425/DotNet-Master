using DotNet.Drawing;
using DotNet.HalconUI;
using HalconDotNet;
using System;
using System.Windows.Forms;
using System.Collections.Generic;


namespace DotNet.HalconAlgo
{
    public class ShapeModelStrategy : ParaStrategyBase<ShapeModel>
    {
        public override AlgoEnum Algorithm => AlgoEnum.ShapeModel;
        public override string Name { get; set; } = "形状匹配";
        public override int RunIndex { get; set; }

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
        public override bool Fun_action(DisplayUI display, List<IParaStrategy> strategys)
        {
            HObject imgReduced = new HObject(); HOperatorSet.GenEmptyObj(out imgReduced);

            try
            {
                HObject ho_Image;
                if (inPara.ImageIn == "默认")
                    ho_Image = display.HoImage;
                else
                    ho_Image = strategys.ResolveFrom<HObject>(inPara.ImageIn);

                HObject ho_Rect;
                if (inPara.RegionIn == "默认")
                    ho_Rect = inPara.HoRect.HoRegion;
                else
                    ho_Rect = strategys.ResolveFrom<HObject>(inPara.RegionIn);

                display.DispRegion(ho_Rect, HColor.Blue);

                inPara.Results = new List<ModelResult>();

                for (int j = 0; j < ho_Rect.CountObj(); j++)
                {
                    HOperatorSet.GenEmptyObj(out imgReduced);
                    HOperatorSet.ReduceDomain(ho_Image, ho_Rect.SelectObj(j + 1), out imgReduced);

                    //查找模板
                    HOperatorSet.FindShapeModel(imgReduced, inPara.ModelID, inPara.AngleStart.TupleRad(), inPara.AngleExtent.TupleRad(),
                                 inPara.MinScore, inPara.NumMatches, inPara.MaxOverlap, inPara.SubPixel, inPara.NumLevels, inPara.Greediness,
                                 out HTuple row, out HTuple column, out HTuple angle, out HTuple score);

                    for (int i = 0; i < score.Length; i++)
                    {
                        var result = new ModelResult(row[i], column[i], angle[i], score[i]);
                        inPara.Results.Add(result);
                        inPara.Coord = new CvCoord(result.X, result.Y, result.Angle);

                        HTuple hv_HomMat2D = new HTuple();
                        HOperatorSet.GenEmptyObj(out inPara.HoContour);
                        HOperatorSet.GetShapeModelContours(out inPara.HoContour, inPara.ModelID, 1);
                        HOperatorSet.VectorAngleToRigid(0, 0, 0, result.Row, result.Column, result.Angle, out hv_HomMat2D);
                        HOperatorSet.AffineTransContourXld(inPara.HoContour, out inPara.HoContour, hv_HomMat2D);

                        display.DispRegion(inPara.HoContour, HColor.Green);
                        display.DispCross(result.Coord, HColor.Red);
                    }
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
                   );

            ClearResolvers();
            RegisterOutput("TmplPoint", () => inPara.TmplPoint);
            RegisterOutput("坐标系", () => inPara.Coord);
            RegisterOutput("坐标系/原点", () => inPara.Coord.Center);
            RegisterOutput("坐标系/原点/行", () => inPara.Coord.Y);
            RegisterOutput("坐标系/原点/列", () => inPara.Coord.X);
            RegisterOutput("坐标系/角度", () => inPara.Coord.Angle);
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

                HOperatorSet.ReduceDomain(hImage, inPara.ModeRect.HoRegion, out imgReduced);

                //制作模板
                HOperatorSet.CreateShapeModel(imgReduced, inPara.NumLevels, inPara.AngleStart.TupleRad(), inPara.AngleExtent.TupleRad(),
                                          "auto", "auto", "use_polarity", "auto", "auto", out HTuple modelID);
                inPara.ModelID = modelID;

                HOperatorSet.FindShapeModel(imgReduced, inPara.ModelID, inPara.AngleStart.TupleRad(), inPara.AngleExtent.TupleRad(),
                                            inPara.MinScore, 1, inPara.MaxOverlap, inPara.SubPixel, inPara.NumLevels, inPara.Greediness,
                                            out HTuple row, out HTuple column, out HTuple angle, out HTuple score);

                var result = new ModelResult(row, column, angle, score);
                HTuple hv_HomMat2D = new HTuple();
                HOperatorSet.GenEmptyObj(out ho_Contour);
                HOperatorSet.GetShapeModelContours(out ho_Contour, modelID, 1);
                HOperatorSet.VectorAngleToRigid(0, 0, 0, result.Row, result.Column, result.Angle, out hv_HomMat2D);
                HOperatorSet.AffineTransContourXld(ho_Contour, out ho_Contour, hv_HomMat2D);

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

    }

    public class ShapeModel
    {
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
        public HTuple SubPixel { get; set; } = "least_squares";

        /// <summary> 金字塔层数 </summary>
        public HTuple NumLevels { get; set; } = 0;

        /// <summary>
        /// 贪婪系数 (形状匹配、缩放匹配)  
        /// 搜索启发式的“贪婪”（0：安全但缓慢；1：快速但匹配可能错过）。默认值：0.9
        /// </summary>
        public HTuple Greediness { get; set; } = 0.7;

        public List<ModelResult> Results { get; set; }

    }

}
