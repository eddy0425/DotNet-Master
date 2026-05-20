using DotNet.Drawing;
using DotNet.HalconUI;
using System;
using System.Collections.Generic;
using System.Windows.Forms;


namespace DotNet.HalconAlgo
{
    /// <summary>
    /// 算法参数策略接口
    /// </summary>
    public interface IParaStrategy
    {
        AlgoEnum Algorithm { get; }
        string Name { get; set; }
        int RunIndex { get; set; }
        object ResolveOutput(string[] path);
        T ResolveOutput<T>(string[] path);

        void Init(HDisplayUI display);
        void Close(HDisplayUI display);
        void GenTreeNode(TreeVisualizer tree);

        bool Fun_action(IHDisplay display, List<IParaStrategy> strategys);
        void DispPara(Control form, Dictionary<string, VsControlModel> VsControls);
        void SavePara(Control form, Dictionary<string, VsControlModel> VsControls);
        void DrawROI(HDisplayUI display, RectEnum type, bool newROI);
        void DispROI(HDisplayUI display);
        void SetTemplate(HDisplayUI display, RectEnum type, bool newModel);
    }

    /// <summary>
    /// 策略抽象基类：自动初始化参数实例，子类只需实现 DispPara / SavePara
    /// </summary>
    public abstract class ParaStrategyBase<TPara> : IParaStrategy where TPara : class, new()
    {
        private readonly Dictionary<string, Func<object>> _resolvers = new Dictionary<string, Func<object>>();

        public abstract AlgoEnum Algorithm { get; }
        public abstract string Name { get; set; }
        public abstract int RunIndex { get; set; }
        public TPara inPara { get; set; } = new TPara();
        protected void RegisterOutput(string path, Func<object> resolver) => _resolvers[path] = resolver;
        protected void ClearResolvers() => _resolvers.Clear();
        public T ResolveOutput<T>(string[] path) => (T)ResolveOutput(path);
        public object ResolveOutput(string[] path)
        {
            for (int depth = path.Length; depth >= 1; depth--)
            {
                var key = string.Join("/", path, 0, depth);
                if (_resolvers.TryGetValue(key, out var resolver))
                    return resolver();
            }
            return null;
        }

        public virtual void Init(HDisplayUI display) { }
        public virtual void Close(HDisplayUI display) { }
        public abstract void GenTreeNode(TreeVisualizer tree);
  
        public abstract bool Fun_action(IHDisplay display, List<IParaStrategy> strategys);
        public abstract void DispPara(Control form, Dictionary<string, VsControlModel> VsControls);
        public abstract void SavePara(Control form, Dictionary<string, VsControlModel> VsControls);
        public virtual void DrawROI(HDisplayUI display, RectEnum type, bool newROI) { }
        public virtual void DispROI(HDisplayUI display) { }
        public virtual void SetTemplate(HDisplayUI display, RectEnum type, bool newModel) { }

    }

    /// <summary>
    /// 策略集合扩展方法：按完整路径解析输出值
    /// </summary>
    public static class StrategyExtensions
    {
        public static object ResolveFrom(this IList<IParaStrategy> strategies, string fullPath, char separator = '/')
        {
            if (string.IsNullOrWhiteSpace(fullPath)) return null;

            var parts = fullPath.Split(separator);
            if (parts.Length < 2) return null;

            var strategyName = parts[0];
            var nodePath = new string[parts.Length - 1];
            Array.Copy(parts, 1, nodePath, 0, nodePath.Length);

            foreach (var s in strategies)
            {
                if (s.Name == strategyName)
                    return s.ResolveOutput(nodePath);
            }
            return null;
        }

        public static T ResolveFrom<T>(this IList<IParaStrategy> strategies, string fullPath, char separator = '/')
        {
            return (T)ResolveFrom(strategies, fullPath, separator);
        }
    }
}
