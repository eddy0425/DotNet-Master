using System;
using System.Windows.Forms;
using DotNet.Vision.Abstractions;


namespace DotNet.HalconUI
{
    /// <summary>
    /// <see cref="ITreeVisualizer"/> 的 WinForms 实现：把树节点声明落到 <see cref="TreeView"/> 上。
    /// </summary>
    /// <remarks>
    /// 契约（<see cref="ITreeVisualizer"/> / <see cref="ITreeBranch"/>）定义在 DotNet.Vision.Abstractions，
    /// 算法层的 <c>GenTreeNode</c> 只认接口，因此不再依赖 System.Windows.Forms。
    /// </remarks>
    public class TreeVisualizer : ITreeVisualizer
    {
        private readonly TreeView _tree;

        public TreeVisualizer(TreeView tree) => _tree = tree;

        /// <summary>
        /// 添加一个分支节点（包含子节点配置）
        /// </summary>
        public ITreeVisualizer Branch(string text, Action<ITreeBranch> config)
        {
            var branch = new TreeBranch(_tree.Nodes.Add(text));
            config(branch);
            return this;
        }

        /// <summary>
        /// 添加多个空节点分支
        /// </summary>
        public ITreeVisualizer Branches(params string[] texts)
        {
            foreach (var text in texts)
                _tree.Nodes.Add(text);
            return this;
        }
    }

    /// <summary>
    /// 树分支构建器，支持链式调用
    /// </summary>
    public class TreeBranch : ITreeBranch
    {
        private readonly TreeNode _node;

        public TreeBranch(TreeNode node) => _node = node;

        /// <summary>
        /// 添加一个子节点
        /// </summary>
        public ITreeBranch Node(string text, OutEnum type, Action<ITreeBranch> config = null)
        {
            var child = _node.Nodes.Add(text);
            child.Name = type.ToString();
            config?.Invoke(new TreeBranch(child));
            return this;
        }

        /// <summary>
        /// 添加一个分支节点（用于嵌套结构）
        /// </summary>
        public ITreeBranch Branch(string text, Action<ITreeBranch> config)
        {
            var branch = new TreeBranch(_node.Nodes.Add(text));
            config(branch);
            return this;
        }

        /// <summary>
        /// 复用点结构（起点/终点）
        /// </summary>
        public ITreeBranch ReusePointStructure(string pointName)
        {
            return Node(pointName, OutEnum.Point, pt => pt
                .Node("行", OutEnum.Number)
                .Node("列", OutEnum.Number)
            );
        }

        /// <summary>
        /// 添加通用节点（结果、文本显示）
        /// </summary>
        public ITreeBranch CommonNodes()
        {
            return this
                .Node("结果", OutEnum.Result)
                .Node("文本显示", OutEnum.String);
        }
    }
}
