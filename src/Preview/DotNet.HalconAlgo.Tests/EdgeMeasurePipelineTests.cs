using System;
using System.Linq;
using DotNet.Drawing;
using HalconDotNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNet.HalconAlgo.Tests
{
    [TestClass]
    public class EdgeMeasurePipelineTests : HalconTestBase
    {
        private const int W = 200, H = 100;

        /// <summary>中心 (100,50)，phi=0：沿列方向测量，沿行方向步进。</summary>
        private static EdgeMeasureSetup Setup(string transition, string select,
            double halfHeight = 30, int stepPace = 10, int threshold = 30, double phiDeg = 0, Point2d? center = null)
            => new EdgeMeasureSetup(center ?? new Point2d(100, 50), Angle.FromDegrees(phiDeg), 30, halfHeight,
                stepPace, 5, 1, threshold, transition, select, W, H);

        /// <summary>列 [100, 120) 为亮条：列 99.5 处上升沿，119.5 处下降沿（都在测量矩形 70..130 内）。</summary>
        private static HObject StripeImage()
        {
            using (var dark = ConstImage(W, H, 0))
            using (var stripe = Rectangle1(0, 100, H - 1, 119))
            {
                return Paint(dark, stripe, 255);
            }
        }

        [TestMethod]
        public void Run_NullImage_Throws()
        {
            Assert.ThrowsException<ArgumentNullException>(() => EdgeMeasurePipeline.Run(null, Setup("positive", "first")));
        }

        [TestMethod]
        public void Run_StepCountAndRectCenters()
        {
            using (var image = VerticalStepImage(W, H, 100))
            {
                var result = EdgeMeasurePipeline.Run(image, Setup("positive", "first", halfHeight: 30, stepPace: 10));

                // stepCount = round(30 / 10) = 3 → 2*3+1 = 7 个测量矩形，行 20..80
                Assert.AreEqual(7, result.RectCenters.Count);
                CollectionAssert.AreEqual(new[] { 20.0, 30, 40, 50, 60, 70, 80 },
                    result.RectCenters.Select(c => Math.Round(c.Y, 6)).ToArray());
                Assert.IsTrue(result.RectCenters.All(c => Math.Abs(c.X - 100) < 1e-9), "phi=0 时步进方向是行，列不变");

                Assert.AreEqual(30, result.HalfLength);
                Assert.AreEqual(2.5, result.HalfWidth);
                Assert.AreEqual(0, result.Phi.Radians);
            }
        }

        [TestMethod]
        public void Run_TinyHalfHeight_StillMeasuresAtLeastThreeRects()
        {
            using (var image = VerticalStepImage(W, H, 100))
            {
                var result = EdgeMeasurePipeline.Run(image, Setup("positive", "first", halfHeight: 2, stepPace: 10));
                Assert.AreEqual(3, result.RectCenters.Count, "步数至少为 1");
            }
        }

        [TestMethod]
        public void Run_FindsStepEdge_OnePointPerRect()
        {
            using (var image = VerticalStepImage(W, H, 100))
            {
                var result = EdgeMeasurePipeline.Run(image, Setup("positive", "first"));

                Assert.AreEqual(7, result.Points.Count);
                foreach (var p in result.Points)
                    Assert.AreEqual(99.5, p.X, 0.5, "边缘点 X 应为列坐标（不是行）");
                CollectionAssert.AreEqual(result.RectCenters.Select(c => Math.Round(c.Y, 3)).ToArray(),
                    result.Points.Select(p => Math.Round(p.Y, 3)).ToArray(), "边缘点落在各测量矩形的中轴上");
            }
        }

        [TestMethod]
        public void Run_WrongTransition_FindsNothing()
        {
            using (var image = VerticalStepImage(W, H, 100))
            {
                var result = EdgeMeasurePipeline.Run(image, Setup("negative", "first"));
                Assert.AreEqual(0, result.Points.Count);
                Assert.AreEqual(7, result.RectCenters.Count, "找不到点时测量矩形仍全部返回，供显示排查");
            }
        }

        [DataTestMethod]
        [DataRow("first", 99.5)]
        [DataRow("last", 119.5)]
        [DataRow("second", 119.5)]
        public void Run_Selection(string select, double expectedX)
        {
            using (var image = StripeImage())
            {
                var result = EdgeMeasurePipeline.Run(image, Setup("all", select));
                Assert.AreEqual(7, result.Points.Count);
                foreach (var p in result.Points)
                    Assert.AreEqual(expectedX, p.X, 0.5);
            }
        }

        [TestMethod]
        public void Run_Second_WithSingleEdge_SkipsRect()
        {
            using (var image = VerticalStepImage(W, H, 100))
            {
                var result = EdgeMeasurePipeline.Run(image, Setup("all", "second"));
                Assert.AreEqual(0, result.Points.Count, "只有一条边时取不到第 2 条，不能越界");
            }
        }

        [TestMethod]
        public void Run_ThresholdAboveContrast_FindsNothing()
        {
            using (var dark = ConstImage(W, H, 100))
            using (var bright = Rectangle1(0, 100, H - 1, W - 1))
            using (var image = Paint(dark, bright, 110))
            {
                Assert.AreEqual(0, EdgeMeasurePipeline.Run(image, Setup("all", "first", threshold: 30)).Points.Count);
                Assert.AreEqual(7, EdgeMeasurePipeline.Run(image, Setup("all", "first", threshold: 2)).Points.Count);
            }
        }

        [TestMethod]
        public void Run_Phi90_StepsAlongColumns()
        {
            // phi=90°：测量方向沿行，步进方向沿列。水平阶跃边在行 50。
            using (var dark = ConstImage(W, H, 0))
            using (var bottom = Rectangle1(50, 0, H - 1, W - 1))
            using (var image = Paint(dark, bottom, 255))
            {
                var result = EdgeMeasurePipeline.Run(image, Setup("all", "first", phiDeg: 90));

                Assert.AreEqual(7, result.RectCenters.Count);
                Assert.IsTrue(result.RectCenters.All(c => Math.Abs(c.Y - 50) < 1e-9));
                CollectionAssert.AreEquivalent(new[] { 70.0, 80, 90, 100, 110, 120, 130 },
                    result.RectCenters.Select(c => Math.Round(c.X, 6)).ToArray());

                Assert.AreEqual(7, result.Points.Count);
                foreach (var p in result.Points)
                    Assert.AreEqual(49.5, p.Y, 0.5);
            }
        }
    }
}
