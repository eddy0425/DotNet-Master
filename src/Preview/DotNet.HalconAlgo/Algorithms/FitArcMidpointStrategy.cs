using DotNet.Drawing;
using HalconDotNet;
using System;
using System.Windows.Forms;
using System.Collections.Generic;


namespace DotNet.HalconAlgo
{
    public class FitArcMidpointStrategy : ParaStrategyBase<FitArcMidpoint>
    {
        public override string Name => "圆弧中点";
        public override void Init(DisplayUI display)
        {
            display.AffRectEvent += AffRectEvent;
        }
        public override void Close(DisplayUI display)
        {
            display.AffRectEvent -= AffRectEvent;
        }
        private void AffRectEvent(object sender, DrawAffRectArgs e)
        {
            if (e.Name == Name)
            {
                inPara.HoRect.UpdateCenter(e.Center, e.RectSize);
                inPara.HoRect.Phi = e.Phi;
                inPara.HoRect.Type = RectEnum.Rectangle2;
                inPara.HoRect.GenRegion();
            }
        }

        public override bool Fun_action(DisplayUI display, List<IParaStrategy> strategys)
        {
            HObject imgReduced = new HObject(); HOperatorSet.GenEmptyObj(out imgReduced);
            HObject contourFitting = new HObject(); HOperatorSet.GenEmptyObj(out contourFitting);
            HObject arcContour = new HObject(); HOperatorSet.GenEmptyObj(out arcContour);

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

                HOperatorSet.ReduceDomain(ho_Image, ho_Rect, out imgReduced);
                display.DispRegion(ho_Rect, HColor.Blue);

                #region 变量
                HTuple fixAgl = inPara.HoRect.Phi;
                HTuple fixRow = inPara.HoRect.Center.Y;
                HTuple fixCol = inPara.HoRect.Center.X;
                HTuple fixLen1 = inPara.HoRect.Width / 2;
                HTuple fixLen2 = inPara.HoRect.Height / 2;
                HTuple imgWid = display.HoWidth;
                HTuple imgHei = display.HoHeight;
                #endregion

                #region 边缘查找
                double stepPace = Convert.ToDouble(inPara.StepPace); if (stepPace < 1) stepPace = 1;
                double stepWid = Convert.ToDouble(inPara.StepWidth) / 2; if (stepWid < 1) stepWid = 1;
                string transition = inPara.GetTransition;
                string select = inPara.GetContourType;

                // 预先确定 MeasurePos 的 select 参数与取点下标，避免在循环内反复判断
                string measureSelect = (select == "second") ? "all" : select;
                int pickIndex = (select == "second") ? 1 : 0;

                int loop_cnt = (int)(fixLen2.D / stepPace + 0.5); if (loop_cnt < 1) loop_cnt = 1;
                double cosLen2 = fixLen2 * Math.Cos(fixAgl) / loop_cnt;
                double sinLen2 = fixLen2 * Math.Sin(fixAgl) / loop_cnt;

                List<double> rowList = new List<double>(2 * loop_cnt + 1);
                List<double> colList = new List<double>(2 * loop_cnt + 1);

                for (int s = -loop_cnt; s <= loop_cnt; s++)
                {
                    HTuple rowNew = fixRow + s * cosLen2;
                    HTuple colNew = fixCol + s * sinLen2;

                    HTuple hMHandle;
                    HOperatorSet.GenMeasureRectangle2(rowNew, colNew, fixAgl, fixLen1, stepWid,
                        imgWid, imgHei, "nearest_neighbor", out hMHandle);
                    try
                    {
                        HTuple mRow, mCol, mAmp, mDis;
                        HOperatorSet.MeasurePos(imgReduced, hMHandle, inPara.Sigma, inPara.Threshold,
                            transition, measureSelect, out mRow, out mCol, out mAmp, out mDis);

                        if (inPara.DispRegion)
                        {
                            display.DispRectangle2(rowNew, colNew, fixAgl, fixLen1, stepWid, HColor.Blue);
                        }

                        if (mRow.Length > pickIndex)
                        {
                            rowList.Add(mRow.TupleSelect(pickIndex).D);
                            colList.Add(mCol.TupleSelect(pickIndex).D);
                        }
                    }
                    finally
                    {
                        HOperatorSet.CloseMeasure(hMHandle);
                    }
                }
                #endregion

                #region 拟合圆弧中点
                if (rowList.Count < 3)
                {
                    throw new InvalidOperationException("未找到足够的轮廓点！");
                }

                double maxErr = inPara.MaxErr; if (maxErr < 0) maxErr = 0;
                // 直线粗滤阈值适度放宽以容纳弧的凸量 (sagitta)
                double lineGate = Math.Max(maxErr * 3.0, 15.0);

                List<double> rowRemoved = new List<double>();
                List<double> colRemoved = new List<double>();

                #region Stage 1：gauss 鲁棒直线拟合剔除严重跑偏的点
                RebuildContour(ref contourFitting, rowList, colList);

                HTuple lineRowBegin, lineColBegin, lineRowEnd, lineColEnd, lineNr, lineNc, lineDist;
                HOperatorSet.FitLineContourXld(contourFitting, "gauss", -1, 0, 5, 1.345,
                    out lineRowBegin, out lineColBegin, out lineRowEnd, out lineColEnd,
                    out lineNr, out lineNc, out lineDist);

                // 使用 Hesse 形式在 C# 侧直接算点到直线距离，省去循环内的 Halcon 调用
                double nr = lineNr.D, nc = lineNc.D, nd = lineDist.D;
                for (int i = rowList.Count - 1; i >= 0; i--)
                {
                    double dist = Math.Abs(nr * rowList[i] + nc * colList[i] - nd);
                    if (dist > lineGate)
                    {
                        rowRemoved.Add(rowList[i]);
                        colRemoved.Add(colList[i]);
                        rowList.RemoveAt(i);
                        colList.RemoveAt(i);
                    }
                }

                if (rowList.Count < 3)
                {
                    throw new InvalidOperationException("直线粗滤后有效点不足，无法拟合圆弧！");
                }
                #endregion

                #region Stage 2：atukey 圆拟合 + 径向距离迭代精滤
                HTuple circRow, circCol, circRadius, circStartPhi, circEndPhi, circPointOrder;
                FitArcFromPoints(ref contourFitting, rowList, colList,
                    out circRow, out circCol, out circRadius,
                    out circStartPhi, out circEndPhi, out circPointOrder);

                int safety = rowList.Count;
                for (int iter = 0; iter < safety; iter++)
                {
                    double cr = circRow.D, cc = circCol.D, rad = circRadius.D;
                    int worstIdx = -1;
                    double worstErr = 0;
                    for (int i = 0; i < rowList.Count; i++)
                    {
                        double dRow = rowList[i] - cr;
                        double dCol = colList[i] - cc;
                        double err = Math.Abs(Math.Sqrt(dRow * dRow + dCol * dCol) - rad);
                        if (err > worstErr) { worstErr = err; worstIdx = i; }
                    }

                    if (worstIdx < 0 || worstErr <= maxErr) break;
                    if (rowList.Count <= 3) break;

                    rowRemoved.Add(rowList[worstIdx]);
                    colRemoved.Add(colList[worstIdx]);
                    rowList.RemoveAt(worstIdx);
                    colList.RemoveAt(worstIdx);

                    FitArcFromPoints(ref contourFitting, rowList, colList,
                        out circRow, out circCol, out circRadius,
                        out circStartPhi, out circEndPhi, out circPointOrder);
                }

                if (rowList.Count < 3)
                {
                    throw new InvalidOperationException("最大偏差筛选后有效点不足，无法拟合圆弧！");
                }
                #endregion

                #region Stage 3：可选裁剪筛选后首尾点并重新拟合
                if (inPara.IsTrimEnds && rowList.Count >= 5)
                {
                    int last = rowList.Count - 1;
                    rowRemoved.Add(rowList[0]);
                    colRemoved.Add(colList[0]);
                    rowRemoved.Add(rowList[last]);
                    colRemoved.Add(colList[last]);

                    rowList.RemoveAt(last);
                    colList.RemoveAt(last);
                    rowList.RemoveAt(0);
                    colList.RemoveAt(0);

                    FitArcFromPoints(ref contourFitting, rowList, colList,
                        out circRow, out circCol, out circRadius,
                        out circStartPhi, out circEndPhi, out circPointOrder);
                }
                #endregion

                if (inPara.DispFittingPoint)
                {
                    for (int i = 0; i < rowRemoved.Count; i++)
                    {
                        display.DispPoint(colRemoved[i], rowRemoved[i], HColor.Red, inPara.PointSize);
                    }
                    for (int i = 0; i < rowList.Count; i++)
                    {
                        display.DispPoint(colList[i], rowList[i], HColor.Green, inPara.PointSize);
                    }
                }

                double arcMidPhi = ComputeArcMidPhi(circStartPhi.D, circEndPhi.D, circPointOrder.S);
                double midRow = circRow.D - circRadius.D * Math.Sin(arcMidPhi);
                double midCol = circCol.D + circRadius.D * Math.Cos(arcMidPhi);
                inPara.ArcMidpoint = new Point2d(midCol, midRow);

                if (inPara.DispRegion) display.DispRegion(ho_Rect, HColor.Blue);
                if (inPara.DispResult)
                {
                    arcContour.Dispose(); HOperatorSet.GenEmptyObj(out arcContour);
                    HOperatorSet.GenCircleContourXld(out arcContour,
                        circRow, circCol, circRadius, circStartPhi, circEndPhi, circPointOrder, 1);
                    display.DispRegion(arcContour, HColor.Red);
                    display.DispPoint(midCol, midRow, HColor.OrangeRed, inPara.PointSize + 50);
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
                arcContour.Dispose();
            }
        }

        /// <summary>
        /// 释放旧轮廓并按给定点集重建 XLD 多边形轮廓。
        /// </summary>
        private static void RebuildContour(ref HObject contour, List<double> rowList, List<double> colList)
        {
            contour.Dispose();
            HOperatorSet.GenEmptyObj(out contour);
            HOperatorSet.GenContourPolygonXld(out contour, rowList.ToArray(), colList.ToArray());
        }

        /// <summary>
        /// 重建轮廓并用 atukey 鲁棒圆拟合，得到圆心/半径/起止角与点序。
        /// </summary>
        private static void FitArcFromPoints(ref HObject contour, List<double> rowList, List<double> colList,
            out HTuple circRow, out HTuple circCol, out HTuple circRadius,
            out HTuple circStartPhi, out HTuple circEndPhi, out HTuple circPointOrder)
        {
            RebuildContour(ref contour, rowList, colList);
            HOperatorSet.FitCircleContourXld(contour, "atukey", -1, 0, 0, 5, 2,
                out circRow, out circCol, out circRadius,
                out circStartPhi, out circEndPhi, out circPointOrder);
        }

        /// <summary>
        /// 根据拟合得到的起止角与点序，计算弧段中点所在角度（Halcon 图像坐标系）。
        /// </summary>
        private static double ComputeArcMidPhi(double startPhi, double endPhi, string pointOrder)
        {
            const double twoPi = 2 * Math.PI;
            if (pointOrder == "positive")
            {
                double span = endPhi - startPhi;
                if (span < 0) span += twoPi;
                return startPhi + span * 0.5;
            }
            else
            {
                double span = startPhi - endPhi;
                if (span < 0) span += twoPi;
                return startPhi - span * 0.5;
            }
        }

        public bool Fun_action2(DisplayUI display, List<IParaStrategy> strategys)
        {
            HObject imgReduced = new HObject(); HOperatorSet.GenEmptyObj(out imgReduced);
            HObject contourFitting = new HObject(); HOperatorSet.GenEmptyObj(out contourFitting);
            HObject arcContour = new HObject(); HOperatorSet.GenEmptyObj(out arcContour);

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

                HOperatorSet.ReduceDomain(ho_Image, ho_Rect, out imgReduced);
                display.DispRegion(ho_Rect, HColor.Blue);

                #region 变量
                HTuple fixAgl = inPara.HoRect.Phi;
                HTuple fixRow = inPara.HoRect.Center.Y;
                HTuple fixCol = inPara.HoRect.Center.X;
                HTuple fixLen1 = inPara.HoRect.Width / 2;
                HTuple fixLen2 = inPara.HoRect.Height / 2;
                HTuple imgWid = display.HoWidth;
                HTuple imgHei = display.HoHeight;
                #endregion

                #region 边缘查找
                double stepPace = Convert.ToDouble(inPara.StepPace); if (stepPace < 1) stepPace = 1;
                double stepWid = Convert.ToDouble(inPara.StepWidth) / 2; if (stepWid < 1) stepWid = 1;
                string transition = inPara.GetTransition;
                string select = inPara.GetContourType;

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
                    }
                    else if (mRow.Length > 1 && select == "second")
                    {
                        Array.Resize(ref rowFitting, rowFitting.Length + 1); rowFitting[rowFitting.Length - 1] = mRow.TupleSelect(1).D;
                        Array.Resize(ref colFitting, colFitting.Length + 1); colFitting[colFitting.Length - 1] = mCol.TupleSelect(1).D;
                    }
                    HOperatorSet.CloseMeasure(hMHandle);
                }
                #endregion

                #region 拟合圆弧中点
                if (rowFitting.Length >= 3)
                {
                    HOperatorSet.GenContourPolygonXld(out contourFitting, rowFitting, colFitting);

                    #region Stage 1：用 gauss 鲁棒直线拟合先剔除严重跑偏的点
                    double maxErr = inPara.MaxErr; if (maxErr < 0) maxErr = 0;
                    // 直线粗滤阈值适度放宽以容纳弧的凸量 (sagitta)
                    double lineGate = Math.Max(maxErr * 3.0, 15.0);

                    List<double> rowList = new List<double>(rowFitting);
                    List<double> colList = new List<double>(colFitting);
                    List<double> rowRemoved = new List<double>();
                    List<double> colRemoved = new List<double>();

                    HTuple lineRowBegin, lineColBegin, lineRowEnd, lineColEnd, lineNr, lineNc, lineDist;
                    HOperatorSet.FitLineContourXld(contourFitting, "gauss", -1, 0, 5, 1.345,
                        out lineRowBegin, out lineColBegin, out lineRowEnd, out lineColEnd,
                        out lineNr, out lineNc, out lineDist);

                    for (int i = rowList.Count - 1; i >= 0; i--)
                    {
                        HTuple curdis;
                        HOperatorSet.DistancePl(rowList[i], colList[i],
                            lineRowBegin, lineColBegin, lineRowEnd, lineColEnd, out curdis);
                        if (Math.Abs(curdis.D) > lineGate)
                        {
                            rowRemoved.Add(rowList[i]);
                            colRemoved.Add(colList[i]);
                            rowList.RemoveAt(i);
                            colList.RemoveAt(i);
                        }
                    }

                    if (rowList.Count < 3)
                    {
                        throw new NullReferenceException("直线粗滤后有效点不足，无法拟合圆弧！");
                    }
                    #endregion

                    #region Stage 2：atukey 圆拟合 + 径向距离迭代精滤
                    contourFitting.Dispose(); HOperatorSet.GenEmptyObj(out contourFitting);
                    HOperatorSet.GenContourPolygonXld(out contourFitting, rowList.ToArray(), colList.ToArray());

                    HTuple circRow, circCol, circRadius, circStartPhi, circEndPhi, circPointOrder;
                    HOperatorSet.FitCircleContourXld(contourFitting, "atukey", -1, 0, 0, 5, 2,
                        out circRow, out circCol, out circRadius, out circStartPhi, out circEndPhi, out circPointOrder);

                    int safety = rowList.Count;
                    for (int iter = 0; iter < safety; iter++)
                    {
                        int worstIdx = -1;
                        double worstErr = 0;
                        for (int i = 0; i < rowList.Count; i++)
                        {
                            double dRow = rowList[i] - circRow.D;
                            double dCol = colList[i] - circCol.D;
                            double err = Math.Abs(Math.Sqrt(dRow * dRow + dCol * dCol) - circRadius.D);
                            if (err > worstErr) { worstErr = err; worstIdx = i; }
                        }

                        if (worstIdx < 0 || worstErr <= maxErr) break;
                        if (rowList.Count <= 3) break;

                        rowRemoved.Add(rowList[worstIdx]);
                        colRemoved.Add(colList[worstIdx]);
                        rowList.RemoveAt(worstIdx);
                        colList.RemoveAt(worstIdx);

                        contourFitting.Dispose(); HOperatorSet.GenEmptyObj(out contourFitting);
                        HOperatorSet.GenContourPolygonXld(out contourFitting, rowList.ToArray(), colList.ToArray());
                        HOperatorSet.FitCircleContourXld(contourFitting, "atukey", -1, 0, 0, 5, 2,
                            out circRow, out circCol, out circRadius, out circStartPhi, out circEndPhi, out circPointOrder);
                    }

                    if (rowList.Count < 3)
                    {
                        throw new NullReferenceException("最大偏差筛选后有效点不足，无法拟合圆弧！");
                    }
                    #endregion

                    #region Stage 3：可选裁剪筛选后首尾点并重新拟合
                    if (inPara.IsTrimEnds && rowList.Count >= 5)
                    {
                        rowRemoved.Add(rowList[0]);
                        colRemoved.Add(colList[0]);
                        rowRemoved.Add(rowList[rowList.Count - 1]);
                        colRemoved.Add(colList[colList.Count - 1]);

                        rowList.RemoveAt(rowList.Count - 1);
                        colList.RemoveAt(colList.Count - 1);
                        rowList.RemoveAt(0);
                        colList.RemoveAt(0);

                        contourFitting.Dispose(); HOperatorSet.GenEmptyObj(out contourFitting);
                        HOperatorSet.GenContourPolygonXld(out contourFitting, rowList.ToArray(), colList.ToArray());
                        HOperatorSet.FitCircleContourXld(contourFitting, "atukey", -1, 0, 0, 5, 2,
                            out circRow, out circCol, out circRadius, out circStartPhi, out circEndPhi, out circPointOrder);
                    }

                    rowFitting = rowList.ToArray();
                    colFitting = colList.ToArray();
                    #endregion

                    if (inPara.DispFittingPoint)
                    {
                        for (int i = 0; i < rowRemoved.Count; i++)
                        {
                            display.DispPoint(colRemoved[i], rowRemoved[i], HColor.Red, inPara.PointSize);
                        }
                        for (int i = 0; i < rowFitting.Length; i++)
                        {
                            display.DispPoint(colFitting[i], rowFitting[i], HColor.Green, inPara.PointSize);
                        }
                    }

                    double startPhi = circStartPhi.D;
                    double endPhi = circEndPhi.D;
                    string pointOrder = circPointOrder.S;

                    double arcSpan, arcMidPhi;
                    if (pointOrder == "positive")
                    {
                        arcSpan = endPhi >= startPhi
                            ? (endPhi - startPhi)
                            : (2 * Math.PI + endPhi - startPhi);
                        arcMidPhi = startPhi + arcSpan / 2.0;
                    }
                    else
                    {
                        arcSpan = startPhi >= endPhi
                            ? (startPhi - endPhi)
                            : (2 * Math.PI + startPhi - endPhi);
                        arcMidPhi = startPhi - arcSpan / 2.0;
                    }

                    double midRow = circRow.D - circRadius.D * Math.Sin(arcMidPhi);
                    double midCol = circCol.D + circRadius.D * Math.Cos(arcMidPhi);
                    inPara.ArcMidpoint = new Point2d(midCol, midRow);

                    if (inPara.DispRegion) display.DispRegion(ho_Rect, HColor.Blue);
                    if (inPara.DispResult)
                    {
                        HOperatorSet.GenCircleContourXld(out arcContour,
                            circRow, circCol, circRadius, circStartPhi, circEndPhi, circPointOrder, 1);
                        display.DispRegion(arcContour, HColor.Red);
                        display.DispPoint(midCol, midRow, HColor.OrangeRed, inPara.PointSize + 50);
                    }
                }
                else
                {
                    throw new NullReferenceException("未找到足够的轮廓点！");
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
                arcContour.Dispose();
            }
        }

        public override void GenTreeNode(TreeVisualizer tree)
        {
            tree.Branch(Name, branch => branch
                       .Node("中点", OutEnum.Point, pt => pt
                               .Node("行", OutEnum.Number)
                               .Node("列", OutEnum.Number)
                           )
                       .CommonNodes()
                   );

            ClearResolvers();
            RegisterOutput("中点", () => inPara.ArcMidpoint);
            RegisterOutput("中点/行", () => inPara.ArcMidpoint.Y);
            RegisterOutput("中点/列", () => inPara.ArcMidpoint.X);
        }
        public override void DispPara(Form form, Dictionary<string, VsControlModel> VsControls)
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

            VsControls.ShowLabel(form, "lbl_113", "裁剪首尾");
            VsControls.ShowComboBoxList(form, "cmb_113", inPara.TrimEnds, new[] { "否", "是" });
            VsControls.ShowButton(form, "btn_113", false);

        }
        public override void SavePara(Form form, Dictionary<string, VsControlModel> VsControls)
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
            inPara.TrimEnds = VsControls["cmb_113"].Text;
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

        /// <summary> 圆弧中点 </summary>
        public Point2d ArcMidpoint { set; get; }

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
        public int StepPace { set; get; } = 10;

        /// <summary> 步宽 </summary>
        public int StepWidth { set; get; } = 5;

        /// <summary> 最大偏差 </summary>
        public int MaxErr { set; get; } = 5;

        /// <summary> 裁剪首尾点 </summary>
        public string TrimEnds { set; get; } = "是";

        internal bool IsTrimEnds => TrimEnds == "是";

        /// <summary> 点大小 </summary>
        public int PointSize { set; get; } = 15;

        /// <summary> 显示区域 </summary>
        public bool DispRegion { set; get; } = false;

        /// <summary> 显示结果 </summary>
        public bool DispResult { set; get; } = true;

        /// <summary> 显示文本 </summary>
        public bool DispText { set; get; } = false;

        /// <summary> 拟合点 </summary>
        public bool DispFittingPoint { set; get; } = true;

    }
}
