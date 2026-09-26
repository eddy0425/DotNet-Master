using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace DotNet.Drawing
{
    /// <summary>整数矩形（用于 OpenCV 互操作）</summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect : IEquatable<Rect>
    {
        /// <summary>左上角 X 坐标</summary>
        public int X;

        /// <summary>左上角 Y 坐标</summary>
        public int Y;

        /// <summary>宽度</summary>
        public int Width;

        /// <summary>高度</summary>
        public int Height;

        /// <summary>结构体字节大小</summary>
        public const int SizeOf = sizeof(int) * 4;

        /// <summary>用位置和尺寸构造矩形。</summary>
        public Rect(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>从位置和尺寸构造矩形。</summary>
        public Rect(Point location, Size size) : this(location.X, location.Y, size.Width, size.Height) { }

        /// <summary>按点的分量平移矩形。</summary>
        public static Rect operator +(Rect rect, Point point) => new(rect.X + point.X, rect.Y + point.Y, rect.Width, rect.Height);
        /// <summary>按点的分量反向平移矩形。</summary>
        public static Rect operator -(Rect rect, Point point) => new(rect.X - point.X, rect.Y - point.Y, rect.Width, rect.Height);
        /// <summary>增加矩形的宽、高，左上角不变。</summary>
        public static Rect operator +(Rect rect, Size size) => new(rect.X, rect.Y, rect.Width + size.Width, rect.Height + size.Height);
        /// <summary>减少矩形的宽、高，左上角不变。</summary>
        public static Rect operator -(Rect rect, Size size) => new(rect.X, rect.Y, rect.Width - size.Width, rect.Height - size.Height);

        /// <summary>按四个分量精确比较矩形。</summary>
        public bool Equals(Rect other) => X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
        public override bool Equals(object obj) => obj is Rect other && Equals(other);
        public override int GetHashCode() => unchecked(((X * 397 ^ Y) * 397 ^ Width) * 397 ^ Height);
        public static bool operator ==(Rect left, Rect right) => left.Equals(right);
        public static bool operator !=(Rect left, Rect right) => !left.Equals(right);
    }
}
