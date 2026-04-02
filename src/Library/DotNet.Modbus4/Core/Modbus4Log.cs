using System;


namespace DotNet.Modbus4
{
    /// <summary>
    /// Modbus4日志级别枚举
    /// </summary>
    public enum Modbus4LogLevel
    {
        Debug,
        Information,
        Warning,
        Error,
        Exception
    }

    /// <summary>
    /// 日志适配器 - 轻量级实现
    /// </summary>
    public static class Modbus4Log
    {
        /// <summary>
        /// 日志事件
        /// </summary>
        public static event Modbus4LogHandler Logged;

        /// <summary>
        /// 记录信息
        /// </summary>
        /// <param name="tag">标签</param>
        /// <param name="message">消息</param>
        public static void Info(string tag, string message)
        {
            Logged?.Invoke(new Modbus4LogArgs(Modbus4LogLevel.Information, tag, message));
        }

        /// <summary>
        /// 记录调试信息
        /// </summary>
        /// <param name="tag">标签</param>
        /// <param name="message">消息</param>
        public static void Debug(string tag, string message)
        {
            Logged?.Invoke(new Modbus4LogArgs(Modbus4LogLevel.Debug, tag, message));
        }

        /// <summary>
        /// 记录警告
        /// </summary>
        /// <param name="tag">标签</param>
        /// <param name="message">消息</param>
        public static void Warning(string tag, string message)
        {
            Logged?.Invoke(new Modbus4LogArgs(Modbus4LogLevel.Warning, tag, message));
        }

        /// <summary>
        /// 记录错误
        /// </summary>
        /// <param name="tag">标签</param>
        /// <param name="message">消息</param>
        public static void Error(string tag, string message)
        {
            Logged?.Invoke(new Modbus4LogArgs(Modbus4LogLevel.Error, tag, message));
        }

        /// <summary>
        /// 记录异常
        /// </summary>
        /// <param name="tag">标签</param>
        /// <param name="ex">异常</param>
        /// <param name="message">附加消息</param>
        public static void Exception(string tag, Exception ex, string message = null)
        {
            Logged?.Invoke(new Modbus4LogArgs(Modbus4LogLevel.Exception, tag, ex, message));
        }
    }

    /// <summary>
    /// 统一日志事件委托
    /// </summary>
    public delegate void Modbus4LogHandler(Modbus4LogArgs args);

    /// <summary>
    /// 统一日志事件参数
    /// </summary>
    public class Modbus4LogArgs : EventArgs
    {
        /// <summary>
        /// 日志级别
        /// </summary>
        public Modbus4LogLevel Level { get; private set; }

        /// <summary>
        /// 标签
        /// </summary>
        public string Tag { get; private set; }

        /// <summary>
        /// 消息
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// 异常对象，普通日志为null
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// 是否为异常日志
        /// </summary>
        public bool IsException => Exception != null;

        /// <summary>
        /// 构造普通日志
        /// </summary>
        public Modbus4LogArgs(Modbus4LogLevel level, string tag, string message)
        {
            Level = level;
            Tag = tag;
            Message = message;
        }

        /// <summary>
        /// 构造异常日志
        /// </summary>
        public Modbus4LogArgs(Modbus4LogLevel level, string tag, Exception exception, string message = null)
        {
            Level = Modbus4LogLevel.Exception;
            Tag = tag;
            Exception = exception;
            Message = message;
        }
    }
}
