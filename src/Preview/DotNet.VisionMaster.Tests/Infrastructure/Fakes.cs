using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNet.Drawing;
using DotNet.HalconCore;
using HalconDotNet;

namespace DotNet.VisionMaster.Tests
{
    /// <summary>
    /// 可配置的算法策略：记录 ParaForm / ValueForm 对它发起的调用，绘制入口返回可由测试控制完成时机的任务。
    /// </summary>
    internal sealed class FakeStrategy : IParaStrategy, IRoiEditable, ITemplateEditable, ITreeNodeProvider
    {
        public FakeStrategy(AlgoEnum algorithm, string name = "fake")
        {
            Algorithm = algorithm;
            Name = name;
        }

        public AlgoEnum Algorithm { get; }
        public string Name { get; set; }
        public int RunIndex { get; set; }

        /// <summary>DrawROIAsync / SetTemplateAsync 的调用记录：(类型, 是否新建)。</summary>
        public readonly List<Tuple<RectEnum, bool>> RoiDraws = new List<Tuple<RectEnum, bool>>();
        public readonly List<Tuple<RectEnum, bool>> TemplateDraws = new List<Tuple<RectEnum, bool>>();

        /// <summary>绘制入口 await 的任务；测试调用 <see cref="FinishDraw"/> 才算绘制结束。</summary>
        private TaskCompletionSource<bool> _pending = new TaskCompletionSource<bool>();

        public void FinishDraw()
        {
            var done = _pending;
            _pending = new TaskCompletionSource<bool>();
            done.SetResult(true);
        }

        /// <summary>GenTreeNode 写入输出树的内容；null 时不写。</summary>
        public Action<ITreeVisualizer> Tree;

        public Task DrawROIAsync(IRoiHost host, RectEnum type, bool newROI)
        {
            RoiDraws.Add(Tuple.Create(type, newROI));
            return _pending.Task;
        }

        public Task SetTemplateAsync(IRoiHost host, RectEnum type, bool newModel)
        {
            TemplateDraws.Add(Tuple.Create(type, newModel));
            return _pending.Task;
        }

        public void DispROI(IRoiHost host) { }

        public void GenTreeNode(ITreeVisualizer tree) => Tree?.Invoke(tree);

        public void Init(IRoiHost host) { }
        public void Close(IRoiHost host) { }
        public bool Fun_action(IHDisplay display, List<IParaStrategy> strategys) => true;
        public bool Fun_action(HObject ho_Image, IHDisplay display) => true;

        public object ResolveOutput(string[] path) => null;
        public T ResolveOutput<T>(string[] path) => default(T);
        public bool TryResolveOutput<T>(string[] path, out T value)
        {
            value = default(T);
            return false;
        }
    }
}
