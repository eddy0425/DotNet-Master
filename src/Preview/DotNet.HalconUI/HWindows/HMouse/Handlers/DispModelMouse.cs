using DotNet.Drawing;
using HalconDotNet;
using DotNet.Vision.Abstractions;


namespace DotNet.HalconUI
{
    /// <summary>
    /// 擦除矩形处理器
    /// 通过左键拖动以圆形画笔擦除区域
    /// </summary>
    public class DispModelMouse : IMouseHandler
    {
        private IHDisplay _display;
        private HObject _findMode;
        private HObject _contour;
        private CvCoord _coord;

        public void SetUp(IHDisplay display, HObject shrFindMode, HObject shrContour, CvCoord shrCoord)
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
            _display.Disp(_findMode, DrawStyle.Of(HColor.Blue));
            _display.Disp(_contour, DrawStyle.Of(HColor.Green));
            _display.Disp(_coord, DrawStyle.Of(HColor.OrangeRed));
        }

    }
}
