using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using DotNet.HalconCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNet.VisionMaster.Tests
{
    /// <summary>
    /// <see cref="ValueForm"/>：按运行顺序生成上游输出变量树、按路径预选节点、双击时按类型过滤并返回变量路径。
    /// </summary>
    /// <remarks>
    /// <c>setValueForm</c> 以 <c>ShowDialog</c> 结尾，无法在无人值守的测试里调用；
    /// 这里用反射分别调它里面的 <c>GenerateTree</c> / <c>Fun_setSelectNode</c>，以及双击处理方法。
    /// </remarks>
    [TestClass]
    public class ValueFormTests
    {
        /// <summary>两个上游工具：直线查找（区域 + 直线/起点/行）与模板匹配（图像 + 坐标）。</summary>
        private static List<IParaStrategy> Strategies() => new List<IParaStrategy>
        {
            new FakeStrategy(AlgoEnum.FitLine, "直线查找0")
            {
                Tree = t => t.Branch("直线查找0", b => b
                    .Node("区域", OutEnum.Region)
                    .Node("直线", OutEnum.Line, l => l.ReusePointStructure("起点"))
                    .Node("角度", OutEnum.Angle)),
            },
            new FakeStrategy(AlgoEnum.ShapeModel, "形状匹配0")
            {
                Tree = t => t.Branch("形状匹配0", b => b
                    .Node("图像", OutEnum.Image)
                    .Node("坐标", OutEnum.Coord)
                    .Node("分数", OutEnum.Number)),
            },
            new FakeStrategy(AlgoEnum.CreateROI, "当前工具")
            {
                Tree = t => t.Branch("当前工具", b => b.Node("区域", OutEnum.Region)),
            },
        };

        private static void Run(Action<ValueForm, TreeView> body) =>
            Sta.Run(() =>
            {
                using (var form = new ValueForm(null))
                {
                    body(form, Priv.Get<TreeView>(form, "treeView1"));
                }
            });

        private static void GenerateTree(ValueForm form, int index, List<IParaStrategy> strategies) =>
            Priv.Call(form, "GenerateTree", index, strategies);

        private static TreeNode Find(TreeView tree, string path)
        {
            var parts = path.Split('/');
            var node = tree.Nodes.Cast<TreeNode>().Single(n => n.Text == parts[0]);
            foreach (var part in parts.Skip(1))
                node = node.Nodes.Cast<TreeNode>().Single(n => n.Text == part);
            return node;
        }

        /// <summary>选中 <paramref name="path"/> 后双击，返回 (是否确认, StrReturn)。</summary>
        private static Tuple<bool, string> DoubleClick(ValueForm form, TreeView tree, string path, OutEnum type)
        {
            form.DialogResult = DialogResult.None;
            form.ValueType = type;
            form.StrReturn = "旧值";
            tree.SelectedNode = Find(tree, path);
            Priv.Call(form, "treeView1_MouseDoubleClick", tree, new MouseEventArgs(MouseButtons.Left, 2, 0, 0, 0));
            return Tuple.Create(form.DialogResult == DialogResult.OK, form.StrReturn);
        }

        #region GenerateTree

        [TestMethod]
        public void GenerateTree_ListsDefaultThenOnlyUpstreamTools()
        {
            Run((form, tree) =>
            {
                GenerateTree(form, 2, Strategies());

                CollectionAssert.AreEqual(new[] { "默认", "直线查找0", "形状匹配0" },
                    tree.Nodes.Cast<TreeNode>().Select(n => n.Text).ToArray(),
                    "只能引用排在当前工具之前的输出，当前工具自身不应出现");
            });
        }

        [TestMethod]
        public void GenerateTree_FirstTool_OnlyDefault()
        {
            Run((form, tree) =>
            {
                GenerateTree(form, 0, Strategies());

                Assert.AreEqual(1, tree.Nodes.Count);
                Assert.AreEqual("默认", tree.Nodes[0].Text);
            });
        }

        [TestMethod]
        public void GenerateTree_IndexOutOfRange_OnlyDefault()
        {
            Run((form, tree) =>
            {
                GenerateTree(form, 99, Strategies());

                Assert.AreEqual(1, tree.Nodes.Count);
            });
        }

        [TestMethod]
        public void GenerateTree_CalledAgain_ReplacesPreviousTree()
        {
            Run((form, tree) =>
            {
                GenerateTree(form, 2, Strategies());
                GenerateTree(form, 1, Strategies());

                CollectionAssert.AreEqual(new[] { "默认", "直线查找0" },
                    tree.Nodes.Cast<TreeNode>().Select(n => n.Text).ToArray(), "同一窗体被 ParaForm 复用，每次打开都要重建");
            });
        }

        [TestMethod]
        public void GenerateTree_SkipsStrategiesWithoutTreeProvider()
        {
            Run((form, tree) =>
            {
                var strategies = Strategies();
                strategies.Insert(0, new PlainStrategy());

                GenerateTree(form, 2, strategies);

                CollectionAssert.AreEqual(new[] { "默认", "直线查找0" }, tree.Nodes.Cast<TreeNode>().Select(n => n.Text).ToArray());
            });
        }

        /// <summary>只实现 <see cref="IParaStrategy"/>、不提供输出树的策略。</summary>
        private sealed class PlainStrategy : IParaStrategy
        {
            public AlgoEnum Algorithm => AlgoEnum.Undefined;
            public string Name { get; set; }
            public int RunIndex { get; set; }
            public void Init(IRoiHost host) { }
            public void Close(IRoiHost host) { }
            public bool Fun_action(IHDisplay display, List<IParaStrategy> strategys) => true;
            public bool Fun_action(HalconDotNet.HObject ho_Image, IHDisplay display) => true;
            public object ResolveOutput(string[] path) => null;
            public T ResolveOutput<T>(string[] path) => default(T);
            public bool TryResolveOutput<T>(string[] path, out T value) { value = default(T); return false; }
        }

        #endregion

        #region Fun_getText

        [TestMethod]
        public void GetText_JoinsPathFromRoot()
        {
            Run((form, tree) =>
            {
                GenerateTree(form, 2, Strategies());

                Assert.AreEqual("/直线查找0/直线/起点/行", form.Fun_getText(Find(tree, "直线查找0/直线/起点/行"), ""));
                Assert.AreEqual("/默认", form.Fun_getText(tree.Nodes[0], ""));
            });
        }

        #endregion

        #region Fun_setSelectNode

        /// <summary>
        /// 再次打开时应预选上次选中的变量。
        /// </summary>
        /// <remarks>
        /// 回归：曾经遍历根节点时一遇到 "默认" 就 return，而 "默认" 总是第一个根节点，
        /// 所以只要路径不为空，预选一律不生效。
        /// </remarks>
        [DataTestMethod]
        [DataRow("默认")]
        [DataRow("直线查找0/区域")]
        [DataRow("直线查找0/直线/起点/行")]
        [DataRow("形状匹配0/坐标")]
        public void SetSelectNode_SelectsPreviousVariable(string path)
        {
            Run((form, tree) =>
            {
                GenerateTree(form, 2, Strategies());

                Priv.Call(form, "Fun_setSelectNode", path);

                Assert.AreSame(Find(tree, path), tree.SelectedNode);
            });
        }

        [TestMethod]
        public void SetSelectNode_UnknownTool_DoesNotThrowOrSelect()
        {
            Run((form, tree) =>
            {
                GenerateTree(form, 2, Strategies());

                Priv.Call(form, "Fun_setSelectNode", "已删除的工具/区域");

                Assert.IsNull(tree.SelectedNode, "上游工具已删除时不应选中任何节点");
            });
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        public void SetSelectNode_BlankPath_KeepsSelectionEmpty(string path)
        {
            Run((form, tree) =>
            {
                GenerateTree(form, 2, Strategies());

                Priv.Call(form, "Fun_setSelectNode", path);

                Assert.IsNull(tree.SelectedNode);
            });
        }

        #endregion

        #region 双击选择

        [TestMethod]
        public void DoubleClick_MatchingType_ReturnsPathWithoutLeadingSeparator()
        {
            Run((form, tree) =>
            {
                GenerateTree(form, 2, Strategies());

                var r = DoubleClick(form, tree, "直线查找0/区域", OutEnum.Region);

                Assert.IsTrue(r.Item1);
                Assert.AreEqual("直线查找0/区域", r.Item2);
            });
        }

        [TestMethod]
        public void DoubleClick_NestedNumber_ReturnsFullPath()
        {
            Run((form, tree) =>
            {
                GenerateTree(form, 2, Strategies());

                var r = DoubleClick(form, tree, "直线查找0/直线/起点/行", OutEnum.Number);

                Assert.IsTrue(r.Item1);
                Assert.AreEqual("直线查找0/直线/起点/行", r.Item2);
            });
        }

        [TestMethod]
        public void DoubleClick_MismatchedType_Rejected()
        {
            Run((form, tree) =>
            {
                GenerateTree(form, 2, Strategies());

                var r = DoubleClick(form, tree, "直线查找0/直线", OutEnum.Region);

                Assert.IsFalse(r.Item1, "要区域却选了直线，不应确认");
                Assert.AreEqual("", r.Item2, "拒绝时清空返回值，调用方只在 OK 时读取");
            });
        }

        [DataTestMethod]
        [DataRow(OutEnum.Image)]
        [DataRow(OutEnum.Region)]
        [DataRow(OutEnum.Coord)]
        public void DoubleClick_Default_AcceptedForImageRegionCoord(OutEnum type)
        {
            Run((form, tree) =>
            {
                GenerateTree(form, 2, Strategies());

                var r = DoubleClick(form, tree, "默认", type);

                Assert.IsTrue(r.Item1);
                Assert.AreEqual("默认", r.Item2);
            });
        }

        [DataTestMethod]
        [DataRow(OutEnum.Line)]
        [DataRow(OutEnum.Number)]
        [DataRow(OutEnum.String)]
        public void DoubleClick_Default_RejectedForOtherTypes(OutEnum type)
        {
            Run((form, tree) =>
            {
                GenerateTree(form, 2, Strategies());

                Assert.IsFalse(DoubleClick(form, tree, "默认", type).Item1);
            });
        }

        [TestMethod]
        public void DoubleClick_ToolRootNode_Rejected()
        {
            Run((form, tree) =>
            {
                GenerateTree(form, 2, Strategies());

                Assert.IsFalse(DoubleClick(form, tree, "直线查找0", OutEnum.Region).Item1, "工具分组节点本身不是变量");
            });
        }

        [TestMethod]
        public void DoubleClick_NothingSelected_Ignored()
        {
            Run((form, tree) =>
            {
                GenerateTree(form, 2, Strategies());
                form.StrReturn = "旧值";

                Priv.Call(form, "treeView1_MouseDoubleClick", tree, new MouseEventArgs(MouseButtons.Left, 2, 0, 0, 0));

                Assert.AreNotEqual(DialogResult.OK, form.DialogResult);
            });
        }

        [DataTestMethod]
        [DataRow("直线查找0/直线/起点/行", true)]     // Number
        [DataRow("直线查找0/角度", true)]            // Angle
        [DataRow("直线查找0/区域", false)]           // Region
        [DataRow("形状匹配0/图像", false)]           // Image
        public void DoubleClick_StringTarget_AcceptsScalarsOnly(string path, bool accepted)
        {
            Run((form, tree) =>
            {
                GenerateTree(form, 2, Strategies());

                Assert.AreEqual(accepted, DoubleClick(form, tree, path, OutEnum.String).Item1);
            });
        }

        [DataTestMethod]
        [DataRow("直线查找0/角度", true)]
        [DataRow("形状匹配0/分数", true)]
        [DataRow("直线查找0/区域", false)]
        [DataRow("形状匹配0/坐标", false)]
        public void DoubleClick_CalOrOutTarget_AcceptsAngleNumberString(string path, bool accepted)
        {
            Run((form, tree) =>
            {
                GenerateTree(form, 2, Strategies());

                Assert.AreEqual(accepted, DoubleClick(form, tree, path, OutEnum.CalOrOut).Item1);
            });
        }

        [TestMethod]
        public void DoubleClick_ImageTarget_AlsoAcceptsRegion()
        {
            Run((form, tree) =>
            {
                GenerateTree(form, 2, Strategies());

                Assert.IsTrue(DoubleClick(form, tree, "形状匹配0/图像", OutEnum.Image).Item1);
                Assert.IsTrue(DoubleClick(form, tree, "直线查找0/区域", OutEnum.Image).Item1, "现有规则：图像输入也可以引用区域");
                Assert.IsFalse(DoubleClick(form, tree, "直线查找0/直线", OutEnum.Image).Item1);
            });
        }

        #endregion
    }
}
