using DotNet.Drawing;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotNet.Vision.Abstractions
{
    #region 拆分后的职责接口

    /// <summary>
    /// 算法执行：策略的核心职责，不涉及任何界面概念。
    /// </summary>
    public interface IAlgoStrategy
    {
        AlgoEnum Algorithm { get; }
        string Name { get; set; }
        int RunIndex { get; set; }

        /// <summary> 在流程中执行：可从上游 <paramref name="strategys"/> 取输入 </summary>
        bool Fun_action(IHDisplay display, List<IParaStrategy> strategys);

        /// <summary> 单张图快速验证：没有上游策略 </summary>
        bool Fun_action(HObject ho_Image, IHDisplay display);
    }

    /// <summary>
    /// 输出解析：把策略的计算结果按路径暴露给下游。
    /// </summary>
    public interface IOutputProvider
    {
        object ResolveOutput(string[] path);

        /// <summary>解析输出; 路径不存在或类型不匹配时抛 <see cref="AlgoOutputNotFoundException"/>.</summary>
        T ResolveOutput<T>(string[] path);

        /// <summary>解析输出的安全版本: 失败返回 false 并把 value 置为 default, 不抛异常.</summary>
        bool TryResolveOutput<T>(string[] path, out T value);
    }

    /// <summary>
    /// ROI 编辑：需要在画面上交互式绘制或显示区域的策略才实现。
    /// </summary>
    public interface IRoiEditable
    {
        /// <summary>
        /// 交互式绘制 / 修改 ROI。要等用户在画面上右键确认，因此是异步的：
        /// 调用方必须在 UI 线程 await，不要 .Wait()（会死锁）。
        /// </summary>
        Task DrawROIAsync(IRoiHost host, RectEnum type, bool newROI);

        /// <summary>把已有 ROI 画到画面上，无交互，保持同步。</summary>
        void DispROI(IRoiHost host);
    }

    /// <summary>
    /// 模板编辑：支持新建或修改匹配模板的策略才实现。
    /// </summary>
    public interface ITemplateEditable
    {
        /// <summary>
        /// 交互式框选模板区域并创建模板。内含 ROI 绘制交互，故为异步；调用方须在 UI 线程 await。
        /// </summary>
        Task SetTemplateAsync(IRoiHost host, RectEnum type, bool newModel);
    }

    /// <summary>
    /// 参数绑定：参数面板的显示与回存。
    /// </summary>
    public interface IParaBinding
    {
        void DispPara(IParaUiHost ui);
        void SavePara(IParaUiHost ui);
    }

    /// <summary>
    /// 输出变量树节点的声明。
    /// </summary>
    public interface ITreeNodeProvider
    {
        void GenTreeNode(ITreeVisualizer tree);
    }

    #endregion

    /// <summary>
    /// 算法参数策略接口。
    /// </summary>
    /// <remarks>
    /// 原本是 15 成员的巨型接口，同时承担参数解析、算法执行、ROI 绘制、树节点生成、
    /// 控件双向同步和模板设置，任何策略都被迫拥有全部能力。现将可选能力拆成
    /// <see cref="IRoiEditable"/>、<see cref="ITemplateEditable"/>、<see cref="IParaBinding"/>、
    /// <see cref="ITreeNodeProvider"/>；本接口只保留所有策略共有的执行、输出和生命周期能力。
    /// <para>
    /// 宿主应按能力接口做类型判断，例如
    /// <c>if (s is IRoiEditable roi) await roi.DrawROIAsync(...)</c>。
    /// </para>
    /// </remarks>
    public interface IParaStrategy : IAlgoStrategy, IOutputProvider
    {
        /// <summary> 工具页打开：申请运行期资源 </summary>
        void Init(IRoiHost host);

        /// <summary> 工具页关闭：只释放运行期临时对象，不销毁配置态 </summary>
        void Close(IRoiHost host);
    }

    /// <summary>
    /// 策略抽象基类：自动初始化参数实例，子类只需实现 DispPara / SavePara
    /// </summary>
    public abstract class ParaStrategyBase<TPara> : IParaStrategy, IParaBinding, ITreeNodeProvider where TPara : class, new()
    {
        private readonly Dictionary<string, Func<object>> _resolvers = new Dictionary<string, Func<object>>();

        public abstract AlgoEnum Algorithm { get; }
        public abstract string Name { get; set; }
        public abstract int RunIndex { get; set; }
        public TPara inPara { get; set; } = new TPara();
        protected void RegisterOutput(string path, Func<object> resolver) => _resolvers[path] = resolver;
        protected void ClearResolvers() => _resolvers.Clear();
        /// <summary>
        /// 解析输出并强转. 路径不存在时 <see cref="ResolveOutput(string[])"/> 返回 null,
        /// 若 T 是值类型 (CvCoord / Point2d ...) 直接强转会抛 NullReferenceException,
        /// 报错信息和真实原因(路径拼错)毫无关系, 因此这里统一换成携带路径的专用异常.
        /// </summary>
        public T ResolveOutput<T>(string[] path)
        {
            var value = ResolveOutput(path);
            if (value == null)
                throw new AlgoOutputNotFoundException(Name, path, typeof(T));
            if (!(value is T))
                throw new AlgoOutputNotFoundException(Name, path, typeof(T), value.GetType());
            return (T)value;
        }

        public bool TryResolveOutput<T>(string[] path, out T value)
        {
            var raw = path == null ? null : ResolveOutput(path);
            if (raw is T typed)
            {
                value = typed;
                return true;
            }
            value = default(T);
            return false;
        }

        public object ResolveOutput(string[] path)
        {
            if (path == null) return null;
            for (int depth = path.Length; depth >= 1; depth--)
            {
                var key = string.Join("/", path, 0, depth);
                if (_resolvers.TryGetValue(key, out var resolver))
                    return resolver();
            }
            return null;
        }

        public virtual void Init(IRoiHost host) { }
        public virtual void Close(IRoiHost host) { }
        public abstract void GenTreeNode(ITreeVisualizer tree);

        public virtual bool Fun_action(HObject ho_Image, IHDisplay display) { return false; }
        public abstract bool Fun_action(IHDisplay display, List<IParaStrategy> strategys);
        public abstract void DispPara(IParaUiHost ui);
        public abstract void SavePara(IParaUiHost ui);

    }

    /// <summary>
    /// 策略集合扩展方法：按完整路径解析输出值
    /// </summary>
    public static class StrategyExtensions
    {
        /// <summary>
        /// 空集合单例. 供 <c>Fun_action(HObject, IHDisplay)</c> 这类没有上游策略的调用路径使用,
        /// 替代原来的 null —— ResolveFrom 里的 foreach 遇到 null 会直接 NRE.
        /// </summary>
        public static readonly IReadOnlyList<IParaStrategy> Empty = new IParaStrategy[0];

        /// <summary>空集合的 List 视图. 现有重载签名是 List&lt;T&gt;, 暂时需要一个可传入的实例.</summary>
        public static List<IParaStrategy> EmptyList()
        {
            return new List<IParaStrategy>(0);
        }

        public static object ResolveFrom(this IList<IParaStrategy> strategies, string fullPath, char separator = '/')
        {
            if (strategies == null) return null;
            if (string.IsNullOrWhiteSpace(fullPath)) return null;

            var parts = fullPath.Split(separator);
            if (parts.Length < 2) return null;

            var strategyName = parts[0];
            var nodePath = new string[parts.Length - 1];
            Array.Copy(parts, 1, nodePath, 0, nodePath.Length);

            foreach (var s in strategies)
            {
                if (s != null && s.Name == strategyName)
                    return s.ResolveOutput(nodePath);
            }
            return null;
        }

        /// <summary>
        /// 解析并强转. 失败时抛 <see cref="AlgoOutputNotFoundException"/> 而不是 NRE / InvalidCastException,
        /// 异常信息里带上完整路径, 便于直接定位到拼错的参数名.
        /// </summary>
        public static T ResolveFrom<T>(this IList<IParaStrategy> strategies, string fullPath, char separator = '/')
        {
            var value = ResolveFrom(strategies, fullPath, separator);
            if (value == null)
                throw new AlgoOutputNotFoundException(fullPath, typeof(T));
            if (!(value is T))
                throw new AlgoOutputNotFoundException(fullPath, typeof(T), value.GetType());
            return (T)value;
        }

        /// <summary>解析并强转的安全版本: 失败返回 false, 不抛异常.</summary>
        public static bool TryResolveFrom<T>(this IList<IParaStrategy> strategies, string fullPath, out T value, char separator = '/')
        {
            var raw = ResolveFrom(strategies, fullPath, separator);
            if (raw is T typed)
            {
                value = typed;
                return true;
            }
            value = default(T);
            return false;
        }
    }


    /// <summary>
    /// 策略输出解析失败. 携带路径与期望类型, 避免退化成 NullReferenceException / InvalidCastException.
    /// </summary>
    public class AlgoOutputNotFoundException : Exception
    {
        public string Path { get; }
        public Type ExpectedType { get; }
        public Type ActualType { get; }

        public AlgoOutputNotFoundException(string fullPath, Type expectedType)
            : base(string.Format("未能解析策略输出 '{0}' (期望类型 {1}): 路径不存在或上游策略尚未产出结果.",
                                 fullPath, expectedType == null ? "?" : expectedType.Name))
        {
            Path = fullPath;
            ExpectedType = expectedType;
        }

        public AlgoOutputNotFoundException(string fullPath, Type expectedType, Type actualType)
            : base(string.Format("策略输出 '{0}' 的类型不匹配: 期望 {1}, 实际 {2}.",
                                 fullPath, expectedType == null ? "?" : expectedType.Name,
                                 actualType == null ? "?" : actualType.Name))
        {
            Path = fullPath;
            ExpectedType = expectedType;
            ActualType = actualType;
        }

        public AlgoOutputNotFoundException(string strategyName, string[] path, Type expectedType)
            : this(Join(strategyName, path), expectedType) { }

        public AlgoOutputNotFoundException(string strategyName, string[] path, Type expectedType, Type actualType)
            : this(Join(strategyName, path), expectedType, actualType) { }

        private static string Join(string strategyName, string[] path)
        {
            var tail = path == null ? string.Empty : string.Join("/", path);
            return string.IsNullOrEmpty(strategyName) ? tail : strategyName + "/" + tail;
        }
    }
}
