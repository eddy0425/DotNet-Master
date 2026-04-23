using System;
using System.Runtime.CompilerServices;

namespace DotNet.Drawing
{
    /// <summary>
    /// 表示箭头（组合模式：包含线段 + 箭头头部样式）
    /// </summary>
    /// <remarks>
    /// 设计特点：
    /// - sealed record class: 不可变引用类型，线程安全
    /// - 使用组合模式，将箭头视为线段的特化
    /// - 自动支持 with 表达式进行函数式更新
    /// </remarks>
    public sealed record CvArrow : ICvShape, ICvTransformable<CvArrow>
    {
        #region Properties

        /// <summary>
        /// 箭头的线段部分
        /// </summary>
        public CvLine Line { get; init; }

        /// <summary>
        /// 箭头头部大小
        /// </summary>
        public double HeadSize { get; init; } = 10.0;

        /// <summary>
        /// 箭头头部角度（度数）
        /// </summary>
        public double HeadAngle { get; init; } = 30.0;

        /// <summary>
        /// 箭头样式
        /// </summary>
        public ArrowStyle Style { get; init; } = ArrowStyle.Open;

        /// <summary>
        /// 起点
        /// </summary>
        public Point2d Start => Line.Start;

        /// <summary>
        /// 终点
        /// </summary>
        public Point2d End => Line.End;

        /// <summary>
        /// 线段长度
        /// </summary>
        public double Length => Line.Length;

        /// <summary>
        /// 线段角度（弧度）
        /// </summary>
        public double Angle => Line.Angle;

        /// <summary>
        /// 线段角度（度数）
        /// </summary>
        public double AngleDegrees => Line.AngleDegrees;

        /// <summary>
        /// 中点
        /// </summary>
        public Point2d MidPoint => Line.MidPoint;

        /// <summary>
        /// 中心点（同 MidPoint）
        /// </summary>
        public Point2d Center => MidPoint;

        /// <summary>
        /// 方向向量
        /// </summary>
        public Point2d Direction => Line.Direction;

        /// <summary>
        /// 单位方向向量
        /// </summary>
        public Point2d UnitDirection => Line.UnitDirection;

        /// <summary>
        /// 边界框
        /// </summary>
        public Rect2d BoundingBox
        {
            get
            {
                // 计算包含箭头头部的边界框
                GetHeadPoints(out Point2d left, out Point2d right);
                double minX = Math.Min(Start.X, Math.Min(End.X, Math.Min(left.X, right.X)));
                double minY = Math.Min(Start.Y, Math.Min(End.Y, Math.Min(left.Y, right.Y)));
                double maxX = Math.Max(Start.X, Math.Max(End.X, Math.Max(left.X, right.X)));
                double maxY = Math.Max(Start.Y, Math.Max(End.Y, Math.Max(left.Y, right.Y)));
                return new Rect2d(minX, minY, maxX - minX, maxY - minY);
            }
        }

        /// <summary>
        /// 箭头头部角度（弧度）
        /// </summary>
        public double HeadAngleRadians
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => HeadAngle * Math.PI / 180.0;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// 从线段构造箭头
        /// </summary>
        public CvArrow(CvLine line, double headSize = 10.0, double headAngle = 30.0, ArrowStyle style = ArrowStyle.Open)
        {
            Line = line ?? throw new ArgumentNullException(nameof(line));
            HeadSize = headSize;
            HeadAngle = headAngle;
            Style = style;
        }

        /// <summary>
        /// 从两点构造箭头
        /// </summary>
        public CvArrow(Point2d start, Point2d end, double headSize = 10.0, double headAngle = 30.0)
        {
            Line = new CvLine(start, end);
            HeadSize = headSize;
            HeadAngle = headAngle;
        }

        /// <summary>
        /// 从坐标构造箭头
        /// </summary>
        public CvArrow(double startX, double startY, double endX, double endY, double headSize = 10.0, double headAngle = 30.0)
        {
            Line = new CvLine(startX, startY, endX, endY);
            HeadSize = headSize;
            HeadAngle = headAngle;
        }

        /// <summary>
        /// 从起点、角度和长度构造箭头
        /// </summary>
        public static CvArrow FromAngle(Point2d start, double angle, double length, double headSize = 10.0, double headAngle = 30.0)
        {
            return new CvArrow(CvLine.FromAngle(start, angle, length), headSize, headAngle);
        }

        /// <summary>
        /// 从中点、角度和长度构造箭头
        /// </summary>
        public static CvArrow FromCenterAngle(Point2d center, double angle, double length, double headSize = 10.0, double headAngle = 30.0)
        {
            return new CvArrow(CvLine.FromCenterAngle(center, angle, length), headSize, headAngle);
        }

        #endregion

        #region Head Points

        /// <summary>
        /// 获取箭头头部的两个端点
        /// </summary>
        /// <param name="left">左翼端点</param>
        /// <param name="right">右翼端点</param>
        public void GetHeadPoints(out Point2d left, out Point2d right)
        {
            double arrowAngle = Line.Angle;
            double halfHeadAngle = HeadAngleRadians / 2;

            // 计算左右两个箭头翼的端点
            left = new Point2d(
                End.X - HeadSize * Math.Cos(arrowAngle - halfHeadAngle),
                End.Y - HeadSize * Math.Sin(arrowAngle - halfHeadAngle)
            );

            right = new Point2d(
                End.X - HeadSize * Math.Cos(arrowAngle + halfHeadAngle),
                End.Y - HeadSize * Math.Sin(arrowAngle + halfHeadAngle)
            );
        }

        /// <summary>
        /// 获取箭头头部左翼端点
        /// </summary>
        public Point2d HeadLeft
        {
            get
            {
                double arrowAngle = Line.Angle;
                double halfHeadAngle = HeadAngleRadians / 2;
                return new Point2d(
                    End.X - HeadSize * Math.Cos(arrowAngle - halfHeadAngle),
                    End.Y - HeadSize * Math.Sin(arrowAngle - halfHeadAngle)
                );
            }
        }

        /// <summary>
        /// 获取箭头头部右翼端点
        /// </summary>
        public Point2d HeadRight
        {
            get
            {
                double arrowAngle = Line.Angle;
                double halfHeadAngle = HeadAngleRadians / 2;
                return new Point2d(
                    End.X - HeadSize * Math.Cos(arrowAngle + halfHeadAngle),
                    End.Y - HeadSize * Math.Sin(arrowAngle + halfHeadAngle)
                );
            }
        }

        /// <summary>
        /// 获取箭头的所有关键点
        /// </summary>
        public Point2d[] GetAllPoints()
        {
            GetHeadPoints(out Point2d left, out Point2d right);
            switch (Style)
            {
                case ArrowStyle.Open:
                    return new[] { Start, End, left, End, right };
                case ArrowStyle.Closed:
                    return new[] { Start, End, left, right, End };
                case ArrowStyle.Filled:
                    return new[] { Start, End, left, right };
                default:
                    return new[] { Start, End, left, End, right };
            }
        }

        #endregion

        #region Containment Methods

        /// <summary>
        /// 判断点是否在线段上
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsPoint(Point2d point, double tolerance = 0.01)
        {
            return Line.ContainsPoint(point, tolerance);
        }

        /// <summary>
        /// 计算点到箭头线段的距离
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double DistanceToPoint(Point2d point)
        {
            return Line.DistanceToPoint(point);
        }

        #endregion

        #region Transform Methods

        /// <summary>
        /// 平移箭头
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvArrow Translate(double dx, double dy)
        {
            return new CvArrow(Line.Translate(dx, dy), HeadSize, HeadAngle, Style);
        }

        /// <summary>
        /// 平移箭头
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvArrow Translate(Point2d offset)
        {
            return new CvArrow(Line.Translate(offset), HeadSize, HeadAngle, Style);
        }

        /// <summary>
        /// 缩放箭头（同时缩放线段长度和箭头头部大小）
        /// </summary>
        public CvArrow Scale(double scale)
        {
            return new CvArrow(Line.Scale(scale), HeadSize * scale, HeadAngle, Style);
        }

        /// <summary>
        /// 绕中点旋转
        /// </summary>
        public CvArrow Rotate(double angle)
        {
            return new CvArrow(Line.Rotate(angle), HeadSize, HeadAngle, Style);
        }

        /// <summary>
        /// 绕指定点旋转
        /// </summary>
        public CvArrow RotateAround(double angle, Point2d pivot)
        {
            return new CvArrow(Line.RotateAround(angle, pivot), HeadSize, HeadAngle, Style);
        }

        /// <summary>
        /// 反转箭头方向
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvArrow Reverse() => new(Line.Reverse(), HeadSize, HeadAngle, Style);

        /// <summary>
        /// 延长箭头
        /// </summary>
        public CvArrow Extend(double startExtension, double endExtension)
        {
            return new CvArrow(Line.Extend(startExtension, endExtension), HeadSize, HeadAngle, Style);
        }

        #endregion

        #region With Methods

        /// <summary>
        /// 创建修改了头部大小的新箭头
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvArrow WithHeadSize(double headSize) => this with { HeadSize = headSize };

        /// <summary>
        /// 创建修改了头部角度的新箭头
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvArrow WithHeadAngle(double headAngle) => this with { HeadAngle = headAngle };

        /// <summary>
        /// 创建修改了样式的新箭头
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvArrow WithStyle(ArrowStyle style) => this with { Style = style };

        #endregion

        #region Equality

        /// <summary>
        /// 使用容差的相等性比较
        /// </summary>
        public bool Equals(CvArrow? other)
        {
            if (other is null) return false;
            return Line.Equals(other.Line) &&
                   MathHelper.AreEqual(HeadSize, other.HeadSize) &&
                   MathHelper.AreEqual(HeadAngle, other.HeadAngle) &&
                   Style == other.Style;
        }

        public override int GetHashCode() => HashCode.Combine(
            Line,
            MathHelper.QuantizeToTolerance(HeadSize),
            MathHelper.QuantizeToTolerance(HeadAngle),
            Style);

        #endregion

        #region Formatting

        public override string ToString()
        {
            return $"Arrow[{Start} → {End}, Length={Length:G6}, Head={HeadSize:G4}@{HeadAngle}°, Style={Style}]";
        }

        #endregion
    }

    /// <summary>
    /// 箭头样式
    /// </summary>
    public enum ArrowStyle
    {
        /// <summary>
        /// 开放式箭头（两条线）
        /// </summary>
        Open,

        /// <summary>
        /// 闭合式箭头（三角形轮廓）
        /// </summary>
        Closed,

        /// <summary>
        /// 实心箭头（填充三角形）
        /// </summary>
        Filled
    }
}
