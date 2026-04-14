using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DotNet.Drawing
{
    /// <summary>
    /// 单精度浮点矩形（用于 OpenCV 互操作）
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect2f : IEquatable<Rect2f>
    {
        #region Field

        /// <summary>
        /// 左上角X
        /// </summary>
        public float X;

        /// <summary>
        /// 左上角Y
        /// </summary>
        public float Y;

        /// <summary>
        /// 宽度
        /// </summary>
        public float Width;

        /// <summary>
        /// 高度
        /// </summary>
        public float Height;

        /// <summary>
        /// sizeof(Rect2f)
        /// </summary>
        public const int SizeOf = sizeof(float) * 4;

        /// <summary>
        /// 空矩形
        /// </summary>
        public static readonly Rect2f Empty = new Rect2f();

        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        public Rect2f(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// 从位置和尺寸构造
        /// </summary>
        public Rect2f(Point2f location, Size2f size)
        {
            X = location.X;
            Y = location.Y;
            Width = size.Width;
            Height = size.Height;
        }

        /// <summary>
        /// 从左上右下坐标创建
        /// </summary>
        public static Rect2f FromLTRB(float left, float top, float right, float bottom)
        {
            if (right < left)
                throw new ArgumentException("right must be >= left", nameof(right));
            if (bottom < top)
                throw new ArgumentException("bottom must be >= top", nameof(bottom));

            return new Rect2f(left, top, right - left, bottom - top);
        }

        #region Operators

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Rect2f other)
        {
            return X == other.X && Y == other.Y && Width == other.Width && Height == other.Height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Rect2f lhs, Rect2f rhs)
        {
            return lhs.Equals(rhs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Rect2f lhs, Rect2f rhs)
        {
            return !lhs.Equals(rhs);
        }

        /// <summary>
        /// 按偏移量平移矩形
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect2f operator +(Rect2f rect, Point2f pt)
        {
            return new Rect2f(rect.X + pt.X, rect.Y + pt.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// 按偏移量反向平移矩形
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect2f operator -(Rect2f rect, Point2f pt)
        {
            return new Rect2f(rect.X - pt.X, rect.Y - pt.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// 扩展矩形尺寸
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect2f operator +(Rect2f rect, Size2f size)
        {
            return new Rect2f(rect.X, rect.Y, rect.Width + size.Width, rect.Height + size.Height);
        }

        /// <summary>
        /// 收缩矩形尺寸
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect2f operator -(Rect2f rect, Size2f size)
        {
            return new Rect2f(rect.X, rect.Y, rect.Width - size.Width, rect.Height - size.Height);
        }

        /// <summary>
        /// 获取两个矩形的交集
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect2f operator &(Rect2f a, Rect2f b)
        {
            return Intersect(a, b);
        }

        /// <summary>
        /// 获取两个矩形的并集
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect2f operator |(Rect2f a, Rect2f b)
        {
            return Union(a, b);
        }

        #endregion

        #region Properties

        /// <summary>
        /// 上边界Y
        /// </summary>
        public float Top
        {
            get { return Y; }
            set { Y = value; }
        }

        /// <summary>
        /// 下边界Y (Y + Height)
        /// </summary>
        public float Bottom
        {
            get { return Y + Height; }
        }

        /// <summary>
        /// 左边界X
        /// </summary>
        public float Left
        {
            get { return X; }
            set { X = value; }
        }

        /// <summary>
        /// 右边界X (X + Width)
        /// </summary>
        public float Right
        {
            get { return X + Width; }
        }

        /// <summary>
        /// 左上角位置
        /// </summary>
        public Point2f Location
        {
            get { return new Point2f(X, Y); }
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        /// <summary>
        /// 矩形大小
        /// </summary>
        public Size2f Size
        {
            get { return new Size2f(Width, Height); }
            set
            {
                Width = value.Width;
                Height = value.Height;
            }
        }

        /// <summary>
        /// 左上角点
        /// </summary>
        public Point2f TopLeft
        {
            get { return new Point2f(X, Y); }
        }

        /// <summary>
        /// 右下角点
        /// </summary>
        public Point2f BottomRight
        {
            get { return new Point2f(X + Width, Y + Height); }
        }

        #endregion

        #region Methods

        /// <summary>
        /// 判断坐标是否在矩形内
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(float x, float y)
        {
            return X <= x && Y <= y && X + Width > x && Y + Height > y;
        }

        /// <summary>
        /// 判断点是否在矩形内
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(Point2f pt)
        {
            return Contains(pt.X, pt.Y);
        }

        /// <summary>
        /// 判断另一个矩形是否完全在此矩形内
        /// </summary>
        public bool Contains(Rect2f rect)
        {
            return X <= rect.X &&
                   (rect.X + rect.Width) <= (X + Width) &&
                   Y <= rect.Y &&
                   (rect.Y + rect.Height) <= (Y + Height);
        }

        /// <summary>
        /// 向外膨胀矩形（修改自身）
        /// </summary>
        public void Inflate(float width, float height)
        {
            X -= width;
            Y -= height;
            Width += (2 * width);
            Height += (2 * height);
        }

        /// <summary>
        /// 向外膨胀矩形（修改自身）
        /// </summary>
        public void Inflate(Size2f size)
        {
            Inflate(size.Width, size.Height);
        }

        /// <summary>
        /// 创建膨胀后的副本
        /// </summary>
        public static Rect2f Inflate(Rect2f rect, float x, float y)
        {
            rect.Inflate(x, y);
            return rect;
        }

        /// <summary>
        /// 获取两个矩形的交集
        /// </summary>
        public static Rect2f Intersect(Rect2f a, Rect2f b)
        {
            float x1 = Math.Max(a.X, b.X);
            float x2 = Math.Min(a.X + a.Width, b.X + b.Width);
            float y1 = Math.Max(a.Y, b.Y);
            float y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);

            if (x2 >= x1 && y2 >= y1)
                return new Rect2f(x1, y1, x2 - x1, y2 - y1);
            return Empty;
        }

        /// <summary>
        /// 获取与另一个矩形的交集
        /// </summary>
        public Rect2f Intersect(Rect2f rect)
        {
            return Intersect(this, rect);
        }

        /// <summary>
        /// 判断是否与另一个矩形相交
        /// </summary>
        public bool IntersectsWith(Rect2f rect)
        {
            return (X < rect.X + rect.Width) &&
                   (X + Width > rect.X) &&
                   (Y < rect.Y + rect.Height) &&
                   (Y + Height > rect.Y);
        }

        /// <summary>
        /// 获取与另一个矩形的并集
        /// </summary>
        public Rect2f Union(Rect2f rect)
        {
            return Union(this, rect);
        }

        /// <summary>
        /// 获取两个矩形的并集
        /// </summary>
        public static Rect2f Union(Rect2f a, Rect2f b)
        {
            float x1 = Math.Min(a.X, b.X);
            float x2 = Math.Max(a.X + a.Width, b.X + b.Width);
            float y1 = Math.Min(a.Y, b.Y);
            float y2 = Math.Max(a.Y + a.Height, b.Y + b.Height);

            return new Rect2f(x1, y1, x2 - x1, y2 - y1);
        }

        public override bool Equals(object obj)
        {
            return obj is Rect2f other && Equals(other);
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
