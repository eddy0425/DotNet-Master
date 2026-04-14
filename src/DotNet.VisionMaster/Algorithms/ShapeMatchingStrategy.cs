using DotNet.Drawing;
using DotNet.HalconUI;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Windows.Forms;


namespace DotNet.VisionMaster
{
    public class ShapeMatchingStrategy : ParaStrategyBase<ShapeMatching>
    {
        public int RunIndex { get; set; }
        public override string Name => "形状匹配";
        public override void Init(DrawContext draw)
        {
            draw.RectangleEvent += RectEvent;
        }
        public override void Close(DrawContext draw)
        {
            draw.RectangleEvent -= RectEvent;
        }
        private void RectEvent(object sender, DrawRectangleArgs e)
        {
            if (e.Name == Name)
            {
                inPara.HContext.Update2Point(e.TopLeft, e.BottomRight);
                inPara.HContext.GenRegion();
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

                display.ReDispImage();
                display.DispRegion(inPara.HContext, HColor.Blue);
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
                       .Node("坐标系", OutEnum.Coord, line => line
                           .Branch("原点", pt => pt
                               .Node("行", OutEnum.Number)
                               .Node("列", OutEnum.Number)
                           )
                           .Node("角度", OutEnum.Array)
                       )
                   );

            ClearResolvers();
            RegisterOutput("坐标系", () => inPara.Coord);
            RegisterOutput("坐标系/原点", () => inPara.Coord.Center);
            RegisterOutput("坐标系/原点/行", () => inPara.Coord.Y);
            RegisterOutput("坐标系/原点/列", () => inPara.Coord.X);
            RegisterOutput("坐标系/角度", () => inPara.Coord.Angle);

        }
        public override void DispPara(ParaForm form, Dictionary<string, VsControlModel> VsControls)
        {
            form.ShowTabs(TabPageEnum.Parameter, TabPageEnum.Region, TabPageEnum.Matching, TabPageEnum.Display);

            CvRegion hRegion = inPara.HContext;
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
        public override void SavePara(ParaForm form, Dictionary<string, VsControlModel> VsControls)
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
            display.SetDrawMode(Name, inPara.HContext, WinDrawType.DispRect);
        }

        public void SetTemplate(DisplayForm display, List<IParaStrategy> strategys, CvRegion modeRect)  //模板设置
        {
            HObject imgReduced = new HObject(); HOperatorSet.GenEmptyObj(out imgReduced);
            HObject contourModel = new HObject(); HOperatorSet.GenEmptyObj(out contourModel);
            HObject modelContours = new HObject(); HOperatorSet.GenEmptyObj(out modelContours);

            try
            {
                var savelPath = FilePaths.JobDir + RunIndex + "\\matching.bmp";
                var hImage = strategys.ResolveFrom<HObject>(inPara.ImageIn);
       
                HOperatorSet.ReduceDomain(hImage, modeRect.HoRegion, out imgReduced);

                //制作模板
                HOperatorSet.CreateShapeModel(imgReduced, inPara.NumLevels, inPara.AngleStart.TupleRad(), inPara.AngleExtent.TupleRad(),
                                          "auto", "auto", "use_polarity", "auto", "auto", out HTuple modelID);

                HOperatorSet.FindShapeModel(imgReduced, modelID, inPara.AngleStart.TupleRad(), inPara.AngleExtent.TupleRad(),
                                            inPara.MinScore, inPara.NumMatches, inPara.MaxOverlap, inPara.SubPixel, inPara.NumLevels, inPara.Greediness,
                                            out HTuple row, out HTuple column, out HTuple angle, out HTuple score);

                inPara.ModelID = modelID;

                var result = new ModelResult(row, column, angle, score);

                if (result.Score.Length > 0)
                {
                    var level = 1;
                    HTuple hv_HomMat2D = new HTuple();
                    HOperatorSet.GetShapeModelContours(out modelContours, modelID, level);
                    HOperatorSet.VectorAngleToRigid(0, 0, 0, result.Row, result.Column, result.Angle, out hv_HomMat2D);
                    HOperatorSet.AffineTransContourXld(modelContours, out modelContours, hv_HomMat2D);

                    HalconHelper.SaveSmallestRectImage(hImage, imgReduced, savelPath);

                    display.ReDispImage();
                    display.DispRegion(modeRect, HColor.Red);
                    display.DispRegion(contourModel, HColor.Green);
                    display.DispCross(result.Row.D, result.Column.D, result.Angle.D.ToDegrees(), HColor.Red, 50);

                    //m_dispModel.ShowModel(imgReduced, modeRect.HoRect, contourModel, result, type);

                    display.DispText("新建模板成功！", 10, 10, HColor.Green);
                }
                else
                {
                    display.DispText("新建模板失败！", 10, 10, HColor.Red);
                }

                //jobPara.SaveParameter();

                inPara.Follow = new Point2d(result.X, result.Y);      //更改跟随坐标
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                imgReduced?.Dispose();
                contourModel?.Dispose();
                modelContours?.Dispose();
            }
        }

    }

    public class ShapeMatching
    {   
        /// <summary> 指令类型 </summary>
        public readonly string Algorithm = "形状匹配";

        /// <summary> 图像来源 </summary>
        public string ImageIn { set; get; } = "默认";

        /// <summary> 区域来源 </summary>
        public string RegionIn { set; get; } = "默认";

        /// <summary> 跟随坐标 </summary>
        public string CoordIn { set; get; } = "默认";

        /// <summary> 坐标系 </summary>
        public CvCoord Coord { get; set; } = new CvCoord();

        /// <summary> 跟随点 </summary>
        public Point2d Follow { get; set; } = new Point2d();

        /// <summary> 区域 </summary>
        public CvRegion HContext { set; get; } = new CvRegion();

        /// <summary> 模版区域 </summary>
        public CvRegion ModeRect { set; get; } = new CvRegion();

        /// <summary> 锁定中心 </summary>
        public bool LockCenter { get; set; } = true;

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
    }

}
