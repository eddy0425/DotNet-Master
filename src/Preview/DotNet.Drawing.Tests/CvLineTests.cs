using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNet.Drawing.Tests
{
    [TestClass]
    public class CvLineTests
    {
        private static readonly CvLine Horizontal = new CvLine(0, 0, 10, 0);

        [TestMethod]
        public void DerivedProperties()
        {
            var line = new CvLine(1, 1, 4, 5);
            Geom.AreClose(5, line.Length);
            Geom.AreClose(25, line.LengthSquared);
            Geom.AreClose(Math.Atan2(4, 3), line.Angle);
            Geom.AreClose(2.5, 3, line.MidPoint);
            Geom.AreClose(3, 4, line.Direction);
            Geom.AreClose(0.6, 0.8, line.UnitDirection);
            Geom.AreClose(-0.8, 0.6, line.Normal);
            Geom.AreClose(90, new CvLine(0, 0, 0, 1).AngleDegrees);
        }

        [TestMethod]
        public void IsDegenerate_UsesPixelTolerance()
        {
            Assert.IsTrue(new CvLine(0, 0, 0.005, 0).IsDegenerate);
            Assert.IsFalse(new CvLine(0, 0, 0.02, 0).IsDegenerate);
        }

        [TestMethod]
        public void Factories()
        {
            var a = CvLine.FromAngle(new Point2d(1, 1), Math.PI / 2, 4);
            Geom.AreClose(1, 1, a.Start);
            Geom.AreClose(1, 5, a.End);

            var c = CvLine.FromCenterAngle(new Point2d(5, 5), 0, 4);
            Geom.AreClose(3, 5, c.Start);
            Geom.AreClose(7, 5, c.End);
        }

        [TestMethod]
        public void Containment()
        {
            Assert.IsTrue(Horizontal.ContainsPoint(new Point2d(5, 0)));
            Assert.IsTrue(Horizontal.ContainsPoint(new Point2d(0, 0)));
            Assert.IsFalse(Horizontal.ContainsPoint(new Point2d(5, 1)));
            Assert.IsFalse(Horizontal.ContainsPoint(new Point2d(11, 0)), "延长线上的点不在线段上");
            Assert.IsTrue(Horizontal.ContainsPoint(new Point2d(5, 0.5), 0.1));
            Assert.IsTrue(Horizontal.ContainsPoint(new Point2d(3, 0)));
        }

        [TestMethod]
        public void DistanceToPoint_ClampsToSegment()
        {
            Geom.AreClose(3, Horizontal.DistanceToPoint(new Point2d(5, 3)));
            Geom.AreClose(5, Horizontal.DistanceToPoint(new Point2d(13, 4)), "超出端点时取到端点的距离");
            Geom.AreClose(10, 0, Horizontal.ClosestPointTo(new Point2d(13, 4)));
            Geom.AreClose(5, 0, Horizontal.ClosestPointTo(new Point2d(5, -7)));
        }

        [TestMethod]
        public void DegenerateLine_FallsBackToStart()
        {
            var dot = new CvLine(2, 2, 2, 2);
            Geom.AreClose(5, dot.DistanceToPoint(new Point2d(5, 6)));
            Geom.AreClose(2, 2, dot.ClosestPointTo(new Point2d(5, 6)));
            Assert.AreEqual(0.0, dot.ProjectPoint(new Point2d(5, 6)));
        }

        [TestMethod]
        public void ProjectPoint_IsUnclamped()
        {
            Geom.AreClose(0.5, Horizontal.ProjectPoint(new Point2d(5, 9)));
            Geom.AreClose(2, Horizontal.ProjectPoint(new Point2d(20, 0)));
            Geom.AreClose(-1, Horizontal.ProjectPoint(new Point2d(-10, 3)));
        }

        [TestMethod]
        public void ReverseExtendPointAt()
        {
            var r = Horizontal.Reverse();
            Geom.AreClose(10, 0, r.Start);
            Geom.AreClose(0, 0, r.End);

            var e = Horizontal.Extend(1, 2);
            Geom.AreClose(-1, 0, e.Start);
            Geom.AreClose(12, 0, e.End);

            Geom.AreClose(2.5, 0, Horizontal.PointAt(0.25));
            Geom.AreClose(15, 0, Horizontal.PointAt(1.5));
        }

        [TestMethod]
        public void TryIntersect_Segments()
        {
            var cross = new CvLine(0, 10, 10, 0);
            Assert.IsTrue(new CvLine(0, 0, 10, 10).TryIntersect(cross, out var p));
            Geom.AreClose(5, 5, p);

            // 共享端点
            Assert.IsTrue(Horizontal.TryIntersect(new CvLine(10, 0, 10, 5), out p));
            Geom.AreClose(10, 0, p);

            // 直线相交但交点不在两条线段上
            Assert.IsFalse(Horizontal.TryIntersect(new CvLine(20, -5, 20, 5), out _));

            // 平行
            Assert.IsFalse(Horizontal.TryIntersect(new CvLine(0, 1, 10, 1), out _));
        }

        [TestMethod]
        public void TryIntersect_LongNearlyParallelLines_AreParallel()
        {
            // 图像坐标量级（4000×3000）上几乎平行的两条长线：
            // 叉积绝对值远大于 1e-9，只有按长度做相对判零才能识别为平行。
            var a = new CvLine(0, 0, 4000, 3000);
            var b = new CvLine(0, 1, 4000, 3001.000001);
            Assert.IsFalse(a.TryIntersect(b, out _));
            Assert.IsFalse(a.TryIntersectLine(b, out _, out _));
        }

        [TestMethod]
        public void TryIntersectLine_UsesInfiniteLines()
        {
            var a = new CvLine(0, 0, 1, 0);
            var b = new CvLine(5, -1, 5, 1);
            Assert.IsTrue(a.TryIntersectLine(b, out var p, out double t));
            Geom.AreClose(5, 0, p);
            Geom.AreClose(5, t);
            Assert.IsFalse(a.TryIntersect(b, out _), "作为线段并不相交");
            Assert.IsFalse(a.TryIntersectLine(new CvLine(0, 3, 7, 3), out _, out _));
        }

        [TestMethod]
        public void Equality_IsToleranceBased()
        {
            var a = new CvLine(0, 0, 10, 0);
            var b = new CvLine(0.004, 0, 10, 0.004);
            Assert.AreEqual(a, b);
            Assert.IsTrue(a == b);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
            Assert.AreNotEqual(a, a.Reverse(), "线段有方向");
            Assert.IsFalse(a.Equals(null));
        }
    }
}
