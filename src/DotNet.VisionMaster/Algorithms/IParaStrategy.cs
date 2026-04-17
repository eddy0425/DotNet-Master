using System;
using System.Collections.Generic;


namespace DotNet.VisionMaster
{
    /// <summary>
    /// 算法参数策略接口
    /// </summary>
    public interface IParaStrategy
    {
        string Name { get; }
        object ResolveOutput(string[] path);
        T ResolveOutput<T>(string[] path);

        void Init(DisplayUI display);
        void Close(DisplayUI display);
        void GenTreeNode(TreeVisualizer tree);

        bool Fun_action(DisplayUI display, List<IParaStrategy> strategys);
        void DispPara(ParaForm form, Dictionary<string, VsControlModel> VsControls);
        void SavePara(ParaForm form, Dictionary<string, VsControlModel> VsControls);
        void DispROI(DisplayUI display);
    }

    /// <summary>
    /// 策略抽象基类：自动初始化参数实例，子类只需实现 DispPara / SavePara
    /// </summary>
    public abstract class ParaStrategyBase<TPara> : IParaStrategy where TPara : class, new()
    {
        private readonly Dictionary<string, Func<object>> _resolvers = new Dictionary<string, Func<object>>();

        public abstract string Name { get; }
        public TPara inPara { get; set; } = new TPara();
        protected void RegisterOutput(string path, Func<object> resolver)
            => _resolvers[path] = resolver;
        protected void ClearResolvers() => _resolvers.Clear();
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
        public T ResolveOutput<T>(string[] path) => (T)ResolveOutput(path);

        public virtual void Init(DisplayUI display) { }
        public virtual void Close(DisplayUI display) { }
        public abstract void GenTreeNode(TreeVisualizer tree);
  
        public abstract bool Fun_action(DisplayUI display, List<IParaStrategy> strategys);
        public abstract void DispPara(ParaForm form, Dictionary<string, VsControlModel> VsControls);
        public abstract void SavePara(ParaForm form, Dictionary<string, VsControlModel> VsControls);
        public virtual void DispROI(DisplayUI display) { }

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
