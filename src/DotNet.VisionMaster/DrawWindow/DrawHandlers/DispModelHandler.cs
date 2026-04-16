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
            if (display.SetUp == SetUpEnum.None)
            {
                display.Reset();
                display.ReDispImage();
                display.SetUp = SetUpEnum.Step1;
            }
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
            switch (display.SetUp)
            {
                case SetUpEnum.Step1:
                    display.DrawDispModel(display.AlgoName, display);
                    break;
            }
        }
    }
}
