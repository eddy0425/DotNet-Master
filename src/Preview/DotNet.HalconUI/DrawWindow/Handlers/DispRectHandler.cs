using DotNet.Drawing;
using HalconDotNet;


namespace DotNet.HalconUI
{
    /// <summary>
    /// 矩形绘图处理器
    /// 用于绘制和编辑矩形区域
    /// </summary>
    public class DispRectHandler : IDrawHandler
    {
        public bool NeedReDisp => true;
        private enum SetUpEnum { None, Step1, Step2, Step3, Step4, Step5 }
        private SetUpEnum _phase;
        public void SetUp(DisplayUI display)
        {
            _phase = SetUpEnum.None;
            if (_phase == SetUpEnum.None)
            {
                Point2d TopLeft = display.ShrRegion.TopLeft;
                Point2d BottomRight = display.ShrRegion.BottomRight;
                Point2d Center = display.ShrRegion.Center;

                // 显示最终结果
                display.DispPoint(TopLeft, HColor.OrangeRed, 50);
                display.DispPoint(BottomRight, HColor.OrangeRed, 50);
                display.DispPoint(Center, HColor.Orange, 50);

                display.DispRegion(display.ShrRegion, HColor.Blue);

                _phase = SetUpEnum.Step1;
            }
        }

        public void OnMouseDown(DisplayUI display, HMouseEventArgs e) { }

        public void OnMouseUp(DisplayUI display, HMouseEventArgs e) { }

        public void OnMouseWheel(DisplayUI display, HMouseEventArgs e) { }

        public void OnMouseMove(DisplayUI display, HMouseEventArgs e)
        {
            switch (_phase)
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
