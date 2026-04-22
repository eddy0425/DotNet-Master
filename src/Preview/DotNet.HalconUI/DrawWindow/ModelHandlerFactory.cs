using System;
using System.Collections.Generic;

namespace DotNet.HalconUI
{
    /// <summary>
    /// 绘图处理器工厂
    /// 负责创建和管理绘图处理器实例
    /// </summary>
    public class ModelHandlerFactory
    {
        /// <summary> 设置步骤枚举 </summary>
        public enum SetUpEnum
        {
            None,
            Step1,
            Step2,
            Step3,
            Step4,
            Step5
        }

        /// <summary> 循环移动状态枚举 </summary>
        public enum CycleMoveEnum
        {
            None,
            Start,
            StartMove,
            End,
            EndMove,
            Center,
            CenterMove
        }

        /// <summary> 绘画类型枚举 </summary>
        public enum DrawEnum
        {
            None,
            NewRect,
            EraseRect,
        }

        private readonly Dictionary<DrawEnum, IModelHandler> _handlers;
        private readonly Dictionary<DrawEnum, Func<IModelHandler>> _customHandlers;

        /// <summary>
        /// 单例实例
        /// </summary>
        private static ModelHandlerFactory _instance;
        public static ModelHandlerFactory Instance => _instance ?? (_instance = new ModelHandlerFactory());

        public ModelHandlerFactory()
        {
            _handlers = new Dictionary<DrawEnum, IModelHandler>();
            _customHandlers = new Dictionary<DrawEnum, Func<IModelHandler>>();

            // 注册默认处理器
            RegisterDefaultHandlers();
        }

        /// <summary>
        /// 注册默认的绘图处理器
        /// </summary>
        private void RegisterDefaultHandlers()
        {
            Register(DrawEnum.None, new NoneHandler());
            Register(DrawEnum.NewRect, new NewRectHandler());
            Register(DrawEnum.EraseRect, new EraseRectHandler());
        }

        /// <summary>
        /// 注册绘图处理器（单例模式）
        /// </summary>
        /// <param name="type">绘图类型</param>
        /// <param name="handler">处理器实例</param>
        public void Register(DrawEnum type, IModelHandler handler)
        {
            _handlers[type] = handler;
        }

        /// <summary>
        /// 注册绘图处理器（工厂模式，每次获取时创建新实例）
        /// </summary>
        /// <param name="type">绘图类型</param>
        /// <param name="factory">处理器工厂方法</param>
        public void RegisterFactory(DrawEnum type, Func<IModelHandler> factory)
        {
            _customHandlers[type] = factory;
        }

        /// <summary>
        /// 获取绘图处理器
        /// </summary>
        /// <param name="type">绘图类型</param>
        /// <returns>对应的绘图处理器</returns>
        public IModelHandler GetHandler(DrawEnum type)
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

    }
}
