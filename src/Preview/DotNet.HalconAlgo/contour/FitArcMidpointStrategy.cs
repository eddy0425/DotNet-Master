using DotNet.Drawing;
using DotNet.Vision.Abstractions;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;


namespace DotNet.HalconAlgo
{
    public class FitArcMidpointStrategy : ParaStrategyBase<FitArcMidpoint>, IRoiEditable, IDisposable
    {
        public override AlgoEnum Algorithm => AlgoEnum.FitArcMidpoint;
        public override string Name { get; set; } = "圆弧中点";
        public override int RunIndex { get; set; }

        /// <summary> 粗滤阈值相对 MaxErr 的放大倍数 </summary>
        /// <remarks>
        /// 粗滤只负责剔除「明显落在别的边上」的点，判据要比 Stage 2 的 MaxErr 宽松，
        /// 否则等于把精滤提前做了一遍，还少了迭代重拟合的纠偏机会。
        /// </remarks>
        private const double CoarseGateErrScale = 3.0;

        // 每次拟合的显示数据槽：仅保留最近一次，未被取走的旧数据在覆盖时释放。
        // 可空是这个槽的语义本身 —— 「没有待取的数据」就是 null，TakeRenderData 取走后也会置回 null。
        private FitArcMidpointRenderData _pendingRenderData;
        private bool _disposed;

        public override void GenTreeNode(ITreeVisualizer tree)
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
                return ComputeFit(ho_Image, StrategyExtensions.EmptyList());
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
                hoImage = display.HoImage.RequireImage(Name);
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
            HObject regionGet; HOperatorSet.GenEmptyObj(out regionGet);
            HObject imgReduced; HOperatorSet.GenEmptyObj(out imgReduced);
            HObject contourFitting; HOperatorSet.GenEmptyObj(out contourFitting);

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
                bool useLocalRegion = inPara.RegionIn == "默认";
                HObject searchRegion;
                if (useLocalRegion)
                {
                    HObject localRegion = inPara.HoRect.HoRegion;

                    // 上游路径的空句柄已由 ResolveRegionFrom 拦下; 本地配置 ROI 不经过它, 需在此补同一层判断,
                    // 否则未绘制 ROI 的 0 长度空元组会一路流进 reduce_domain, 抛出与真实原因无关的 HALCON 原生异常。
                    if (!localRegion.IsUsableRegion())
                        throw new InvalidOperationException($"{Name} : 尚未绘制 ROI，无法执行拟合！");

                    searchRegion = localRegion;
                }
                else
                {
                    // 上游句柄由 ResolveRegionFrom 保证非空且可用（拿不到就抛），无需再判一次。
                    searchRegion = strategys.ResolveRegionFrom(inPara.RegionIn);
                }

                Point2d fixCenter = inPara.HoRect.Center;
                Angle fixPhi = Angle.FromRadians(inPara.HoRect.Phi.D);
                if (inPara.CoordIn != "默认")
                {
                    var inCoord = strategys.ResolveFrom<CvCoord>(inPara.CoordIn);
                    var tmplPoint = strategys.ResolveFrom<Point2d>(inPara.CoordIn.ToTmplPoint());
                    var tmplCoord = new CvCoord(tmplPoint);

                    // 本地测量几何独立跟随，不能因使用上游区域而跳过。
                    fixCenter = HalconController.TransPoint(tmplCoord, inCoord, fixCenter);
                    fixPhi = (fixPhi + inCoord.Angle).Normalized;

                    // 上游运行结果已在当前图像坐标系；只变换本地配置区域。
                    if (useLocalRegion)
                    {
                        regionGet.Dispose();
                        HalconController.TransRegion(tmplCoord, inCoord, searchRegion, out regionGet);
                        searchRegion = regionGet;
                    }
                }

                imgReduced.Dispose();
                HOperatorSet.ReduceDomain(ho_Image, searchRegion, out imgReduced);
                render.SearchRegion = searchRegion.Clone();

                #region 边缘查找
                HOperatorSet.GetImageSize(ho_Image, out HTuple imgWid, out HTuple imgHei);

                var setup = new EdgeMeasureSetup(
                    fixCenter,
                    fixPhi,
                    inPara.HoRect.Width / 2,
                    inPara.HoRect.Height / 2,
                    inPara.StepPace, inPara.StepWidth,
                    inPara.Sigma, inPara.Threshold,
                    inPara.GetTransition, inPara.GetContourType,
                    imgWid.I, imgHei.I);

                EdgeMeasureResult measured = EdgeMeasurePipeline.Run(imgReduced, setup);
                List<Point2d> points = measured.Points;

                render.MeasurePoints = measured.RectCenters;
                render.MeasurePhi = measured.Phi;
                render.MeasureLen1 = measured.HalfLength;
                render.MeasureLen2 = measured.HalfWidth;
                #endregion

                #region 拟合圆弧中点
                if (points.Count < MinFitPoints)
                {
                    throw new InvalidOperationException("未找到足够的轮廓点！");
                }

                double maxErr = inPara.MaxErr; if (maxErr < 0) maxErr = 0;
                double coarseFloor = inPara.CoarseGate; if (coarseFloor < 0) coarseFloor = 0;
                double coarseGate = Math.Max(maxErr * CoarseGateErrScale, coarseFloor);

                var removed = new List<Point2d>();

                // 拟合结果：由下面的 refit 闭包更新，供残差函数与最终取值共用
                HTuple circRow = 0, circCol = 0, circRadius = 0;
                HTuple circStartPhi = 0, circEndPhi = 0, circPointOrder = "positive";

                Action refit = () =>
                {
                    RobustFitPipeline.GenContour(ref contourFitting, points);
                    HOperatorSet.FitCircleContourXld(contourFitting, "atukey", -1, 0, 0, 5, 2,
                        out circRow, out circCol, out circRadius,
                        out circStartPhi, out circEndPhi, out circPointOrder);
                };

                // 残差一律取「到拟合圆的径向偏差」，Stage 1 / Stage 2 同一把尺子
                Func<Point2d, double> radialErr =
                    pt => RobustFitPipeline.CircleResidual(pt, circRow.D, circCol.D, circRadius.D);

                #region Stage 1：atukey 稳健圆拟合 + 径向残差一次性粗滤
                // 审查项 C12：原实现这一级用 gauss 直线拟合做粗滤。弧相对其弦存在凸量(sagitta)，
                // 曲率越大凸量越大 —— 半圆的凸量就等于半径，于是「正确的边缘点」自身的直线残差
                // 就能轻易越过门限被当成离群点剔掉，只能靠 15px 的经验下限硬撑，且撑不住大曲率。
                // 改成直接用圆拟合后，模型与待测几何同形，凸量问题从根上消失；
                // atukey 是重降权(redescending)估计，离群点在首次拟合时权重就已趋零，
                // 不必先用另一种模型粗滤一遍。
                refit();

                int coarseCulled = RobustFitPipeline.RemoveOutliers(points, removed, coarseGate, radialErr);

                if (points.Count < MinFitPoints)
                {
                    throw new InvalidOperationException("粗滤后有效点不足，无法拟合圆弧！");
                }

                // 点集变了，模型必须跟上：Stage 2 的收敛判据建立在「模型对应当前点集」之上
                if (coarseCulled > 0) refit();
                #endregion

                #region Stage 2：径向距离迭代精滤
                RobustFitPipeline.Refine(points, removed, maxErr, MinFitPoints, radialErr, refit);

                if (points.Count < MinFitPoints)
                {
                    throw new InvalidOperationException("最大偏差筛选后有效点不足，无法拟合圆弧！");
                }
                #endregion

                #region Stage 3：可选裁剪筛选后首尾点并重新拟合
                if (inPara.IsTrimEnds &&
                    RobustFitPipeline.TrimEnds(points, removed, MinFitPoints + 2))
                {
                    refit();
                }
                #endregion

                Angle arcMidPhi = ComputeArcMidPhi(
                    Angle.FromRadians(circStartPhi.D), Angle.FromRadians(circEndPhi.D), circPointOrder.S);
                double midRow = circRow.D - circRadius.D * Math.Sin(arcMidPhi.Radians);
                double midCol = circCol.D + circRadius.D * Math.Cos(arcMidPhi.Radians);
                inPara.ArcMidpoint = new Point2d(midCol, midRow);

                #endregion

                #region 显示数据

                render.UsedPoints = points;
                render.RemovedPoints = removed;

                HOperatorSet.GenCircleContourXld(out HObject arcContour, circRow, circCol, circRadius, circStartPhi, circEndPhi, circPointOrder, 1);
                render.ArcContour = arcContour;

                render.Midpoint = inPara.ArcMidpoint;
                render.HasMidpoint = true;
                render.Message = $"{Name} : 中点:({inPara.ArcMidpoint.X:F2},{inPara.ArcMidpoint.Y:F2}) 半径:{circRadius.D:F2} 用点:{points.Count}";

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

        /// <summary> 拟合一段圆弧所需的最少点数 </summary>
        private const int MinFitPoints = 3;

        /// <summary>
        /// 根据拟合得到的起止角与点序，计算弧段中点所在角度（Halcon 图像坐标系）。
        /// </summary>
        private static Angle ComputeArcMidPhi(Angle startPhi, Angle endPhi, string pointOrder)
        {
            const double twoPi = 2 * Math.PI;
            double start = startPhi.Radians;
            double end = endPhi.Radians;

            if (pointOrder == "positive")
            {
                double span = end - start;
                if (span < 0) span += twoPi;
                return Angle.FromRadians(start + span * 0.5);
            }
            else
            {
                double span = start - end;
                if (span < 0) span += twoPi;
                return Angle.FromRadians(start - span * 0.5);
            }
        }
        public override void DispPara(IParaUiHost ui)
        {
            ui.ShowTabs(TabPageEnum.Parameter, TabPageEnum.Region, TabPageEnum.Display);

            ui.ShowComboBox("cmb_CoordIn", inPara.CoordIn.ToString(), false);

            CvRegion hRegion = inPara.HoRect;
            ui.ShowComboBox("cmb_Width", hRegion.Width.ToString(), false);
            ui.ShowComboBox("cmb_Height", hRegion.Height.ToString(), false);
            ui.ShowComboBox("cmb_TopLeft", $"{hRegion.TopLeft.X};{hRegion.TopLeft.Y}", false);
            ui.ShowComboBox("cmb_BottomRight", $"{hRegion.BottomRight.X};{hRegion.BottomRight.Y}", false);
            ui.ShowComboBox("cmb_Center", $"{hRegion.Center.X};{hRegion.Center.Y}", false);

            ui.ShowLabel("lbl_100", "图像来源");
            ui.ShowComboBox("cmb_100", inPara.ImageIn, false);
            ui.ShowButton("btn_100", true);

            ui.ShowLabel("lbl_101", "区域来源");
            ui.ShowComboBox("cmb_101", inPara.RegionIn, false);
            ui.ShowButton("btn_101", true);

            ui.ShowLabel("lbl_102", "过渡方向");
            ui.ShowComboBoxList("cmb_102", inPara.Transition, new[] { "由黑到白", "由白到黑", "全部" });
            ui.ShowButton("btn_102", false);

            ui.ShowLabel("lbl_103", "选择");
            ui.ShowComboBoxList("cmb_103", inPara.ContourType, new[] { "第一条边", "第二条边", "最后一条", "全部" });
            ui.ShowButton("btn_103", false);

            ui.ShowLabel("lbl_104", "滤波");
            ui.ShowComboBoxDropDown("cmb_104", inPara.Sigma.ToString(), new[] { "0", "1" });
            ui.ShowButton("btn_104", false);

            ui.ShowLabel("lbl_105", "阈值");
            ui.ShowComboBoxDropDown("cmb_105", inPara.Threshold.ToString(), new[] { "30", "50" });
            ui.ShowButton("btn_105", false);

            ui.ShowLabel("lbl_110", "步距");
            ui.ShowComboBoxDropDown("cmb_110", inPara.StepPace.ToString(), new[] { "2", "5", "10" });

            ui.ShowLabel("lbl_111", "步宽");
            ui.ShowComboBoxDropDown("cmb_111", inPara.StepWidth.ToString(), new[] { "2", "5", "10" });

            ui.ShowLabel("lbl_112", "最大偏差");
            ui.ShowComboBoxDropDown("cmb_112", inPara.MaxErr.ToString(), new[] { "1", "3", "5", "10" });

            ui.ShowLabel("lbl_113", "裁剪首尾");
            ui.ShowComboBoxList("cmb_113", inPara.TrimEnds, new[] { "否", "是" });
            ui.ShowButton("btn_113", false);

            ui.ShowLabel("lbl_114", "粗滤阈值");
            ui.ShowComboBoxDropDown("cmb_114", inPara.CoarseGate.ToString(), new[] { "5", "10", "15", "30" });

            //------------------------------------------
            ui.ShowCheckBox("ckb_disp0", "显示文本", inPara.DispText);
            ui.ShowCheckBox("ckb_disp1", "查找区域", inPara.DispRegion);
            ui.ShowCheckBox("ckb_disp2", "拟合区域", inPara.DispFixRegion);
            ui.ShowCheckBox("ckb_disp3", "拟合点", inPara.DispFixPoint);
            ui.ShowCheckBox("ckb_disp4", "显示结果", inPara.DispResult);

            ui.ShowComboBoxDropDown("CB_FontX", inPara.FontX.ToString(), new[] { "20", "50" });
            ui.ShowComboBoxDropDown("CB_FontY", inPara.FontY.ToString(), new[] { "20", "50" });
            ui.ShowComboBoxDropDown("CB_FontSize", inPara.FontSize.ToString(), new[] { "15", "30" });
        }
        public override void SavePara(IParaUiHost ui)
        {
            inPara.CoordIn = ui.GetString("cmb_CoordIn");

            inPara.ImageIn = ui.GetString("cmb_100");
            inPara.RegionIn = ui.GetString("cmb_101");
            inPara.Transition = ui.GetString("cmb_102");
            inPara.ContourType = ui.GetString("cmb_103");
            inPara.Sigma = ui.GetInt("cmb_104");
            inPara.Threshold = ui.GetInt("cmb_105");

            inPara.StepPace = ui.GetInt("cmb_110");
            inPara.StepWidth = ui.GetInt("cmb_111");
            inPara.MaxErr = ui.GetInt("cmb_112");
            inPara.TrimEnds = ui.GetString("cmb_113");
            inPara.CoarseGate = ui.GetDouble("cmb_114", 15.0);

            //------------------------------------------
            inPara.DispText = ui.GetBool("ckb_disp0");
            inPara.DispRegion = ui.GetBool("ckb_disp1");
            inPara.DispFixRegion = ui.GetBool("ckb_disp2");
            inPara.DispFixPoint = ui.GetBool("ckb_disp3");
            inPara.DispResult = ui.GetBool("ckb_disp4");

            inPara.FontX = ui.GetInt("CB_FontX");
            inPara.FontY = ui.GetInt("CB_FontY");
            inPara.FontSize = ui.GetInt("CB_FontSize");
        }
        public async Task DrawROIAsync(IRoiHost host, RectEnum type, bool newROI)
        {
            if (newROI)
            {
                // Type 必须在绘制前写入(HDisplay 按它分发图元), 但取消时几何不会被回写,
                // 所以要连 Type 一起还原, 否则 Type 与 HoRegion / 外接框对不上, 还会存进 job 配置.
                var prevType = inPara.HoRect.Type;
                inPara.HoRect.Type = type;
                if (!await host.DrawRegionAsync(inPara.HoRect))
                    inPara.HoRect.Type = prevType;
            }
            else await host.DrawRegionModAsync(inPara.HoRect);

            // 这里故意不短路: 取消后仍要把原 ROI 重画回去(ParaForm 事先 ReDispImage 已清屏)
            host.Display.Disp(inPara.HoRect, DrawStyle.Of(HColor.Blue));
            host.SetRectPara(inPara.HoRect);
        }
        public void DispROI(IRoiHost host)
        {
            inPara.HoRect.Type = RectEnum.AffRect;
            host.SetRectPara(inPara.HoRect);
        }
        /// <summary>
        /// 关闭工具页. 只丢弃运行期的显示数据, 不碰配置态的 <c>inPara.HoRect</c> —— 原实现直接
        /// 转调 Dispose, 会把配置 ROI 一并销毁; 而策略实例在宿主里是长期复用的, 再次打开必 NRE.
        /// </summary>
        public override void Close(IRoiHost host)
        {
            TakeRenderData()?.Dispose();
        }

        /// <summary>策略实例生命周期结束时才释放配置态资源. 幂等.</summary>
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

        /// <summary> 粗滤阈值下限（像素） </summary>
        /// <remarks>
        /// Stage 1 的实际门限取 <c>Max(MaxErr * 3, CoarseGate)</c>：径向偏差超过它的点
        /// 在首次稳健圆拟合后就整批剔除，不参与后续迭代。
        /// <para>
        /// 默认值沿用改造前的硬编码 15（审查项 C12 要求把它提为可配置参数），
        /// 但含义已经变了：改造前 Stage 1 是直线拟合，这 15px 主要用来兜住弧的凸量，
        /// 调小会误删有效点；现在 Stage 1 已是圆拟合，凸量不再计入残差，
        /// 需要更早拦住跳到邻边的点时可以放心调小。
        /// </para>
        /// </remarks>
        public double CoarseGate { set; get; } = 15.0;

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
