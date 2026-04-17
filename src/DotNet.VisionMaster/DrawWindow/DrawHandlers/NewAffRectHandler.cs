using DotNet.Drawing;
using HalconDotNet;


namespace DotNet.VisionMaster
{
    /// <summary>
    /// 空绘图处理器
    /// 当不需要绘图时使用
    /// </summary>
    public class NewAffRectHandler : IDrawHandler
    {
        Point2d Center;
        Size2d RectSize;
        public double Phi;

        public bool NeedReDisp => false;

        public void SetUp(DisplayUI display)
        {
            if (display.SetUp == SetUpEnum.None)
            {
                display.Reset();
                display.ReDispImage();
                display.SetColor(HColor.Red);
                HOperatorSet.DrawRectangle2(display.HoWindow, out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2);
                //HOperatorSet.GenRectangle2(out display.ShrRegion.InRegion, row, column, phi, length1, length2);

                Center = new Point2d(column.D, row.D);
                RectSize = new Size2d(length1.D * 2, length2.D * 2);
                Phi = phi;
                display.SetUp = SetUpEnum.Step1;
            }
        }

        public void OnMouseDown(DisplayUI display, HMouseEventArgs e)
        {
            // 无操作
        }

        public void OnMouseUp(DisplayUI display, HMouseEventArgs e)
        {
            // 无操作
        }

        public void OnMouseWheel(DisplayUI display, HMouseEventArgs e)
        {
            // 无操作
        }

        public void OnMouseMove(DisplayUI display, HMouseEventArgs e)
        {
            switch (display.SetUp)
            {
                case SetUpEnum.Step1:
                    display.ShrRegion.UpdateCenter(Center, RectSize);
                    display.ShrRegion.Phi = Phi;
                    display.ShrRegion.Type = RectEnum.Rectangle2;
                    display.ShrRegion.GenRegion();
                    display.DrawAffRect(display.AlgoName, Center, RectSize, Phi);
                    display.SetUp = SetUpEnum.Step2;
                    break;

                case SetUpEnum.Step2:
                    display.DispRegion(display.ShrRegion, HColor.Green);
                    //display.SetUp = SetUpEnum.Step3;
                    break;
            }
        }

    }
}
