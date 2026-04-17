using System.Windows.Forms;

namespace DotNet.VisionMaster
{
    public interface IVsControlBinding
    {
        void Bind(Form form, VsControlModel bindingSource);
        void Unbind(Form form, VsControlModel bindingSource);
    }

    public static class VsControlBindingStrategyFactory
    {
        private static readonly VsTabPageBindingStrategy _tabPageBindingStrategy = new VsTabPageBindingStrategy();
        //private static readonly VsLabelBindingStrategy _labelBindingStrategy = new VsLabelBindingStrategy();
        //private static readonly VsButtonBindingStrategy _buttonBindingStrategy = new VsButtonBindingStrategy();
        private static readonly VsTextBoxBindingStrategy _textBoxBindingStrategy = new VsTextBoxBindingStrategy();
        private static readonly VsComboBoxBindingStrategy _comboBoxBindingStrategy = new VsComboBoxBindingStrategy();
        private static readonly VsCheckBoxBindingStrategy _checkBoxBindingStrategy = new VsCheckBoxBindingStrategy();
        private static readonly VsRadioButtonBindingStrategy _radioButtonBindingStrategy = new VsRadioButtonBindingStrategy();
        private static readonly VsTrackBarBindingStrategy _trackBarBindingStrategy = new VsTrackBarBindingStrategy();
        private static readonly VsDataGridViewBindingStrategy _dataGridViewBindingStrategy = new VsDataGridViewBindingStrategy();
        private static readonly VsNullBindingStrategy _nullBindingStrategy = new VsNullBindingStrategy();

        public static IVsControlBinding CreateStrategy(string controlType)
        {
            switch (controlType)
            {
                case "TabPage":       return _tabPageBindingStrategy;
                //case "Label":         return _labelBindingStrategy;
                //case "Button":        return _buttonBindingStrategy;
                case "TextBox":       return _textBoxBindingStrategy;
                case "ComboBox":      return _comboBoxBindingStrategy;
                case "CheckBox":      return _checkBoxBindingStrategy;
                case "RadioButton":   return _radioButtonBindingStrategy;
                case "TrackBar":      return _trackBarBindingStrategy;
                case "DataGridView":  return _dataGridViewBindingStrategy;
                default:              return _nullBindingStrategy;
            }
        }
    }

    // ============================================================
    //  通用 Unbind 辅助：解除 DataBindings + 移除 BoundHandler
    // ============================================================
    internal static class BindingHelper
    {
        /// <summary>
        /// 安全移除 BoundHandler 委托（Bind 时存储的同一实例）
        /// </summary>
        public static void RemoveBoundHandler(VsControlModel bindingSource)
        {
            if (bindingSource.BoundHandler != null)
            {
                bindingSource.PropertyChanged -= bindingSource.BoundHandler;
                bindingSource.BoundHandler = null;
            }
        }
    }

    // ============================================================
    //  TabPage
    // ============================================================
    public class VsTabPageBindingStrategy : IVsControlBinding
    {
        public void Bind(Form form, VsControlModel bindingSource)
        {
            var con = (TabPage)form.GetControl(bindingSource.Name);
            con.DataBindings.Add("Text", bindingSource, nameof(VsControlModel.Text), false, DataSourceUpdateMode.OnPropertyChanged);

            //bindingSource.BoundHandler = (sender, args) =>
            //{
            //    if (args.PropertyName == nameof(VsControlModel.Visible))
            //        con.Visible = bindingSource.Visible;
            //};
            //bindingSource.PropertyChanged += bindingSource.BoundHandler;
        }

        public void Unbind(Form form, VsControlModel bindingSource)
        {
            var con = (TabPage)form.GetControl(bindingSource.Name);
            con.DataBindings.Clear();
            BindingHelper.RemoveBoundHandler(bindingSource);
        }
    }

    // ============================================================
    //  Label
    // ============================================================
    public class VsLabelBindingStrategy : IVsControlBinding
    {
        public void Bind(Form form, VsControlModel bindingSource)
        {
            //var con = (Label)form.GetControl(bindingSource.Name);
            //con.DataBindings.Add("Text", bindingSource, nameof(VsControlModel.Text), false, DataSourceUpdateMode.OnPropertyChanged);

            //bindingSource.BoundHandler = (sender, args) =>
            //{
            //    if (args.PropertyName == nameof(VsControlModel.Visible))
            //        con.Visible = bindingSource.Visible;
            //    else if (args.PropertyName == nameof(VsControlModel.Enabled))
            //        con.Enabled = bindingSource.Enabled;
            //};
            //bindingSource.PropertyChanged += bindingSource.BoundHandler;
        }

        public void Unbind(Form form, VsControlModel bindingSource)
        {
            //var con = (Label)form.GetControl(bindingSource.Name);
            //con.DataBindings.Clear();
            //BindingHelper.RemoveBoundHandler(bindingSource);
        }
    }

    // ============================================================
    //  Button
    // ============================================================
    public class VsButtonBindingStrategy : IVsControlBinding
    {
        public void Bind(Form form, VsControlModel bindingSource)
        {
            //var con = (Button)form.GetControl(bindingSource.Name);
            //con.DataBindings.Add("Text", bindingSource, nameof(VsControlModel.Text), false, DataSourceUpdateMode.OnPropertyChanged);

            //bindingSource.BoundHandler = (sender, args) =>
            //{
            //    if (args.PropertyName == nameof(VsControlModel.Visible))
            //        con.Visible = bindingSource.Visible;
            //};
            //bindingSource.PropertyChanged += bindingSource.BoundHandler;
        }

        public void Unbind(Form form, VsControlModel bindingSource)
        {
            //var con = (Button)form.GetControl(bindingSource.Name);
            //con.DataBindings.Clear();
            //BindingHelper.RemoveBoundHandler(bindingSource);
        }
    }

    // ============================================================
    //  TextBox
    // ============================================================
    public class VsTextBoxBindingStrategy : IVsControlBinding
    {
        public void Bind(Form form, VsControlModel bindingSource)
        {
            var con = (TextBox)form.GetControl(bindingSource.Name);
            con.DataBindings.Add("Text", bindingSource, nameof(VsControlModel.Text), false, DataSourceUpdateMode.OnPropertyChanged);

            //bindingSource.BoundHandler = (sender, args) =>
            //{
            //    if (args.PropertyName == nameof(VsControlModel.Visible))
            //        con.Visible = bindingSource.Visible;
            //    else if (args.PropertyName == nameof(VsControlModel.Enabled))
            //        con.Enabled = bindingSource.Enabled;
            //};
            //bindingSource.PropertyChanged += bindingSource.BoundHandler;
        }

        public void Unbind(Form form, VsControlModel bindingSource)
        {
            var con = (TextBox)form.GetControl(bindingSource.Name);
            con.DataBindings.Clear();
            BindingHelper.RemoveBoundHandler(bindingSource);
        }
    }

    // ============================================================
    //  ComboBox
    // ============================================================
    public class VsComboBoxBindingStrategy : IVsControlBinding
    {
        public void Bind(Form form, VsControlModel bindingSource)
        {
            var con = (ComboBox)form.GetControl(bindingSource.Name);
            con.DataBindings.Add("Text", bindingSource, nameof(VsControlModel.Text), false, DataSourceUpdateMode.OnPropertyChanged);

            //bindingSource.BoundHandler = (sender, args) =>
            //{
            //    if (args.PropertyName == nameof(VsControlModel.Visible))
            //        con.Visible = bindingSource.Visible;
            //    else if (args.PropertyName == nameof(VsControlModel.Enabled))
            //        con.Enabled = bindingSource.Enabled;
            //    else if (args.PropertyName == nameof(VsControlModel.DropDownStyle))
            //        con.DropDownStyle = bindingSource.DropDownStyle ? ComboBoxStyle.DropDownList : ComboBoxStyle.DropDown;
            //    else if (args.PropertyName == nameof(VsControlModel.Items))
            //    {
            //        if (con.DropDownStyle == ComboBoxStyle.DropDownList)
            //        {
            //            con.Items.Clear();
            //            con.Items.AddRange(bindingSource.Items);
            //        }
            //    }
            //};
            //bindingSource.PropertyChanged += bindingSource.BoundHandler;
        }

        public void Unbind(Form form, VsControlModel bindingSource)
        {
            var con = (ComboBox)form.GetControl(bindingSource.Name);
            con.DataBindings.Clear();
            BindingHelper.RemoveBoundHandler(bindingSource);
        }
    }

    // ============================================================
    //  CheckBox
    // ============================================================
    public class VsCheckBoxBindingStrategy : IVsControlBinding
    {
        public void Bind(Form form, VsControlModel bindingSource)
        {
            var con = (CheckBox)form.GetControl(bindingSource.Name);
            con.DataBindings.Add("Checked", bindingSource, nameof(VsControlModel.Checked), false, DataSourceUpdateMode.OnPropertyChanged);

            //bindingSource.BoundHandler = (sender, args) =>
            //{
            //    if (args.PropertyName == nameof(VsControlModel.Text))
            //        con.Text = bindingSource.Text;
            //    else if (args.PropertyName == nameof(VsControlModel.Visible))
            //        con.Visible = bindingSource.Visible;
            //};
            //bindingSource.PropertyChanged += bindingSource.BoundHandler;
        }

        public void Unbind(Form form, VsControlModel bindingSource)
        {
            var con = (CheckBox)form.GetControl(bindingSource.Name);
            con.DataBindings.Clear();
            BindingHelper.RemoveBoundHandler(bindingSource);
        }
    }

    // ============================================================
    //  RadioButton
    // ============================================================
    public class VsRadioButtonBindingStrategy : IVsControlBinding
    {
        public void Bind(Form form, VsControlModel bindingSource)
        {
            var con = (RadioButton)form.GetControl(bindingSource.Name);
            con.DataBindings.Add("Checked", bindingSource, nameof(VsControlModel.Checked), false, DataSourceUpdateMode.OnPropertyChanged);

            //bindingSource.BoundHandler = (sender, args) =>
            //{
            //    if (args.PropertyName == nameof(VsControlModel.Text))
            //        con.Text = bindingSource.Text;
            //    else if (args.PropertyName == nameof(VsControlModel.Visible))
            //        con.Visible = bindingSource.Visible;
            //};
            //bindingSource.PropertyChanged += bindingSource.BoundHandler;
        }

        public void Unbind(Form form, VsControlModel bindingSource)
        {
            var con = (RadioButton)form.GetControl(bindingSource.Name);
            con.DataBindings.Clear();
            BindingHelper.RemoveBoundHandler(bindingSource);
        }
    }

    // ============================================================
    //  TrackBar
    // ============================================================
    public class VsTrackBarBindingStrategy : IVsControlBinding
    {
        public void Bind(Form form, VsControlModel bindingSource)
        {
            var con = (TrackBar)form.GetControl(bindingSource.Name);
            con.DataBindings.Add("Value", bindingSource, nameof(VsControlModel.Value), false, DataSourceUpdateMode.OnPropertyChanged);
        }

        public void Unbind(Form form, VsControlModel bindingSource)
        {
            var con = (TrackBar)form.GetControl(bindingSource.Name);
            con.DataBindings.Clear();
            BindingHelper.RemoveBoundHandler(bindingSource);
        }
    }

    // ============================================================
    //  DataGridView
    // ============================================================
    public class VsDataGridViewBindingStrategy : IVsControlBinding
    {
        public void Bind(Form form, VsControlModel bindingSource)
        {
            var con = (DataGridView)form.GetControl(bindingSource.Name);

            //bindingSource.BoundHandler = (sender, args) =>
            //{
            //    if (args.PropertyName == nameof(VsControlModel.Visible))
            //        con.Visible = bindingSource.Visible;
            //    else if (args.PropertyName == nameof(VsControlModel.Enabled))
            //        con.Enabled = bindingSource.Enabled;
            //};
            //bindingSource.PropertyChanged += bindingSource.BoundHandler;
        }

        public void Unbind(Form form, VsControlModel bindingSource)
        {
            var con = (DataGridView)form.GetControl(bindingSource.Name);
            con.DataBindings.Clear();
            BindingHelper.RemoveBoundHandler(bindingSource);
        }
    }

    // ============================================================
    //  Null（兜底策略）
    // ============================================================
    public class VsNullBindingStrategy : IVsControlBinding
    {
        public void Bind(Form form, VsControlModel bindingSource) { }
        public void Unbind(Form form, VsControlModel bindingSource) { }
    }
}
