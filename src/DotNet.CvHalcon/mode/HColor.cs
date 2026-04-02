using System;
using System.Collections.Generic;

namespace DotNet.CvHalcon
{
    /// <summary>
    /// RGB 颜色值结构体
    /// </summary>
    public readonly struct RgbColor
    {
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }

        public RgbColor(byte r, byte g, byte b)
        {
            R = r;
            G = g;
            B = b;
        }

        public override string ToString() => $"RGB({R}, {G}, {B})";
    }

    /// <summary>
    /// Halcon 颜色常量和颜色工具
    /// </summary>
    /// <remarks>
    /// 设计特点：
    /// - 静态类，无状态，线程安全
    /// - 提供完整的 Halcon 支持的颜色名称
    /// - 支持 RGB 颜色转换
    /// </remarks>
    public static class HColor
    {
        #region Basic Colors

        /// <summary>绿色</summary>
        public const string Green = "green";

        /// <summary>红色</summary>
        public const string Red = "red";

        /// <summary>蓝色</summary>
        public const string Blue = "blue";

        /// <summary>橙色</summary>
        public const string Orange = "orange";

        /// <summary>粉色</summary>
        public const string Pink = "pink";

        /// <summary>黄色</summary>
        public const string Yellow = "yellow";

        /// <summary>青色</summary>
        public const string Cyan = "cyan";

        /// <summary>品红</summary>
        public const string Magenta = "magenta";

        /// <summary>珊瑚色</summary>
        public const string Coral = "coral";

        #endregion

        #region Grayscale Colors

        /// <summary>黑色</summary>
        public const string Black = "black";

        /// <summary>白色</summary>
        public const string White = "white";

        /// <summary>灰色</summary>
        public const string Gray = "gray";

        /// <summary>暗灰色</summary>
        public const string DimGray = "dim gray";

        /// <summary>浅灰色</summary>
        public const string LightGray = "light gray";

        /// <summary>深灰色</summary>
        public const string DarkGray = "dark gray";

        /// <summary>银色</summary>
        public const string Silver = "silver";

        #endregion

        #region Blue Variants

        /// <summary>军蓝色</summary>
        public const string CadetBlue = "cadet blue";

        /// <summary>中灰蓝色</summary>
        public const string MediumSlateBlue = "medium slate blue";

        /// <summary>灰蓝色</summary>
        public const string SlateBlue = "slate blue";

        /// <summary>天蓝色</summary>
        public const string SkyBlue = "sky blue";

        /// <summary>淡蓝色</summary>
        public const string LightBlue = "light blue";

        /// <summary>深蓝色</summary>
        public const string DarkBlue = "dark blue";

        /// <summary>海军蓝</summary>
        public const string Navy = "navy";

        /// <summary>宝蓝色</summary>
        public const string RoyalBlue = "royal blue";

        /// <summary>钢蓝色</summary>
        public const string SteelBlue = "steel blue";

        /// <summary>道奇蓝</summary>
        public const string DodgerBlue = "dodger blue";

        #endregion

        #region Green Variants

        /// <summary>春绿色</summary>
        public const string SpringGreen = "spring green";

        /// <summary>暗橄榄绿</summary>
        public const string DarkOliveGreen = "dark olive green";

        /// <summary>森林绿</summary>
        public const string ForestGreen = "forest green";

        /// <summary>淡绿色</summary>
        public const string LightGreen = "light green";

        /// <summary>深绿色</summary>
        public const string DarkGreen = "dark green";

        /// <summary>草绿色</summary>
        public const string LawnGreen = "lawn green";

        /// <summary>酸橙绿</summary>
        public const string LimeGreen = "lime green";

        /// <summary>海绿色</summary>
        public const string SeaGreen = "sea green";

        /// <summary>橄榄色</summary>
        public const string Olive = "olive";

        /// <summary>青绿色</summary>
        public const string Teal = "teal";

        #endregion

        #region Red Variants

        /// <summary>橙红色</summary>
        public const string OrangeRed = "orange red";

        /// <summary>深红色</summary>
        public const string DarkRed = "dark red";

        /// <summary>深红</summary>
        public const string Crimson = "crimson";

        /// <summary>火砖红</summary>
        public const string Firebrick = "firebrick";

        /// <summary>印度红</summary>
        public const string IndianRed = "indian red";

        /// <summary>褐红色</summary>
        public const string Maroon = "maroon";

        /// <summary>番茄红</summary>
        public const string Tomato = "tomato";

        #endregion

        #region Yellow/Orange Variants

        /// <summary>金色</summary>
        public const string Gold = "gold";

        /// <summary>浅黄色</summary>
        public const string LightYellow = "light yellow";

        /// <summary>柠檬绿</summary>
        public const string LemonChiffon = "lemon chiffon";

        /// <summary>卡其色</summary>
        public const string Khaki = "khaki";

        /// <summary>深橙色</summary>
        public const string DarkOrange = "dark orange";

        /// <summary>沙棕色</summary>
        public const string SandyBrown = "sandy brown";

        /// <summary>桃色</summary>
        public const string PeachPuff = "peach puff";

        #endregion

        #region Purple/Violet Variants

        /// <summary>紫色</summary>
        public const string Purple = "purple";

        /// <summary>紫罗兰</summary>
        public const string Violet = "violet";

        /// <summary>兰花紫</summary>
        public const string Orchid = "orchid";

        /// <summary>深紫色</summary>
        public const string DarkViolet = "dark violet";

        /// <summary>蓝紫色</summary>
        public const string BlueViolet = "blue violet";

        /// <summary>靛蓝</summary>
        public const string Indigo = "indigo";

        /// <summary>梅红色</summary>
        public const string Plum = "plum";

        /// <summary>淡紫色</summary>
        public const string Lavender = "lavender";

        #endregion

        #region Color Collections

        /// <summary>
        /// 所有基础颜色
        /// </summary>
        public static readonly IReadOnlyList<string> BasicColors = new[]
        {
            Green, Red, Blue, Orange, Pink, Yellow, Cyan, Magenta, Coral
        };

        /// <summary>
        /// 所有灰度颜色
        /// </summary>
        public static readonly IReadOnlyList<string> GrayscaleColors = new[]
        {
            Black, DimGray, Gray, DarkGray, Silver, LightGray, White
        };

        /// <summary>
        /// 适合绘制的对比色序列
        /// </summary>
        public static readonly IReadOnlyList<string> ContrastColors = new[]
        {
            Green, Red, Blue, Orange, Cyan, Magenta, Yellow, Purple, Coral, SpringGreen
        };

        /// <summary>
        /// 暖色调
        /// </summary>
        public static readonly IReadOnlyList<string> WarmColors = new[]
        {
            Red, Orange, Yellow, Coral, Tomato, Gold, OrangeRed, Pink
        };

        /// <summary>
        /// 冷色调
        /// </summary>
        public static readonly IReadOnlyList<string> CoolColors = new[]
        {
            Blue, Cyan, Green, Teal, SkyBlue, SteelBlue, SeaGreen, SpringGreen
        };

        /// <summary>
        /// 状态颜色
        /// </summary>
        public static class Status
        {
            /// <summary>成功状态</summary>
            public const string Success = Green;

            /// <summary>警告状态</summary>
            public const string Warning = Orange;

            /// <summary>错误状态</summary>
            public const string Error = Red;

            /// <summary>信息状态</summary>
            public const string Info = Blue;

            /// <summary>禁用状态</summary>
            public const string Disabled = Gray;
        }

        #endregion

        #region Color Utilities

        private static readonly Dictionary<string, RgbColor> _colorMap = new Dictionary<string, RgbColor>(StringComparer.OrdinalIgnoreCase)
        {
            { Green, new RgbColor(0, 128, 0) },
            { Red, new RgbColor(255, 0, 0) },
            { Blue, new RgbColor(0, 0, 255) },
            { Orange, new RgbColor(255, 165, 0) },
            { Pink, new RgbColor(255, 192, 203) },
            { Yellow, new RgbColor(255, 255, 0) },
            { Cyan, new RgbColor(0, 255, 255) },
            { Magenta, new RgbColor(255, 0, 255) },
            { Coral, new RgbColor(255, 127, 80) },
            { Black, new RgbColor(0, 0, 0) },
            { White, new RgbColor(255, 255, 255) },
            { Gray, new RgbColor(128, 128, 128) },
            { Purple, new RgbColor(128, 0, 128) },
            { Violet, new RgbColor(238, 130, 238) },
            { Gold, new RgbColor(255, 215, 0) }
        };

        /// <summary>
        /// 从颜色名称获取 RGB 值
        /// </summary>
        /// <param name="colorName">颜色名称</param>
        /// <param name="rgb">输出的 RGB 值</param>
        /// <returns>是否找到对应颜色</returns>
        public static bool TryGetRgb(string colorName, out RgbColor rgb)
        {
            return _colorMap.TryGetValue(colorName, out rgb);
        }

        /// <summary>
        /// 从颜色名称获取 RGB 值
        /// </summary>
        public static RgbColor? GetRgb(string colorName)
        {
            if (_colorMap.TryGetValue(colorName, out var rgb))
                return rgb;
            return null;
        }

        /// <summary>
        /// 从 RGB 创建颜色字符串（用于 Halcon）
        /// </summary>
        public static string FromRgb(byte r, byte g, byte b)
        {
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        /// <summary>
        /// 从 HSV 创建颜色字符串
        /// </summary>
        /// <param name="h">色相 (0-360)</param>
        /// <param name="s">饱和度 (0-1)</param>
        /// <param name="v">明度 (0-1)</param>
        public static string FromHsv(double h, double s, double v)
        {
            var rgb = HsvToRgb(h, s, v);
            return FromRgb(rgb.R, rgb.G, rgb.B);
        }

        /// <summary>
        /// 获取循环颜色（用于多目标显示）
        /// </summary>
        /// <param name="index">索引</param>
        public static string GetCyclic(int index)
        {
            return ContrastColors[index % ContrastColors.Count];
        }

        /// <summary>
        /// 获取渐变颜色
        /// </summary>
        /// <param name="t">参数 (0-1)</param>
        /// <param name="fromColor">起始颜色</param>
        /// <param name="toColor">结束颜色</param>
        public static string GetGradient(double t, string fromColor, string toColor)
        {
            var from = GetRgb(fromColor) ?? new RgbColor(0, 0, 0);
            var to = GetRgb(toColor) ?? new RgbColor(255, 255, 255);

            t = MathHelper.Clamp01(t);
            byte r = (byte)(from.R + (to.R - from.R) * t);
            byte g = (byte)(from.G + (to.G - from.G) * t);
            byte b = (byte)(from.B + (to.B - from.B) * t);

            return FromRgb(r, g, b);
        }

        /// <summary>
        /// 获取热力图颜色 (蓝 -> 青 -> 绿 -> 黄 -> 红)
        /// </summary>
        /// <param name="t">参数 (0-1)，0=冷，1=热</param>
        public static string GetHeatmapColor(double t)
        {
            t = MathHelper.Clamp01(t);
            // 使用 HSV 色彩空间，从蓝(240°)到红(0°)
            double h = (1 - t) * 240;
            var rgb = HsvToRgb(h, 1.0, 1.0);
            return FromRgb(rgb.R, rgb.G, rgb.B);
        }

        /// <summary>
        /// HSV 转 RGB
        /// </summary>
        private static RgbColor HsvToRgb(double h, double s, double v)
        {
            h = h % 360;
            if (h < 0) h += 360;

            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            double m = v - c;

            double r1, g1, b1;

            if (h < 60) { r1 = c; g1 = x; b1 = 0; }
            else if (h < 120) { r1 = x; g1 = c; b1 = 0; }
            else if (h < 180) { r1 = 0; g1 = c; b1 = x; }
            else if (h < 240) { r1 = 0; g1 = x; b1 = c; }
            else if (h < 300) { r1 = x; g1 = 0; b1 = c; }
            else { r1 = c; g1 = 0; b1 = x; }

            return new RgbColor(
                (byte)((r1 + m) * 255),
                (byte)((g1 + m) * 255),
                (byte)((b1 + m) * 255)
            );
        }

        #endregion
    }
}
