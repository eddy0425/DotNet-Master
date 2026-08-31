using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace DotNet.HalconUI
{
    /// <summary>
    /// 控件视图模型 (ViewModel). 承载 WinForms 控件的可绑定状态.
    /// 设计要点:
    /// <list type="bullet">
    /// <item>WinForms 的 DataBindings 会通过 Binding.DataSource 强引用 ViewModel,
    ///       必须在不再需要时调用 <see cref="Dispose"/> 解除绑定, 否则 VM 永远无法被 GC.</item>
    /// <item>所有字段赋值完成之后, 才在构造函数末尾调用 Bind, 避免 "先 Bind 后赋值" 把控件原值反向同步成 default.</item>
    /// <item><see cref="Value"/> 以 object 统一承载控件主属性: TextBox/TabPage/ComboBox 为 string,
    ///       CheckBox/RadioButton 为 bool, TrackBar 为 int. 类型由构造函数和绑定策略共同保证.</item>
    /// </list>
    /// </summary>
    public sealed class VsControlModel : INotifyPropertyChanged, IDisposable
    {
        private readonly Control _form;
        private bool _disposed;

        private object _value;
        // 控件主属性: TabPage/TextBox/ComboBox -> string, CheckBox/RadioButton -> bool, TrackBar -> int.
        public object Value { get { return _value; } set { SetField(ref _value, value); } }
        public string Name { get; }
        public string Type { get; }

        // 以下三个外观属性同样参与 DataBindings, 因此必须走 SetField 发出 PropertyChanged.
        // 原实现是自动属性(无通知), 绑定建立后在代码里改它们, 控件不会有任何反应.
        private bool _visible;
        private bool _enabled;
        private bool _dropDownStyle;

        /// <summary>控件可见性. 绑定到 Control.Visible (TabPage 除外, 见 VsTabPageBindingStrategy).</summary>
        public bool Visible { get { return _visible; } set { SetField(ref _visible, value); } }

        /// <summary>控件可用性. 绑定到 Control.Enabled (TabPage 除外).</summary>
        public bool Enabled { get { return _enabled; } set { SetField(ref _enabled, value); } }

        /// <summary>
        /// ComboBox 是否为只读下拉 (true = <see cref="ComboBoxStyle.DropDownList"/>,
        /// false = <see cref="ComboBoxStyle.DropDown"/>). 仅对 ComboBox 有意义.
        /// </summary>
        public bool DropDownStyle { get { return _dropDownStyle; } set { SetField(ref _dropDownStyle, value); } }

        // Items 防御性拷贝, 避免外部数组在 VM 生命期内被改写.
        private string[]? _items;
        public string[]? Items
        {
            get { return _items == null ? null : (string[])_items.Clone(); }
            set { _items = value == null ? null : (string[])value.Clone(); }
        }

        // 绑定时由策略写入的 Control 引用. 让 Dispose 时直接解绑, 不必再走 Form 反射,
        // 这样即便 Form 已经先一步 Dispose 也能安全解绑.
        private Control? _boundControl;
        internal Control? BoundControl { get { return _boundControl; } }
        internal void AttachControl(Control control) { _boundControl = control; }


        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            OnPropertyChanged(propertyName);
        }

        /// <summary>TrackBar</summary>
        public VsControlModel(Control form, string name, string type, int value)
            : this(form, name, type)
        {
            _value = value;
            BindToControl();
        }

        // 说明: 所有构造函数都在 BindToControl() 之前把 _visible/_enabled 置为控件当前的实际状态
        // (VsControlFactory 在 new VM 之前已经设置好 con.Visible/Enabled), 保证首次绑定不会改变界面。

        /// <summary>TabPage / TextBox</summary>
        public VsControlModel(Control form, string name, string type, string text, bool visible)
            : this(form, name, type)
        {
            _value = text;
            _visible = visible;
            BindToControl();
        }

        /// <summary>CheckBox / RadioButton. text 仅用于控件外观, 不进入 VM 状态.</summary>
        public VsControlModel(Control form, string name, string type, string text, bool visible, bool @checked)
            : this(form, name, type)
        {
            _value = @checked;
            _visible = visible;
            BindToControl();
        }

        /// <summary>ComboBox</summary>
        public VsControlModel(Control form, string name, string type, string text, bool visible, bool enabled, bool dropDownStyle, string[]? items)
            : this(form, name, type)
        {
            _value = text;
            _visible = visible;
            _enabled = enabled;
            _dropDownStyle = dropDownStyle;
            _items = items == null ? null : (string[])items.Clone();
            BindToControl();
        }

        private VsControlModel(Control form, string name, string type)
        {
            // 默认可见可用: TrackBar 等未显式传值的构造路径依赖这个默认, 否则绑定建立时会把控件藏掉。
            _visible = true;
            _enabled = true;

            if (form == null) throw new ArgumentNullException(nameof(form));
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(type)) throw new ArgumentNullException(nameof(type));

            _form = form;
            Name = name;
            Type = type;
        }

        /// <summary>
        /// 必须在所有字段赋值之后再绑定: 否则 Bind 会以 default 值反向同步控件, 丢掉控件上的原始内容.
        /// </summary>
        private void BindToControl()
        {
            VsControlBindingStrategyFactory.GetStrategy(Type).Bind(_form, this);
        }

        /// <summary>读取字符串. <see cref="Value"/> 为 null 或非 string 时返回 <see cref="string.Empty"/>.</summary>
        public string AsString() { return _value as string ?? string.Empty; }

        /// <summary>读取布尔. <see cref="Value"/> 不是 bool (含 null) 时返回 false.</summary>
        public bool AsBool() { return _value is bool b && b; }

        /// <summary>读取 Int32. null 返回 0, 其余经 <see cref="Convert.ToInt32(object)"/> 转换 (装箱整数 / 可解析字符串).</summary>
        public int AsInt() { return _value == null ? 0 : Convert.ToInt32(_value); }

        /// <summary>读取 Int64. null 返回 0, 其余经 <see cref="Convert.ToInt64(object)"/> 转换.</summary>
        public long AsInt64() { return _value == null ? 0L : Convert.ToInt64(_value); }

        /// <summary>读取 Double. null 返回 0, 其余经 <see cref="Convert.ToDouble(object)"/> 转换.</summary>
        public double AsDouble() { return _value == null ? 0d : Convert.ToDouble(_value); }

        /// <summary>读取 Single. null 返回 0, 其余经 <see cref="Convert.ToSingle(object)"/> 转换.</summary>
        public float AsFloat() { return _value == null ? 0f : Convert.ToSingle(_value); }

        /// <summary>
        /// 解除与控件的 DataBindings 强引用, 释放 VM 自身. 多次调用安全.
        /// 优先用 Bind 时缓存的 Control 解绑, 避免在 Form 已 Dispose 后再走反射出错.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                VsControlBindingStrategyFactory.GetStrategy(Type).Unbind(_form, this);
            }
            catch
            {
                // 控件可能在 Form Disposed 之后才被释放, 静默忽略, 避免影响 ClearAll 全流程.
            }
            _boundControl = null;
            PropertyChanged = null;
        }
    }
}
