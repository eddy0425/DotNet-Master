using System;
using System.Runtime.CompilerServices;
using DotNet.Drawing.Internal;

namespace DotNet.Drawing
{
    /// <summary>
    /// 表示线段
    /// </summary>
    /// <remarks>
    /// 设计特点：
    /// - sealed record class: 不可变引用类型，线程安全
    /// - 自动支持 with 表达式进行函数式更新
    /// </remarks>
    public sealed record CvLine
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
            get => Math.Sqrt(LengthSquared);
        }

        /// <summary>
        /// 线段长度平方（避免开方，用于比较）
        /// </summary>
        public double LengthSquared
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var dir = Direction;
                return dir.X * dir.X + dir.Y * dir.Y;
            }
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
            get => Normalize(Direction);
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
                return Normalize(new Point2d(-dir.Y, dir.X));
            }
        }

        /// <summary>
        /// 是否为退化线段（长度为零）
        /// </summary>
        public bool IsDegenerate
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            // 必须在同一量纲上比较：LengthSquared 与 PixelTolerance 的平方比较，
            // 等价于"线段长度不足 0.01 像素"。原先拿长度平方与 1e-9 比，实际阈值是 3e-5 像素。
            get => LengthSquared < MathHelper.PixelTolerance * MathHelper.PixelTolerance;
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
            double distanceToStart = Distance(point, Start);
            double distanceToEnd = Distance(point, End);
            double lineLength = Length;
            return Math.Abs(distanceToStart + distanceToEnd - lineLength) < tolerance;
        }

        /// <summary>
        /// 计算点到线段的最短距离
        /// </summary>
        public double DistanceToPoint(Point2d point)
        {
            if (IsDegenerate)
                return Distance(point, Start);

            var dir = Direction;
            double t = MathHelper.Clamp01(Dot(point - Start, dir) / Dot(dir, dir));
            Point2d closest = Start + dir * t;
            return Distance(point, closest);
        }

        /// <summary>
        /// 获取点在线段上的最近点
        /// </summary>
        public Point2d ClosestPointTo(Point2d point)
        {
            if (IsDegenerate)
                return Start;

            var dir = Direction;
            double t = MathHelper.Clamp01(Dot(point - Start, dir) / Dot(dir, dir));
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
            return Dot(point - Start, dir) / Dot(dir, dir);
        }

        #endregion

        #region Transform Methods

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
            return Start + Direction * t;
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
            intersection = default;

            var d1 = Direction;
            var d2 = other.Direction;
            double cross = Cross(d1, d2);

            // cross = |d1|*|d2|*sin(theta)，量级随线段长度平方增长，
            // 必须以 |d1|*|d2| 为参考做相对判零，否则长线段永远判不出平行。
            if (MathHelper.IsZeroRelative(cross, Length * other.Length))
                return false; // 平行或共线

            var diff = other.Start - Start;
            double t = Cross(diff, d2) / cross;
            double u = Cross(diff, d1) / cross;

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
            intersection = default;
            t = 0;

            var d1 = Direction;
            var d2 = other.Direction;
            double cross = Cross(d1, d2);

            // 同 TryIntersect：叉积量纲为长度平方，按量级做相对判零。
            if (MathHelper.IsZeroRelative(cross, Length * other.Length))
                return false; // 平行

            var diff = other.Start - Start;
            t = Cross(diff, d2) / cross;
            intersection = PointAt(t);
            return true;
        }

        #endregion

        private static double Distance(Point2d a, Point2d b)
        {
            var delta = a - b;
            return Math.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
        }

        private static double Dot(Point2d a, Point2d b) => a.X * b.X + a.Y * b.Y;

        private static double Cross(Point2d a, Point2d b) => a.X * b.Y - a.Y * b.X;

        private static Point2d Normalize(Point2d point)
        {
            double length = Math.Sqrt(point.X * point.X + point.Y * point.Y);
            return MathHelper.AreEqual(length, 0) ? default : point / length;
        }

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
