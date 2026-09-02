using Sunny.UI;
using System;
using DotNet.HalconUI;
using DotNet.Vision.Abstractions;
using DotNet.HalconAlgo;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing;


namespace DotNet.VisionMaster
{
    public partial class ValueForm : UIForm
    {
        int _runIndex;
        public string StrReturn;
        public OutEnum ValueType;

        char varSplit = '/';
        char valSplit = ';';

        IWin32Window _owner;

        public ValueForm(IWin32Window owner)
        {
            InitializeComponent();
            _owner = owner;
        }
        public void setValueForm(int runIndex, List<IParaStrategy> _strategys, string strOrg, OutEnum type)
        {
            _runIndex = runIndex;
            StrReturn = strOrg;
            ValueType = type;

            GenerateTree(runIndex, _strategys);
            Fun_setSelectNode(StrReturn);
            this.ShowDialog(_owner);
        }
        private void Fun_setSelectNode(string strIn)      //更新程序树选中节点
        {
            try
            {
                if (string.IsNullOrWhiteSpace(strIn)) return;
                string[] arrStr = strIn.Split(varSplit);
                TreeNode node = new TreeNode(); string se = treeView1.PathSeparator;
                if (arrStr.Length >= 1)
                {
                    foreach (TreeNode item in treeView1.Nodes)
                    {
                        if (item.Text == "默认") return;
                        if (item.Text == arrStr[0])
                        {
                            node = item;
                            break;
                        }
                    }
                }
                if (arrStr.Length >= 2)
                {
                    foreach (TreeNode item in node.Nodes)
                    {
                        if (item.Text == arrStr[1])
                        {
                            node = item;
                            break;
                        }
                    }
                }
                if (arrStr.Length >= 3)
                {
                    foreach (TreeNode item in node.Nodes)
                    {
                        if (item.Text == arrStr[2])
                        {
                            node = item;
                            break;
                        }
                    }
                }
                if (arrStr.Length >= 4)
                {
                    foreach (TreeNode item in node.Nodes)
                    {
                        if (item.Text == arrStr[3])
                        {
                            node = item;
                            break;
                        }
                    }
                }
                treeView1.SelectedNode = node;
                treeView1.SelectedNode.EnsureVisible();
            }
            catch { }
        }

        // 最终调用形式（与YAML结构1:1对应）
        private void GenerateTree(TreeView treeView1)
        {
            treeView1.Nodes.Clear();

            new TreeVisualizer(treeView1)
                .Branch("直线查找0", branch => branch
                    .Node("直线", OutEnum.Line, line => line
                        .Branch("起点", pt => pt
                            .Node("行", OutEnum.Number)
                            .Node("列", OutEnum.Number)
                        )
                        .Branch("终点", pt => pt
                            .Node("行", OutEnum.Number)
                            .Node("列", OutEnum.Number)
                        )
                    )
                    .CommonNodes()
                )
                .Branch("点线垂线0", branch => branch
                    .Node("直线", OutEnum.Line, line => line
                        .ReusePointStructure("起点")  // 复用结构
                        .ReusePointStructure("终点")
                    )
                    .CommonNodes()
                )
                .Branches("点线垂线1", "点线垂线2"); // 空节点生成
        }

        private void GenerateTree(int index, List<IParaStrategy> paraStrategies)    //生成输出变量节点
        {
            treeView1.Nodes.Clear();
            treeView1.Nodes.Add(new TreeNode("默认"));

            if(index >= paraStrategies.Count) return;
            TreeVisualizer treeVisualizer = new TreeVisualizer(treeView1);
            for (int i = 0; i < index; i++)
            {
                if (paraStrategies[i] is ITreeNodeProvider provider)
                    provider.GenTreeNode(treeVisualizer);
            }
        }
 
        public string Fun_getText(TreeNode node, string str)
        {
            str = varSplit + node.Text + str;
            if (node.Level > 0)
            {
                return Fun_getText(node.Parent, str);  // 递归处理父节点
            }
            return str;  // 返回最终结果
        }

        private void 全部展开ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeView1.ExpandAll();
        }
        private void 全部折叠ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeView1.CollapseAll();
        }
        private void treeView1_MouseDown(object sender, MouseEventArgs e)  //当鼠标指针在组件上方并按下鼠标按钮时发生
        {
            if (e.Button == MouseButtons.Left)  //鼠标左键获取选的节点
            {
                TreeNode SelectedNode = treeView1.GetNodeAt(e.Location);

                if (SelectedNode is TreeNode)  //判断是否为节点
                {
                    treeView1.SelectedNode = SelectedNode;
                }
            }
            else if (e.Button == MouseButtons.Right) //鼠标右键获取选的节点
            {
                treeView1.ContextMenuStrip = contextMenuStrip1;// 添加右键菜单             
            }
        }
        private void treeView1_DrawNode(object sender, DrawTreeNodeEventArgs e)  //当需要绘制节点时，在所有者描述模式下发生
        {
            ////绘制文字      
            //int cmdIndex = schemePara.defaultJob.ToolInfos.FindIndex(item => item.Text.Equals(e.Node.Text));
            //string text = (e.Node.Level == 0 && e.Node.Text != "默认") ? cmdIndex.ToString() + ". " + e.Node.Text : e.Node.Text;
            //e.Graphics.DrawString(text, treeView1.Font, new SolidBrush(Color.Black), e.Node.Bounds.X, e.Node.Bounds.Top + (e.Node.Bounds.Height - treeView1.Font.Height) / 2);
        }
        private void treeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e) //在将要展开节点时发生
        {
            treeView1.Invalidate();
        }
        private void treeView1_MouseDoubleClick(object sender, MouseEventArgs e)  //用鼠标双击控件时发生 //来源 chatgpt
        {
            StrReturn = "";
            TreeNode currentNode = treeView1.SelectedNode;
            if (currentNode == null) return;

            // 检查根节点及类型
            if (currentNode.Level == 0)
            {
                if (currentNode != treeView1.Nodes[0]) return;
                if (ValueType != OutEnum.Image && ValueType != OutEnum.Region && ValueType != OutEnum.Coord) return;
            }

            // 检查节点名称和类型匹配
            if (currentNode.Name != ValueType.ToString() && currentNode != treeView1.Nodes[0])
            {
                switch (ValueType)
                {
                    case OutEnum.String:
                        if (currentNode.Name == nameof(OutEnum.HTuple) ||
                            currentNode.Name == nameof(OutEnum.Outline) ||
                            currentNode.Name == nameof(OutEnum.Image) ||
                            currentNode.Name == nameof(OutEnum.Region)) return;
                        break;

                    case OutEnum.CalOrOut:
                        if (currentNode.Name != nameof(OutEnum.Angle) &&
                            currentNode.Name != nameof(OutEnum.Number) &&
                            currentNode.Name != nameof(OutEnum.String)) return;
                        break;

                    case OutEnum.Angle:
                        if (currentNode.Name != nameof(OutEnum.CalOrOut)) return;
                        break;

                    case OutEnum.Array:
                        if (currentNode.Name != nameof(OutEnum.Array)) return;
                        break;

                    case OutEnum.Image:
                        if (currentNode.Name != nameof(OutEnum.Region)) return;
                        break;

                    default:
                        return;
                }
            }

            // 获取节点文本并关闭窗口
            StrReturn = Fun_getText(currentNode, StrReturn).Substring(1);
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void ValueForm_KeyUp(object sender, KeyEventArgs e)  //在释放时发生
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }
        private void ValueForm_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible)
            {
                Point point = new Point(500, 300);
                Form ownerForm = this.Owner;
                if (ownerForm != null && ownerForm.WindowState != FormWindowState.Maximized)
                {
                    point = new Point(ownerForm.Location.X + ownerForm.Width, ownerForm.Location.Y);
                }

                this.Location = point;
            }
        }
        private void ValueForm_ExtendBoxClick(object sender, EventArgs e)
        {
            //TopMost = !TopMost;

            //if (TopMost)
            //    ExtendSymbol = 61475;
            //else
            //    ExtendSymbol = 61758;
        }

    }
}
