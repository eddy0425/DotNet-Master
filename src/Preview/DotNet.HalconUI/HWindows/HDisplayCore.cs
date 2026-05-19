using DotNet.Drawing;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Drawing;


namespace DotNet.HalconUI
{
    public class HDisplayCore : IDisposable, IHDisplay
    {
        bool _disposed;

        HWindow _hWindow;
        HWindowMouse _hWindowMouse;
        HWindowControl _hWindowControl;
        IHDisplay display;

        #region 属性
        public double HoWidth { get { return display.HoWidth; } }
        public double HoHeight { get { return display.HoHeight; } }
        public Size2d Size => new Size2d(display.HoWidth, display.HoHeight);
        public Point2d Centre => new Point2d(display.HoWidth / 2, display.HoHeight / 2);
        public HObject HoImage => display.HoImage;  //图像
        public HWindow HoWindow => _hWindow;  //窗体控件
        public bool IsCross { get { return display.IsCross; } set { display.IsCross = value; } } //是否画十字
        public bool Adaptive { get { return display.Adaptive; } set { display.Adaptive = value; } } //自适应
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
            _hWindowControl = hWindowControl;
            _hWindow = _hWindowControl.HalconWindow;
            display = new HDisplay(_hWindow, _hWindowControl);
            _hWindowMouse = new HWindowMouse(_hWindow, _hWindowControl, display);

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

            _hWindowMouse?.Dispose();
            display.Dispose();

            GC.SuppressFinalize(this);
        }

        #region HWindowImage

        /// <summary> 显示图片 </summary>
        public void DispImage(HObject image)
        {
            display.DispImage(image);
        }

        /// <summary> 显示图片 </summary>
        public void DispImage(HObject image, bool isSetPart)
        {
            display.DispImage(image, isSetPart);
        }

        /// <summary> 重新显示图片 </summary>
        public void ReDispImage()
        {
            display.ReDispImage();
        }

        #endregion

        #region IHWindowFont

        /// <summary> 设置字体大小 </summary>
        public void SetFontSize(HTuple hv_Size)
        {
            display.SetFontSize(hv_Size);
        }

        /// <summary> 显示字体 </summary>
        public void DispText(string message, HTuple FontX, HTuple FontY, string color)
        {
            display.DispText(message, FontX, FontY, color);
        }

        /// <summary> 显示字体 </summary>
        public void DispText(string message, HTuple FontX, HTuple FontY, HTuple size, string color)
        {
            display.DispText(message, FontX, FontY, size, color);
        }

        #endregion

        /// <summary> 获取颜色 </summary>
        public string GetColor()
        {
            return display.GetColor();
        }

        /// <summary> 设置颜色 </summary>
        public void SetColor(string color)
        {
            display.SetColor(color);
        }

        public void ClearWinDisp(HObject objectVal)
        {
            display.ClearWinDisp(objectVal);
        }

        #region 区域相关

        /// <summary> 显示橡皮筋区域 </summary>
        public void DispGenRegion(CvRegion hRegion)
        {
            display.DispGenRegion(hRegion);
        }

        /// <summary> 获取坐标区域并显示 </summary>
        public void GenCoordsRegion(CvRegion hRegion, List<CvCoord> coords)
        {
            display.GenCoordsRegion(hRegion, coords);
        }

        #endregion

        #region 点相关
        public void DispPoint(double crossX, double crossY, double size = 20)
        {
            display.DispPoint(crossX, crossY, size);
        }
        public void DispPoint(double crossX, double crossY, string color, int size = 20)
        {
            display.DispPoint(crossX, crossY, color, size);
        }
        public void DispPoint(HTuple crossX, HTuple crossY, double size = 20)
        {
            display.DispPoint(crossX, crossY, size);
        }
        public void DispPoint(HTuple crossX, HTuple crossY, string color, int size = 20)
        {
            display.DispPoint(crossX, crossY, color, size);
        }
        public void DispPoint(double[] rowPoints, double[] columnPoints, int size = 20)
        {
            display.DispPoint(rowPoints, columnPoints, size);
        }
        public void DispPoint(double[] rowPoints, double[] columnPoints, string color, int size = 20)
        {
            display.DispPoint(rowPoints, columnPoints, color, size);
        }
        public void DispPoint(List<Point2d> polygons, int size = 20)
        {
            display.DispPoint(polygons, size);
        }
        public void DispPoint(List<Point2d> polygons, string color, int size = 20)
        {
            display.DispPoint(polygons, color, size);
        }
        public void DispPoint(Point2d point, double size = 20)
        {
            display.DispPoint(point, size);
        }
        public void DispPoint(Point2d point, string color, double size = 20)
        {
            display.DispPoint(point, color, size);
        }

        #endregion

        #region 坐标相关
        public void DispCross(double crossX, double crossY, double angle, double size = 20)
        {
            display.DispCross(crossX, crossY, angle, size);
        }
        public void DispCross(double crossX, double crossY, double angle, string color, double size = 20)
        {
            display.DispCross(crossX, crossY, angle, color, size);
        }
        public void DispCross(Point2d point, double angle, double size = 20)
        {
            display.DispCross(point, angle, size);
        }
        public void DispCross(Point2d point, double angle, string color, double size = 20)
        {
            display.DispCross(point, angle, color, size);
        }
        public void DispCross(CvCoord coord, double size = 20)
        {
            display.DispCross(coord, size);
        }
        public void DispCross(CvCoord coord, string color, double size = 20)
        {
            display.DispCross(coord, color, size);
        }

        #endregion

        #region 线相关
        public void DispLine(double startX, double startY, double endX, double endY)
        {
            display.DispLine(startX, startY, endX, endY);
        }
        public void DispLine(double startX, double startY, double endX, double endY, string color)
        {
            display.DispLine(startX, startY, endX, endY, color);
        }
        public void DispLine(CvLine line)
        {
            display.DispLine(line);
        }
        public void DispLine(CvLine line, string color)
        {
            display.DispLine(line, color);
        }
        public void DispLine(CvLine line, int radius)
        {
            display.DispLine(line, radius);
        }
        public void DispLine(CvLine line, int radius, string color)
        {
            display.DispLine(line, radius, color);
        }

        /// <summary>
        /// 画两点一线
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="step"></param>
        /// <param name="color"></param>
        public void DispLine(Point2d point1, Point2d point2, int step)
        {
            display.DispLine(point1, point2, step);
        }

        /// <summary>
        /// 画两点一线
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="step"></param>
        /// <param name="color"></param>
        public void DispLine(Point2d point1, Point2d point2, int step, string color)
        {
            display.DispLine(point1, point2, step, color);
        }

        #endregion

        #region 方向线
        public void DispArrow(double startX, double startY, double endX, double endY, double size = 20)
        {
            display.DispArrow(startX, startY, endX, endY, size);
        }
        public void DispArrow(double startX, double startY, double endX, double endY, string color, double size = 20)
        {
            display.DispArrow(startX, startY, endX, endY, color, size);
        }
        public void DispArrow(CvLine line, double size = 20)
        {
            display.DispArrow(line, size);
        }
        public void DispArrow(CvLine line, string color, double size = 20)
        {
            display.DispArrow(line, color, size);
        }
        public void DispArrow(CvArrow arrow)
        {
            display.DispArrow(arrow);
        }
        public void DispArrow(CvArrow arrow, string color)
        {
            display.DispArrow(arrow, color);
        }

        #endregion

        #region 圆
        public void DispCircle(double crossX, double crossY, double radius)
        {
            display.DispCircle(crossX, crossY, radius);
        }
        public void DispCircle(double crossX, double crossY, double radius, string color)
        {
            display.DispCircle(crossX, crossY, radius, color);
        }
        public void DispCircle(CvCircle circle)
        {
            display.DispCircle(circle);
        }
        public void DispCircle(CvCircle circle, string color)
        {
            display.DispCircle(circle, color);
        }

        #endregion

        #region Region

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        public void DispRegion(HObject hRegion)
        {
            display.DispRegion(hRegion);
        }

        /// <summary>
        /// 显示ROI区域
        /// </summary>
        public void DispRegion(HObject hRegion, string color)
        {
            display.DispRegion(hRegion, color);
        }

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        public void DispRegion(CvRegion hRegion)
        {
            display.DispRegion(hRegion);
        }

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        public void DispRegion(CvRegion hRegion, string color)
        {
            display.DispRegion(hRegion, color);
        }

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        public void DispCvRegion(CvRegion hRegion)
        {
            display.DispCvRegion(hRegion);
        }

        /// <summary> 绘制（创建）橡皮筋区域 </summary>
        public void DrawRegion(CvRegion hRegion)
        {
            display.DrawRegion(hRegion);
        }

        /// <summary> 绘制（修改）橡皮筋区域 </summary>
        public void DrawRegionMod(CvRegion hRegion)
        {
            display.DrawRegionMod(hRegion);
        }

        /// <summary> 绘制（创建）橡皮筋区域 </summary>
        public void DrawRegion(RectEnum type, out HObject rectangle)
        {
            display.DrawRegion(type, out rectangle);
        }

        public void DispRectangle2(HTuple centerRow, HTuple centerCol, HTuple phi, HTuple length1, HTuple length2)
        {
            display.DispRectangle2(centerRow, centerCol, phi, length1, length2);
        }

        public void DispRectangle2(HTuple centerRow, HTuple centerCol, HTuple phi, HTuple length1, HTuple length2, string color)
        {
            display.DispRectangle2(centerRow, centerCol, phi, length1, length2, color);
        }

        #endregion
    }
}
