using System;
using System.Collections.Generic;
using System.Windows.Forms;
using DotNet.Drawing;
using DotNet.Vision.Abstractions;


namespace DotNet.HalconUI
{
    /// <summary>
    /// <see cref="IParaUiHost"/> 的 WinForms 适配器：把「参数面板宿主控件 + VsControlModel 字典」
    /// 这一对总是同进同出的东西包成一个对象，交给算法层。
    /// </summary>
    /// <remarks>
    /// 原签名是 <c>DispPara(Control form, Dictionary&lt;string, VsControlModel&gt; VsControls)</c>，
    /// 两个参数必须成对传递且顺序固定，同时把 WinForms 类型泄漏进算法层。
    /// <para>
    /// 必需控件经 <see cref="GetRequired"/> 读取，缺失时抛出包含控件名的明确异常；
    /// 只有调用方显式提供 fallback 时才允许缺失并记录警告，避免静默覆盖有效配置。
    /// </para>
    /// </remarks>
    public sealed class WinFormsParaUiHost : IParaUiHost
    {
        private readonly Control _form;
        private readonly Dictionary<string, VsControlModel> _controls;

        public WinFormsParaUiHost(Control form, Dictionary<string, VsControlModel> controls)
        {
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _controls = controls ?? throw new ArgumentNullException(nameof(controls));
        }

        /// <summary> 底层控件字典，供 UI 层自身（非算法层）继续按老方式访问 </summary>
        public Dictionary<string, VsControlModel> Controls => _controls;

        #region 布局

        public void ShowTabs(params TabPageEnum[] tabsToShow) => _form.ShowTabs(tabsToShow);

        public void ClearAll() => _controls.ClearAll();

        #endregion

        #region 控件声明

        public void ShowLabel(string name, string text) => _controls.ShowLabel(_form, name, text);

        public void ShowButton(string name, bool visible) => _controls.ShowButton(_form, name, visible);

        public void ShowTextBox(string name, string text) => _controls.ShowTextBox(_form, name, text);

        public void ShowComboBox(string name, string text, bool enabled) => _controls.ShowComboBox(_form, name, text, enabled);

        public void ShowComboBoxList(string name, string text, string[] items) => _controls.ShowComboBoxList(_form, name, text, items);

        public void ShowComboBoxDropDown(string name, string text, string[] items) => _controls.ShowComboBoxDropDown(_form, name, text, items);

        public void ShowCheckBox(string name, string text, bool isChecked) => _controls.ShowCheckBox(_form, name, text, isChecked);

        public void ShowGroupBox(string name) => _controls.ShowGroupBox(_form, name);

        public void ShowRadioButton(string name, string text, bool visible, bool isChecked)
            => _controls.ShowRadioButton(_form, name, text, visible, isChecked);

        public void ShowTrackBar(string name, int value) => _controls.ShowTrackBar(_form, name, value);

        public void ShowTabPage(string name, string text, bool visible) => _controls.ShowTabPage(_form, name, text, visible);

        #endregion

        #region 读回

        public string GetString(string name) => GetRequired(name).AsString();

        public string GetString(string name, string fallback)
            => TryGet(name, out var model) ? model.AsString() : fallback;

        public bool GetBool(string name) => GetRequired(name).AsBool();

        public bool GetBool(string name, bool fallback)
            => TryGet(name, out var model) ? model.AsBool() : fallback;

        public int GetInt(string name) => GetRequired(name).AsInt();

        public int GetInt(string name, int fallback)
            => TryGet(name, out var model) ? model.AsInt() : fallback;

        public double GetDouble(string name) => GetRequired(name).AsDouble();

        public double GetDouble(string name, double fallback)
            => TryGet(name, out var model) ? model.AsDouble() : fallback;

        private VsControlModel GetRequired(string name)
        {
            if (_controls.TryGetValue(name, out var model)) return model;

            throw new InvalidOperationException(
                string.Format("参数面板上不存在控件 '{0}'。请检查 DispPara 与 SavePara 的控件名称是否一致。", name));
        }

        private bool TryGet(string name, out VsControlModel model)
        {
            if (_controls.TryGetValue(name, out model)) return true;

            Log.Warn("VsControl", string.Format("参数面板上不存在控件 '{0}'，本次读回使用显式默认值。", name));
            return false;
        }

        #endregion
    }
}
