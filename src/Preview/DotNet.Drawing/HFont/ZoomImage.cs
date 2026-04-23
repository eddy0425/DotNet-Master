using System.Windows.Forms;
using HalconDotNet;

namespace DotNet.Drawing
{
    /// <summary>
    /// 图像缩放信息载体
    /// </summary>
    public class ZoomImage
    {
        /// <summary>
        /// 图像宽度（默认 1248）
        /// </summary>
        public HTuple width = 1248;

        /// <summary>
        /// 图像高度（默认 2200）
        /// </summary>
        public HTuple height = 2200;

        /// <summary>
        /// 父容器引用（用于尺寸计算）
        /// </summary>
        public Control parent = new Control();
    }
}
