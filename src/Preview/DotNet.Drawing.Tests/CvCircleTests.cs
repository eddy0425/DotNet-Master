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
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new CvCircle(default(Point2d), -1, 0, 1));
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
        public void Circumference_FullCircle()
        {
            var c = new CvCircle(0, 0, 5);
            Assert.IsTrue(c.IsOnCircumference(new Point2d(3, 4)));
            Assert.IsFalse(c.IsOnCircumference(new Point2d(1, 1)));
            Assert.IsTrue(c.IsOnCircumference(new Point2d(0, -5)));
        }

        [TestMethod]
        public void Circumference_Arc_ChecksAngleRange()
        {
            var arc = new CvCircle(0, 0, 5, 0, Math.PI / 2);
            Assert.IsTrue(arc.IsOnCircumference(new Point2d(3, 4)));
            Assert.IsFalse(arc.IsOnCircumference(new Point2d(-3, 4)));
        }

        [TestMethod]
        public void DistanceToPoint()
        {
            var c = new CvCircle(0, 0, 5);
            Geom.AreClose(5, c.DistanceToPoint(new Point2d(6, 8)));
            Geom.AreClose(5, c.DistanceToPoint(default(Point2d)));

            var arc = new CvCircle(0, 0, 5, 0, Math.PI / 2);
            Geom.AreClose(1, arc.DistanceToPoint(new Point2d(0, 6)));
            // (0,-5) 在圆周上但不在弧的角度范围内：取到最近端点 (5,0) 的距离
            Geom.AreClose(Math.Sqrt(50), arc.DistanceToPoint(new Point2d(0, -5)));
        }

        [TestMethod]
        public void ArcHelpers()
        {
            var arc = new CvCircle(1, 1, 2, 0, Math.PI / 2);
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
