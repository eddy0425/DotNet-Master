using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DotNet.Logging.Sinks
{
    /// <summary>
    /// 文件日志 Sink - 高性能文件写入
    /// </summary>
    public sealed class FileLogSink : ILogSink
    {
        private readonly string _basePath;
        private readonly string _filePrefix;
        private readonly long _maxFileSize;
        private readonly int _retentionDays;
        private readonly object _lock = new object();
        private readonly Encoding _encoding;

        private StreamWriter _writer;
        private string _currentFilePath;
        private long _currentFileSize;
        private string _currentDate;
        private bool _disposed;

        /// <summary>
        /// 创建文件日志 Sink
        /// </summary>
        /// <param name="directory">日志目录</param>
        /// <param name="filePrefix">文件名前缀</param>
        /// <param name="maxFileSize">最大文件大小（字节），默认 10MB</param>
        /// <param name="retentionDays">保留天数，默认 30 天</param>
        public FileLogSink(
            string directory,
            string filePrefix = "app",
            long maxFileSize = 10 * 1024 * 1024,
            int retentionDays = 30)
        {
            if (string.IsNullOrWhiteSpace(directory))
                throw new ArgumentNullException(nameof(directory));

            _basePath = Path.GetFullPath(directory);
            _filePrefix = string.IsNullOrWhiteSpace(filePrefix) ? "app" : filePrefix;
            _maxFileSize = maxFileSize > 0 ? maxFileSize : 10 * 1024 * 1024;
            _retentionDays = retentionDays > 0 ? retentionDays : 30;
            _encoding = new UTF8Encoding(false); // 无 BOM 的 UTF-8

            Directory.CreateDirectory(_basePath);
            EnsureWriter();
        }

        public void Write(LogEntry entry)
        {
            if (_disposed || entry == null)
                return;

            var line = entry.Format();
            var bytes = _encoding.GetByteCount(line) + 2; // +2 for newline

            lock (_lock)
            {
                if (_disposed)
                    return;

                // 检查是否需要轮换
                if (NeedsRotation(bytes))
                {
                    RotateFile();
                }

                _writer.WriteLine(line);
                _currentFileSize += bytes;
            }
        }

        public void WriteBatch(IReadOnlyList<LogEntry> entries)
        {
            if (_disposed || entries == null || entries.Count == 0)
                return;

            lock (_lock)
            {
                if (_disposed)
                    return;

                foreach (var entry in entries)
                {
                    if (entry == null)
                        continue;

                    var line = entry.Format();
                    var bytes = _encoding.GetByteCount(line) + 2;

                    if (NeedsRotation(bytes))
                    {
                        RotateFile();
                    }

                    _writer.WriteLine(line);
                    _currentFileSize += bytes;
                }
            }
        }

        public void Flush()
        {
            if (_disposed)
                return;

            lock (_lock)
            {
                _writer?.Flush();
            }
        }

        /// <summary>
        /// 检查是否需要轮换文件
        /// </summary>
        private bool NeedsRotation(int additionalBytes)
        {
            // 检查日期变化
            var today = DateTime.Now.ToString("yyyyMMdd");
            if (_currentDate != today)
                return true;

            // 检查文件大小
            return _currentFileSize + additionalBytes > _maxFileSize;
        }

        /// <summary>
        /// 轮换到新文件
        /// </summary>
        private void RotateFile()
        {
            CloseWriter();
            CleanupOldFiles();
            EnsureWriter();
        }

        /// <summary>
        /// 确保写入器可用
        /// </summary>
        private void EnsureWriter()
        {
            if (_writer != null)
                return;

            _currentDate = DateTime.Now.ToString("yyyyMMdd");
            var sequence = GetNextSequence();

            var fileName = sequence == 1
                ? $"{_currentDate}_{_filePrefix}.log"
                : $"{_currentDate}_{_filePrefix}_{sequence}.log";

            _currentFilePath = Path.Combine(_basePath, fileName);

            var fileStream = new FileStream(
                _currentFilePath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: 64 * 1024, // 64KB 缓冲区
                FileOptions.None);

            _writer = new StreamWriter(fileStream, _encoding, 64 * 1024)
            {
                AutoFlush = false
            };

            _currentFileSize = fileStream.Length;
        }

        /// <summary>
        /// 获取下一个序号
        /// </summary>
        private int GetNextSequence()
        {
            var pattern = $"{_currentDate}_{_filePrefix}*.log";
            var files = Directory.GetFiles(_basePath, pattern);

            if (files.Length == 0)
                return 1;

            int maxSeq = 0;
            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var parts = name.Split('_');

                if (parts.Length == 2)
                {
                    // 格式: yyyyMMdd_prefix
                    maxSeq = Math.Max(maxSeq, 1);
                }
                else if (parts.Length >= 3 && int.TryParse(parts[parts.Length - 1], out var seq))
                {
                    // 格式: yyyyMMdd_prefix_N
                    maxSeq = Math.Max(maxSeq, seq);
                }
            }

            // 检查当前最大序号的文件是否已达到大小限制
            var currentFile = maxSeq == 1
                ? Path.Combine(_basePath, $"{_currentDate}_{_filePrefix}.log")
                : Path.Combine(_basePath, $"{_currentDate}_{_filePrefix}_{maxSeq}.log");

            if (File.Exists(currentFile))
            {
                var info = new FileInfo(currentFile);
                if (info.Length >= _maxFileSize)
                    return maxSeq + 1;
            }

            return Math.Max(maxSeq, 1);
        }

        /// <summary>
        /// 清理过期文件
        /// </summary>
        private void CleanupOldFiles()
        {
            try
            {
                var cutoff = DateTime.Now.AddDays(-_retentionDays);
                var files = Directory.GetFiles(_basePath, $"*_{_filePrefix}*.log");

                foreach (var file in files)
                {
                    var info = new FileInfo(file);
                    if (info.LastWriteTime < cutoff)
                    {
                        try
                        {
                            info.Delete();
                        }
                        catch
                        {
                            // 忽略删除失败
                        }
                    }
                }
            }
            catch
            {
                // 清理失败不影响正常写入
            }
        }

        /// <summary>
        /// 关闭当前写入器
        /// </summary>
        private void CloseWriter()
        {
            if (_writer == null)
                return;

            try
            {
                _writer.Flush();
                _writer.Dispose();
            }
            catch
            {
                // 忽略
            }
            finally
            {
                _writer = null;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_lock)
            {
                if (_disposed)
                    return;

                _disposed = true;
                CloseWriter();
            }
        }
    }
}

