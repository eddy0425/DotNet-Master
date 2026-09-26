using System;
using System.Runtime.InteropServices;

namespace DotNet.Drawing
{
    /// <summary>单精度浮点矩形（用于 OpenCV 互操作）</summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect2f : IEquatable<Rect2f>
    {
        /// <summary>左上角 X 坐标</summary>
        public float X;

        /// <summary>左上角 Y 坐标</summary>
        public float Y;

        /// <summary>宽度</summary>
        public float Width;

        /// <summary>高度</summary>
        public float Height;

        /// <summary>结构体字节大小</summary>
        public const int SizeOf = sizeof(float) * 4;

        /// <summary>用位置和尺寸构造矩形。</summary>
        public Rect2f(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>从位置和尺寸构造矩形。</summary>
        public Rect2f(Point2f location, Size2f size) : this(location.X, location.Y, size.Width, size.Height) { }

        /// <summary>按点的分量平移矩形。</summary>
        public static Rect2f operator +(Rect2f rect, Point2f point) => new(rect.X + point.X, rect.Y + point.Y, rect.Width, rect.Height);
        /// <summary>按点的分量反向平移矩形。</summary>
        public static Rect2f operator -(Rect2f rect, Point2f point) => new(rect.X - point.X, rect.Y - point.Y, rect.Width, rect.Height);
        /// <summary>增加矩形的宽、高，左上角不变。</summary>
        public static Rect2f operator +(Rect2f rect, Size2f size) => new(rect.X, rect.Y, rect.Width + size.Width, rect.Height + size.Height);
        /// <summary>减少矩形的宽、高，左上角不变。</summary>
        public static Rect2f operator -(Rect2f rect, Size2f size) => new(rect.X, rect.Y, rect.Width - size.Width, rect.Height - size.Height);

        /// <summary>按四个分量精确比较矩形。</summary>
        public bool Equals(Rect2f other) => X.Equals(other.X) && Y.Equals(other.Y) && Width.Equals(other.Width) && Height.Equals(other.Height);
        public override bool Equals(object? obj) => obj is Rect2f other && Equals(other);
        public override int GetHashCode() => unchecked(((X.GetHashCode() * 397 ^ Y.GetHashCode()) * 397 ^ Width.GetHashCode()) * 397 ^ Height.GetHashCode());
        public static bool operator ==(Rect2f left, Rect2f right) => left.Equals(right);
        public static bool operator !=(Rect2f left, Rect2f right) => !left.Equals(right);
    }
}
