using HalconDotNet;
using Newtonsoft.Json;
using System;

namespace DotNet.Drawing
{
    /// <summary>
    /// 双精度矩形（<b>不可变</b>引用类型，支持 JSON 序列化）
    /// </summary>
    /// <remarks>
    /// <b>不可变</b>：四个几何分量只在构造函数里赋值，之后不再变化。
    /// 这样做是为了兑现构造函数里"宽高非负"的校验——原本 X/Y/Width/Height 是公开可写字段，
    /// 任何人都能在构造之后把 Width 改成负数，校验形同虚设；同时可变的矩形被多处引用时，
    /// 一方的修改会静默波及另一方。需要"改一改"的场景请用返回新实例的
    /// <see cref="Inflate(double,double)"/>、<see cref="Intersect(Rect2d)"/>、
    /// <see cref="Union(Rect2d)"/> 或直接 <c>new</c>。
    /// <para>
    /// 曾带有 <c>[StructLayout(LayoutKind.Sequential)]</c> 与 <c>SizeOf</c> 常量，那是从
    /// struct 版本移植时的遗留物：本类型是引用类型且成员含引用，并非 blittable，
    /// 这两者对它没有任何意义，已移除。
    /// </para>
    /// <para>
    /// 曾被 <c>CvRegion</c> 继承。现在 <c>CvRegion</c> 改为<b>组合</b>持有本类型：
    /// "带 Halcon 句柄、可增可减、还能是圆/椭圆/多边形的 ROI"与"矩形"不构成 is-a 关系，
    /// 那次继承只是为了复用四个分量。因此本类不再需要为派生类留任何虚成员。
    /// </para>
    /// </remarks>
    [Serializable]
    public class Rect2d : IEquatable<Rect2d>
    {
        #region Geometry

        /// <summary>
        /// 左上角X
        /// </summary>
        public double X { get; }

        /// <summary>
        /// 左上角Y
        /// </summary>
        public double Y { get; }

        /// <summary>
        /// 区域宽（非负）
        /// </summary>
        public double Width { get; }

        /// <summary>
        /// 区域高（非负）
        /// </summary>
        public double Height { get; }

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
        /// <remarks>原实现直接赋值、跳过了非负校验，负尺寸的 <see cref="Size2d"/> 能造出非法矩形；
        /// 现在转发到主构造函数，四个入口的校验保持一致。</remarks>
        /// <exception cref="ArgumentOutOfRangeException">宽或高为负</exception>
        public Rect2d(Point2d location, Size2d size)
            : this(location.X, location.Y, size.Width, size.Height)
        {
        }

        /// <summary>
        /// 从坐标和尺寸构造
        /// </summary>
        [JsonConstructor]
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
        /// 曾是 <c>virtual</c>，供当时继承本类的 <c>CvRegion</c> 覆写。那套覆写破坏了对称性：
        /// <c>rect.Equals(region)</c> 与 <c>region.Equals(rect)</c> 结果不同，放进
        /// <c>HashSet</c> / 字典的行为未定义。<c>CvRegion</c> 改为组合之后本类不再有派生类，
        /// 虚分派没有意义，恢复为非虚——判等严格是"四个几何分量在容差内相等"。
        /// </remarks>
        public bool Equals(Rect2d? obj)
        {
            if (ReferenceEquals(obj, null)) return false;
            if (ReferenceEquals(this, obj)) return true;
            // 使用 MathHelper.AreEqualGeometric 与 Point2d/Size2d/CvCoord 等同口径：
            // 矩形四个分量都是像素量，用像素级容差才符合"画面上重合即相等"的直觉。
            return MathHelper.AreEqualGeometric(X, obj.X)
                && MathHelper.AreEqualGeometric(Y, obj.Y)
                && MathHelper.AreEqualGeometric(Width, obj.Width)
                && MathHelper.AreEqualGeometric(Height, obj.Height);
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

        // 四条边界一律只读：Top/Left 曾可写而 Bottom/Right 只读，语义不对称；
        // 现在整个类型不可变，四者自然对称，也不存在"改了 Right 把 Width 改成负数"的路径。

        /// <summary>
        /// 上边界Y
        /// </summary>
        [JsonIgnore]
        public double Top { get { return Y; } }

        /// <summary>
        /// 下边界Y (Y + Height)
        /// </summary>
        [JsonIgnore]
        public double Bottom { get { return Y + Height; } }

        /// <summary>
        /// 左边界X
        /// </summary>
        [JsonIgnore]
        public double Left { get { return X; } }

        /// <summary>
        /// 右边界X (X + Width)
        /// </summary>
        [JsonIgnore]
        public double Right { get { return X + Width; } }

        /// <summary>
        /// 左上角位置
        /// </summary>
        [JsonIgnore]
        public Point2d Location { get { return new Point2d(X, Y); } }

        /// <summary>
        /// 矩形大小
        /// </summary>
        [JsonIgnore]
        public Size2d Size { get { return new Size2d(Width, Height); } }

        /// <summary>
        /// 左上角点
        /// </summary>
        [JsonIgnore]
        public Point2d TopLeft
        {
            get { return new Point2d(X, Y); }
        }

        /// <summary>
        /// 右下角点
        /// </summary>
        [JsonIgnore]
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
        /// 判断坐标是否在矩形内（右开 / 下开区间：<c>Right</c>、<c>Bottom</c> 上的点不算）
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
        /// 判断另一个矩形是否完全在此矩形内（闭区间，允许边界重合）
        /// </summary>
        /// <remarks>
        /// <b>边界语义</b>：本重载用<b>闭区间</b>——与自身完全重合的矩形视为被包含。
        /// 而 <see cref="Contains(double,double)"/> 用<b>右开 / 下开区间</b>
        /// （即 <c>Right</c>、<c>Bottom</c> 上的点不算在内），与像素栅格的半开约定一致。
        /// 两者语义不同是有意为之，调用时请留意。
        /// </remarks>
        public bool Contains(Rect2d rect)
        {
            return X <= rect.X &&
                   (rect.X + rect.Width) <= (X + Width) &&
                   Y <= rect.Y &&
                   (rect.Y + rect.Height) <= (Y + Height);
        }

        /// <summary>
        /// 返回向外膨胀后的<b>新矩形</b>；本实例不变。
        /// </summary>
        /// <remarks>
        /// 传负值即为向内收缩。收缩幅度过大会产生负的宽 / 高——那是构造函数明令禁止的状态，
        /// 由构造函数统一拒绝。
        /// <para>
        /// <b>破坏性变更</b>：原来是 <c>void Inflate(...)</c> 就地修改自身。就地修改是本类型
        /// 唯一绕过"宽高非负"校验的路径，也是矩形被多处共享时最容易踩的坑，因此改为返回新实例。
        /// 调用点须写成 <c>rect = rect.Inflate(dx, dy);</c>。
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">膨胀后宽或高为负</exception>
        public Rect2d Inflate(double width, double height)
        {
            return new Rect2d(X - width, Y - height, Width + 2 * width, Height + 2 * height);
        }

        /// <summary>
        /// 返回向外膨胀后的<b>新矩形</b>；本实例不变。
        /// </summary>
        public Rect2d Inflate(Size2d size)
        {
            return Inflate(size.Width, size.Height);
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
            // 不相交时返回全新实例：原先返回共享的 static readonly Default，
            // 调用方一旦 Inflate() 或改 X/Y/Width/Height 就会永久污染全局单例。
            return new Rect2d();
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
                MathHelper.QuantizeGeometric(X),
                MathHelper.QuantizeGeometric(Y),
                MathHelper.QuantizeGeometric(Width),
                MathHelper.QuantizeGeometric(Height));
        }

        public override string ToString()
        {
            return $"(x:{X} y:{Y} width:{Width} height:{Height})";
        }

        #endregion
    }
}
