using HalconDotNet;


namespace DotNet.VisionMaster
{
    /// <summary>
    /// 设置模型绘图处理器
    /// 用于显示模型匹配结果
    /// </summary>
    public class DispModelHandler : IDrawHandler
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
            display.DrawDispModel(display.AlgoName, display);
        }
    }
}
