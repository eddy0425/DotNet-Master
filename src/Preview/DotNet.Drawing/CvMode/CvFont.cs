using System;
using System.Runtime.CompilerServices;

namespace DotNet.Drawing
{
    /// <summary>
    /// 显示字体设置
    /// </summary>
    /// <remarks>
    /// 设计特点：
    /// - sealed record class: 不可变引用类型，线程安全
    /// - 使用 with 表达式进行函数式更新
    /// - 提供丰富的预设字体和工厂方法
    /// </remarks>
    public sealed record CvFont
    {
        #region Properties

        /// <summary>
        /// 字体X坐标
        /// </summary>
        public int X { get; init; } = 50;

        /// <summary>
        /// 字体Y坐标
        /// </summary>
        public int Y { get; init; } = 50;

        /// <summary>
        /// 字体偏移X
        /// </summary>
        public int OffsetX { get; init; } = 0;

        /// <summary>
        /// 字体偏移Y
        /// </summary>
        public int OffsetY { get; init; } = 0;

        /// <summary>
        /// 字体大小
        /// </summary>
        public int Size { get; init; } = 15;

        /// <summary>
        /// 文本内容
        /// </summary>
        public string Text { get; init; } = string.Empty;

        /// <summary>
        /// 字体颜色
        /// </summary>
        public string Color { get; init; } = HColor.Green;

        /// <summary>
        /// 字体粗细
        /// </summary>
        public FontWeight Weight { get; init; } = FontWeight.Normal;

        /// <summary>
        /// 文本对齐方式
        /// </summary>
        public TextAlignment Alignment { get; init; } = TextAlignment.Left;

        /// <summary>
        /// 背景颜色（null 表示透明）
        /// </summary>
        public string? BackgroundColor { get; init; }

        /// <summary>
        /// 是否显示边框
        /// </summary>
        public bool ShowBorder { get; init; } = false;

        /// <summary>
        /// 边框颜色
        /// </summary>
        public string BorderColor { get; init; } = HColor.Black;

        #endregion

        #region Computed Properties

        /// <summary>
        /// 字体位置（考虑偏移）
        /// </summary>
        public Point2d Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(X + OffsetX, Y + OffsetY);
        }

        /// <summary>
        /// 基础位置（不含偏移）
        /// </summary>
        public Point2d BasePosition
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(X, Y);
        }

        /// <summary>
        /// 是否有文本内容
        /// </summary>
        public bool HasText
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => !string.IsNullOrEmpty(Text);
        }

        /// <summary>
        /// 是否有背景
        /// </summary>
        public bool HasBackground
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => !string.IsNullOrEmpty(BackgroundColor);
        }

        /// <summary>
        /// 字体粗细字符串（用于 Halcon）
        /// </summary>
        public string WeightString
        {
            get
            {
                switch (Weight)
                {
                    case FontWeight.Thin: return "thin";
                    case FontWeight.Light: return "light";
                    case FontWeight.Normal: return "normal";
                    case FontWeight.Bold: return "bold";
                    default: return "normal";
                }
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// 默认构造函数
        /// </summary>
        public CvFont()
        {
        }

        /// <summary>
        /// 从位置和大小构造
        /// </summary>
        public CvFont(int x, int y, int size = 15)
        {
            ValidateSize(size);
            X = x;
            Y = y;
            Size = size;
        }

        /// <summary>
        /// 从位置、文本和大小构造
        /// </summary>
        public CvFont(int x, int y, string text, int size = 15)
        {
            ValidateSize(size);
            X = x;
            Y = y;
            Text = text ?? string.Empty;
            Size = size;
        }

        /// <summary>
        /// 从点位置构造
        /// </summary>
        public CvFont(Point2d position, int size = 15)
        {
            ValidateSize(size);
            X = (int)position.X;
            Y = (int)position.Y;
            Size = size;
        }

        /// <summary>
        /// 从点位置和文本构造
        /// </summary>
        public CvFont(Point2d position, string text, int size = 15)
        {
            ValidateSize(size);
            X = (int)position.X;
            Y = (int)position.Y;
            Text = text ?? string.Empty;
            Size = size;
        }

        /// <summary>
        /// 完整构造函数
        /// </summary>
        public CvFont(int x, int y, string text, int size, string color,
                      int offsetX = 0, int offsetY = 0,
                      FontWeight weight = FontWeight.Normal,
                      TextAlignment alignment = TextAlignment.Left)
        {
            ValidateSize(size);
            X = x;
            Y = y;
            Text = text ?? string.Empty;
            Size = size;
            Color = color ?? HColor.Green;
            OffsetX = offsetX;
            OffsetY = offsetY;
            Weight = weight;
            Alignment = alignment;
        }

        #endregion

        #region Factory Methods

        /// <summary>
        /// 创建带文本的字体
        /// </summary>
        public static CvFont WithText(string text, int size = 15, string? color = null)
        {
            return new CvFont
            {
                Text = text ?? string.Empty,
                Size = size,
                Color = color ?? HColor.Green
            };
        }

        /// <summary>
        /// 创建带位置和文本的字体
        /// </summary>
        public static CvFont At(int x, int y, string? text = null, int size = 15)
        {
            return new CvFont
            {
                X = x,
                Y = y,
                Text = text ?? string.Empty,
                Size = size
            };
        }

        /// <summary>
        /// 创建带位置和文本的字体
        /// </summary>
        public static CvFont At(Point2d position, string? text = null, int size = 15)
        {
            return At((int)position.X, (int)position.Y, text, size);
        }

        /// <summary>
        /// 从坐标系创建字体（用于显示匹配结果）
        /// </summary>
        public static CvFont FromCoord(CvCoord coord, string? text = null, int size = 15)
        {
            return new CvFont
            {
                X = (int)coord.X,
                Y = (int)coord.Y,
                Text = text ?? string.Empty,
                Size = size
            };
        }

        #endregion

        #region Fluent Methods

        /// <summary>
        /// 设置位置
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvFont AtPosition(int x, int y) => this with { X = x, Y = y };

        /// <summary>
        /// 设置位置
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvFont AtPosition(Point2d position) => this with { X = (int)position.X, Y = (int)position.Y };

        /// <summary>
        /// 设置位置（兼容性别名）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvFont WithPosition(int x, int y) => AtPosition(x, y);

        /// <summary>
        /// 设置位置（兼容性别名）
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvFont WithPosition(Point2d position) => AtPosition(position);

        /// <summary>
        /// 设置偏移
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvFont WithOffset(int offsetX, int offsetY) => this with { OffsetX = offsetX, OffsetY = offsetY };

        /// <summary>
        /// 设置文本
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvFont WithText(string text) => this with { Text = text ?? string.Empty };

        /// <summary>
        /// 设置格式化文本
        /// </summary>
        public CvFont WithFormattedText(string format, params object[] args)
        {
            return this with { Text = string.Format(format, args) };
        }

        /// <summary>
        /// 设置大小
        /// </summary>
        public CvFont WithSize(int size)
        {
            ValidateSize(size);
            return this with { Size = size };
        }

        /// <summary>
        /// 设置颜色
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvFont WithColor(string color) => this with { Color = color ?? HColor.Green };

        /// <summary>
        /// 设置粗细
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvFont WithWeight(FontWeight weight) => this with { Weight = weight };

        /// <summary>
        /// 设置对齐
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvFont WithAlignment(TextAlignment alignment) => this with { Alignment = alignment };

        /// <summary>
        /// 设置背景
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvFont WithBackground(string? color) => this with { BackgroundColor = color };

        /// <summary>
        /// 设置边框
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvFont WithBorder(bool show = true, string? color = null)
        {
            return this with
            {
                ShowBorder = show,
                BorderColor = color ?? BorderColor
            };
        }

        #endregion

        #region Transform Methods

        /// <summary>
        /// 平移字体位置
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvFont Translate(int dx, int dy) => this with { X = X + dx, Y = Y + dy };

        /// <summary>
        /// 平移字体位置
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CvFont Translate(Point2d offset) => Translate((int)offset.X, (int)offset.Y);

        /// <summary>
        /// 缩放字体大小
        /// </summary>
        public CvFont Scale(double factor)
        {
            if (factor <= 0) throw new ArgumentOutOfRangeException(nameof(factor), "Scale factor must be positive.");
            int newSize = Math.Max(1, (int)(Size * factor));
            return this with { Size = newSize };
        }

        #endregion

        #region Validation

        private static void ValidateSize(int size)
        {
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size), "Font size must be positive.");
        }

        #endregion

        #region Equality

        public bool Equals(CvFont? other)
        {
            if (other is null) return false;
            return X == other.X &&
                   Y == other.Y &&
                   OffsetX == other.OffsetX &&
                   OffsetY == other.OffsetY &&
                   Size == other.Size &&
                   Text == other.Text &&
                   Color == other.Color &&
                   Weight == other.Weight &&
                   Alignment == other.Alignment;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Size, Text, Color);
        }

        #endregion

        #region Formatting

        public override string ToString()
        {
            if (HasText)
                return $"Font[({X},{Y}), Size={Size}, Color={Color}, \"{Text}\"]";
            else
                return $"Font[({X},{Y}), Size={Size}, Color={Color}]";
        }

        #endregion

        #region Presets

        /// <summary>
        /// 默认字体
        /// </summary>
        public static readonly CvFont Default = new(50, 50, 15);

        /// <summary>
        /// 小字体 (10pt)
        /// </summary>
        public static readonly CvFont Small = new(50, 50, 10);

        /// <summary>
        /// 中字体 (15pt)
        /// </summary>
        public static readonly CvFont Medium = new(50, 50, 15);

        /// <summary>
        /// 大字体 (24pt)
        /// </summary>
        public static readonly CvFont Large = new(50, 50, 24);

        /// <summary>
        /// 超大字体 (36pt)
        /// </summary>
        public static readonly CvFont ExtraLarge = new(50, 50, 36);

        /// <summary>
        /// 标题字体 (32pt, 粗体)
        /// </summary>
        public static readonly CvFont Title = new CvFont { X = 100, Y = 50, Size = 32, Weight = FontWeight.Bold };

        /// <summary>
        /// 标签字体 (12pt, 绿色)
        /// </summary>
        public static readonly CvFont Label = new CvFont { Size = 12, Color = HColor.Green };

        /// <summary>
        /// 警告字体 (15pt, 红色)
        /// </summary>
        public static readonly CvFont Warning = new CvFont { Size = 15, Color = HColor.Red };

        /// <summary>
        /// 信息字体 (15pt, 蓝色)
        /// </summary>
        public static readonly CvFont Info = new CvFont { Size = 15, Color = HColor.Blue };

        /// <summary>
        /// 成功字体 (15pt, 绿色, 粗体)
        /// </summary>
        public static readonly CvFont Success = new CvFont { Size = 15, Color = HColor.Green, Weight = FontWeight.Bold };

        /// <summary>
        /// 错误字体 (15pt, 红色, 粗体)
        /// </summary>
        public static readonly CvFont Error = new CvFont { Size = 15, Color = HColor.Red, Weight = FontWeight.Bold };

        /// <summary>
        /// 调试字体 (10pt, 灰色)
        /// </summary>
        public static readonly CvFont Debug = new CvFont { Size = 10, Color = HColor.Gray };

        #endregion
    }

    #region Enums

    /// <summary>
    /// 字体粗细
    /// </summary>
    public enum FontWeight
    {
        /// <summary>
        /// 细体
        /// </summary>
        Thin,

        /// <summary>
        /// 轻体
        /// </summary>
        Light,

        /// <summary>
        /// 正常
        /// </summary>
        Normal,

        /// <summary>
        /// 粗体
        /// </summary>
        Bold
    }

    /// <summary>
    /// 文本对齐方式
    /// </summary>
    public enum TextAlignment
    {
        /// <summary>
        /// 左对齐
        /// </summary>
        Left,

        /// <summary>
        /// 居中
        /// </summary>
        Center,

        /// <summary>
        /// 右对齐
        /// </summary>
        Right
    }

    #endregion
}
