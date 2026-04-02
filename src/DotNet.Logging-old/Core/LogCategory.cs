using System;

namespace DotNet.Logging
{
    /// <summary>
    /// 定义日志类别，可以组合使用
    /// </summary>
    [Flags]
    public enum LogCategory : uint
    {
        None = 0u,
        Protocol = 1u,
        Messaging = 2u,
        ChangeRecord = 4u,
        Error = 8u,
        Warning = 0x10u,
        Debug = 0x20u,
        EntryExit = 0x80u,
        Callback = 0x100u,
        History = 0x200u,
        OperatorAttention = 0x400u,
        Information = 0x8000u,
        Argument = 0x10000u,
        Exception = 0x20000u,
        All = uint.MaxValue,
        Custom = 0xFF000000u
    }
} 