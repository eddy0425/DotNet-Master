using System;
using System.Collections.Generic;

namespace DotNet.HalconUI
{
    /// <summary>
    /// 绘图处理器工厂.
    /// </summary>
    /// <remarks>
    /// 设计要点:
    /// <list type="bullet">
    /// <item>统一采用 <c>Func&lt;IDrawHandler&gt;</c> 工厂方法注册, 每次 <see cref="Create"/> 返回全新实例,
    /// 避免多个 <see cref="DisplayUI"/> 共享同一 handler 时残留状态相互污染 (例如 TopLeft/BottomRight).</item>
    /// <item>不再持有进程级单例: 每个 <see cref="DisplayUI"/> 自行 new 一个工厂, 与控件生命周期对齐.</item>
    /// </list>
    /// </remarks>
    public class DrawHandlerFactory
    {
        private readonly Dictionary<DrawEnum, Func<IDrawHandler>> _factories =
            new Dictionary<DrawEnum, Func<IDrawHandler>>();

        public DrawHandlerFactory()
        {
            RegisterDefaults();
        }

        private void RegisterDefaults()
        {
            Register(DrawEnum.None,        () => new NoneHandler());
            Register(DrawEnum.DispRect,    () => new DispRectHandler());
            Register(DrawEnum.DispModel,   () => new DispModelHandler());
            Register(DrawEnum.Synthethic,  () => new SynthethicDrawHandler());
            // EditRect 暂未实现, 留空; Create 时会回落到 NoneHandler.
        }

        /// <summary>
        /// 注册或替换某绘图类型的工厂方法
        /// </summary>
        public void Register(DrawEnum type, Func<IDrawHandler> factory)
        {
            if (factory == null) throw new ArgumentNullException("factory");
            _factories[type] = factory;
        }

        /// <summary>
        /// 兼容旧 API: 以"实例"形式注册 (内部包装成工厂方法, 该实例会被复用).
        /// </summary>
        /// <remarks>
        /// 不推荐: 多个 DisplayUI 共享同一实例会导致状态污染.
        /// 优先使用 <see cref="Register(DrawEnum, Func{IDrawHandler})"/> 重载.
        /// </remarks>
        public void Register(DrawEnum type, IDrawHandler handler)
        {
            if (handler == null) throw new ArgumentNullException("handler");
            _factories[type] = () => handler;
        }

        /// <summary>
        /// 创建对应类型的处理器实例; 未注册时返回 <see cref="NoneMouse"/>.
        /// </summary>
        public IDrawHandler Create(DrawEnum type)
        {
            Func<IDrawHandler> factory;
            if (_factories.TryGetValue(type, out factory)) return factory();
            return new NoneHandler();
        }

        /// <summary>
        /// 兼容旧 API: 与 <see cref="Create"/> 等价.
        /// </summary>
        public IDrawHandler GetHandler(DrawEnum type)
        {
            return Create(type);
        }

        public bool HasHandler(DrawEnum type)
        {
            return _factories.ContainsKey(type);
        }

        public void Unregister(DrawEnum type)
        {
            _factories.Remove(type);
        }

        /// <summary>
        /// 清空并重新注册默认处理器.
        /// </summary>
        public void Reset()
        {
            _factories.Clear();
            RegisterDefaults();
        }
    }
}
