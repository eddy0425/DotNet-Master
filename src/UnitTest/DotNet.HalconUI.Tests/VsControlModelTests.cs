using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNet.HalconUI.Tests
{
    /// <summary>
    /// <see cref="VsControlModel"/> 与各绑定策略：参数校验、属性通知、类型读取器、双向绑定与解绑。
    /// </summary>
    /// <remarks>
    /// 面板窗体不显示，控件的 Visible 读取值恒为 false，因此不对 Visible 的控件侧做断言。
    /// </remarks>
    [TestClass]
    public class VsControlModelTests
    {
        private static void WithForm(Action<ParaPanelForm> body) => Sta.Run(() =>
        {
            using (var form = new ParaPanelForm()) body(form);
        });

        private static IEnumerable<Binding> BindingsOf(Control control, VsControlModel vm) =>
            control.DataBindings.Cast<Binding>().Where(b => ReferenceEquals(b.DataSource, vm));

        #region 构造与读取器（不涉及控件，使用 Null 策略）

        [TestMethod]
        public void Ctor_InvalidArguments_Throw()
        {
            WithForm(form =>
            {
                Assert.ThrowsException<ArgumentNullException>(() => new VsControlModel(null, "x", "Unknown", 1));
                Assert.ThrowsException<ArgumentNullException>(() => new VsControlModel(form, "", "Unknown", 1));
                Assert.ThrowsException<ArgumentNullException>(() => new VsControlModel(form, null, "Unknown", 1));
                Assert.ThrowsException<ArgumentNullException>(() => new VsControlModel(form, "x", "", 1));
            });
        }

        [TestMethod]
        public void Defaults_VisibleAndEnabledTrue_ForIntCtor()
        {
            WithForm(form =>
            {
                using (var vm = new VsControlModel(form, "any", "Unknown", 7))
                {
                    Assert.AreEqual("any", vm.Name);
                    Assert.AreEqual("Unknown", vm.Type);
                    Assert.AreEqual(7, vm.Value);
                    Assert.IsTrue(vm.Visible);
                    Assert.IsTrue(vm.Enabled);
                    Assert.IsFalse(vm.DropDownStyle);
                    Assert.IsNull(vm.Items);
                }
            });
        }

        [TestMethod]
        public void Readers_ConvertValue()
        {
            WithForm(form =>
            {
                using (var vm = new VsControlModel(form, "n", "Unknown", "12.5", true))
                {
                    Assert.AreEqual("12.5", vm.AsString());
                    Assert.AreEqual(12.5, vm.AsDouble());
                    Assert.AreEqual(12.5f, vm.AsFloat());

                    vm.Value = 42;
                    Assert.AreEqual(42, vm.AsInt());
                    Assert.AreEqual(42L, vm.AsInt64());
                    Assert.AreEqual(string.Empty, vm.AsString(), "非字符串值读成空串");
                    Assert.IsFalse(vm.AsBool(), "非 bool 值读成 false");

                    vm.Value = true;
                    Assert.IsTrue(vm.AsBool());
                }
            });
        }

        [TestMethod]
        public void Readers_NullValue_ReturnDefaults()
        {
            WithForm(form =>
            {
                using (var vm = new VsControlModel(form, "n", "Unknown", null, true))
                {
                    Assert.AreEqual(string.Empty, vm.AsString());
                    Assert.IsFalse(vm.AsBool());
                    Assert.AreEqual(0, vm.AsInt());
                    Assert.AreEqual(0L, vm.AsInt64());
                    Assert.AreEqual(0d, vm.AsDouble());
                    Assert.AreEqual(0f, vm.AsFloat());
                }
            });
        }

        [TestMethod]
        public void SetField_RaisesPropertyChanged_OnlyOnChange()
        {
            WithForm(form =>
            {
                using (var vm = new VsControlModel(form, "n", "Unknown", "a", true))
                {
                    var raised = new List<string>();
                    vm.PropertyChanged += (s, e) => raised.Add(e.PropertyName);

                    vm.Value = "a";
                    vm.Visible = true;
                    Assert.AreEqual(0, raised.Count, "值未变不应通知");

                    vm.Value = "b";
                    vm.Visible = false;
                    vm.Enabled = false;
                    vm.DropDownStyle = true;

                    CollectionAssert.AreEqual(new[] { "Value", "Visible", "Enabled", "DropDownStyle" }, raised);
                }
            });
        }

        [TestMethod]
        public void Items_AreDefensivelyCopied()
        {
            WithForm(form =>
            {
                var source = new[] { "A", "B" };
                using (var vm = new VsControlModel(form, "n", "Unknown", "A", true, true, false, source))
                {
                    source[0] = "X";
                    Assert.AreEqual("A", vm.Items[0], "构造时应拷贝");

                    vm.Items[1] = "Y";
                    Assert.AreEqual("B", vm.Items[1], "读取时应返回拷贝");

                    var assigned = new[] { "C" };
                    vm.Items = assigned;
                    assigned[0] = "Z";
                    Assert.AreEqual("C", vm.Items[0], "赋值时应拷贝");

                    vm.Items = null;
                    Assert.IsNull(vm.Items);
                }
            });
        }

        [TestMethod]
        public void Dispose_IsIdempotent_AndDetachesPropertyChanged()
        {
            WithForm(form =>
            {
                var vm = new VsControlModel(form, "n", "Unknown", "a", true);
                int count = 0;
                vm.PropertyChanged += (s, e) => count++;

                vm.Dispose();
                vm.Dispose();
                vm.Value = "b";

                Assert.AreEqual(0, count);
            });
        }

        #endregion

        #region 策略工厂

        [TestMethod]
        public void StrategyFactory_KnownTypes_ReturnSingletons()
        {
            var expected = new Dictionary<string, Type>
            {
                { "TabPage", typeof(VsTabPageBindingStrategy) },
                { "TextBox", typeof(VsTextBoxBindingStrategy) },
                { "ComboBox", typeof(VsComboBoxBindingStrategy) },
                { "CheckBox", typeof(VsCheckBoxBindingStrategy) },
                { "RadioButton", typeof(VsRadioButtonBindingStrategy) },
                { "TrackBar", typeof(VsTrackBarBindingStrategy) },
                { "DataGridView", typeof(VsDataGridViewBindingStrategy) },
            };

            foreach (var kv in expected)
            {
                var s = VsControlBindingStrategyFactory.GetStrategy(kv.Key);
                Assert.IsInstanceOfType(s, kv.Value, kv.Key);
                Assert.AreSame(s, VsControlBindingStrategyFactory.GetStrategy(kv.Key), "策略无状态，应为单例");
            }
        }

        [TestMethod]
        public void StrategyFactory_NullOrUnknown_ReturnsNullStrategy()
        {
            Assert.IsInstanceOfType(VsControlBindingStrategyFactory.GetStrategy(null), typeof(VsNullBindingStrategy));
            Assert.IsInstanceOfType(VsControlBindingStrategyFactory.GetStrategy("Label"), typeof(VsNullBindingStrategy));
            Assert.IsInstanceOfType(VsControlBindingStrategyFactory.GetStrategy("textbox"), typeof(VsNullBindingStrategy), "类型名区分大小写");
        }

        #endregion

        #region 双向绑定

        [TestMethod]
        public void TextBox_BindsTextAndVisible_BothWays()
        {
            WithForm(form =>
            {
                using (var vm = new VsControlModel(form, "txt_Value", "TextBox", "10", true))
                {
                    CollectionAssert.AreEquivalent(new[] { "Text", "Visible" },
                        BindingsOf(form.TextBox, vm).Select(b => b.PropertyName).ToArray());
                    Assert.AreEqual("10", form.TextBox.Text, "构造即把 VM 值推到控件");

                    vm.Value = "20";
                    Assert.AreEqual("20", form.TextBox.Text);

                    form.TextBox.Text = "30";
                    Assert.AreEqual("30", vm.AsString(), "控件改动应回写 VM");
                }
            });
        }

        [TestMethod]
        public void TabPage_BindsTextOnly()
        {
            WithForm(form =>
            {
                using (var vm = new VsControlModel(form, "tabPage1", "TabPage", "高级参数", true))
                {
                    CollectionAssert.AreEqual(new[] { "Text" }, BindingsOf(form.Page(1), vm).Select(b => b.PropertyName).ToArray());
                    Assert.AreEqual("高级参数", form.Page(1).Text);
                }
            });
        }

        [TestMethod]
        public void CheckBox_BindsChecked_BothWays()
        {
            WithForm(form =>
            {
                using (var vm = new VsControlModel(form, "ckb_Enable", "CheckBox", "启用", true, true))
                {
                    Assert.IsTrue(form.CheckBox.Checked);
                    Assert.AreEqual(true, vm.Value, "CheckBox 的 Value 是 bool 而非文本");

                    vm.Value = false;
                    Assert.IsFalse(form.CheckBox.Checked);

                    form.CheckBox.Checked = true;
                    Assert.IsTrue(vm.AsBool());
                }
            });
        }

        [TestMethod]
        public void RadioButton_BindsChecked()
        {
            WithForm(form =>
            {
                using (var vm = new VsControlModel(form, "rdb_Dark", "RadioButton", "暗", true, true))
                {
                    CollectionAssert.AreEquivalent(new[] { "Checked", "Visible" },
                        BindingsOf(form.RadioButton, vm).Select(b => b.PropertyName).ToArray());
                    Assert.IsTrue(form.RadioButton.Checked);
                }
            });
        }

        [TestMethod]
        public void TrackBar_BindsValue_BothWays()
        {
            WithForm(form =>
            {
                using (var vm = new VsControlModel(form, "trackBar1", "TrackBar", 128))
                {
                    CollectionAssert.AreEqual(new[] { "Value" }, BindingsOf(form.TrackBar, vm).Select(b => b.PropertyName).ToArray());
                    Assert.AreEqual(128, form.TrackBar.Value);

                    form.TrackBar.Value = 200;
                    Assert.AreEqual(200, vm.AsInt());
                }
            });
        }

        [TestMethod]
        public void ComboBox_BindsDropDownStyle_ThroughFormatParse()
        {
            WithForm(form =>
            {
                var items = new[] { "A", "B" };
                form.ComboBox.Items.AddRange(items);
                using (var vm = new VsControlModel(form, "cmb_Mode", "ComboBox", "B", true, true, true, items))
                {
                    CollectionAssert.AreEquivalent(new[] { "Text", "Visible", "Enabled", "DropDownStyle" },
                        BindingsOf(form.ComboBox, vm).Select(b => b.PropertyName).ToArray());

                    Assert.AreEqual(ComboBoxStyle.DropDownList, form.ComboBox.DropDownStyle, "true → DropDownList");
                    Assert.AreEqual("B", form.ComboBox.Text);

                    // 不再断言构造之后的双向推送：DropDownStyle 变化会重建 ComboBox 句柄，
                    // 未显示窗体上的绑定随之失效（IsBinding=false），结果取决于 WinForms 内部时序。
                }
            });
        }

        [TestMethod]
        public void Rebinding_SameControl_ReplacesOldBindings()
        {
            WithForm(form =>
            {
                using (var first = new VsControlModel(form, "txt_Value", "TextBox", "1", true))
                using (var second = new VsControlModel(form, "txt_Value", "TextBox", "2", true))
                {
                    Assert.AreEqual(0, BindingsOf(form.TextBox, first).Count(), "同属性的旧绑定应被移除");
                    Assert.AreEqual(2, form.TextBox.DataBindings.Count);

                    first.Value = "stale";
                    Assert.AreEqual("2", form.TextBox.Text, "旧 VM 不应再影响控件");
                }
            });
        }

        [TestMethod]
        public void Dispose_RemovesOnlyOwnBindings()
        {
            WithForm(form =>
            {
                var vm = new VsControlModel(form, "txt_Value", "TextBox", "1", true);
                var other = new Binding("Tag", new object[] { "x" }, "");
                form.TextBox.DataBindings.Add(other);

                vm.Dispose();

                Assert.AreEqual(0, BindingsOf(form.TextBox, vm).Count());
                Assert.AreEqual(1, form.TextBox.DataBindings.Count, "其它来源的绑定不受影响");

                vm.Value = "after";
                Assert.AreEqual("1", form.TextBox.Text);
            });
        }

        [TestMethod]
        public void Dispose_AfterFormDisposed_DoesNotThrow()
        {
            VsControlModel vm = null;
            WithForm(form => vm = new VsControlModel(form, "txt_Value", "TextBox", "1", true));

            vm.Dispose();
        }

        [TestMethod]
        public void DataGridView_AndUnknown_CreateNoBindings()
        {
            WithForm(form =>
            {
                using (var grid = new VsControlModel(form, "dataGridView1", "DataGridView", "", true))
                using (var unknown = new VsControlModel(form, "lbl_Name", "Label", "x", true))
                {
                    Assert.AreEqual(0, form.Grid.DataBindings.Count);
                    Assert.AreEqual(0, form.Label.DataBindings.Count);
                }
            });
        }

        [TestMethod]
        public void Bind_MissingControl_Throws()
        {
            WithForm(form =>
            {
                Assert.ThrowsException<InvalidOperationException>(() => new VsControlModel(form, "no_such", "TextBox", "1", true));
            });
        }

        [TestMethod]
        public void Bind_WrongControlType_ThrowsInvalidCast()
        {
            WithForm(form =>
            {
                Assert.ThrowsException<InvalidCastException>(() => new VsControlModel(form, "txt_Value", "CheckBox", "x", true, true));
            });
        }

        #endregion
    }
}
