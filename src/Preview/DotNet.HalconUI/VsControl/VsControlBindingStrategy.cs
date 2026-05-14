using System.Windows.Forms;


namespace DotNet.HalconUI
{
    public interface IVsControlBinding
    {
        void Bind(Form form, VsControlModel bindingSource);
        void Unbind(Form form, VsControlModel bindingSource);
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
        private static readonly IVsControlBinding _tabPage = new VsTabPageBindingStrategy();
        private static readonly IVsControlBinding _textBox = new VsTextBoxBindingStrategy();
        private static readonly IVsControlBinding _comboBox = new VsComboBoxBindingStrategy();
        private static readonly IVsControlBinding _checkBox = new VsCheckBoxBindingStrategy();
        private static readonly IVsControlBinding _radioButton = new VsRadioButtonBindingStrategy();
        private static readonly IVsControlBinding _trackBar = new VsTrackBarBindingStrategy();
        private static readonly IVsControlBinding _dataGridView = new VsDataGridViewBindingStrategy();
        private static readonly IVsControlBinding _null = new VsNullBindingStrategy();

        public static IVsControlBinding CreateStrategy(string controlType)
        {
            switch (controlType)
            {
                case VsControlTypes.TabPage: return _tabPage;
                case VsControlTypes.TextBox: return _textBox;
                case VsControlTypes.ComboBox: return _comboBox;
                case VsControlTypes.CheckBox: return _checkBox;
                case VsControlTypes.RadioButton: return _radioButton;
                case VsControlTypes.TrackBar: return _trackBar;
                case VsControlTypes.DataGridView: return _dataGridView;
                default: return _null;
            }
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
        /// 再添加新 Binding. 这一步本身已经能阻止 "DataBindings 累积".
        /// </summary>
        internal static void AddPropertyBinding(Control con, string controlProperty, VsControlModel vm, string vmProperty)
        {
            for (int i = con.DataBindings.Count - 1; i >= 0; i--)
            {
                if (con.DataBindings[i].PropertyName == controlProperty)
                    con.DataBindings.RemoveAt(i);
            }
            con.DataBindings.Add(controlProperty, vm, vmProperty, false, DataSourceUpdateMode.OnPropertyChanged);
        }

        /// <summary>
        /// 只移除 DataSource 为指定 VM 的 Binding.
        /// 调用时机: VM.Dispose. 这种 "按 source 精确移除" 的实现, 可以保证
        /// 即使 "new 新 VM (已 Bind) -> Replace 字典 -> Dispose 旧 VM" 的执行顺序下,
        /// 新 VM 刚加上的 Binding 也不会被误删 (旧 Binding 在 AddPropertyBinding 中已被同名移除).
        /// </summary>
        internal static void RemoveBindingsBySource(Control con, VsControlModel source)
        {
            if (con == null) return;
            for (int i = con.DataBindings.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(con.DataBindings[i].DataSource, source))
                    con.DataBindings.RemoveAt(i);
            }
        }
    }


    // ============================================================
    //  TabPage
    // ============================================================
    public sealed class VsTabPageBindingStrategy : IVsControlBinding
    {
        public void Bind(Form form, VsControlModel vm)
        {
            var con = (TabPage)form.GetControl(vm.Name);
            BindingHelper.AddPropertyBinding(con, "Text", vm, nameof(VsControlModel.Value));
        }

        public void Unbind(Form form, VsControlModel vm)
        {
            BindingHelper.RemoveBindingsBySource((TabPage)form.GetControl(vm.Name), vm);
        }
    }

    // ============================================================
    //  TextBox
    // ============================================================
    public sealed class VsTextBoxBindingStrategy : IVsControlBinding
    {
        public void Bind(Form form, VsControlModel vm)
        {
            var con = (TextBox)form.GetControl(vm.Name);
            BindingHelper.AddPropertyBinding(con, "Text", vm, nameof(VsControlModel.Value));
        }

        public void Unbind(Form form, VsControlModel vm)
        {
            BindingHelper.RemoveBindingsBySource((TextBox)form.GetControl(vm.Name), vm);
        }
    }

    // ============================================================
    //  ComboBox
    // ============================================================
    public sealed class VsComboBoxBindingStrategy : IVsControlBinding
    {
        public void Bind(Form form, VsControlModel vm)
        {
            var con = (ComboBox)form.GetControl(vm.Name);
            BindingHelper.AddPropertyBinding(con, "Text", vm, nameof(VsControlModel.Value));
        }

        public void Unbind(Form form, VsControlModel vm)
        {
            BindingHelper.RemoveBindingsBySource((ComboBox)form.GetControl(vm.Name), vm);
        }
    }

    // ============================================================
    //  CheckBox
    // ============================================================
    public sealed class VsCheckBoxBindingStrategy : IVsControlBinding
    {
        public void Bind(Form form, VsControlModel vm)
        {
            var con = (CheckBox)form.GetControl(vm.Name);
            BindingHelper.AddPropertyBinding(con, "Checked", vm, nameof(VsControlModel.Value));
        }

        public void Unbind(Form form, VsControlModel vm)
        {
            BindingHelper.RemoveBindingsBySource((CheckBox)form.GetControl(vm.Name), vm);
        }
    }

    // ============================================================
    //  RadioButton
    // ============================================================
    public sealed class VsRadioButtonBindingStrategy : IVsControlBinding
    {
        public void Bind(Form form, VsControlModel vm)
        {
            var con = (RadioButton)form.GetControl(vm.Name);
            BindingHelper.AddPropertyBinding(con, "Checked", vm, nameof(VsControlModel.Value));
        }

        public void Unbind(Form form, VsControlModel vm)
        {
            BindingHelper.RemoveBindingsBySource((RadioButton)form.GetControl(vm.Name), vm);
        }
    }

    // ============================================================
    //  TrackBar
    // ============================================================
    public sealed class VsTrackBarBindingStrategy : IVsControlBinding
    {
        public void Bind(Form form, VsControlModel vm)
        {
            var con = (TrackBar)form.GetControl(vm.Name);
            // 控件侧 TrackBar.Value (int) 与 VM 侧 Value (object/装箱 int) 通过 WinForms 反射绑定自动拆装箱.
            BindingHelper.AddPropertyBinding(con, "Value", vm, nameof(VsControlModel.Value));
        }

        public void Unbind(Form form, VsControlModel vm)
        {
            BindingHelper.RemoveBindingsBySource((TrackBar)form.GetControl(vm.Name), vm);
        }
    }

    // ============================================================
    //  DataGridView (当前未启用具体属性绑定, 保留 hook)
    // ============================================================
    public sealed class VsDataGridViewBindingStrategy : IVsControlBinding
    {
        public void Bind(Form form, VsControlModel vm) { }

        public void Unbind(Form form, VsControlModel vm)
        {
            BindingHelper.RemoveBindingsBySource((DataGridView)form.GetControl(vm.Name), vm);
        }
    }

    // ============================================================
    //  Null (兜底策略)
    // ============================================================
    public sealed class VsNullBindingStrategy : IVsControlBinding
    {
        public void Bind(Form form, VsControlModel vm) { }
        public void Unbind(Form form, VsControlModel vm) { }
    }
}
