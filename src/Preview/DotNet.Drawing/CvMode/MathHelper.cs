using System;
using System.Runtime.CompilerServices;

namespace DotNet.Drawing
{
    /// <summary>
    /// 数学辅助类，提供浮点数比较、角度转换等工具方法
    /// </summary>
    /// <remarks>
    /// 设计特点：
    /// - 静态类，无状态，线程安全
    /// - 使用 AggressiveInlining 优化性能关键路径
    /// - 提供常用的数学常量和工具方法
    /// </remarks>
    public static class MathHelper
    {
        #region Constants

        /// <summary>
        /// 浮点数比较容差（适用于像素级精度）
        /// </summary>
        public const double Tolerance = 1e-9;

        /// <summary>
        /// 较宽松的容差（适用于一般几何计算）
        /// </summary>
        public const double LooseTolerance = 1e-6;

        /// <summary>
        /// 像素级容差
        /// </summary>
        public const double PixelTolerance = 0.01;

        /// <summary>
        /// Pi 的两倍
        /// </summary>
        public const double TwoPi = 2 * Math.PI;

        /// <summary>
        /// Pi 的一半
        /// </summary>
        public const double HalfPi = Math.PI / 2;

        /// <summary>
        /// 弧度到度数的转换系数
        /// </summary>
        public const double RadToDeg = 180.0 / Math.PI;

        /// <summary>
        /// 度数到弧度的转换系数
        /// </summary>
        public const double DegToRad = Math.PI / 180.0;

        #endregion

        #region Equality Comparisons

        /// <summary>
        /// 判断两个浮点数是否近似相等
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreEqual(double a, double b)
        {
            return Math.Abs(a - b) < Tolerance;
        }

        /// <summary>
        /// 判断两个浮点数是否近似相等（自定义容差）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreEqual(double a, double b, double tolerance)
        {
            return Math.Abs(a - b) < tolerance;
        }

        /// <summary>
        /// 判断浮点数是否近似为零
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsZero(double value)
        {
            return Math.Abs(value) < Tolerance;
        }

        /// <summary>
        /// 判断浮点数是否近似为零（自定义容差）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsZero(double value, double tolerance)
        {
            return Math.Abs(value) < tolerance;
        }

        /// <summary>
        /// 判断浮点数是否为正数
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPositive(double value)
        {
            return value > Tolerance;
        }

        /// <summary>
        /// 判断浮点数是否为负数
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNegative(double value)
        {
            return value < -Tolerance;
        }

        /// <summary>
        /// 将浮点值量化到容差网格上，用于实现"容差版 GetHashCode"
        /// </summary>
        /// <remarks>
        /// 用法：当类型的 <c>Equals</c> 使用容差比较 (<see cref="AreEqual(double,double)"/>)
        /// 时，<c>GetHashCode</c> 必须保证 <c>x.Equals(y) ⇒ x.GetHashCode() == y.GetHashCode()</c>。
        /// 直接 <c>x.GetHashCode()</c> 会让 1e-15 的舍入误差产生不同哈希。
        /// 把值量化到容差网格 (默认 <see cref="Tolerance"/>) 后再哈希，可以让"近似相等"的值
        /// 在绝大多数情况下落入同一桶；唯一例外是恰好跨越桶边界 (e.g. 1.4999e-9 vs 1.5000e-9) 的极端值，
        /// 这是任何"容差等价 + 离散哈希"方案的固有限制。
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double QuantizeToTolerance(double value, double tolerance = Tolerance)
        {
            if (tolerance <= 0 || double.IsNaN(value) || double.IsInfinity(value)) return value;
            return Math.Round(value / tolerance) * tolerance;
        }

        /// <summary>
        /// 直接生成与容差版 <c>Equals</c> 兼容的哈希分量
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TolerantHash(double value, double tolerance = Tolerance)
        {
            return QuantizeToTolerance(value, tolerance).GetHashCode();
        }

        #endregion

        #region Angle Conversions

        /// <summary>
        /// 弧度转度数
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ToDegrees(double radians)
        {
            return radians * RadToDeg;
        }

        /// <summary>
        /// 度数转弧度
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ToRadians(double degrees)
        {
            return degrees * DegToRad;
        }

        /// <summary>
        /// 将角度规范化到 [-π, π) 范围
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double NormalizeAngle(double angle)
        {
            while (angle >= Math.PI) angle -= TwoPi;
            while (angle < -Math.PI) angle += TwoPi;
            return angle;
        }

        /// <summary>
        /// 将角度规范化到 [0, 2π) 范围
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double NormalizeAnglePositive(double angle)
        {
            while (angle >= TwoPi) angle -= TwoPi;
            while (angle < 0) angle += TwoPi;
            return angle;
        }

        /// <summary>
        /// 将角度规范化到 [-180, 180) 度数范围
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double NormalizeAngleDegrees(double degrees)
        {
            while (degrees >= 180) degrees -= 360;
            while (degrees < -180) degrees += 360;
            return degrees;
        }

        /// <summary>
        /// 计算两个角度之间的最短差值（弧度）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double AngleDifference(double from, double to)
        {
            return NormalizeAngle(to - from);
        }

        /// <summary>
        /// 计算两个角度之间的最短差值（度数）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double AngleDifferenceDegrees(double from, double to)
        {
            return NormalizeAngleDegrees(to - from);
        }

        #endregion

        #region Clamping and Rounding

        /// <summary>
        /// 将值限制在指定范围内
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// 将值限制在指定范围内（整数版本）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// 将值限制在 [0, 1] 范围内
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Clamp01(double value)
        {
            if (value < 0) return 0;
            if (value > 1) return 1;
            return value;
        }

        /// <summary>
        /// 四舍五入到指定小数位
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Round(double value, int decimals = 6)
        {
            return Math.Round(value, decimals, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// 向上取整到像素
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CeilToPixel(double value)
        {
            return (int)Math.Ceiling(value);
        }

        /// <summary>
        /// 向下取整到像素
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FloorToPixel(double value)
        {
            return (int)Math.Floor(value);
        }

        /// <summary>
        /// 四舍五入到像素
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RoundToPixel(double value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }

        #endregion

        #region Interpolation

        /// <summary>
        /// 线性插值
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Lerp(double a, double b, double t)
        {
            return a + (b - a) * t;
        }

        /// <summary>
        /// 反向线性插值（求参数 t）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double InverseLerp(double a, double b, double value)
        {
            if (AreEqual(a, b)) return 0;
            return (value - a) / (b - a);
        }

        /// <summary>
        /// 区间映射
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Map(double value, double fromMin, double fromMax, double toMin, double toMax)
        {
            double t = InverseLerp(fromMin, fromMax, value);
            return Lerp(toMin, toMax, t);
        }

        /// <summary>
        /// 平滑插值 (Smoothstep)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SmoothStep(double a, double b, double t)
        {
            t = Clamp01((t - a) / (b - a));
            return t * t * (3 - 2 * t);
        }

        #endregion

        #region Distance and Geometry

        /// <summary>
        /// 计算两点间的欧几里得距离
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Distance(double x1, double y1, double x2, double y2)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 计算两点间的距离平方（避免开方）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double DistanceSquared(double x1, double y1, double x2, double y2)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            return dx * dx + dy * dy;
        }

        /// <summary>
        /// 计算向量长度
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Magnitude(double x, double y)
        {
            return Math.Sqrt(x * x + y * y);
        }

        /// <summary>
        /// 计算向量长度平方
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double MagnitudeSquared(double x, double y)
        {
            return x * x + y * y;
        }

        /// <summary>
        /// 计算点积
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Dot(double x1, double y1, double x2, double y2)
        {
            return x1 * x2 + y1 * y2;
        }

        /// <summary>
        /// 计算叉积（二维空间返回标量）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Cross(double x1, double y1, double x2, double y2)
        {
            return x1 * y2 - y1 * x2;
        }

        #endregion

        #region Comparison

        /// <summary>
        /// 返回较大值
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Max(double a, double b, double c)
        {
            return Math.Max(a, Math.Max(b, c));
        }

        /// <summary>
        /// 返回较小值
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Min(double a, double b, double c)
        {
            return Math.Min(a, Math.Min(b, c));
        }

        /// <summary>
        /// 返回符号 (-1, 0, 1)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Sign(double value)
        {
            if (IsZero(value)) return 0;
            return value > 0 ? 1 : -1;
        }

        #endregion
    }
}
