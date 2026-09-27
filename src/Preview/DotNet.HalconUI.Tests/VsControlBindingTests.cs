using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using DotNet.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNet.HalconUI.Tests
{
    /// <summary>
    /// 各绑定策略在<b>显示中的</b>面板上的行为：Visible / Enabled 只由 VM 驱动控件，
    /// 其余主属性双向同步；以及重绑失败、读回转换失败时的状态一致性。
    /// </summary>
    /// <remarks>
    /// 与 <see cref="VsControlModelTests"/> 不同，这里的面板窗体放到屏幕外显示，
    /// 控件的 Visible 读取值才有意义（它取的是含父链的实际可见性）。
    /// </remarks>
    [TestClass]
    public class VsControlBindingTests
    {
        private static void WithShownForm(Action<ParaPanelForm> body) => Sta.Run(() =>
        {
            using (var form = new ParaPanelForm())
            {
                WindowHost.ShowOffscreen(form);
                form.Tabs.SelectedTab = form.Page(1);
                Application.DoEvents();
                body(form);
            }
        });

        /// <summary>切到别的页再切回参数页：参数页隐藏时其子控件的 Visible 读出来都是 false。</summary>
        private static void SwitchAwayAndBack(ParaPanelForm form)
        {
            form.Tabs.SelectedTab = form.Page(0);
            Application.DoEvents();
            form.Tabs.SelectedTab = form.Page(1);
            Application.DoEvents();
        }

        #region Visible / Enabled 只能单向（VM → 控件）

        [TestMethod]
        public void TabSwitch_DoesNotHideBoundControls()
        {
            // Control.Visible 读的是含父链的实际可见性。若 Visible 双向绑定，参数页被切走时
            // 子控件的 VisibleChanged 会把 false 写回 VM，VM 再把 false 推回控件——切回来控件就再也不显示了。
            WithShownForm(form =>
            {
                var models = new List<VsControlModel>
                {
                    new VsControlModel(form, "txt_Value", VsControlTypes.TextBox, "1", true),
                    new VsControlModel(form, "ckb_Enable", VsControlTypes.CheckBox, "", true, true),
                    new VsControlModel(form, "rdb_Dark", VsControlTypes.RadioButton, "", true, false),
                    new VsControlModel(form, "cmb_Mode", VsControlTypes.ComboBox, "a", true, true, false, new[] { "a", "b" }),
                };
                try
                {
                    SwitchAwayAndBack(form);

                    foreach (var vm in models)
                        Assert.IsTrue(vm.Visible, vm.Name + "：父容器隐藏不应改写 VM.Visible");
                    Assert.IsTrue(form.TextBox.Visible, "TextBox 应随参数页重新显示");
                    Assert.IsTrue(form.CheckBox.Visible, "CheckBox 应随参数页重新显示");
                    Assert.IsTrue(form.RadioButton.Visible, "RadioButton 应随参数页重新显示");
                    Assert.IsTrue(form.ComboBox.Visible, "ComboBox 应随参数页重新显示");
                }
                finally { models.ForEach(m => m.Dispose()); }
            });
        }

        [TestMethod]
        public void DisabledParent_DoesNotDisableComboBoxModel()
        {
            WithShownForm(form =>
            {
                using (var vm = new VsControlModel(form, "cmb_Mode", VsControlTypes.ComboBox, "a", true, true, false, new[] { "a", "b" }))
                {
                    form.Page(1).Enabled = false;
                    Application.DoEvents();
                    Assert.IsTrue(vm.Enabled, "父容器禁用不应改写 VM.Enabled");

                    form.Page(1).Enabled = true;
                    Application.DoEvents();
                    Assert.IsTrue(form.ComboBox.Enabled);
                }
            });
        }

        [TestMethod]
        public void VisibleAndEnabled_FlowFromModelToControl()
        {
            WithShownForm(form =>
            {
                using (var text = new VsControlModel(form, "txt_Value", VsControlTypes.TextBox, "1", true))
                using (var combo = new VsControlModel(form, "cmb_Mode", VsControlTypes.ComboBox, "a", true, true, false, new[] { "a", "b" }))
                {
                    Assert.IsTrue(form.TextBox.Visible);

                    text.Visible = false;
                    combo.Enabled = false;
                    Assert.IsFalse(form.TextBox.Visible);
                    Assert.IsFalse(form.ComboBox.Enabled);

                    text.Visible = true;
                    combo.Enabled = true;
                    Assert.IsTrue(form.TextBox.Visible);
                    Assert.IsTrue(form.ComboBox.Enabled);
                }
            });
        }

        [TestMethod]
        public void ComboBox_DropDownStyle_FlowsOnlyFromModelToControl()
        {
            WithShownForm(form =>
            {
                using (var vm = new VsControlModel(form, "cmb_Mode", VsControlTypes.ComboBox, "a", true, true, true, new[] { "a", "b" }))
                {
                    Assert.AreEqual(ComboBoxStyle.DropDownList, form.ComboBox.DropDownStyle);

                    vm.DropDownStyle = false;
                    Assert.AreEqual(ComboBoxStyle.DropDown, form.ComboBox.DropDownStyle);

                    form.ComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                    Assert.IsFalse(vm.DropDownStyle, "外观属性不回写 VM");

                    vm.DropDownStyle = true;
                    vm.DropDownStyle = false;
                    Assert.AreEqual(ComboBoxStyle.DropDown, form.ComboBox.DropDownStyle, "VM 仍能驱动控件");
                }
            });
        }

        #endregion

        #region 主属性双向同步

        [TestMethod]
        public void TrackBar_ModelToControl()
        {
            WithShownForm(form =>
            {
                using (var vm = new VsControlModel(form, "trackBar1", VsControlTypes.TrackBar, 10))
                {
                    vm.Value = 42;
                    Assert.AreEqual(42, form.TrackBar.Value);
                }
            });
        }

        [TestMethod]
        public void RadioButton_BothDirections()
        {
            WithShownForm(form =>
            {
                using (var vm = new VsControlModel(form, "rdb_Dark", VsControlTypes.RadioButton, "", true, false))
                {
                    vm.Value = true;
                    Assert.IsTrue(form.RadioButton.Checked);

                    form.RadioButton.Checked = false;
                    Assert.AreEqual(false, vm.Value);
                }
            });
        }

        #endregion

        #region 重绑失败

        [TestMethod]
        public void TrackBar_OutOfRange_IsClampedAndStaysConsistent()
        {
            // TrackBar.Value 越界会抛 ArgumentOutOfRangeException。原实现在抛出前已经摘掉了旧 VM 的绑定，
            // 旧 VM 仍留在字典里却再也收不到控件变化。
            WithShownForm(form =>
            {
                var controls = new Dictionary<string, VsControlModel>();
                try
                {
                    controls.ShowTrackBar(form, "trackBar1", 10);
                    using (var log = new CapturingLogger())
                    {
                        controls.ShowTrackBar(form, "trackBar1", 300);
                        Assert.AreEqual(1, log.Messages(LogLevel.Warn).Count(), "越界值应记 Warn");
                    }

                    Assert.AreEqual(255, form.TrackBar.Value);
                    Assert.AreEqual(255, controls["trackBar1"].AsInt());

                    form.TrackBar.Value = 20;
                    Assert.AreEqual(20, controls["trackBar1"].AsInt(), "字典里的 VM 必须仍与控件绑定");

                    controls.ShowTrackBar(form, "trackBar1", -5);
                    Assert.AreEqual(0, form.TrackBar.Value);
                    Assert.AreEqual(0, controls["trackBar1"].AsInt());
                }
                finally { controls.ClearAll(); }
            });
        }

        #endregion
    }

    /// <summary><see cref="WinFormsParaUiHost"/> 读回时的类型转换失败要指明是哪个控件。</summary>
    [TestClass]
    public class WinFormsParaUiHostConversionTests
    {
        private static void WithHost(Action<ParaPanelForm, WinFormsParaUiHost> body) => Sta.Run(() =>
        {
            using (var form = new ParaPanelForm())
            {
                var host = new WinFormsParaUiHost(form, new Dictionary<string, VsControlModel>());
                try { body(form, host); }
                finally { host.ClearAll(); }
            }
        });

        [TestMethod]
        public void GetInt_Unparsable_ThrowsFormatExceptionNamingControl()
        {
            WithHost((form, host) =>
            {
                host.ShowTextBox("txt_Value", "1");
                form.TextBox.Text = "";

                var ex = Assert.ThrowsException<FormatException>(() => host.GetInt("txt_Value"));
                StringAssert.Contains(ex.Message, "txt_Value");
                Assert.IsNotNull(ex.InnerException);

                ex = Assert.ThrowsException<FormatException>(() => host.GetInt("txt_Value", 5));
                StringAssert.Contains(ex.Message, "txt_Value", "带默认值的重载只对「控件不存在」回退，值非法仍应报错");
            });
        }

        [TestMethod]
        public void GetDouble_Unparsable_ThrowsFormatExceptionNamingControl()
        {
            WithHost((form, host) =>
            {
                host.ShowComboBoxDropDown("cmb_Mode", "0.5", new[] { "0.5" });
                form.ComboBox.Text = "abc";

                var ex = Assert.ThrowsException<FormatException>(() => host.GetDouble("cmb_Mode"));
                StringAssert.Contains(ex.Message, "cmb_Mode");
                StringAssert.Contains(ex.Message, "abc");
            });
        }

        [TestMethod]
        public void GetInt_Overflow_ThrowsFormatExceptionNamingControl()
        {
            WithHost((form, host) =>
            {
                host.ShowTextBox("txt_Value", "99999999999");
                var ex = Assert.ThrowsException<FormatException>(() => host.GetInt("txt_Value"));
                StringAssert.Contains(ex.Message, "txt_Value");
            });
        }
    }
}
