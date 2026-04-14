using System;
using System.Runtime.CompilerServices;

namespace DotNet.Drawing
{
    /// <summary>
    /// 表示坐标系（包含位置和角度）
    /// </summary>
    /// <remarks>
    /// 设计特点：
    /// - readonly record struct: 不可变值类型，天生线程安全，零GC分配
    /// - 用于表示物体的位置和朝向（位姿）
    /// - 自动支持 with 表达式进行函数式更新
    /// </remarks>
    public readonly record struct CvCoord : IEquatable<CvCoord>, ICvTranslatable<CvCoord>, ICvRotatable<CvCoord>
    {
        #region Properties

        /// <summary>
        /// X坐标
        /// </summary>
        public double X { get; init; }

        /// <summary>
        /// Y坐标
        /// </summary>
        public double Y { get; init; }

        /// <summary>
        /// 角度（弧度）
        /// </summary>
        public double Angle { get; init; }

        /// <summary>
        /// 角度（度数）
        /// </summary>
        public double AngleDegrees
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Angle * 180.0 / Math.PI;
        }

        /// <summary>
        /// 中心点
        /// </summary>
        public Point2d Center
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(X, Y);
        }

        /// <summary>
        /// 单位方向向量
        /// </summary>
        public Point2d Direction
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(Math.Cos(Angle), Math.Sin(Angle));
        }

        /// <summary>
        /// 是否为单位坐标系（位于原点且无旋转）
        /// </summary>
        public bool IsIdentity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MathHelper.AreEqual(X, 0) && MathHelper.AreEqual(Y, 0) && MathHelper.AreEqual(Angle, 0);
        }

        #endregion

        #region Constructors

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="angle">角度（弧度）</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvCoord(double x, double y, double angle = 0)
        {
            X = x;
            Y = y;
            Angle = NormalizeAngle(angle);
        }

        /// <summary>
        /// 从点和角度构造
        /// </summary>
        /// <param name="center">中心点</param>
        /// <param name="angle">角度（弧度）</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvCoord(Point2d center, double angle = 0)
        {
            X = center.X;
            Y = center.Y;
            Angle = NormalizeAngle(angle);
        }

        /// <summary>
        /// 从度数角度创建坐标系
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CvCoord FromDegrees(double x, double y, double angleDegrees)
        {
            return new CvCoord(x, y, angleDegrees * Math.PI / 180.0);
        }

        #endregion

        #region Transform Methods

        /// <summary>
        /// 旋转坐标系
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvCoord Rotate(double deltaAngle)
        {
            return new CvCoord(X, Y, Angle + deltaAngle);
        }

        /// <summary>
        /// 绕指定点旋转
        /// </summary>
        public CvCoord RotateAround(double deltaAngle, Point2d pivot)
        {
            // 先旋转位置
            Point2d newCenter = Center.RotateAround(deltaAngle, pivot);
            // 再更新角度
            return new CvCoord(newCenter.X, newCenter.Y, Angle + deltaAngle);
        }

        /// <summary>
        /// 平移坐标系
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvCoord Translate(double dx, double dy)
        {
            return new CvCoord(X + dx, Y + dy, Angle);
        }

        /// <summary>
        /// 平移坐标系
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvCoord Translate(Point2d offset)
        {
            return new CvCoord(X + offset.X, Y + offset.Y, Angle);
        }

        /// <summary>
        /// 沿当前方向平移
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvCoord TranslateForward(double distance)
        {
            return new CvCoord(
                X + distance * Math.Cos(Angle),
                Y + distance * Math.Sin(Angle),
                Angle
            );
        }

        /// <summary>
        /// 沿垂直方向平移（正值向左）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvCoord TranslateSideways(double distance)
        {
            double perpAngle = Angle + Math.PI / 2;
            return new CvCoord(
                X + distance * Math.Cos(perpAngle),
                Y + distance * Math.Sin(perpAngle),
                Angle
            );
        }

        /// <summary>
        /// 将点从世界坐标转换到局部坐标
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point2d WorldToLocal(Point2d worldPoint)
        {
            double dx = worldPoint.X - X;
            double dy = worldPoint.Y - Y;
            double cos = Math.Cos(-Angle);
            double sin = Math.Sin(-Angle);
            return new Point2d(dx * cos - dy * sin, dx * sin + dy * cos);
        }

        /// <summary>
        /// 将点从局部坐标转换到世界坐标
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point2d LocalToWorld(Point2d localPoint)
        {
            double cos = Math.Cos(Angle);
            double sin = Math.Sin(Angle);
            return new Point2d(
                X + localPoint.X * cos - localPoint.Y * sin,
                Y + localPoint.X * sin + localPoint.Y * cos
            );
        }

        /// <summary>
        /// 组合两个坐标系变换
        /// </summary>
        public CvCoord Compose(CvCoord other)
        {
            Point2d newCenter = LocalToWorld(other.Center);
            return new CvCoord(newCenter.X, newCenter.Y, Angle + other.Angle);
        }

        /// <summary>
        /// 获取逆变换
        /// </summary>
        public CvCoord Inverse
        {
            get
            {
                double cos = Math.Cos(-Angle);
                double sin = Math.Sin(-Angle);
                return new CvCoord(
                    -X * cos + Y * sin,
                    -X * sin - Y * cos,
                    -Angle
                );
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// 计算到另一个坐标系的距离
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double DistanceTo(CvCoord other)
        {
            return Center.DistanceTo(other.Center);
        }

        /// <summary>
        /// 计算与另一个坐标系的角度差
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double AngleDifferenceTo(CvCoord other)
        {
            return NormalizeAngle(other.Angle - Angle);
        }

        /// <summary>
        /// 线性插值
        /// </summary>
        public CvCoord Lerp(CvCoord other, double t)
        {
            return new CvCoord(
                X + (other.X - X) * t,
                Y + (other.Y - Y) * t,
                Angle + ShortestAngleDifference(Angle, other.Angle) * t
            );
        }

        /// <summary>
        /// 将角度规范化到 [-π, π) 范围
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double NormalizeAngle(double angle)
        {
            while (angle >= Math.PI) angle -= 2 * Math.PI;
            while (angle < -Math.PI) angle += 2 * Math.PI;
            return angle;
        }

        /// <summary>
        /// 计算两个角度之间的最短差值
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double ShortestAngleDifference(double from, double to)
        {
            double diff = NormalizeAngle(to - from);
            return diff;
        }

        #endregion

        #region Equality

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(CvCoord other)
        {
            return MathHelper.AreEqual(X, other.X) &&
                   MathHelper.AreEqual(Y, other.Y) &&
                   MathHelper.AreEqual(Angle, other.Angle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => HashCode.Combine(X, Y, Angle);

        #endregion

        #region Formatting

        public override string ToString()
        {
            return $"Coord[({X:G6}, {Y:G6}), {Angle:F4}rad ({AngleDegrees:F2}°)]";
        }

        /// <summary>
        /// 格式化输出
        /// </summary>
        public string ToString(string format)
        {
            return $"Coord[({X.ToString(format)}, {Y.ToString(format)}), {Angle.ToString(format)}rad]";
        }

        #endregion

        #region Static Members

        /// <summary>
        /// 单位坐标系常量（原点，无旋转）
        /// </summary>
        public static readonly CvCoord Identity = new(0, 0, 0);

        /// <summary>
        /// 零坐标系常量（同 Identity）
        /// </summary>
        public static readonly CvCoord Zero = Identity;

        #endregion
    }
}

