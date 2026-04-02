using HalconDotNet;

namespace DotNet.HWindows
{
    public abstract class HWindowFontBase
    {
        //窗体句柄
        protected readonly HWindow hWindow;

        public HWindowFontBase(HWindow hWindow)
        {
            this.hWindow = hWindow;
        }

        /// <summary>
        /// 设置显示窗体字体大小
        /// </summary>
        /// <param name="hv_Size"></param>
        /// <param name="font"></param>
        /// <param name="bold"></param>
        /// <param name="slant"></param>
        public abstract void SetFontSize(HTuple hv_Size, string font = "serif", string bold = "true", string slant = "false");

        /// <summary>
        /// 显示文本
        /// </summary>
        /// <param name="message"></param>
        /// <param name="hv_Row"></param>
        /// <param name="hv_Column"></param>
        /// <param name="color"></param>
        /// <param name="coordSystem"></param>
        public abstract void DispText(string message, HTuple hv_Row, HTuple hv_Column, string color, string coordSystem = "image");

        /// <summary>
        /// 设置字体和显示文本
        /// </summary>
        /// <param name="message"></param>
        /// <param name="hv_Row"></param>
        /// <param name="hv_Column"></param>
        /// <param name="hv_Size"></param>
        /// <param name="color"></param>
        public abstract void DispText(string message, HTuple hv_Row, HTuple hv_Column, HTuple hv_Size, string color);


        /// <summary>
        /// 设置字体和显示文本
        /// </summary>
        /// <param name="mesLines"></param>
        /// <param name="hv_Row"></param>
        /// <param name="hv_Column"></param>
        /// <param name="hv_Size"></param>
        /// <param name="color"></param>
        /// <param name="lineSpacing"></param>
        public abstract void DispText(string[] mesLines, HTuple hv_Row, HTuple hv_Column, HTuple hv_Size, string color, double lineSpacing = 2);
    }

    /*

    在选择使用接口(`interface`) 还是抽象类(`abstract class`) 时，需要考虑以下几点：

    1. ** 继承的需求**：  
       - 如果需要提供一些基础实现或共享代码，使用抽象类更合适，因为抽象类可以包含字段、属性和实现方法。
       - 如果你只是想定义行为契约，不需要实现任何方法，那么接口更合适。

    2. ** 灵活性**：  
       - 抽象类强制所有子类必须继承自一个基类，这在某些情况下可能限制了灵活性。
       - 接口允许一个类实现多个接口，提供了更大的灵活性。

    3. ** 扩展性**：  
       - 如果将来可能需要为类添加更多功能，并且这些功能可能包含一些默认实现，抽象类更为合适。
       - 如果类层次结构需要很简单且只关心行为契约，接口可能是更好的选择。

    ### 针对你的具体情况

    在你的代码中，`HWindowFontBase` 作为所有具体字体处理类的基类，其主要作用是：

    1. 提供构造函数，用于初始化 `HWindow` 字段。
    2. 定义了 `SetFontSize` 和 `DispText` 方法，这些方法是所有具体类必须实现的行为。

    ### 推荐选择

    ** 使用抽象类** 更加合适，因为：

    - 你需要在基类中共享 `HWindow` 字段并通过构造函数初始化它。这是抽象类的优势。
    - 如果将来需要在基类中添加一些共享的实现（如常用的工具方法或默认行为），抽象类能够更好地支持这些需求。

    ### 结论

    保留当前的抽象类设计更为合理。`HWindowFontBase` 可以继续作为抽象基类来定义和共享基础功能，同时强制子类实现必要的方法。

    */

}
