using DotNet.Drawing;
using HalconDotNet;
using System;
using System.Collections.Generic;

namespace DotNet.HalconAlgo
{
    /// <summary>
    /// 一次卡尺式边缘查找的输入参数。构造时完成全部钳位与枚举翻译，
    /// 因此所有调用方拿到的都是同一套规范化后的量。
    /// </summary>
    /// <remarks>
    /// 长度语义统一为「半量」：<see cref="HalfLength"/> 是测量矩形沿 Phi 方向的半长，
    /// <see cref="HalfHeight"/> 是 ROI 沿 Phi 方向的半高（决定采样步数），
    /// <see cref="HalfWidth"/> 是单个测量矩形的半宽。与 Halcon
    /// <c>gen_measure_rectangle2</c> 的 Length1 / Length2 语义一致。
    /// </remarks>
    public readonly struct EdgeMeasureSetup
    {
        /// <summary> ROI 中心（图像坐标，X=列 Y=行） </summary>
        public Point2d Center { get; }

        /// <summary> ROI 朝向 </summary>
        public Angle Phi { get; }

        /// <summary> 测量矩形半长（Halcon Length1） </summary>
        public double HalfLength { get; }

        /// <summary> ROI 半高，采样沿此方向铺开 </summary>
        public double HalfHeight { get; }

        /// <summary> 测量矩形半宽（Halcon Length2），最小 1 </summary>
        public double HalfWidth { get; }

        /// <summary> 采样步距（像素），最小 1 </summary>
        public double StepPace { get; }

        /// <summary> 高斯平滑 sigma </summary>
        public int Sigma { get; }

        /// <summary> 边缘幅值阈值 </summary>
        public int Threshold { get; }

        /// <summary> 过渡方向：positive / negative / all </summary>
        public string Transition { get; }

        /// <summary> 传给 measure_pos 的 Select 参数 </summary>
        public string MeasureSelect { get; }

        /// <summary> 从 measure_pos 结果里取第几个点 </summary>
        public int PickIndex { get; }

        public int ImageWidth { get; }
        public int ImageHeight { get; }

        /// <param name="contourType">first / second / last / all。"second" 需要先取 all 再取下标 1，此处一并翻译。</param>
        public EdgeMeasureSetup(Point2d center, Angle phi, double halfLength, double halfHeight,
            int stepPace, int stepWidth, int sigma, int threshold,
            string transition, string contourType, int imageWidth, int imageHeight)
        {
            Center = center;
            Phi = phi;
            HalfLength = halfLength;
            HalfHeight = halfHeight;
            HalfWidth = Math.Max(stepWidth / 2.0, 1);
            StepPace = Math.Max(stepPace, 1);
            Sigma = sigma;
            Threshold = threshold;
            Transition = transition;

            // "第二条边" 在 Halcon 里没有直接对应的 Select，只能取全部再按下标挑
            bool second = contourType == "second";
            MeasureSelect = second ? "all" : contourType;
            PickIndex = second ? 1 : 0;

            ImageWidth = imageWidth;
            ImageHeight = imageHeight;
        }
    }

    /// <summary>
    /// 边缘查找结果。坐标一律是 <see cref="Point2d"/>(X=列, Y=行)，
    /// Halcon 的 (Row, Col) 序不越出 <see cref="EdgeMeasurePipeline"/>（审查项 C1）。
    /// </summary>
    public sealed class EdgeMeasureResult
    {
        /// <summary> 找到的边缘点 </summary>
        public List<Point2d> Points { get; }

        /// <summary> 每一步测量矩形的中心，仅用于显示「拟合区域」 </summary>
        public List<Point2d> RectCenters { get; }

        /// <summary> 测量矩形姿态与尺寸，各步相同 </summary>
        public Angle Phi { get; }
        public double HalfLength { get; }
        public double HalfWidth { get; }

        internal EdgeMeasureResult(List<Point2d> points, List<Point2d> rectCenters,
            Angle phi, double halfLength, double halfWidth)
        {
            Points = points;
            RectCenters = rectCenters;
            Phi = phi;
            HalfLength = halfLength;
            HalfWidth = halfWidth;
        }
    }

    /// <summary>
    /// 沿 ROI 主轴等距铺开一串测量矩形，逐个 <c>measure_pos</c> 取边缘点。
    /// </summary>
    /// <remarks>
    /// 提取动机（审查项 D5）：拟合直线与圆弧中点两个策略各自抄了一份完全相同的
    /// <c>gen_measure_rectangle2</c> → <c>measure_pos</c> 循环，包括同样的步数计算、
    /// 同样的 "second" 特判、同样的 <c>CloseMeasure</c> 释放。任何一处修 bug 都要记得改两遍。
    /// </remarks>
    public static class EdgeMeasurePipeline
    {
        /// <param name="reducedImage">已 reduce_domain 到 ROI 的图像</param>
        public static EdgeMeasureResult Run(HObject reducedImage, EdgeMeasureSetup setup)
        {
            if (reducedImage == null) throw new ArgumentNullException(nameof(reducedImage));

            int stepCount = (int)(setup.HalfHeight / setup.StepPace + 0.5);
            if (stepCount < 1) stepCount = 1;

            // 沿 Phi 的法线方向铺开：与原实现一致，行增量取 cos、列增量取 sin
            double rowStep = setup.HalfHeight * Math.Cos(setup.Phi.Radians) / stepCount;
            double colStep = setup.HalfHeight * Math.Sin(setup.Phi.Radians) / stepCount;

            int capacity = 2 * stepCount + 1;
            var points = new List<Point2d>(capacity);
            var rectCenters = new List<Point2d>(capacity);

            for (int s = -stepCount; s <= stepCount; s++)
            {
                double row = setup.Center.Y + s * rowStep;
                double col = setup.Center.X + s * colStep;
                rectCenters.Add(new Point2d(col, row));

                HTuple measureHandle;
                HOperatorSet.GenMeasureRectangle2(row, col, setup.Phi.Radians,
                    setup.HalfLength, setup.HalfWidth,
                    setup.ImageWidth, setup.ImageHeight, "nearest_neighbor", out measureHandle);
                try
                {
                    HTuple mRow, mCol, mAmp, mDis;
                    HOperatorSet.MeasurePos(reducedImage, measureHandle, setup.Sigma, setup.Threshold,
                        setup.Transition, setup.MeasureSelect, out mRow, out mCol, out mAmp, out mDis);

                    if (mRow.Length > setup.PickIndex)
                    {
                        // (Row, Col) → (X, Y) 的唯一翻转点
                        points.Add(new Point2d(mCol.TupleSelect(setup.PickIndex).D,
                                               mRow.TupleSelect(setup.PickIndex).D));
                    }
                }
                finally
                {
                    HOperatorSet.CloseMeasure(measureHandle);
                }
            }

            return new EdgeMeasureResult(points, rectCenters, setup.Phi, setup.HalfLength, setup.HalfWidth);
        }
    }
}
