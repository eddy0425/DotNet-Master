using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace DotNet.Drawing.Tests
{
    [TestClass]
    public class Point2dTests
    {
        [TestMethod]
        public void DerivedProperties()
        {
            var p = new Point2d(3, 4);
            Geom.AreClose(5, p.Magnitude);
            Geom.AreClose(Math.Atan2(4, 3), p.Angle);
            Geom.AreClose(45, new Point2d(1, 1).AngleDegrees);
            Assert.IsTrue(new Point2d(0.004, -0.004).IsZero);
            Assert.IsFalse(new Point2d(0.02, 0).IsZero);
        }

        [TestMethod]
        public void Distance()
        {
            var a = new Point2d(1, 1);
            var b = new Point2d(4, 5);
            Geom.AreClose(5, a.DistanceTo(b));
            Geom.AreClose(25, a.DistanceSquaredTo(b));
            Geom.AreClose(5, Point2d.Distance(a, b));
            Geom.AreClose(25, Point2d.DistanceSquared(a, b));
        }

        [TestMethod]
        public void Transforms_AreRelativeToOrigin()
        {
            var p = new Point2d(2, 1);
            Geom.AreClose(5, 3, p.Translate(3, 2));
            Geom.AreClose(5, 3, p.Translate(new Point2d(3, 2)));
            Geom.AreClose(4, 2, p.Scale(2));                       // 向量语义：相对原点
            Geom.AreClose(-1, 2, p.Rotate(Math.PI / 2));           // 绕原点
            Geom.AreClose(0, 1, p.RotateAround(Math.PI, new Point2d(1, 1)));
        }

        [TestMethod]
        public void Normalized()
        {
            Geom.AreClose(0.6, 0.8, new Point2d(3, 4).Normalized);
            Assert.AreEqual(Point2d.Zero, Point2d.Zero.Normalized, "零向量归一化返回零而不是 NaN");
        }

        [TestMethod]
        public void VectorOperations()
        {
            var a = new Point2d(1, 2);
            var b = new Point2d(3, 4);
            Geom.AreClose(11, a.Dot(b));
            Geom.AreClose(-2, a.Cross(b));
            Geom.AreClose(2, 3, a.Lerp(b, 0.5));
            Geom.AreClose(4, 6, a + b);
            Geom.AreClose(-2, -2, a - b);
            Geom.AreClose(2, 4, a * 2);
            Geom.AreClose(2, 4, 2 * a);
            Geom.AreClose(0.5, 1, a / 2);
            Geom.AreClose(-1, -2, -a);
        }

        [TestMethod]
        public void Divide_OnlyExactZeroThrows()
        {
            Assert.ThrowsException<DivideByZeroException>(() => new Point2d(1, 1) / 0);
            Geom.AreClose(1e10, 2e10, new Point2d(1, 2) / 1e-10, 1);
        }

        [TestMethod]
        public void Equality_UsesPixelGrid()
        {
            var a = new Point2d(1, 1);
            var near = new Point2d(1.004, 1);
            Assert.IsTrue(a == near);
            Assert.IsTrue(a.Equals((object)near));
            Assert.AreEqual(a.GetHashCode(), near.GetHashCode());
            Assert.IsTrue(a != new Point2d(1.006, 1));
            Assert.IsFalse(a.Equals("(1, 1)"));

            var set = new HashSet<Point2d> { a };
            Assert.IsTrue(set.Contains(near), "判等为真的两点必须落入同一哈希桶");
        }

        [TestMethod]
        public void StaticMembers()
        {
            Geom.AreClose(0, 0, Point2d.Zero);
            Geom.AreClose(1, 0, Point2d.UnitX);
            Geom.AreClose(0, 1, Point2d.UnitY);
            Geom.AreClose(1, 1, Point2d.One);
            Geom.AreClose(0, 2, Point2d.FromPolar(2, Math.PI / 2));
        }

        [TestMethod]
        public void Centroid()
        {
            var pts = new[] { new Point2d(0, 0), new Point2d(4, 0), new Point2d(4, 2), new Point2d(0, 2) };
            Geom.AreClose(2, 1, Point2d.Centroid(pts));
            Geom.AreClose(2, 1, Point2d.Centroid(new List<Point2d>(pts)));
            Assert.AreEqual(Point2d.Zero, Point2d.Centroid(new Point2d[0]));
            Assert.AreEqual(Point2d.Zero, Point2d.Centroid((Point2d[])null!));
            Assert.AreEqual(Point2d.Zero, Point2d.Centroid((IReadOnlyList<Point2d>)new List<Point2d>()));
        }

        [TestMethod]
        public void Formatting()
        {
            Assert.AreEqual("(1.5, 2)", new Point2d(1.5, 2).ToString());
            Assert.AreEqual("(1.50, 2.00)", new Point2d(1.5, 2).ToString("F2"));
        }

        [TestMethod]
        public void Json_RoundTripsOnlyCoordinates()
        {
            string json = JsonConvert.SerializeObject(new Point2d(1.5, -2));
            Assert.AreEqual("{\"X\":1.5,\"Y\":-2.0}", json);
            var back = JsonConvert.DeserializeObject<Point2d>(json);
            Geom.AreClose(1.5, -2, back);
        }
    }
}
