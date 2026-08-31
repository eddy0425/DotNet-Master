using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace DotNet.Logging
{
    /// <summary>
    /// 日志工具静态类，提供日志记录功能
    /// </summary>
    public static class Log
    {
        /// <summary>
        /// 日志接收器集合
        /// </summary>
        public static class Sinks
        {
            private static readonly List<ILogSink> sinks = new List<ILogSink>();
            private static readonly object syncLock = new object();

            /// <summary>
            /// 获取日志接收器数量
            /// </summary>
            public static int Count
            {
                get
                {
                    lock (syncLock)
                    {
                        return sinks.Count;
                    }
                }
            }

            /// <summary>
            /// 添加日志接收器
            /// </summary>
            /// <param name="sink">要添加的日志接收器</param>
            public static void Add(ILogSink sink)
            {
                if (sink == null)
                {
                    return;
                }

                int count;
                lock (syncLock)
                {
                    if (sinks.Contains(sink))
                    {
                        WriteIfEnabled(LogCategory.Debug, "Log.Sinks", "Sink {0} hashcode {1} already added, skipping", sink.GetType().ToString(), sink.GetHashCode());
                        return;
                    }
                    sinks.Add(sink);
                    count = sinks.Count;
                }
                WriteIfEnabled(LogCategory.Debug, "Log.Sinks", "Added sink {0} hashcode {1}, count = {2}", sink.GetType().ToString(), sink.GetHashCode(), count);
            }

            /// <summary>
            /// 移除日志接收器
            /// </summary>
            /// <param name="sink">要移除的日志接收器</param>
            public static void Remove(ILogSink sink)
            {
                if (sink != null)
                {
                    int count;
                    lock (syncLock)
                    {
                        sinks.Remove(sink);
                        count = sinks.Count;
                    }
                    WriteIfEnabled(LogCategory.Debug, "Log.Sinks", "Removed sink {0}, count = {1}", sink.GetType().ToString(), count);
                }
            }

            /// <summary>
            /// 清空所有日志接收器
            /// </summary>
            public static void Clear()
            {
                lock (syncLock)
                {
                    sinks.Clear();
                }
            }

            /// <summary>
            /// 检查是否包含指定的日志接收器
            /// </summary>
            /// <param name="sink">要检查的日志接收器</param>
            /// <returns>是否包含</returns>
            public static bool Contains(ILogSink sink)
            {
                lock (syncLock)
                {
                    return sinks.Contains(sink);
                }
            }

            /// <summary>
            /// 获取日志接收器数组副本
            /// </summary>
            /// <returns>日志接收器数组</returns>
            public static ILogSink[] ToArray()
            {
                lock (syncLock)
                {
                    return sinks.ToArray();
                }
            }

            /// <summary>
            /// 内部获取日志接收器列表引用
            /// </summary>
            internal static List<ILogSink> GetSinks()
            {
                return sinks;
            }
        }

        private static readonly object syncLock = new object();
        private static readonly double microsecondsPerTick = 1000000.0 / (double)Stopwatch.Frequency;
        private static EventHandler<Exception> sinkException;

        /// <summary>
        /// 当前启用的日志类别
        /// </summary>
        public static LogCategory EnabledCategories { get; set; }

        /// <summary>
        /// 源进程名称
        /// </summary>
        public static string SourceProcess { get; private set; }

        /// <summary>
        /// 日志接收器异常事件
        /// </summary>
        public static event EventHandler<Exception> LogSinkException
        {
            add
            {
                AddDelegate(ref sinkException, value);
            }
            remove
            {
                RemoveDelegate(ref sinkException, value);
            }
        }

        /// <summary>
        /// 静态构造函数
        /// </summary>
        static Log()
        {
            SourceProcess = AppDomain.CurrentDomain.FriendlyName + ".";
            EnabledCategories = LogCategory.All;
        }

        /// <summary>
        /// 检查指定的日志类别是否启用
        /// </summary>
        /// <param name="logCategory">要检查的日志类别</param>
        /// <returns>是否启用</returns>
        public static bool IsCategoryEnabled(LogCategory logCategory)
        {
            return (logCategory & EnabledCategories) != 0;
        }

        /// <summary>
        /// 如果指定的日志类别启用，则写入日志
        /// </summary>
        /// <param name="logCategory">日志类别</param>
        /// <param name="source">日志源</param>
        /// <param name="message">日志消息</param>
        public static void WriteIfEnabled(LogCategory logCategory, string source, string message)
        {
            if (IsCategoryEnabled(logCategory))
            {
                Write(logCategory, source, message);
            }
        }

        /// <summary>
        /// 如果指定的日志类别启用，则格式化并写入日志
        /// </summary>
        /// <param name="logCategory">日志类别</param>
        /// <param name="source">日志源</param>
        /// <param name="format">格式字符串</param>
        /// <param name="args">格式化参数</param>
        public static void WriteIfEnabled(LogCategory logCategory, string source, string format, params object[] args)
        {
            if (IsCategoryEnabled(logCategory))
            {
                string text = FormatString(source, format, args);
                if (text != null)
                {
                    Write(logCategory, source, text);
                }
            }
        }

        /// <summary>
        /// 检查是否存在日志接收器
        /// </summary>
        /// <returns>是否存在日志接收器</returns>
        private static bool HasSinks()
        {
            lock (syncLock)
            {
                return Sinks.Count > 0;
            }
        }

        /// <summary>
        /// 写入日志
        /// </summary>
        /// <param name="category">日志类别</param>
        /// <param name="source">日志源</param>
        /// <param name="message">日志消息</param>
        public static void Write(LogCategory category, string source, string message)
        {
            string type = category.ToString();
            Write(type, category, source, message);
        }

        /// <summary>
        /// 使用指定的线程ID写入日志
        /// </summary>
        /// <param name="threadId">线程ID</param>
        /// <param name="category">日志类别</param>
        /// <param name="source">日志源</param>
        /// <param name="message">日志消息</param>
        public static void Write(int threadId, LogCategory category, string source, string message)
        {
            if (HasSinks())
            {
                string type = category.ToString();
                LogMessage logMsg = new LogMessage
                {
                    Type = type,
                    Category = category,
                    SourceProcess = SourceProcess,
                    Source = source,
                    ThreadId = "0x" + threadId.ToString("x4"),
                    Message = message
                };
                WriteLogMsg(logMsg);
            }
        }

        /// <summary>
        /// 使用指定的类型写入日志
        /// </summary>
        /// <param name="type">日志类型</param>
        /// <param name="category">日志类别</param>
        /// <param name="source">日志源</param>
        /// <param name="message">日志消息</param>
        public static void Write(string type, LogCategory category, string source, string message)
        {
            if (HasSinks())
            {
                LogMessage logMsg = new LogMessage
                {
                    Type = type,
                    Category = category,
                    SourceProcess = SourceProcess,
                    Source = source,
                    ThreadId = "0x" + Thread.CurrentThread.ManagedThreadId.ToString("x4"),
                    Message = message
                };
                WriteLogMsg(logMsg);
            }
        }

        /// <summary>
        /// 写入日志消息到所有接收器
        /// </summary>
        /// <param name="logMsg">日志消息</param>
        private static void WriteLogMsg(LogMessage logMsg)
        {
            ILogSink[] sinkArray = Sinks.ToArray();
            foreach (ILogSink sink in sinkArray)
            {
                try
                {
                    sink.Write(logMsg);
                }
                catch (Exception ex)
                {
                    HandleSinkException(sink, ex);
                }
            }
        }

        /// <summary>
        /// 记录异常并重新抛出
        /// </summary>
        /// <param name="source">异常源</param>
        /// <param name="exception">异常对象</param>
        /// <param name="methodName">方法名</param>
        /// <returns>原始异常</returns>
        public static Exception WriteExceptionThrow(string source, Exception exception, [CallerMemberName] string methodName = "")
        {
            WriteExceptionInternal(source, exception, methodName);
            return exception;
        }

        /// <summary>
        /// 记录捕获的异常
        /// </summary>
        /// <param name="source">异常源</param>
        /// <param name="exception">异常对象</param>
        /// <param name="methodName">方法名</param>
        public static void WriteExceptionCatch(string source, Exception exception, [CallerMemberName] string methodName = "")
        {
            WriteExceptionInternal(source, exception, methodName);
        }

        /// <summary>
        /// 记录捕获的异常
        /// </summary>
        /// <param name="source">异常源</param>
        /// <param name="throwingFunction">抛出异常的函数</param>
        /// <param name="exception">异常对象</param>
        /// <param name="methodName">方法名</param>
        public static void WriteExceptionCatch(string source, string throwingFunction, Exception exception, [CallerMemberName] string methodName = "")
        {
            WriteExceptionInternal(source, exception, methodName + " (from " + throwingFunction + ")");
        }

        /// <summary>
        /// 记录异常内部实现
        /// </summary>
        /// <param name="source">异常源</param>
        /// <param name="exception">异常对象</param>
        /// <param name="methodName">方法名</param>
        private static void WriteExceptionInternal(string source, Exception exception, string methodName)
        {
            WriteIfEnabled(LogCategory.Error, source, "Exception in {0}: {1} - {2}", methodName, exception.GetType().Name, exception.Message);
            if (exception.StackTrace != null)
            {
                WriteIfEnabled(LogCategory.Error, source, "Stack trace: {0}", exception.StackTrace);
            }
            if (exception.InnerException != null)
            {
                WriteIfEnabled(LogCategory.Error, source, "Inner exception: {0} - {1}", exception.InnerException.GetType().Name, exception.InnerException.Message);
                if (exception.InnerException.StackTrace != null)
                {
                    WriteIfEnabled(LogCategory.Error, source, "Inner stack trace: {0}", exception.InnerException.StackTrace);
                }
            }
        }

        /// <summary>
        /// 关闭日志系统
        /// </summary>
        public static void Shutdown()
        {
            WriteIfEnabled(LogCategory.Information, "Log", "Shutting down logging system");
            
            lock (syncLock)
            {
                ILogSink[] sinkArray = Sinks.ToArray();
                Sinks.Clear();
                
                foreach (ILogSink sink in sinkArray)
                {
                    if (sink is IDisposable disposable)
                    {
                        try
                        {
                            disposable.Dispose();
                        }
                        catch
                        {
                            // 忽略关闭时的异常
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 格式化字符串
        /// </summary>
        /// <param name="source">日志源</param>
        /// <param name="format">格式字符串</param>
        /// <param name="args">格式化参数</param>
        /// <returns>格式化后的字符串</returns>
        private static string FormatString(string source, string format, params object[] args)
        {
            if (string.IsNullOrEmpty(format))
            {
                return string.Empty;
            }

            try
            {
                return string.Format(format, args);
            }
            catch (Exception ex)
            {
                WriteIfEnabled(LogCategory.Error, source, "Error formatting log message: {0}", ex.Message);
                return format;
            }
        }

        /// <summary>
        /// 处理接收器异常
        /// </summary>
        /// <param name="sink">异常发生的接收器</param>
        /// <param name="exception">异常对象</param>
        private static void HandleSinkException(ILogSink sink, Exception exception)
        {
            var handler = sinkException;
            if (handler != null)
            {
                try
                {
                    handler(sink, exception);
                }
                catch
                {
                    // 忽略处理器中的异常
                }
            }
        }

        /// <summary>
        /// 从所有接收器设置启用的日志类别
        /// </summary>
        public static void SetEnabledCategoriesFromLogSinks()
        {
            lock (syncLock)
            {
                LogCategory combinedCategories = LogCategory.None;
                ILogSink[] sinkArray = Sinks.ToArray();
                
                foreach (ILogSink sink in sinkArray)
                {
                    if (sink is LogSink logSink)
                    {
                        combinedCategories |= logSink.EnabledCategories;
                    }
                }
                
                EnabledCategories = combinedCategories;
            }
        }

        /// <summary>
        /// 添加委托到事件
        /// </summary>
        private static void AddDelegate<T>(ref T field, T value) where T : Delegate
        {
            T original = field;
            T newDelegate = (T)Delegate.Combine(original, value);
            field = newDelegate;
        }

        /// <summary>
        /// 从事件中移除委托
        /// </summary>
        private static void RemoveDelegate<T>(ref T field, T value) where T : Delegate
        {
            T original = field;
            T newDelegate = (T)Delegate.Remove(original, value);
            field = newDelegate;
        }
    }
} 