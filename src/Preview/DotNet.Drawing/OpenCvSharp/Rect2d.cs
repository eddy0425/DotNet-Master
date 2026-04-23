using HalconDotNet;
using Newtonsoft.Json;
using System;
using System.Runtime.InteropServices;

namespace DotNet.Drawing
{
    /// <summary>
    /// 双精度矩形（引用类型，支持继承和 JSON 序列化）
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public class Rect2d : IEquatable<Rect2d>
    {
        #region Field

        /// <summary>
        /// 左上角X
        /// </summary>
        public double X;

        /// <summary>
        /// 左上角Y
        /// </summary>
        public double Y;

        /// <summary>
        /// 区域宽
        /// </summary>
        public double Width;

        /// <summary>
        /// 区域高
        /// </summary>
        public double Height;

        /// <summary>
        /// sizeof(Rect2d)
        /// </summary>
        public const int SizeOf = sizeof(double) * 4;

        /// <summary>
        /// 空矩形（所有值为0）
        /// </summary>
        public static readonly Rect2d Default = new Rect2d();

        #endregion

        /// <summary>
        /// 默认构造函数（创建空矩形）
        /// </summary>
        public Rect2d()
        {
            X = 0;
            Y = 0;
            Width = 0;
            Height = 0;
        }

        /// <summary>
        /// 从位置和尺寸构造
        /// </summary>
        public Rect2d(Point2d location, Size2d size)
        {
            X = location.X;
            Y = location.Y;
            Width = size.Width;
            Height = size.Height;
        }

        /// <summary>
        /// 从坐标和尺寸构造
        /// </summary>
        public Rect2d(double x, double y, double width, double height)
        {
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be non-negative.");
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be non-negative.");

            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// 从 Halcon HTuple 构造 (row1, column1, row2, column2)
        /// </summary>
        public Rect2d(HTuple row1, HTuple column1, HTuple row2, HTuple column2)
        {
            if (row1 == null || column1 == null || row2 == null || column2 == null)
                throw new ArgumentNullException("HTuple arguments cannot be null.");

            X = column1.D;
            Y = row1.D;
            Width = column2.D - column1.D;
            Height = row2.D - row1.D;

            if (Width < 0 || Height < 0)
                throw new ArgumentOutOfRangeException("Width and Height must be non-negative.");
        }

        /// <summary>
        /// 从左上右下坐标创建
        /// </summary>
        public static Rect2d FromLTRB(double left, double top, double right, double bottom)
        {
            if (right < left)
                throw new ArgumentException("The 'right' value must be greater than or equal to 'left'.", nameof(right));
            if (bottom < top)
                throw new ArgumentException("The 'bottom' value must be greater than or equal to 'top'.", nameof(bottom));

            return new Rect2d(left, top, right - left, bottom - top);
        }

        #region Operators

        #region == / !=

        /// <summary>
        /// 判断两个矩形是否在容差内相等
        /// </summary>
        /// <remarks>
        /// 设为 <c>virtual</c> 让派生类（如 <see cref="CvRegion"/>）可以扩展比较语义（增加 Phi/Type/...），
        /// 同时通过虚分派保证 <c>HashSet&lt;Rect2d&gt;</c>、<c>EqualityComparer&lt;Rect2d&gt;.Default</c> 等
        /// 标准容器调用时仍走最具体类型的 Equals，从而维持 Equals/GetHashCode 契约。
        /// </remarks>
        public virtual bool Equals(Rect2d? obj)
        {
            if (ReferenceEquals(obj, null)) return false;
            if (ReferenceEquals(this, obj)) return true;
            // 使用 MathHelper.AreEqual 与 Point2d/Size2d/CvCoord 等同口径，
            // 避免 IEEE-754 舍入误差导致的"显示一样但 Equals==false"问题。
            return MathHelper.AreEqual(X, obj.X)
                && MathHelper.AreEqual(Y, obj.Y)
                && MathHelper.AreEqual(Width, obj.Width)
                && MathHelper.AreEqual(Height, obj.Height);
        }

        public static bool operator ==(Rect2d? lhs, Rect2d? rhs)
        {
            if (ReferenceEquals(lhs, null))
                return ReferenceEquals(rhs, null);
            return lhs.Equals(rhs);
        }

        public static bool operator !=(Rect2d? lhs, Rect2d? rhs) => !(lhs == rhs);

        #endregion

        #region + / -

        /// <summary>
        /// 按偏移量平移矩形
        /// </summary>
        public static Rect2d operator +(Rect2d rect, Point2d pt)
        {
            return new Rect2d(rect.X + pt.X, rect.Y + pt.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// 按偏移量反向平移矩形
        /// </summary>
        public static Rect2d operator -(Rect2d rect, Point2d pt)
        {
            return new Rect2d(rect.X - pt.X, rect.Y - pt.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// 扩展矩形尺寸
        /// </summary>
        public static Rect2d operator +(Rect2d rect, Size2d size)
        {
            return new Rect2d(rect.X, rect.Y, rect.Width + size.Width, rect.Height + size.Height);
        }

        /// <summary>
        /// 收缩矩形尺寸
        /// </summary>
        public static Rect2d operator -(Rect2d rect, Size2d size)
        {
            return new Rect2d(rect.X, rect.Y, rect.Width - size.Width, rect.Height - size.Height);
        }

        #endregion

        #region & / |

        /// <summary>
        /// 获取两个矩形的交集
        /// </summary>
        public static Rect2d operator &(Rect2d a, Rect2d b)
        {
            return Intersect(a, b);
        }

        /// <summary>
        /// 获取两个矩形的并集
        /// </summary>
        public static Rect2d operator |(Rect2d a, Rect2d b)
        {
            return Union(a, b);
        }

        #endregion

        #endregion

        #region Properties

        /// <summary>
        /// 中心X
        /// </summary>
        [JsonIgnore]
        public double CenterX { get { return (Left + Right) / 2; } }

        /// <summary>
        /// 中心Y
        /// </summary>
        [JsonIgnore]
        public double CenterY { get { return (Top + Bottom) / 2; } }

        /// <summary>
        /// 上边界Y
        /// </summary>
        public double Top
        {
            get { return Y; }
            set { Y = value; }
        }

        /// <summary>
        /// 下边界Y (Y + Height)
        /// </summary>
        public double Bottom
        {
            get { return Y + Height; }
        }

        /// <summary>
        /// 左边界X
        /// </summary>
        public double Left
        {
            get { return X; }
            set { X = value; }
        }

        /// <summary>
        /// 右边界X (X + Width)
        /// </summary>
        public double Right
        {
            get { return X + Width; }
        }

        /// <summary>
        /// 左上角位置
        /// </summary>
        public Point2d Location
        {
            get { return new Point2d(X, Y); }
            set
            {
                X = value.X;
                Y = value.Y;
            }
        }

        /// <summary>
        /// 矩形大小
        /// </summary>
        public Size2d Size
        {
            get { return new Size2d(Width, Height); }
            set
            {
                Width = value.Width;
                Height = value.Height;
            }
        }

        /// <summary>
        /// 左上角点
        /// </summary>
        public Point2d TopLeft
        {
            get { return new Point2d(X, Y); }
        }

        /// <summary>
        /// 右下角点
        /// </summary>
        public Point2d BottomRight
        {
            get { return new Point2d(X + Width, Y + Height); }
        }

        #endregion

        #region Methods

        /// <summary>
        /// 转换为整数矩形
        /// </summary>
        public Rect ToRect()
        {
            return new Rect((int)X, (int)Y, (int)Width, (int)Height);
        }

        /// <summary>
        /// 判断坐标是否在矩形内
        /// </summary>
        public bool Contains(double x, double y)
        {
            return X <= x && Y <= y && X + Width > x && Y + Height > y;
        }

        /// <summary>
        /// 判断点是否在矩形内
        /// </summary>
        public bool Contains(Point2d pt)
        {
            return Contains(pt.X, pt.Y);
        }

        /// <summary>
        /// 判断另一个矩形是否完全在此矩形内
        /// </summary>
        public bool Contains(Rect2d rect)
        {
            return X <= rect.X &&
                   (rect.X + rect.Width) <= (X + Width) &&
                   Y <= rect.Y &&
                   (rect.Y + rect.Height) <= (Y + Height);
        }

        /// <summary>
        /// 向外膨胀矩形（修改自身）
        /// </summary>
        public void Inflate(double width, double height)
        {
            X -= width;
            Y -= height;
            Width += (2 * width);
            Height += (2 * height);
        }

        /// <summary>
        /// 向外膨胀矩形（修改自身）
        /// </summary>
        public void Inflate(Size2d size)
        {
            Inflate(size.Width, size.Height);
        }

        /// <summary>
        /// 创建膨胀后的副本
        /// </summary>
        public static Rect2d Inflate(Rect2d rect, double x, double y)
        {
            return new Rect2d(rect.X - x, rect.Y - y, rect.Width + 2 * x, rect.Height + 2 * y);
        }

        /// <summary>
        /// 获取两个矩形的交集
        /// </summary>
        public static Rect2d Intersect(Rect2d a, Rect2d b)
        {
            double x1 = Math.Max(a.X, b.X);
            double x2 = Math.Min(a.X + a.Width, b.X + b.Width);
            double y1 = Math.Max(a.Y, b.Y);
            double y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);

            if (x2 >= x1 && y2 >= y1)
                return new Rect2d(x1, y1, x2 - x1, y2 - y1);
            return Default;
        }

        /// <summary>
        /// 获取与另一个矩形的交集
        /// </summary>
        public Rect2d Intersect(Rect2d rect)
        {
            return Intersect(this, rect);
        }

        /// <summary>
        /// 判断是否与另一个矩形相交
        /// </summary>
        public bool IntersectsWith(Rect2d rect)
        {
            return (X < rect.X + rect.Width) &&
                   (X + Width > rect.X) &&
                   (Y < rect.Y + rect.Height) &&
                   (Y + Height > rect.Y);
        }

        /// <summary>
        /// 获取与另一个矩形的并集
        /// </summary>
        public Rect2d Union(Rect2d rect)
        {
            return Union(this, rect);
        }

        /// <summary>
        /// 获取两个矩形的并集
        /// </summary>
        public static Rect2d Union(Rect2d a, Rect2d b)
        {
            double x1 = Math.Min(a.X, b.X);
            double x2 = Math.Max(a.X + a.Width, b.X + b.Width);
            double y1 = Math.Min(a.Y, b.Y);
            double y2 = Math.Max(a.Y + a.Height, b.Y + b.Height);

            return new Rect2d(x1, y1, x2 - x1, y2 - y1);
        }

        public override bool Equals(object? obj) => Equals(obj as Rect2d);

        public override int GetHashCode()
        {
            return HashCode.Combine(
                MathHelper.QuantizeToTolerance(X),
                MathHelper.QuantizeToTolerance(Y),
                MathHelper.QuantizeToTolerance(Width),
                MathHelper.QuantizeToTolerance(Height));
        }

        public override string ToString()
        {
            return $"(x:{X} y:{Y} width:{Width} height:{Height})";
        }

        #endregion
    }
}
