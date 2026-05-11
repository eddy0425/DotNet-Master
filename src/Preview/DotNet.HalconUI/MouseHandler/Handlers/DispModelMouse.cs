using DotNet.Drawing;
using HalconDotNet;


namespace DotNet.HalconUI
{
    /// <summary>
    /// 擦除矩形处理器
    /// 通过左键拖动以圆形画笔擦除区域
    /// </summary>
    public class DispModelMouse : IMouseHandler
    {
        private HDisplayControl _display;
        private HObject _findMode;
        private HObject _contour;
        private CvCoord _coord;

        public void SetUp(HDisplayControl display, HObject shrFindMode, HObject shrContour, CvCoord shrCoord)
        {
            _display = display;
            _findMode = shrFindMode;
            _contour = shrContour;
            _coord = shrCoord;
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
            _display.DispRegion(_findMode, HColor.Blue);
            _display.DispRegion(_contour, HColor.Green);
            _display.DispCross(_coord, HColor.OrangeRed);
        }

    }
}
