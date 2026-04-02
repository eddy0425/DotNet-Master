using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace DotNet.Logging
{
    /// <summary>
    /// 文件日志接收器，将日志写入文件
    /// </summary>
    public class FileLogSink : LogSink, IDisposable
    {
        private readonly object _lockObject = new object();
        private readonly object _compressLock = new object();
        private StreamWriter _streamWriter;
        private string _currentFileName;
        private DateTime _currentFileDate = DateTime.MinValue;
        private bool _disposed = false;
        private int _cleanupInProgress = 0;
        private readonly int _maxRetryCount = 3;
        private readonly int _retryDelayMs = 100;

        #region 公共属性

        /// <summary>
        /// 是否启用压缩
        /// </summary>
        public bool EnableCompression { get; set; }

        /// <summary>
        /// 是否自动刷新
        /// </summary>
        public bool AutoFlush { get; set; } = true;

        /// <summary>
        /// 是否按天创建文件
        /// </summary>
        public bool UseDailyFile { get; set; } = true;

        /// <summary>
        /// 最大文件大小（字节）
        /// </summary>
        public long MaxFileSize { get; set; } = 10485760; // 10MB

        /// <summary>
        /// 基础文件名
        /// </summary>
        public string BaseFileName { get; private set; }

        /// <summary>
        /// 基础目录
        /// </summary>
        public string BaseDirectory { get; set; }

        /// <summary>
        /// 日期子目录
        /// </summary>
        public string DateSubDirectory { get; set; }

        /// <summary>
        /// 文件扩展名
        /// </summary>
        public string FileExtension { get; set; } = ".log";

        /// <summary>
        /// 当前完整文件名
        /// </summary>
        public string FileName => _currentFileName;

        /// <summary>
        /// 最大文件数量
        /// </summary>
        public int MaxNumFiles { get; set; } = 10;

        /// <summary>
        /// 最大保留天数
        /// </summary>
        public int MaxDaysOld { get; set; } = 30;

        /// <summary>
        /// GZip压缩的默认缓冲区大小
        /// </summary>
        public int CompressionBufferSize { get; set; } = 81920; // 80KB缓冲区

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="baseFileName">基础文件名（完整路径）</param>
        /// <param name="extension">文件扩展名</param>
        public FileLogSink(string baseFileName, string extension)
            : this(Path.GetDirectoryName(Path.GetFullPath(baseFileName)), string.Empty, Path.GetFileName(baseFileName), extension)
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="baseFileName">基础文件名（完整路径）</param>
        public FileLogSink(string baseFileName)
            : this(Path.GetDirectoryName(Path.GetFullPath(baseFileName)), string.Empty, Path.GetFileName(baseFileName))
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="baseDirectory">基础目录</param>
        /// <param name="dateSubDirectory">日期子目录</param>
        /// <param name="baseFileName">基础文件名</param>
        public FileLogSink(string baseDirectory, string dateSubDirectory, string baseFileName)
            : base()
        {
            BaseDirectory = baseDirectory;
            DateSubDirectory = dateSubDirectory;
            BaseFileName = baseFileName;
            FileExtension = ".log";
            EnableCompression = false;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="baseDirectory">基础目录</param>
        /// <param name="dateSubDirectory">日期子目录</param>
        /// <param name="baseFileName">基础文件名</param>
        /// <param name="extension">文件扩展名</param>
        public FileLogSink(string baseDirectory, string dateSubDirectory, string baseFileName, string extension)
            : base()
        {
            BaseDirectory = baseDirectory;
            DateSubDirectory = dateSubDirectory;
            BaseFileName = baseFileName;
            FileExtension = extension;
            EnableCompression = extension.EndsWith(".gz", StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region 日志写入

        /// <summary>
        /// 写入日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <returns>是否成功</returns>
        protected override bool DoWrite(LogMessage message)
        {
            if (_disposed)
                return false;
                
            for (int attempt = 0; attempt < _maxRetryCount; attempt++)
            {
                try
                {
                    return WriteLog(message);
                }
                catch (IOException ex)
                {
                    // 只有在不是最后一次尝试时才等待重试
                    if (attempt < _maxRetryCount - 1)
                    {
                        HandleLogException($"写入日志失败，将在{_retryDelayMs}ms后重试 (尝试 {attempt + 1}/{_maxRetryCount})", ex);
                        Thread.Sleep(_retryDelayMs * (attempt + 1)); // 递增延迟
                    }
                    else
                    {
                        HandleLogException("写入日志失败，已达到最大重试次数", ex);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    HandleLogException("写入日志失败", ex);
                    return false;
                }
            }
            
            return false;
        }

        /// <summary>
        /// 写入日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <returns>是否写入成功</returns>
        private bool WriteLog(LogMessage message)
        {
            if (message == null) return false;
            
            DateTime utcTimeStamp = DateTime.FromBinary(message.UtcTimestamp);
            return WriteLog(utcTimeStamp, () => WriteLogMessage(message));
        }

        /// <summary>
        /// 写入日志记录
        /// </summary>
        /// <param name="utcDateTime">UTC日期时间</param>
        /// <param name="writeAction">写入操作</param>
        /// <returns>写入是否成功</returns>
        private bool WriteLog(DateTime utcDateTime, Action writeAction)
        {
            bool result = true;
            
            try
            {
                lock (_lockObject)
                {
                    EnsureFileOpen(utcDateTime);
                    
                    if (_streamWriter == null)
                    {
                        return false;
                    }
                    
                    writeAction();
                    
                    if (AutoFlush)
                    {
                        _streamWriter.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                result = false;
                HandleLogException("写入日志文件失败", ex);
            }
            
            return result;
        }

        /// <summary>
        /// 写入日志消息到文件
        /// </summary>
        /// <param name="message">日志消息</param>
        private void WriteLogMessage(LogMessage message)
        {
            if (message != null && _streamWriter != null)
            {
                string value = message.Serialize();
                _streamWriter.WriteLine(value);
            }
        }

        #endregion

        #region 文件管理

        /// <summary>
        /// 确保文件已打开
        /// </summary>
        /// <param name="utcDateTime">UTC日期时间</param>
        private void EnsureFileOpen(DateTime utcDateTime)
        {
            try
            {
                DateTime localDate = utcDateTime.ToLocalTime().Date;
                    
                // 如果文件已打开但需要新文件（日期变化或文件过大）
                if (_streamWriter != null && 
                    ((UseDailyFile && localDate != _currentFileDate.Date) || 
                    (File.Exists(_currentFileName) && new FileInfo(_currentFileName).Length >= MaxFileSize)))
                {
                    CloseFile();
                }
                
                // 如果没有打开文件，则创建新文件
                if (_streamWriter == null)
                {
                    OpenNewFile(localDate);
                    
                    // 在打开新文件后，使用原子操作确保只启动一个清理任务
                    TriggerCleanupOldFiles();
                }
            }
            catch (IOException ex)
            {
                HandleLogException("确保文件打开时发生IO异常", ex);
                
                // 尝试关闭现有的流并将其设为null，以便下次尝试重新创建
                SafeCloseStreamWriter();
                
                throw;
            }
            catch (Exception ex)
            {
                HandleLogException("确保文件打开失败", ex);
                throw;
            }
        }

        /// <summary>
        /// 安全关闭StreamWriter，忽略异常
        /// </summary>
        private void SafeCloseStreamWriter()
        {
            if (_streamWriter != null)
            {
                try
                {
                    _streamWriter.Flush();
                    _streamWriter.Dispose();
                }
                catch
                {
                    // 忽略关闭时的异常
                }
                finally
                {
                    _streamWriter = null;
                }
            }
        }

        /// <summary>
        /// 触发清理旧日志文件任务，确保同一时间只执行一次
        /// </summary>
        private void TriggerCleanupOldFiles()
        {
            // 如果已有清理任务在进行中，则不启动新任务
            if (Interlocked.CompareExchange(ref _cleanupInProgress, 1, 0) == 0)
            {
                Task.Run(() => 
                {
                    try
                    {
                        CleanupOldFiles();
                    }
                    catch (Exception ex)
                    {
                        HandleLogException("清理旧文件任务出错", ex);
                    }
                    finally
                    {
                        // 无论是否发生异常，都要重置清理标志
                        Interlocked.Exchange(ref _cleanupInProgress, 0);
                    }
                });
            }
        }

        /// <summary>
        /// 打开新文件
        /// </summary>
        /// <param name="localDate">本地日期</param>
        private void OpenNewFile(DateTime localDate)
        {
            // 尝试多次打开文件，处理可能的访问冲突
            for (int attempt = 0; attempt < _maxRetryCount; attempt++)
            {
                try
                {
                    // 创建目录
                    string directory = GetFullDirectory();
                    
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    // 生成文件名
                    string dateStr = localDate.ToString("yyyyMMdd");
                    string baseFileNameWithDate = $"{BaseFileName}_{dateStr}";
                    
                    // 查找序号
                    int sequenceNumber = 1;
                    string fileName;
                    
                    do
                    {
                        fileName = Path.Combine(directory, $"{baseFileNameWithDate}_{sequenceNumber}{FileExtension}");
                        sequenceNumber++;
                    } while (File.Exists(fileName) && new FileInfo(fileName).Length >= MaxFileSize);
                                
                    // 创建文件
                    _currentFileName = fileName;
                    _currentFileDate = localDate;
                    bool fileExists = File.Exists(_currentFileName);
                    
                    _streamWriter = new StreamWriter(
                        new FileStream(_currentFileName, FileMode.Append, FileAccess.Write, FileShare.Read),
                        Encoding.UTF8)
                    {
                        AutoFlush = AutoFlush
                    };
                    
                    if (!fileExists)
                    {
                        WriteFileHeader();
                    }
                    
                    // 如果成功打开文件，则跳出重试循环
                    break;
                }
                catch (IOException ex)
                {
                    // 关闭可能已打开的流
                    SafeCloseStreamWriter();
                    
                    // 只有在不是最后一次尝试时才等待重试
                    if (attempt < _maxRetryCount - 1)
                    {
                        HandleLogException($"打开新日志文件失败，将在{_retryDelayMs}ms后重试 (尝试 {attempt + 1}/{_maxRetryCount})", ex);
                        Thread.Sleep(_retryDelayMs * (attempt + 1)); // 递增延迟
                    }
                    else
                    {
                        HandleLogException("打开新日志文件失败，已达到最大重试次数", ex);
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    HandleLogException("打开新日志文件失败", ex);
                    throw;
                }
            }
        }

        /// <summary>
        /// 写入文件头
        /// </summary>
        protected virtual void WriteFileHeader()
        {
            if (_streamWriter != null)
            {
                // 写入Cimetrix LogViewer兼容的表头，严格匹配原始格式
                _streamWriter.WriteLine("UtcTimestamp\tMicroseconds\tThreadId\tSourceProcess\tSource\tType\tMessage");
            }
        }

        /// <summary>
        /// 关闭当前文件
        /// </summary>
        protected virtual void CloseFile()
        {
            if (_streamWriter != null)
            {
                string currentFileName = _currentFileName;
                bool enabledCompression = EnableCompression;
                
                SafeCloseStreamWriter();
                
                // 如果启用了压缩且文件存在
                if (enabledCompression && 
                    FileExtension.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) && 
                    File.Exists(currentFileName))
                {
                    CompressFileAsync(currentFileName);
                }
            }
        }

        /// <summary>
        /// 异步压缩文件
        /// </summary>
        /// <param name="fileName">要压缩的文件名</param>
        private void CompressFileAsync(string fileName)
        {
            // 在另一个线程中压缩文件，避免阻塞主线程
            Task.Run(() => 
            {
                try
                {
                    lock (_compressLock)
                    {
                        string uncompressedFileName = fileName.Substring(0, fileName.Length - 3);
                        
                        // 重命名原文件为不带.gz的文件
                        File.Move(fileName, uncompressedFileName);
                        
                        // 压缩文件
                        bool success = CompressFile(uncompressedFileName, fileName);
                        
                        // 删除未压缩的文件
                        if (success && File.Exists(uncompressedFileName))
                        {
                            File.Delete(uncompressedFileName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    HandleLogException("压缩日志文件失败", ex);
                }
            });
        }

        /// <summary>
        /// 压缩文件
        /// </summary>
        /// <param name="sourceFile">源文件路径</param>
        /// <param name="destinationFile">目标文件路径</param>
        /// <returns>是否成功压缩</returns>
        private bool CompressFile(string sourceFile, string destinationFile)
        {
            try
            {
                // 确保源文件存在
                if (!File.Exists(sourceFile))
                {
                    throw new FileNotFoundException("源文件不存在", sourceFile);
                }
                    
                    // 使用FileStream直接压缩，确保兼容Cimetrix LogViewer
                    using (FileStream sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        // 创建GZ文件
                        using (FileStream destStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            // 使用标准压缩级别
                            using (System.IO.Compression.GZipStream gzipStream = 
                               new System.IO.Compression.GZipStream(destStream, System.IO.Compression.CompressionLevel.Optimal))
                            {
                                // 大缓冲区提高性能
                            byte[] buffer = new byte[CompressionBufferSize];
                                int bytesRead;
                                
                                // 一次读取一个块并写入GZ流
                                while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    gzipStream.Write(buffer, 0, bytesRead);
                                }
                                
                                // 确保所有数据都刷新到文件
                                gzipStream.Flush();
                            }
                        }
                    }
                    
                    // 验证压缩文件已成功创建
                    if (new FileInfo(destinationFile).Length == 0)
                    {
                        throw new InvalidOperationException("压缩后的文件大小为0，压缩可能失败");
                    }

                return true;
                }
                catch (Exception ex)
                {
                HandleLogException($"压缩文件时发生错误: {ex.Message}", ex);
                    
                    try
                    {
                        // 如果压缩文件已创建但无效，则删除它
                        if (File.Exists(destinationFile))
                        {
                            File.Delete(destinationFile);
                        }
                    }
                    catch
                    {
                    // 忽略清理过程中的错误
                }
                
                return false;
            }
        }

        /// <summary>
        /// 获取完整目录路径
        /// </summary>
        /// <returns>完整目录路径</returns>
        private string GetFullDirectory()
        {
            string directory = BaseDirectory;
            
            if (!string.IsNullOrEmpty(DateSubDirectory))
            {
                directory = Path.Combine(directory, DateSubDirectory);
            }
            
            return directory;
        }

        /// <summary>
        /// 清理旧日志文件
        /// </summary>
        private void CleanupOldFiles()
        {
            try
            {
                string directory = GetFullDirectory();
                if (!Directory.Exists(directory)) return;

                var allLogFiles = new List<FileInfo>();
                
                // 获取所有匹配的日志文件
                try
                {
                    var standardLogFiles = Directory.GetFiles(directory, $"{BaseFileName}_*{FileExtension}")
                                                   .Select(f => new FileInfo(f));
                    allLogFiles.AddRange(standardLogFiles);
                    
                    // 如果使用GZ压缩，也获取压缩文件
                    if (EnableCompression)
                    {
                        var compressedLogFiles = Directory.GetFiles(directory, $"{BaseFileName}_*.log.gz")
                                                        .Select(f => new FileInfo(f));
                        allLogFiles.AddRange(compressedLogFiles);
                    }
                }
                catch (Exception ex)
                {
                    HandleLogException("获取日志文件列表失败", ex);
                    return;
                }
                
                // 按最后写入时间排序
                var sortedFiles = allLogFiles.OrderByDescending(f => f.LastWriteTime).ToArray();

                // 限制文件数量
                if (MaxNumFiles > 0 && sortedFiles.Length > MaxNumFiles)
                {
                    for (int i = MaxNumFiles; i < sortedFiles.Length; i++)
                    {
                        DeleteFileWithRetry(sortedFiles[i].FullName);
                    }
                }

                // 删除过期的文件
                if (MaxDaysOld > 0)
                {
                    DateTime oldestAllowed = DateTime.Now.AddDays(-MaxDaysOld);
                    var oldFiles = sortedFiles.Where(f => f.LastWriteTime < oldestAllowed).ToArray();
                    
                    foreach (var file in oldFiles)
                    {
                        DeleteFileWithRetry(file.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                HandleLogException("清理旧日志文件失败", ex);
            }
        }
        
        /// <summary>
        /// 带重试的文件删除
        /// </summary>
        /// <param name="filePath">要删除的文件路径</param>
        /// <returns>是否成功删除</returns>
        private bool DeleteFileWithRetry(string filePath)
        {
            for (int attempt = 0; attempt < _maxRetryCount; attempt++)
            {
                try
                {
                    // 尝试重置文件属性后删除
                    if (File.Exists(filePath))
                    {
                        File.SetAttributes(filePath, FileAttributes.Normal);
                        File.Delete(filePath);
                        return true;
                    }
                    return false; // 文件不存在
                }
                catch (IOException)
                {
                    // 文件可能被其他进程占用，稍后重试
                    if (attempt < _maxRetryCount - 1)
                    {
                        Thread.Sleep(_retryDelayMs * (attempt + 1));
                    }
                }
                catch (Exception ex)
                {
                    HandleLogException($"删除文件失败: {filePath}", ex);
                    return false;
                }
            }
            
            return false; // 达到最大重试次数仍然失败
        }

        #endregion

        #region 错误处理和资源管理

        /// <summary>
        /// 处理日志异常
        /// </summary>
        private void HandleLogException(string message, Exception ex)
        {
            try
            {
                // 记录到内部错误日志
                Log.WriteIfEnabled(LogCategory.Error, "FileLogSink", $"{message}: {ex.Message}");

                // 将详细信息记录到调试输出（仅 Debug 模式）
                Debug.WriteLine($"[FileLogSink Error] {message}: {ex.GetType().Name} - {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"[InnerException] {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                }
            }
            catch
            {
                // 最后的保底方案，如果内部错误记录失败，则输出到调试
                Debug.WriteLine($"[日志系统错误] {message}: {ex.Message}");
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing">是否正在主动释放</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 释放托管资源
                    lock (_lockObject)
                    {
                        CloseFile();
                    }
                    
                    // 等待清理任务完成
                    int waitCount = 0;
                    while (Interlocked.CompareExchange(ref _cleanupInProgress, 0, 0) != 0 && waitCount < 50)
                    {
                        Thread.Sleep(10);
                        waitCount++;
                    }
                }

                // 标记为已释放
                _disposed = true;
            }
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~FileLogSink()
        {
            Dispose(false);
        }

        #endregion
    }
} 