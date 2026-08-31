using System;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Globalization;

namespace DotNet.Logging
{
    /// <summary>
    /// 日志消息类，包含日志记录所需的所有信息
    /// </summary>
    [DataContract]
    public class LogMessage
    {
        private static readonly Stopwatch globalStopwatch = new Stopwatch();
        [DataMember(IsRequired = true)]
        private uint category;

        // Cimetrix LogViewer兼容的列头，完全按照原始格式
        public const string SerializeColumnHeaders = "UtcTimestamp\tMicroseconds\tThreadId\tSourceProcess\tSource\tType\tMessage";

        static LogMessage()
        {
            globalStopwatch.Start();
        }

        /// <summary>
        /// UTC时间戳
        /// </summary>
        [DataMember(IsRequired = true)]
        public long UtcTimestamp { get; set; }

        /// <summary>
        /// UTC时间戳的字符串表示
        /// </summary>
        public string UtcTimestampAsString => FormatUtcTimestamp(UtcTimestamp);

        /// <summary>
        /// 微秒级时间戳
        /// </summary>
        [DataMember(IsRequired = true)]
        public long Microseconds { get; set; }

        /// <summary>
        /// 相对时间（毫秒）- 用于Cimetrix兼容性
        /// </summary>
        public double RelativeTimeMsec { get; set; }

        /// <summary>
        /// 日志类型
        /// </summary>
        [DataMember(IsRequired = true)]
        public string Type { get; set; }

        /// <summary>
        /// 日志类别
        /// </summary>
        public LogCategory Category
        {
            get
            {
                return (LogCategory)category;
            }
            set
            {
                category = (uint)value;
                
                // 根据LogCategory设置Cimetrix兼容的Type值
                switch (value)
                {
                    case LogCategory.Debug:
                        Type = "Debug";
                        break;
                    case LogCategory.Information:
                        Type = "Information";
                        break;
                    case LogCategory.Warning:
                        Type = "Warning";
                        break;
                    case LogCategory.Error:
                        Type = "Error";
                        break;
                    case LogCategory.Exception:
                        Type = "Exception";
                        break;
                    case LogCategory.EntryExit:
                        Type = "EntryExit";
                        break;
                    case LogCategory.Callback:
                        Type = "Callback";
                        break;
                    case LogCategory.Protocol:
                        Type = "Protocol";
                        break;
                    default:
                        Type = "Information";
                        break;
                }
            }
        }

        /// <summary>
        /// 源进程
        /// </summary>
        [DataMember(IsRequired = true)]
        public string SourceProcess { get; set; }

        /// <summary>
        /// 源（类名/模块名）
        /// </summary>
        [DataMember(IsRequired = true)]
        public string Source { get; set; }

        /// <summary>
        /// 日志消息内容
        /// </summary>
        [DataMember(IsRequired = true)]
        public string Message { get; set; }

        /// <summary>
        /// 线程ID
        /// </summary>
        [DataMember(IsRequired = true)]
        public string ThreadId { get; set; }

        /// <summary>
        /// 创建一个当前LogMessage对象的深度副本
        /// </summary>
        /// <returns>日志消息的副本</returns>
        public LogMessage Clone()
        {
            var clone = new LogMessage
            {
                UtcTimestamp = this.UtcTimestamp,
                Microseconds = this.Microseconds,
                RelativeTimeMsec = this.RelativeTimeMsec,
                Type = this.Type,
                category = this.category,
                SourceProcess = this.SourceProcess,
                Source = this.Source,
                Message = this.Message,
                ThreadId = this.ThreadId
            };
            return clone;
        }

        /// <summary>
        /// 序列化日志消息为制表符分隔的字符串，严格兼容Cimetrix原始格式
        /// </summary>
        public string Serialize()
        {
            // 完全匹配Cimetrix原始格式：
            // UtcTimestamp\tMicroseconds\tThreadId\tSourceProcess\tSource\tType\tMessage
            return $"{FormatUtcTimestamp(UtcTimestamp)}\t{Microseconds}\t{ThreadId}\t{SourceProcess}\t{Source}\t{Type}\t{Message}";
        }

        /// <summary>
        /// 尝试从字符串解析日志消息
        /// </summary>
        public bool TryParse(string s)
        {
            bool result = false;
            string[] array = s.Split(new char[1] { '\t' });
            if (array.Length == 7)
            {
                try
                {
                    UtcTimestamp = ParseDateTime(array[0]);
                    Microseconds = long.Parse(array[1], CultureInfo.InvariantCulture);
                    ThreadId = array[2];
                    SourceProcess = array[3];
                    Source = array[4];
                    Type = array[5];
                    Message = array[6];
                    result = true;
                }
                catch
                {
                    // 解析失败
                }
            }
            return result;
        }

        /// <summary>
        /// 将长整型时间戳转换为Cimetrix格式的ISO 8601日期时间字符串
        /// </summary>
        private string FormatUtcTimestamp(long timestamp)
        {
            // 将Binary时间戳转换为DateTime
            DateTime dt = DateTime.FromBinary(timestamp);
            
            // 获取当地时区时间和偏移量
            TimeZoneInfo localZone = TimeZoneInfo.Local;
            TimeSpan offset = localZone.GetUtcOffset(dt);
            
            // 格式化为Cimetrix格式: yyyy-MM-ddTHH:mm:ss.fff+HH:mm
            string sign = offset.TotalHours >= 0 ? "+" : "-";
            return string.Format(
                "{0:yyyy-MM-dd}T{0:HH:mm:ss.fff}{1}{2:00}:{3:00}",
                dt,
                sign,
                Math.Abs((int)offset.TotalHours),
                Math.Abs(offset.Minutes)
            );
        }

        /// <summary>
        /// 将日期时间字符串解析为长整型时间戳
        /// </summary>
        private long ParseDateTime(string timestampString)
        {
            return DateTime.Parse(timestampString).ToBinary();
        }

        /// <summary>
        /// 创建一个新的LogMessage实例
        /// </summary>
        public LogMessage()
        {
            UtcTimestamp = DateTime.Now.ToBinary(); // 使用本地时间而非UTC
            Microseconds = globalStopwatch.ElapsedTicks * 1000000 / Stopwatch.Frequency; // 微秒计数
            ThreadId = "0x" + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString("X4"); // 格式化为0x0001形式
            SourceProcess = Process.GetCurrentProcess().ProcessName + "."; // 添加末尾的点
        }
    }
} 