using DotNet.Drawing;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Windows.Forms;


namespace DotNet.HalconUI
{
    public partial class HDisplayForm : UserControl
    {
        HDisplayCore display;
        public event HMouseEventHandler HMouseUp { add => hWindowControl.HMouseUp += value; remove => hWindowControl.HMouseUp -= value; }
        public event HMouseEventHandler HMouseMove { add => hWindowControl.HMouseMove += value; remove => hWindowControl.HMouseMove -= value; }
        public event HMouseEventHandler HMouseDown { add => hWindowControl.HMouseDown += value; remove => hWindowControl.HMouseDown -= value; }
        public event HMouseEventHandler HMouseWheel { add => hWindowControl.HMouseWheel += value; remove => hWindowControl.HMouseWheel -= value; }

        public event EventHandler<DrawModelUIArgs> DrawDoneEvent;
        public void DrawDone(string modelPath, HObject ho_ModeRect, HObject ho_Contour, ModelResult result)
        {
            var handler = DrawDoneEvent;
            if (handler != null) handler(this, new DrawModelUIArgs(modelPath, ho_ModeRect, ho_Contour, result));
        }

        #region 属性
        public double HoWidth => display.Width;
        public double HoHeight => display.Height;
        public Size2d HoSize => display.Size;
        public Point2d HoCentre => display.Centre;
        public HObject HoImage => display.HoImage;  //图像
        public HWindow HoWindow => hWindowControl.HalconWindow;  //窗体句柄
        public bool HoMouseDown { get { return display.MouseDown; } set { display.MouseDown = value; } } //鼠标按下
        public bool HoMouseDouble { get { return display.MouseDouble; } set { display.MouseDouble = value; } }  //鼠标双击按下
        public bool IsCross { get { return display.IsCross; } set { display.IsCross = value; } } //是否画十字
        public bool Adaptive { get { return display.Adaptive; } set { display.Adaptive = value; } } //自适应

        #endregion

        public HDisplayForm()
        {
            InitializeComponent();
            display = new HDisplayCore(hWindowControl);
            display.RefreshUI += Display_RefreshUI;
        }

        private void Display_RefreshUI(HTuple Row, HTuple Column, HTuple egray)
        {
            lbl_result.Text = string.Format("坐标[X:{0} Y:{1}]  灰度:{2} ", Column.D.ToString("F2"), Row.D.ToString("F2"), egray.D.ToString());
        }

        private void btn_ReSetPart_Click(object sender, EventArgs e)
        {
            Adaptive = !Adaptive;
        }

        private void but_IsCross_Click(object sender, EventArgs e)
        {
            IsCross = !IsCross;
        }

        public void Reset()
        {
            hWindowControl.Focus();
        }

        public void SetDraw(HTuple mode)
        {
            HOperatorSet.SetDraw(HoWindow, mode);
        }

        /// <summary> 绘制区域 </summary>
        public void DrawRegion(RectEnum type, out CvRegion hRegion)
        {
            Reset();
            DrawHelper.CancelDraw();
            hRegion = new CvRegion();
            hRegion.Type = type;
            switch (type)
            {
                case RectEnum.Rectangle:
                    {
                        DrawHelper.DrawRectangle1(HoWindow, out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2);
                        HOperatorSet.GenRectangle1(out HObject rectangle, row1, column1, row2, column2);

                        hRegion.Update2Point(row1, column1, row2, column2);
                        hRegion.HoRegion.Dispose();
                        hRegion.HoRegion = rectangle;
                    }
                    break;
                case RectEnum.AffRect:
                    {
                        DrawHelper.DrawRectangle2(HoWindow, out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2);
                        HOperatorSet.GenRectangle2(out HObject rectangle, row, column, phi, length1, length2);

                        hRegion.UpdateCenter(new Point2d(column.D, row.D), new Size2d(length1.D * 2, length2.D * 2));
                        hRegion.Phi = phi;
                        hRegion.HoRegion.Dispose();
                        hRegion.HoRegion = rectangle;
                    }
                    break;
                case RectEnum.Circle:
                    {
                        DrawHelper.DrawCircle(HoWindow, out HTuple row, out HTuple column, out HTuple radius);
                        HOperatorSet.GenCircle(out HObject circle, row, column, radius);

                        hRegion.UpdateCenter(new Point2d(column.D, row.D), new Size2d(radius.D * 2, radius.D * 2));
                        hRegion.HoRegion.Dispose();
                        hRegion.HoRegion = circle;
                    }
                    break;
                case RectEnum.Ellipse:
                    {
                        DrawHelper.DrawEllipse(HoWindow, out HTuple row, out HTuple column, out HTuple phi, out HTuple radius1, out HTuple radius2);
                        HOperatorSet.GenEllipse(out HObject ellipse, row, column, phi, radius1, radius2);

                        hRegion.UpdateCenter(new Point2d(column.D, row.D), new Size2d(radius1.D * 2, radius2.D * 2));
                        hRegion.Phi = phi;
                        hRegion.HoRegion.Dispose();
                        hRegion.HoRegion = ellipse;
                    }
                    break;
                case RectEnum.Polygon:
                    {
                        DrawHelper.DrawRegion(out HObject region, HoWindow);
                        HOperatorSet.GetRegionPolygon(region, 1, out HTuple rows, out HTuple columns);
                        HOperatorSet.AreaCenter(region, out HTuple area, out HTuple hv_Row, out HTuple hv_Column);
                        hRegion.PolygonX = columns;
                        hRegion.PolygonY = rows;
                        hRegion.Center = new Point2d(hv_Column.D, hv_Row.D);
                        hRegion.HoRegion.Dispose();
                        hRegion.HoRegion = region;
                    }
                    break;
                case RectEnum.Ring:
                    {
                        //HObject circle1; HOperatorSet.GenEmptyObj(out circle1);
                        //HObject circle2; HOperatorSet.GenEmptyObj(out circle2);
                        //try
                        //{
                        //    HOperatorSet.GenCircle(out circle1, hRegion.CenterY, hRegion.CenterX, hRegion.MaxRadius);
                        //    HOperatorSet.GenCircle(out circle2, hRegion.CenterY, hRegion.CenterX, hRegion.MinRadius);
                        //    HOperatorSet.Difference(circle1, circle2, out HObject region);
                        //    hRegion.HoRegion.Dispose();
                        //    hRegion.HoRegion = region;
                        //}
                        //finally
                        //{
                        //    circle1.Dispose();
                        //    circle2.Dispose();
                        //}
                    }
                    break;
            }
        }


        #region IHWindowFont

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

        #region DispImage

        /// <summary> 重新显示图片 </summary>
        public void ReDispImage()
        {
            display.ReDispImage();
        }

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

        public void ClearWinDisp(HObject objectVal)
        {
            display.ClearWinDisp(objectVal);
        }

        #endregion

        #region 区域相关

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        public void DispGenRegion(CvRegion hRegion)
        {
            display.DispGenRegion(hRegion);
        }

        /// <summary>
        /// 获取坐标区域并显示
        /// </summary>
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

        /// <summary>
        /// 绘制（创建）橡皮筋区域
        /// </summary>
        public void DrawRegion(CvRegion hRegion)
        {
            display.DrawRegion(hRegion);
        }

        /// <summary>
        /// 绘制（创建）橡皮筋区域
        /// </summary>
        public void DrawRegionMod(CvRegion hRegion)
        {
            display.DrawRegionMod(hRegion);
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
