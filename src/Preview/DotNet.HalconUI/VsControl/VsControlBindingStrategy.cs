using System;
using System.Windows.Forms;
using System.Collections.Generic;


namespace DotNet.HalconUI
{
    public interface IVsControlBinding
    {
        void Bind(Control form, VsControlModel bindingSource);
        void Unbind(Control form, VsControlModel bindingSource);
    }


    /// <summary>
    /// 支持的控件类型常量. 集中管理可避免 switch 中拼写错误走兜底策略.
    /// </summary>
    internal static class VsControlTypes
    {
        public const string TabPage = "TabPage";
        public const string TextBox = "TextBox";
        public const string ComboBox = "ComboBox";
        public const string CheckBox = "CheckBox";
        public const string RadioButton = "RadioButton";
        public const string TrackBar = "TrackBar";
        public const string DataGridView = "DataGridView";
    }


    public static class VsControlBindingStrategyFactory
    {
        // 单例策略集合: 所有策略类是无状态的, 全进程共用一份即可.
        private static readonly Dictionary<string, IVsControlBinding> _strategies =
            new Dictionary<string, IVsControlBinding>(StringComparer.Ordinal)
            {
                { VsControlTypes.TabPage, new VsTabPageBindingStrategy() },
                { VsControlTypes.TextBox, new VsTextBoxBindingStrategy() },
                { VsControlTypes.ComboBox, new VsComboBoxBindingStrategy() },
                { VsControlTypes.CheckBox, new VsCheckBoxBindingStrategy() },
                { VsControlTypes.RadioButton, new VsRadioButtonBindingStrategy() },
                { VsControlTypes.TrackBar, new VsTrackBarBindingStrategy() },
                { VsControlTypes.DataGridView, new VsDataGridViewBindingStrategy() },
            };
        private static readonly IVsControlBinding _null = new VsNullBindingStrategy();

        /// <summary>
        /// 根据控件类型获取对应策略 (单例). 未知类型返回 NullStrategy, 保持调用方无 null 防御.
        /// </summary>
        public static IVsControlBinding GetStrategy(string controlType)
        {
            if (controlType == null) return _null;
            IVsControlBinding s;
            return _strategies.TryGetValue(controlType, out s) ? s : _null;
        }

    }


    /// <summary>
    /// DataBindings 操作辅助.
    /// 关键不变量: 同一控件的同一属性最多只能存在一条 Binding,
    /// 否则会出现 "重复 Add 累积 -> VM 永不回收 + 属性变化触发多份 VM" 的内存泄漏.
    /// </summary>
    internal static class BindingHelper
    {
        /// <summary>
        /// 添加/替换一条属性绑定. 先移除控件上 PropertyName 相同的旧 Binding (无论 source 是谁),
        /// 再添加新 Binding. 同时把 Control 缓存到 VM, 供 Dispose 时复用.
        /// </summary>
        internal static void AddPropertyBinding(Control con, string controlProperty, VsControlModel vm, string vmProperty)
        {
            AddPropertyBinding(con, controlProperty, vm, vmProperty, null);
        }

        /// <summary>
        /// 同上, 额外允许在 Add 之前配置 Binding (例如挂 Format/Parse 做类型转换).
        /// </summary>
        internal static void AddPropertyBinding(Control con, string controlProperty, VsControlModel vm, string vmProperty, Action<Binding>? configure)
        {
            for (int i = con.DataBindings.Count - 1; i >= 0; i--)
            {
                if (con.DataBindings[i].PropertyName == controlProperty)
                    con.DataBindings.RemoveAt(i);
            }
            var binding = new Binding(controlProperty, vm, vmProperty, false, DataSourceUpdateMode.OnPropertyChanged);
            if (configure != null) configure(binding);
            con.DataBindings.Add(binding);
            vm.AttachControl(con);
        }

        /// <summary>
        /// 只移除 DataSource 为指定 VM 的 Binding.
        /// 调用时机: VM.Dispose. 这种 "按 source 精确移除" 的实现, 可以保证
        /// 即使 "new 新 VM (已 Bind) -> Replace 字典 -> Dispose 旧 VM" 的执行顺序下,
        /// 新 VM 刚加上的 Binding 也不会被误删 (旧 Binding 在 AddPropertyBinding 中已被同名移除).
        /// </summary>
        internal static void RemoveBindingsBySource(Control? con, VsControlModel source)
        {
            if (con == null) return;
            for (int i = con.DataBindings.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(con.DataBindings[i].DataSource, source))
                    con.DataBindings.RemoveAt(i);
            }
        }
    }


    /// <summary>
    /// 单属性双向绑定策略的泛型基类. 把 "反射查找 Control -> 加绑定" 和 "复用缓存 Control -> 解绑定"
    /// 这两段重复模板代码集中到这里, 子类只声明控件类型与控件侧属性名.
    /// </summary>
    public abstract class VsBindingStrategyBase<TControl> : IVsControlBinding where TControl : Control
    {
        /// <summary>控件侧被绑定的属性名 (e.g. "Text" / "Checked" / "Value").</summary>
        protected abstract string ControlPropertyName { get; }

        /// <summary>
        /// 是否把 <see cref="VsControlModel.Visible"/> 绑定到 Control.Visible.
        /// 默认 false: 建立 Binding 时 WinForms 会立刻把 VM 侧的值推给控件, 只有 VsControlFactory
        /// 在 new VM 之前确实按同一个值设置过 con.Visible 的控件类型才能安全开启, 否则会意外隐藏控件.
        /// </summary>
        protected virtual bool BindsVisible { get { return false; } }

        /// <summary>
        /// 是否把 <see cref="VsControlModel.Enabled"/> 绑定到 Control.Enabled. 约束同 <see cref="BindsVisible"/>.
        /// </summary>
        protected virtual bool BindsEnabled { get { return false; } }

        public virtual void Bind(Control form, VsControlModel vm)
        {
            var con = (TControl)form.GetControl(vm.Name);
            BindingHelper.AddPropertyBinding(con, ControlPropertyName, vm, nameof(VsControlModel.Value));
            if (BindsVisible)
                BindingHelper.AddPropertyBinding(con, "Visible", vm, nameof(VsControlModel.Visible));
            if (BindsEnabled)
                BindingHelper.AddPropertyBinding(con, "Enabled", vm, nameof(VsControlModel.Enabled));
        }

        /// <summary>
        /// 优先复用 Bind 时缓存的 Control, 避免在 Form 已 Dispose 后反射抛异常.
        /// 兜底再做一次反射, 保持 "VM 没被 Bind 过也能安全 Unbind" 的契约.
        /// </summary>
        public virtual void Unbind(Control form, VsControlModel vm)
        {
            var con = vm.BoundControl as TControl;
            if (con == null && form != null)
            {
                con = (TControl)form.GetControl(vm.Name);
            }
            BindingHelper.RemoveBindingsBySource(con, vm);
        }
    }


    /// <summary>
    /// TabPage 刻意不绑定 Visible/Enabled: WinForms 下 TabPage.Visible 由所属 TabControl 接管,
    /// 直接赋值不会真正隐藏页签 (要增删 TabPages, 见 VsControlFactory.ShowTabs).
    /// </summary>
    public sealed class VsTabPageBindingStrategy : VsBindingStrategyBase<TabPage>
    {
        protected override string ControlPropertyName { get { return "Text"; } }
    }

    public sealed class VsTextBoxBindingStrategy : VsBindingStrategyBase<TextBox>
    {
        protected override string ControlPropertyName { get { return "Text"; } }
        protected override bool BindsVisible { get { return true; } }
    }

    public sealed class VsComboBoxBindingStrategy : VsBindingStrategyBase<ComboBox>
    {
        protected override string ControlPropertyName { get { return "Text"; } }
        protected override bool BindsVisible { get { return true; } }
        protected override bool BindsEnabled { get { return true; } }

        public override void Bind(Control form, VsControlModel vm)
        {
            base.Bind(form, vm);

            // VM 侧用 bool 表达下拉样式 (true = 只读下拉), 控件侧是 ComboBoxStyle 枚举,
            // 因此这条绑定必须带 Format/Parse 做双向转换, 否则 WinForms 会抛类型转换异常.
            var con = (ComboBox)form.GetControl(vm.Name);
            BindingHelper.AddPropertyBinding(con, "DropDownStyle", vm, nameof(VsControlModel.DropDownStyle),
                b =>
                {
                    b.Format += (s, e) =>
                    {
                        e.Value = (e.Value is bool flag && flag) ? ComboBoxStyle.DropDownList : ComboBoxStyle.DropDown;
                    };
                    b.Parse += (s, e) =>
                    {
                        e.Value = (e.Value is ComboBoxStyle style) && style == ComboBoxStyle.DropDownList;
                    };
                });
        }
    }

    public sealed class VsCheckBoxBindingStrategy : VsBindingStrategyBase<CheckBox>
    {
        protected override string ControlPropertyName { get { return "Checked"; } }
        protected override bool BindsVisible { get { return true; } }
    }

    public sealed class VsRadioButtonBindingStrategy : VsBindingStrategyBase<RadioButton>
    {
        protected override string ControlPropertyName { get { return "Checked"; } }
        protected override bool BindsVisible { get { return true; } }
    }

    public sealed class VsTrackBarBindingStrategy : VsBindingStrategyBase<TrackBar>
    {
        // 控件侧 TrackBar.Value (int) 与 VM 侧 Value (object/装箱 int) 通过 WinForms 反射绑定自动拆装箱.
        protected override string ControlPropertyName { get { return "Value"; } }
    }


    // ============================================================
    //  DataGridView (当前未启用具体属性绑定, 保留 hook).
    //  不继承泛型基类, 因为 Bind 不需要建立绑定, 仅占位.
    // ============================================================
    public sealed class VsDataGridViewBindingStrategy : IVsControlBinding
    {
        public void Bind(Control form, VsControlModel vm) { }

        public void Unbind(Control form, VsControlModel vm)
        {
            var con = vm.BoundControl as DataGridView;
            if (con == null && form != null)
            {
                con = (DataGridView)form.GetControl(vm.Name);
            }
            BindingHelper.RemoveBindingsBySource(con, vm);
        }
    }


    // ============================================================
    //  Null (兜底策略)
    // ============================================================
    public sealed class VsNullBindingStrategy : IVsControlBinding
    {
        public void Bind(Control form, VsControlModel vm) { }
        public void Unbind(Control form, VsControlModel vm) { }
    }
}
