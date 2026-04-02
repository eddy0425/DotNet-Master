using System;

namespace DotNet.Logging
{
    /// <summary>
    /// 静态日志门面 - 提供全局日志访问点
    /// </summary>
    public static class Log
    {
        private static Logger _logger;
        private static readonly object _lock = new object();

        /// <summary>
        /// 获取或设置全局 Logger 实例
        /// </summary>
        public static Logger Logger
        {
            get => _logger;
            set
            {
                lock (_lock)
                {
                    _logger?.Dispose();
                    _logger = value;
                }
            }
        }

        /// <summary>
        /// 初始化全局日志
        /// </summary>
        public static void Initialize(Logger logger)
        {
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 使用构建器配置初始化
        /// </summary>
        public static void Initialize(Action<LoggerBuilder> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var builder = LoggerBuilder.Create();
            configure(builder);
            Logger = builder.Build();
        }

        /// <summary>
        /// 关闭日志系统
        /// </summary>
        public static void Shutdown()
        {
            lock (_lock)
            {
                _logger?.Dispose();
                _logger = null;
            }
        }

        /// <summary>
        /// 刷新待处理的日志
        /// </summary>
        public static void Flush() => _logger?.Flush();

        /// <summary>
        /// 检查指定级别是否启用
        /// </summary>
        public static bool IsEnabled(LogLevel level) => _logger?.IsEnabled(level) ?? false;

        #region 便捷方法（指定 Source）

        /// <summary>
        /// 记录 Trace 级别日志
        /// </summary>
        public static void Trace(string source, string message)
            => _logger?.Write(LogLevel.Trace, source, message);

        /// <summary>
        /// 记录 Debug 级别日志
        /// </summary>
        public static void Debug(string source, string message)
            => _logger?.Write(LogLevel.Debug, source, message);

        /// <summary>
        /// 记录 Information 级别日志
        /// </summary>
        public static void Info(string source, string message)
            => _logger?.Write(LogLevel.Information, source, message);

        /// <summary>
        /// 记录 Warning 级别日志
        /// </summary>
        public static void Warning(string source, string message)
            => _logger?.Write(LogLevel.Warning, source, message);

        /// <summary>
        /// 记录 Error 级别日志
        /// </summary>
        public static void Error(string source, string message)
            => _logger?.Write(LogLevel.Error, source, message);

        /// <summary>
        /// 记录带异常的 Error 级别日志
        /// </summary>
        public static void Error(string source, Exception exception, string message = null)
            => _logger?.Write(LogLevel.Error, source, message ?? exception?.Message, exception);

        /// <summary>
        /// 记录 Fatal 级别日志
        /// </summary>
        public static void Fatal(string source, string message)
            => _logger?.Write(LogLevel.Fatal, source, message);

        /// <summary>
        /// 记录带异常的 Fatal 级别日志
        /// </summary>
        public static void Fatal(string source, Exception exception, string message = null)
            => _logger?.Write(LogLevel.Fatal, source, message ?? exception?.Message, exception);

        #endregion
    }
}
