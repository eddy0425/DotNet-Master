using System;
using System.Reflection;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Collections.Concurrent;
using DotNet.Vision.Abstractions;


namespace DotNet.HalconUI
{
    public static class VsControlFactory
    {
        // 反射结果缓存: (ControlType, FieldName) -> FieldInfo.
        // 只缓存元数据, 不持有 Control 实例, 不影响 GC.
        private static readonly ConcurrentDictionary<Tuple<Type, string>, FieldInfo> _fieldCache
            = new ConcurrentDictionary<Tuple<Type, string>, FieldInfo>();

        // Display 标签页上需要默认隐藏的 CheckBox 名称.
        // 集中配置以便后续 Designer 调整后只改一处.
        private static readonly string[] _displayCheckBoxNames =
            { "ckb_disp0", "ckb_disp1", "ckb_disp2", "ckb_disp3", "ckb_disp4" };


        /// <summary>
        /// 通过名称在 Control（Form 或 UserControl）上反射查找控件 (Designer 生成的私有字段). 结果按 (ControlType, FieldName) 缓存.
        /// </summary>
        public static object GetControl(this Control form, string name)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            var type = form.GetType();
            var field = _fieldCache.GetOrAdd(Tuple.Create(type, name), k => k.Item1.GetField(k.Item2, BindingFlags.NonPublic | BindingFlags.Instance));

            if (field == null)
                throw new InvalidOperationException(
                    string.Format("在 '{0}' 上找不到名为 '{1}' 的私有字段.", type.FullName, name));

            return field.GetValue(form);
        }


        /// <summary>
        /// 按枚举顺序重建 tabControl1 的可见 TabPage 集合.
        /// </summary>
        public static void ShowTabs(this Control form, params TabPageEnum[] tabsToShow)
        {
            if (tabsToShow == null) return;

            var tabControl1 = (TabControl)form.GetControl("tabControl1");
            tabControl1.TabPages.Clear();

            foreach (var tab in tabsToShow)
            {
                switch (tab)
                {
                    case TabPageEnum.FileImage:
                        tabControl1.TabPages.Add((TabPage)form.GetControl("tabPage0"));
                        break;
                    case TabPageEnum.Parameter:
                        var tabPage1 = (TabPage)form.GetControl("tabPage1");
                        tabControl1.TabPages.Add(tabPage1);
                        foreach (Control c in tabPage1.Controls) c.Visible = false;
                        break;
                    case TabPageEnum.Region:
                        tabControl1.TabPages.Add((TabPage)form.GetControl("tabPage2"));
                        break;
                    case TabPageEnum.Matching:
                        tabControl1.TabPages.Add((TabPage)form.GetControl("tabPage3"));
                        break;
                    case TabPageEnum.Display:
                        tabControl1.TabPages.Add((TabPage)form.GetControl("tabPage4"));
                        foreach (var n in _displayCheckBoxNames)
                            ((CheckBox)form.GetControl(n)).Visible = false;
                        break;
                }
            }

            if (tabControl1.TabPages.Count > 0)
                tabControl1.SelectedIndex = 0;
        }


        /// <summary>
        /// 解绑所有 VM 并清空字典. 切换算法策略或 Form 关闭时调用.
        /// </summary>
        public static void ClearAll(this Dictionary<string, VsControlModel> controls)
        {
            if (controls == null) return;
            foreach (var kvp in controls)
            {
                if (kvp.Value != null) kvp.Value.Dispose();
            }
            controls.Clear();
        }

        /// <summary>
        /// 同名 key 再次 ShowXxx 时, 先 Dispose 旧 VM (解除 DataBindings 强引用), 再放入新 VM.
        /// 这是 "重复刷新参数面板而不必显式 ClearAll 也不泄漏" 的核心机制.
        /// </summary>
        private static void Replace(this Dictionary<string, VsControlModel> controls, string name, VsControlModel newModel)
        {
            VsControlModel old;
            if (controls.TryGetValue(name, out old))
            {
                if (old != null) old.Dispose();
                controls[name] = newModel;
            }
            else
            {
                controls.Add(name, newModel);
            }
        }


        // ============================================================
        //  Show 系列扩展方法
        //  Label / Button / GroupBox 三个仅操作控件本体, 不纳入 VM 字典.
        //  其余控件创建对应 VsControlModel 并通过 Replace 放入字典.
        // ============================================================

        public static void ShowTrackBar(this Dictionary<string, VsControlModel> controls, Control form, string name, int value)
        {
            controls.Replace(name, new VsControlModel(form, name, VsControlTypes.TrackBar, value));
        }

        public static void ShowTabPage(this Dictionary<string, VsControlModel> controls, Control form, string name, string text, bool visible)
        {
            controls.Replace(name, new VsControlModel(form, name, VsControlTypes.TabPage, text, visible));
        }

        public static void ShowLabel(this Dictionary<string, VsControlModel> controls, Control form, string name, string text)
        {
            var con = (Label)form.GetControl(name);
            con.Text = text;
            con.Visible = true;
        }

        public static void ShowButton(this Dictionary<string, VsControlModel> controls, Control form, string name, bool visible)
        {
            var con = (Button)form.GetControl(name);
            con.Visible = visible;
        }

        public static void ShowTextBox(this Dictionary<string, VsControlModel> controls, Control form, string name, string text)
        {
            var con = (TextBox)form.GetControl(name);
            con.Visible = true;
            controls.Replace(name, new VsControlModel(form, name, VsControlTypes.TextBox, text, true));
        }

        public static void ShowComboBox(this Dictionary<string, VsControlModel> controls, Control form, string name, string text, bool enabled)
        {
            var con = (ComboBox)form.GetControl(name);
            con.Visible = true;
            con.Enabled = enabled;
            con.DropDownStyle = ComboBoxStyle.DropDown;
            controls.Replace(name, new VsControlModel(form, name, VsControlTypes.ComboBox, text, true, enabled, false, null));
        }

        public static void ShowComboBoxList(this Dictionary<string, VsControlModel> controls, Control form, string name, string text, string[] items)
        {
            var con = (ComboBox)form.GetControl(name);
            con.Visible = true;
            con.Enabled = true;
            con.DropDownStyle = ComboBoxStyle.DropDownList;
            con.Items.Clear();
            if (items != null) con.Items.AddRange(items);
            controls.Replace(name, new VsControlModel(form, name, VsControlTypes.ComboBox, text, true, true, true, items));
        }

        public static void ShowComboBoxDropDown(this Dictionary<string, VsControlModel> controls, Control form, string name, string text, string[] items)
        {
            var con = (ComboBox)form.GetControl(name);
            con.Visible = true;
            con.Enabled = true;
            con.DropDownStyle = ComboBoxStyle.DropDown;
            con.Items.Clear();
            if (items != null) con.Items.AddRange(items);
            controls.Replace(name, new VsControlModel(form, name, VsControlTypes.ComboBox, text, true, true, false, items));
        }

        public static void ShowCheckBox(this Dictionary<string, VsControlModel> controls, Control form, string name, string text, bool _checked)
        {
            var con = (CheckBox)form.GetControl(name);
            con.Visible = true;
            con.Text = text;
            controls.Replace(name, new VsControlModel(form, name, VsControlTypes.CheckBox, text, true, _checked));
        }

        public static void ShowGroupBox(this Dictionary<string, VsControlModel> controls, Control form, string name)
        {
            var con = (GroupBox)form.GetControl(name);
            con.Visible = true;
        }

        public static void ShowRadioButton(this Dictionary<string, VsControlModel> controls, Control form, string name, string text, bool visible, bool _checked)
        {
            var con = (RadioButton)form.GetControl(name);
            con.Visible = visible;
            con.Text = text;
            controls.Replace(name, new VsControlModel(form, name, VsControlTypes.RadioButton, text, visible, _checked));
        }
    }
}
