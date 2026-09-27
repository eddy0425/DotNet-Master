using System;
using System.Collections.Generic;
using System.Linq;
using DotNet.Drawing;
using DotNet.Vision.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNet.HalconUI.Tests
{
    /// <summary>
    /// <see cref="WinFormsParaUiHost"/>：<see cref="IParaUiHost"/> 的 WinForms 适配，重点是读回语义：
    /// 无默认值重载找不到控件即抛出，带默认值重载记 Warn 并回退。
    /// </summary>
    [TestClass]
    public class WinFormsParaUiHostTests
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
        public void Ctor_NullArguments_Throw()
        {
            Sta.Run(() =>
            {
                using (var form = new ParaPanelForm())
                {
                    Assert.ThrowsException<ArgumentNullException>(() => new WinFormsParaUiHost(null, new Dictionary<string, VsControlModel>()));
                    Assert.ThrowsException<ArgumentNullException>(() => new WinFormsParaUiHost(form, null));
                }
            });
        }

        [TestMethod]
        public void Controls_ExposesSameDictionary()
        {
            Sta.Run(() =>
            {
                using (var form = new ParaPanelForm())
                {
                    var dict = new Dictionary<string, VsControlModel>();
                    var host = new WinFormsParaUiHost(form, dict);
                    Assert.AreSame(dict, host.Controls);
                }
            });
        }

        [TestMethod]
        public void ShowThenGet_RoundTripsEveryType()
        {
            WithHost((form, host) =>
            {
                host.ShowTextBox("txt_Value", "3.5");
                host.ShowCheckBox("ckb_Enable", "启用", true);
                host.ShowTrackBar("trackBar1", 99);
                host.ShowComboBoxList("cmb_Mode", "B", new[] { "A", "B" });

                Assert.AreEqual("3.5", host.GetString("txt_Value"));
                Assert.AreEqual(3.5, host.GetDouble("txt_Value"));
                Assert.IsTrue(host.GetBool("ckb_Enable"));
                Assert.AreEqual(99, host.GetInt("trackBar1"));
                Assert.AreEqual("B", host.GetString("cmb_Mode"));
            });
        }

        [TestMethod]
        public void Get_ReflectsUserEditsOnControls()
        {
            WithHost((form, host) =>
            {
                host.ShowTextBox("txt_Value", "1");
                host.ShowCheckBox("ckb_Enable", "启用", false);
                host.ShowTrackBar("trackBar1", 10);

                form.TextBox.Text = "42";
                form.CheckBox.Checked = true;
                form.TrackBar.Value = 250;

                Assert.AreEqual(42, host.GetInt("txt_Value"));
                Assert.IsTrue(host.GetBool("ckb_Enable"));
                Assert.AreEqual(250, host.GetInt("trackBar1"));
            });
        }

        [TestMethod]
        public void RequiredGet_MissingControl_ThrowsWithName()
        {
            WithHost((form, host) =>
            {
                var ex = Assert.ThrowsException<InvalidOperationException>(() => host.GetString("txt_Missing"));
                StringAssert.StartsWith(ex.Message, "参数面板上不存在控件 'txt_Missing'");

                Assert.ThrowsException<InvalidOperationException>(() => host.GetBool("x"));
                Assert.ThrowsException<InvalidOperationException>(() => host.GetInt("x"));
                Assert.ThrowsException<InvalidOperationException>(() => host.GetDouble("x"));
            });
        }

        [TestMethod]
        public void FallbackGet_MissingControl_ReturnsFallback_AndLogsWarn()
        {
            WithHost((form, host) =>
            {
                using (var log = new CapturingLogger())
                {
                    Assert.AreEqual("def", host.GetString("a", "def"));
                    Assert.IsTrue(host.GetBool("b", true));
                    Assert.AreEqual(7, host.GetInt("c", 7));
                    Assert.AreEqual(1.5, host.GetDouble("d", 1.5));

                    var warns = log.Entries.Where(e => e.Level == LogLevel.Warn).ToList();
                    Assert.AreEqual(4, warns.Count);
                    Assert.IsTrue(warns.All(w => w.Category == "VsControl"));
                    StringAssert.Contains(warns[0].Message, "'a'");
                }
            });
        }

        [TestMethod]
        public void FallbackGet_ExistingControl_IgnoresFallback_AndDoesNotLog()
        {
            WithHost((form, host) =>
            {
                host.ShowTextBox("txt_Value", "5");
                using (var log = new CapturingLogger())
                {
                    Assert.AreEqual("5", host.GetString("txt_Value", "def"));
                    Assert.AreEqual(5, host.GetInt("txt_Value", 0));
                    Assert.AreEqual(5d, host.GetDouble("txt_Value", 0));
                    Assert.AreEqual(0, log.Entries.Count);
                }
            });
        }

        [TestMethod]
        public void ShowTabs_And_ClearAll_DelegateToFactory()
        {
            WithHost((form, host) =>
            {
                host.ShowTabs(TabPageEnum.Region, TabPageEnum.Display);
                CollectionAssert.AreEqual(new[] { form.Page(2), form.Page(4) }, form.Tabs.TabPages.Cast<object>().ToArray());

                host.ShowTextBox("txt_Value", "1");
                host.ShowLabel("lbl_Name", "名称");
                host.ShowButton("but_Apply", true);
                host.ShowGroupBox("groupBox1");
                host.ShowComboBox("cmb_Mode", "x", true);
                host.ShowComboBoxDropDown("cmb_Mode", "y", new[] { "y" });
                host.ShowRadioButton("rdb_Dark", "暗", true, false);
                host.ShowTabPage("tabPage2", "区域", true);
                Assert.AreEqual(4, host.Controls.Count);
                Assert.AreEqual("名称", form.Label.Text);

                host.ClearAll();
                Assert.AreEqual(0, host.Controls.Count);
            });
        }
    }
}
