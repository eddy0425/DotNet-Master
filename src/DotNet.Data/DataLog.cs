using System;


namespace DotNet.Data
{
    /// <summary> Data日志级别枚举 </summary>
    public enum DataLogLevel
    {
        Debug,
        Information,
        Warning,
        Error,
        Exception
    }

    /// <summary> 日志适配器 - 轻量级实现 </summary>
    public static class DataLog
    {
        /// <summary> 日志事件 </summary>
        public static event DataLogHandler Logged;

        /// <summary>
        /// 记录信息
        /// </summary>
        /// <param name="tag">标签</param>
        /// <param name="message">消息</param>
        /// <param name="alarmName">报警码</param>
        public static void Info(string tag, string message, string alarmName = "")
        {
            var handler = Logged;
            if (handler != null)
                handler?.Invoke(new DataLogArgs(DataLogLevel.Information, tag, message, alarmName));
            else
                System.Diagnostics.Trace.WriteLine($"[{tag}] {message}"); // 兜底，避免静默丢失
        }

        /// <summary>
        /// 记录调试信息
        /// </summary>
        /// <param name="tag">标签</param>
        /// <param name="message">消息</param>
        /// <param name="alarmName">报警码</param>
        public static void Debug(string tag, string message, string alarmName = "")
        {
            var handler = Logged;
            if (handler != null)
                handler?.Invoke(new DataLogArgs(DataLogLevel.Debug, tag, message, alarmName));
            else
                System.Diagnostics.Trace.WriteLine($"[{tag}] {message}"); // 兜底，避免静默丢失
        }

        /// <summary>
        /// 记录警告
        /// </summary>
        /// <param name="tag">标签</param>
        /// <param name="message">消息</param>
        /// <param name="alarmName">报警码</param>
        public static void Warning(string tag, string message, string alarmName = "")
        {
            var handler = Logged;
            if (handler != null)
                handler?.Invoke(new DataLogArgs(DataLogLevel.Warning, tag, message, alarmName));
            else
                System.Diagnostics.Trace.WriteLine($"[{tag}] {message}"); // 兜底，避免静默丢失
        }

        /// <summary>
        /// 记录错误
        /// </summary>
        /// <param name="tag">标签</param>
        /// <param name="message">消息</param>
        /// <param name="alarmName">报警码</param>
        public static void Error(string tag, string message, string alarmName = "")
        {
            var handler = Logged;
            if (handler != null)
                handler?.Invoke(new DataLogArgs(DataLogLevel.Error, tag, message, alarmName));
            else
                System.Diagnostics.Trace.WriteLine($"[{tag}] {message}"); // 兜底，避免静默丢失
        }

        /// <summary>
        /// 记录异常
        /// </summary>
        /// <param name="tag">标签</param>
        /// <param name="ex">异常</param>
        /// <param name="alarmName">报警码</param>
        /// <param name="message">附加消息</param>
        public static void Exception(string tag, Exception ex, string message = null, string alarmName = "")
        {
            var handler = Logged;
            if (handler != null)
                handler.Invoke(new DataLogArgs(DataLogLevel.Exception, tag, ex, message, alarmName));
            else
                System.Diagnostics.Trace.WriteLine($"[{tag}] {message} {ex}"); // 兜底，避免静默丢失
        }
    }

    /// <summary> 统一日志事件委托 </summary>
    public delegate void DataLogHandler(DataLogArgs args);

    /// <summary> 统一日志事件参数 </summary>
    public class DataLogArgs : EventArgs
    {
        /// <summary> 日志级别 </summary>
        public DataLogLevel Level { get; private set; }

        /// <summary> 标签 </summary>
        public string Tag { get; private set; }

        /// <summary> 消息 </summary>
        public string Message { get; private set; }

        /// <summary> 报警码 </summary>
        public string AlarmName { get; private set; }

        /// <summary> 异常对象，普通日志为null </summary>
        public Exception Exception { get; private set; }

        /// <summary> 是否为异常日志 </summary>
        public bool IsException => Exception != null;

        /// <summary> 构造普通日志 </summary>
        public DataLogArgs(DataLogLevel level, string tag, string message, string alarmName = "")
        {
            Level = level;
            Tag = tag;
            Message = message;
            AlarmName = alarmName;
        }

        /// <summary> 构造异常日志 </summary>
        public DataLogArgs(DataLogLevel level, string tag, Exception exception, string message = null, string alarmName = "")
        {
            Level = DataLogLevel.Exception;
            Tag = tag;
            Exception = exception;
            Message = message;
            AlarmName = alarmName;
        }
    }
}
