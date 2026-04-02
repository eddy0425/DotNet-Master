using System;
using System.Windows.Forms;

namespace DotNet.VisionMaster
{
    
    // 实现核心（约60行）
    public class TreeVisualizer
    {
        private readonly TreeView _tree;

        public TreeVisualizer(TreeView tree) => _tree = tree;

        /// <summary>
        /// 添加一个分支节点（包含子节点配置）
        /// </summary>
        public TreeVisualizer Branch(string text, Action<TreeBranch> config)
        {
            var branch = new TreeBranch(_tree.Nodes.Add(text));
            config(branch);
            return this;
        }

        /// <summary>
        /// 添加多个空节点分支
        /// </summary>
        public TreeVisualizer Branches(params string[] texts)
        {
            foreach (var text in texts)
                _tree.Nodes.Add(text);
            return this;
        }
    }

    /// <summary>
    /// 树分支构建器，支持链式调用
    /// </summary>
    public class TreeBranch
    {
        private readonly TreeNode _node;

        public TreeBranch(TreeNode node) => _node = node;

        /// <summary>
        /// 添加一个子节点
        /// </summary>
        public TreeBranch Node(string text, OutEnum type, Action<TreeBranch> config = null)
        {
            var child = _node.Nodes.Add(text);
            child.Name = type.ToString();
            config?.Invoke(new TreeBranch(child));
            return this;
        }

        /// <summary>
        /// 添加一个分支节点（用于嵌套结构）
        /// </summary>
        public TreeBranch Branch(string text, Action<TreeBranch> config)
        {
            var branch = new TreeBranch(_node.Nodes.Add(text));
            config(branch);
            return this;
        }

        /// <summary>
        /// 复用点结构（起点/终点）
        /// </summary>
        public TreeBranch ReusePointStructure(string pointName)
        {
            return Node(pointName, OutEnum.Point, pt => pt
                .Node("行", OutEnum.Number)
                .Node("列", OutEnum.Number)
            );
        }

        /// <summary>
        /// 添加通用节点（结果、文本显示）
        /// </summary>
        public TreeBranch CommonNodes()
        {
            return this
                .Node("结果", OutEnum.Result)
                .Node("文本显示", OutEnum.String);
        }
    }
}