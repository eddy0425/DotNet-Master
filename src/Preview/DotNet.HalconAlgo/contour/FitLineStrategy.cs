using HalconDotNet;
using System;
using DotNet.Drawing;
using DotNet.HalconUI;
using System.Windows.Forms;
using System.Collections.Generic;


namespace DotNet.HalconAlgo
{
    public class FitLineStrategy : ParaStrategyBase<FitLine>
    {
        public override AlgoEnum Algorithm => AlgoEnum.FitLine;
        public override string Name { get; set; } = "拟合直线";
        public override int RunIndex { get; set; }

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
        public override bool Fun_action(HDisplayUI display, List<IParaStrategy> strategys)
        {
            HObject regionGet = new HObject(); HOperatorSet.GenEmptyObj(out regionGet);
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


                HTuple fixRow = inPara.HoRect.Center.Y;
                HTuple fixCol = inPara.HoRect.Center.X;
                if (inPara.CoordIn == "默认")
                {
                    HOperatorSet.ReduceDomain(ho_Image, ho_Rect, out imgReduced);
                    if (inPara.DispRegion) display.DispRegion(ho_Rect, HColor.Blue);
                }
                else
                {
                    var inCoord = strategys.ResolveFrom<CvCoord>(inPara.CoordIn);
                    var tmplPoint = strategys.ResolveFrom<Point2d>(inPara.CoordIn.ToTmplPoint());
                    HalconHelper.TransRegion(tmplPoint, inCoord.Center, ho_Rect, out regionGet);
                    HalconHelper.TransPixel(tmplPoint, inCoord.Center, fixRow, fixCol, out fixRow, out fixCol);

                    HOperatorSet.ReduceDomain(ho_Image, regionGet, out imgReduced);
                    if (inPara.DispRegion) display.DispRegion(regionGet, HColor.Blue);
                }

                #region 变量
                HTuple fixAgl = inPara.HoRect.Phi;
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

                        if (inPara.DispFixRegion)
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

                #region 拟合直线
                if (rowList.Count < 2)
                {
                    throw new InvalidOperationException("未找到足够的轮廓点！");
                }

                double maxErr = inPara.MaxErr; if (maxErr < 0) maxErr = 0;

                List<double> rowRemoved = new List<double>();
                List<double> colRemoved = new List<double>();

                #region Stage 1：gauss 鲁棒直线拟合
                HTuple rowBegin, colBegin, rowEnd, colEnd, nr, nc, lineDist;
                FitLineFromPoints(ref contourFitting, rowList, colList,
                    out rowBegin, out colBegin, out rowEnd, out colEnd,
                    out nr, out nc, out lineDist);
                #endregion

                #region Stage 2：依据最大偏差迭代精滤
                if (maxErr > 0)
                {
                    int safety = rowList.Count;
                    for (int iter = 0; iter < safety; iter++)
                    {
                        double pn = nr.D, pc = nc.D, pd = lineDist.D;
                        int worstIdx = -1;
                        double worstErr = 0;
                        for (int i = 0; i < rowList.Count; i++)
                        {
                            double err = Math.Abs(pn * rowList[i] + pc * colList[i] - pd);
                            if (err > worstErr) { worstErr = err; worstIdx = i; }
                        }

                        if (worstIdx < 0 || worstErr <= maxErr) break;
                        if (rowList.Count <= 2) break;

                        rowRemoved.Add(rowList[worstIdx]);
                        colRemoved.Add(colList[worstIdx]);
                        rowList.RemoveAt(worstIdx);
                        colList.RemoveAt(worstIdx);

                        FitLineFromPoints(ref contourFitting, rowList, colList,
                            out rowBegin, out colBegin, out rowEnd, out colEnd,
                            out nr, out nc, out lineDist);
                    }
                }

                if (rowList.Count < 2)
                {
                    throw new InvalidOperationException("最大偏差筛选后有效点不足，无法拟合直线！");
                }
                #endregion

                #region Stage 3：可选裁剪筛选后首尾点并重新拟合
                if (inPara.IsTrimEnds && rowList.Count >= 4)
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

                    FitLineFromPoints(ref contourFitting, rowList, colList,
                        out rowBegin, out colBegin, out rowEnd, out colEnd,
                        out nr, out nc, out lineDist);
                }
                #endregion

                inPara.Line = new CvLine(colBegin.D, rowBegin.D, colEnd.D, rowEnd.D);

                #endregion

                #region Display

                if (inPara.DispFixPoint)
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

                if (inPara.DispResult) display.DispArrow(inPara.Line, HColor.Red, 2);

                if (inPara.DispText)
                {
                    string message = $"{Name} : 起点:({inPara.Line.Start.X:F2},{inPara.Line.Start.Y:F2}) 终点:({inPara.Line.End.X:F2},{inPara.Line.End.Y:F2}) 角度:{inPara.Line.AngleDegrees:F2}° 用点:{rowList.Count}";
                    display.DispText(message, inPara.FontX, inPara.FontY, inPara.FontSize, HColor.Green);
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
                regionGet.Dispose();
                imgReduced.Dispose();
                contourFitting.Dispose();
                arcContour.Dispose();
            }
        }

        /// <summary>
        /// 重建轮廓并用 gauss 鲁棒直线拟合，得到端点与 Hesse 法线参数。
        /// </summary>
        private static void FitLineFromPoints(ref HObject contour, List<double> rowList, List<double> colList,
            out HTuple rowBegin, out HTuple colBegin, out HTuple rowEnd, out HTuple colEnd,
            out HTuple nr, out HTuple nc, out HTuple dist)
        {
            contour.Dispose();
            HOperatorSet.GenContourPolygonXld(out contour, rowList.ToArray(), colList.ToArray());
            HOperatorSet.FitLineContourXld(contour, "gauss", -1, 0, 5, 1.345,
                out rowBegin, out colBegin, out rowEnd, out colEnd, out nr, out nc, out dist);
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
        public override void SavePara(Form form, Dictionary<string, VsControlModel> VsControls)
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
            inPara.HoRect.Dispose();
        }
    }

    public class FitLine : AlgoFont
    {
        public FitLine()
        {
            HoRect.Type = RectEnum.AffRect;
        }

        /// <summary> 图像来源 </summary>
        public string ImageIn { set; get; } = "默认";

        /// <summary> 区域来源 </summary>
        public string RegionIn { set; get; } = "默认";

        /// <summary> 跟随坐标 </summary>
        public string CoordIn { set; get; } = "默认";

        /// <summary> 直线 </summary>
        public CvLine Line { set; get; }

        /// <summary> 区域 </summary>
        public CvRegion HoRect { set; get; } = new CvRegion();

        /// <summary> 过渡方向 </summary>
        public string Transition { set; get; } = "由黑到白";

        /// <summary> 获取过渡方向 </summary>
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

        /// <summary> 获取选择 </summary>
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
        public bool DispRegion { set; get; } = true;

        /// <summary> 显示拟合点 </summary>
        public bool DispFixPoint { set; get; } = true;

        /// <summary> 显示拟合区域 </summary>
        public bool DispFixRegion { set; get; } = false;

        /// <summary> 显示结果 </summary>
        public bool DispResult { set; get; } = true;

    }
}
