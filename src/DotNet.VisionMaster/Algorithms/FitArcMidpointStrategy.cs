using DotNet.Drawing;
using HalconDotNet;
using System;
using System.Collections.Generic;


namespace DotNet.VisionMaster
{
    public class FitArcMidpointStrategy : ParaStrategyBase<FitArcMidpoint>
    {
        public override string Name => "圆弧中点";
        public override void Init(DisplayUI display)
        {
            display.RectangleEvent += RectEvent;
        }
        public override void Close(DisplayUI display)
        {
            display.RectangleEvent -= RectEvent;
        }
        private void RectEvent(object sender, DrawRectangleArgs e)
        {
            if (e.Name == Name)
            {
                inPara.HoRect.Update2Point(e.TopLeft, e.BottomRight);
                inPara.HoRect.GenRegion();
            }
        }
        public override bool Fun_action(DisplayUI display, List<IParaStrategy> strategys)
        {
            HObject imgReduced = new HObject(); HOperatorSet.GenEmptyObj(out imgReduced);
            HObject contourFitting = new HObject(); HOperatorSet.GenEmptyObj(out contourFitting);

            try
            {
                HObject ho_Image;
                if (inPara.ImageIn == "默认")
                    ho_Image = display.HoImage;
                else
                    ho_Image = strategys.ResolveFrom<HObject>(inPara.ImageIn);

                HObject ho_Rect;
                if (inPara.ImageIn == "默认")
                    ho_Rect = inPara.HoRect.HoRegion;
                else
                    ho_Rect = strategys.ResolveFrom<HObject>(inPara.RegionIn);

                HOperatorSet.ReduceDomain(ho_Image, ho_Rect, out imgReduced);

                #region 角度&变量
                HTuple baseAgl = 0;
                HTuple startAgl = baseAgl + inPara.StartAngle.ToRadians();
                HTuple deltaAgl = baseAgl + inPara.DeltaAngle.ToRadians();
                HTuple fixAgl = baseAgl + inPara.HoRect.Phi;

                HTuple fixRow = inPara.HoRect.Center.Y;
                HTuple fixCol = inPara.HoRect.Center.X;
                HTuple fixLen1 = inPara.HoRect.Width / 2;
                HTuple fixLen2 = inPara.HoRect.Height / 2;

                HTuple imgWid = display.HoWidth;
                HTuple imgHei = display.HoHeight;
                #endregion

                #region 直边轮廓查找
                double stepPace = Convert.ToDouble(inPara.StepPace); if (stepPace < 1) stepPace = 1;
                double stepWid = Convert.ToDouble(inPara.StepWidth) / 2; if (stepWid < 1) stepWid = 1;
                string transition = inPara.GetTransition;
                string select = inPara.GetContourType;
                //double stepPace = Convert.ToDouble(inPara.StepPace); if (stepPace < 1) stepPace = 1;
                //double stepWid = Convert.ToDouble(inPara.StepWidth) / 2; if (stepWid < 1) stepWid = 1;
                //
                HTuple mRow = new HTuple(), mCol = new HTuple(), mAmp = new HTuple(), mDis = new HTuple();
                HTuple rowNew = new HTuple(), colNew = new HTuple();
                HTuple hMHandle;
                double[] rowFitting = new double[0];
                double[] colFitting = new double[0];
                int loop_cnt = (int)(fixLen2.D / stepPace + 0.5); if (loop_cnt < 1) loop_cnt = 1;
                double cosLen2 = fixLen2 * Math.Cos(fixAgl) / ((double)(loop_cnt));
                double sinLen2 = fixLen2 * Math.Sin(fixAgl) / ((double)(loop_cnt));

                for (float s = -1 * loop_cnt; s <= loop_cnt; s++)
                {
                    rowNew = fixRow + s * cosLen2;
                    colNew = fixCol + s * sinLen2;
                    HOperatorSet.GenMeasureRectangle2(rowNew, colNew, fixAgl, fixLen1, stepWid, imgWid, imgHei, "nearest_neighbor", out hMHandle);
                    if (select != "second")
                    {
                        HOperatorSet.MeasurePos(imgReduced, hMHandle, inPara.Sigma, inPara.Threshold, transition, select, out mRow, out mCol, out mAmp, out mDis);
                    }
                    else
                    {
                        HOperatorSet.MeasurePos(imgReduced, hMHandle, inPara.Sigma, inPara.Threshold, transition, "all", out mRow, out mCol, out mAmp, out mDis);
                    }
                    if (inPara.DispRegion)
                    {
                        display.DispRectangle2(rowNew, colNew, fixAgl, fixLen1, stepWid, HColor.Blue);
                    }
                    if (mRow.Length > 0 && select != "second")
                    {
                        Array.Resize(ref rowFitting, rowFitting.Length + 1); rowFitting[rowFitting.Length - 1] = mRow.TupleSelect(0).D;
                        Array.Resize(ref colFitting, colFitting.Length + 1); colFitting[colFitting.Length - 1] = mCol.TupleSelect(0).D;

                        if (inPara.DispFittingPoint)
                        {
                            display.DispPoint(mCol.TupleSelect(0), mRow.TupleSelect(0), HColor.Red, inPara.PointSize);
                        }
                    }
                    else if (mRow.Length > 1 && select == "second")
                    {
                        Array.Resize(ref rowFitting, rowFitting.Length + 1); rowFitting[rowFitting.Length - 1] = mRow.TupleSelect(1).D;
                        Array.Resize(ref colFitting, colFitting.Length + 1); colFitting[colFitting.Length - 1] = mCol.TupleSelect(1).D;
                        if (inPara.DispFittingPoint)
                        {
                            display.DispPoint(mCol.TupleSelect(1), mRow.TupleSelect(1), HColor.Red, inPara.PointSize);
                        }
                    }
                    HOperatorSet.CloseMeasure(hMHandle);
                }

                //拟合直线轮廓
                if (rowFitting.Length > 1)
                {
                    HOperatorSet.GenContourPolygonXld(out contourFitting, rowFitting, colFitting);

                    if (inPara.DispRegion) display.DispRegion(ho_Rect, HColor.Blue);
                    if (inPara.DispResult) display.DispRegion(contourFitting, HColor.Red);

                     var contours = new Point2d[rowFitting.Length];
                    for (int i = 0; i < rowFitting.Length; i++)
                    {
                        contours[i] = new Point2d(colFitting[i], rowFitting[i]);
                    }
                }
                else
                {
                    throw new NullReferenceException("未找到轮廓！");
                }

                #endregion

                return true;
            }
            catch
            {
                throw;
            }
            finally
            {
                imgReduced.Dispose();
                contourFitting.Dispose();
            }
        }
        public override void GenTreeNode(TreeVisualizer tree)
        {
            tree.Branch(Name, branch => branch
                       .Node("直线", OutEnum.Line, line => line
                           .Branch("起点", pt => pt
                               .Node("行", OutEnum.Number)
                               .Node("列", OutEnum.Number)
                           )
                           .Branch("终点", pt => pt
                               .Node("行", OutEnum.Number)
                               .Node("列", OutEnum.Number)
                           )
                       )
                       .CommonNodes()
                   );

            ClearResolvers();
            RegisterOutput("直线", () => inPara.Line);
            RegisterOutput("直线/起点", () => inPara.Line.Start);
            RegisterOutput("直线/起点/行", () => inPara.Line.Start.Y);
            RegisterOutput("直线/起点/列", () => inPara.Line.Start.X);
            RegisterOutput("直线/终点", () => inPara.Line.End);
            RegisterOutput("直线/终点/行", () => inPara.Line.End.Y);
            RegisterOutput("直线/终点/列", () => inPara.Line.End.X);

        }
        public override void DispPara(ParaForm form, Dictionary<string, VsControlModel> VsControls)
        {
            form.ShowTabs(TabPageEnum.Parameter, TabPageEnum.Region, TabPageEnum.Display);

            VsControls.ShowComboBox(form, "cmb_CoordIn", inPara.CoordIn.ToString(), false);

            CvRegion hRegion = inPara.HoRect;
            VsControls.ShowComboBox(form, "cmb_Width", hRegion.Width.ToString(), false);
            VsControls.ShowComboBox(form, "cmb_Height", hRegion.Height.ToString(), false);
            VsControls.ShowComboBox(form, "cmb_TopLeft", $"{hRegion.TopLeft.X};{hRegion.TopLeft.Y}", false);
            VsControls.ShowComboBox(form, "cmb_BottomRight", $"{hRegion.BottomRight.X};{hRegion.BottomRight.Y}", false);
            VsControls.ShowComboBox(form, "cmb_Center", $"{hRegion.Center.X};{hRegion.Center.Y}", false);

            VsControls.ShowLabel(form, "lbl_100", "图像来源");
            VsControls.ShowComboBox(form, "cmb_100", inPara.ImageIn, false);
            VsControls.ShowButton(form, "btn_100", true);

            VsControls.ShowLabel(form, "lbl_101", "区域来源");
            VsControls.ShowComboBox(form, "cmb_101", inPara.RegionIn, false);
            VsControls.ShowButton(form, "btn_101", true);

            VsControls.ShowLabel(form, "lbl_102", "过渡方向");
            VsControls.ShowComboBoxList(form, "cmb_102", inPara.Transition, new[] { "由黑到白", "由白到黑", "全部" });
            VsControls.ShowButton(form, "btn_102", false);

            VsControls.ShowLabel(form, "lbl_103", "选择");
            VsControls.ShowComboBoxList(form, "cmb_103", inPara.ContourType, new[] { "第一条边", "第二条边", "最后一条", "全部" });
            VsControls.ShowButton(form, "btn_103", false);

            VsControls.ShowLabel(form, "lbl_104", "滤波");
            VsControls.ShowComboBoxDropDown(form, "cmb_104", inPara.Sigma.ToString(), new[] { "0", "1" });
            VsControls.ShowButton(form, "btn_104", false);

            VsControls.ShowLabel(form, "lbl_105", "阈值");
            VsControls.ShowComboBoxDropDown(form, "cmb_105", inPara.Threshold.ToString(), new[] { "30", "50" });
            VsControls.ShowButton(form, "btn_105", false);

            VsControls.ShowLabel(form, "lbl_110", "步距");
            VsControls.ShowComboBoxDropDown(form, "cmb_110", inPara.StepPace.ToString(), new[] { "2", "5", "10" });

            VsControls.ShowLabel(form, "lbl_111", "步宽");
            VsControls.ShowComboBoxDropDown(form, "cmb_111", inPara.StepWidth.ToString(), new[] { "2", "5", "10" });

            VsControls.ShowLabel(form, "lbl_112", "最大偏差");
            VsControls.ShowComboBoxDropDown(form, "cmb_112", inPara.MaxErr.ToString(), new[] { "1", "3", "5", "10" });
        }
        public override void SavePara(ParaForm form, Dictionary<string, VsControlModel> VsControls)
        {
            inPara.CoordIn = VsControls["cmb_CoordIn"].Text;
            inPara.ImageIn = VsControls["cmb_100"].Text;
            inPara.RegionIn = VsControls["cmb_101"].Text;
            inPara.Transition = VsControls["cmb_102"].Text;
            inPara.ContourType = VsControls["cmb_103"].Text;
            inPara.Sigma = Convert.ToInt16(VsControls["cmb_104"].Text);
            inPara.Threshold = Convert.ToInt16(VsControls["cmb_105"].Text);

            inPara.StepPace = Convert.ToInt16(VsControls["cmb_110"].Text);
            inPara.StepWidth = Convert.ToInt16(VsControls["cmb_111"].Text);
            inPara.MaxErr = Convert.ToInt16(VsControls["cmb_112"].Text);
        }
        public override void DispROI(DisplayUI display)
        {
            display.SetDrawMode(Name, inPara.HoRect, DrawEnum.DispRect);
        }

    }

    public class FitArcMidpoint
    {
        /// <summary> 指令类型 </summary>
        public readonly string Algorithm = "圆弧中点";

        /// <summary> 图像来源 </summary>
        public string ImageIn { set; get; } = "默认";

        /// <summary> 区域来源 </summary>
        public string RegionIn { set; get; } = "默认";

        /// <summary> 跟随坐标 </summary>
        public string CoordIn { set; get; } = "默认";

        public CvLine Line { set; get; }

        /// <summary> 区域 </summary>
        public CvRegion HoRect { set; get; } = new CvRegion();

        /// <summary> 过渡方向 </summary>
        public string Transition { set; get; } = "由黑到白";

        /// <summary>
        /// 获取过渡方向
        /// </summary>
        internal string GetTransition
        {
            get
            {
                if (Transition == "由黑到白") return "positive";
                else if (Transition == "由白到黑") return "negative";
                else if (Transition == "全部") return "all";
                return "";
            }
        }

        /// <summary> 选择 </summary>
        public string ContourType { set; get; } = "第一条边";

        /// <summary>
        /// 获取选择
        /// </summary>
        internal string GetContourType
        {
            get
            {
                if (ContourType == "第一条边") return "first";
                else if (ContourType == "第二条边") return "second";
                else if (ContourType == "最后一条") return "last";
                else if (ContourType == "全部") return "all";
                return "";
            }
        }

        /// <summary> 滤波 </summary>
        public int Sigma { set; get; } = 1;

        /// <summary>
        /// 阈值 val = 0: 自动阈值, val > 0: 手动阈值, val = -1: 能量最强, val 小于 -1: 百分比阈值
        /// </summary>
        public int Threshold { set; get; } = 80;

        /// <summary> 步距 </summary>
        public int StepPace { set; get; } = 5;

        /// <summary> 步宽 </summary>
        public int StepWidth { set; get; } = 5;

        /// <summary> 最大偏差 </summary>
        public int MaxErr { set; get; } = 5;

        /// <summary> 开始角度 </summary>
        public double StartAngle { set; get; } = 0;

        /// <summary> 增量角度 </summary>
        public double DeltaAngle { set; get; } = 360;

        /// <summary> 点大小 </summary>
        public int PointSize { set; get; } = 15;

        /// <summary> 显示区域 </summary>
        public bool DispRegion { set; get; } = false;

        /// <summary> 显示结果 </summary>
        public bool DispResult { set; get; } = true;

        /// <summary> 显示文本 </summary>
        public bool DispText { set; get; } = false;

        /// <summary> 拟合点 </summary>
        public bool DispFittingPoint { set; get; } = false;

    }
}
