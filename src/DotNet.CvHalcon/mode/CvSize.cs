using System;
using System.Runtime.CompilerServices;

namespace DotNet.CvHalcon
{
    /// <summary>
    /// 表示二维尺寸
    /// </summary>
    /// <remarks>
    /// 设计特点：
    /// - readonly record struct: 不可变值类型，天生线程安全，零GC分配
    /// - 自动支持 with 表达式进行函数式更新
    /// </remarks>
    public readonly record struct CvSize : IEquatable<CvSize>, ICvScalable<CvSize>
    {
        #region Properties

        /// <summary>
        /// 宽度
        /// </summary>
        public double Width { get; init; }

        /// <summary>
        /// 高度
        /// </summary>
        public double Height { get; init; }

        /// <summary>
        /// 面积
        /// </summary>
        public double Area
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Width * Height;
        }

        /// <summary>
        /// 周长
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
        /// 宽高比 (Width / Height)
        /// </summary>
        public double AspectRatio
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MathHelper.AreEqual(Height, 0) ? 0 : Width / Height;
        }

        /// <summary>
        /// 是否为空（宽度或高度为零）
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
        /// 较长边
        /// </summary>
        public double LongerSide
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Math.Max(Width, Height);
        }

        /// <summary>
        /// 较短边
        /// </summary>
        public double ShorterSide
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Math.Min(Width, Height);
        }

        #endregion

        #region Constructors

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="width">宽度</param>
        /// <param name="height">高度</param>
        public CvSize(double width, double height)
        {
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be non-negative.");
            if (height < 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be non-negative.");
            Width = width;
            Height = height;
        }

        /// <summary>
        /// 创建正方形尺寸
        /// </summary>
        /// <param name="side">边长</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CvSize Square(double side)
        {
            if (side < 0) throw new ArgumentOutOfRangeException(nameof(side), "Side must be non-negative.");
            return new CvSize(side, side);
        }

        #endregion

        #region Transform Methods

        /// <summary>
        /// 统一缩放
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvSize Scale(double scale)
        {
            if (scale < 0) throw new ArgumentOutOfRangeException(nameof(scale), "Scale must be non-negative.");
            return new CvSize(Width * scale, Height * scale);
        }

        /// <summary>
        /// 分别缩放宽高
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvSize Scale(double scaleX, double scaleY)
        {
            if (scaleX < 0) throw new ArgumentOutOfRangeException(nameof(scaleX), "ScaleX must be non-negative.");
            if (scaleY < 0) throw new ArgumentOutOfRangeException(nameof(scaleY), "ScaleY must be non-negative.");
            return new CvSize(Width * scaleX, Height * scaleY);
        }

        /// <summary>
        /// 膨胀（向外扩展）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvSize Inflate(double delta)
        {
            double newWidth = Width + 2 * delta;
            double newHeight = Height + 2 * delta;
            return new CvSize(Math.Max(0, newWidth), Math.Max(0, newHeight));
        }

        /// <summary>
        /// 分别膨胀宽高
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvSize Inflate(double deltaX, double deltaY)
        {
            double newWidth = Width + 2 * deltaX;
            double newHeight = Height + 2 * deltaY;
            return new CvSize(Math.Max(0, newWidth), Math.Max(0, newHeight));
        }

        /// <summary>
        /// 交换宽高
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvSize Transpose() => new(Height, Width);

        /// <summary>
        /// 将尺寸约束在指定范围内
        /// </summary>
        public CvSize Clamp(CvSize min, CvSize max)
        {
            return new CvSize(
                MathHelper.Clamp(Width, min.Width, max.Width),
                MathHelper.Clamp(Height, min.Height, max.Height)
            );
        }

        /// <summary>
        /// 按比例缩放以适应目标尺寸（保持宽高比）
        /// </summary>
        public CvSize FitInto(CvSize target)
        {
            if (IsEmpty || target.IsEmpty)
                return Zero;

            double scaleX = target.Width / Width;
            double scaleY = target.Height / Height;
            double scale = Math.Min(scaleX, scaleY);
            return new CvSize(Width * scale, Height * scale);
        }

        /// <summary>
        /// 按比例缩放以填充目标尺寸（保持宽高比）
        /// </summary>
        public CvSize FillInto(CvSize target)
        {
            if (IsEmpty || target.IsEmpty)
                return Zero;

            double scaleX = target.Width / Width;
            double scaleY = target.Height / Height;
            double scale = Math.Max(scaleX, scaleY);
            return new CvSize(Width * scale, Height * scale);
        }

        #endregion

        #region Operators

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CvSize operator +(CvSize s1, CvSize s2) => new(s1.Width + s2.Width, s1.Height + s2.Height);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CvSize operator -(CvSize s1, CvSize s2) => new(Math.Max(0, s1.Width - s2.Width), Math.Max(0, s1.Height - s2.Height));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CvSize operator *(CvSize s, double scalar)
        {
            if (scalar < 0) throw new ArgumentOutOfRangeException(nameof(scalar), "Scalar must be non-negative.");
            return new CvSize(s.Width * scalar, s.Height * scalar);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CvSize operator *(double scalar, CvSize s) => s * scalar;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CvSize operator /(CvSize s, double scalar)
        {
            if (MathHelper.AreEqual(scalar, 0))
                throw new DivideByZeroException("Cannot divide by zero.");
            if (scalar < 0)
                throw new ArgumentOutOfRangeException(nameof(scalar), "Scalar must be positive.");
            return new CvSize(s.Width / scalar, s.Height / scalar);
        }

        /// <summary>
        /// 隐式转换为 CvPoint（将尺寸转换为向量）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator CvPoint(CvSize size) => new(size.Width, size.Height);

        #endregion

        #region Equality

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(CvSize other)
        {
            return MathHelper.AreEqual(Width, other.Width) &&
                   MathHelper.AreEqual(Height, other.Height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => HashCode.Combine(Width, Height);

        #endregion

        #region Formatting

        public override string ToString() => $"{Width:G6} × {Height:G6}";

        /// <summary>
        /// 格式化输出
        /// </summary>
        public string ToString(string format) => $"{Width.ToString(format)} × {Height.ToString(format)}";

        #endregion

        #region Static Members

        /// <summary>
        /// 零尺寸常量
        /// </summary>
        public static readonly CvSize Zero = new(0, 0);

        /// <summary>
        /// 单位尺寸常量 (1x1)
        /// </summary>
        public static readonly CvSize Unit = new(1, 1);

        /// <summary>
        /// 取两个尺寸的最大值
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CvSize Max(CvSize s1, CvSize s2) => new(Math.Max(s1.Width, s2.Width), Math.Max(s1.Height, s2.Height));

        /// <summary>
        /// 取两个尺寸的最小值
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static CvSize Min(CvSize s1, CvSize s2) => new(Math.Min(s1.Width, s2.Width), Math.Min(s1.Height, s2.Height));

        #endregion
    }
}
