using HalconDotNet;
using DotNet.Drawing;
using DotNet.HalconUI;

namespace DotNet.VisionMaster
{
    /// <summary>
    /// 矩形绘图处理器
    /// 用于绘制和编辑矩形区域
    /// </summary>
    public class RectDispHandler : IDrawHandler
    {
        public bool NeedReDispImage => true;
        DrawContext context;
        DisplayForm display => context.display;
        public void SetUp(DrawContext _context)
        {
            context = _context;
            if (context.SetUp == SetUpEnum.None)
            {
                Point2d TopLeft = context.HoRegion.TopLeft;
                Point2d BottomRight = context.HoRegion.BottomRight;
                Point2d Center = context.HoRegion.Center;

                // 显示最终结果
                display.DispPoint(TopLeft, HColor.OrangeRed, 50);
                display.DispPoint(BottomRight, HColor.OrangeRed, 50);
                display.DispPoint(Center, HColor.Orange, 50);

                display.DispRegion(context.HoRegion, HColor.Blue);

                context.SetUp = SetUpEnum.Step1;
            }
        }

        public void OnMouseDown(DrawContext context, HMouseEventArgs e) { }

        public void OnMouseUp(DrawContext context, HMouseEventArgs e) { }

        public void OnMouseWheel(DrawContext context, HMouseEventArgs e) { }

        public void OnMouseMove(DrawContext context, HMouseEventArgs e)
        {
            switch (context.SetUp)
            {
                case SetUpEnum.Step1:
                    {
                        Point2d TopLeft = context.HoRegion.TopLeft;
                        Point2d BottomRight = context.HoRegion.BottomRight;
                        Point2d Center = context.HoRegion.Center;

                        // 显示最终结果
                        display.DispPoint(TopLeft, HColor.OrangeRed, 50);
                        display.DispPoint(BottomRight, HColor.OrangeRed, 50);
                        display.DispPoint(Center, HColor.Orange, 50);

                        display.DispRegion(context.HoRegion, HColor.Blue);
                    }
                    break;
            }
        }
    }
}
