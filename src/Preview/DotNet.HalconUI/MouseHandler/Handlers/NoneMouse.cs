using DotNet.Drawing;
using HalconDotNet;

namespace DotNet.HalconUI
{

    /// <summary>
    /// 空绘图处理器
    /// 当不需要绘图时使用
    /// </summary>
    public class NoneMouse : IMouseHandler
    {

        public void OnMouseDown(HMouseEventArgs e)
        {
            // 无操作
        }

        public void OnMouseUp(HMouseEventArgs e)
        {
            // 无操作
        }

        public void OnMouseWheel(HMouseEventArgs e)
        {
            // 无操作
        }

        public void OnMouseMove(HMouseEventArgs e)
        {
            // 无操作
        }

    }
}
