using System;
using System.Runtime.CompilerServices;

namespace DotNet.Drawing
{
    /// <summary>
    /// 表示线段
    /// </summary>
    /// <remarks>
    /// 设计特点：
    /// - sealed record class: 不可变引用类型，线程安全
    /// - 自动支持 with 表达式进行函数式更新
    /// - 实现几何变换接口
    /// </remarks>
    public sealed record CvLine : ICvShape, ICvTransformable<CvLine>, ICvContainable
    {
        #region Properties

        /// <summary>
        /// 起点
        /// </summary>
        public Point2d Start { get; init; }

        /// <summary>
        /// 终点
        /// </summary>
        public Point2d End { get; init; }

        /// <summary>
        /// 线段长度
        /// </summary>
        public double Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Start.DistanceTo(End);
        }

        /// <summary>
        /// 线段长度平方（避免开方，用于比较）
        /// </summary>
        public double LengthSquared
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Start.DistanceSquaredTo(End);
        }

        /// <summary>
        /// 线段角度（弧度）
        /// </summary>
        public double Angle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Math.Atan2(End.Y - Start.Y, End.X - Start.X);
        }

        /// <summary>
        /// 线段角度（度数）
        /// </summary>
        public double AngleDegrees
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Angle * 180.0 / Math.PI;
        }

        /// <summary>
        /// 中点
        /// </summary>
        public Point2d MidPoint
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new((Start.X + End.X) / 2, (Start.Y + End.Y) / 2);
        }

        /// <summary>
        /// 中心点（同 MidPoint）
        /// </summary>
        public Point2d Center => MidPoint;

        /// <summary>
        /// 方向向量（从起点到终点）
        /// </summary>
        public Point2d Direction
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => End - Start;
        }

        /// <summary>
        /// 单位方向向量
        /// </summary>
        public Point2d UnitDirection
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Direction.Normalized;
        }

        /// <summary>
        /// 法向量（垂直于线段，指向左侧）
        /// </summary>
        public Point2d Normal
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var dir = Direction;
                return new Point2d(-dir.Y, dir.X).Normalized;
            }
        }

        /// <summary>
        /// 边界框
        /// </summary>
        public Rect2d BoundingBox
        {
            get
            {
                double minX = Math.Min(Start.X, End.X);
                double minY = Math.Min(Start.Y, End.Y);
                double maxX = Math.Max(Start.X, End.X);
                double maxY = Math.Max(Start.Y, End.Y);
                return new Rect2d(minX, minY, maxX - minX, maxY - minY);
            }
        }

        /// <summary>
        /// 是否为退化线段（长度为零）
        /// </summary>
        public bool IsDegenerate
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MathHelper.AreEqual(LengthSquared, 0);
        }

        #endregion

        #region Constructors

        /// <summary>
        /// 从两点构造线段
        /// </summary>
        public CvLine(Point2d start, Point2d end)
        {
            Start = start;
            End = end;
        }

        /// <summary>
        /// 从坐标构造线段
        /// </summary>
        public CvLine(double startX, double startY, double endX, double endY)
        {
            Start = new Point2d(startX, startY);
            End = new Point2d(endX, endY);
        }

        /// <summary>
        /// 从起点、角度和长度构造线段
        /// </summary>
        public static CvLine FromAngle(Point2d start, double angle, double length)
        {
            var end = new Point2d(
                start.X + length * Math.Cos(angle),
                start.Y + length * Math.Sin(angle)
            );
            return new CvLine(start, end);
        }

        /// <summary>
        /// 从中点、角度和长度构造线段
        /// </summary>
        public static CvLine FromCenterAngle(Point2d center, double angle, double length)
        {
            double halfLen = length / 2;
            var start = new Point2d(
                center.X - halfLen * Math.Cos(angle),
                center.Y - halfLen * Math.Sin(angle)
            );
            var end = new Point2d(
                center.X + halfLen * Math.Cos(angle),
                center.Y + halfLen * Math.Sin(angle)
            );
            return new CvLine(start, end);
        }

        #endregion

        #region Containment Methods

        /// <summary>
        /// 判断点是否在线段上（带容差）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ContainsPoint(Point2d point, double tolerance = 0.01)
        {
            double distanceToStart = point.DistanceTo(Start);
            double distanceToEnd = point.DistanceTo(End);
            double lineLength = Length;
            return Math.Abs(distanceToStart + distanceToEnd - lineLength) < tolerance;
        }

        /// <summary>
        /// 判断点是否在线段内（实现 ICvContainable）
        /// </summary>
        public bool Contains(Point2d point) => ContainsPoint(point, MathHelper.Tolerance);

        /// <summary>
        /// 判断点是否在边界上（实现 ICvContainable）
        /// </summary>
        public bool IsOnBoundary(Point2d point, double tolerance = 0.01) => ContainsPoint(point, tolerance);

        /// <summary>
        /// 计算点到线段的最短距离
        /// </summary>
        public double DistanceToPoint(Point2d point)
        {
            if (IsDegenerate)
                return point.DistanceTo(Start);

            var dir = Direction;
            double t = MathHelper.Clamp01((point - Start).Dot(dir) / dir.Dot(dir));
            Point2d closest = Start + dir * t;
            return point.DistanceTo(closest);
        }

        /// <summary>
        /// 获取点在线段上的最近点
        /// </summary>
        public Point2d ClosestPointTo(Point2d point)
        {
            if (IsDegenerate)
                return Start;

            var dir = Direction;
            double t = MathHelper.Clamp01((point - Start).Dot(dir) / dir.Dot(dir));
            return Start + dir * t;
        }

        /// <summary>
        /// 获取点在线段上的参数 t (0=Start, 1=End)
        /// </summary>
        public double ProjectPoint(Point2d point)
        {
            if (IsDegenerate)
                return 0;

            var dir = Direction;
            return (point - Start).Dot(dir) / dir.Dot(dir);
        }

        #endregion

        #region Transform Methods

        /// <summary>
        /// 平移线段
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvLine Translate(double dx, double dy)
        {
            return new CvLine(Start.Translate(dx, dy), End.Translate(dx, dy));
        }

        /// <summary>
        /// 平移线段
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvLine Translate(Point2d offset)
        {
            return new CvLine(Start + offset, End + offset);
        }

        /// <summary>
        /// 缩放线段（以中点为中心）
        /// </summary>
        public CvLine Scale(double scale)
        {
            var center = MidPoint;
            var newStart = center + (Start - center) * scale;
            var newEnd = center + (End - center) * scale;
            return new CvLine(newStart, newEnd);
        }

        /// <summary>
        /// 绕中点旋转
        /// </summary>
        public CvLine Rotate(double angle)
        {
            var center = MidPoint;
            return RotateAround(angle, center);
        }

        /// <summary>
        /// 绕指定点旋转
        /// </summary>
        public CvLine RotateAround(double angle, Point2d pivot)
        {
            return new CvLine(
                Start.RotateAround(angle, pivot),
                End.RotateAround(angle, pivot)
            );
        }

        /// <summary>
        /// 反转线段方向
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvLine Reverse() => new(End, Start);

        /// <summary>
        /// 延长线段
        /// </summary>
        /// <param name="startExtension">起点延长量（负值表示收缩）</param>
        /// <param name="endExtension">终点延长量（负值表示收缩）</param>
        public CvLine Extend(double startExtension, double endExtension)
        {
            var dir = UnitDirection;
            return new CvLine(
                Start - dir * startExtension,
                End + dir * endExtension
            );
        }

        /// <summary>
        /// 按百分比分割线段
        /// </summary>
        /// <param name="t">分割参数 (0-1)</param>
        /// <returns>分割点</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point2d PointAt(double t)
        {
            return Start.Lerp(End, t);
        }

        #endregion

        #region Intersection Methods

        /// <summary>
        /// 计算与另一条线段的交点
        /// </summary>
        /// <param name="other">另一条线段</param>
        /// <param name="intersection">交点（如果存在）</param>
        /// <returns>是否相交</returns>
        public bool TryIntersect(CvLine other, out Point2d intersection)
        {
            intersection = Point2d.Zero;

            var d1 = Direction;
            var d2 = other.Direction;
            double cross = d1.Cross(d2);

            if (MathHelper.AreEqual(cross, 0))
                return false; // 平行或共线

            var diff = other.Start - Start;
            double t = diff.Cross(d2) / cross;
            double u = diff.Cross(d1) / cross;

            if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
            {
                intersection = PointAt(t);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 计算与另一条直线的交点（将线段视为无限长直线）
        /// </summary>
        public bool TryIntersectLine(CvLine other, out Point2d intersection, out double t)
        {
            intersection = Point2d.Zero;
            t = 0;

            var d1 = Direction;
            var d2 = other.Direction;
            double cross = d1.Cross(d2);

            if (MathHelper.AreEqual(cross, 0))
                return false; // 平行

            var diff = other.Start - Start;
            t = diff.Cross(d2) / cross;
            intersection = PointAt(t);
            return true;
        }

        #endregion

        #region Equality

        /// <summary>
        /// 使用容差的相等性比较
        /// </summary>
        public bool Equals(CvLine? other)
        {
            if (other is null) return false;
            return Start.Equals(other.Start) && End.Equals(other.End);
        }

        public override int GetHashCode() => HashCode.Combine(Start, End);

        #endregion

        #region Formatting

        public override string ToString()
        {
            return $"Line[{Start} → {End}, Length={Length:G6}]";
        }

        #endregion
    }
}
