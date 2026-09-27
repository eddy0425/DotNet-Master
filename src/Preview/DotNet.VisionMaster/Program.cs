using System;
using System.IO;
using System.Windows.Forms;
using DotNet.Logging;

namespace DotNet.VisionMaster
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            // LogFile 把 JsonLog 转到 Log; 不初始化的话 Log 内部 Logger 为 null, 日志全部被静默丢弃。
            Log.Initialize(b => b.WriteToFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"), "VisionMaster"));
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            finally
            {
                // 排空异步队列里还没写盘的日志
                Log.Shutdown();
            }
        }
    }
}
