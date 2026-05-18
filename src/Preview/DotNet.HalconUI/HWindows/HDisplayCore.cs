using DotNet.Drawing;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Drawing;


namespace DotNet.HalconUI
{
    public class HDisplayCore : IDisposable
    {
        HWindow hWindow;
        HObject srcImage;
        IHWindowFont _hWindowFont;
        HWindowImage _hWindowImage;
        HWindowMouse _hWindowMouse;
        HWindowControl _hWindowControl;

        bool _disposed;

        public bool IsCross;           //是否画十字
        public bool Adaptive = true;   //自适应

        #region 属性
        public double Width { get { return _hWindowImage.Width; } }
        public double Height { get { return _hWindowImage.Height; } }
        public Size2d Size => new Size2d(_hWindowImage.Width, _hWindowImage.Height);
        public Point2d Centre => new Point2d(_hWindowImage.Width / 2, _hWindowImage.Height / 2);
        public HObject HoImage => _hWindowImage.HoImage;  //图像
        public HWindow HoWindow => hWindow;  //窗体控件
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
            hWindow = _hWindowControl.HalconWindow;
            _hWindowFont = new HWindowFont2018(hWindow);
            _hWindowImage = new HWindowImage(hWindow, _hWindowControl);
            _hWindowMouse = new HWindowMouse(hWindow, _hWindowControl, _hWindowImage);

            HOperatorSet.GenEmptyObj(out srcImage);

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
            _hWindowImage?.Dispose();
            srcImage?.Dispose();

            GC.SuppressFinalize(this);
        }

        #region IHWindowFont

        /// <summary> 设置字体大小 </summary>
        public void SetFontSize(HTuple hv_Size)
        {
            _hWindowFont.SetFontSize(hv_Size);
        }

        /// <summary> 显示字体 </summary>
        public void DispText(string message, HTuple FontX, HTuple FontY, string color)
        {
            _hWindowFont.DispText(message, FontY, FontX, color);
        }

        /// <summary> 显示字体 </summary>
        public void DispText(string message, HTuple FontX, HTuple FontY, HTuple size, string color)
        {
            _hWindowFont.SetFontSize(size);
            _hWindowFont.DispText(message, FontY, FontX, color);
        }

        #endregion

        #region HWindowImage

        /// <summary> 显示图片 </summary>
        public void DispImage(HObject image)
        {
            try
            {
                srcImage.Dispose();
                HOperatorSet.CopyImage(image, out srcImage);
                DispImage(srcImage, Adaptive);
            }
            catch
            {

            }
        }

        /// <summary> 显示图片 </summary>
        public void DispImage(HObject image, bool isSetPart)
        {
            _hWindowImage.Fun_DispImage(image, isSetPart);

            if (IsCross)
            {
                if (GetColor() != HColor.Red)
                {
                    SetColor(HColor.Red);
                }

                double size = Width > Height ? Width : Height;
                HOperatorSet.DispCross(hWindow, Height / 2, Width / 2, size, 0);
            }
        }

        /// <summary> 重新显示图片 </summary>
        public void ReDispImage()
        {
            _hWindowImage.Fun_ReDisplay();
        }

        #endregion

        IHDisplay display = new HDisplay();

        /// <summary> 获取颜色 </summary>
        public string GetColor()
        {
            return display.GetColor();
        }

        /// <summary> 设置颜色 </summary>
        public void SetColor(string color)
        {
            display.SetColor(hWindow, color);
        }

        public void ClearWinDisp(HObject objectVal)
        {
            display.ClearWinDisp(hWindow, objectVal);
        }

        #region 区域相关

        /// <summary> 显示橡皮筋区域 </summary>
        public void DispGenRegion(CvRegion hRegion)
        {
            display.DispGenRegion(hWindow, hRegion);
        }

        /// <summary> 获取坐标区域并显示 </summary>
        public void GenCoordsRegion(CvRegion hRegion, List<CvCoord> coords)
        {
            display.GenCoordsRegion(hWindow, hRegion, coords);
        }

        #endregion

        #region 点相关
        public void DispPoint(double crossX, double crossY, double size = 20)
        {
            display.DispPoint(hWindow, crossX, crossY, size);
        }
        public void DispPoint(double crossX, double crossY, string color, int size = 20)
        {
            display.DispPoint(hWindow, crossX, crossY, color, size);
        }
        public void DispPoint(HTuple crossX, HTuple crossY, double size = 20)
        {
            display.DispPoint(hWindow, crossX, crossY, size);
        }
        public void DispPoint(HTuple crossX, HTuple crossY, string color, int size = 20)
        {
            display.DispPoint(hWindow, crossX, crossY, color, size);
        }
        public void DispPoint(double[] rowPoints, double[] columnPoints, int size = 20)
        {
            display.DispPoint(hWindow, rowPoints, columnPoints, size);
        }
        public void DispPoint(double[] rowPoints, double[] columnPoints, string color, int size = 20)
        {
            display.DispPoint(hWindow, rowPoints, columnPoints, color, size);
        }
        public void DispPoint(List<Point2d> polygons, int size = 20)
        {
            display.DispPoint(hWindow, polygons, size);
        }
        public void DispPoint(List<Point2d> polygons, string color, int size = 20)
        {
            display.DispPoint(hWindow, polygons, color, size);
        }
        public void DispPoint(Point2d point, double size = 20)
        {
            display.DispPoint(hWindow, point, size);
        }
        public void DispPoint(Point2d point, string color, double size = 20)
        {
            display.DispPoint(hWindow, point, color, size);
        }

        #endregion

        #region 坐标相关
        public void DispCross(double crossX, double crossY, double angle, double size = 20)
        {
            display.DispCross(hWindow, crossX, crossY, angle, size);
        }
        public void DispCross(double crossX, double crossY, double angle, string color, double size = 20)
        {
            display.DispCross(hWindow, crossX, crossY, angle, color, size);
        }
        public void DispCross(Point2d point, double angle, double size = 20)
        {
            display.DispCross(hWindow, point, angle, size);
        }
        public void DispCross(Point2d point, double angle, string color, double size = 20)
        {
            display.DispCross(hWindow, point, angle, color, size);
        }
        public void DispCross(CvCoord coord, double size = 20)
        {
            display.DispCross(hWindow, coord, size);
        }
        public void DispCross(CvCoord coord, string color, double size = 20)
        {
            display.DispCross(hWindow, coord, color, size);
        }

        #endregion

        #region 线相关
        public void DispLine(double startX, double startY, double endX, double endY)
        {
            display.DispLine(hWindow, startX, startY, endX, endY);
        }
        public void DispLine(double startX, double startY, double endX, double endY, string color)
        {
            display.DispLine(hWindow, startX, startY, endX, endY, color);
        }
        public void DispLine(CvLine line)
        {
            display.DispLine(hWindow, line);
        }
        public void DispLine(CvLine line, string color)
        {
            display.DispLine(hWindow, line, color);
        }
        public void DispLine(CvLine line, int radius)
        {
            display.DispLine(hWindow, line, radius);
        }
        public void DispLine(CvLine line, int radius, string color)
        {
            display.DispLine(hWindow, line, radius, color);
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
            display.DispLine(hWindow, point1, point2, step);
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
            display.DispLine(hWindow, point1, point2, step, color);
        }

        #endregion

        #region 方向线
        public void DispArrow(double startX, double startY, double endX, double endY, double size = 20)
        {
            display.DispArrow(hWindow, startX, startY, endX, endY, size);
        }
        public void DispArrow(double startX, double startY, double endX, double endY, string color, double size = 20)
        {
            display.DispArrow(hWindow, startX, startY, endX, endY, color, size);
        }
        public void DispArrow(CvLine line, double size = 20)
        {
            display.DispArrow(hWindow, line, size);
        }
        public void DispArrow(CvLine line, string color, double size = 20)
        {
            display.DispArrow(hWindow, line, color, size);
        }
        public void DispArrow(CvArrow arrow)
        {
            display.DispArrow(hWindow, arrow);
        }
        public void DispArrow(CvArrow arrow, string color)
        {
            display.DispArrow(hWindow, arrow, color);
        }

        #endregion

        #region 圆
        public void DispCircle(double crossX, double crossY, double radius)
        {
            display.DispCircle(hWindow, crossX, crossY, radius);
        }
        public void DispCircle(double crossX, double crossY, double radius, string color)
        {
            display.DispCircle(hWindow, crossX, crossY, radius, color);
        }
        public void DispCircle(CvCircle circle)
        {
            display.DispCircle(hWindow, circle);
        }
        public void DispCircle(CvCircle circle, string color)
        {
            display.DispCircle(hWindow, circle, color);
        }

        #endregion

        #region Region

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        public void DispRegion(HObject hRegion)
        {
            display.DispRegion(hWindow, hRegion);
        }

        /// <summary>
        /// 显示ROI区域
        /// </summary>
        public void DispRegion(HObject hRegion, string color)
        {
            display.DispRegion(hWindow, hRegion, color);
        }

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        public void DispRegion(CvRegion hRegion)
        {
            display.DispRegion(hWindow, hRegion);
        }

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        public void DispRegion(CvRegion hRegion, string color)
        {
            display.DispRegion(hWindow, hRegion, color);
        }

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        public void DispCvRegion(CvRegion hRegion)
        {
            display.DispCvRegion(hWindow, hRegion);
        }

        /// <summary> 绘制（创建）橡皮筋区域 </summary>
        public void DrawRegion(CvRegion hRegion)
        {
            display.DrawRegion(hWindow, hRegion);
        }

        /// <summary> 绘制（修改）橡皮筋区域 </summary>
        public void DrawRegionMod(CvRegion hRegion)
        {
            display.DrawRegionMod(hWindow, hRegion);
        }

        /// <summary> 绘制（创建）橡皮筋区域 </summary>
        public void DrawRegion(RectEnum type, out HObject rectangle)
        {
            display.DrawRegion(hWindow, type, out rectangle);
        }

        public void DispRectangle2(HTuple centerRow, HTuple centerCol, HTuple phi, HTuple length1, HTuple length2)
        {
            display.DispRectangle2(hWindow, centerRow, centerCol, phi, length1, length2);
        }

        public void DispRectangle2(HTuple centerRow, HTuple centerCol, HTuple phi, HTuple length1, HTuple length2, string color)
        {
            display.DispRectangle2(hWindow, centerRow, centerCol, phi, length1, length2, color);
        }

        #endregion
    }
}
