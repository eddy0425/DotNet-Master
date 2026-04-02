using DotNet.HWindows;
using HalconDotNet;
using OpenCvSharp;

namespace DotNet.VisionMaster
{
    /// <summary>
    /// 矩形绘图处理器
    /// 用于绘制和编辑矩形区域
    /// </summary>
    public class RectDispHandler : IDrawHandler
    {
        public bool NeedReDispImage => true;

        public void SetUp(DrawContext context)
        {
            if (context.SetUp == SetUpEnum.None)
            {
                Point2d TopLeft = context.HContext.TopLeft;
                Point2d BottomRight = context.HContext.BottomRight;
                Point2d Center = context.HContext.Center;

                // 显示最终结果
                context.DispCross(TopLeft.Y, TopLeft.X, 50, HColor.OrangeRed);
                context.DispCross(BottomRight.Y, BottomRight.X, 50, HColor.OrangeRed);
                context.DispCross(Center.Y, Center.X, 50, HColor.Orange);

                context.DispRegion(context.HContext.HoRect, HColor.Blue);

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
                        Point2d TopLeft = context.HContext.TopLeft;
                        Point2d BottomRight = context.HContext.BottomRight;
                        Point2d Center = context.HContext.Center;

                        // 显示最终结果
                        context.DispCross(TopLeft.Y, TopLeft.X, 50, HColor.OrangeRed);
                        context.DispCross(BottomRight.Y, BottomRight.X, 50, HColor.OrangeRed);
                        context.DispCross(Center.Y, Center.X, 50, HColor.Orange);
          
                        context.DispRegion(context.HContext.HoRect, HColor.Blue);
                    }
                    break;
            }
        }
    }
}
