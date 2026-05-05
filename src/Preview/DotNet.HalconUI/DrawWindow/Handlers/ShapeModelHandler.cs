using HalconDotNet;
using DotNet.Drawing;
using System.Windows.Forms;


namespace DotNet.HalconUI
{
    /// <summary>
    /// 设置模型绘图处理器
    /// 用于显示模型匹配结果
    /// </summary>
    public class ShapeModelHandler : IDrawHandler
    {
        HObject HoRect;
        Point2d TopLeft;
        Point2d BottomRight;
        Point2d RegCenter => new Point2d((TopLeft.X + BottomRight.X) / 2, (TopLeft.Y + BottomRight.Y) / 2);

        public ShapeModelHandler()
        {
            HoRect = new HObject(); HOperatorSet.GenEmptyObj(out HoRect); // 创建初始空对象
        }

        public bool NeedReDisp => true;

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
            if (e.Button == MouseButtons.Left)
            {
                if (display.SetUp == SetUpEnum.Step1)
                {
                    // 开始绘制矩形，记录左上角
                    TopLeft = new Point2d(e.X, e.Y);
                    display.SetUp = SetUpEnum.Step2;
                }
                else if (display.SetUp == SetUpEnum.Step3)
                {
                    // 编辑模式：开始移动角点
                    if (display.CycleMove == CycleMoveEnum.Start)
                    {
                        display.CycleMove = CycleMoveEnum.StartMove;
                    }
                    else if (display.CycleMove == CycleMoveEnum.End)
                    {
                        display.CycleMove = CycleMoveEnum.EndMove;
                    }
                    else if (display.CycleMove == CycleMoveEnum.Center)
                    {
                        display.CycleMove = CycleMoveEnum.CenterMove;
                    }
                }
            }
        }

        public void OnMouseUp(DisplayUI display, HMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (display.SetUp == SetUpEnum.Step2)
                {
                    // 完成矩形绘制，记录右下角
                    BottomRight = new Point2d(e.X, e.Y);
                    display.SetUp = SetUpEnum.Step3;
                }
                else if (display.SetUp == SetUpEnum.Step3)
                {
                    // 结束角点移动
                    display.CycleMove = CycleMoveEnum.None;
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (display.SetUp == SetUpEnum.Step3)
                {
                    // 右键确认，进入下一步
                    display.SetUp = SetUpEnum.Step4;
                }
            }
        }

        public void OnMouseWheel(DisplayUI display, HMouseEventArgs e)
        {
            // 滚轮事件仅触发重绘
        }

        public void OnMouseMove(DisplayUI display, HMouseEventArgs e)
        {
            switch (display.SetUp)
            {
                case SetUpEnum.Step1:
                case SetUpEnum.Step2:
                case SetUpEnum.Step3:
                    // 显示模版中心
                    if (display.ShrCenter != null)
                    {
                        display.DispPoint(display.ShrCenter, HColor.OrangeRed, 100);
                    }
                    break;
            }

            switch (display.SetUp)
            {
                case SetUpEnum.Step1:
                    // 显示光标十字
                    display.DispPoint(e.X, e.Y, HColor.OrangeRed);
                    break;

                case SetUpEnum.Step2:
                    // 绘制中：显示两个角点和矩形预览
                    display.DispPoint(TopLeft, HColor.OrangeRed);
                    display.DispPoint(e.X, e.Y, HColor.OrangeRed);
                    dispRectangle(display, TopLeft.X, TopLeft.Y, e.X, e.Y, HColor.Red);
                    break;

                case SetUpEnum.Step3:
                    HandleStep3Move(display, e);
                    break;

                case SetUpEnum.Step4:
                    display.DrawRectangle(display.AlgoName, TopLeft, BottomRight);
                    display.SetUp = SetUpEnum.Step5;
                    break;

                case SetUpEnum.Step5:
                    {
                        // 显示最终结果
                        display.DispPoint(TopLeft, HColor.OrangeRed, 50);
                        display.DispPoint(BottomRight, HColor.OrangeRed, 50);
                        dispRectangle(display, HColor.Blue);

                        display.DispRegion(display.ShrRegion, HColor.Blue);
                        display.DispRegion(display.ShrContour, HColor.Green);

                        if (display.ShrCenter != null)
                        {
                            display.DispPoint(display.ShrCenter, HColor.Yellow);
                        }
                    }
                    break;
            }
        }

        private void HandleStep3Move(DisplayUI display, HMouseEventArgs e)
        {
            // 显示角点
            display.DispPoint(TopLeft, HColor.Orange, 50);
            display.DispPoint(BottomRight, HColor.Orange, 50);
            dispRectangle(display, HColor.Red);

            if (display.CycleMove == CycleMoveEnum.StartMove)
            {
                // 移动左上角
                TopLeft = new Point2d(e.X, e.Y);
                dispRectangle(display, HColor.Red);
            }
            else if (display.CycleMove == CycleMoveEnum.EndMove)
            {
                // 移动右下角
                BottomRight = new Point2d(e.X, e.Y);
                dispRectangle(display, HColor.Red);
            }
            else if (display.CycleMove == CycleMoveEnum.CenterMove)
            {
                // 移动右下角
                var calX = RegCenter.X - e.X;
                var calY = RegCenter.Y - e.Y;
                TopLeft = new Point2d(TopLeft.X - calX, TopLeft.Y - calY);
                BottomRight = new Point2d(BottomRight.X - calX, BottomRight.Y - calY);
                dispRectangle(display, HColor.Red);
            }
            else if (HalconHelper.IsNearPoint(TopLeft.X, TopLeft.Y, e.X, e.Y))
            {
                // 靠近左上角，高亮显示
                display.DispPoint(TopLeft, HColor.Green, 10);
                display.CycleMove = CycleMoveEnum.Start;
            }
            else if (HalconHelper.IsNearPoint(BottomRight.X, BottomRight.Y, e.X, e.Y))
            {
                // 靠近右下角，高亮显示
                display.DispPoint(BottomRight, HColor.Green, 10);
                display.CycleMove = CycleMoveEnum.End;
            }
            else if (HalconHelper.IsNearPoint(RegCenter.X, RegCenter.Y, e.X, e.Y))
            {
                // 靠近右下角，高亮显示
                display.DispPoint(RegCenter, HColor.Green, 10);
                display.CycleMove = CycleMoveEnum.Center;
            }
            else
            {
                display.CycleMove = CycleMoveEnum.None;
            }
        }

        private void dispRectangle(DisplayUI display, string color = "red")
        {
            var X1 = TopLeft.X;
            var Y1 = TopLeft.Y;
            var X2 = BottomRight.X;
            var Y2 = BottomRight.Y;

            dispRectangle(display, X1, Y1, X2, Y2, color);
        }

        private void dispRectangle(DisplayUI display, double X1, double Y1, double X2, double Y2, string color = "red")
        {
            try
            {
                HoRect.Dispose();
                HOperatorSet.GenRectangle1(out HoRect, Y1, X1, Y2, X2);
                display.DispRegion(HoRect, color);

                var p = HalconHelper.Cal2P(X1, Y1, X2, Y2);
                display.DispPoint(p.X, p.Y, HColor.Orange, 100);
            }
            catch { }
        }

    }
}
