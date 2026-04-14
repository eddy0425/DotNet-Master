using DotNet.Drawing;
using DotNet.HalconUI;
using HalconDotNet;

namespace DotNet.VisionMaster
{
    /// <summary>
    /// 设置模型绘图处理器
    /// 用于显示模型匹配结果
    /// </summary>
    public class SetModelDrawHandler : IDrawHandler
    {
        public bool NeedReDispImage => true;
        DrawContext context;
        DisplayForm display => context.display;
        public void SetUp(DrawContext _context)
        {
            context = _context;
            if (context.SetUp == SetUpEnum.None)
            {
                OnReDisplay(context);
                context.SetUp = SetUpEnum.Step1;
            }
        }

        public void OnMouseDown(DrawContext context, HMouseEventArgs e)
        {
            // 设置模型模式不处理鼠标按下
        }

        public void OnMouseUp(DrawContext context, HMouseEventArgs e)
        {
            // 设置模型模式不处理鼠标释放
        }

        public void OnMouseWheel(DrawContext context, HMouseEventArgs e)
        {
            // 滚轮事件仅触发重绘
        }

        public void OnMouseMove(DrawContext context, HMouseEventArgs e)
        {
            OnReDisplay(context);
        }

        public void OnReDisplay(DrawContext context)
        {
            // 显示模型相关区域
            display.DispRegion(context.HoRegion, HColor.Blue);
            display.DispRegion(context.HoContour, HColor.Green);

            if (context.Center != null)
            {
                display.DispPoint(context.Center, HColor.Yellow);
            }
        }

    }
}
