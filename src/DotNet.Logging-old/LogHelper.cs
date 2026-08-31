using System;

namespace DotNet.Logging
{
    /// <summary>
    /// 日志助手类，提供简化的日志API
    /// </summary>
    public static class LogHelper
    {
        private static LogCore logCore;

        /// <summary>
        /// 日志系统初始化和健康状态事件
        /// </summary>
        public static event EventHandler<LogHelperStatusEventArgs> StatusChanged;

        /// <summary>
        /// 是否自动尝试在日志系统失败时重启
        /// </summary>
        public static bool AutoRestartEnabled
        {
            get 
            { 
                CheckInitialized();
                return logCore.AutoRestartEnabled; 
            }
            set 
            { 
                CheckInitialized();
                logCore.AutoRestartEnabled = value; 
            }
        }

        /// <summary>
        /// 是否启用周期性健康检查
        /// </summary>
        public static bool HealthCheckEnabled
        {
            get 
            { 
                CheckInitialized();
                return logCore.HealthCheckEnabled; 
            }
            set 
            { 
                CheckInitialized();
                logCore.HealthCheckEnabled = value; 
            }
        }

        /// <summary>
        /// 检查日志系统是否已初始化
        /// </summary>
        private static void CheckInitialized()
        {
            if (logCore == null)
                throw new InvalidOperationException("日志系统尚未初始化，请先调用Initialize方法");
        }

        /// <summary>
        /// 初始化日志系统（使用默认设置）
        /// </summary>
        /// <param name="applicationName">应用程序名称，用作日志文件前缀</param>
        /// <param name="logToConsole">是否同时输出到控制台</param>
        /// <param name="enableCompression">是否启用GZ压缩</param>
        public static void Initialize(string applicationName, bool logToConsole = true, bool enableCompression = true)
        {
            logCore = new LogCore();
            logCore.StatusChanged += (sender, args) => StatusChanged?.Invoke(sender, args);
            logCore.Initialize(applicationName, logToConsole, enableCompression);
        }

        /// <summary>
        /// 初始化日志系统
        /// </summary>
        /// <param name="applicationName">应用程序名称，用作日志文件前缀</param>
        /// <param name="logDirectory">日志文件目录</param>
        /// <param name="enabledCategories">启用的日志类别</param>
        /// <param name="maxFileSize">最大文件大小（字节）</param>
        /// <param name="maxDaysOld">日志文件保留天数</param>
        /// <param name="logToConsole">是否同时输出到控制台</param>
        /// <param name="enableCompression">是否启用GZ压缩</param>
        public static void Initialize(
            string applicationName,
            string logDirectory,
            LogCategory enabledCategories,
            long maxFileSize = 102400,
            int maxDaysOld = 30,
            bool logToConsole = true,
            bool enableCompression = true)
        {
            logCore = new LogCore();
            logCore.StatusChanged += (sender, args) => StatusChanged?.Invoke(sender, args);
            logCore.Initialize(applicationName, logDirectory, enabledCategories, maxFileSize, maxDaysOld, logToConsole, enableCompression);
        }

        /// <summary>
        /// 重新初始化日志系统
        /// </summary>
        public static void Reinitialize()
        {
            CheckInitialized();
            logCore.Reinitialize();
        }


        /// <summary>
        /// 记录调试信息
        /// </summary>
        /// <param name="source">日志源</param>
        /// <param name="message">日志消息</param>
        public static void Debug(string source, string message)
        {
            CheckInitialized();
            logCore.Debug(source, message);
        }

        /// <summary>
        /// 记录格式化的调试信息
        /// </summary>
        /// <param name="source">日志源</param>
        /// <param name="format">格式字符串</param>
        /// <param name="args">格式化参数</param>
        public static void Debug(string source, string format, params object[] args)
        {
            CheckInitialized();
            logCore.Debug(source, format, args);
        }

        /// <summary>
        /// 记录信息
        /// </summary>
        /// <param name="source">日志源</param>
        /// <param name="message">日志消息</param>
        public static void Info(string source, string message)
        {
            CheckInitialized();
            logCore.Info(source, message);
        }

        /// <summary>
        /// 记录格式化的信息
        /// </summary>
        /// <param name="source">日志源</param>
        /// <param name="format">格式字符串</param>
        /// <param name="args">格式化参数</param>
        public static void Info(string source, string format, params object[] args)
        {
            CheckInitialized();
            logCore.Info(source, format, args);
        }

        /// <summary>
        /// 记录警告
        /// </summary>
        /// <param name="source">日志源</param>
        /// <param name="message">日志消息</param>
        public static void Warning(string source, string message)
        {
            CheckInitialized();
            logCore.Warning(source, message);
        }

        /// <summary>
        /// 记录格式化的警告
        /// </summary>
        /// <param name="source">日志源</param>
        /// <param name="format">格式字符串</param>
        /// <param name="args">格式化参数</param>
        public static void Warning(string source, string format, params object[] args)
        {
            CheckInitialized();
            logCore.Warning(source, format, args);
        }

        /// <summary>
        /// 记录错误
        /// </summary>
        /// <param name="source">日志源</param>
        /// <param name="message">日志消息</param>
        public static void Error(string source, string message)
        {
            CheckInitialized();
            logCore.Error(source, message);
        }

        /// <summary>
        /// 记录格式化的错误
        /// </summary>
        /// <param name="source">日志源</param>
        /// <param name="format">格式字符串</param>
        /// <param name="args">格式化参数</param>
        public static void Error(string source, string format, params object[] args)
        {
            CheckInitialized();
            logCore.Error(source, format, args);
        }

        /// <summary>
        /// 记录异常
        /// </summary>
        /// <param name="source">日志源</param>
        /// <param name="exception">异常对象</param>
        /// <param name="message">附加消息</param>
        public static void Exception(string source, Exception exception, string message = null)
        {
            CheckInitialized();
            logCore.Exception(source, exception, message);
        }

        /// <summary>
        /// 添加内存日志接收器
        /// </summary>
        /// <param name="maxMessages">最大消息数量</param>
        /// <returns>创建的内存日志接收器</returns>
        public static MemoryLogSink AddMemoryLogger(int maxMessages = 1000)
        {
            CheckInitialized();
            return logCore.AddMemoryLogger(maxMessages);
        }

        /// <summary>
        /// 关闭日志系统
        /// </summary>
        public static void Shutdown()
        {
            if (logCore != null)
            {
                logCore.Shutdown();
                logCore = null;
            }
        }

    }

}