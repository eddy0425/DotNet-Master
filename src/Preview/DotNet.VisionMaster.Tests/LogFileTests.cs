using System;
using System.Collections.Generic;
using DotNet.Json;
using DotNet.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNet.VisionMaster.Tests
{
    /// <summary>
    /// <see cref="LogFile"/>：把 <see cref="JsonLog"/> 的日志事件转接出去。
    /// </summary>
    [TestClass]
    public class LogFileTests
    {
        [TestInitialize]
        public void Init() => Priv.ResetJsonLog();

        [TestCleanup]
        public void Cleanup()
        {
            Priv.ResetJsonLog();
            Log.Shutdown();
        }

        private sealed class ReentryDetected : Exception { }

        /// <summary>收集写入的日志条目；批次列表写完会被处理器清空复用，这里逐条拷走。</summary>
        private sealed class CollectingSink : ILogSink
        {
            public readonly List<LogEntry> Entries = new List<LogEntry>();

            public void Write(LogEntry entry) { lock (Entries) Entries.Add(entry); }
            public void WriteBatch(IReadOnlyList<LogEntry> entries) { lock (Entries) Entries.AddRange(entries); }
            public void Flush() { }
            public void Dispose() { }
        }

        [TestMethod]
        public void Constructor_SubscribesToJsonLog()
        {
            new LogFile();

            Assert.AreEqual(1, Priv.JsonLogSubscriberCount());
        }

        /// <summary>
        /// 转接 handler 不得再次触发 <see cref="JsonLog.Logged"/>。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 若 handler 里又调 <c>JsonLog.Debug/Info/...</c>，事件会再次派发给自己，无限递归直到 StackOverflow ——
        /// 进程直接崩溃，catch 不住。<c>JsonCore</c> 读写失败（例如配置文件不存在）时都会打日志，
        /// 所以只要 <c>MainForm</c> 建过 <c>LogFile</c>，任何一次 JSON 读写失败都会让程序闪退。
        /// </para>
        /// <para>
        /// 为了不把测试宿主一起带崩：先挂一个哨兵 handler（多播委托按订阅顺序调用，它总在 LogFile 前面），
        /// 发现重入就抛异常把整条调用链打断。
        /// </para>
        /// </remarks>
        [DataTestMethod]
        [DataRow(JsonLogLevel.Debug)]
        [DataRow(JsonLogLevel.Information)]
        [DataRow(JsonLogLevel.Warning)]
        [DataRow(JsonLogLevel.Error)]
        [DataRow(JsonLogLevel.Exception)]
        public void Logged_DoesNotReenterJsonLog(JsonLogLevel level)
        {
            int depth = 0, maxDepth = 0;
            JsonLog.Logged += args =>
            {
                maxDepth = Math.Max(maxDepth, ++depth);
                if (depth > 1) throw new ReentryDetected();
            };
            new LogFile();

            try
            {
                switch (level)
                {
                    case JsonLogLevel.Debug: JsonLog.Debug("tag", "msg"); break;
                    case JsonLogLevel.Information: JsonLog.Info("tag", "msg"); break;
                    case JsonLogLevel.Warning: JsonLog.Warning("tag", "msg"); break;
                    case JsonLogLevel.Error: JsonLog.Error("tag", "msg"); break;
                    case JsonLogLevel.Exception: JsonLog.Exception("tag", new InvalidOperationException(), "msg"); break;
                }
            }
            catch (ReentryDetected) { }

            Assert.AreEqual(1, maxDepth, "LogFile 的 handler 又触发了 JsonLog.Logged，真实运行时会无限递归导致 StackOverflow");
        }

        /// <summary>
        /// 每个 <see cref="JsonLogLevel"/> 都应原样（级别、Tag、消息、异常）转到 <see cref="Log"/>。
        /// </summary>
        /// <remarks><see cref="Log.Shutdown"/> 会等处理线程排空队列，之后再断言，不需要等待。</remarks>
        [DataTestMethod]
        [DataRow(JsonLogLevel.Debug, LogLevel.Debug)]
        [DataRow(JsonLogLevel.Information, LogLevel.Information)]
        [DataRow(JsonLogLevel.Warning, LogLevel.Warning)]
        [DataRow(JsonLogLevel.Error, LogLevel.Error)]
        [DataRow(JsonLogLevel.Exception, LogLevel.Error)]
        public void Logged_ForwardsToLog(JsonLogLevel level, LogLevel expected)
        {
            var sink = new CollectingSink();
            Log.Initialize(b => b.MinimumLevel(LogLevel.Trace).WriteTo(sink));
            new LogFile();
            var ex = new InvalidOperationException();

            switch (level)
            {
                case JsonLogLevel.Debug: JsonLog.Debug("tag", "msg"); break;
                case JsonLogLevel.Information: JsonLog.Info("tag", "msg"); break;
                case JsonLogLevel.Warning: JsonLog.Warning("tag", "msg"); break;
                case JsonLogLevel.Error: JsonLog.Error("tag", "msg"); break;
                case JsonLogLevel.Exception: JsonLog.Exception("tag", ex, "msg"); break;
            }
            Log.Shutdown();

            Assert.AreEqual(1, sink.Entries.Count);
            var entry = sink.Entries[0];
            Assert.AreEqual(expected, entry.Level);
            Assert.AreEqual("tag", entry.Source);
            Assert.AreEqual("msg", entry.Message);
            Assert.AreSame(level == JsonLogLevel.Exception ? ex : null, entry.Exception);
        }
    }
}
