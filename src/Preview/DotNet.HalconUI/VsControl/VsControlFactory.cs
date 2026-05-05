using System.Reflection;
using System.Windows.Forms;
using System.Collections.Generic;


namespace DotNet.HalconUI
{
    public static class VsControlFactory
    {
        public static object GetControl(this Form form, string name)
        {
            return form.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
        }

        public static void ShowTabs(this Form form, params TabPageEnum[] tabsToShow)
        {
            // 清空当前显示的标签页 TabControl
            var tabControl1 = (TabControl)form.GetControl("tabControl1");
            tabControl1.TabPages.Clear();

            for (int i = 0; i < tabsToShow.Length; i++)
            {
                switch (tabsToShow[i])
                {
                    case TabPageEnum.FileImage:
                        tabControl1.TabPages.Add((TabPage)form.GetControl("tabPage0"));
                        break;
                    case TabPageEnum.Parameter:
                        var tabPage1 = (TabPage)form.GetControl("tabPage1");
                        tabControl1.TabPages.Add(tabPage1);
                        foreach (Control itemt in tabPage1.Controls) itemt.Visible = false;  //关闭对应的显示
                        break;
                    case TabPageEnum.Region:
                        tabControl1.TabPages.Add((TabPage)form.GetControl("tabPage2"));
                        break;
                    case TabPageEnum.Matching:
                        tabControl1.TabPages.Add((TabPage)form.GetControl("tabPage3"));
                        break;
                    case TabPageEnum.Display:
                        tabControl1.TabPages.Add((TabPage)form.GetControl("tabPage4"));
                        T Get<T>(string name) where T : Control => (T)form.GetControl(name);
                        foreach (var name in new[] { "ckb_disp0", "ckb_disp1", "ckb_disp2", "ckb_disp3", "ckb_disp4" })
                            Get<CheckBox>(name).Visible = false;
                        break;
                }
            }

            // 默认显示第一个 TabPage
            tabControl1.SelectedIndex = 0;
          
        }

        /// <summary>
        /// 解绑所有控件并清空字典（切换策略时调用）
        /// </summary>
        public static void ClearAll(this Dictionary<string, VsControlModel> controls, Form form)
        {
            foreach (var kvp in controls)
            {
                var bindingStrategy = VsControlBindingStrategyFactory.CreateStrategy(kvp.Value.Type);
                bindingStrategy.Unbind(form, kvp.Value);
            }
            controls.Clear();
        }

        public static void ShowTrackBar(this Dictionary<string, VsControlModel> controls, Form form, string name, int value)
        {
            controls.Add(name, new VsControlModel(form, name, "TrackBar", value));
        }

        public static void ShowTabPage(this Dictionary<string, VsControlModel> controls, Form form, string name, string text, bool visible)
        {
            controls.Add(name, new VsControlModel(form, name, "TabPage", text, visible));
        }

        public static void ShowLabel(this Dictionary<string, VsControlModel> controls, Form form, string name, string text)
        {
            //controls.Add(name, new VsControlModel(form, name, "Label", text, true));
            var con = (Label)form.GetControl(name);
            con.Text = text;
            con.Visible = true;
        }

        public static void ShowButton(this Dictionary<string, VsControlModel> controls, Form form, string name, bool visible)
        {
            //controls.Add(name, new VsControlModel(form, name, "Button", "", visible));
            var con = (Button)form.GetControl(name);
            con.Visible = visible;
        }

        public static void ShowTextBox(this Dictionary<string, VsControlModel> controls, Form form, string name, string text)
        {
            var con = (TextBox)form.GetControl(name);
            con.Visible = true;
            controls.Add(name, new VsControlModel(form, name, "TextBox", text, true));
        }
      
        public static void ShowComboBox(this Dictionary<string, VsControlModel> controls, Form form, string name, string text, bool enabled)
        {
            var con = (ComboBox)form.GetControl(name);
            con.Visible = true;
            con.Enabled = enabled;
            con.DropDownStyle = false ? ComboBoxStyle.DropDownList : ComboBoxStyle.DropDown;
            controls.Add(name, new VsControlModel(form, name, "ComboBox", text, true, enabled, false, null));
        }

        public static void ShowComboBoxList(this Dictionary<string, VsControlModel> controls, Form form, string name, string text, string[] items)
        {
            var con = (ComboBox)form.GetControl(name);
            con.Visible = true;
            con.Enabled = true;
            con.DropDownStyle = true ? ComboBoxStyle.DropDownList : ComboBoxStyle.DropDown;
            con.Items.Clear();
            con.Items.AddRange(items);
            controls.Add(name, new VsControlModel(form, name, "ComboBox", text, true, true, true, items));
        }

        public static void ShowComboBoxDropDown(this Dictionary<string, VsControlModel> controls, Form form, string name, string text, string[] items)
        {
            var con = (ComboBox)form.GetControl(name);
            con.Visible = true;
            con.Enabled = true;
            con.DropDownStyle = false ? ComboBoxStyle.DropDownList : ComboBoxStyle.DropDown;
            con.Items.Clear();
            con.Items.AddRange(items);
            controls.Add(name, new VsControlModel(form, name, "ComboBox", text, true, true, false, items));
        }

        public static void ShowCheckBox(this Dictionary<string, VsControlModel> controls, Form form, string name, string text, bool _checked)
        {
            var con = (CheckBox)form.GetControl(name);
            con.Visible = true;
            con.Text = text;
            controls.Add(name, new VsControlModel(form, name, "CheckBox", text, true, _checked));
        }
        
        public static void ShowGroupBox(this Dictionary<string, VsControlModel> controls, Form form, string name)
        {
            var con = (GroupBox)form.GetControl(name);
            con.Visible = true;
        }

        public static void ShowRadioButton(this Dictionary<string, VsControlModel> controls, Form form, string name, string text, bool visible, bool _checked)
        {
            var con = (RadioButton)form.GetControl(name);
            con.Visible = visible;
            con.Text = text;
            controls.Add(name, new VsControlModel(form, name, "RadioButton", text, visible, _checked));
        }

    }
}
