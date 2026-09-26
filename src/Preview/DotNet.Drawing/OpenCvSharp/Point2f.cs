using System;
using System.Runtime.InteropServices;
using System.Drawing;

namespace DotNet.Drawing
{
    /// <summary>二维浮点点（用于 OpenCV 互操作）</summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Point2f : IEquatable<Point2f>
    {
        /// <summary>X 坐标</summary>
        public float X;

        /// <summary>Y 坐标</summary>
        public float Y;

        /// <summary>结构体字节大小</summary>
        public const int SizeOf = sizeof(float) * 2;

        /// <summary>用 X、Y 坐标构造点。</summary>
        public Point2f(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// <summary>显式转换为整数点（截断小数部分）。</summary>
        public static explicit operator Point(Point2f p) => new((int)p.X, (int)p.Y);
        /// <summary>显式从整数点转换。</summary>
        public static explicit operator Point2f(Point p) => new(p.X, p.Y);
        public static Point2f operator +(Point2f a, Point2f b) => new(a.X + b.X, a.Y + b.Y);
        public static Point2f operator -(Point2f a, Point2f b) => new(a.X - b.X, a.Y - b.Y);
        public static Point2f operator -(Point2f p) => new(-p.X, -p.Y);
        public static Point2f operator *(Point2f p, double scalar) => new((float)(p.X * scalar), (float)(p.Y * scalar));

        /// <summary>按坐标精确比较两个点。</summary>
        public bool Equals(Point2f other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override bool Equals(object? obj) => obj is Point2f other && Equals(other);
        public override int GetHashCode() => unchecked(X.GetHashCode() * 397 ^ Y.GetHashCode());
        public static bool operator ==(Point2f left, Point2f right) => left.Equals(right);
        public static bool operator !=(Point2f left, Point2f right) => !left.Equals(right);
    }
}
