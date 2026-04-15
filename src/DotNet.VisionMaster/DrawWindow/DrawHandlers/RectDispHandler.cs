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
        public bool NeedReDisp => true;

        public void SetUp(DisplayUI display)
        {
            if (display.SetUp == SetUpEnum.None)
            {
                Point2d TopLeft = display.ShrRegion.TopLeft;
                Point2d BottomRight = display.ShrRegion.BottomRight;
                Point2d Center = display.ShrRegion.Center;

                // 显示最终结果
                display.DispPoint(TopLeft, HColor.OrangeRed, 50);
                display.DispPoint(BottomRight, HColor.OrangeRed, 50);
                display.DispPoint(Center, HColor.Orange, 50);

                display.DispRegion(display.ShrRegion, HColor.Blue);

                display.SetUp = SetUpEnum.Step1;
            }
        }

        public void OnMouseDown(DisplayUI display, HMouseEventArgs e) { }

        public void OnMouseUp(DisplayUI display, HMouseEventArgs e) { }

        public void OnMouseWheel(DisplayUI display, HMouseEventArgs e) { }

        public void OnMouseMove(DisplayUI display, HMouseEventArgs e)
        {
            switch (display.SetUp)
            {
                case SetUpEnum.Step1:
                    {
                        Point2d TopLeft = display.ShrRegion.TopLeft;
                        Point2d BottomRight = display.ShrRegion.BottomRight;
                        Point2d Center = display.ShrRegion.Center;

                        // 显示最终结果
                        display.DispPoint(TopLeft, HColor.OrangeRed, 50);
                        display.DispPoint(BottomRight, HColor.OrangeRed, 50);
                        display.DispPoint(Center, HColor.Orange, 50);

                        display.DispRegion(display.ShrRegion, HColor.Blue);
                    }
                    break;
            }
        }
    }
}
