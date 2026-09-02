namespace DotNet.Vision.Abstractions
{
    /// <summary>
    /// 参数面板宿主：策略声明「要显示哪些控件」以及「从控件读回什么值」的唯一入口。
    /// </summary>
    /// <remarks>
    /// 取代原来的 <c>DispPara(Control form, Dictionary&lt;string, VsControlModel&gt; VsControls)</c>
    /// 双参数签名，它把 WinForms 的 <c>Control</c> 和 UI 层的 <c>VsControlModel</c> 一起
    /// 泄漏进算法层。这里只暴露与控件框架无关的动作，WinForms 适配器留在 UI 层。
    /// <para>
    /// 必需控件使用单参数 <c>GetXxx(name)</c>，缺失时立即抛出带控件名的明确异常；
    /// 只有调用方明确接受控件缺失时，才使用带 fallback 的重载。
    /// </para>
    /// </remarks>
    public interface IParaUiHost
    {
        #region 布局

        /// <summary> 按枚举顺序重建可见的标签页集合 </summary>
        void ShowTabs(params TabPageEnum[] tabsToShow);

        /// <summary> 隐藏并解绑当前面板上的全部控件 </summary>
        void ClearAll();

        #endregion

        #region 控件声明

        void ShowLabel(string name, string text);
        void ShowButton(string name, bool visible);
        void ShowTextBox(string name, string text);
        void ShowComboBox(string name, string text, bool enabled);
        void ShowComboBoxList(string name, string text, string[] items);
        void ShowComboBoxDropDown(string name, string text, string[] items);
        void ShowCheckBox(string name, string text, bool isChecked);
        void ShowGroupBox(string name);
        void ShowRadioButton(string name, string text, bool visible, bool isChecked);
        void ShowTrackBar(string name, int value);
        void ShowTabPage(string name, string text, bool visible);

        #endregion

        #region 读回

        /// <summary> 读回必需字符串；控件不存在时抛出异常 </summary>
        string GetString(string name);

        /// <summary> 读回可选字符串；控件不存在时返回 <paramref name="fallback"/> </summary>
        string GetString(string name, string fallback);

        /// <summary> 读回必需布尔值；控件不存在时抛出异常 </summary>
        bool GetBool(string name);

        /// <summary> 读回可选布尔值；控件不存在时返回 <paramref name="fallback"/> </summary>
        bool GetBool(string name, bool fallback);

        /// <summary> 读回必需整数；控件不存在时抛出异常 </summary>
        int GetInt(string name);

        /// <summary> 读回可选整数；控件不存在时返回 <paramref name="fallback"/> </summary>
        int GetInt(string name, int fallback);

        /// <summary> 读回必需浮点数；控件不存在时抛出异常 </summary>
        double GetDouble(string name);

        /// <summary> 读回可选浮点数；控件不存在时返回 <paramref name="fallback"/> </summary>
        double GetDouble(string name, double fallback);

        #endregion
    }
}
