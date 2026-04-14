using System;
using System.Collections.Generic;

namespace DotNet.VisionMaster
{
    /// <summary>
    /// 绘图类型枚举
    /// </summary>
    public enum WinDrawType
    {
        None,
        SetModel,
        NewRect,
        EditRect,
        DispRect,//EditRectHandler
        NewPolygon,
        EditPolygon,
        Synthethic,
        ShapeModel
    }

    public enum DrawEnum
    {
        None,
        NewRectangle,
        EditRectangle,
        DispRectangle,
        NewRectangle2,
        EditRectangle2,
        DispRectangle2,
        NewCircle,
        EditCircle,
        DispCircle,
        NewEllipse,
        EditEllipse,
        DispEllipse,
        NewPolygon,
        EditPolygon,
        DispPolygon,
        NewRing,
        EditRing,
        DispRing
    }


    /// <summary>
    /// 绘图处理器工厂
    /// 负责创建和管理绘图处理器实例
    /// </summary>
    public class DrawHandlerFactory
    {
        private readonly Dictionary<WinDrawType, IDrawHandler> _handlers;
        private readonly Dictionary<WinDrawType, Func<IDrawHandler>> _customHandlers;

        /// <summary>
        /// 单例实例
        /// </summary>
        private static DrawHandlerFactory _instance;
        public static DrawHandlerFactory Instance => _instance ?? (_instance = new DrawHandlerFactory());

        public DrawHandlerFactory()
        {
            _handlers = new Dictionary<WinDrawType, IDrawHandler>();
            _customHandlers = new Dictionary<WinDrawType, Func<IDrawHandler>>();
            
            // 注册默认处理器
            RegisterDefaultHandlers();
        }

        /// <summary>
        /// 注册默认的绘图处理器
        /// </summary>
        private void RegisterDefaultHandlers()
        {
            Register(WinDrawType.None, new NoneHandler());
            Register(WinDrawType.SetModel, new SetModelDrawHandler());
            Register(WinDrawType.NewRect, new RectNewHandler());
            Register(WinDrawType.DispRect, new RectDispHandler());
            Register(WinDrawType.NewPolygon, new PolygonNewDrawHandler());
            Register(WinDrawType.EditPolygon, new PolygonEditDrawHandler());
            Register(WinDrawType.Synthethic, new SynthethicDrawHandler());
        }

        /// <summary>
        /// 注册绘图处理器（单例模式）
        /// </summary>
        /// <param name="type">绘图类型</param>
        /// <param name="handler">处理器实例</param>
        public void Register(WinDrawType type, IDrawHandler handler)
        {
            _handlers[type] = handler;
        }

        /// <summary>
        /// 注册绘图处理器（工厂模式，每次获取时创建新实例）
        /// </summary>
        /// <param name="type">绘图类型</param>
        /// <param name="factory">处理器工厂方法</param>
        public void RegisterFactory(WinDrawType type, Func<IDrawHandler> factory)
        {
            _customHandlers[type] = factory;
        }

        /// <summary>
        /// 获取绘图处理器
        /// </summary>
        /// <param name="type">绘图类型</param>
        /// <returns>对应的绘图处理器</returns>
        public IDrawHandler GetHandler(WinDrawType type)
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
        public bool HasHandler(WinDrawType type)
        {
            return _handlers.ContainsKey(type) || _customHandlers.ContainsKey(type);
        }

        /// <summary>
        /// 移除绘图处理器
        /// </summary>
        /// <param name="type">绘图类型</param>
        public void Unregister(WinDrawType type)
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
