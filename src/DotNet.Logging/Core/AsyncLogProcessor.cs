using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace DotNet.Logging
{
    /// <summary>
    /// 异步日志处理器 - 使用后台线程批量处理日志
    /// </summary>
    internal sealed class AsyncLogProcessor : IDisposable
    {
        private readonly BlockingCollection<LogEntry> _queue;
        private readonly List<ILogSink> _sinks;
        private readonly Thread _workerThread;
        private readonly int _batchSize;
        private readonly TimeSpan _flushInterval;
        private readonly CancellationTokenSource _cts;
        private volatile bool _disposed;

        /// <summary>
        /// 创建异步日志处理器
        /// </summary>
        /// <param name="sinks">日志输出目标列表</param>
        /// <param name="batchSize">批量处理大小</param>
        /// <param name="flushInterval">刷新间隔</param>
        /// <param name="queueCapacity">队列容量（0 表示无限制）</param>
        public AsyncLogProcessor(
            List<ILogSink> sinks,
            int batchSize = 100,
            TimeSpan? flushInterval = null,
            int queueCapacity = 10000)
        {
            _sinks = sinks ?? throw new ArgumentNullException(nameof(sinks));
            _batchSize = batchSize > 0 ? batchSize : 100;
            _flushInterval = flushInterval ?? TimeSpan.FromMilliseconds(500);
            _cts = new CancellationTokenSource();

            _queue = queueCapacity > 0
                ? new BlockingCollection<LogEntry>(queueCapacity)
                : new BlockingCollection<LogEntry>();

            _workerThread = new Thread(ProcessQueue)
            {
                Name = "LogProcessor",
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal
            };
            _workerThread.Start();
        }

        /// <summary>
        /// 入队日志条目
        /// </summary>
        public bool Enqueue(LogEntry entry)
        {
            if (_disposed || entry == null)
                return false;

            try
            {
                // TryAdd 避免在队列满时阻塞
                return _queue.TryAdd(entry, 0);
            }
            catch (InvalidOperationException)
            {
                // 队列已完成添加
                return false;
            }
        }

        /// <summary>
        /// 处理队列中的日志
        /// </summary>
        private void ProcessQueue()
        {
            var batch = new List<LogEntry>(_batchSize);
            var lastFlush = DateTime.UtcNow;

            while (!_disposed)
            {
                try
                {
                    // 尝试从队列获取日志，带超时
                    if (_queue.TryTake(out var entry, (int)_flushInterval.TotalMilliseconds, _cts.Token))
                    {
                        batch.Add(entry);

                        // 继续获取更多日志直到批量大小或队列为空
                        while (batch.Count < _batchSize && _queue.TryTake(out entry, 0))
                        {
                            batch.Add(entry);
                        }
                    }

                    // 检查是否需要刷新
                    var now = DateTime.UtcNow;
                    var shouldFlush = batch.Count >= _batchSize ||
                                      (batch.Count > 0 && (now - lastFlush) >= _flushInterval);

                    if (shouldFlush && batch.Count > 0)
                    {
                        FlushBatch(batch);
                        batch.Clear();
                        lastFlush = now;
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // 日志处理器的错误不应导致线程终止
                    System.Diagnostics.Debug.WriteLine($"[AsyncLogProcessor] Error: {ex.Message}");
                }
            }

            // 处理剩余的日志
            DrainQueue(batch);
        }

        /// <summary>
        /// 排空队列中的剩余日志
        /// </summary>
        private void DrainQueue(List<LogEntry> batch)
        {
            try
            {
                while (_queue.TryTake(out var entry, 0))
                {
                    batch.Add(entry);
                    if (batch.Count >= _batchSize)
                    {
                        FlushBatch(batch);
                        batch.Clear();
                    }
                }

                if (batch.Count > 0)
                {
                    FlushBatch(batch);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AsyncLogProcessor] Drain error: {ex.Message}");
            }
        }

        /// <summary>
        /// 将批量日志写入所有 Sink
        /// </summary>
        private void FlushBatch(List<LogEntry> batch)
        {
            foreach (var sink in _sinks)
            {
                try
                {
                    sink.WriteBatch(batch);
                    sink.Flush();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AsyncLogProcessor] Sink error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 同步刷新所有待处理的日志
        /// </summary>
        public void FlushSync()
        {
            // 给处理线程一点时间处理当前批次
            var spinWait = new SpinWait();
            var timeout = DateTime.UtcNow.AddSeconds(5);

            while (_queue.Count > 0 && DateTime.UtcNow < timeout)
            {
                spinWait.SpinOnce();
            }

            // 刷新所有 Sink
            foreach (var sink in _sinks)
            {
                try
                {
                    sink.Flush();
                }
                catch
                {
                    // 忽略
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            // 停止接收新日志
            _queue.CompleteAdding();

            // 取消等待
            _cts.Cancel();

            // 等待处理线程完成（最多 5 秒）
            _workerThread.Join(5000);

            // 释放 Sink
            foreach (var sink in _sinks)
            {
                try
                {
                    sink.Dispose();
                }
                catch
                {
                    // 忽略
                }
            }

            _queue.Dispose();
            _cts.Dispose();
        }
    }
}

