using System;
using System.Runtime.CompilerServices;
using HalconDotNet;

namespace DotNet.CvHalcon
{
    /// <summary>
    /// 表示矩形区域（轴对齐边界框）
    /// </summary>
    /// <remarks>
    /// 设计特点：
    /// - sealed record class: 不可变引用类型，线程安全
    /// - 自动支持 with 表达式进行函数式更新
    /// - 支持与 Halcon HTuple 互操作
    /// </remarks>
    public sealed record CvRegion : ICvShape, ICvTranslatable<CvRegion>, ICvScalable<CvRegion>, ICvContainable
    {
        #region Properties

        /// <summary>
        /// 左上角X
        /// </summary>
        public double X { get; init; }

        /// <summary>
        /// 左上角Y
        /// </summary>
        public double Y { get; init; }

        /// <summary>
        /// 区域宽度
        /// </summary>
        public double Width { get; init; }

        /// <summary>
        /// 区域高度
        /// </summary>
        public double Height { get; init; }

        /// <summary>
        /// 右边界X
        /// </summary>
        public double Right
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => X + Width;
        }

        /// <summary>
        /// 下边界Y
        /// </summary>
        public double Bottom
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Y + Height;
        }

        /// <summary>
        /// 中心点
        /// </summary>
        public CvPoint Center
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(X + Width / 2, Y + Height / 2);
        }

        /// <summary>
        /// 左上角点
        /// </summary>
        public CvPoint TopLeft
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(X, Y);
        }

        /// <summary>
        /// 右上角点
        /// </summary>
        public CvPoint TopRight
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(Right, Y);
        }

        /// <summary>
        /// 左下角点
        /// </summary>
        public CvPoint BottomLeft
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(X, Bottom);
        }

        /// <summary>
        /// 右下角点
        /// </summary>
        public CvPoint BottomRight
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(Right, Bottom);
        }

        /// <summary>
        /// 左上角点（同 TopLeft，兼容旧代码）
        /// </summary>
        public CvPoint Location => TopLeft;

        /// <summary>
        /// 区域大小
        /// </summary>
        public CvSize Size
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(Width, Height);
        }

        /// <summary>
        /// 区域面积
        /// </summary>
        public double Area
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Width * Height;
        }

        /// <summary>
        /// 区域周长
        /// </summary>
        public double Perimeter
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => 2 * (Width + Height);
        }

        /// <summary>
        /// 对角线长度
        /// </summary>
        public double Diagonal
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Math.Sqrt(Width * Width + Height * Height);
        }

        /// <summary>
        /// 宽高比
        /// </summary>
        public double AspectRatio
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MathHelper.AreEqual(Height, 0) ? 0 : Width / Height;
        }

        /// <summary>
        /// 是否为空（面积为零）
        /// </summary>
        public bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MathHelper.AreEqual(Width, 0) || MathHelper.AreEqual(Height, 0);
        }

        /// <summary>
        /// 是否为正方形
        /// </summary>
        public bool IsSquare
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MathHelper.AreEqual(Width, Height);
        }

        /// <summary>
        /// 边界框（返回自身）
        /// </summary>
        public CvRegion BoundingBox => this;

        #endregion

        #region Constructors

        /// <summary>
        /// 默认构造函数（创建空区域）
        /// </summary>
        public CvRegion()
        {
        }

        /// <summary>
        /// 从坐标和尺寸构造
        /// </summary>
        public CvRegion(double x, double y, double width, double height)
        {
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be non-negative.");
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be non-negative.");

            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// 从位置和大小构造
        /// </summary>
        public CvRegion(CvPoint location, CvSize size)
        {
            X = location.X;
            Y = location.Y;
            Width = size.Width;
            Height = size.Height;
        }

        /// <summary>
        /// 从两个角点构造（左上角和右下角）
        /// </summary>
        public CvRegion(CvPoint topLeft, CvPoint bottomRight)
        {
            double width = bottomRight.X - topLeft.X;
            double height = bottomRight.Y - topLeft.Y;

            if (width < 0) throw new ArgumentOutOfRangeException(nameof(bottomRight), "BottomRight.X must be >= TopLeft.X");
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(bottomRight), "BottomRight.Y must be >= TopLeft.Y");

            X = topLeft.X;
            Y = topLeft.Y;
            Width = width;
            Height = height;
        }

        /// <summary>
        /// 从 Halcon HTuple 构造 (row1, column1, row2, column2)
        /// </summary>
        public CvRegion(HTuple row1, HTuple column1, HTuple row2, HTuple column2)
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
        /// 从中心点和尺寸构造
        /// </summary>
        public static CvRegion FromCenter(CvPoint center, CvSize size)
        {
            return new CvRegion(
                center.X - size.Width / 2,
                center.Y - size.Height / 2,
                size.Width,
                size.Height
            );
        }

        /// <summary>
        /// 从中心点和尺寸构造
        /// </summary>
        public static CvRegion FromCenter(double centerX, double centerY, double width, double height)
        {
            return new CvRegion(
                centerX - width / 2,
                centerY - height / 2,
                width,
                height
            );
        }

        /// <summary>
        /// 创建正方形区域
        /// </summary>
        public static CvRegion Square(double x, double y, double side)
        {
            if (side < 0) throw new ArgumentOutOfRangeException(nameof(side), "Side must be non-negative.");
            return new CvRegion(x, y, side, side);
        }

        /// <summary>
        /// 从点集合创建包围盒
        /// </summary>
        public static CvRegion FromPoints(CvPoint[] points)
        {
            if (points == null || points.Length == 0)
                return Empty;

            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            for (int i = 0; i < points.Length; i++)
            {
                var p = points[i];
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }

            return new CvRegion(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// 从点集合创建包围盒
        /// </summary>
        public static CvRegion FromPoints(System.Collections.Generic.IReadOnlyList<CvPoint> points)
        {
            if (points == null || points.Count == 0)
                return Empty;

            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }

            return new CvRegion(minX, minY, maxX - minX, maxY - minY);
        }

        #endregion

        #region Containment Methods

        /// <summary>
        /// 判断点是否在区域内
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(CvPoint point)
        {
            return point.X >= X && point.X <= Right &&
                   point.Y >= Y && point.Y <= Bottom;
        }

        /// <summary>
        /// 判断坐标是否在区域内
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Contains(double px, double py)
        {
            return px >= X && px <= Right &&
                   py >= Y && py <= Bottom;
        }

        /// <summary>
        /// 判断另一个区域是否完全在此区域内
        /// </summary>
        public bool Contains(CvRegion region)
        {
            if (region == null) return false;
            return region.X >= X && region.Right <= Right &&
                   region.Y >= Y && region.Bottom <= Bottom;
        }

        /// <summary>
        /// 判断点是否在边界上
        /// </summary>
        public bool IsOnBoundary(CvPoint point, double tolerance = 0.01)
        {
            if (!Contains(point))
                return false;

            bool onLeftRight = Math.Abs(point.X - X) < tolerance || Math.Abs(point.X - Right) < tolerance;
            bool onTopBottom = Math.Abs(point.Y - Y) < tolerance || Math.Abs(point.Y - Bottom) < tolerance;
            bool inHorizontal = point.X >= X && point.X <= Right;
            bool inVertical = point.Y >= Y && point.Y <= Bottom;

            return (onLeftRight && inVertical) || (onTopBottom && inHorizontal);
        }

        /// <summary>
        /// 判断是否与另一个区域相交
        /// </summary>
        public bool IntersectsWith(CvRegion region)
        {
            if (region == null) return false;
            return !(region.X > Right || region.Right < X ||
                     region.Y > Bottom || region.Bottom < Y);
        }

        #endregion

        #region Set Operations

        /// <summary>
        /// 获取与另一个区域的交集
        /// </summary>
        public CvRegion? Intersect(CvRegion region)
        {
            if (region == null || !IntersectsWith(region))
                return null;

            double x = Math.Max(X, region.X);
            double y = Math.Max(Y, region.Y);
            double right = Math.Min(Right, region.Right);
            double bottom = Math.Min(Bottom, region.Bottom);

            return new CvRegion(x, y, right - x, bottom - y);
        }

        /// <summary>
        /// 获取与另一个区域的并集（包围盒）
        /// </summary>
        public CvRegion Union(CvRegion region)
        {
            if (region == null) throw new ArgumentNullException(nameof(region));

            double x = Math.Min(X, region.X);
            double y = Math.Min(Y, region.Y);
            double right = Math.Max(Right, region.Right);
            double bottom = Math.Max(Bottom, region.Bottom);

            return new CvRegion(x, y, right - x, bottom - y);
        }

        /// <summary>
        /// 计算两个区域的交集面积
        /// </summary>
        public double IntersectionArea(CvRegion region)
        {
            var intersection = Intersect(region);
            return intersection?.Area ?? 0;
        }

        /// <summary>
        /// 计算两个区域的并集面积
        /// </summary>
        public double UnionArea(CvRegion region)
        {
            if (region == null) return Area;
            return Area + region.Area - IntersectionArea(region);
        }

        /// <summary>
        /// 计算两个区域的 IoU (Intersection over Union)
        /// </summary>
        public double IoU(CvRegion region)
        {
            if (region == null) return 0;
            double unionArea = UnionArea(region);
            if (MathHelper.AreEqual(unionArea, 0)) return 0;
            return IntersectionArea(region) / unionArea;
        }

        #endregion

        #region Transform Methods

        /// <summary>
        /// 平移区域
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvRegion Translate(double dx, double dy)
        {
            return new CvRegion(X + dx, Y + dy, Width, Height);
        }

        /// <summary>
        /// 平移区域
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvRegion Translate(CvPoint offset)
        {
            return new CvRegion(X + offset.X, Y + offset.Y, Width, Height);
        }

        /// <summary>
        /// 别名：偏移区域
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvRegion Offset(double dx, double dy) => Translate(dx, dy);

        /// <summary>
        /// 别名：偏移区域
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvRegion Offset(CvPoint offset) => Translate(offset);

        /// <summary>
        /// 统一缩放区域（以左上角为基准）
        /// </summary>
        public CvRegion Scale(double scale)
        {
            if (scale < 0) throw new ArgumentOutOfRangeException(nameof(scale), "Scale must be non-negative.");
            return new CvRegion(X, Y, Width * scale, Height * scale);
        }

        /// <summary>
        /// 分别缩放宽高
        /// </summary>
        public CvRegion Scale(double scaleX, double scaleY)
        {
            if (scaleX < 0 || scaleY < 0)
                throw new ArgumentOutOfRangeException("Scale factors must be non-negative.");
            return new CvRegion(X, Y, Width * scaleX, Height * scaleY);
        }

        /// <summary>
        /// 以中心为基准缩放
        /// </summary>
        public CvRegion ScaleFromCenter(double scale)
        {
            if (scale < 0) throw new ArgumentOutOfRangeException(nameof(scale), "Scale must be non-negative.");
            var center = Center;
            double newWidth = Width * scale;
            double newHeight = Height * scale;
            return new CvRegion(
                center.X - newWidth / 2,
                center.Y - newHeight / 2,
                newWidth,
                newHeight
            );
        }

        /// <summary>
        /// 膨胀区域（向外扩展）
        /// </summary>
        public CvRegion Inflate(double dx, double dy)
        {
            double newWidth = Width + 2 * dx;
            double newHeight = Height + 2 * dy;
            return new CvRegion(
                X - dx,
                Y - dy,
                Math.Max(0, newWidth),
                Math.Max(0, newHeight)
            );
        }

        /// <summary>
        /// 膨胀区域（统一扩展）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvRegion Inflate(double delta) => Inflate(delta, delta);

        /// <summary>
        /// 收缩区域（向内收缩）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvRegion Deflate(double dx, double dy) => Inflate(-dx, -dy);

        /// <summary>
        /// 收缩区域（统一收缩）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvRegion Deflate(double delta) => Inflate(-delta, -delta);

        /// <summary>
        /// 约束区域在指定范围内
        /// </summary>
        public CvRegion ClampTo(CvRegion bounds)
        {
            if (bounds == null) throw new ArgumentNullException(nameof(bounds));

            double newX = MathHelper.Clamp(X, bounds.X, bounds.Right);
            double newY = MathHelper.Clamp(Y, bounds.Y, bounds.Bottom);
            double newRight = MathHelper.Clamp(Right, bounds.X, bounds.Right);
            double newBottom = MathHelper.Clamp(Bottom, bounds.Y, bounds.Bottom);

            return new CvRegion(newX, newY, Math.Max(0, newRight - newX), Math.Max(0, newBottom - newY));
        }

        #endregion

        #region Corner Methods

        /// <summary>
        /// 获取四个角点
        /// </summary>
        public CvPoint[] GetCorners()
        {
            return new[] { TopLeft, TopRight, BottomRight, BottomLeft };
        }

        /// <summary>
        /// 获取四条边
        /// </summary>
        public CvLine[] GetEdges()
        {
            return new[]
            {
                new CvLine(TopLeft, TopRight),      // Top
                new CvLine(TopRight, BottomRight),  // Right
                new CvLine(BottomRight, BottomLeft),// Bottom
                new CvLine(BottomLeft, TopLeft)     // Left
            };
        }

        #endregion

        #region Halcon Interop

        /// <summary>
        /// 转换为 Halcon 格式 (row1, column1, row2, column2)
        /// </summary>
        public void ToHalconFormat(out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2)
        {
            row1 = new HTuple(Y);
            column1 = new HTuple(X);
            row2 = new HTuple(Bottom);
            column2 = new HTuple(Right);
        }

        /// <summary>
        /// 转换为 Halcon 格式数组 [row1, column1, row2, column2]
        /// </summary>
        public HTuple[] ToHalconFormatArray()
        {
            return new HTuple[] { new HTuple(Y), new HTuple(X), new HTuple(Bottom), new HTuple(Right) };
        }

        /// <summary>
        /// 解构为坐标值
        /// </summary>
        public void Deconstruct(out double row1, out double column1, out double row2, out double column2)
        {
            row1 = Y;
            column1 = X;
            row2 = Bottom;
            column2 = Right;
        }

        #endregion

        #region Equality

        /// <summary>
        /// 使用容差的相等性比较
        /// </summary>
        public bool Equals(CvRegion? other)
        {
            if (other is null) return false;
            return MathHelper.AreEqual(X, other.X) &&
                   MathHelper.AreEqual(Y, other.Y) &&
                   MathHelper.AreEqual(Width, other.Width) &&
                   MathHelper.AreEqual(Height, other.Height);
        }

        public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

        #endregion

        #region Formatting

        public override string ToString()
        {
            return $"Region[({X:G6}, {Y:G6}), {Width:G6}×{Height:G6}]";
        }

        /// <summary>
        /// 格式化输出
        /// </summary>
        public string ToString(string format)
        {
            return $"Region[({X.ToString(format)}, {Y.ToString(format)}), {Width.ToString(format)}×{Height.ToString(format)}]";
        }

        #endregion

        #region Static Members

        /// <summary>
        /// 空区域
        /// </summary>
        public static readonly CvRegion Empty = new CvRegion(0.0, 0.0, 0.0, 0.0);

        /// <summary>
        /// 单位区域 (0,0,1,1)
        /// </summary>
        public static readonly CvRegion Unit = new CvRegion(0.0, 0.0, 1.0, 1.0);

        #endregion
    }
}
