using System;
using System.Collections.Generic;
using DotNet.Logging.Sinks;

namespace DotNet.Logging
{
    /// <summary>
    /// Logger 构建器 - 流畅的配置 API
    /// </summary>
    public sealed class LoggerBuilder
    {
        private readonly List<ILogSink> _sinks = new List<ILogSink>();
        private LogLevel _minLevel = LogLevel.Information;
        private string _defaultSource = "App";
        private int _batchSize = 100;
        private TimeSpan _flushInterval = TimeSpan.FromMilliseconds(500);
        private int _queueCapacity = 10000;

        /// <summary>
        /// 创建新的构建器
        /// </summary>
        public static LoggerBuilder Create() => new LoggerBuilder();

        /// <summary>
        /// 设置最小日志级别
        /// </summary>
        public LoggerBuilder MinimumLevel(LogLevel level)
        {
            _minLevel = level;
            return this;
        }

        /// <summary>
        /// 设置默认源名称
        /// </summary>
        public LoggerBuilder DefaultSource(string source)
        {
            _defaultSource = source;
            return this;
        }

        /// <summary>
        /// 设置批处理大小
        /// </summary>
        public LoggerBuilder BatchSize(int size)
        {
            _batchSize = size > 0 ? size : 100;
            return this;
        }

        /// <summary>
        /// 设置刷新间隔
        /// </summary>
        public LoggerBuilder FlushInterval(TimeSpan interval)
        {
            _flushInterval = interval;
            return this;
        }

        /// <summary>
        /// 设置队列容量
        /// </summary>
        public LoggerBuilder QueueCapacity(int capacity)
        {
            _queueCapacity = capacity > 0 ? capacity : 10000;
            return this;
        }

        /// <summary>
        /// 添加自定义 Sink
        /// </summary>
        public LoggerBuilder WriteTo(ILogSink sink)
        {
            if (sink != null)
                _sinks.Add(sink);
            return this;
        }

        /// <summary>
        /// 添加文件日志
        /// </summary>
        /// <param name="directory">日志目录</param>
        /// <param name="filePrefix">文件名前缀</param>
        /// <param name="maxFileSize">最大文件大小（字节），默认 10MB</param>
        /// <param name="retentionDays">保留天数，默认 30 天</param>
        public LoggerBuilder WriteToFile(
            string directory,
            string filePrefix = "app",
            long maxFileSize = 10 * 1024 * 1024,
            int retentionDays = 30)
        {
            _sinks.Add(new FileLogSink(directory, filePrefix, maxFileSize, retentionDays));
            return this;
        }

        /// <summary>
        /// 添加控制台日志
        /// </summary>
        /// <param name="useColors">是否使用彩色输出</param>
        public LoggerBuilder WriteToConsole(bool useColors = true)
        {
            _sinks.Add(new ConsoleLogSink(useColors));
            return this;
        }

        /// <summary>
        /// 构建 Logger 实例
        /// </summary>
        public Logger Build()
        {
            if (_sinks.Count == 0)
                throw new InvalidOperationException("至少需要配置一个日志输出目标 (Sink)");

            return new Logger(
                _sinks,
                _minLevel,
                _defaultSource,
                _batchSize,
                _flushInterval,
                _queueCapacity);
        }
    }
}

