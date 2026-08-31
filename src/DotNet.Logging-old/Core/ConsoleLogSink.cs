using System;

namespace DotNet.Logging
{
    /// <summary>
    /// 控制台日志接收器，将日志输出到控制台
    /// </summary>
    public class ConsoleLogSink : LogSink
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public ConsoleLogSink() : base()
        {
        }

        /// <summary>
        /// 执行实际的日志写入操作
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <returns>是否写入成功</returns>
        protected override bool DoWrite(LogMessage message)
        {
            try
            {
                ConsoleColor originalColor = Console.ForegroundColor;
                SetConsoleColorByLogType(message.Type);
                
                Console.WriteLine($"[{message.UtcTimestampAsString}] [{message.ThreadId}] [{message.Type}] {message.Source}: {message.Message}");
                
                Console.ForegroundColor = originalColor;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 根据日志类型设置控制台颜色
        /// </summary>
        /// <param name="logType">日志类型</param>
        private void SetConsoleColorByLogType(string logType)
        {
            switch (logType)
            {
                case "Error":
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case "Warning":
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case "Information":
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case "Debug":
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
            }
        }
    }
} 