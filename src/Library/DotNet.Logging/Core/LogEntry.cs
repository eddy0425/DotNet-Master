using System;
using System.Collections.Generic;
using System.Text;

namespace DotNet.Logging
{
    /// <summary>
    /// 日志条目，不可变记录类型
    /// </summary>
    public sealed class LogEntry
    {
        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// 日志级别
        /// </summary>
        public LogLevel Level { get; }

        /// <summary>
        /// 日志源/类别
        /// </summary>
        public string Source { get; }

        /// <summary>
        /// 日志消息
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// 异常信息（可选）
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// 附加属性（用于结构化日志）
        /// </summary>
        public IReadOnlyDictionary<string, object> Properties { get; }

        public LogEntry(
            LogLevel level,
            string source,
            string message,
            Exception exception = null,
            IDictionary<string, object> properties = null)
        {
            Timestamp = DateTime.Now;
            Level = level;
            Source = source ?? string.Empty;
            Message = message ?? string.Empty;
            Exception = exception;
            Properties = properties != null
                ? new Dictionary<string, object>(properties)
                : null;
        }

        /// <summary>
        /// 获取级别的显示标签
        /// </summary>
        public string LevelTag
        {
            get
            {
                switch (Level)
                {
                    case LogLevel.Trace: return "TRC";
                    case LogLevel.Debug: return "DBG";
                    case LogLevel.Information: return "INF";
                    case LogLevel.Warning: return "WRN";
                    case LogLevel.Error: return "ERR";
                    case LogLevel.Fatal: return "FTL";
                    default: return "???";
                }
            }
        }

        /// <summary>
        /// 格式化为标准字符串
        /// </summary>
        public string Format()
        {
            var sb = new StringBuilder();
            sb.Append('[');
            sb.Append(Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            sb.Append("] [");
            sb.Append(LevelTag);
            sb.Append("] [");
            sb.Append(Source);
            sb.Append("] ");
            sb.Append(Message);

            if (Exception != null)
            {
                sb.AppendLine();
                FormatException(sb, Exception, 0);
            }

            return sb.ToString();
        }

        private static void FormatException(StringBuilder sb, Exception ex, int depth)
        {
            var indent = new string(' ', depth * 2);
            sb.Append(indent);
            sb.Append("  → ");
            sb.Append(ex.GetType().FullName);
            sb.Append(": ");
            sb.AppendLine(ex.Message);

            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                foreach (var line in ex.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                {
                    sb.Append(indent);
                    sb.Append("    ");
                    sb.AppendLine(line.Trim());
                }
            }

            if (ex.InnerException != null)
            {
                sb.Append(indent);
                sb.AppendLine("  ↳ Inner Exception:");
                FormatException(sb, ex.InnerException, depth + 1);
            }
        }
    }
}
