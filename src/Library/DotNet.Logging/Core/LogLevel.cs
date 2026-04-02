namespace DotNet.Logging
{
    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// 追踪级别，最详细的日志
        /// </summary>
        Trace = 0,

        /// <summary>
        /// 调试级别
        /// </summary>
        Debug = 1,

        /// <summary>
        /// 信息级别
        /// </summary>
        Information = 2,

        /// <summary>
        /// 警告级别
        /// </summary>
        Warning = 3,

        /// <summary>
        /// 错误级别
        /// </summary>
        Error = 4,

        /// <summary>
        /// 致命错误级别
        /// </summary>
        Fatal = 5,

        /// <summary>
        /// 关闭日志
        /// </summary>
        None = 6
    }
}

