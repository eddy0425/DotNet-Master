using System.Drawing;
using HalconDotNet;

namespace DotNet.HalconUI
{
    /// <summary>
    /// 图像缩放信息载体
    /// </summary>
    public struct ZoomImage
    {
        /// <summary>
        /// 图像宽度（默认 1248）
        /// </summary>
        public HTuple width;

        /// <summary>
        /// 图像高度（默认 2200）
        /// </summary>
        public HTuple height;

        /// <summary>
        /// 父容器引用（用于尺寸计算）
        /// </summary>
        public Size parent;

        public ZoomImage()
        {
            width = 1248;
            height = 2200;
            parent = new Size();
        }
    }
}
