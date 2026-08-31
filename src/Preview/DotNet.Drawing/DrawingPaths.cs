using System;
using System.IO;

namespace DotNet.Drawing
{
    /// <summary>
    /// 图像落盘目录的集中配置。
    /// </summary>
    /// <remarks>
    /// 替代原先散落在 <see cref="HalconController"/> 默认参数里的 <c>D:\Picture\...</c> 硬编码：
    /// 那种写法要求每台部署机器都存在 D 盘且可写，换机即失败，且无法在不改代码的前提下调整。
    /// 默认值改为可执行文件所在目录下的 Picture，宿主程序可在启动时改写 <see cref="RootDir"/>。
    /// </remarks>
    public static class DrawingPaths
    {
        /// <summary> 图像保存根目录，默认 {程序目录}\Picture。唯一允许外部改写的路径。 </summary>
        public static string RootDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Picture");

        /// <summary> 原图保存目录 </summary>
        public static string OriginalImageDir => Path.Combine(RootDir, "SaveOriginalImages");

        /// <summary> 窗口截图保存目录 </summary>
        public static string CropWindowDir => Path.Combine(RootDir, "SaveCropWindow");
    }
}
