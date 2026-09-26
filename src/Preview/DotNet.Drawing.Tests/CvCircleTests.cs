using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNet.Drawing.Tests
{
    [TestClass]
    public class CvCircleTests
    {
        [TestMethod]
        public void Constructors()
        {
            var a = new CvCircle(1, 2, 3);
            Geom.AreClose(1, 2, a.Center);
            Geom.AreClose(3, a.Radius);
            Assert.IsTrue(a.IsFullCircle);
            Assert.IsFalse(a.IsArc);

            var b = new CvCircle(new Point2d(0, 0), new Point2d(3, 4));
            Geom.AreClose(5, b.Radius);

            var arc = new CvCircle(0, 0, 2, 0, Math.PI / 2);
            Assert.IsTrue(arc.IsArc);
            Geom.AreClose(Math.PI / 2, arc.ArcSpan);
            Geom.AreClose(Math.PI, arc.ArcLength);
            Geom.AreClose(2, 0, arc.StartPoint);
            Geom.AreClose(0, 2, arc.EndPoint);

            Assert.IsTrue(new CvCircle(0, 0, 1, 0, 2 * Math.PI).IsFullCircle);
        }

        [TestMethod]
        public void NegativeRadius_IsRejected_IncludingWithExpression()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new CvCircle(0, 0, -1));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new CvCircle(Point2d.Zero, -1, 0, 1));
            var c = new CvCircle(0, 0, 1);
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => c with { Radius = -1 });
        }

        [TestMethod]
        public void Measurements()
        {
            var c = new CvCircle(0, 0, 2);
            Geom.AreClose(4, c.Diameter);
            Geom.AreClose(4 * Math.PI, c.Circumference);
            Geom.AreClose(4 * Math.PI, c.Area);
            Assert.IsFalse(c.IsDegenerate);
            Assert.IsTrue(new CvCircle(0, 0, 0).IsDegenerate);
        }

        [TestMethod]
        public void BoundingBox_FullCircle()
        {
            Geom.AreClose(-1, 0, 4, 4, new CvCircle(1, 2, 2).BoundingBox);
        }

        [TestMethod]
        public void BoundingBox_SmallArc_ExcludesCenter()
        {
            // π/6..π/3 的弧只在第一象限：包围盒 x/y 均为 [5, 8.66]，不含圆心 (0,0)
            var arc = new CvCircle(0, 0, 10, Math.PI / 6, Math.PI / 3);
            double c = 10 * Math.Cos(Math.PI / 6);
            Geom.AreClose(5, 5, c - 5, c - 5, arc.BoundingBox, 1e-9);
        }

        [TestMethod]
        public void BoundingBox_ArcAcrossZero_IncludesExtremePoint()
        {
            var arc = new CvCircle(0, 0, 10, -Math.PI / 4, Math.PI / 4);
            var box = arc.BoundingBox;
            double c = 10 * Math.Cos(Math.PI / 4);
            Geom.AreClose(c, box.Left);
            Geom.AreClose(10, box.Right, "跨越 0 弧度时要包含 (r, 0) 这个极值点");
            Geom.AreClose(-c, box.Top);
            Geom.AreClose(c, box.Bottom);
        }

        [TestMethod]
        public void SamplePoints_FullCircle_DoesNotRepeatStart()
        {
            var pts = new CvCircle(0, 0, 1).SamplePoints(4);
            Assert.AreEqual(4, pts.Length);
            Geom.AreClose(1, 0, pts[0]);
            Geom.AreClose(0, 1, pts[1]);
            Geom.AreClose(-1, 0, pts[2]);
            Geom.AreClose(0, -1, pts[3]);
        }

        [TestMethod]
        public void SamplePoints_Arc_IncludesBothEnds()
        {
            var pts = new CvCircle(0, 0, 1, 0, Math.PI).SamplePoints(3);
            Geom.AreClose(1, 0, pts[0]);
            Geom.AreClose(0, 1, pts[1]);
            Geom.AreClose(-1, 0, pts[2]);

            var one = new CvCircle(0, 0, 1, Math.PI / 2, Math.PI).SamplePoints(1);
            Assert.AreEqual(1, one.Length);
            Geom.AreClose(0, 1, one[0]);

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new CvCircle(0, 0, 1).SamplePoints(0));
        }

        [TestMethod]
        public void FromThreePoints()
        {
            var c = CvCircle.FromThreePoints(new Point2d(0, 0), new Point2d(2, 0), new Point2d(1, 1));
            Assert.IsNotNull(c);
            Geom.AreClose(1, 0, c!.Center);
            Geom.AreClose(1, c.Radius);

            Assert.IsNull(CvCircle.FromThreePoints(new Point2d(0, 0), new Point2d(1, 1), new Point2d(3, 3)));
            Assert.IsNull(CvCircle.FromThreePoints(new Point2d(1, 1), new Point2d(1, 1), new Point2d(5, 2)));
        }

        [TestMethod]
        public void FromThreePoints_IsTranslationInvariant()
        {
            // 以 p1 为局部原点计算：整体平移到 1e6 量级后结果不应劣化
            const double o = 1e6;
            var c = CvCircle.FromThreePoints(new Point2d(o, o), new Point2d(o + 2, o), new Point2d(o + 1, o + 1));
            Assert.IsNotNull(c);
            Geom.AreClose(o + 1, o, c!.Center, 1e-6);
            Assert.AreEqual(1, c.Radius, 1e-6);

            Assert.IsNull(CvCircle.FromThreePoints(new Point2d(o, o), new Point2d(o + 1, o + 1), new Point2d(o + 3, o + 3)));
        }

        [TestMethod]
        public void Containment_FullCircle()
        {
            var c = new CvCircle(0, 0, 5);
            Assert.IsTrue(c.Contains(new Point2d(3, 4)));
            Assert.IsTrue(c.Contains(Point2d.Zero));
            Assert.IsFalse(c.Contains(new Point2d(4, 4)));
            Assert.IsTrue(c.IsOnCircumference(new Point2d(3, 4)));
            Assert.IsFalse(c.IsOnCircumference(new Point2d(1, 1)));
            Assert.IsTrue(c.IsOnBoundary(new Point2d(0, -5)));
        }

        [TestMethod]
        public void Containment_Arc_ChecksAngleRange()
        {
            var arc = new CvCircle(0, 0, 5, 0, Math.PI / 2);
            Assert.IsTrue(arc.Contains(new Point2d(1, 1)));
            Assert.IsFalse(arc.Contains(new Point2d(-1, 1)));
            Assert.IsTrue(arc.IsOnCircumference(new Point2d(3, 4)));
            Assert.IsFalse(arc.IsOnCircumference(new Point2d(-3, 4)));
        }

        [TestMethod]
        public void DistanceToPoint()
        {
            var c = new CvCircle(0, 0, 5);
            Geom.AreClose(5, c.DistanceToPoint(new Point2d(6, 8)));
            Geom.AreClose(5, c.DistanceToPoint(Point2d.Zero));

            var arc = new CvCircle(0, 0, 5, 0, Math.PI / 2);
            Geom.AreClose(1, arc.DistanceToPoint(new Point2d(0, 6)));
            // (0,-5) 在圆周上但不在弧的角度范围内：取到最近端点 (5,0) 的距离
            Geom.AreClose(Math.Sqrt(50), arc.DistanceToPoint(new Point2d(0, -5)));
        }

        [TestMethod]
        public void Transforms()
        {
            var arc = new CvCircle(1, 1, 2, 0, Math.PI / 2);

            var moved = arc.Translate(3, 4);
            Geom.AreClose(4, 5, moved.Center);
            Geom.AreClose(Math.PI / 2, moved.EndPhi);
            Geom.AreClose(4, 5, arc.Translate(new Point2d(3, 4)).Center);

            var scaled = arc.Scale(3);
            Geom.AreClose(1, 1, scaled.Center, Geom.Eps);   // 以自身中心缩放
            Geom.AreClose(6, scaled.Radius);
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => arc.Scale(-1));

            var rotated = arc.Rotate(Math.PI);
            Geom.AreClose(1, 1, rotated.Center);
            Geom.AreClose(Math.PI, rotated.StartPhi);
            Geom.AreClose(1.5 * Math.PI, rotated.EndPhi);

            var around = arc.RotateAround(Math.PI / 2, Point2d.Zero);
            Geom.AreClose(-1, 1, around.Center);
            Geom.AreClose(Math.PI / 2, around.StartPhi);

            var reversed = arc.ReverseArc();
            Geom.AreClose(Math.PI / 2, reversed.StartPhi);
            Geom.AreClose(0, reversed.EndPhi);

            Assert.IsTrue(arc.ToFullCircle().IsFullCircle);
        }

        [TestMethod]
        public void PointHelpers()
        {
            var arc = new CvCircle(0, 0, 2, 0, Math.PI);
            Geom.AreClose(0, 2, arc.PointAtAngle(Math.PI / 2));
            Geom.AreClose(0, 2, arc.PointAt(0.5));
            Geom.AreClose(Math.PI / 2, arc.AngleOfPoint(new Point2d(0, 7)));
        }

        [TestMethod]
        public void Equality()
        {
            var a = new CvCircle(0, 0, 5);
            var b = new CvCircle(0.004, 0, 5.004);
            Assert.AreEqual(a, b);
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
            Assert.AreNotEqual(a, new CvCircle(0, 0, 5.1));
            Assert.AreNotEqual(a, new CvCircle(0, 0, 5, 0, 1));
            Geom.AreClose(0, 0, CvCircle.Unit.Center);
            Geom.AreClose(1, CvCircle.Unit.Radius);
        }
    }
}
