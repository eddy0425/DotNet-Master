using DotNet.Drawing;
using DotNet.HalconUI;
using HalconDotNet;
using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading;


namespace DotNet.HalconAlgo
{
    public class FitArcMidpointStrategy : ParaStrategyBase<FitArcMidpoint>, IDisposable
    {
        public override AlgoEnum Algorithm => AlgoEnum.FitArcMidpoint;
        public override string Name { get; set; } = "圆弧中点";
        public override int RunIndex { get; set; }

        // 每次拟合的显示数据槽：仅保留最近一次，未被取走的旧数据在覆盖时释放
        private FitArcMidpointRenderData _pendingRenderData;
        private bool _disposed;

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

        public override bool Fun_action(HObject ho_Image, IHDisplay display)
        {
            display.SetImage(ho_Image);
            try
            {
                return ComputeFit(ho_Image, null);
            }
            finally
            {
                DrawPendingOverlay(display);
            }
        }

        public override bool Fun_action(IHDisplay display, List<IParaStrategy> strategys)
        {
            HObject hoImage;
            if (inPara.ImageIn == "默认")
                hoImage = display.HoImage;
            else
                hoImage = strategys.ResolveFrom<HObject>(inPara.ImageIn);
            try
            {
                return ComputeFit(hoImage, strategys);
            }
            finally
            {
                DrawPendingOverlay(display);
            }
        }

        /// <summary>
        /// 取走最近一次拟合的显示数据，所有权随之转移（调用方负责 Dispose）；无数据返回 null。
        /// </summary>
        public FitArcMidpointRenderData TakeRenderData()
        {
            return Interlocked.Exchange(ref _pendingRenderData, null);
        }

        private void PublishRenderData(FitArcMidpointRenderData data)
        {
            Interlocked.Exchange(ref _pendingRenderData, data)?.Dispose();
        }

        /// <summary>
        /// 编辑器同步路径：拟合结束后立即绘制并释放本次显示数据。
        /// </summary>
        private void DrawPendingOverlay(IHDisplay display)
        {
            using (var data = TakeRenderData())
            {
                data?.DrawTo(display);
            }
        }

        /// <summary>
        /// 纯计算：不触碰任何显示对象。显示数据在拟合过程中写入 FitArcMidpointRenderData，
        /// 无论成败都会发布（失败时为部分数据），由调用方决定同步绘制还是交给渲染线程。
        /// </summary>
        protected bool ComputeFit(HObject ho_Image, List<IParaStrategy> strategys)
        {
            HObject regionGet = new HObject(); HOperatorSet.GenEmptyObj(out regionGet);
            HObject imgReduced = new HObject(); HOperatorSet.GenEmptyObj(out imgReduced);
            HObject contourFitting = new HObject(); HOperatorSet.GenEmptyObj(out contourFitting);

            var render = new FitArcMidpointRenderData
            {
                PointSize = inPara.PointSize,
                FontX = inPara.FontX,
                FontY = inPara.FontY,
                FontSize = inPara.FontSize,
                ShowRegion = inPara.DispRegion,
                ShowFixRegion = inPara.DispFixRegion,
                ShowPoints = inPara.DispFixPoint,
                ShowResult = inPara.DispResult,
                ShowText = inPara.DispText,
            };

            try
            {
                HObject ho_Rect;
                if (inPara.RegionIn == "默认")
                    ho_Rect = inPara.HoRect.HoRegion;
                else
                    ho_Rect = strategys.ResolveFrom<HObject>(inPara.RegionIn);

                HTuple fixRow = inPara.HoRect.Center.Y;
                HTuple fixCol = inPara.HoRect.Center.X;
                if (inPara.CoordIn == "默认")
                {
                    imgReduced.Dispose();
                    HOperatorSet.ReduceDomain(ho_Image, ho_Rect, out imgReduced);
                    render.SearchRegion = ho_Rect.Clone();
                }
                else
                {
                    var inCoord = strategys.ResolveFrom<CvCoord>(inPara.CoordIn);
                    var tmplPoint = strategys.ResolveFrom<Point2d>(inPara.CoordIn.ToTmplPoint());
                    regionGet.Dispose();
                    HalconHelper.TransRegion(tmplPoint, inCoord.Center, ho_Rect, out regionGet);
                    HalconHelper.TransPixel(tmplPoint, inCoord.Center, fixRow, fixCol, out fixRow, out fixCol);

                    imgReduced.Dispose();
                    HOperatorSet.ReduceDomain(ho_Image, regionGet, out imgReduced);
                    render.SearchRegion = regionGet.Clone();
                }

                #region 变量
                HTuple fixAgl = inPara.HoRect.Phi;
                HTuple fixLen1 = inPara.HoRect.Width / 2;
                HTuple fixLen2 = inPara.HoRect.Height / 2;
                HOperatorSet.GetImageSize(ho_Image, out HTuple imgWid, out HTuple imgHei);
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

                render.MeasurePhi = fixAgl.D;
                render.MeasureLen1 = fixLen1.D;
                render.MeasureLen2 = stepWid;

                var rowList = new List<double>(2 * loop_cnt + 1);
                var colList = new List<double>(2 * loop_cnt + 1);

                for (int s = -loop_cnt; s <= loop_cnt; s++)
                {
                    HTuple rowNew = fixRow + s * cosLen2;
                    HTuple colNew = fixCol + s * sinLen2;
                    render.MeasureRows.Add(rowNew.D);
                    render.MeasureCols.Add(colNew.D);

                    HTuple hMHandle;
                    HOperatorSet.GenMeasureRectangle2(rowNew, colNew, fixAgl, fixLen1, stepWid,
                        imgWid, imgHei, "nearest_neighbor", out hMHandle);
                    try
                    {
                        HTuple mRow, mCol, mAmp, mDis;
                        HOperatorSet.MeasurePos(imgReduced, hMHandle, inPara.Sigma, inPara.Threshold,
                            transition, measureSelect, out mRow, out mCol, out mAmp, out mDis);

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

                var rowRemoved = new List<double>();
                var colRemoved = new List<double>();

                #region Stage 1：gauss 鲁棒直线拟合剔除严重跑偏的点
                contourFitting.Dispose();
                HOperatorSet.GenContourPolygonXld(out contourFitting, rowList.ToArray(), colList.ToArray());

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

                double arcMidPhi = ComputeArcMidPhi(circStartPhi.D, circEndPhi.D, circPointOrder.S);
                double midRow = circRow.D - circRadius.D * Math.Sin(arcMidPhi);
                double midCol = circCol.D + circRadius.D * Math.Cos(arcMidPhi);
                inPara.ArcMidpoint = new Point2d(midCol, midRow);

                #endregion

                #region 显示数据

                render.UsedRows = rowList;
                render.UsedCols = colList;
                render.RemovedRows = rowRemoved;
                render.RemovedCols = colRemoved;

                HOperatorSet.GenCircleContourXld(out HObject arcContour, circRow, circCol, circRadius, circStartPhi, circEndPhi, circPointOrder, 1);
                render.ArcContour = arcContour;

                render.MidRow = midRow;
                render.MidCol = midCol;
                render.HasMidpoint = true;
                render.Message = $"{Name} : 中点:({inPara.ArcMidpoint.X:F2},{inPara.ArcMidpoint.Y:F2}) 半径:{circRadius.D:F2} 用点:{rowList.Count}";

                #endregion

                return true;
            }
            finally
            {
                regionGet.Dispose();
                imgReduced.Dispose();
                contourFitting.Dispose();
                PublishRenderData(render);
            }
        }

        /// <summary>
        /// 重建轮廓并用 atukey 鲁棒圆拟合，得到圆心/半径/起止角与点序。
        /// </summary>
        private static void FitArcFromPoints(ref HObject contour, List<double> rowList, List<double> colList,
            out HTuple circRow, out HTuple circCol, out HTuple circRadius,
            out HTuple circStartPhi, out HTuple circEndPhi, out HTuple circPointOrder)
        {
            contour.Dispose();
            HOperatorSet.GenContourPolygonXld(out contour, rowList.ToArray(), colList.ToArray());
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
        public override void DispPara(Control form, Dictionary<string, VsControlModel> VsControls)
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

            //------------------------------------------
            VsControls.ShowCheckBox(form, "ckb_disp0", "显示文本", inPara.DispText);
            VsControls.ShowCheckBox(form, "ckb_disp1", "查找区域", inPara.DispRegion);
            VsControls.ShowCheckBox(form, "ckb_disp2", "拟合区域", inPara.DispFixRegion);
            VsControls.ShowCheckBox(form, "ckb_disp3", "拟合点", inPara.DispFixPoint);
            VsControls.ShowCheckBox(form, "ckb_disp4", "显示结果", inPara.DispResult);

            VsControls.ShowComboBoxDropDown(form, "CB_FontX", inPara.FontX.ToString(), new[] { "20", "50" });
            VsControls.ShowComboBoxDropDown(form, "CB_FontY", inPara.FontY.ToString(), new[] { "20", "50" });
            VsControls.ShowComboBoxDropDown(form, "CB_FontSize", inPara.FontSize.ToString(), new[] { "15", "30" });
        }
        public override void SavePara(Control form, Dictionary<string, VsControlModel> VsControls)
        {
            inPara.CoordIn = VsControls["cmb_CoordIn"].AsString();

            inPara.ImageIn = VsControls["cmb_100"].AsString();
            inPara.RegionIn = VsControls["cmb_101"].AsString();
            inPara.Transition = VsControls["cmb_102"].AsString();
            inPara.ContourType = VsControls["cmb_103"].AsString();
            inPara.Sigma = VsControls["cmb_104"].AsInt();
            inPara.Threshold = VsControls["cmb_105"].AsInt();

            inPara.StepPace = VsControls["cmb_110"].AsInt();
            inPara.StepWidth = VsControls["cmb_111"].AsInt();
            inPara.MaxErr = VsControls["cmb_112"].AsInt();
            inPara.TrimEnds = VsControls["cmb_113"].AsString();

            //------------------------------------------
            inPara.DispText = VsControls["ckb_disp0"].AsBool();
            inPara.DispRegion = VsControls["ckb_disp1"].AsBool();
            inPara.DispFixRegion = VsControls["ckb_disp2"].AsBool();
            inPara.DispFixPoint = VsControls["ckb_disp3"].AsBool();
            inPara.DispResult = VsControls["ckb_disp4"].AsBool();

            inPara.FontX = VsControls["CB_FontX"].AsInt();
            inPara.FontY = VsControls["CB_FontY"].AsInt();
            inPara.FontSize = VsControls["CB_FontSize"].AsInt();
        }
        public override void DrawROI(HDisplayUI display, RectEnum type, bool newROI)
        {
            if (newROI)
            {
                inPara.HoRect.Type = type;
                display.DrawRegion(inPara.HoRect);
            }
            else display.DrawRegionMod(inPara.HoRect);

            display.DispRegion(inPara.HoRect, HColor.Blue);
            display.SetRectPara(inPara.HoRect);
        }
        public override void DispROI(HDisplayUI display)
        {
            inPara.HoRect.Type = RectEnum.AffRect;
            display.SetRectPara(inPara.HoRect);
        }
        public override void Close(HDisplayUI display)
        {
            Dispose();
        }
        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            inPara?.HoRect?.Dispose();
            TakeRenderData()?.Dispose();
        }
    }

    public class FitArcMidpoint : AlgoFont
    {
        public FitArcMidpoint()
        {
            HoRect.Type = RectEnum.AffRect;
        }

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
        public int Threshold { set; get; } = 60;

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
        public bool DispRegion { set; get; } = true;

        /// <summary> 显示拟合点 </summary>
        public bool DispFixPoint { set; get; } = true;

        /// <summary> 显示拟合区域 </summary>
        public bool DispFixRegion { set; get; } = false;

        /// <summary> 显示结果 </summary>
        public bool DispResult { set; get; } = true;

    }
}
