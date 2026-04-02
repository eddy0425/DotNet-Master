using HalconDotNet;
using OpenCvSharp;
using System.Collections.Generic;

namespace DotNet.HWindows
{
    public static class HWDisPlayExtension
    {
        public static void InitHalcon(this Form_HWDisPlay display)
        {
            //5120x3840  //512 × 512
            HImage hImage = new HImage("byte", 5120, 3840);
            display.DispImage(hImage);
            hImage.Dispose();
        }
        public static void SetDraw(this Form_HWDisPlay display, string format)
        {
            display.HoWindow.SetDraw(format);
        }
        public static void SetColor(this Form_HWDisPlay display, string color)
        {
            if (color == null) color = HColor.Red;
            display.HoWindow.SetColor(color);
        }
        public static void SetColorExists(this Form_HWDisPlay display, string color)
        {
            display.HoWindow.SetColorExists(color);
        }
        public static void DispObj(this Form_HWDisPlay display, HObject objectVal)
        {
            display.HoWindow.DispObj(objectVal);
        }
        public static void DispObj(this Form_HWDisPlay display, HObject objectVal, string color)
        {
            display.HoWindow.DispObj(objectVal, color);
        }

        #region 区域相关

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        public static void DispRegion(this Form_HWDisPlay display, CvRegion hRegion)
        {
            display.HoWindow.DispRegion(hRegion);
        }

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        public static void DispRegion(this Form_HWDisPlay display, CvRegion hRegion, string color)
        {
            display.HoWindow.DispRegion(hRegion, color);
        }

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        public static void DispGenRegion(this Form_HWDisPlay display, CvRegion hRegion)
        {
            display.HoWindow.DispGenRegion(hRegion);
        }

        /// <summary>
        /// 绘制（创建）橡皮筋区域
        /// </summary>
        public static void DrawRegion(this Form_HWDisPlay display, CvRegion hRegion)
        {
            display.hWindowControl1.Focus();
            display.HoWindow.DrawRegion(hRegion);
        }

        /// <summary>
        /// 绘制（创建）橡皮筋区域
        /// </summary>
        public static void DrawRegionMod(this Form_HWDisPlay display, CvRegion hRegion)
        {
            display.hWindowControl1.Focus();
            display.HoWindow.DrawRegionMod(hRegion);
        }

        /// <summary>
        /// 获取坐标区域并显示
        /// </summary>
        public static void GenCoordsRegion(this Form_HWDisPlay display, CvRegion hRegion, List<CvCoord> coords)
        {
            for (int i = 0; i < coords.Count; i++)
            {
                display.HoWindow.DispCross(coords[i], 100, HColor.Red);
                display.DispText((i + 1).ToString(), coords[i].X, coords[i].X, HColor.Green);
            }

            display.HoWindow.GenCoordsRegion(hRegion, coords);
        }

        #endregion

        #region 点相关

        //Point

        public static void DispPoint(this Form_HWDisPlay display, Point2d point)
        {
            display.HoWindow.DispPoint(point);
        }

        public static void DispPoint(this Form_HWDisPlay display, Point2d point, HTuple size)
        {
            display.HoWindow.DispPoint(point, size);
        }

        public static void DispPoint(this Form_HWDisPlay display, Point2d point, HTuple size, string color)
        {
            display.HoWindow.DispPoint(point, size, color);
        }

        #endregion

        #region 坐标点相关

        static int crossSize = 20;

        public static void DispCross(this Form_HWDisPlay display, CvCoord hCoord)
        {
            display.HoWindow.DispCross(hCoord);
        }

        public static void DispCross(this Form_HWDisPlay display, CvCoord hCoord, HTuple size)
        {
            display.HoWindow.DispCross(hCoord, size);
        }
       
        public static void DispCross(this Form_HWDisPlay display, CvCoord hCoord, HTuple size, string color)
        {
            display.HoWindow.DispCross(hCoord, size, color);
        }

        public static void DispCross(this Form_HWDisPlay display, HTuple crossX, HTuple crossY, HTuple angle)
        {
            display.HoWindow.DispCross(crossX, crossY, angle);
        }

        public static void DispCross(this Form_HWDisPlay display, HTuple crossX, HTuple crossY, HTuple angle, string color)
        {
            display.HoWindow.DispCross(crossX, crossY, angle, color);
        }

        public static void DispCross(this Form_HWDisPlay display, HTuple crossX, HTuple crossY, HTuple angle, HTuple size)
        {
            display.HoWindow.DispCross(crossY, crossX, size, angle);
        }

        public static void DispCross(this Form_HWDisPlay display, HTuple crossX, HTuple crossY, HTuple angle, HTuple size, string color)
        {
            display.HoWindow.DispCross(crossX, crossY, angle, size, color);
        }

        #endregion

        #region 线相关

        public static void DispLine(this Form_HWDisPlay display, CvLine line)
        {
            display.HoWindow.DispLine(line);
        }

        public static void DispLine(this Form_HWDisPlay display, CvLine line, string color)
        {
            display.HoWindow.DispLine(line, color);
        }

        public static void DispLine(this Form_HWDisPlay display, CvLine hLine, int r)
        {
            display.HoWindow.DispLine(hLine, r);
        }

        public static void DispLine(this Form_HWDisPlay display, CvLine hLine, int r, string color)
        {
            display.HoWindow.DispLine(hLine, color);
        }

        public static void draw_2poin_line(this Form_HWDisPlay display, Point2d point1, Point2d point2, int step)
        {
            display.HoWindow.draw_2poin_line(point1, point2, step);
        }

        public static void draw_2poin_line(this Form_HWDisPlay display, Point2d point1, Point2d point2, int step, string color)
        {
            display.HoWindow.draw_2poin_line(point1, point2, step, color);
        }

        #endregion

        #region 方向线 DispArrow

        public static void DispArrow(this Form_HWDisPlay display, CvLine line, double arrowSize)
        {
            display.HoWindow.DispArrow(line, arrowSize);
        }
        public static void DispArrow(this Form_HWDisPlay display, CvLine line, double arrowSize, string color)
        {
            display.HoWindow.DispArrow(line, arrowSize, color);
        }
        public static void DispArrow(this Form_HWDisPlay display, DispArrow arrow)
        {
            display.HoWindow.DispArrow(arrow);
        }
        public static void DispArrow(this Form_HWDisPlay display, double startX, double startY, double endX, double endY, double arrowSize)
        {
            display.HoWindow.DispArrow(startY, startX, endY, endX, arrowSize);
        }
        public static void DispArrow(this Form_HWDisPlay display, double startX, double startY, double endX, double endY, double arrowSize, string color)
        {
            display.HoWindow.DispArrow(startX, startY, endX, endY, arrowSize, color);
        }

        #endregion

        #region 圆相关
        public static void DispCircle(this Form_HWDisPlay display, CvCircle circle)
        {
            display.HoWindow.DispCircle(circle);
        }
        public static void DispCircle(this Form_HWDisPlay display, CvCircle circle, string color)
        {
            display.HoWindow.DispCircle(circle, color);
        }
        public static void DispCircle(this Form_HWDisPlay display, HTuple crossX, HTuple crossY, HTuple radius, string color)
        {
            display.HoWindow.DispCircle(crossX, crossY, radius, color);
        }
        public static void DispCircle(this Form_HWDisPlay display, DispDCircle dispDCircle)
        {
            display.HoWindow.DispCircle(dispDCircle);
        }

        #endregion

    }
}
