namespace DotNet.Logging
{
    /// <summary>
    /// 日志接收器接口
    /// </summary>
    public interface ILogSink
    {
        /// <summary>
        /// 写入单条日志消息
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <returns>是否写入成功</returns>
        bool Write(LogMessage message);

        /// <summary>
        /// 批量写入日志消息
        /// </summary>
        /// <param name="messages">日志消息数组</param>
        /// <returns>成功写入的消息数量</returns>
        int Write(LogMessage[] messages);
    }
} 