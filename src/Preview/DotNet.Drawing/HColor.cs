using System;

namespace DotNet.Drawing
{
    /// <summary>
    /// HALCON 颜色。以颜色名 (HALCON <c>set_color</c> 接受的字符串) 为唯一状态的不可变值类型。
    /// </summary>
    /// <remarks>
    /// 原先是一组 <c>public const string</c>：颜色在类型系统里就是普通字符串，
    /// 任何拼错的字面量都要等到 HALCON 运行期才报错，编译器帮不上忙。
    /// <para>
    /// 改成 <c>readonly struct</c> 后，绘制 API 的形参写成 <see cref="HColor"/>，
    /// 传错类型编译期即拒绝；同时保留与 <see cref="string"/> 的双向隐式转换，
    /// 既有的 <c>DispXxx(..., "red")</c> 调用与把颜色存成字符串的配置/序列化代码都不用改。
    /// </para>
    /// <para>
    /// 传给 HALCON 时请用 <see cref="Name"/>：C# 不做连续隐式转换，
    /// <c>HColor -&gt; string -&gt; HTuple</c> 不会自动发生。
    /// </para>
    /// </remarks>
    public readonly struct HColor : IEquatable<HColor>
    {
        private readonly string _name;

        /// <summary>用颜色名构造。名称语义由 HALCON 解释，本类型不做白名单校验。</summary>
        public HColor(string name)
        {
            _name = name;
        }

        /// <summary>颜色名。未赋值的 <c>default(HColor)</c> 返回空字符串而非 null。</summary>
        public string Name => _name ?? string.Empty;

        /// <summary>是否为未指定颜色 (<c>default(HColor)</c> 或空名)。</summary>
        public bool IsEmpty => string.IsNullOrEmpty(_name);

        public static implicit operator HColor(string name) => new HColor(name);

        public static implicit operator string(HColor color) => color.Name;

        public bool Equals(HColor other) => string.Equals(Name, other.Name, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is HColor other && Equals(other);

        public override int GetHashCode() => Name.GetHashCode();

        public static bool operator ==(HColor left, HColor right) => left.Equals(right);

        public static bool operator !=(HColor left, HColor right) => !left.Equals(right);

        public override string ToString() => Name;

        #region Basic Colors

        /// <summary>绿色</summary>
        public static readonly HColor Green = new HColor("green");

        /// <summary>红色</summary>
        public static readonly HColor Red = new HColor("red");

        /// <summary>蓝色</summary>
        public static readonly HColor Blue = new HColor("blue");

        /// <summary>橙色</summary>
        public static readonly HColor Orange = new HColor("orange");

        /// <summary>粉色</summary>
        public static readonly HColor Pink = new HColor("pink");

        /// <summary>黄色</summary>
        public static readonly HColor Yellow = new HColor("yellow");

        /// <summary>青色</summary>
        public static readonly HColor Cyan = new HColor("cyan");

        /// <summary>品红</summary>
        public static readonly HColor Magenta = new HColor("magenta");

        /// <summary>珊瑚色</summary>
        public static readonly HColor Coral = new HColor("coral");

        #endregion

        #region Grayscale Colors

        /// <summary>黑色</summary>
        public static readonly HColor Black = new HColor("black");

        /// <summary>白色</summary>
        public static readonly HColor White = new HColor("white");

        /// <summary>灰色</summary>
        public static readonly HColor Gray = new HColor("gray");

        /// <summary>暗灰色</summary>
        public static readonly HColor DimGray = new HColor("dim gray");

        /// <summary>浅灰色</summary>
        public static readonly HColor LightGray = new HColor("light gray");

        /// <summary>深灰色</summary>
        public static readonly HColor DarkGray = new HColor("dark gray");

        /// <summary>银色</summary>
        public static readonly HColor Silver = new HColor("silver");

        #endregion

        #region Blue Variants

        /// <summary>军蓝色</summary>
        public static readonly HColor CadetBlue = new HColor("cadet blue");

        /// <summary>中灰蓝色</summary>
        public static readonly HColor MediumSlateBlue = new HColor("medium slate blue");

        /// <summary>灰蓝色</summary>
        public static readonly HColor SlateBlue = new HColor("slate blue");

        /// <summary>天蓝色</summary>
        public static readonly HColor SkyBlue = new HColor("sky blue");

        /// <summary>淡蓝色</summary>
        public static readonly HColor LightBlue = new HColor("light blue");

        /// <summary>深蓝色</summary>
        public static readonly HColor DarkBlue = new HColor("dark blue");

        /// <summary>海军蓝</summary>
        public static readonly HColor Navy = new HColor("navy");

        /// <summary>宝蓝色</summary>
        public static readonly HColor RoyalBlue = new HColor("royal blue");

        /// <summary>钢蓝色</summary>
        public static readonly HColor SteelBlue = new HColor("steel blue");

        /// <summary>道奇蓝</summary>
        public static readonly HColor DodgerBlue = new HColor("dodger blue");

        #endregion

        #region Green Variants

        /// <summary>春绿色</summary>
        public static readonly HColor SpringGreen = new HColor("spring green");

        /// <summary>暗橄榄绿</summary>
        public static readonly HColor DarkOliveGreen = new HColor("dark olive green");

        /// <summary>森林绿</summary>
        public static readonly HColor ForestGreen = new HColor("forest green");

        /// <summary>淡绿色</summary>
        public static readonly HColor LightGreen = new HColor("light green");

        /// <summary>深绿色</summary>
        public static readonly HColor DarkGreen = new HColor("dark green");

        /// <summary>草绿色</summary>
        public static readonly HColor LawnGreen = new HColor("lawn green");

        /// <summary>酸橙绿</summary>
        public static readonly HColor LimeGreen = new HColor("lime green");

        /// <summary>海绿色</summary>
        public static readonly HColor SeaGreen = new HColor("sea green");

        /// <summary>橄榄色</summary>
        public static readonly HColor Olive = new HColor("olive");

        /// <summary>青绿色</summary>
        public static readonly HColor Teal = new HColor("teal");

        #endregion

        #region Red Variants

        /// <summary>橙红色</summary>
        public static readonly HColor OrangeRed = new HColor("orange red");

        /// <summary>深红色</summary>
        public static readonly HColor DarkRed = new HColor("dark red");

        /// <summary>深红</summary>
        public static readonly HColor Crimson = new HColor("crimson");

        /// <summary>火砖红</summary>
        public static readonly HColor Firebrick = new HColor("firebrick");

        /// <summary>印度红</summary>
        public static readonly HColor IndianRed = new HColor("indian red");

        /// <summary>褐红色</summary>
        public static readonly HColor Maroon = new HColor("maroon");

        /// <summary>番茄红</summary>
        public static readonly HColor Tomato = new HColor("tomato");

        #endregion

        #region Yellow/Orange Variants

        /// <summary>金色</summary>
        public static readonly HColor Gold = new HColor("gold");

        /// <summary>浅黄色</summary>
        public static readonly HColor LightYellow = new HColor("light yellow");

        /// <summary>柠檬绿</summary>
        public static readonly HColor LemonChiffon = new HColor("lemon chiffon");

        /// <summary>卡其色</summary>
        public static readonly HColor Khaki = new HColor("khaki");

        /// <summary>深橙色</summary>
        public static readonly HColor DarkOrange = new HColor("dark orange");

        /// <summary>沙棕色</summary>
        public static readonly HColor SandyBrown = new HColor("sandy brown");

        /// <summary>桃色</summary>
        public static readonly HColor PeachPuff = new HColor("peach puff");

        #endregion

        #region Purple/Violet Variants

        /// <summary>紫色</summary>
        public static readonly HColor Purple = new HColor("purple");

        /// <summary>紫罗兰</summary>
        public static readonly HColor Violet = new HColor("violet");

        /// <summary>兰花紫</summary>
        public static readonly HColor Orchid = new HColor("orchid");

        /// <summary>深紫色</summary>
        public static readonly HColor DarkViolet = new HColor("dark violet");

        /// <summary>蓝紫色</summary>
        public static readonly HColor BlueViolet = new HColor("blue violet");

        /// <summary>靛蓝</summary>
        public static readonly HColor Indigo = new HColor("indigo");

        /// <summary>梅红色</summary>
        public static readonly HColor Plum = new HColor("plum");

        /// <summary>淡紫色</summary>
        public static readonly HColor Lavender = new HColor("lavender");

        #endregion

    }
}
