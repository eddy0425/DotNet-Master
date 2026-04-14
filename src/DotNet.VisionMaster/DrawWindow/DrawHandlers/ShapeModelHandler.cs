using HalconDotNet;
using DotNet.Drawing;
using DotNet.HalconUI;
using System.Windows.Forms;


namespace DotNet.VisionMaster
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
        DrawContext context;
        DisplayForm display => context.display;
        Point2d RegCenter => new Point2d((TopLeft.X + BottomRight.X) / 2, (TopLeft.Y + BottomRight.Y) / 2);

        public ShapeModelHandler()
        {
            HoRect = new HObject(); HOperatorSet.GenEmptyObj(out HoRect); // 创建初始空对象
        }

        public bool NeedReDispImage => true;

        public void SetUp(DrawContext _context)
        {
            context = _context;
            if (context.SetUp == SetUpEnum.None)
            {
                context.SetUp = SetUpEnum.Step1;
            }
        }

        public void OnMouseDown(DrawContext context, HMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (context.SetUp == SetUpEnum.Step1)
                {
                    // 开始绘制矩形，记录左上角
                    TopLeft = new Point2d(e.X, e.Y);
                    context.SetUp = SetUpEnum.Step2;
                }
                else if (context.SetUp == SetUpEnum.Step3)
                {
                    // 编辑模式：开始移动角点
                    if (context.CycleMove == CycleMoveEnum.Start)
                    {
                        context.CycleMove = CycleMoveEnum.StartMove;
                    }
                    else if (context.CycleMove == CycleMoveEnum.End)
                    {
                        context.CycleMove = CycleMoveEnum.EndMove;
                    }
                    else if (context.CycleMove == CycleMoveEnum.Center)
                    {
                        context.CycleMove = CycleMoveEnum.CenterMove;
                    }
                }
            }
        }

        public void OnMouseUp(DrawContext context, HMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (context.SetUp == SetUpEnum.Step2)
                {
                    // 完成矩形绘制，记录右下角
                    BottomRight = new Point2d(e.X, e.Y);
                    context.SetUp = SetUpEnum.Step3;
                }
                else if (context.SetUp == SetUpEnum.Step3)
                {
                    // 结束角点移动
                    context.CycleMove = CycleMoveEnum.None;
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (context.SetUp == SetUpEnum.Step3)
                {
                    // 右键确认，进入下一步
                    context.SetUp = SetUpEnum.Step4;
                }
            }
        }

        public void OnMouseWheel(DrawContext context, HMouseEventArgs e)
        {
            // 滚轮事件仅触发重绘
        }

        public void OnMouseMove(DrawContext context, HMouseEventArgs e)
        {
            switch (context.SetUp)
            {
                case SetUpEnum.Step1:
                case SetUpEnum.Step2:
                case SetUpEnum.Step3:
                    // 显示模版中心
                    if (context.Center != null)
                    {
                        display.DispPoint(context.Center, HColor.OrangeRed, 100);
                    }
                    break;
            }

            switch (context.SetUp)
            {
                case SetUpEnum.Step1:
                    // 显示光标十字
                    display.DispPoint(e.X, e.Y, HColor.OrangeRed);
                    break;

                case SetUpEnum.Step2:
                    // 绘制中：显示两个角点和矩形预览
                    display.DispPoint(TopLeft, HColor.OrangeRed);
                    display.DispPoint(e.X, e.Y, HColor.OrangeRed);
                    dispRectangle(context, TopLeft.X, TopLeft.Y, e.X, e.Y, HColor.Red);
                    break;

                case SetUpEnum.Step3:
                    HandleStep3Move(context, e);
                    break;

                case SetUpEnum.Step4:
                    context.DrawRectangle(context.Name, TopLeft, BottomRight);
                    context.SetUp = SetUpEnum.Step5;
                    break;

                case SetUpEnum.Step5:
                    {
                        // 显示最终结果
                        display.DispPoint(TopLeft, HColor.OrangeRed, 50);
                        display.DispPoint(BottomRight, HColor.OrangeRed, 50);
                        dispRectangle(context, HColor.Blue);

                        display.DispRegion(context.HoRegion, HColor.Blue);
                        display.DispRegion(context.HoContour, HColor.Green);

                        if (context.Center != null)
                        {
                            display.DispPoint(context.Center, HColor.Yellow);
                        }
                    }
                    break;
            }
        }

        private void HandleStep3Move(DrawContext context, HMouseEventArgs e)
        {
            // 显示角点
            display.DispPoint(TopLeft, HColor.Orange, 50);
            display.DispPoint(BottomRight, HColor.Orange, 50);
            dispRectangle(context, HColor.Red);

            if (context.CycleMove == CycleMoveEnum.StartMove)
            {
                // 移动左上角
                TopLeft = new Point2d(e.X, e.Y);
                dispRectangle(context, HColor.Red);
            }
            else if (context.CycleMove == CycleMoveEnum.EndMove)
            {
                // 移动右下角
                BottomRight = new Point2d(e.X, e.Y);
                dispRectangle(context, HColor.Red);
            }
            else if (context.CycleMove == CycleMoveEnum.CenterMove)
            {
                // 移动右下角
                var calX = RegCenter.X - e.X;
                var calY = RegCenter.Y - e.Y;
                TopLeft = new Point2d(TopLeft.X - calX, TopLeft.Y - calY);
                BottomRight = new Point2d(BottomRight.X - calX, BottomRight.Y - calY);
                dispRectangle(context, HColor.Red);
            }
            else if (HalconHelper.IsNearPoint(TopLeft.X, TopLeft.Y, e.X, e.Y))
            {
                // 靠近左上角，高亮显示
                display.DispPoint(TopLeft, HColor.Green, 10);
                context.CycleMove = CycleMoveEnum.Start;
            }
            else if (HalconHelper.IsNearPoint(BottomRight.X, BottomRight.Y, e.X, e.Y))
            {
                // 靠近右下角，高亮显示
                display.DispPoint(BottomRight, HColor.Green, 10);
                context.CycleMove = CycleMoveEnum.End;
            }
            else if (HalconHelper.IsNearPoint(RegCenter.X, RegCenter.Y, e.X, e.Y))
            {
                // 靠近右下角，高亮显示
                display.DispPoint(RegCenter, HColor.Green, 10);
                context.CycleMove = CycleMoveEnum.Center;
            }
            else
            {
                context.CycleMove = CycleMoveEnum.None;
            }
        }

        private void dispRectangle(DrawContext context, string color = "red")
        {
            var X1 = TopLeft.X;
            var Y1 = TopLeft.Y;
            var X2 = BottomRight.X;
            var Y2 = BottomRight.Y;

            dispRectangle(context, X1, Y1, X2, Y2, color);
        }

        private void dispRectangle(DrawContext context, double X1, double Y1, double X2, double Y2, string color = "red")
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
