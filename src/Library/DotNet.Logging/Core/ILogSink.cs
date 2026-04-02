using System;
using System.Collections.Generic;

namespace DotNet.Logging
{
    /// <summary>
    /// 日志输出接口
    /// </summary>
    public interface ILogSink : IDisposable
    {
        /// <summary>
        /// 写入单条日志
        /// </summary>
        void Write(LogEntry entry);

        /// <summary>
        /// 批量写入日志
        /// </summary>
        void WriteBatch(IReadOnlyList<LogEntry> entries);

        /// <summary>
        /// 刷新缓冲区
        /// </summary>
        void Flush();
    }
}
