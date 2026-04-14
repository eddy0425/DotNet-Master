using System;
using System.Runtime.CompilerServices;

namespace DotNet.Drawing
{
    /// <summary>
    /// 表示圆或圆弧
    /// </summary>
    /// <remarks>
    /// 设计特点：
    /// - sealed record class: 不可变引用类型，线程安全
    /// - 自动支持 with 表达式进行函数式更新
    /// - 支持完整圆和圆弧
    /// </remarks>
    public sealed record CvCircle : ICvShape, ICvTransformable<CvCircle>, ICvContainable
    {
        #region Properties

        /// <summary>
        /// 圆心
        /// </summary>
        public Point2d Center { get; init; }

        /// <summary>
        /// 半径
        /// </summary>
        public double Radius { get; init; }

        /// <summary>
        /// 开始角度（弧度）
        /// </summary>
        public double StartPhi { get; init; }

        /// <summary>
        /// 结束角度（弧度）
        /// </summary>
        public double EndPhi { get; init; }

        /// <summary>
        /// 是否为完整圆
        /// </summary>
        public bool IsFullCircle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MathHelper.AreEqual(StartPhi, EndPhi) ||
                   MathHelper.AreEqual(Math.Abs(EndPhi - StartPhi), 2 * Math.PI);
        }

        /// <summary>
        /// 是否为圆弧
        /// </summary>
        public bool IsArc
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => !IsFullCircle;
        }

        /// <summary>
        /// 圆的周长
        /// </summary>
        public double Circumference
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => 2 * Math.PI * Radius;
        }

        /// <summary>
        /// 圆的面积
        /// </summary>
        public double Area
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Math.PI * Radius * Radius;
        }

        /// <summary>
        /// 圆弧的长度
        /// </summary>
        public double ArcLength
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Radius * Math.Abs(EndPhi - StartPhi);
        }

        /// <summary>
        /// 圆弧角度跨度（弧度）
        /// </summary>
        public double ArcSpan
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Math.Abs(EndPhi - StartPhi);
        }

        /// <summary>
        /// 圆的直径
        /// </summary>
        public double Diameter
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => 2 * Radius;
        }

        /// <summary>
        /// 边界框
        /// </summary>
        public Rect2d BoundingBox
        {
            get
            {
                if (IsFullCircle)
                {
                    return new Rect2d(
                        Center.X - Radius,
                        Center.Y - Radius,
                        Diameter,
                        Diameter
                    );
                }

                // 对于圆弧，需要计算实际的边界框
                double minX = Center.X, maxX = Center.X;
                double minY = Center.Y, maxY = Center.Y;

                // 检查起点和终点
                var startPoint = PointAtAngle(StartPhi);
                var endPoint = PointAtAngle(EndPhi);
                minX = Math.Min(minX, Math.Min(startPoint.X, endPoint.X));
                maxX = Math.Max(maxX, Math.Max(startPoint.X, endPoint.X));
                minY = Math.Min(minY, Math.Min(startPoint.Y, endPoint.Y));
                maxY = Math.Max(maxY, Math.Max(startPoint.Y, endPoint.Y));

                // 检查是否跨越四个极值点
                double normalizedStart = NormalizeAngle(StartPhi);
                double normalizedEnd = NormalizeAngle(EndPhi);
                double[] extremeAngles = { 0, Math.PI / 2, Math.PI, 3 * Math.PI / 2 };

                foreach (double angle in extremeAngles)
                {
                    if (IsAngleInArc(angle, normalizedStart, normalizedEnd))
                    {
                        var point = PointAtAngle(angle);
                        minX = Math.Min(minX, point.X);
                        maxX = Math.Max(maxX, point.X);
                        minY = Math.Min(minY, point.Y);
                        maxY = Math.Max(maxY, point.Y);
                    }
                }

                return new Rect2d(minX, minY, maxX - minX, maxY - minY);
            }
        }

        /// <summary>
        /// 圆弧起点
        /// </summary>
        public Point2d StartPoint
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => PointAtAngle(StartPhi);
        }

        /// <summary>
        /// 圆弧终点
        /// </summary>
        public Point2d EndPoint
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => PointAtAngle(EndPhi);
        }

        /// <summary>
        /// 是否为退化圆（半径为零）
        /// </summary>
        public bool IsDegenerate
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MathHelper.AreEqual(Radius, 0);
        }

        #endregion

        #region Constructors

        /// <summary>
        /// 构造完整圆
        /// </summary>
        public CvCircle(double x, double y, double radius)
        {
            if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius), "Radius must be non-negative.");
            Center = new Point2d(x, y);
            Radius = radius;
            StartPhi = 0;
            EndPhi = 2 * Math.PI;
        }

        /// <summary>
        /// 构造完整圆
        /// </summary>
        public CvCircle(Point2d center, double radius)
        {
            if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius), "Radius must be non-negative.");
            Center = center;
            Radius = radius;
            StartPhi = 0;
            EndPhi = 2 * Math.PI;
        }

        /// <summary>
        /// 构造圆弧
        /// </summary>
        public CvCircle(double x, double y, double radius, double startPhi, double endPhi)
        {
            if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius), "Radius must be non-negative.");
            Center = new Point2d(x, y);
            Radius = radius;
            StartPhi = startPhi;
            EndPhi = endPhi;
        }

        /// <summary>
        /// 构造圆弧
        /// </summary>
        public CvCircle(Point2d center, double radius, double startPhi, double endPhi)
        {
            if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius), "Radius must be non-negative.");
            Center = center;
            Radius = radius;
            StartPhi = startPhi;
            EndPhi = endPhi;
        }

        /// <summary>
        /// 从两点构造圆（第一个点为圆心，第二个点在圆周上）
        /// </summary>
        public CvCircle(Point2d centerPoint, Point2d edgePoint)
        {
            Center = centerPoint;
            Radius = centerPoint.DistanceTo(edgePoint);
            StartPhi = 0;
            EndPhi = 2 * Math.PI;
        }

        /// <summary>
        /// 从三点构造圆（三点确定一个圆）
        /// </summary>
        public static CvCircle? FromThreePoints(Point2d p1, Point2d p2, Point2d p3)
        {
            // 计算两条垂直平分线的交点
            double ax = p1.X, ay = p1.Y;
            double bx = p2.X, by = p2.Y;
            double cx = p3.X, cy = p3.Y;

            double d = 2 * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));
            if (MathHelper.AreEqual(d, 0))
                return null; // 三点共线

            double ux = ((ax * ax + ay * ay) * (by - cy) + (bx * bx + by * by) * (cy - ay) + (cx * cx + cy * cy) * (ay - by)) / d;
            double uy = ((ax * ax + ay * ay) * (cx - bx) + (bx * bx + by * by) * (ax - cx) + (cx * cx + cy * cy) * (bx - ax)) / d;

            var center = new Point2d(ux, uy);
            double radius = center.DistanceTo(p1);
            return new CvCircle(center, radius);
        }

        #endregion

        #region Containment Methods

        /// <summary>
        /// 判断点是否在圆/圆弧内
        /// </summary>
        public bool Contains(Point2d point)
        {
            double distance = Center.DistanceTo(point);
            if (distance > Radius)
                return false;

            if (IsFullCircle)
                return true;

            // 对于圆弧，检查角度是否在范围内
            double angle = Math.Atan2(point.Y - Center.Y, point.X - Center.X);
            return IsAngleInArc(angle, StartPhi, EndPhi);
        }

        /// <summary>
        /// 判断点是否在圆周上（带容差）
        /// </summary>
        public bool IsOnCircumference(Point2d point, double tolerance = 0.01)
        {
            double distance = Center.DistanceTo(point);
            if (Math.Abs(distance - Radius) >= tolerance)
                return false;

            if (IsFullCircle)
                return true;

            double angle = Math.Atan2(point.Y - Center.Y, point.X - Center.X);
            return IsAngleInArc(angle, StartPhi, EndPhi);
        }

        /// <summary>
        /// 判断点是否在边界上
        /// </summary>
        public bool IsOnBoundary(Point2d point, double tolerance = 0.01)
            => IsOnCircumference(point, tolerance);

        /// <summary>
        /// 计算点到圆/圆弧的最短距离
        /// </summary>
        public double DistanceToPoint(Point2d point)
        {
            double distToCenter = Center.DistanceTo(point);

            if (IsFullCircle)
            {
                return Math.Abs(distToCenter - Radius);
            }

            // 计算点的角度
            double angle = Math.Atan2(point.Y - Center.Y, point.X - Center.X);

            if (IsAngleInArc(angle, StartPhi, EndPhi))
            {
                // 点在圆弧角度范围内
                return Math.Abs(distToCenter - Radius);
            }
            else
            {
                // 点不在圆弧角度范围内，计算到两个端点的最短距离
                return Math.Min(point.DistanceTo(StartPoint), point.DistanceTo(EndPoint));
            }
        }

        #endregion

        #region Transform Methods

        /// <summary>
        /// 平移圆
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvCircle Translate(double dx, double dy)
        {
            return new CvCircle(Center.Translate(dx, dy), Radius, StartPhi, EndPhi);
        }

        /// <summary>
        /// 平移圆
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvCircle Translate(Point2d offset)
        {
            return new CvCircle(Center + offset, Radius, StartPhi, EndPhi);
        }

        /// <summary>
        /// 缩放圆
        /// </summary>
        public CvCircle Scale(double scale)
        {
            if (scale < 0) throw new ArgumentOutOfRangeException(nameof(scale), "Scale must be non-negative.");
            return new CvCircle(Center, Radius * scale, StartPhi, EndPhi);
        }

        /// <summary>
        /// 绕圆心旋转圆弧
        /// </summary>
        public CvCircle Rotate(double angle)
        {
            return new CvCircle(Center, Radius, StartPhi + angle, EndPhi + angle);
        }

        /// <summary>
        /// 绕指定点旋转
        /// </summary>
        public CvCircle RotateAround(double angle, Point2d pivot)
        {
            return new CvCircle(
                Center.RotateAround(angle, pivot),
                Radius,
                StartPhi + angle,
                EndPhi + angle
            );
        }

        /// <summary>
        /// 反转圆弧方向
        /// </summary>
        public CvCircle ReverseArc() => new(Center, Radius, EndPhi, StartPhi);

        /// <summary>
        /// 转换为完整圆
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvCircle ToFullCircle() => new(Center, Radius);

        #endregion

        #region Point2d Methods

        /// <summary>
        /// 获取圆周上指定角度的点
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point2d PointAtAngle(double phi)
        {
            return new Point2d(
                Center.X + Radius * Math.Cos(phi),
                Center.Y + Radius * Math.Sin(phi)
            );
        }

        /// <summary>
        /// 获取圆弧上指定参数的点
        /// </summary>
        /// <param name="t">参数 (0=StartPhi, 1=EndPhi)</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point2d PointAt(double t)
        {
            double phi = StartPhi + t * (EndPhi - StartPhi);
            return PointAtAngle(phi);
        }

        /// <summary>
        /// 获取点在圆周上对应的角度
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double AngleOfPoint(Point2d point)
        {
            return Math.Atan2(point.Y - Center.Y, point.X - Center.X);
        }

        /// <summary>
        /// 获取圆周上等距分布的点
        /// </summary>
        /// <param name="count">点的数量</param>
        public Point2d[] SamplePoints(int count)
        {
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive.");

            var points = new Point2d[count];
            double span = IsFullCircle ? 2 * Math.PI : (EndPhi - StartPhi);
            double step = span / count;

            for (int i = 0; i < count; i++)
            {
                double phi = StartPhi + i * step;
                points[i] = PointAtAngle(phi);
            }
            return points;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// 将角度规范化到 [0, 2π) 范围
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double NormalizeAngle(double angle)
        {
            while (angle < 0) angle += 2 * Math.PI;
            while (angle >= 2 * Math.PI) angle -= 2 * Math.PI;
            return angle;
        }

        /// <summary>
        /// 检查角度是否在圆弧范围内
        /// </summary>
        private static bool IsAngleInArc(double angle, double startPhi, double endPhi)
        {
            angle = NormalizeAngle(angle);
            startPhi = NormalizeAngle(startPhi);
            endPhi = NormalizeAngle(endPhi);

            if (startPhi <= endPhi)
            {
                return angle >= startPhi && angle <= endPhi;
            }
            else
            {
                // 圆弧跨越0度
                return angle >= startPhi || angle <= endPhi;
            }
        }

        #endregion

        #region Equality

        /// <summary>
        /// 使用容差的相等性比较
        /// </summary>
        public bool Equals(CvCircle? other)
        {
            if (other is null) return false;
            return Center.Equals(other.Center) &&
                   MathHelper.AreEqual(Radius, other.Radius) &&
                   MathHelper.AreEqual(StartPhi, other.StartPhi) &&
                   MathHelper.AreEqual(EndPhi, other.EndPhi);
        }

        public override int GetHashCode() => HashCode.Combine(Center, Radius, StartPhi, EndPhi);

        #endregion

        #region Formatting

        public override string ToString()
        {
            if (IsFullCircle)
                return $"Circle[Center={Center}, Radius={Radius:G6}]";
            else
                return $"Arc[Center={Center}, Radius={Radius:G6}, φ=[{StartPhi:F3}, {EndPhi:F3}]]";
        }

        #endregion

        #region Static Members

        /// <summary>
        /// 单位圆
        /// </summary>
        public static readonly CvCircle Unit = new(Point2d.Zero, 1);

        #endregion
    }
}
