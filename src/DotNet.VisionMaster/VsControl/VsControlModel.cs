using System.Windows.Forms;
using System.ComponentModel;


namespace DotNet.VisionMaster
{
    public class VsControlModel : INotifyPropertyChanged
    {
        private string _text;
        private int _value;
        private bool _checked;

        /// <summary>
        /// TrackBar
        /// </summary>
        public VsControlModel(Form form, string name, string type, int value)
        {
            Name = name;
            Type = type;
            VsControlBindingStrategyFactory.CreateStrategy(Type).Bind(form, this);

            Value = value;
        }

        /// <summary>
        /// TabPage
        /// Label
        /// TextBox
        /// Button
        /// </summary>
        public VsControlModel(Form form, string name, string type, string text, bool visible)
        {
            Name = name;
            Type = type;
            VsControlBindingStrategyFactory.CreateStrategy(Type).Bind(form, this);

            Text = text;
            Visible = visible;
        }

        /// <summary>
        /// CheckBox
        /// RadioButton
        /// </summary>
        public VsControlModel(Form form, string name, string type, string text, bool visible, bool _checked)
        {
            Name = name;
            Type = type;
            VsControlBindingStrategyFactory.CreateStrategy(Type).Bind(form, this);

            Text = text;
            Visible = visible;
            Checked = _checked;
        }

        /// <summary>
        /// ComboBox
        /// </summary>
        public VsControlModel(Form form, string name, string type, string text, bool visible, bool enabled, bool dropDownStyle, string[] items)
        {
            Name = name;
            Type = type;
            VsControlBindingStrategyFactory.CreateStrategy(Type).Bind(form, this);

            DropDownStyle = dropDownStyle;
            if (items != null) Items = items;

            Text = text;
            Visible = visible;
            Enabled = enabled;
        }

        public string Name { get; set; }
        public string Type { get; set; }
        public int Value
        {
            get { return _value; }
            set
            {
                if (_value != value)
                {
                    _value = value;
                    OnPropertyChanged(nameof(Value));
                }
            }
        }
        public string Text
        {
            get { return _text; }
            set
            {
                if (_text != value)
                {
                    _text = value;
                    OnPropertyChanged(nameof(Text));
                }
            }
        }
        public bool Checked
        {
            get { return _checked; }
            set
            {
                if (_checked != value)
                {
                    _checked = value;
                    OnPropertyChanged(nameof(Checked));
                }
            }
        }
        public bool Visible { get; set; }
        public bool Enabled { get; set; }
        public bool DropDownStyle { get; set; }
        public string[] Items { get; set; }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

}
