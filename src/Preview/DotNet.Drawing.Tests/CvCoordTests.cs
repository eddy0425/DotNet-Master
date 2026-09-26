using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace DotNet.Drawing.Tests
{
    [TestClass]
    public class CvCoordTests
    {
        private static void AreClose(double x, double y, double radians, CvCoord actual, double eps = Geom.Eps)
        {
            Assert.AreEqual(x, actual.X, eps, $"X of {actual}");
            Assert.AreEqual(y, actual.Y, eps, $"Y of {actual}");
            Assert.AreEqual(radians, actual.Angle.Radians, eps, $"Angle of {actual}");
        }

        [TestMethod]
        public void Constructor_NormalizesAngle()
        {
            AreClose(1, 2, -Math.PI / 2, CvCoord.FromDegrees(1, 2, 270));
            AreClose(0, 0, -Math.PI, CvCoord.FromDegrees(0, 0, 180));
            AreClose(0, 0, -Math.PI, CvCoord.FromRadians(0, 0, 3 * Math.PI));
            AreClose(3, 4, 0.5, new CvCoord(new Point2d(3, 4), Angle.FromRadians(0.5)));
            AreClose(3, 4, 0, new CvCoord(3, 4));
        }

        [TestMethod]
        public void DerivedProperties()
        {
            var c = CvCoord.FromDegrees(1, 2, 90);
            Geom.AreClose(90, c.AngleDegrees);
            Geom.AreClose(1, 2, c.Center);
            Geom.AreClose(0, 1, c.Direction);
            Assert.IsTrue(CvCoord.Identity.IsIdentity);
            Assert.IsTrue(CvCoord.Zero.IsIdentity);
            Assert.IsFalse(c.IsIdentity);
        }

        [TestMethod]
        public void DirectionalTranslations()
        {
            var c = CvCoord.FromDegrees(1, 1, 90);
            AreClose(1, 3, Math.PI / 2, c.TranslateForward(2));
            AreClose(-1, 1, Math.PI / 2, c.TranslateSideways(2), 1e-9);   // 侧向 = 朝向 +90°
        }

        [TestMethod]
        public void WorldLocal_RoundTrip()
        {
            var frame = CvCoord.FromDegrees(10, 20, 30);
            var world = new Point2d(-7.5, 42.25);
            Geom.AreClose(world, frame.LocalToWorld(frame.WorldToLocal(world)), 1e-9);

            var f90 = CvCoord.FromDegrees(1, 1, 90);
            Geom.AreClose(1, 2, f90.LocalToWorld(new Point2d(1, 0)));
            Geom.AreClose(1, 0, f90.WorldToLocal(new Point2d(1, 2)));
        }

        [TestMethod]
        public void Compose_WithInverse_IsIdentity()
        {
            var frame = CvCoord.FromDegrees(10, -20, 135);
            var left = frame.Compose(frame.Inverse);
            var right = frame.Inverse.Compose(frame);
            AreClose(0, 0, 0, left, 1e-9);
            AreClose(0, 0, 0, right, 1e-9);
            Assert.IsTrue(left.IsIdentity);
        }

        [TestMethod]
        public void Compose_ChainsTransforms()
        {
            var a = CvCoord.FromDegrees(10, 0, 90);
            var b = CvCoord.FromDegrees(5, 0, 90);
            var ab = a.Compose(b);
            AreClose(10, 5, -Math.PI, ab);

            var p = new Point2d(1, 2);
            Geom.AreClose(a.LocalToWorld(b.LocalToWorld(p)), ab.LocalToWorld(p), 1e-9);
        }

        [TestMethod]
        public void DistanceAndAngleDifference()
        {
            var a = CvCoord.FromDegrees(0, 0, 170);
            var b = CvCoord.FromDegrees(3, 4, -170);
            Geom.AreClose(5, a.DistanceTo(b));
            Geom.AreClose(20, a.AngleDifferenceTo(b).Degrees);
        }

        [TestMethod]
        public void Lerp_TakesShortestAngle()
        {
            var a = CvCoord.FromDegrees(0, 0, 170);
            var b = CvCoord.FromDegrees(10, 20, -170);
            var mid = a.Lerp(b, 0.5);
            Geom.AreClose(5, mid.X);
            Geom.AreClose(10, mid.Y);
            // 走 170° → 180° 的短路径，而不是经过 0°；规范化后落在 -π
            Assert.AreEqual(Math.PI, Math.Abs(mid.Angle.Radians), 1e-9);
        }

        [TestMethod]
        public void Equality()
        {
            var a = CvCoord.FromRadians(1, 2, 0.5);
            var b = CvCoord.FromRadians(1.004, 2, 0.5 + 1e-12);
            Assert.IsTrue(a == b);
            Assert.IsTrue(a.Equals((object)b));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
            Assert.IsTrue(a != CvCoord.FromRadians(1, 2, 0.5001));
            Assert.IsFalse(a.Equals("x"));
            // π 与 -π 经构造规范化后是同一个朝向
            Assert.AreEqual(CvCoord.FromRadians(0, 0, Math.PI), CvCoord.FromRadians(0, 0, -Math.PI));
        }

        [TestMethod]
        public void Json_RoundTrips()
        {
            var c = CvCoord.FromRadians(1.5, -2, 0.75);
            string json = JsonConvert.SerializeObject(c);
            Assert.AreEqual("{\"X\":1.5,\"Y\":-2.0,\"Angle\":0.75}", json);
            AreClose(1.5, -2, 0.75, JsonConvert.DeserializeObject<CvCoord>(json));
        }
    }
}
