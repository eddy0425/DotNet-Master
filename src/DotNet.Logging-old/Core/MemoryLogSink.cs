using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DotNet.Logging
{
    /// <summary>
    /// 内存日志接收器，将日志保存在内存中
    /// </summary>
    public class MemoryLogSink : LogSink
    {
        private readonly List<LogMessage> messages;
        private readonly int maxMessages;
        private readonly object lockObject = new object();
        
        /// <summary>
        /// 当新消息被添加到内存中时发生
        /// </summary>
        public event EventHandler<LogMessage> NewMessageAdded;
        
        /// <summary>
        /// 获取当前保存的日志消息数量
        /// </summary>
        public int Count
        {
            get
            {
                lock (lockObject)
                {
                    return messages?.Count ?? 0;
                }
            }
        }
        
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="maxMessages">保存在内存中的最大消息数量，0表示不限制</param>
        public MemoryLogSink(int maxMessages = 1000)
        {
            this.maxMessages = maxMessages > 0 ? maxMessages : 1000; // 确保最大消息数有有效值
            this.messages = new List<LogMessage>(Math.Min(100, this.maxMessages)); // 预分配合理的初始容量
        }
        
        /// <summary>
        /// 执行实际的日志写入操作
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <returns>是否写入成功</returns>
        protected override bool DoWrite(LogMessage message)
        {
            if (message == null)
            {
                return false;
            }
            
            try
            {
                lock (lockObject)
                {
                    // 如果达到最大消息数量且限制大于0，则移除最旧的消息
                    if (maxMessages > 0 && messages.Count >= maxMessages)
                    {
                        messages.RemoveAt(0);
                    }
                    
                    // 存储消息副本而不是原始引用，避免外部修改
                    messages.Add(message.Clone());
                }
                
                // 触发新消息添加事件
                OnNewMessageAdded(message);
                
                return true;
            }
            catch (Exception ex)
            {
                // 处理异常，确保日志系统不会因为内存日志错误而中断
                HandleException("写入内存日志失败", ex);
                return false;
            }
        }
        
        /// <summary>
        /// 清空内存中的所有日志消息
        /// </summary>
        public void Clear()
        {
            try
            {
                lock (lockObject)
                {
                    messages.Clear();
                }
            }
            catch (Exception ex)
            {
                HandleException("清空内存日志失败", ex);
            }
        }
        
        /// <summary>
        /// 获取当前内存中的所有日志消息的副本
        /// </summary>
        /// <returns>日志消息数组</returns>
        public LogMessage[] GetMessages()
        {
            try
            {
                lock (lockObject)
                {
                    if (messages.Count == 0)
                    {
                        return new LogMessage[0];
                    }
                    
                    // 创建每个消息的深度副本
                    LogMessage[] result = new LogMessage[messages.Count];
                    for (int i = 0; i < messages.Count; i++)
                    {
                        result[i] = messages[i].Clone();
                    }
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                HandleException("获取内存日志失败", ex);
                return new LogMessage[0]; // 出错时返回空数组而非null
            }
        }
        
        /// <summary>
        /// 获取指定范围内的日志消息
        /// </summary>
        /// <param name="startIndex">起始索引</param>
        /// <param name="count">获取的消息数量</param>
        /// <returns>日志消息数组</returns>
        public LogMessage[] GetMessages(int startIndex, int count)
        {
            if (startIndex < 0 || count <= 0)
            {
                return new LogMessage[0];
            }
            
            try
            {
                lock (lockObject)
                {
                    if (messages.Count == 0 || startIndex >= messages.Count)
                    {
                        return new LogMessage[0];
                    }
                    
                    int realCount = Math.Min(count, messages.Count - startIndex);
                    LogMessage[] result = new LogMessage[realCount];
                    
                    for (int i = 0; i < realCount; i++)
                    {
                        result[i] = messages[startIndex + i].Clone();
                    }
                    
                    return result;
                }
            }
            catch (Exception ex)
            {
                HandleException("获取范围内存日志失败", ex);
                return new LogMessage[0];
            }
        }
        
        /// <summary>
        /// 触发新消息添加事件
        /// </summary>
        /// <param name="message">添加的日志消息</param>
        protected virtual void OnNewMessageAdded(LogMessage message)
        {
            if (message == null) return;
            
            try
            {
                var handler = NewMessageAdded;
                if (handler != null)
                {
                    // 创建消息副本，避免事件处理过程中修改原始消息
                    handler(this, message.Clone());
                }
            }
            catch (Exception ex)
            {
                HandleException("触发日志事件失败", ex);
            }
        }
        
        /// <summary>
        /// 处理内部异常
        /// </summary>
        /// <param name="message">错误消息</param>
        /// <param name="ex">异常对象</param>
        private void HandleException(string message, Exception ex)
        {
            try
            {
                // 尝试记录到主日志系统
                Log.WriteIfEnabled(LogCategory.Error, "MemoryLogSink",
                    $"{message}: {ex.GetType().Name} - {ex.Message}");
            }
            catch
            {
                // 如果连日志系统也出错，输出到调试（仅 Debug 模式）
                Debug.WriteLine($"[MemoryLogSink Error] {message}: {ex.Message}");
            }
        }
    }
} 