using DotNet.Drawing;
using HalconDotNet;
using System;


namespace DotNet.HalconUI
{
    /// <summary>
    /// 擦除矩形处理器
    /// 通过左键拖动以圆形画笔擦除区域
    /// </summary>
    public class DispRectMouse : IMouseHandler
    {
        private IHDisplay _display;
        private CvRegion _shrRegion;

        public void SetUp(IHDisplay display, CvRegion shrRegion)
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
            try
            {
                Point2d TopLeft = _shrRegion.TopLeft;
                Point2d BottomRight = _shrRegion.BottomRight;
                Point2d Center = _shrRegion.Center;

                // 显示最终结果
                //_display.Disp(TopLeft, DrawStyle.Of(HColor.OrangeRed, 50));
                //_display.Disp(BottomRight, DrawStyle.Of(HColor.OrangeRed, 50));
                _display.Disp(Center, DrawStyle.Of(HColor.Orange, 50));

                _display.Disp(_shrRegion, DrawStyle.Of(HColor.Blue));
            }
            catch (Exception ex)
            {
                // 运行在鼠标移动回调里，抛出会连带打断整个拖拽交互；
                // 但一次都不记录的话，区域显示异常在现场完全无迹可循。
                Log.Warn(nameof(DispRectMouse), "显示区域失败.", ex);
            }
  
        }

    }
}
