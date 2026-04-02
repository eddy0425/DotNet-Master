using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace DotNet.Logging
{
    /// <summary>
    /// 日志记录器 - 线程安全、高性能
    /// </summary>
    public sealed class Logger : IDisposable
    {
        private readonly AsyncLogProcessor _processor;
        private readonly LogLevel _minLevel;
        private readonly string _defaultSource;
        private volatile bool _disposed;

        internal Logger(
            List<ILogSink> sinks,
            LogLevel minLevel,
            string defaultSource,
            int batchSize,
            TimeSpan flushInterval,
            int queueCapacity)
        {
            _minLevel = minLevel;
            _defaultSource = defaultSource ?? "App";
            _processor = new AsyncLogProcessor(sinks, batchSize, flushInterval, queueCapacity);
        }

        /// <summary>
        /// 最小日志级别
        /// </summary>
        public LogLevel MinimumLevel => _minLevel;

        /// <summary>
        /// 检查指定级别是否启用
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsEnabled(LogLevel level) => level >= _minLevel && !_disposed;

        /// <summary>
        /// 写入日志
        /// </summary>
        public void Write(
            LogLevel level,
            string source,
            string message,
            Exception exception = null,
            IDictionary<string, object> properties = null)
        {
            if (!IsEnabled(level))
                return;

            var entry = new LogEntry(
                level,
                source ?? _defaultSource,
                message,
                exception,
                properties);

            _processor.Enqueue(entry);
        }

        #region 便捷方法（指定 Source）

        /// <summary>
        /// 记录 Trace 级别日志
        /// </summary>
        public void Trace(string source, string message)
            => Write(LogLevel.Trace, source, message);

        /// <summary>
        /// 记录 Debug 级别日志
        /// </summary>
        public void Debug(string source, string message)
            => Write(LogLevel.Debug, source, message);

        /// <summary>
        /// 记录 Information 级别日志
        /// </summary>
        public void Info(string source, string message)
            => Write(LogLevel.Information, source, message);

        /// <summary>
        /// 记录 Warning 级别日志
        /// </summary>
        public void Warning(string source, string message)
            => Write(LogLevel.Warning, source, message);

        /// <summary>
        /// 记录 Error 级别日志
        /// </summary>
        public void Error(string source, string message)
            => Write(LogLevel.Error, source, message);

        /// <summary>
        /// 记录带异常的 Error 级别日志
        /// </summary>
        public void Error(string source, Exception exception, string message = null)
            => Write(LogLevel.Error, source, message ?? exception?.Message ?? "Error", exception);

        /// <summary>
        /// 记录 Fatal 级别日志
        /// </summary>
        public void Fatal(string source, string message)
            => Write(LogLevel.Fatal, source, message);

        /// <summary>
        /// 记录带异常的 Fatal 级别日志
        /// </summary>
        public void Fatal(string source, Exception exception, string message = null)
            => Write(LogLevel.Fatal, source, message ?? exception?.Message ?? "Fatal error", exception);

        #endregion

        /// <summary>
        /// 同步刷新所有待处理的日志
        /// </summary>
        public void Flush()
        {
            if (!_disposed)
            {
                _processor.FlushSync();
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _processor.Dispose();
        }
    }
}
