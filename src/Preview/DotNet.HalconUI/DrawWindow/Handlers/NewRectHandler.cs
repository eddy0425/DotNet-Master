using DotNet.Drawing;
using HalconDotNet;
using System.Windows.Forms;
using static DotNet.HalconUI.ModelHandlerFactory;


namespace DotNet.HalconUI
{
    /// <summary>
    /// 矩形绘图处理器
    /// 用于绘制和编辑矩形区域
    /// </summary>
    public class NewRectHandler : IModelHandler
    {
        private Point2d TopLeft;
        private Point2d BottomRight;
        private Point2d RegCenter => new Point2d((TopLeft.X + BottomRight.X) / 2, (TopLeft.Y + BottomRight.Y) / 2);

        public bool NeedReDisp => true;

        public void SetUp(EditModelForm display)
        {
            if (display.SetUp != SetUpEnum.None) return;

            display.Reset();
            display.ReDispImage();
            display.SetUp = SetUpEnum.Step1;
        }

        public void OnMouseDown(EditModelForm display, HMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            switch (display.SetUp)
            {
                case SetUpEnum.Step1:
                    // 开始绘制矩形，记录左上角
                    TopLeft = new Point2d(e.X, e.Y);
                    display.SetUp = SetUpEnum.Step2;
                    break;

                case SetUpEnum.Step3:
                    // 编辑模式：从悬停状态切换到拖动状态
                    display.CycleMove = ToMoveState(display.CycleMove);
                    break;
            }
        }

        public void OnMouseUp(EditModelForm display, HMouseEventArgs e)
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
            else if (e.Button == MouseButtons.Right && display.SetUp == SetUpEnum.Step3)
            {
                // 右键确认，进入下一步
                display.SetUp = SetUpEnum.Step4;
            }
        }

        public void OnMouseWheel(EditModelForm display, HMouseEventArgs e)
        {
            // 滚轮事件仅触发重绘
        }

        public void OnMouseMove(EditModelForm display, HMouseEventArgs e)
        {
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
                    DispRectangle(display, TopLeft.X, TopLeft.Y, e.X, e.Y, HColor.Red);
                    break;

                case SetUpEnum.Step3:
                    HandleStep3Move(display, e);
                    break;

                case SetUpEnum.Step4:
                    display.ShrRegion.Update2Point(TopLeft, BottomRight);
                    display.ShrRegion.GenRegion();
                    display.SetUp = SetUpEnum.Step5;
                    break;

                case SetUpEnum.Step5:
                    // 显示最终结果
                    display.DispPoint(TopLeft, HColor.OrangeRed, 50);
                    display.DispPoint(BottomRight, HColor.OrangeRed, 50);
                    display.DispPoint(RegCenter, HColor.Orange, 50);
                    display.DispRegion(display.ShrRegion, HColor.Blue);
                    break;
            }
        }

        private void HandleStep3Move(EditModelForm display, HMouseEventArgs e)
        {
            // 始终显示两个角点
            display.DispPoint(TopLeft, HColor.Orange, 50);
            display.DispPoint(BottomRight, HColor.Orange, 50);

            switch (display.CycleMove)
            {
                case CycleMoveEnum.StartMove:
                    // 移动左上角
                    TopLeft = new Point2d(e.X, e.Y);
                    break;

                case CycleMoveEnum.EndMove:
                    // 移动右下角
                    BottomRight = new Point2d(e.X, e.Y);
                    break;

                case CycleMoveEnum.CenterMove:
                    // 移动中心：整体平移
                    var dx = e.X - RegCenter.X;
                    var dy = e.Y - RegCenter.Y;
                    TopLeft = new Point2d(TopLeft.X + dx, TopLeft.Y + dy);
                    BottomRight = new Point2d(BottomRight.X + dx, BottomRight.Y + dy);
                    break;

                default:
                    UpdateHoverState(display, e);
                    break;
            }

            DispRectangle(display, HColor.Red);
        }

        private void UpdateHoverState(EditModelForm display, HMouseEventArgs e)
        {
            if (HalconHelper.IsNearPoint(TopLeft.X, TopLeft.Y, e.X, e.Y))
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
                // 靠近中心点，高亮显示
                display.DispPoint(RegCenter, HColor.Green, 10);
                display.CycleMove = CycleMoveEnum.Center;
            }
            else
            {
                display.CycleMove = CycleMoveEnum.None;
            }
        }

        private static CycleMoveEnum ToMoveState(CycleMoveEnum hover)
        {
            switch (hover)
            {
                case CycleMoveEnum.Start: return CycleMoveEnum.StartMove;
                case CycleMoveEnum.End: return CycleMoveEnum.EndMove;
                case CycleMoveEnum.Center: return CycleMoveEnum.CenterMove;
                default: return hover;
            }
        }

        private void DispRectangle(EditModelForm display, string color = "red")
            => DispRectangle(display, TopLeft.X, TopLeft.Y, BottomRight.X, BottomRight.Y, color);

        private static void DispRectangle(EditModelForm display, double x1, double y1, double x2, double y2, string color = "red")
        {
            try
            {
                HOperatorSet.GenEmptyObj(out display.ShrRegion.InRegion);
                HOperatorSet.GenRectangle1(out display.ShrRegion.InRegion, y1, x1, y2, x2);
                display.DispRegion(display.ShrRegion.InRegion, color);

                var p = HalconHelper.Cal2P(x1, y1, x2, y2);
                display.DispPoint(p.X, p.Y, HColor.Orange, 100);
            }
            catch { }
        }

    }
}
