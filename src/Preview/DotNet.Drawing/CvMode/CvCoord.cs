using System;
using System.Runtime.CompilerServices;

namespace DotNet.Drawing
{
    /// <summary>
    /// 表示坐标系（包含位置和角度）
    /// </summary>
    /// <remarks>
    /// 设计特点：
    /// - readonly struct: 不可变值类型，天生线程安全，零GC分配
    /// - 用于表示物体的位置和朝向（位姿）
    /// - 属性使用 init 访问器，保证不可变语义
    /// </remarks>
    public readonly struct CvCoord : IEquatable<CvCoord>, ICvTranslatable<CvCoord>, ICvRotatable<CvCoord>
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
        /// 朝向角
        /// </summary>
        /// <remarks>
        /// 类型是 <see cref="DotNet.Drawing.Angle"/> 而非裸 <c>double</c>：历史上这里是弧度，
        /// 但调用方屡屡再补一次 <c>ToRadians()</c>（审查项 B5），编译器无从发现。
        /// 现在取值必须显式写 <c>.Radians</c> 或 <c>.Degrees</c>，单位由类型保证。
        /// JSON 落盘形状不变，仍是一个弧度数字（见 <see cref="AngleJsonConverter"/>）。
        /// </remarks>
        public Angle Angle { get; init; }

        /// <summary>
        /// 角度（度数）
        /// </summary>
        public double AngleDegrees
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Angle.Degrees;
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
            get => Angle.Direction;
        }

        /// <summary>
        /// 是否为单位坐标系（位于原点且无旋转）
        /// </summary>
        public bool IsIdentity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            // X/Y 是像素量走几何容差；Angle 是弧度，属纯数值恒等判定，保留严格容差。
            get => MathHelper.IsZeroGeometric(X) && MathHelper.IsZeroGeometric(Y) && MathHelper.IsZero(Angle.Radians);
        }

        #endregion

        #region Constructors

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="angle">朝向角，缺省为零角</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvCoord(double x, double y, Angle angle = default)
        {
            X = x;
            Y = y;
            Angle = angle.Normalized;
        }

        /// <summary>
        /// 从点和角度构造
        /// </summary>
        /// <param name="center">中心点</param>
        /// <param name="angle">朝向角，缺省为零角</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvCoord(Point2d center, Angle angle = default)
        {
            X = center.X;
            Y = center.Y;
            Angle = angle.Normalized;
        }

        /// <summary>
        /// 从弧度角度创建坐标系
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CvCoord FromRadians(double x, double y, double angleRadians)
        {
            return new CvCoord(x, y, Angle.FromRadians(angleRadians));
        }

        /// <summary>
        /// 从度数角度创建坐标系
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CvCoord FromDegrees(double x, double y, double angleDegrees)
        {
            return new CvCoord(x, y, Angle.FromDegrees(angleDegrees));
        }

        #endregion

        #region Transform Methods

        /// <summary>
        /// 旋转坐标系
        /// </summary>
        /// <param name="deltaAngle">旋转量（弧度）</param>
        /// <remarks>
        /// 形参保持 <c>double</c> 是 <see cref="ICvRotatable{T}"/> 的统一签名（各图元共用）；
        /// 需要强类型旋转量时用 <see cref="Rotate(Angle)"/>。
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvCoord Rotate(double deltaAngle) => Rotate(Angle.FromRadians(deltaAngle));

        /// <summary>
        /// 旋转坐标系（强类型旋转量）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvCoord Rotate(Angle deltaAngle)
        {
            return new CvCoord(X, Y, Angle + deltaAngle);
        }

        /// <summary>
        /// 绕指定点旋转
        /// </summary>
        /// <param name="deltaAngle">旋转量（弧度）</param>
        public CvCoord RotateAround(double deltaAngle, Point2d pivot)
            => RotateAround(Angle.FromRadians(deltaAngle), pivot);

        /// <summary>
        /// 绕指定点旋转（强类型旋转量）
        /// </summary>
        public CvCoord RotateAround(Angle deltaAngle, Point2d pivot)
        {
            // 先旋转位置
            Point2d newCenter = Center.RotateAround(deltaAngle.Radians, pivot);
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
            Point2d dir = Angle.Direction;
            return new CvCoord(X + distance * dir.X, Y + distance * dir.Y, Angle);
        }

        /// <summary>
        /// 沿垂直方向平移（正值向左）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvCoord TranslateSideways(double distance)
        {
            Point2d perp = (Angle + Angle.FromRadians(Math.PI / 2)).Direction;
            return new CvCoord(X + distance * perp.X, Y + distance * perp.Y, Angle);
        }

        /// <summary>
        /// 将点从世界坐标转换到局部坐标
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point2d WorldToLocal(Point2d worldPoint)
        {
            double dx = worldPoint.X - X;
            double dy = worldPoint.Y - Y;
            double cos = Math.Cos(-Angle.Radians);
            double sin = Math.Sin(-Angle.Radians);
            return new Point2d(dx * cos - dy * sin, dx * sin + dy * cos);
        }

        /// <summary>
        /// 将点从局部坐标转换到世界坐标
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Point2d LocalToWorld(Point2d localPoint)
        {
            double cos = Math.Cos(Angle.Radians);
            double sin = Math.Sin(Angle.Radians);
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
                double cos = Math.Cos(-Angle.Radians);
                double sin = Math.Sin(-Angle.Radians);
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
        public Angle AngleDifferenceTo(CvCoord other)
        {
            return (other.Angle - Angle).Normalized;
        }

        /// <summary>
        /// 线性插值
        /// </summary>
        public CvCoord Lerp(CvCoord other, double t)
        {
            return new CvCoord(
                X + (other.X - X) * t,
                Y + (other.Y - Y) * t,
                Angle + Angle.DifferenceTo(other.Angle) * t
            );
        }

        #endregion

        #region Equality

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(CvCoord other)
        {
            return MathHelper.AreEqualGeometric(X, other.X) &&
                   MathHelper.AreEqualGeometric(Y, other.Y) &&
                   Angle.Equals(other.Angle);
        }

        public override bool Equals(object? obj) => obj is CvCoord other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => HashCode.Combine(
            MathHelper.QuantizeGeometric(X),
            MathHelper.QuantizeGeometric(Y),
            MathHelper.QuantizeToTolerance(Angle.Radians));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(CvCoord left, CvCoord right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(CvCoord left, CvCoord right) => !left.Equals(right);

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
        public static readonly CvCoord Identity = new(0, 0, Angle.Zero);

        /// <summary>
        /// 零坐标系常量（同 Identity）
        /// </summary>
        public static readonly CvCoord Zero = Identity;

        #endregion
    }
}

