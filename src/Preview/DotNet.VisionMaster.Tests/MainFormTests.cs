using System;
using System.Collections.Generic;
using System.Linq;
using DotNet.HalconAlgo;
using DotNet.HalconCore;
using DotNet.HalconUI;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNet.VisionMaster.Tests
{
    /// <summary>
    /// <see cref="MainForm"/>：装配 8 个算法策略，按钮切换工具时与 <see cref="ParaForm"/> 保持同一个索引。
    /// </summary>
    /// <remarks>
    /// 绘制进行中切换工具的那道闸门会弹 <c>MessageBox</c>，无人值守的测试里没法点掉，这里不覆盖；
    /// 闸门的判据 <see cref="ParaForm.IsDrawBusy"/> 由 <see cref="ParaFormTests"/> 覆盖。
    /// </remarks>
    [TestClass]
    public class MainFormTests : HalconTestBase
    {
        private bool _savedUIBlock;

        [TestInitialize]
        public void Init() => _savedUIBlock = AlgoPaths.UIBlock;

        [TestCleanup]
        public void Cleanup()
        {
            AlgoPaths.UIBlock = _savedUIBlock;
            // MainForm 会 new 一个 LogFile 挂到 JsonLog 上且不退订，见 LogFileTests
            Priv.ResetJsonLog();
        }

        private static void Run(Action<MainForm> body) =>
            Sta.Run(() =>
            {
                using (var form = new MainForm())
                {
                    WindowHost.ShowOffscreen(form);
                    body(form);
                }
            });

        private static List<IParaStrategy> Strategies(MainForm form) => Priv.Get<List<IParaStrategy>>(form, "_strategys");

        [TestMethod]
        public void Constructor_RegistersStrategiesInButtonOrder()
        {
            Run(form =>
            {
                CollectionAssert.AreEqual(new[]
                {
                    typeof(FileImageStrategy),
                    typeof(CreateROIStrategy),
                    typeof(ShapeModelStrategy),
                    typeof(FitLineStrategy),
                    typeof(FitArcMidpointStrategy),
                    typeof(NccModelStrategy),
                    typeof(ScaledModelStrategy),
                    typeof(GenericModelStrategy),
                }, Strategies(form).Select(s => s.GetType()).ToArray());
            });
        }

        [TestMethod]
        public void Constructor_DisablesUIBlock_AndPresetsImageFolder()
        {
            AlgoPaths.UIBlock = true;

            Run(form =>
            {
                Assert.IsFalse(AlgoPaths.UIBlock);
                Assert.AreEqual("D:\\testImage\\FitArcMidpoint", ((FileImageStrategy)Strategies(form)[0]).inPara.ImageFolder);
            });
        }

        [TestMethod]
        public void ToolButtons_SwitchToMatchingStrategy_AndSyncParaForm()
        {
            Run(form =>
            {
                var para = Priv.Get<ParaForm>(form, "_formPara");

                for (int i = 0; i < 8; i++)
                {
                    Priv.Click(form, "button" + (i + 1) + "_Click");

                    Assert.AreEqual(i, Priv.Get<int>(form, "_index"), $"button{i + 1}");
                    Assert.AreEqual(i, Priv.Get<int>(para, "_index"), $"button{i + 1}: ParaForm 的索引必须跟宿主一致");
                    Assert.AreSame(Strategies(form), Priv.Get<List<IParaStrategy>>(para, "_strategys"));
                }
            });
        }

        [TestMethod]
        public void SwitchStrategy_ClearsPreviousBindings()
        {
            Run(form =>
            {
                var bindings = Priv.Get<Dictionary<string, VsControlModel>>(form, "_vsControls");

                Priv.Click(form, "button3_Click");
                var previous = bindings.Values.ToList();
                Assert.IsTrue(previous.Count > 0, "前提：形状匹配的参数页应建立控件绑定");

                Priv.Click(form, "button4_Click");

                Assert.IsFalse(previous.Any(bindings.ContainsValue), "切换工具前必须清掉上一个工具的控件绑定");
            });
        }
    }
}
