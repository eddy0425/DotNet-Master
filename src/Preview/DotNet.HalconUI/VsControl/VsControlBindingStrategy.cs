using System.Windows.Forms;


namespace DotNet.HalconUI
{
    public interface IVsControlBinding
    {
        void Bind(Form form, VsControlModel bindingSource);
        void Unbind(Form form, VsControlModel bindingSource);
    }

    public static class VsControlBindingStrategyFactory
    {
        private static readonly VsTabPageBindingStrategy _tabPageBindingStrategy = new VsTabPageBindingStrategy();
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
        }

        public void Unbind(Form form, VsControlModel bindingSource)
        {
            //var con = (Button)form.GetControl(bindingSource.Name);
            //con.DataBindings.Clear();
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
        }

        public void Unbind(Form form, VsControlModel bindingSource)
        {
            var con = (TextBox)form.GetControl(bindingSource.Name);
            con.DataBindings.Clear();
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
        }

        public void Unbind(Form form, VsControlModel bindingSource)
        {
            var con = (ComboBox)form.GetControl(bindingSource.Name);
            con.DataBindings.Clear();
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
        }

        public void Unbind(Form form, VsControlModel bindingSource)
        {
            var con = (CheckBox)form.GetControl(bindingSource.Name);
            con.DataBindings.Clear();
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
        }

        public void Unbind(Form form, VsControlModel bindingSource)
        {
            var con = (RadioButton)form.GetControl(bindingSource.Name);
            con.DataBindings.Clear();
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
