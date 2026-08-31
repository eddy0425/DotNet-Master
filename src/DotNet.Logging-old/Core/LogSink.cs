namespace DotNet.Logging
{
    /// <summary>
    /// 日志接收器抽象基类
    /// </summary>
    public abstract class LogSink : ILogSink
    {
        /// <summary>
        /// 启用的日志类别
        /// </summary>
        public LogCategory EnabledCategories { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        protected LogSink()
        {
            EnabledCategories = LogCategory.All;
        }

        /// <summary>
        /// 检查指定的日志类别是否启用
        /// </summary>
        /// <param name="logCategory">要检查的日志类别</param>
        /// <returns>如果启用则返回true，否则返回false</returns>
        public bool IsCategoryEnabled(LogCategory logCategory)
        {
            return (logCategory & EnabledCategories) != 0;
        }

        /// <summary>
        /// 具体的写入日志操作，由子类实现
        /// </summary>
        /// <param name="message">要写入的日志消息</param>
        /// <returns>写入是否成功</returns>
        protected abstract bool DoWrite(LogMessage message);

        /// <summary>
        /// 写入单条日志消息
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <returns>是否写入成功</returns>
        public virtual bool Write(LogMessage message)
        {
            if (message == null)
            {
                return true;
            }
            if (IsCategoryEnabled(message.Category))
            {
                return DoWrite(message);
            }
            return false;
        }

        /// <summary>
        /// 批量写入日志消息
        /// </summary>
        /// <param name="messages">日志消息数组</param>
        /// <returns>成功写入的消息数量</returns>
        public virtual int Write(LogMessage[] messages)
        {
            if (messages == null || messages.Length == 0)
            {
                return 0;
            }
            int count = 0;
            for (int i = 0; i < messages.Length; i++)
            {
                if (Write(messages[i]))
                {
                    count++;
                }
            }
            return count;
        }
    }
} 