using System;
using System.Collections.Generic;

namespace DotNet.Logging.Sinks
{
    /// <summary>
    /// 控制台日志 Sink - 支持彩色输出
    /// </summary>
    public sealed class ConsoleLogSink : ILogSink
    {
        private readonly object _lock = new object();
        private readonly bool _useColors;
        private bool _disposed;

        public ConsoleLogSink(bool useColors = true)
        {
            _useColors = useColors && !Console.IsOutputRedirected;
        }

        public void Write(LogEntry entry)
        {
            if (_disposed || entry == null)
                return;

            lock (_lock)
            {
                WriteColored(entry);
            }
        }

        public void WriteBatch(IReadOnlyList<LogEntry> entries)
        {
            if (_disposed || entries == null || entries.Count == 0)
                return;

            lock (_lock)
            {
                foreach (var entry in entries)
                {
                    if (entry != null)
                        WriteColored(entry);
                }
            }
        }

        private void WriteColored(LogEntry entry)
        {
            if (_useColors)
            {
                ConsoleColor fg, bg;
                GetColors(entry.Level, out fg, out bg);
                var originalFg = Console.ForegroundColor;
                var originalBg = Console.BackgroundColor;

                try
                {
                    // 时间戳 - 灰色
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write('[');
                    Console.Write(entry.Timestamp.ToString("HH:mm:ss.fff"));
                    Console.Write("] ");

                    // 级别标签 - 带颜色
                    Console.ForegroundColor = fg;
                    if (bg != ConsoleColor.Black)
                        Console.BackgroundColor = bg;
                    Console.Write('[');
                    Console.Write(entry.LevelTag);
                    Console.Write(']');
                    Console.BackgroundColor = originalBg;
                    Console.Write(' ');

                    // 源 - 青色
                    Console.ForegroundColor = ConsoleColor.DarkCyan;
                    Console.Write('[');
                    Console.Write(entry.Source);
                    Console.Write("] ");

                    // 消息 - 根据级别
                    Console.ForegroundColor = entry.Level >= LogLevel.Error ? fg : ConsoleColor.Gray;
                    Console.WriteLine(entry.Message);

                    // 异常 - 红色
                    if (entry.Exception != null)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        WriteException(entry.Exception, 0);
                    }
                }
                finally
                {
                    Console.ForegroundColor = originalFg;
                    Console.BackgroundColor = originalBg;
                }
            }
            else
            {
                Console.WriteLine(entry.Format());
            }
        }

        private void WriteException(Exception ex, int depth)
        {
            var indent = new string(' ', depth * 2 + 2);
            Console.Write(indent);
            Console.Write("→ ");
            Console.Write(ex.GetType().Name);
            Console.Write(": ");
            Console.WriteLine(ex.Message);

            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                var lines = ex.StackTrace.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    Console.Write(indent);
                    Console.Write("  ");
                    Console.WriteLine(line.Trim());
                }
            }

            if (ex.InnerException != null)
            {
                WriteException(ex.InnerException, depth + 1);
            }
        }

        private static void GetColors(LogLevel level, out ConsoleColor fg, out ConsoleColor bg)
        {
            bg = ConsoleColor.Black;
            switch (level)
            {
                case LogLevel.Trace:
                    fg = ConsoleColor.DarkGray;
                    break;
                case LogLevel.Debug:
                    fg = ConsoleColor.Gray;
                    break;
                case LogLevel.Information:
                    fg = ConsoleColor.Green;
                    break;
                case LogLevel.Warning:
                    fg = ConsoleColor.Yellow;
                    break;
                case LogLevel.Error:
                    fg = ConsoleColor.Red;
                    break;
                case LogLevel.Fatal:
                    fg = ConsoleColor.White;
                    bg = ConsoleColor.DarkRed;
                    break;
                default:
                    fg = ConsoleColor.Gray;
                    break;
            }
        }

        public void Flush()
        {
            // 控制台无需显式刷新
        }

        public void Dispose()
        {
            _disposed = true;
        }
    }
}
