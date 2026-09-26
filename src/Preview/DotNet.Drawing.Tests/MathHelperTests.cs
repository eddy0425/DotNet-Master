using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNet.Drawing.Tests
{
    [TestClass]
    public class MathHelperTests
    {
        #region 角度规范化

        [DataTestMethod]
        [DataRow(0.5, 0.5)]
        [DataRow(Math.PI, -Math.PI)]            // 区间 [-π, π) 右开：π 落到 -π
        [DataRow(-Math.PI, -Math.PI)]
        [DataRow(3 * Math.PI, -Math.PI)]
        [DataRow(-3 * Math.PI / 2, Math.PI / 2)]
        [DataRow(7.0, 7.0 - 2 * Math.PI)]
        public void NormalizeAngle_MapsIntoHalfOpenRange(double input, double expected)
        {
            Geom.AreClose(expected, MathHelper.NormalizeAngle(input));
        }

        [TestMethod]
        public void NormalizeAngle_HugeInput_ReturnsWithoutLooping()
        {
            // 旧的循环加减实现对这类输入会迭代上亿次
            double r = MathHelper.NormalizeAngle(1e18);
            Assert.IsTrue(r >= -Math.PI && r < Math.PI, r.ToString());
        }

        [TestMethod]
        public void NormalizeAngle_NaNAndInfinity_PassThrough()
        {
            Assert.IsTrue(double.IsNaN(MathHelper.NormalizeAngle(double.NaN)));
            Assert.IsTrue(double.IsPositiveInfinity(MathHelper.NormalizeAngle(double.PositiveInfinity)));
            Assert.IsTrue(double.IsNaN(MathHelper.NormalizeAnglePositive(double.NaN)));
        }

        [DataTestMethod]
        [DataRow(0.0, 0.0)]
        [DataRow(2 * Math.PI, 0.0)]
        [DataRow(-Math.PI / 2, 3 * Math.PI / 2)]
        [DataRow(5 * Math.PI, Math.PI)]
        public void NormalizeAnglePositive_MapsIntoZeroToTwoPi(double input, double expected)
        {
            Geom.AreClose(expected, MathHelper.NormalizeAnglePositive(input));
        }

        [DataTestMethod]
        [DataRow(180.0, -180.0)]
        [DataRow(540.0, -180.0)]
        [DataRow(-190.0, 170.0)]
        [DataRow(45.0, 45.0)]
        public void NormalizeAngleDegrees_MapsIntoHalfOpenRange(double input, double expected)
        {
            Geom.AreClose(expected, MathHelper.NormalizeAngleDegrees(input));
        }

        [DataTestMethod]
        [DataRow(-30.0, 330.0)]
        [DataRow(360.0, 0.0)]
        [DataRow(725.0, 5.0)]
        public void NormalizeAngleDegreesPositive_MapsIntoZeroTo360(double input, double expected)
        {
            Geom.AreClose(expected, MathHelper.NormalizeAngleDegreesPositive(input));
        }

        [TestMethod]
        public void AngleDifference_TakesShortestWayAcrossWrap()
        {
            Geom.AreClose(-0.2, MathHelper.AngleDifference(0.1, -0.1));
            Geom.AreClose(0.2, MathHelper.AngleDifference(Math.PI - 0.1, -Math.PI + 0.1));
            Geom.AreClose(20.0, MathHelper.AngleDifferenceDegrees(170, -170));
        }

        [TestMethod]
        public void DegreeRadianConversion_RoundTrips()
        {
            Geom.AreClose(Math.PI, MathHelper.ToRadians(180));
            Geom.AreClose(90.0, MathHelper.ToDegrees(Math.PI / 2));
            Geom.AreClose(12.34, MathHelper.ToDegrees(MathHelper.ToRadians(12.34)));
        }

        #endregion

        #region 容差体系

        [TestMethod]
        public void AreEqualGeometric_UsesPixelGrid()
        {
            Assert.IsTrue(MathHelper.AreEqualGeometric(1.0, 1.004));
            Assert.IsFalse(MathHelper.AreEqualGeometric(1.0, 1.006));
            Assert.IsFalse(MathHelper.AreEqualGeometric(double.NaN, double.NaN));
            Assert.IsTrue(MathHelper.IsZeroGeometric(0.004));
            Assert.IsFalse(MathHelper.IsZeroGeometric(0.02));
        }

        [TestMethod]
        public void AreEqualGeometric_ImpliesEqualQuantizedHash()
        {
            // Equals/GetHashCode 契约：判等为真时量化值必须相同
            var rnd = new Random(12345);
            for (int i = 0; i < 10000; i++)
            {
                double a = rnd.NextDouble() * 100;
                double b = a + (rnd.NextDouble() - 0.5) * 0.03;
                if (MathHelper.AreEqualGeometric(a, b))
                    Assert.AreEqual(MathHelper.QuantizeGeometric(a), MathHelper.QuantizeGeometric(b), $"{a} vs {b}");
            }
        }

        [TestMethod]
        public void AreEqualRelative_ScalesWithMagnitude()
        {
            // 1e7 量级（坐标平方）上差 1 仍视为相等；绝对容差的 AreEqual 判不出来
            Assert.IsTrue(MathHelper.AreEqualRelative(1e7, 1e7 + 1));
            Assert.IsFalse(MathHelper.AreEqual(1e7, 1e7 + 1));
            // 接近 0 时退化为绝对容差
            Assert.IsTrue(MathHelper.AreEqualRelative(0, 1e-7));
            Assert.IsFalse(MathHelper.AreEqualRelative(0, 1e-5));
        }

        [TestMethod]
        public void IsZeroRelative_UsesGivenScale()
        {
            Assert.IsTrue(MathHelper.IsZeroRelative(5, 1e7));
            Assert.IsFalse(MathHelper.IsZeroRelative(5, 1));
        }

        [TestMethod]
        public void StrictToleranceHelpers()
        {
            Assert.IsTrue(MathHelper.AreEqual(1.0, 1.0 + 1e-12));
            Assert.IsFalse(MathHelper.AreEqual(1.0, 1.0 + 1e-6));
            Assert.IsTrue(MathHelper.AreEqual(1.0, 1.05, 0.1));
            Assert.IsTrue(MathHelper.IsZero(1e-12));
            Assert.IsFalse(MathHelper.IsZero(1e-3));
            Assert.IsTrue(MathHelper.IsZero(1e-3, 1e-2));
            Assert.IsTrue(MathHelper.IsPositive(1e-6));
            Assert.IsFalse(MathHelper.IsPositive(1e-12));
            Assert.IsTrue(MathHelper.IsNegative(-1e-6));
            Assert.AreEqual(0, MathHelper.Sign(1e-12));
            Assert.AreEqual(-1, MathHelper.Sign(-2));
            Assert.AreEqual(1, MathHelper.Sign(3));
        }

        [TestMethod]
        public void QuantizeToTolerance_NonFiniteAndNonPositiveTolerance_PassThrough()
        {
            Assert.IsTrue(double.IsNaN(MathHelper.QuantizeToTolerance(double.NaN)));
            Assert.AreEqual(1.23456, MathHelper.QuantizeToTolerance(1.23456, 0));
            Assert.AreEqual(MathHelper.TolerantHash(1.0), MathHelper.TolerantHash(1.0 + 1e-12));
        }

        #endregion

        #region 取整 / 插值 / 几何

        [TestMethod]
        public void ClampFamily()
        {
            Assert.AreEqual(0.0, MathHelper.Clamp(-1.0, 0.0, 10.0));
            Assert.AreEqual(10.0, MathHelper.Clamp(11.0, 0.0, 10.0));
            Assert.AreEqual(5, MathHelper.Clamp(5, 0, 10));
            Assert.AreEqual(0, MathHelper.Clamp(-5, 0, 10));
            Assert.AreEqual(1.0, MathHelper.Clamp01(3.0));
            Assert.AreEqual(0.0, MathHelper.Clamp01(-3.0));
        }

        [TestMethod]
        public void RoundingIsAwayFromZero()
        {
            Assert.AreEqual(0.13, MathHelper.Round(0.125, 2));
            Assert.AreEqual(3, MathHelper.RoundToPixel(2.5));
            Assert.AreEqual(-3, MathHelper.RoundToPixel(-2.5));
            Assert.AreEqual(3, MathHelper.CeilToPixel(2.1));
            Assert.AreEqual(2, MathHelper.FloorToPixel(2.9));
        }

        [TestMethod]
        public void Interpolation()
        {
            Geom.AreClose(2.5, MathHelper.Lerp(0, 10, 0.25));
            Geom.AreClose(0.25, MathHelper.InverseLerp(0, 10, 2.5));
            Assert.AreEqual(0.0, MathHelper.InverseLerp(3, 3, 100), "退化区间返回 0 而不是除零");
            Geom.AreClose(150, MathHelper.Map(5, 0, 10, 100, 200));
        }

        [TestMethod]
        public void SmoothStepBetween_ReturnsUnitWeight()
        {
            Geom.AreClose(0.5, MathHelper.SmoothStepBetween(0, 1, 0.5));
            Assert.AreEqual(0.0, MathHelper.SmoothStepBetween(10, 20, 5));
            Assert.AreEqual(1.0, MathHelper.SmoothStepBetween(10, 20, 25));
            Assert.AreEqual(0.0, MathHelper.SmoothStepBetween(3, 3, 2));
            Assert.AreEqual(1.0, MathHelper.SmoothStepBetween(3, 3, 3));
        }

        [TestMethod]
        public void VectorHelpers()
        {
            Geom.AreClose(5, MathHelper.Distance(0, 0, 3, 4));
            Geom.AreClose(25, MathHelper.DistanceSquared(0, 0, 3, 4));
            Geom.AreClose(5, MathHelper.Magnitude(3, 4));
            Geom.AreClose(25, MathHelper.MagnitudeSquared(3, 4));
            Geom.AreClose(11, MathHelper.Dot(1, 2, 3, 4));
            Geom.AreClose(-2, MathHelper.Cross(1, 2, 3, 4));
            Assert.AreEqual(3.0, MathHelper.Max(1, 3, 2));
            Assert.AreEqual(1.0, MathHelper.Min(2, 1, 3));
        }

        #endregion
    }
}
