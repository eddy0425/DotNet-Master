using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DotNet.Logging
{
    /// <summary>
    /// 日志助手类，提供简化的日志API
    /// </summary>
    public class LogCore
    {
        private bool initialized = false;
        private readonly object initLock = new object();
        private int shutdownInProgress = 0;
        private bool autoRestartEnabled = true;
        private bool healthCheckEnabled = false;
        private Timer healthCheckTimer = null;
        private readonly TimeSpan healthCheckInterval = TimeSpan.FromMinutes(5);
        private readonly TimeSpan restartDelay = TimeSpan.FromSeconds(3);
        private LogInitSettings lastInitSettings = null;

        /// <summary>
        /// 日志系统初始化和健康状态事件
        /// </summary>
        public event EventHandler<LogHelperStatusEventArgs> StatusChanged;

        /// <summary>
        /// 是否自动尝试在日志系统失败时重启
        /// </summary>
        public bool AutoRestartEnabled
        {
            get { return autoRestartEnabled; }
            set { autoRestartEnabled = value; }
        }

        /// <summary>
        /// 是否启用周期性健康检查
        /// </summary>
        public bool HealthCheckEnabled
        {
            get { return healthCheckEnabled; }
            set 
            {
                lock (initLock)
                {
                    if (healthCheckEnabled != value)
                    {
                        healthCheckEnabled = value;
                        UpdateHealthCheckTimer();
                    }
                }
            }
        }

        /// <summary>
        /// 初始化日志系统（使用默认设置）
        /// </summary>
        /// <param name="applicationName">应用程序名称，用作日志文件前缀</param>
        /// <param name="logToConsole">是否同时输出到控制台</param>
        /// <param name="enableCompression">是否启用GZ压缩</param>
        public void Initialize(string applicationName, bool logToConsole = true, bool enableCompression = true)
        {
            Initialize(applicationName, "Logs", LogCategory.All, 102400, 30, logToConsole, enableCompression);
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
        public void Initialize(
            string applicationName,
            string logDirectory,
            LogCategory enabledCategories,
            long maxFileSize = 102400,
            int maxDaysOld = 30,
            bool logToConsole = true,
            bool enableCompression = true)
        {
            if (string.IsNullOrEmpty(applicationName))
            {
                throw new ArgumentNullException(nameof(applicationName), "应用程序名称不能为空");
            }

            lock (initLock)
            {
                // 保存初始化设置，用于可能的重新初始化
                lastInitSettings = new LogInitSettings
                {
                    ApplicationName = applicationName,
                    LogDirectory = logDirectory,
                    EnabledCategories = enabledCategories,
                    MaxFileSize = maxFileSize,
                    MaxDaysOld = maxDaysOld,
                    LogToConsole = logToConsole,
                    EnableCompression = enableCompression
                };

                // 如果正在进行关闭操作，等待完成
                WaitForShutdownToComplete();

                // 确保关闭现有日志系统
                if (initialized)
                {
                    ShutdownInternal(false);
                }

                try
                {
                    // 设置日志类别
                    Log.EnabledCategories = enabledCategories;

                    // 创建日志目录
                    string logPath = Path.IsPathRooted(logDirectory) 
                        ? logDirectory 
                        : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logDirectory);
                    
                    if (!Directory.Exists(logPath))
                    {
                        Directory.CreateDirectory(logPath);
                    }

                    // 创建文件日志接收器
                    var fileLogSink = new FileLogSink(logPath, string.Empty, applicationName, 
                        enableCompression ? ".log.gz" : ".log")
                    {
                        EnabledCategories = enabledCategories,
                        MaxFileSize = maxFileSize,
                        MaxDaysOld = maxDaysOld,
                        AutoFlush = true,
                        EnableCompression = enableCompression
                    };
                    Log.Sinks.Add(fileLogSink);

                    // 如果需要，创建控制台日志接收器
                    if (logToConsole)
                    {
                        var consoleLogSink = new ConsoleLogSink
                        {
                            EnabledCategories = enabledCategories
                        };
                        Log.Sinks.Add(consoleLogSink);
                    }

                    initialized = true;
                    
                    // 更新健康检查定时器
                    UpdateHealthCheckTimer();
                    
                    // 记录初始化成功信息
                    Log.WriteIfEnabled(LogCategory.Information, "LogHelper",
                        "日志系统初始化成功: 应用程序={0}, 日志目录={1}, 最大文件大小={2}KB, 保留天数={3}, 压缩={4}",
                        applicationName, logPath, maxFileSize / 1024, maxDaysOld, enableCompression ? "启用" : "禁用");

                    // 触发状态改变事件
                    OnStatusChanged(new LogHelperStatusEventArgs(LogHelperStatus.Initialized));
                }
                catch (Exception ex)
                {
                    // 触发状态改变事件
                    OnStatusChanged(new LogHelperStatusEventArgs(LogHelperStatus.InitializationFailed, ex));

                    throw new Exception($"日志系统初始化失败: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// 重新初始化日志系统
        /// </summary>
        public void Reinitialize()
        {
            lock (initLock)
            {
                if (lastInitSettings == null)
                {
                    throw new InvalidOperationException("无法重新初始化日志系统，未找到之前的初始化设置");
                }

                try
                {
                    var settings = lastInitSettings;
                    Initialize(
                        settings.ApplicationName,
                        settings.LogDirectory,
                        settings.EnabledCategories,
                        settings.MaxFileSize,
                        settings.MaxDaysOld,
                        settings.LogToConsole,
                        settings.EnableCompression);
                }
                catch (Exception ex)
                {
                    throw new Exception($"重新初始化日志系统失败: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// 等待关闭操作完成
        /// </summary>
        private void WaitForShutdownToComplete()
        {
            int retryCount = 0;
            while (Interlocked.CompareExchange(ref shutdownInProgress, 0, 0) != 0 && retryCount < 50)
            {
                Thread.Sleep(10);
                retryCount++;
            }
        }

        /// <summary>
        /// 更新健康检查定时器
        /// </summary>
        private void UpdateHealthCheckTimer()
        {
            lock (initLock)
            {
                if (healthCheckTimer != null)
                {
                    healthCheckTimer.Dispose();
                    healthCheckTimer = null;
                }

                if (healthCheckEnabled && initialized)
                {
                    healthCheckTimer = new Timer(PerformHealthCheck, null, 
                        healthCheckInterval, healthCheckInterval);
                }
            }
        }

        /// <summary>
        /// 执行健康检查
        /// </summary>
        private void PerformHealthCheck(object state)
        {
            try
            {
                // 检查日志系统是否可用
                bool isHealthy = CheckLogSystemHealth();

                if (!isHealthy && autoRestartEnabled && lastInitSettings != null)
                {
                    // 尝试重新初始化
                    Task.Delay(restartDelay).ContinueWith(_ =>
                    {
                        try
                        {
                            Reinitialize();
                        }
                        catch (Exception ex)
                        {
                            // 重新初始化失败，触发事件通知
                            OnStatusChanged(new LogHelperStatusEventArgs(LogHelperStatus.InitializationFailed, ex));
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                // 在 Timer 回调中不应抛出异常，否则会导致应用程序崩溃
                // 触发状态改变事件通知健康检查失败
                OnStatusChanged(new LogHelperStatusEventArgs(LogHelperStatus.Unhealthy, ex));
            }
        }

        /// <summary>
        /// 检查日志系统健康状态
        /// </summary>
        /// <returns>系统是否健康</returns>
        private bool CheckLogSystemHealth()
        {
            if (!initialized)
            {
                return false;
            }

            try
            {
                // 尝试写入测试日志
                Log.WriteIfEnabled(LogCategory.Debug, "LogHelper.HealthCheck", "日志系统健康检查");
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 触发状态改变事件
        /// </summary>
        private void OnStatusChanged(LogHelperStatusEventArgs args)
        {
            try
            {
                StatusChanged?.Invoke(null, args);
            }
            catch
            {
                // 忽略事件处理器中的错误
            }
        }

        /// <summary>
        /// 记录调试信息
        /// </summary>
        /// <param name="source">日志源</param>
        /// <param name="message">日志消息</param>
        public void Debug(string source, string message)
        {
            Log.WriteIfEnabled(LogCategory.Debug, source, message);
        }

        /// <summary>
        /// 记录格式化的调试信息
        /// </summary>
        /// <param name="source">日志源</param>
        /// <param name="format">格式字符串</param>
        /// <param name="args">格式化参数</param>
        public void Debug(string source, string format, params object[] args)
        {
            Log.WriteIfEnabled(LogCategory.Debug, source, format, args);
        }

        /// <summary>
        /// 记录信息
        /// </summary>
        /// <param name="source">日志源</param>
        /// <param name="message">日志消息</param>
        public void Info(string source, string message)
        {
            Log.WriteIfEnabled(LogCategory.Information, source, message);
        }

        /// <summary>
        /// 记录格式化的信息
        /// </summary>
        /// <param name="source">日志源</param>
        /// <param name="format">格式字符串</param>
        /// <param name="args">格式化参数</param>
        public void Info(string source, string format, params object[] args)
        {
            Log.WriteIfEnabled(LogCategory.Information, source, format, args);
        }

        /// <summary>
        /// 记录警告
        /// </summary>
        /// <param name="source">日志源</param>
        /// <param name="message">日志消息</param>
        public void Warning(string source, string message)
        {
            Log.WriteIfEnabled(LogCategory.Warning, source, message);
        }

        /// <summary>
        /// 记录格式化的警告
        /// </summary>
        /// <param name="source">日志源</param>
        /// <param name="format">格式字符串</param>
        /// <param name="args">格式化参数</param>
        public void Warning(string source, string format, params object[] args)
        {
            Log.WriteIfEnabled(LogCategory.Warning, source, format, args);
        }

        /// <summary>
        /// 记录错误
        /// </summary>
        /// <param name="source">日志源</param>
        /// <param name="message">日志消息</param>
        public void Error(string source, string message)
        {
            Log.WriteIfEnabled(LogCategory.Error, source, message);
        }

        /// <summary>
        /// 记录格式化的错误
        /// </summary>
        /// <param name="source">日志源</param>
        /// <param name="format">格式字符串</param>
        /// <param name="args">格式化参数</param>
        public void Error(string source, string format, params object[] args)
        {
            Log.WriteIfEnabled(LogCategory.Error, source, format, args);
        }

        /// <summary>
        /// 记录异常
        /// </summary>
        /// <param name="source">日志源</param>
        /// <param name="exception">异常对象</param>
        /// <param name="message">附加消息</param>
        public void Exception(string source, Exception exception, string message = null)
        {
            if (!string.IsNullOrEmpty(message))
            {
                Log.WriteIfEnabled(LogCategory.Error, source, "{0}: {1}", message, exception.Message);
            }
            Log.WriteExceptionCatch(source, exception);
        }

        /// <summary>
        /// 添加内存日志接收器
        /// </summary>
        /// <param name="maxMessages">最大消息数量</param>
        /// <returns>创建的内存日志接收器</returns>
        public MemoryLogSink AddMemoryLogger(int maxMessages = 1000)
        {
            lock (initLock)
            {
                if (!initialized)
                {
                    throw new InvalidOperationException("无法添加内存日志接收器，日志系统尚未初始化");
                }
                
                var memorySink = new MemoryLogSink(maxMessages)
                {
                    EnabledCategories = Log.EnabledCategories
                };
                Log.Sinks.Add(memorySink);
                return memorySink;
            }
        }

        /// <summary>
        /// 关闭日志系统
        /// </summary>
        public void Shutdown()
        {
            ShutdownInternal(true);
        }
        
        /// <summary>
        /// 关闭日志系统内部实现
        /// </summary>
        private void ShutdownInternal(bool updateLastSettings)
        {
            lock (initLock)
            {
                if (initialized && Interlocked.CompareExchange(ref shutdownInProgress, 1, 0) == 0)
                {
                    try
                    {
                        // 停止健康检查定时器
                        if (healthCheckTimer != null)
                        {
                            healthCheckTimer.Dispose();
                            healthCheckTimer = null;
                        }
                        
                        // 记录关闭消息
                        Log.WriteIfEnabled(LogCategory.Information, "LogHelper", "正在关闭日志系统...");
                        
                        // 关闭日志系统
                        Log.Shutdown();
                        
                        // 清除最后一次设置
                        if (updateLastSettings)
                        {
                            lastInitSettings = null;
                        }
                        
                        // 触发状态改变事件
                        OnStatusChanged(new LogHelperStatusEventArgs(LogHelperStatus.ShutDown));
                    }
                    catch (Exception ex)
                    {
                        // 清理方法不抛异常，避免在 finally 块中调用时掩盖原始异常
                        // 触发状态改变事件通知关闭失败
                        OnStatusChanged(new LogHelperStatusEventArgs(LogHelperStatus.ShutdownFailed, ex));
                    }
                    finally
                    {
                        initialized = false;
                        Interlocked.Exchange(ref shutdownInProgress, 0);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// 日志系统状态枚举
    /// </summary>
    public enum LogHelperStatus
    {
        /// <summary>已初始化</summary>
        Initialized,
        
        /// <summary>初始化失败</summary>
        InitializationFailed,
        
        /// <summary>已关闭</summary>
        ShutDown,
        
        /// <summary>关闭失败</summary>
        ShutdownFailed,
        
        /// <summary>运行状况异常</summary>
        Unhealthy
    }
    
    /// <summary>
    /// 日志系统状态事件参数
    /// </summary>
    public class LogHelperStatusEventArgs : EventArgs
    {
        /// <summary>当前状态</summary>
        public LogHelperStatus Status { get; }
        
        /// <summary>发生的异常（如果有）</summary>
        public Exception Exception { get; }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="status">日志系统状态</param>
        /// <param name="exception">相关异常（可选）</param>
        public LogHelperStatusEventArgs(LogHelperStatus status, Exception exception = null)
        {
            Status = status;
            Exception = exception;
        }
    }
    
    /// <summary>
    /// 日志初始化设置
    /// </summary>
    internal class LogInitSettings
    {
        /// <summary> 应用程序名称 </summary>
        public string ApplicationName { get; set; }

        /// <summary> 日志目录 </summary>
        public string LogDirectory { get; set; }

        /// <summary> 启用的日志类别 </summary>
        public LogCategory EnabledCategories { get; set; }

        /// <summary> 最大文件大小 </summary>
        public long MaxFileSize { get; set; }

        /// <summary> 最大保留天数 </summary>
        public int MaxDaysOld { get; set; }

        /// <summary> 是否输出到控制台 </summary>
        public bool LogToConsole { get; set; }

        /// <summary> 是否启用压缩 </summary>
        public bool EnableCompression { get; set; }
    }
} 