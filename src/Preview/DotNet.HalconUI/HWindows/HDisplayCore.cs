using DotNet.Drawing;
using HalconDotNet;
using System;
using System.Collections.Generic;


namespace DotNet.HalconUI
{
    public class HDisplayCore : IDisposable, IHDisplay
    {
        bool _disposed;

        readonly HWindow _hWindow;
        readonly HWindowMouse _hWindowMouse;
        readonly IHDisplay _display;

        #region 属性
        public double HoWidth { get { return _display.HoWidth; } }
        public double HoHeight { get { return _display.HoHeight; } }
        public Size2d Size => new Size2d(_display.HoWidth, _display.HoHeight);
        public Point2d Centre => new Point2d(_display.HoWidth / 2, _display.HoHeight / 2);
        public HObject HoImage => _display.HoImage;  //图像
        public HWindow HoWindow => _hWindow;  //窗体控件
        public bool IsCross { get { return _display.IsCross; } set { _display.IsCross = value; } } //是否画十字
        public bool Adaptive { get { return _display.Adaptive; } set { _display.Adaptive = value; } } //自适应
        public bool MouseDown { get { return _hWindowMouse.MouseDown; } set { _hWindowMouse.MouseDown = value; } } //鼠标按下
        public bool MouseDouble { get { return _hWindowMouse.MouseDouble; } set { _hWindowMouse.MouseDouble = value; } }  //鼠标双击按下

        public event Action<HTuple, HTuple, HTuple> RefreshUI
        {
            add => _hWindowMouse.RefreshUI += value;
            remove => _hWindowMouse.RefreshUI -= value;
        }

        #endregion

        public HDisplayCore(HWindowControl hWindowControl)
        {
            if (hWindowControl == null) throw new ArgumentNullException(nameof(hWindowControl));

            _hWindow = hWindowControl.HalconWindow;
            _display = new HDisplay(hWindowControl);
            _hWindowMouse = new HWindowMouse(hWindowControl, _display);

            // 用占位灰图初始化窗口；CopyImage 后这里 using 释放也是安全的
            using (HImage hImage = new HImage("byte", 800, 600))
            {
                DispImage(hImage);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // 释放顺序：先解绑事件订阅，再释放本类持有所有权的图像。
            // 注意：hWindow / _hWindowControl 由宿主 UserControl (HDisplayUI) 通过 Designer 的 Dispose(bool) 负责释放，本类不再主动释放，避免双重释放。

            try { _hWindowMouse?.Dispose(); } catch { /* swallow */ }
            try { _display?.Dispose(); } catch { /* swallow */ }

            GC.SuppressFinalize(this);
        }

        #region HWindowImage

        /// <summary> 设置图像 </summary>
        public void SetImage(HObject image)
        {
            _display.SetImage(image);
        }

        /// <summary> 显示图片 </summary>
        public void DispImage(HObject image)
        {
            _display.DispImage(image);
        }

        /// <summary> 显示图片 </summary>
        public void DispImage(HObject image, bool isSetPart)
        {
            _display.DispImage(image, isSetPart);
        }

        /// <summary> 重新显示图片 </summary>
        public void ReDispImage()
        {
            _display.ReDispImage();
        }

        #endregion

        #region IHWindowFont

        /// <summary> 设置字体大小 </summary>
        public void SetFontSize(HTuple hv_Size)
        {
            _display.SetFontSize(hv_Size);
        }

        /// <summary> 显示字体 </summary>
        public void DispText(string message, HTuple FontX, HTuple FontY, string color)
        {
            _display.DispText(message, FontX, FontY, color);
        }

        /// <summary> 显示字体 </summary>
        public void DispText(string message, HTuple FontX, HTuple FontY, HTuple size, string color)
        {
            _display.DispText(message, FontX, FontY, size, color);
        }

        #endregion

        /// <summary> 获取颜色 </summary>
        public string GetColor()
        {
            return _display.GetColor();
        }

        /// <summary> 设置颜色 </summary>
        public void SetColor(string color)
        {
            _display.SetColor(color);
        }

        public void ClearWinDisp(HObject objectVal)
        {
            _display.ClearWinDisp(objectVal);
        }

        #region 区域相关

        /// <summary> 显示橡皮筋区域 </summary>
        public void DispGenRegion(CvRegion hRegion)
        {
            _display.DispGenRegion(hRegion);
        }

        /// <summary> 获取坐标区域并显示 </summary>
        public void GenCoordsRegion(CvRegion hRegion, List<CvCoord> coords)
        {
            _display.GenCoordsRegion(hRegion, coords);
        }

        #endregion

        #region 点相关
        public void DispPoint(double crossX, double crossY, double size = 20)
        {
            _display.DispPoint(crossX, crossY, size);
        }
        public void DispPoint(double crossX, double crossY, string color, int size = 20)
        {
            _display.DispPoint(crossX, crossY, color, size);
        }
        public void DispPoint(HTuple crossX, HTuple crossY, double size = 20)
        {
            _display.DispPoint(crossX, crossY, size);
        }
        public void DispPoint(HTuple crossX, HTuple crossY, string color, int size = 20)
        {
            _display.DispPoint(crossX, crossY, color, size);
        }
        public void DispPoint(double[] rowPoints, double[] columnPoints, int size = 20)
        {
            _display.DispPoint(rowPoints, columnPoints, size);
        }
        public void DispPoint(double[] rowPoints, double[] columnPoints, string color, int size = 20)
        {
            _display.DispPoint(rowPoints, columnPoints, color, size);
        }
        public void DispPoint(List<Point2d> polygons, int size = 20)
        {
            _display.DispPoint(polygons, size);
        }
        public void DispPoint(List<Point2d> polygons, string color, int size = 20)
        {
            _display.DispPoint(polygons, color, size);
        }
        public void DispPoint(Point2d point, double size = 20)
        {
            _display.DispPoint(point, size);
        }
        public void DispPoint(Point2d point, string color, double size = 20)
        {
            _display.DispPoint(point, color, size);
        }

        #endregion

        #region 坐标相关
        public void DispCross(double crossX, double crossY, double angle, double size = 20)
        {
            _display.DispCross(crossX, crossY, angle, size);
        }
        public void DispCross(double crossX, double crossY, double angle, string color, double size = 20)
        {
            _display.DispCross(crossX, crossY, angle, color, size);
        }
        public void DispCross(Point2d point, double angle, double size = 20)
        {
            _display.DispCross(point, angle, size);
        }
        public void DispCross(Point2d point, double angle, string color, double size = 20)
        {
            _display.DispCross(point, angle, color, size);
        }
        public void DispCross(CvCoord coord, double size = 20)
        {
            _display.DispCross(coord, size);
        }
        public void DispCross(CvCoord coord, string color, double size = 20)
        {
            _display.DispCross(coord, color, size);
        }

        #endregion

        #region 线相关
        public void DispLine(double startX, double startY, double endX, double endY)
        {
            _display.DispLine(startX, startY, endX, endY);
        }
        public void DispLine(double startX, double startY, double endX, double endY, string color)
        {
            _display.DispLine(startX, startY, endX, endY, color);
        }
        public void DispLine(CvLine line)
        {
            _display.DispLine(line);
        }
        public void DispLine(CvLine line, string color)
        {
            _display.DispLine(line, color);
        }
        public void DispLine(CvLine line, int radius)
        {
            _display.DispLine(line, radius);
        }
        public void DispLine(CvLine line, int radius, string color)
        {
            _display.DispLine(line, radius, color);
        }

        /// <summary> 画两点一线 </summary>
        public void DispLine(Point2d point1, Point2d point2, int step)
        {
            _display.DispLine(point1, point2, step);
        }

        /// <summary> 画两点一线 </summary>
        public void DispLine(Point2d point1, Point2d point2, int step, string color)
        {
            _display.DispLine(point1, point2, step, color);
        }

        #endregion

        #region 方向线
        public void DispArrow(double startX, double startY, double endX, double endY, double size = 20)
        {
            _display.DispArrow(startX, startY, endX, endY, size);
        }
        public void DispArrow(double startX, double startY, double endX, double endY, string color, double size = 20)
        {
            _display.DispArrow(startX, startY, endX, endY, color, size);
        }
        public void DispArrow(CvLine line, double size = 20)
        {
            _display.DispArrow(line, size);
        }
        public void DispArrow(CvLine line, string color, double size = 20)
        {
            _display.DispArrow(line, color, size);
        }
        public void DispArrow(CvArrow arrow)
        {
            _display.DispArrow(arrow);
        }
        public void DispArrow(CvArrow arrow, string color)
        {
            _display.DispArrow(arrow, color);
        }

        #endregion

        #region 圆
        public void DispCircle(double crossX, double crossY, double radius)
        {
            _display.DispCircle(crossX, crossY, radius);
        }
        public void DispCircle(double crossX, double crossY, double radius, string color)
        {
            _display.DispCircle(crossX, crossY, radius, color);
        }
        public void DispCircle(CvCircle circle)
        {
            _display.DispCircle(circle);
        }
        public void DispCircle(CvCircle circle, string color)
        {
            _display.DispCircle(circle, color);
        }

        #endregion

        #region Region

        /// <summary> 显示橡皮筋区域 </summary>
        public void DispRegion(HObject hRegion)
        {
            _display.DispRegion(hRegion);
        }

        /// <summary> 显示ROI区域 </summary>
        public void DispRegion(HObject hRegion, string color)
        {
            _display.DispRegion(hRegion, color);
        }

        /// <summary> 显示橡皮筋区域 </summary>
        public void DispRegion(CvRegion hRegion)
        {
            _display.DispRegion(hRegion);
        }

        /// <summary> 显示橡皮筋区域 </summary>
        public void DispRegion(CvRegion hRegion, string color)
        {
            _display.DispRegion(hRegion, color);
        }

        /// <summary> 显示橡皮筋区域 </summary>
        public void DispCvRegion(CvRegion hRegion)
        {
            _display.DispCvRegion(hRegion);
        }

        /// <summary> 绘制（创建）橡皮筋区域 </summary>
        public void DrawRegion(CvRegion hRegion)
        {
            _display.DrawRegion(hRegion);
        }

        /// <summary> 绘制（修改）橡皮筋区域 </summary>
        public void DrawRegionMod(CvRegion hRegion)
        {
            _display.DrawRegionMod(hRegion);
        }

        /// <summary> 绘制（创建）橡皮筋区域 </summary>
        public void DrawRegion(RectEnum type, out HObject rectangle)
        {
            _display.DrawRegion(type, out rectangle);
        }

        public void DispRectangle2(HTuple centerRow, HTuple centerCol, HTuple phi, HTuple length1, HTuple length2)
        {
            _display.DispRectangle2(centerRow, centerCol, phi, length1, length2);
        }

        public void DispRectangle2(HTuple centerRow, HTuple centerCol, HTuple phi, HTuple length1, HTuple length2, string color)
        {
            _display.DispRectangle2(centerRow, centerCol, phi, length1, length2, color);
        }

        #endregion
    }
}
