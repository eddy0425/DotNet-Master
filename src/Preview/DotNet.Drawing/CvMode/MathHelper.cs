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
        /// 数值恒等容差：用于判断"是否为同一个数"，例如判零、判等价参数。
        /// </summary>
        /// <remarks>
        /// 注意：本档 <b>远严于像素精度</b>（约为 1 像素的十亿分之一）。
        /// 涉及坐标、长度、半径等<b>几何量</b>的比较请改用 <see cref="PixelTolerance"/>；
        /// 量级敏感（结果量纲为坐标平方、叉积等）的判定请改用 <see cref="AreEqualRelative"/>。
        /// </remarks>
        public const double Tolerance = 1e-9;

        /// <summary>
        /// 较宽松的容差：用于累积了多步浮点运算、但仍要求"数值上相同"的场景。
        /// </summary>
        public const double LooseTolerance = 1e-6;

        /// <summary>
        /// 像素级容差：几何量（坐标 / 长度 / 半径 / 距离）比较的默认档位。
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
        /// 以<b>相对容差</b>判断两个浮点数是否近似相等
        /// </summary>
        /// <remarks>
        /// 适用于量级不确定的中间量（叉积、行列式等）：这类值的量纲是坐标的平方，
        /// 在 4000x3000 图像上轻易达到 1e7 量级，此时任何绝对容差都失去意义。
        /// 判据为 <c>|a-b| &lt;= tolerance * max(1, |a|, |b|)</c>，
        /// 其中 <c>max</c> 的 1 用于保证在两值都接近 0 时退化为绝对容差。
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreEqualRelative(double a, double b, double tolerance = LooseTolerance)
        {
            double scale = Math.Max(1.0, Math.Max(Math.Abs(a), Math.Abs(b)));
            return Math.Abs(a - b) <= tolerance * scale;
        }

        /// <summary>
        /// 以<b>相对容差</b>判断浮点数是否近似为零（相对于给定的参考量级）
        /// </summary>
        /// <param name="value">待判定的值</param>
        /// <param name="scale">该值的期望量级，例如叉积判平行时传两向量长度之积</param>
        /// <param name="tolerance">相对容差</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsZeroRelative(double value, double scale, double tolerance = LooseTolerance)
        {
            return Math.Abs(value) <= tolerance * Math.Max(1.0, Math.Abs(scale));
        }

        /// <summary>
        /// 将两个几何量量化到像素容差网格后判断是否相等。
        /// </summary>
        /// <remarks>
        /// 适用对象：坐标、长度、半径、宽高等以像素为单位的量。
        /// 网格判等是等价关系，并与 <see cref="QuantizeGeometric"/> 生成的哈希分量严格一致；
        /// 避免使用 <c>|a-b| &lt; tolerance</c> 时因不具传递性、跨网格边界而破坏
        /// <c>Equals/GetHashCode</c> 契约。
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AreEqualGeometric(double a, double b)
        {
            if (double.IsNaN(a) || double.IsNaN(b)) return false;
            return QuantizeGeometric(a).Equals(QuantizeGeometric(b));
        }

        /// <summary>
        /// 按几何量网格判等规则判断是否为零。
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsZeroGeometric(double value)
        {
            return AreEqualGeometric(value, 0);
        }

        /// <summary>
        /// <see cref="AreEqualGeometric"/> 配套的哈希量化（像素级网格）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double QuantizeGeometric(double value)
        {
            return QuantizeToTolerance(value, PixelTolerance);
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
        /// <remarks>
        /// 使用 <c>floor</c> 一次性折算而非循环加减：循环版本对 1e18 这类极大输入需要迭代
        /// 上亿次，实际表现为挂起。NaN / 无穷输入原样返回，避免产生无意义的结果。
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double NormalizeAngle(double angle)
        {
            if (double.IsNaN(angle) || double.IsInfinity(angle)) return angle;
            double result = angle - TwoPi * Math.Floor((angle + Math.PI) / TwoPi);
            // 浮点舍入可能让结果恰好落在开区间端点 π 上，强制回落到 -π。
            return result >= Math.PI ? result - TwoPi : result;
        }

        /// <summary>
        /// 将角度规范化到 [0, 2π) 范围
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double NormalizeAnglePositive(double angle)
        {
            if (double.IsNaN(angle) || double.IsInfinity(angle)) return angle;
            double result = angle - TwoPi * Math.Floor(angle / TwoPi);
            return result >= TwoPi ? 0 : (result < 0 ? 0 : result);
        }

        /// <summary>
        /// 将角度规范化到 [-180, 180) 度数范围
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double NormalizeAngleDegrees(double degrees)
        {
            if (double.IsNaN(degrees) || double.IsInfinity(degrees)) return degrees;
            double result = degrees - 360.0 * Math.Floor((degrees + 180.0) / 360.0);
            return result >= 180.0 ? result - 360.0 : result;
        }

        /// <summary>
        /// 将角度规范化到 [0, 360) 度数范围
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double NormalizeAngleDegreesPositive(double degrees)
        {
            if (double.IsNaN(degrees) || double.IsInfinity(degrees)) return degrees;
            double result = degrees - 360.0 * Math.Floor(degrees / 360.0);
            return result >= 360.0 ? 0 : (result < 0 ? 0 : result);
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
        /// 平滑阶跃 (Smoothstep)：把 <paramref name="value"/> 相对区间
        /// [<paramref name="edge0"/>, <paramref name="edge1"/>] 的位置映射为 <b>0..1</b> 的平滑权重。
        /// </summary>
        /// <remarks>
        /// 注意返回值域是 <b>[0, 1]</b>，而不是 [edge0, edge1] —— 它不是"在 a 与 b 之间插值"，
        /// 若需要后者请用 <see cref="Lerp"/>。原名 <c>SmoothStep(a, b, t)</c> 的参数命名容易被误读为
        /// 插值端点，故更名以贴合实际语义。
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SmoothStepBetween(double edge0, double edge1, double value)
        {
            if (AreEqual(edge0, edge1)) return value < edge0 ? 0 : 1;
            double t = Clamp01((value - edge0) / (edge1 - edge0));
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
