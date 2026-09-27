using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace DotNet.VisionMaster.Tests
{
    /// <summary>
    /// 把窗体 / 控件显示在屏幕外，并提供消息泵辅助。
    /// </summary>
    /// <remarks>
    /// 必须在 STA 线程上调用（见 <see cref="Sta"/>）。控件只有真正显示出来才会触发 <c>Load</c>
    /// 并建出 HWindowControl 的 HALCON 窗口；放到屏幕外显示，既满足可见性又不打扰桌面。
    /// </remarks>
    internal static class WindowHost
    {
        public static void ShowOffscreen(Form form)
        {
            form.ShowInTaskbar = false;
            form.StartPosition = FormStartPosition.Manual;
            form.WindowState = FormWindowState.Normal;
            form.Location = new Point(-32000, -32000);
            form.Show();
            Application.DoEvents();
        }

        /// <summary>把若干控件放进一个屏幕外的窗体里显示，调用方负责 Dispose 返回的窗体。</summary>
        public static Form ShowOffscreen(params Control[] controls)
        {
            var form = new Form { ClientSize = new Size(800, 600), FormBorderStyle = FormBorderStyle.None };
            foreach (var control in controls)
                form.Controls.Add(control);
            ShowOffscreen(form);
            return form;
        }

        /// <summary>
        /// 泵消息直到 <paramref name="done"/> 成立。
        /// </summary>
        /// <remarks>
        /// 窗体一建出来 WinForms 就在当前线程装上了 SynchronizationContext，
        /// <c>async void</c> handler 里 await 之后的续体是 Post 回消息队列的，不泵就永远不会跑。
        /// </remarks>
        public static void PumpUntil(Func<bool> done, int timeoutMs = 5000)
        {
            var watch = Stopwatch.StartNew();
            while (!done())
            {
                if (watch.ElapsedMilliseconds > timeoutMs)
                    throw new TimeoutException("等待条件成立超时.");
                Application.DoEvents();
                Thread.Sleep(1);
            }
            Application.DoEvents();
        }

        /// <summary>泵几轮消息，让已 Post 的续体跑完。</summary>
        public static void Pump()
        {
            for (int i = 0; i < 5; i++) Application.DoEvents();
        }
    }
}
