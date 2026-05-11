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
            DrawHelper.Active?.OnMouseDown(e);
        }

        public void OnMouseUp(HMouseEventArgs e)
        {
            DrawHelper.Active?.OnMouseUp(e);
        }

        public void OnMouseWheel(HMouseEventArgs e)
        {
            DrawHelper.Active?.OnMouseWheel(e);
        }

        public void OnMouseMove(HMouseEventArgs e)
        {
            DrawHelper.Active?.OnMouseMove(e);
        }

    }
}
