using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Drawing;

namespace DotNet.Drawing
{
    /// <summary>
    /// 二维浮点点（用于 OpenCV 互操作）
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Point2f : IEquatable<Point2f>
    {
        /// <summary>
        /// X坐标
        /// </summary>
        public float X;

        /// <summary>
        /// Y坐标
        /// </summary>
        public float Y;

        /// <summary>
        /// 结构体字节大小
        /// </summary>
        public const int SizeOf = sizeof(float) * 2;

        /// <summary>
        /// 构造函数
        /// </summary>
        public Point2f(float x, float y)
        {
            X = x;
            Y = y;
        }

        #region Cast

        /// <summary>
        /// 显式转换为 System.Drawing.Point（截断到整数）
        /// </summary>
        public static explicit operator Point(Point2f self)
        {
            return new Point((int)self.X, (int)self.Y);
        }

        /// <summary>
        /// 显式从 System.Drawing.Point 转换
        /// </summary>
        public static explicit operator Point2f(Point point)
        {
            return new Point2f((float)point.X, (float)point.Y);
        }

        #endregion

        #region Operators

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Point2f other)
        {
            return X == other.X && Y == other.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Point2f lhs, Point2f rhs)
        {
            return lhs.X == rhs.X && lhs.Y == rhs.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Point2f lhs, Point2f rhs)
        {
            return lhs.X != rhs.X || lhs.Y != rhs.Y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point2f operator +(Point2f pt) => pt;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point2f operator -(Point2f pt) => new Point2f(-pt.X, -pt.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point2f operator +(Point2f p1, Point2f p2)
        {
            return new Point2f(p1.X + p2.X, p1.Y + p2.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point2f operator -(Point2f p1, Point2f p2)
        {
            return new Point2f(p1.X - p2.X, p1.Y - p2.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point2f operator *(Point2f pt, double scale)
        {
            return new Point2f((float)(pt.X * scale), (float)(pt.Y * scale));
        }

        #endregion

        #region Override

        public override bool Equals(object obj)
        {
            return obj is Point2f other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }

        public override string ToString()
        {
            return $"({X}, {Y})";
        }

        #endregion

        #region Methods

        /// <summary>
        /// 计算两点之间的距离
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Distance(Point2f p1, Point2f p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// 计算到另一个点的距离
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double DistanceTo(Point2f p)
        {
            return Distance(this, p);
        }

        /// <summary>
        /// 计算两个向量的点积
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double DotProduct(Point2f p1, Point2f p2)
        {
            return (double)p1.X * p2.X + (double)p1.Y * p2.Y;
        }

        /// <summary>
        /// 计算与另一个向量的点积
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double DotProduct(Point2f p)
        {
            return DotProduct(this, p);
        }

        /// <summary>
        /// 计算两个向量的叉积
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double CrossProduct(Point2f p1, Point2f p2)
        {
            return (double)p1.X * p2.Y - (double)p2.X * p1.Y;
        }

        /// <summary>
        /// 计算与另一个向量的叉积
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double CrossProduct(Point2f p)
        {
            return CrossProduct(this, p);
        }

        #endregion
    }
}
