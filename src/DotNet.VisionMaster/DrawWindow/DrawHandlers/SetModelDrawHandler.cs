using DotNet.Drawing;
using HalconDotNet;

namespace DotNet.VisionMaster
{
    /// <summary>
    /// 设置模型绘图处理器
    /// 用于显示模型匹配结果
    /// </summary>
    public class SetModelDrawHandler : IDrawHandler
    {
        public bool NeedReDisp => true;
        public void SetUp(DisplayUI display)
        {
            if (display.SetUp == SetUpEnum.None)
            {
                OnReDisplay(display);
                display.SetUp = SetUpEnum.Step1;
            }
        }

        public void OnMouseDown(DisplayUI display, HMouseEventArgs e)
        {
            // 设置模型模式不处理鼠标按下
        }

        public void OnMouseUp(DisplayUI display, HMouseEventArgs e)
        {
            // 设置模型模式不处理鼠标释放
        }

        public void OnMouseWheel(DisplayUI display, HMouseEventArgs e)
        {
            // 滚轮事件仅触发重绘
        }

        public void OnMouseMove(DisplayUI display, HMouseEventArgs e)
        {
            OnReDisplay(display);
        }

        public void OnReDisplay(DisplayUI display)
        {
            // 显示模型相关区域
            display.DispRegion(display.ShrRegion, HColor.Blue);
            display.DispRegion(display.ShrContour, HColor.Green);

            if (display.ShrCenter != null)
            {
                display.DispPoint(display.ShrCenter, HColor.Yellow);
            }
        }

    }
}
