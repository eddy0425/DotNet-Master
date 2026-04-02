using DotNet.HWindows;
using HalconDotNet;
using OpenCvSharp;
using System.Drawing;
using System.Windows.Forms;

namespace DotNet.VisionMaster
{
    /// <summary>
    /// 矩形绘图处理器
    /// 用于绘制和编辑矩形区域
    /// </summary>
    public class RectNewHandler : IDrawHandler
    {
        HObject HoRectangle;
        Point2d TopLeft;
        Point2d BottomRight;
        Point2d RegCenter => new Point2d((TopLeft.X + BottomRight.X) / 2, (TopLeft.Y + BottomRight.Y) / 2);
        public RectNewHandler()
        {
            HoRectangle = new HObject(); HOperatorSet.GenEmptyObj(out HoRectangle); // 创建初始空对象
        }

        public bool NeedReDispImage => true;

        public void SetUp(DrawContext context)
        {
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
                        context.DispCross(context.Center.Y, context.Center.X, 100, HColor.OrangeRed);
                    }
                    break;
            }

            switch (context.SetUp)
            {
                case SetUpEnum.Step1:
                    // 显示光标十字
                    context.DispCross(e.Y, e.X, HColor.OrangeRed);
                    break;

                case SetUpEnum.Step2:
                    // 绘制中：显示两个角点和矩形预览
                    context.DispCross(TopLeft.Y, TopLeft.X, HColor.OrangeRed);
                    context.DispCross(e.Y, e.X, HColor.OrangeRed);
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
                        context.DispCross(TopLeft.Y, TopLeft.X, 50, HColor.OrangeRed);
                        context.DispCross(BottomRight.Y, BottomRight.X, 50, HColor.OrangeRed);
                        dispRectangle(context, HColor.Blue);

                        context.DispRegion(context.HContext.HoRect, HColor.Blue);
                        context.DispRegion(context.HoContour, HColor.Green);

                        if (context.Center != null)
                        {
                            context.DispCross(context.Center.Y, context.Center.X, HColor.Yellow);
                        }
                    }
                    break;
            }
        }

        private void HandleStep3Move(DrawContext context, HMouseEventArgs e)
        {
            // 显示角点
            context.DispCross(TopLeft.Y, TopLeft.X, 50, HColor.Orange);
            context.DispCross(BottomRight.Y, BottomRight.X, 50, HColor.Orange);
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
            else if (context.IsNearPoint(TopLeft.X, TopLeft.Y, e.X, e.Y))
            {
                // 靠近左上角，高亮显示
                context.DispCross(TopLeft.Y, TopLeft.X, 10, HColor.Green);
                context.CycleMove = CycleMoveEnum.Start;
            }
            else if (context.IsNearPoint(BottomRight.X, BottomRight.Y, e.X, e.Y))
            {
                // 靠近右下角，高亮显示
                context.DispCross(BottomRight.Y, BottomRight.X, 10, HColor.Green);
                context.CycleMove = CycleMoveEnum.End;
            }
            else if (context.IsNearPoint(RegCenter.X, RegCenter.Y, e.X, e.Y))
            {
                // 靠近右下角，高亮显示
                context.DispCross(RegCenter.Y, RegCenter.X, 10, HColor.Green);
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
                HoRectangle.Dispose();
                HOperatorSet.GenRectangle1(out HoRectangle, Y1, X1, Y2, X2);
                context.DispRegion(HoRectangle, color);

                var p = context.Cal2P(X1, Y1, X2, Y2);
                context.DispCross(p.Y, p.X, 100, HColor.Orange);
            }
            catch { }
        }

    }
}
