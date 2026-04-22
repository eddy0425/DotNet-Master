using DotNet.Drawing;
using HalconDotNet;
using System.Collections.Generic;


namespace DotNet.HalconUI
{
    public static class EditModelExtension
    {
        public static void SetDraw(this EditModelForm form, HTuple mode)
        {
            var display = form.GetDisplay();
            display.SetDraw(mode);
        }

        public static void Reset(this EditModelForm form)
        {
            var display = form.GetDisplay();
            display.Reset();
        }

        #region IHWindowFont

        /// <summary> 获取颜色 </summary>
        public static string GetColor(this EditModelForm form)
        {
            var display = form.GetDisplay();
            return display.GetColor();
        }

        /// <summary> 设置颜色 </summary>
        public static void SetColor(this EditModelForm form, string color)
        {
            var display = form.GetDisplay();
            display.SetColor(color);
        }

        /// <summary> 设置字体大小 </summary>
        public static void SetFontSize(this EditModelForm form, HTuple hv_Size)
        {
            var display = form.GetDisplay();
            display.SetFontSize(hv_Size);
        }

        /// <summary> 显示字体 </summary>
        public static void DispText(this EditModelForm form, string message, HTuple FontX, HTuple FontY, string color)
        {
            var display = form.GetDisplay();
            display.DispText(message, FontX, FontY, color);
        }

        /// <summary> 显示字体 </summary>
        public static void DispText(this EditModelForm form, string message, HTuple FontX, HTuple FontY, HTuple size, string color)
        {
            var display = form.GetDisplay();
            display.DispText(message, FontX, FontY, size, color);
        }

        #endregion

        #region DispImage

        /// <summary> 重新显示图片 </summary>
        public static void ReDispImage(this EditModelForm form)
        {
            var display = form.GetDisplay();
            display.ReDispImage();
        }

        /// <summary> 显示图片 </summary>
        public static void DispImage(this EditModelForm form, HObject image)
        {
            var display = form.GetDisplay();
            display.DispImage(image);
        }

        /// <summary> 显示图片 </summary>
        public static void DispImage(this EditModelForm form, HObject image, bool isSetPart)
        {
            var display = form.GetDisplay();
            display.DispImage(image, isSetPart);
        }

        public static void ClearWinDisp(this EditModelForm form, HObject objectVal)
        {
            var display = form.GetDisplay();
            display.ClearWinDisp(objectVal);
        }

        #endregion

        #region 区域相关

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        public static void DispGenRegion(this EditModelForm form, CvRegion hRegion)
        {
            var display = form.GetDisplay();
            display.DispGenRegion(hRegion);
        }

        /// <summary>
        /// 获取坐标区域并显示
        /// </summary>
        public static void GenCoordsRegion(this EditModelForm form, CvRegion hRegion, List<CvCoord> coords)
        {
            var display = form.GetDisplay();
            display.GenCoordsRegion(hRegion, coords);
        }

        #endregion

        #region 点相关
        public static void DispPoint(this EditModelForm form, double crossX, double crossY, double size = 20)
        {
            var display = form.GetDisplay();
            display.DispPoint(crossX, crossY, size);
        }
        public static void DispPoint(this EditModelForm form, double crossX, double crossY, string color, int size = 20)
        {
            var display = form.GetDisplay();
            display.DispPoint(crossX, crossY, color, size);
        }
        public static void DispPoint(this EditModelForm form, HTuple crossX, HTuple crossY, double size = 20)
        {
            var display = form.GetDisplay();
            display.DispPoint(crossX, crossY, size);
        }
        public static void DispPoint(this EditModelForm form, HTuple crossX, HTuple crossY, string color, int size = 20)
        {
            var display = form.GetDisplay();
            display.DispPoint(crossX, crossY, color, size);
        }
        public static void DispPoint(this EditModelForm form, double[] rowPoints, double[] columnPoints, int size = 20)
        {
            var display = form.GetDisplay();
            display.DispPoint(rowPoints, columnPoints, size);
        }
        public static void DispPoint(this EditModelForm form, double[] rowPoints, double[] columnPoints, string color, int size = 20)
        {
            var display = form.GetDisplay();
            display.DispPoint(rowPoints, columnPoints, color, size);
        }
        public static void DispPoint(this EditModelForm form, List<Point2d> polygons, int size = 20)
        {
            var display = form.GetDisplay();
            display.DispPoint(polygons, size);
        }
        public static void DispPoint(this EditModelForm form, List<Point2d> polygons, string color, int size = 20)
        {
            var display = form.GetDisplay();
            display.DispPoint(polygons, color, size);
        }
        public static void DispPoint(this EditModelForm form, Point2d point, double size = 20)
        {
            var display = form.GetDisplay();
            display.DispPoint(point, size);
        }
        public static void DispPoint(this EditModelForm form, Point2d point, string color, double size = 20)
        {
            var display = form.GetDisplay();
            display.DispPoint(point, color, size);
        }

        #endregion

        #region 坐标相关
        public static void DispCross(this EditModelForm form, double crossX, double crossY, double angle, double size = 20)
        {
            var display = form.GetDisplay();
            display.DispCross(crossX, crossY, angle, size);
        }
        public static void DispCross(this EditModelForm form, double crossX, double crossY, double angle, string color, double size = 20)
        {
            var display = form.GetDisplay();
            display.DispCross(crossX, crossY, angle, color, size);
        }
        public static void DispCross(this EditModelForm form, Point2d point, double angle, double size = 20)
        {
            var display = form.GetDisplay();
            display.DispCross(point, angle, size);
        }
        public static void DispCross(this EditModelForm form, Point2d point, double angle, string color, double size = 20)
        {
            var display = form.GetDisplay();
            display.DispCross(point, angle, color, size);
        }
        public static void DispCross(this EditModelForm form, CvCoord coord, double size = 20)
        {
            var display = form.GetDisplay();
            display.DispCross(coord, size);
        }
        public static void DispCross(this EditModelForm form, CvCoord coord, string color, double size = 20)
        {
            var display = form.GetDisplay();
            display.DispCross(coord, color, size);
        }

        #endregion

        #region 线相关
        public static void DispLine(this EditModelForm form, double startX, double startY, double endX, double endY)
        {
            var display = form.GetDisplay();
            display.DispLine(startX, startY, endX, endY);
        }
        public static void DispLine(this EditModelForm form, double startX, double startY, double endX, double endY, string color)
        {
            var display = form.GetDisplay();
            display.DispLine(startX, startY, endX, endY, color);
        }
        public static void DispLine(this EditModelForm form, CvLine line)
        {
            var display = form.GetDisplay();
            display.DispLine(line);
        }
        public static void DispLine(this EditModelForm form, CvLine line, string color)
        {
            var display = form.GetDisplay();
            display.DispLine(line, color);
        }
        public static void DispLine(this EditModelForm form, CvLine line, int radius)
        {
            var display = form.GetDisplay();
            display.DispLine(line, radius);
        }
        public static void DispLine(this EditModelForm form, CvLine line, int radius, string color)
        {
            var display = form.GetDisplay();
            display.DispLine(line, radius, color);
        }

        /// <summary>
        /// 画两点一线
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="step"></param>
        /// <param name="color"></param>
        public static void DispLine(this EditModelForm form, Point2d point1, Point2d point2, int step)
        {
            var display = form.GetDisplay();
            display.DispLine(point1, point2, step);
        }

        /// <summary>
        /// 画两点一线
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="step"></param>
        /// <param name="color"></param>
        public static void DispLine(this EditModelForm form, Point2d point1, Point2d point2, int step, string color)
        {
            var display = form.GetDisplay();
            display.DispLine(point1, point2, step, color);
        }

        #endregion

        #region 方向线
        public static void DispArrow(this EditModelForm form, double startX, double startY, double endX, double endY, double size = 20)
        {
            var display = form.GetDisplay();
            display.DispArrow(startX, startY, endX, endY, size);
        }
        public static void DispArrow(this EditModelForm form, double startX, double startY, double endX, double endY, string color, double size = 20)
        {
            var display = form.GetDisplay();
            display.DispArrow(startX, startY, endX, endY, color, size);
        }
        public static void DispArrow(this EditModelForm form, CvLine line, double size = 20)
        {
            var display = form.GetDisplay();
            display.DispArrow(line, size);
        }
        public static void DispArrow(this EditModelForm form, CvLine line, string color, double size = 20)
        {
            var display = form.GetDisplay();
            display.DispArrow(line, color, size);
        }
        public static void DispArrow(this EditModelForm form, CvArrow arrow)
        {
            var display = form.GetDisplay();
            display.DispArrow(arrow);
        }
        public static void DispArrow(this EditModelForm form, CvArrow arrow, string color)
        {
            var display = form.GetDisplay();
            display.DispArrow(arrow, color);
        }

        #endregion

        #region 圆
        public static void DispCircle(this EditModelForm form, double crossX, double crossY, double radius)
        {
            var display = form.GetDisplay();
            display.DispCircle(crossX, crossY, radius);
        }
        public static void DispCircle(this EditModelForm form, double crossX, double crossY, double radius, string color)
        {
            var display = form.GetDisplay();
            display.DispCircle(crossX, crossY, radius, color);
        }
        public static void DispCircle(this EditModelForm form, CvCircle circle)
        {
            var display = form.GetDisplay();
            display.DispCircle(circle);
        }
        public static void DispCircle(this EditModelForm form, CvCircle circle, string color)
        {
            var display = form.GetDisplay();
            display.DispCircle(circle, color);
        }

        #endregion

        #region Region

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        public static void DispRegion(this EditModelForm form, HObject hRegion)
        {
            var display = form.GetDisplay();
            display.DispRegion(hRegion);
        }

        /// <summary>
        /// 显示ROI区域
        /// </summary>
        public static void DispRegion(this EditModelForm form, HObject hRegion, string color)
        {
            var display = form.GetDisplay();
            display.DispRegion(hRegion, color);
        }

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        public static void DispRegion(this EditModelForm form, CvRegion hRegion)
        {
            var display = form.GetDisplay();
            display.DispRegion(hRegion);
        }

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        public static void DispRegion(this EditModelForm form, CvRegion hRegion, string color)
        {
            var display = form.GetDisplay();
            display.DispRegion(hRegion, color);
        }

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        public static void DispCvRegion(this EditModelForm form, CvRegion hRegion)
        {
            var display = form.GetDisplay();
            display.DispCvRegion(hRegion);
        }

        /// <summary>
        /// 绘制（创建）橡皮筋区域
        /// </summary>
        public static void DrawRegion(this EditModelForm form, CvRegion hRegion)
        {
            var display = form.GetDisplay();
            display.DrawRegion(hRegion);
        }

        /// <summary>
        /// 绘制（创建）橡皮筋区域
        /// </summary>
        public static void DrawRegionMod(this EditModelForm form, CvRegion hRegion)
        {
            var display = form.GetDisplay();
            display.DrawRegionMod(hRegion);
        }

        public static void DispRectangle2(this EditModelForm form, HTuple centerRow, HTuple centerCol, HTuple phi, HTuple length1, HTuple length2)
        {
            var display = form.GetDisplay();
            display.DispRectangle2(centerRow, centerCol, phi, length1, length2);
        }

        public static void DispRectangle2(this EditModelForm form, HTuple centerRow, HTuple centerCol, HTuple phi, HTuple length1, HTuple length2, string color)
        {
            var display = form.GetDisplay();
            display.DispRectangle2(centerRow, centerCol, phi, length1, length2, color);
        }

        #endregion


    }
}
