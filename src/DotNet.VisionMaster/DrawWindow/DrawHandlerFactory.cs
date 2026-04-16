using System;
using System.Collections.Generic;

namespace DotNet.VisionMaster
{
    
    /// <summary>
    /// 绘图处理器工厂
    /// 负责创建和管理绘图处理器实例
    /// </summary>
    public class DrawHandlerFactory
    {
        private readonly Dictionary<DrawEnum, IDrawHandler> _handlers;
        private readonly Dictionary<DrawEnum, Func<IDrawHandler>> _customHandlers;

        /// <summary>
        /// 单例实例
        /// </summary>
        private static DrawHandlerFactory _instance;
        public static DrawHandlerFactory Instance => _instance ?? (_instance = new DrawHandlerFactory());

        public DrawHandlerFactory()
        {
            _handlers = new Dictionary<DrawEnum, IDrawHandler>();
            _customHandlers = new Dictionary<DrawEnum, Func<IDrawHandler>>();
            
            // 注册默认处理器
            RegisterDefaultHandlers();
        }

        /// <summary>
        /// 注册默认的绘图处理器
        /// </summary>
        private void RegisterDefaultHandlers()
        {
            Register(DrawEnum.None, new NoneHandler());
            Register(DrawEnum.SetModel, new SetModelHandler());
            Register(DrawEnum.DispModel, new DispModelHandler());
            Register(DrawEnum.NewRect, new NewRectHandler());
            Register(DrawEnum.NewAffRect, new NewAffRectHandler());
            Register(DrawEnum.DispRect, new DispRectHandler());
            Register(DrawEnum.NewPolygon, new PolygonNewDrawHandler());
            Register(DrawEnum.EditPolygon, new PolygonEditDrawHandler());
            Register(DrawEnum.Synthethic, new SynthethicDrawHandler());
        }

        /// <summary>
        /// 注册绘图处理器（单例模式）
        /// </summary>
        /// <param name="type">绘图类型</param>
        /// <param name="handler">处理器实例</param>
        public void Register(DrawEnum type, IDrawHandler handler)
        {
            _handlers[type] = handler;
        }

        /// <summary>
        /// 注册绘图处理器（工厂模式，每次获取时创建新实例）
        /// </summary>
        /// <param name="type">绘图类型</param>
        /// <param name="factory">处理器工厂方法</param>
        public void RegisterFactory(DrawEnum type, Func<IDrawHandler> factory)
        {
            _customHandlers[type] = factory;
        }

        /// <summary>
        /// 获取绘图处理器
        /// </summary>
        /// <param name="type">绘图类型</param>
        /// <returns>对应的绘图处理器</returns>
        public IDrawHandler GetHandler(DrawEnum type)
        {
            // 优先使用工厂模式创建的处理器
            if (_customHandlers.TryGetValue(type, out var factory))
            {
                return factory();
            }

            // 使用单例模式的处理器
            if (_handlers.TryGetValue(type, out var handler))
            {
                return handler;
            }

            // 默认返回空处理器
            return new NoneHandler();
        }

        /// <summary>
        /// 检查是否已注册指定类型的处理器
        /// </summary>
        /// <param name="type">绘图类型</param>
        /// <returns>是否已注册</returns>
        public bool HasHandler(DrawEnum type)
        {
            return _handlers.ContainsKey(type) || _customHandlers.ContainsKey(type);
        }

        /// <summary>
        /// 移除绘图处理器
        /// </summary>
        /// <param name="type">绘图类型</param>
        public void Unregister(DrawEnum type)
        {
            _handlers.Remove(type);
            _customHandlers.Remove(type);
        }

        /// <summary>
        /// 清除所有处理器并重新注册默认处理器
        /// </summary>
        public void Reset()
        {
            _handlers.Clear();
            _customHandlers.Clear();
            RegisterDefaultHandlers();
        }
    }
}
