using DotNet.Drawing;
using HalconDotNet;
using System.Collections.Generic;

namespace DotNet.HalconUI
{
    public interface IHDisplay
    {
        /// <summary> 获取颜色 </summary>
        string GetColor();

        /// <summary> 设置颜色 </summary>
        void SetColor(HWindow hWindow, string color);

        void ClearWinDisp(HWindow hWindow, HObject objectVal);

        #region 区域相关

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        void DispGenRegion(HWindow hWindow, CvRegion hRegion);

        /// <summary>
        /// 获取坐标区域并显示
        /// </summary>
        void GenCoordsRegion(HWindow hWindow, CvRegion hRegion, List<CvCoord> coords);

        #endregion

        #region 点相关
        void DispPoint(HWindow hWindow, double crossX, double crossY, double size = 20);
        void DispPoint(HWindow hWindow, double crossX, double crossY, string color, int size = 20);
        void DispPoint(HWindow hWindow, HTuple crossX, HTuple crossY, double size = 20);
        void DispPoint(HWindow hWindow, HTuple crossX, HTuple crossY, string color, int size = 20);
        void DispPoint(HWindow hWindow, double[] rowPoints, double[] columnPoints, int size = 20);
        void DispPoint(HWindow hWindow, double[] rowPoints, double[] columnPoints, string color, int size = 20);
        void DispPoint(HWindow hWindow, List<Point2d> polygons, int size = 20);
        void DispPoint(HWindow hWindow, List<Point2d> polygons, string color, int size = 20);
        void DispPoint(HWindow hWindow, Point2d point, double size = 20);
        void DispPoint(HWindow hWindow, Point2d point, string color, double size = 20);

        #endregion

        #region 坐标相关
        void DispCross(HWindow hWindow, double crossX, double crossY, double angle, double size = 20);
        void DispCross(HWindow hWindow, double crossX, double crossY, double angle, string color, double size = 20);
        void DispCross(HWindow hWindow, Point2d point, double angle, double size = 20);
        void DispCross(HWindow hWindow, Point2d point, double angle, string color, double size = 20);
        void DispCross(HWindow hWindow, CvCoord coord, double size = 20);
        void DispCross(HWindow hWindow, CvCoord coord, string color, double size = 20);

        #endregion

        #region 线相关
        void DispLine(HWindow hWindow, double startX, double startY, double endX, double endY);
        void DispLine(HWindow hWindow, double startX, double startY, double endX, double endY, string color);
        void DispLine(HWindow hWindow, CvLine line);
        void DispLine(HWindow hWindow, CvLine line, string color);
        void DispLine(HWindow hWindow, CvLine hLine, int radius);
        void DispLine(HWindow hWindow, CvLine hLine, int radius, string color);
        void DispLine(HWindow hWindow, Point2d point1, Point2d point2, int step);
        void DispLine(HWindow hWindow, Point2d point1, Point2d point2, int step, string color);

        #endregion

        #region 方向线
        void DispArrow(HWindow hWindow, double startX, double startY, double endX, double endY, double size = 20);
        void DispArrow(HWindow hWindow, double startX, double startY, double endX, double endY, string color, double size = 20);
        void DispArrow(HWindow hWindow, CvLine line, double size = 20);
        void DispArrow(HWindow hWindow, CvLine line, string color, double size = 20);
        void DispArrow(HWindow hWindow, CvArrow arrow);
        void DispArrow(HWindow hWindow, CvArrow arrow, string color);

        #endregion

        #region 圆
        void DispCircle(HWindow hWindow, double crossX, double crossY, double radius);
        void DispCircle(HWindow hWindow, double crossX, double crossY, double radius, string color);
        void DispCircle(HWindow hWindow, CvCircle circle);
        void DispCircle(HWindow hWindow, CvCircle circle, string color);

        #endregion

        #region Region

        /// <summary>
        /// 显示ROI区域
        /// </summary>
        void DispRegion(HWindow hWindow, HObject hRegion);

        /// <summary>
        /// 显示ROI区域
        /// </summary>
        void DispRegion(HWindow hWindow, HObject hRegion, string color);

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        void DispRegion(HWindow hWindow, CvRegion hRegion);

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        void DispRegion(HWindow hWindow, CvRegion hRegion, string color);

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        void DispCvRegion(HWindow hWindow, CvRegion hRegion);

        /// <summary>
        /// 绘制（创建）橡皮筋区域
        /// </summary>
        void DrawRegion(HWindow hWindow, CvRegion hRegion);

        /// <summary>
        /// 绘制（创建）橡皮筋区域
        /// </summary>
        void DrawRegionMod(HWindow hWindow, CvRegion hRegion);

        /// <summary>
        /// 绘制（创建）橡皮筋区域
        /// </summary>
        void DrawRegionMod2(HWindow hWindow, CvRegion hRegion);

        void DispRectangle2(HWindow hWindow, HTuple centerRow, HTuple centerCol, HTuple phi, HTuple length1, HTuple length2);
        void DispRectangle2(HWindow hWindow, HTuple centerRow, HTuple centerCol, HTuple phi, HTuple length1, HTuple length2, string color);

        #endregion
    }
}
