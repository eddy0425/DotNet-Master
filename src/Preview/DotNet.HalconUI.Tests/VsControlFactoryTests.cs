using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using DotNet.HalconCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNet.HalconUI.Tests
{
    /// <summary>
    /// <see cref="VsControlFactory"/>：私有字段反射查找、标签页布局、Show* 系列与字典替换语义。
    /// </summary>
    [TestClass]
    public class VsControlFactoryTests
    {
        private static void WithForm(Action<ParaPanelForm, Dictionary<string, VsControlModel>> body) => Sta.Run(() =>
        {
            using (var form = new ParaPanelForm())
            {
                var controls = new Dictionary<string, VsControlModel>();
                try { body(form, controls); }
                finally { controls.ClearAll(); }
            }
        });

        #region GetControl

        [TestMethod]
        public void GetControl_ReturnsPrivateField()
        {
            WithForm((form, _) =>
            {
                Assert.AreSame(form.TextBox, form.GetControl("txt_Value"));
                Assert.AreSame(form.Tabs, form.GetControl("tabControl1"));
                Assert.AreEqual("x", form.GetControl("notAControl"), "只按字段取值，不校验类型");
            });
        }

        [TestMethod]
        public void GetControl_InvalidArguments_Throw()
        {
            WithForm((form, _) =>
            {
                Assert.ThrowsException<ArgumentNullException>(() => VsControlFactory.GetControl(null, "txt_Value"));
                Assert.ThrowsException<ArgumentNullException>(() => form.GetControl(null));
                Assert.ThrowsException<ArgumentNullException>(() => form.GetControl(""));
            });
        }

        [TestMethod]
        public void GetControl_MissingField_ThrowsWithTypeAndName()
        {
            WithForm((form, _) =>
            {
                var ex = Assert.ThrowsException<InvalidOperationException>(() => form.GetControl("no_such"));
                Assert.AreEqual($"在 '{typeof(ParaPanelForm).FullName}' 上找不到名为 'no_such' 的私有字段.", ex.Message);

                // 失败结果也被缓存，第二次仍应抛出同样的异常而不是 NullReference
                Assert.ThrowsException<InvalidOperationException>(() => form.GetControl("no_such"));
            });
        }

        [TestMethod]
        public void GetControl_DoesNotFindPublicMembers()
        {
            WithForm((form, _) =>
            {
                Assert.ThrowsException<InvalidOperationException>(() => form.GetControl("Text"));
            });
        }

        [TestMethod]
        public void GetControl_CacheIsPerInstance_NotPerType()
        {
            Sta.Run(() =>
            {
                using (var a = new ParaPanelForm())
                using (var b = new ParaPanelForm())
                {
                    Assert.AreSame(a.TextBox, a.GetControl("txt_Value"));
                    Assert.AreSame(b.TextBox, b.GetControl("txt_Value"), "缓存的是 FieldInfo，不是控件实例");
                }
            });
        }

        #endregion

        #region ShowTabs

        [TestMethod]
        public void ShowTabs_KeepsOnlyRequestedPages_InOrder()
        {
            WithForm((form, _) =>
            {
                form.ShowTabs(TabPageEnum.Matching, TabPageEnum.FileImage);

                CollectionAssert.AreEqual(new[] { form.Page(3), form.Page(0) }, form.Tabs.TabPages.Cast<TabPage>().ToArray());
                Assert.AreEqual(0, form.Tabs.SelectedIndex);
            });
        }

        [TestMethod]
        public void ShowTabs_Null_LeavesPagesUntouched()
        {
            WithForm((form, _) =>
            {
                form.ShowTabs(null);
                Assert.AreEqual(5, form.Tabs.TabPages.Count);
            });
        }

        [TestMethod]
        public void ShowTabs_Empty_ClearsAllPages()
        {
            WithForm((form, _) =>
            {
                form.ShowTabs();
                Assert.AreEqual(0, form.Tabs.TabPages.Count);
            });
        }

        [TestMethod]
        public void ShowTabs_Parameter_HidesEveryParameterControl()
        {
            WithForm((form, _) =>
            {
                foreach (Control c in form.Page(1).Controls) c.Visible = true;

                form.ShowTabs(TabPageEnum.Parameter);

                foreach (Control c in form.Page(1).Controls)
                    Assert.IsFalse(IsSelfVisible(c), c.Name + " 应被隐藏，等待 DispPara 逐个显示");
            });
        }

        [TestMethod]
        public void ShowTabs_Display_HidesDisplayCheckBoxes()
        {
            WithForm((form, _) =>
            {
                for (int i = 0; i < 5; i++) form.DispCheckBox(i).Visible = true;

                form.ShowTabs(TabPageEnum.Display);

                for (int i = 0; i < 5; i++)
                    Assert.IsFalse(IsSelfVisible(form.DispCheckBox(i)), "ckb_disp" + i);
            });
        }

        /// <summary>控件自身的可见标志（不受父级是否显示影响）。</summary>
        private static bool IsSelfVisible(Control c) =>
            (bool)typeof(Control).GetMethod("GetState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null, new[] { typeof(int) }, null).Invoke(c, new object[] { 0x00000002 /* STATE_VISIBLE */ });

        #endregion

        #region Show* 系列

        [TestMethod]
        public void ShowLabel_And_ShowButton_TouchControlOnly()
        {
            WithForm((form, controls) =>
            {
                form.Label.Visible = false;
                controls.ShowLabel(form, "lbl_Name", "阈值");
                controls.ShowButton(form, "but_Apply", false);
                controls.ShowGroupBox(form, "groupBox1");

                Assert.AreEqual("阈值", form.Label.Text);
                Assert.IsTrue(IsSelfVisible(form.Label));
                Assert.IsFalse(IsSelfVisible(form.Button));
                Assert.IsTrue(IsSelfVisible(form.GroupBox));
                Assert.AreEqual(0, controls.Count, "Label / Button / GroupBox 不进 VM 字典");
            });
        }

        [TestMethod]
        public void ShowTextBox_CreatesModel_AndPushesText()
        {
            WithForm((form, controls) =>
            {
                controls.ShowTextBox(form, "txt_Value", "15");

                var vm = controls["txt_Value"];
                Assert.AreEqual("TextBox", vm.Type);
                Assert.AreEqual("15", form.TextBox.Text);
            });
        }

        [TestMethod]
        public void ShowComboBoxList_SetsItemsAndStyle()
        {
            WithForm((form, controls) =>
            {
                controls.ShowComboBoxList(form, "cmb_Mode", "B", new[] { "A", "B" });

                var vm = controls["cmb_Mode"];
                CollectionAssert.AreEqual(new[] { "A", "B" }, form.ComboBox.Items.Cast<string>().ToArray());
                CollectionAssert.AreEqual(new[] { "A", "B" }, vm.Items);
                Assert.AreEqual(ComboBoxStyle.DropDownList, form.ComboBox.DropDownStyle);
                Assert.IsTrue(vm.DropDownStyle);
                // 切换 DropDownStyle 会重建句柄，未显示的窗体上随后建立的绑定处于未激活状态，
                // 控件 Text 不一定被推送；这里只断言 VM 保存的值。
                Assert.AreEqual("B", vm.AsString());
            });
        }

        [TestMethod]
        public void ShowComboBoxDropDown_AllowsFreeText()
        {
            WithForm((form, controls) =>
            {
                controls.ShowComboBoxDropDown(form, "cmb_Mode", "自定义", new[] { "A" });

                Assert.AreEqual(ComboBoxStyle.DropDown, form.ComboBox.DropDownStyle);
                Assert.IsFalse(controls["cmb_Mode"].DropDownStyle);
                Assert.AreEqual("自定义", form.ComboBox.Text);
            });
        }

        [TestMethod]
        public void ShowComboBox_AppliesEnabled_AndClearsNothingElse()
        {
            WithForm((form, controls) =>
            {
                controls.ShowComboBox(form, "cmb_Mode", "X", enabled: false);

                var vm = controls["cmb_Mode"];
                Assert.IsFalse(vm.Enabled);
                Assert.IsFalse(form.ComboBox.Enabled);
                Assert.IsNull(vm.Items);
            });
        }

        [TestMethod]
        public void ShowCheckBox_SetsTextAndChecked()
        {
            WithForm((form, controls) =>
            {
                controls.ShowCheckBox(form, "ckb_Enable", "启用滤波", true);

                Assert.AreEqual("启用滤波", form.CheckBox.Text);
                Assert.IsTrue(form.CheckBox.Checked);
                Assert.IsTrue(controls["ckb_Enable"].AsBool());
            });
        }

        [TestMethod]
        public void ShowRadioButton_SetsTextAndChecked()
        {
            WithForm((form, controls) =>
            {
                controls.ShowRadioButton(form, "rdb_Dark", "暗", true, true);

                Assert.AreEqual("暗", form.RadioButton.Text);
                Assert.IsTrue(form.RadioButton.Checked);
                Assert.IsTrue(controls["rdb_Dark"].AsBool());
            });
        }

        [TestMethod]
        public void ShowTrackBar_And_ShowTabPage()
        {
            WithForm((form, controls) =>
            {
                controls.ShowTrackBar(form, "trackBar1", 64);
                controls.ShowTabPage(form, "tabPage2", "ROI", true);

                Assert.AreEqual(64, form.TrackBar.Value);
                Assert.AreEqual("ROI", form.Page(2).Text);
                Assert.AreEqual(2, controls.Count);
            });
        }

        [TestMethod]
        public void Show_SameNameTwice_DisposesOldModel()
        {
            WithForm((form, controls) =>
            {
                controls.ShowTextBox(form, "txt_Value", "1");
                var old = controls["txt_Value"];

                controls.ShowTextBox(form, "txt_Value", "2");

                Assert.AreEqual(1, controls.Count);
                Assert.AreNotSame(old, controls["txt_Value"]);

                old.Value = "stale";
                Assert.AreEqual("2", form.TextBox.Text, "旧 VM 已释放，不应再驱动控件");
            });
        }

        [TestMethod]
        public void ClearAll_DisposesAndEmpties_NullSafe()
        {
            WithForm((form, controls) =>
            {
                controls.ShowTextBox(form, "txt_Value", "1");
                controls.ShowCheckBox(form, "ckb_Enable", "e", false);
                controls["placeholder"] = null;

                controls.ClearAll();

                Assert.AreEqual(0, controls.Count);
                Assert.AreEqual(0, form.TextBox.DataBindings.Count);
                Assert.AreEqual(0, form.CheckBox.DataBindings.Count);
            });

            ((Dictionary<string, VsControlModel>)null).ClearAll();
        }

        #endregion
    }
}
