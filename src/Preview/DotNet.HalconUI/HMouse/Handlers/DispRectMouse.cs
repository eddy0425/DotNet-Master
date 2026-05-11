using DotNet.Drawing;
using HalconDotNet;


namespace DotNet.HalconUI
{
    /// <summary>
    /// 擦除矩形处理器
    /// 通过左键拖动以圆形画笔擦除区域
    /// </summary>
    public class DispRectMouse : IMouseHandler
    {
        private HDisplayUI _display;
        private CvRegion _shrRegion;

        public void SetUp(HDisplayUI display, CvRegion shrRegion)
        {
            _display = display;
            _shrRegion = shrRegion;
        }

        public void OnMouseDown(HMouseEventArgs e)
        {
            // 无操作
        }

        public void OnMouseUp(HMouseEventArgs e)
        {
            // 无操作
        }

        public void OnMouseWheel(HMouseEventArgs e)
        {
            // 无操作
        }

        public void OnMouseMove(HMouseEventArgs e)
        {
            Point2d TopLeft = _shrRegion.TopLeft;
            Point2d BottomRight = _shrRegion.BottomRight;
            Point2d Center = _shrRegion.Center;

            // 显示最终结果
            _display.DispPoint(TopLeft, HColor.OrangeRed, 50);
            _display.DispPoint(BottomRight, HColor.OrangeRed, 50);
            _display.DispPoint(Center, HColor.Orange, 50);

            _display.DispRegion(_shrRegion, HColor.Blue);
        }

    }
}
