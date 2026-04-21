using HalconDotNet;

namespace DotNet.HalconAlgo
{
    /// <summary>
    /// 空绘图处理器
    /// 当不需要绘图时使用
    /// </summary>
    public class NoneHandler : IDrawHandler
    {
        public bool NeedReDisp => false;

        public void SetUp(DisplayUI display)
        {
            // 无操作
        }

        public void OnMouseDown(DisplayUI display, HMouseEventArgs e)
        {
            // 无操作
        }

        public void OnMouseUp(DisplayUI display, HMouseEventArgs e)
        {
            // 无操作
        }

        public void OnMouseWheel(DisplayUI display, HMouseEventArgs e)
        {
            // 无操作
        }

        public void OnMouseMove(DisplayUI display, HMouseEventArgs e)
        {
            // 无操作
        }

    }
}
