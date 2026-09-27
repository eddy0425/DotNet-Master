using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
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
                    var tree = Priv.Get<TreeView>(form, "treeView1");
                    // 双击按鼠标位置命中节点，节点的 Bounds 要有句柄才算得出来；只建句柄、不显示窗体
                    GC.KeepAlive(form.Handle);
                    GC.KeepAlive(tree.Handle);
                    body(form, tree);
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
            var node = Find(tree, path);
            tree.SelectedNode = node;
            node.EnsureVisible();
            DoubleClickAt(form, tree, Center(node.Bounds));
            return Tuple.Create(form.DialogResult == DialogResult.OK, form.StrReturn);
        }

        private static Point Center(Rectangle r) => new Point(r.X + r.Width / 2, r.Y + r.Height / 2);

        private static void DoubleClickAt(ValueForm form, TreeView tree, Point location) =>
            Priv.Call(form, "treeView1_MouseDoubleClick", tree, new MouseEventArgs(MouseButtons.Left, 2, location.X, location.Y, 0));

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

        /// <summary>路径层数不受限：此前只比较前 4 段，更深的变量只能预选到第 4 层的祖先。</summary>
        [TestMethod]
        public void SetSelectNode_DeeperThanFourLevels_SelectsLeaf()
        {
            Run((form, tree) =>
            {
                var strategies = new List<IParaStrategy>
                {
                    new FakeStrategy(AlgoEnum.FitLine, "深层0")
                    {
                        Tree = t => t.Branch("深层0", b => b
                            .Branch("甲", x => x.Branch("乙", y => y.Branch("丙", z => z.Node("丁", OutEnum.Number))))),
                    },
                    new FakeStrategy(AlgoEnum.CreateROI),
                };
                GenerateTree(form, 1, strategies);

                Priv.Call(form, "Fun_setSelectNode", "深层0/甲/乙/丙/丁");

                Assert.AreSame(Find(tree, "深层0/甲/乙/丙/丁"), tree.SelectedNode);
            });
        }

        /// <summary>上游工具还在、只是那个输出没了：退而选中最深的现存祖先，方便用户就近重选。</summary>
        [TestMethod]
        public void SetSelectNode_UnknownOutput_SelectsDeepestExistingAncestor()
        {
            Run((form, tree) =>
            {
                GenerateTree(form, 2, Strategies());

                Priv.Call(form, "Fun_setSelectNode", "直线查找0/直线/已删除");

                Assert.AreSame(Find(tree, "直线查找0/直线"), tree.SelectedNode);
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

                DoubleClickAt(form, tree, Center(tree.Nodes[0].Bounds));

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

        /// <remarks>
        /// 每种情况单独一个窗体：确认时 <c>Close()</c> 一个没以 <c>ShowDialog</c> 显示的窗体会把它 Dispose 掉，
        /// 不能在同一个窗体上接着双击。
        /// </remarks>
        [DataTestMethod]
        [DataRow("形状匹配0/图像", true)]
        [DataRow("直线查找0/区域", true)]     // 现有规则：图像输入也可以引用区域
        [DataRow("直线查找0/直线", false)]
        public void DoubleClick_ImageTarget_AlsoAcceptsRegion(string path, bool accepted)
        {
            Run((form, tree) =>
            {
                GenerateTree(form, 2, Strategies());

                Assert.AreEqual(accepted, DoubleClick(form, tree, path, OutEnum.Image).Item1);
            });
        }

        /// <summary>
        /// 双击树的空白处不算选择。
        /// </summary>
        /// <remarks>
        /// 回归：此前取的是 <c>SelectedNode</c>，左键点空白又不会改变选中项，
        /// 于是双击空白会把上一次单击选中的节点当成结果确认掉。
        /// </remarks>
        [TestMethod]
        public void DoubleClick_BlankArea_Ignored()
        {
            Run((form, tree) =>
            {
                GenerateTree(form, 2, Strategies());
                tree.SelectedNode = Find(tree, "直线查找0/区域");
                form.DialogResult = DialogResult.None;
                form.ValueType = OutEnum.Region;
                form.StrReturn = "旧值";

                DoubleClickAt(form, tree, new Point(5, tree.ClientSize.Height - 5));

                Assert.AreNotEqual(DialogResult.OK, form.DialogResult);
                Assert.AreEqual("旧值", form.StrReturn, "没选到东西就不该动返回值");
            });
        }

        #endregion

        #region 交互

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private const int WM_KEYDOWN = 0x0100, WM_KEYUP = 0x0101;

        /// <summary>
        /// 焦点在树上时按 Esc 关闭引用窗。
        /// </summary>
        /// <remarks>
        /// 回归：Esc 挂在窗体的 KeyUp 上，但窗体里能拿焦点的只有树，
        /// 不开 <c>KeyPreview</c> 的话窗体永远收不到按键，Esc 形同虚设。
        /// </remarks>
        [TestMethod]
        public void Escape_WhileTreeFocused_ClosesDialog()
        {
            Run((form, tree) =>
            {
                GenerateTree(form, 2, Strategies());
                bool closed = false;

                using (WindowHost.RespondWhenShown(form, () =>
                {
                    tree.Focus();
                    SendMessage(tree.Handle, WM_KEYDOWN, (IntPtr)Keys.Escape, IntPtr.Zero);
                    SendMessage(tree.Handle, WM_KEYUP, (IntPtr)Keys.Escape, IntPtr.Zero);
                    closed = form.DialogResult == DialogResult.Cancel;
                }))
                {
                    form.ShowDialog();
                }

                Assert.IsTrue(closed);
            });
        }

        [TestMethod]
        public void ContextMenu_ExpandAll_ThenCollapseAll()
        {
            Run((form, tree) =>
            {
                GenerateTree(form, 2, Strategies());
                var point = Find(tree, "直线查找0/直线/起点");

                Priv.Click(form, "全部展开ToolStripMenuItem_Click");
                Assert.IsTrue(point.IsExpanded && point.Parent.IsExpanded);

                Priv.Click(form, "全部折叠ToolStripMenuItem_Click");
                Assert.IsFalse(tree.Nodes.Cast<TreeNode>().Any(n => n.IsExpanded));
            });
        }

        [TestMethod]
        public void MouseDown_LeftSelectsNodeUnderCursor_RightAttachesContextMenu()
        {
            Run((form, tree) =>
            {
                GenerateTree(form, 2, Strategies());
                var target = Find(tree, "形状匹配0");
                var at = Center(target.Bounds);

                Priv.Call(form, "treeView1_MouseDown", tree, new MouseEventArgs(MouseButtons.Left, 1, at.X, at.Y, 0));
                Assert.AreSame(target, tree.SelectedNode);

                Priv.Call(form, "treeView1_MouseDown", tree, new MouseEventArgs(MouseButtons.Right, 1, at.X, at.Y, 0));
                Assert.AreSame(Priv.Get<ContextMenuStrip>(form, "contextMenuStrip1"), tree.ContextMenuStrip);
            });
        }

        #endregion

        #region 弹出位置

        private static readonly Rectangle WorkArea = new Rectangle(0, 0, 1920, 1040);
        private static readonly Size DialogSize = new Size(223, 571);

        [TestMethod]
        public void Placement_BesideOwner_WhenItFits()
        {
            Assert.AreEqual(new Point(900, 100),
                DialogPlacement.Beside(new Rectangle(100, 100, 800, 600), DialogSize, WorkArea));
        }

        /// <summary>
        /// 主窗靠右、靠下或在屏幕外时，引用窗不能跑出屏幕。
        /// </summary>
        /// <remarks>回归：此前总是贴在主窗右侧；主窗贴右边时引用窗整个落在屏幕外，而它是置顶的模态窗，看起来就像程序卡死。</remarks>
        [DataTestMethod]
        [DataRow(1200, 100, 1697, 100)]    // 右侧放不下：贴屏幕右缘
        [DataRow(100, 800, 900, 469)]      // 下方放不下：贴屏幕下缘
        [DataRow(-3000, -3000, 0, 0)]      // 主窗在屏幕外
        public void Placement_ClampedToWorkingArea(int ownerX, int ownerY, int expectedX, int expectedY)
        {
            Assert.AreEqual(new Point(expectedX, expectedY),
                DialogPlacement.Beside(new Rectangle(ownerX, ownerY, 800, 600), DialogSize, WorkArea));
        }

        [TestMethod]
        public void Placement_NoOwner_UsesDefaultPoint()
        {
            Assert.AreEqual(new Point(500, 300), DialogPlacement.Beside(null, DialogSize, WorkArea));
        }

        /// <summary>默认位置按工作区偏移：副屏上的主窗最大化时，弹窗留在副屏。</summary>
        [TestMethod]
        public void Placement_NoOwner_DefaultIsRelativeToWorkingArea()
        {
            var secondary = new Rectangle(1920, 0, 1920, 1040);
            Assert.AreEqual(new Point(2420, 300), DialogPlacement.Beside(null, DialogSize, secondary));
        }

        [TestMethod]
        public void Placement_MaximizedOwnerForm_UsesDefaultPoint()
        {
            Sta.Run(() =>
            {
                using (var owner = new Form { WindowState = FormWindowState.Maximized })
                    Assert.AreEqual(DialogPlacement.Beside(null, DialogSize, Screen.FromControl(owner).WorkingArea),
                        DialogPlacement.Beside(owner, DialogSize));
                Assert.AreEqual(DialogPlacement.Beside(null, DialogSize, Screen.FromPoint(Cursor.Position).WorkingArea),
                    DialogPlacement.Beside((Form)null, DialogSize));
            });
        }

        /// <summary>真实窗体走的是 <see cref="Form"/> 重载：取主窗所在屏幕的工作区，结果必须落在工作区内。</summary>
        [TestMethod]
        public void Placement_OwnerForm_StaysInsideOwnersScreen()
        {
            Sta.Run(() =>
            {
                var area = Screen.PrimaryScreen.WorkingArea;
                // 主窗整个在主屏内、右缘贴屏幕右缘；伸出屏幕的话多显示器下 FromControl 可能选到别的屏
                using (var owner = new Form { StartPosition = FormStartPosition.Manual, Bounds = new Rectangle(area.Right - 400, area.Top, 400, 300) })
                {
                    var at = DialogPlacement.Beside(owner, DialogSize);

                    Assert.IsTrue(new Rectangle(at, DialogSize).Right <= area.Right, "贴右缘的主窗旁边放不下，应挪回屏幕内");
                    Assert.AreEqual(area.Top, at.Y);
                }
            });
        }

        [TestMethod]
        public void Shown_WithOwner_PlacedBesideOwner()
        {
            Run((form, tree) =>
            {
                using (var owner = new Form { StartPosition = FormStartPosition.Manual, Bounds = new Rectangle(100, 100, 400, 300), ShowInTaskbar = false })
                {
                    owner.Show();
                    Point? shownAt = null;
                    using (WindowHost.RespondWhenShown(form, () => shownAt = form.Location))
                        form.ShowDialog(owner);

                    Assert.AreEqual(DialogPlacement.Beside(owner, form.Size), shownAt);
                }
            });
        }

        #endregion
    }
}
