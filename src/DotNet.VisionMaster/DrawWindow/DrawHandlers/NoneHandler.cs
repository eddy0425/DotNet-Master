using HalconDotNet;

namespace DotNet.VisionMaster
{
    /// <summary>
    /// 空绘图处理器
    /// 当不需要绘图时使用
    /// </summary>
    public class NoneHandler : IDrawHandler
    {
        public bool NeedReDispImage => false;

        public void SetUp(DrawContext context)
        {
            // 无操作
        }

        public void OnMouseDown(DrawContext context, HMouseEventArgs e)
        {
            // 无操作
        }

        public void OnMouseUp(DrawContext context, HMouseEventArgs e)
        {
            // 无操作
        }

        public void OnMouseWheel(DrawContext context, HMouseEventArgs e)
        {
            // 无操作
        }

        public void OnMouseMove(DrawContext context, HMouseEventArgs e)
        {
            // 无操作
        }

    }
}
