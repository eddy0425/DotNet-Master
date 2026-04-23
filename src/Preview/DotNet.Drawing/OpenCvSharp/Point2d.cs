using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace DotNet.Drawing
{
    /// <summary>
    /// 表示二维空间中的点
    /// </summary>
    /// <remarks>
    /// 设计特点：
    /// - readonly struct: 不可变值类型，天生线程安全，零GC分配
    /// - 属性使用 init 访问器，保证不可变语义
    /// - 实现 IEquatable&lt;T&gt; 提供高效相等性比较
    /// </remarks>
    public readonly struct Point2d : IEquatable<Point2d>, ICvTranslatable<Point2d>, ICvScalable<Point2d>
    {
        #region Properties

        /// <summary>
        /// X坐标
        /// </summary>
        public double X { get; init; }

        /// <summary>
        /// Y坐标
        /// </summary>
        public double Y { get; init; }

        /// <summary>
        /// 向量模长（到原点的距离）
        /// </summary>
        public double Magnitude
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Math.Sqrt(X * X + Y * Y);
        }

        /// <summary>
        /// 向量角度（弧度）
        /// </summary>
        public double Angle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Math.Atan2(Y, X);
        }

        /// <summary>
        /// 向量角度（度数）
        /// </summary>
        public double AngleDegrees
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Angle * 180.0 / Math.PI;
        }

        /// <summary>
        /// 是否为零向量
        /// </summary>
        public bool IsZero
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MathHelper.AreEqual(X, 0) && MathHelper.AreEqual(Y, 0);
        }

        #endregion

        #region Constructors

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point2d(double x, double y)
        {
            X = x;
            Y = y;
        }

        #endregion

        #region Distance Methods

        /// <summary>
        /// 计算到另一个点的距离
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double DistanceTo(Point2d other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 计算到另一个点的距离平方（避免开方运算，用于比较）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double DistanceSquaredTo(Point2d other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            return dx * dx + dy * dy;
        }

        /// <summary>
        /// 计算两点之间的距离
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Distance(Point2d p1, Point2d p2) => p1.DistanceTo(p2);

        /// <summary>
        /// 计算两点之间的距离平方
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double DistanceSquared(Point2d p1, Point2d p2) => p1.DistanceSquaredTo(p2);

        #endregion

        #region Transform Methods

        /// <summary>
        /// 平移点
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point2d Translate(double dx, double dy) => new(X + dx, Y + dy);

        /// <summary>
        /// 平移点
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point2d Translate(Point2d offset) => new(X + offset.X, Y + offset.Y);

        /// <summary>
        /// 缩放点（相对于原点）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point2d Scale(double scale) => new(X * scale, Y * scale);

        /// <summary>
        /// 绕原点旋转
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point2d Rotate(double angle)
        {
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            return new Point2d(X * cos - Y * sin, X * sin + Y * cos);
        }

        /// <summary>
        /// 绕指定点旋转
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point2d RotateAround(double angle, Point2d pivot)
        {
            double dx = X - pivot.X;
            double dy = Y - pivot.Y;
            double cos = Math.Cos(angle);
            double sin = Math.Sin(angle);
            return new Point2d(
                pivot.X + dx * cos - dy * sin,
                pivot.Y + dx * sin + dy * cos
            );
        }

        /// <summary>
        /// 获取单位向量
        /// </summary>
        public Point2d Normalized
        {
            get
            {
                double mag = Magnitude;
                if (MathHelper.AreEqual(mag, 0))
                    return Zero;
                return new Point2d(X / mag, Y / mag);
            }
        }

        #endregion

        #region Vector Operations

        /// <summary>
        /// 点积
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Dot(Point2d other) => X * other.X + Y * other.Y;

        /// <summary>
        /// 叉积（二维空间中返回标量）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Cross(Point2d other) => X * other.Y - Y * other.X;

        /// <summary>
        /// 两点间的线性插值
        /// </summary>
        /// <param name="other">目标点</param>
        /// <param name="t">插值参数 (0-1)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point2d Lerp(Point2d other, double t)
        {
            return new Point2d(
                X + (other.X - X) * t,
                Y + (other.Y - Y) * t
            );
        }

        #endregion

        #region Operators

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point2d operator +(Point2d p1, Point2d p2) => new(p1.X + p2.X, p1.Y + p2.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point2d operator -(Point2d p1, Point2d p2) => new(p1.X - p2.X, p1.Y - p2.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point2d operator *(Point2d p, double scalar) => new(p.X * scalar, p.Y * scalar);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point2d operator *(double scalar, Point2d p) => new(p.X * scalar, p.Y * scalar);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point2d operator /(Point2d p, double scalar)
        {
            if (MathHelper.AreEqual(scalar, 0))
                throw new DivideByZeroException("Cannot divide by zero.");
            return new Point2d(p.X / scalar, p.Y / scalar);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point2d operator -(Point2d p) => new(-p.X, -p.Y);

        #endregion

        #region Equality

        /// <summary>
        /// 使用容差的相等性比较
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Point2d other)
        {
            return MathHelper.AreEqual(X, other.X) && MathHelper.AreEqual(Y, other.Y);
        }

        public override bool Equals(object? obj) => obj is Point2d other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => HashCode.Combine(
            MathHelper.QuantizeToTolerance(X),
            MathHelper.QuantizeToTolerance(Y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Point2d left, Point2d right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Point2d left, Point2d right) => !left.Equals(right);

        #endregion

        #region Formatting

        public override string ToString() => $"({X:G6}, {Y:G6})";

        /// <summary>
        /// 格式化输出
        /// </summary>
        /// <param name="format">数值格式</param>
        public string ToString(string format) => $"({X.ToString(format)}, {Y.ToString(format)})";

        #endregion

        #region Static Members

        /// <summary>
        /// 零点常量
        /// </summary>
        public static readonly Point2d Zero = new(0, 0);

        /// <summary>
        /// X轴单位向量
        /// </summary>
        public static readonly Point2d UnitX = new(1, 0);

        /// <summary>
        /// Y轴单位向量
        /// </summary>
        public static readonly Point2d UnitY = new(0, 1);

        /// <summary>
        /// (1,1) 向量
        /// </summary>
        public static readonly Point2d One = new(1, 1);

        /// <summary>
        /// 从极坐标创建点
        /// </summary>
        /// <param name="radius">半径</param>
        /// <param name="angle">角度（弧度）</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point2d FromPolar(double radius, double angle)
        {
            return new Point2d(radius * Math.Cos(angle), radius * Math.Sin(angle));
        }

        /// <summary>
        /// 计算多个点的中心点
        /// </summary>
        public static Point2d Centroid(Point2d[] points)
        {
            if (points == null || points.Length == 0)
                return Zero;

            double sumX = 0, sumY = 0;
            for (int i = 0; i < points.Length; i++)
            {
                sumX += points[i].X;
                sumY += points[i].Y;
            }
            return new Point2d(sumX / points.Length, sumY / points.Length);
        }

        /// <summary>
        /// 计算多个点的中心点
        /// </summary>
        public static Point2d Centroid(System.Collections.Generic.IReadOnlyList<Point2d> points)
        {
            if (points == null || points.Count == 0)
                return Zero;

            double sumX = 0, sumY = 0;
            for (int i = 0; i < points.Count; i++)
            {
                sumX += points[i].X;
                sumY += points[i].Y;
            }
            return new Point2d(sumX / points.Count, sumY / points.Count);
        }

        #endregion
    }
}