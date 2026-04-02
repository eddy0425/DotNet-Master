using System;

namespace DotNet.Library.Extension
{
    /// <summary>
    /// 实现 IDisposable 模式的基类，用于派生类清理托管与非托管资源。
    /// </summary>
    public abstract class DisposableBase : IDisposable
    {
        /// <summary>
        /// 标志是否已释放资源
        /// </summary>
        protected bool _disposed = false;

        /// <summary>
        /// 清理托管资源的方法。
        /// 派生类可以重写此方法以释放托管资源。
        /// </summary>
        protected virtual void ManagedResourcesDispose() { }

        /// <summary>
        /// 清理非托管资源的方法。
        /// 派生类可以重写此方法以释放非托管资源。
        /// </summary>
        protected virtual void UnmanagedResourcesDispose() { }

        /// <summary>
        /// 实现 Dispose 模式，用于清理托管和非托管资源。
        /// </summary>
        /// <param name="disposing">true 表示清理托管资源，同时清理非托管资源；false 表示仅清理非托管资源</param>
        protected void Dispose(bool disposing)
        {
            if (!_disposed) // 确保仅释放一次
            {
                if (disposing)
                {
                    // 清理托管资源
                    ManagedResourcesDispose();
                }

                // 清理非托管资源
                UnmanagedResourcesDispose();

                _disposed = true; // 标记为已释放
            }
        }

        /// <summary>
        /// 释放资源。
        /// 调用此方法将清理资源，并阻止终结器被调用。
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            // 通知垃圾回收器不再调用终结器
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 析构函数，用于在未显式调用 Dispose 的情况下清理非托管资源。
        /// </summary>
        ~DisposableBase()
        {
            Dispose(false);
        }
    }
}