using DotNet.Drawing;
using HalconDotNet;
using DotNet.Vision.Abstractions;


namespace DotNet.HalconUI
{
    /// <summary>
    /// 模板显示处理器
    /// 鼠标移动时重绘模板区域、轮廓与原点坐标（文档原为 EraseRectMouse 的复制品，与实现无关）
    /// </summary>
    public class DispModelMouse : IMouseHandler
    {
        // 两段式初始化，见 EraseRectMouse 同名字段的说明：
        // 事件只在 HDisplayUI.DrawType == DrawEnum.DispModel 时分发，而该赋值与 SetUp 同在 SetModelPara 里。
        private IHDisplay _display = null!;
        // 这两个只是转交给 Disp(HObject?) 的借用句柄, 空句柄由那边按"不画"处理, 无需 null!。
        private HObject? _findMode;
        private HObject? _contour;
        private CvCoord _coord;

        public void SetUp(IHDisplay display, HObject? shrFindMode, HObject? shrContour, CvCoord shrCoord)
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
