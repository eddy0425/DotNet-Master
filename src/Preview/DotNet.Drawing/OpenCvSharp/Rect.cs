using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Drawing;

namespace DotNet.Drawing
{
    /// <summary>
    /// 整数矩形（用于 OpenCV 互操作）
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect : IEquatable<Rect>
    {
        #region Field

        /// <summary>
        /// 左上角X
        /// </summary>
        public int X;

        /// <summary>
        /// 左上角Y
        /// </summary>
        public int Y;

        /// <summary>
        /// 宽度
        /// </summary>
        public int Width;

        /// <summary>
        /// 高度
        /// </summary>
        public int Height;

        /// <summary>
        /// sizeof(Rect)
        /// </summary>
        public const int SizeOf = sizeof(int) * 4;

        /// <summary>
        /// 空矩形
        /// </summary>
        public static readonly Rect Empty = new Rect();

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        public Rect(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// 从位置和尺寸构造
        /// </summary>
        public Rect(Point location, Size size)
        {
            X = (int)location.X;
            Y = (int)location.Y;
            Width = (int)size.Width;
            Height = (int)size.Height;
        }

        /// <summary>
        /// 从左上右下坐标创建
        /// </summary>
        public static Rect FromLTRB(int left, int top, int right, int bottom)
        {
            if (right < left)
                throw new ArgumentException("right must be >= left", nameof(right));
            if (bottom < top)
                throw new ArgumentException("bottom must be >= top", nameof(bottom));

            return new Rect(left, top, right - left, bottom - top);
        }

        #region Operators

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Rect other)
        {
            return X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Rect lhs, Rect rhs)
        {
            return lhs.Equals(rhs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Rect lhs, Rect rhs)
        {
            return !lhs.Equals(rhs);
        }

        /// <summary>
        /// 按偏移量平移矩形
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect operator +(Rect rect, Point pt)
        {
            return new Rect((int)(rect.X + pt.X), (int)(rect.Y + pt.Y), rect.Width, rect.Height);
        }

        /// <summary>
        /// 按偏移量反向平移矩形
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect operator -(Rect rect, Point pt)
        {
            return new Rect((int)(rect.X - pt.X), (int)(rect.Y - pt.Y), rect.Width, rect.Height);
        }

        /// <summary>
        /// 扩展矩形尺寸
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect operator +(Rect rect, Size size)
        {
            return new Rect(rect.X, rect.Y, (int)(rect.Width + size.Width), (int)(rect.Height + size.Height));
        }

        /// <summary>
        /// 收缩矩形尺寸
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect operator -(Rect rect, Size size)
        {
            return new Rect(rect.X, rect.Y, (int)(rect.Width - size.Width), (int)(rect.Height - size.Height));
        }

        /// <summary>
        /// 获取两个矩形的交集
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect operator &(Rect a, Rect b)
        {
            return Intersect(a, b);
        }

        /// <summary>
        /// 获取两个矩形的并集
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect operator |(Rect a, Rect b)
        {
            return Union(a, b);
        }

        #endregion

        #region Properties

        /// <summary>
        /// 上边界Y
        /// </summary>
        public int Top
        {
            get { return Y; }
            set { Y = value; }
        }

        /// <summary>
        /// 下边界Y (Y + Height)
        /// </summary>
        public int Bottom
        {
            get { return Y + Height; }
        }

        /// <summary>
        /// 左边界X
        /// </summary>
        public int Left
        {
            get { return X; }
            set { X = value; }
        }

        /// <summary>
        /// 右边界X (X + Width)
        /// </summary>
        public int Right
        {
            get { return X + Width; }
        }

        /// <summary>
        /// 左上角位置
        /// </summary>
        public Point Location
        {
            get { return new Point(X, Y); }
            set
            {
                X = (int)value.X;
                Y = (int)value.Y;
            }
        }

        /// <summary>
        /// 矩形大小
        /// </summary>
        public Size Size
        {
            get { return new Size(Width, Height); }
            set
            {
                Width = (int)value.Width;
                Height = (int)value.Height;
            }
        }

        /// <summary>
        /// 左上角点
        /// </summary>
        public Point TopLeft
        {
            get { return new Point(X, Y); }
        }

        /// <summary>
        /// 右下角点
        /// </summary>
        public Point BottomRight
        {
            get { return new Point(X + Width, Y + Height); }
        }

        #endregion

        #region Methods

        /// <summary>
        /// 判断坐标是否在矩形内
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(int x, int y)
        {
            return X <= x && Y <= y && X + Width > x && Y + Height > y;
        }

        /// <summary>
        /// 判断点是否在矩形内
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(Point pt)
        {
            return Contains((int)pt.X, (int)pt.Y);
        }

        /// <summary>
        /// 判断另一个矩形是否完全在此矩形内
        /// </summary>
        public bool Contains(Rect rect)
        {
            return X <= rect.X &&
                   (rect.X + rect.Width) <= (X + Width) &&
                   Y <= rect.Y &&
                   (rect.Y + rect.Height) <= (Y + Height);
        }

        /// <summary>
        /// 向外膨胀矩形（修改自身）
        /// </summary>
        public void Inflate(int width, int height)
        {
            X -= width;
            Y -= height;
            Width += (2 * width);
            Height += (2 * height);
        }

        /// <summary>
        /// 向外膨胀矩形（修改自身）
        /// </summary>
        public void Inflate(Size size)
        {
            Inflate((int)size.Width, (int)size.Height);
        }

        /// <summary>
        /// 创建膨胀后的副本
        /// </summary>
        public static Rect Inflate(Rect rect, int x, int y)
        {
            rect.Inflate(x, y);
            return rect;
        }

        /// <summary>
        /// 获取两个矩形的交集
        /// </summary>
        public static Rect Intersect(Rect a, Rect b)
        {
            int x1 = Math.Max(a.X, b.X);
            int x2 = Math.Min(a.X + a.Width, b.X + b.Width);
            int y1 = Math.Max(a.Y, b.Y);
            int y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);

            if (x2 >= x1 && y2 >= y1)
                return new Rect(x1, y1, x2 - x1, y2 - y1);
            return Empty;
        }

        /// <summary>
        /// 获取与另一个矩形的交集
        /// </summary>
        public Rect Intersect(Rect rect)
        {
            return Intersect(this, rect);
        }

        /// <summary>
        /// 判断是否与另一个矩形相交
        /// </summary>
        public bool IntersectsWith(Rect rect)
        {
            return (X < rect.X + rect.Width) &&
                   (X + Width > rect.X) &&
                   (Y < rect.Y + rect.Height) &&
                   (Y + Height > rect.Y);
        }

        /// <summary>
        /// 获取与另一个矩形的并集
        /// </summary>
        public Rect Union(Rect rect)
        {
            return Union(this, rect);
        }

        /// <summary>
        /// 获取两个矩形的并集
        /// </summary>
        public static Rect Union(Rect a, Rect b)
        {
            int x1 = Math.Min(a.X, b.X);
            int x2 = Math.Max(a.X + a.Width, b.X + b.Width);
            int y1 = Math.Min(a.Y, b.Y);
            int y2 = Math.Max(a.Y + a.Height, b.Y + b.Height);

            return new Rect(x1, y1, x2 - x1, y2 - y1);
        }

        public override bool Equals(object obj)
        {
            return obj is Rect other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Width, Height);
        }

        public override string ToString()
        {
            return $"(x:{X} y:{Y} width:{Width} height:{Height})";
        }

        #endregion
    }
}
