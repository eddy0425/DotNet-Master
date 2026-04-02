using System;
using System.Windows.Forms;
using System.ComponentModel;

namespace DotNet.VisionMaster
{
    public class VsControlModel : INotifyPropertyChanged
    {
        private string _text;
        private int _value;
        private bool _checked;
        private bool _visible;
        private bool _enabled;
        private bool _dropDownStyle;
        private string[] _items;

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
        public VsControlModel(Form form, string name, string type, string text,bool visible)
        { 
            Name= name;
            Type = type;
            VsControlBindingStrategyFactory.CreateStrategy(Type).Bind(form, this);

            Text = text;
            Visible = visible;
        }

        /// <summary>
        /// CheckBox
        /// RadioButton
        /// </summary>
        public VsControlModel(Form form, string name, string type, string text, bool visible,bool _checked)
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
        public VsControlModel(Form form, string name, string type, string text, bool visible, bool enabled,bool dropDownStyle, string[] items)
        {
            Name =name;
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

        /// <summary>
        /// 存储 Bind 时注册的 PropertyChanged 委托，确保 Unbind 能移除同一实例
        /// </summary>
        internal PropertyChangedEventHandler BoundHandler { get; set; }

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
                //if (_checked != value)
                {
                    _checked = value;
                    OnPropertyChanged(nameof(Checked));
                }
            }
        }

        public bool Visible
        {
            get => _visible;
            set
            {
                //if (_visible != value)
                {
                    _visible = value;
                    OnPropertyChanged(nameof(Visible));
                }
            }
        }

        public bool Enabled
        {
            get => _enabled;
            set
            {
                //if (_enabled != value)
                {
                    _enabled = value;
                    OnPropertyChanged(nameof(Enabled));
                }
            }
        }
       
        public bool DropDownStyle
        {
            get => _dropDownStyle;
            set
            {
                //if (_dropDownStyle != value)
                {
                    _dropDownStyle = value;
                    OnPropertyChanged(nameof(DropDownStyle));
                }
            }
        }

        public string[] Items
        {
            get => _items;
            set
            {
                if (_items != value)
                {
                    _items = value;
                    OnPropertyChanged(nameof(Items));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }

   
}
