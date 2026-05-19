using DotNet.Drawing;
using HalconDotNet;
using System;
using System.Collections.Generic;

namespace DotNet.HalconUI
{
    public interface IHDisplay : IDisposable
    {
        bool IsCross { get; set; }      //是否画十字
        bool Adaptive { get; set; }     //自适应
        double HoWidth { get; }
        double HoHeight { get; }
        HObject HoImage { get; }

        #region HWindowImage

        /// <summary> 显示图片 </summary>
        void DispImage(HObject image);

        /// <summary> 显示图片 </summary>
        void DispImage(HObject image, bool isSetPart);

        /// <summary> 重新显示图片 </summary>
        void ReDispImage();

        #endregion

        #region WindowFont

        /// <summary> 设置字体大小 </summary>
        void SetFontSize(HTuple hv_Size);

        /// <summary> 显示字体 </summary>
        void DispText(string message, HTuple FontX, HTuple FontY, string color);

        /// <summary> 显示字体 </summary>
        void DispText(string message, HTuple FontX, HTuple FontY, HTuple size, string color);

        #endregion

        /// <summary> 获取颜色 </summary>
        string GetColor();

        /// <summary> 设置颜色 </summary>
        void SetColor(string color);

        void ClearWinDisp(HObject objectVal);

        #region 区域相关

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        void DispGenRegion(CvRegion hRegion);

        /// <summary>
        /// 获取坐标区域并显示
        /// </summary>
        void GenCoordsRegion(CvRegion hRegion, List<CvCoord> coords);

        #endregion

        #region 点相关
        void DispPoint(double crossX, double crossY, double size = 20);
        void DispPoint(double crossX, double crossY, string color, int size = 20);
        void DispPoint(HTuple crossX, HTuple crossY, double size = 20);
        void DispPoint(HTuple crossX, HTuple crossY, string color, int size = 20);
        void DispPoint(double[] rowPoints, double[] columnPoints, int size = 20);
        void DispPoint(double[] rowPoints, double[] columnPoints, string color, int size = 20);
        void DispPoint(List<Point2d> polygons, int size = 20);
        void DispPoint(List<Point2d> polygons, string color, int size = 20);
        void DispPoint(Point2d point, double size = 20);
        void DispPoint(Point2d point, string color, double size = 20);

        #endregion

        #region 坐标相关
        void DispCross(double crossX, double crossY, double angle, double size = 20);
        void DispCross(double crossX, double crossY, double angle, string color, double size = 20);
        void DispCross(Point2d point, double angle, double size = 20);
        void DispCross(Point2d point, double angle, string color, double size = 20);
        void DispCross(CvCoord coord, double size = 20);
        void DispCross(CvCoord coord, string color, double size = 20);

        #endregion

        #region 线相关
        void DispLine(double startX, double startY, double endX, double endY);
        void DispLine(double startX, double startY, double endX, double endY, string color);
        void DispLine(CvLine line);
        void DispLine(CvLine line, string color);
        void DispLine(CvLine hLine, int radius);
        void DispLine(CvLine hLine, int radius, string color);
        void DispLine(Point2d point1, Point2d point2, int step);
        void DispLine(Point2d point1, Point2d point2, int step, string color);

        #endregion

        #region 方向线
        void DispArrow(double startX, double startY, double endX, double endY, double size = 20);
        void DispArrow(double startX, double startY, double endX, double endY, string color, double size = 20);
        void DispArrow(CvLine line, double size = 20);
        void DispArrow(CvLine line, string color, double size = 20);
        void DispArrow(CvArrow arrow);
        void DispArrow(CvArrow arrow, string color);

        #endregion

        #region 圆
        void DispCircle(double crossX, double crossY, double radius);
        void DispCircle(double crossX, double crossY, double radius, string color);
        void DispCircle(CvCircle circle);
        void DispCircle(CvCircle circle, string color);

        #endregion

        #region Draw Region

        /// <summary> 绘制（创建）橡皮筋区域 </summary>
        void DrawRegion(CvRegion hRegion);

        /// <summary> 绘制（修改）橡皮筋区域 </summary>
        void DrawRegionMod(CvRegion hRegion);

        /// <summary> 绘制（创建）橡皮筋区域 </summary>
        void DrawRegion(RectEnum type, out HObject rectangle);

        #endregion

        #region Region

        /// <summary>
        /// 显示ROI区域
        /// </summary>
        void DispRegion(HObject hRegion);

        /// <summary>
        /// 显示ROI区域
        /// </summary>
        void DispRegion(HObject hRegion, string color);

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        void DispRegion(CvRegion hRegion);

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        void DispRegion(CvRegion hRegion, string color);

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        void DispCvRegion(CvRegion hRegion);


        void DispRectangle2(HTuple centerRow, HTuple centerCol, HTuple phi, HTuple length1, HTuple length2);
        void DispRectangle2(HTuple centerRow, HTuple centerCol, HTuple phi, HTuple length1, HTuple length2, string color);

        #endregion
    }
}
