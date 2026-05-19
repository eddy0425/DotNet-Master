using DotNet.Drawing;
using HalconDotNet;
using System;
using System.Collections.Generic;

namespace DotNet.HalconUI
{
    public class HDisplay : IHDisplay
    {
        bool _disposed;
        string _color;

        HObject _hoImage;
        readonly HWindow _hWindow;
        readonly IHWindowFont _hWindowFont;
        readonly HWindowImage _hWindowImage;

        public bool IsCross { get; set; }           //是否画十字
        public bool Adaptive { get; set; } = true;   //自适应
        public double HoWidth => _hWindowImage.HoWidth;
        public double HoHeight => _hWindowImage.HoHeight;
        public HObject HoImage => _hWindowImage.HoImage;  //图像

        public HDisplay(HWindow hWindow, HWindowControl _hWindowControl)
        {
            HOperatorSet.GenEmptyObj(out _hoImage);

            _hWindow = hWindow;
            _hWindowFont = new HWindowFont2018(hWindow);
            _hWindowImage = new HWindowImage(hWindow, _hWindowControl);
        }

        public HWindowImage GetHWindowImage()
        {
            return _hWindowImage;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // 释放顺序：先解绑事件订阅，再释放本类持有所有权的图像。
            // 注意：hWindow / _hWindowControl 由宿主 UserControl (HDisplayUI) 通过 Designer 的 Dispose(bool) 负责释放，本类不再主动释放，避免双重释放。

            _hWindowImage?.Dispose();
            _hoImage?.Dispose();

            GC.SuppressFinalize(this);
        }

        #region HWindowImage

        /// <summary> 显示图片 </summary>
        public void DispImage(HObject image)
        {
            try
            {
                _hoImage.Dispose();
                HOperatorSet.CopyImage(image, out _hoImage);
                DispImage(_hoImage, Adaptive);
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

                double size = HoWidth > HoHeight ? HoWidth : HoHeight;
                HOperatorSet.DispCross(_hWindow, HoHeight / 2, HoWidth / 2, size, 0);
            }
        }

        /// <summary> 重新显示图片 </summary>
        public void ReDispImage()
        {
            _hWindowImage.Fun_ReDisplay();
        }

        #endregion

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

        /// <summary> 获取颜色 </summary>
        public string GetColor()
        {
            return _color;
        }

        /// <summary> 设置颜色 </summary>
        public void SetColor(string color)
        {
            // 确保 hWindow 不是 null
            if (_hWindow == null)
            {
                throw new ArgumentNullException(nameof(_hWindow), "HALCON window handle is null.");
            }

            // 使用 IsInitialized 检查 hWindow 是否有效
            if (!_hWindow.IsInitialized())
            {
                throw new InvalidOperationException("HALCON window handle is not initialized.");
            }

            // 确保颜色字符串有效
            if (color == null)
            {
                color = HColor.Red; // 默认颜色
            }

            _color = color;
            _hWindow.SetColor(color);
        }

        public void ClearWinDisp(HObject objectVal)
        {
            if (objectVal.NotNull())
            {
                _hWindow.ClearWindow();
                _hWindow.DispObj(objectVal);
            }
        }

        #region 区域相关

        /// <summary> 显示橡皮筋区域 </summary>
        public void DispGenRegion(CvRegion hRegion)
        {
            hRegion.GenRegion();
            _hWindow.DispObj(hRegion.HoRegion);
        }

        /// <summary> 获取坐标区域并显示 </summary>
        public void GenCoordsRegion(CvRegion hRegion, List<CvCoord> coords)
        {
            hRegion.GenCoordsRegion(coords);
            DispRegion(hRegion.HoRegion, HColor.Green);
        }

        #endregion

        #region 点相关
        public void DispPoint(double crossX, double crossY, double size = 20)
        {
            _hWindow.DispCross(crossY, crossX, size, 0);
        }
        public void DispPoint(double crossX, double crossY, string color, int size = 20)
        {
            SetColor(color);
            _hWindow.DispCross(crossY, crossX, size, 0);
        }
        public void DispPoint(HTuple crossX, HTuple crossY, double size = 20)
        {
            _hWindow.DispCross(crossY, crossX, size, 0);
        }
        public void DispPoint(HTuple crossX, HTuple crossY, string color, int size = 20)
        {
            SetColor(color);
            _hWindow.DispCross(crossY, crossX, size, 0);
        }
        public void DispPoint(double[] rowPoints, double[] columnPoints, int size = 20)
        {
            if (rowPoints.Length != columnPoints.Length) return;

            for (int i = 0; i < rowPoints.Length; i++)
            {
                _hWindow.DispCross(rowPoints[i], columnPoints[i], size, 0);
            }
        }
        public void DispPoint(double[] rowPoints, double[] columnPoints, string color, int size = 20)
        {
            SetColor(color);

            if (rowPoints.Length != columnPoints.Length) return;

            for (int i = 0; i < rowPoints.Length; i++)
            {
                _hWindow.DispCross(rowPoints[i], columnPoints[i], size, 0);
            }
        }
        public void DispPoint(List<Point2d> polygons, int size = 20)
        {
            if (polygons.Count == 0) return;

            for (int i = 0; i < polygons.Count; i++)
            {
                _hWindow.DispCross(polygons[i].Y, polygons[i].X, size, 0);
            }
        }
        public void DispPoint(List<Point2d> polygons, string color, int size = 20)
        {
            SetColor(color);

            if (polygons.Count == 0) return;

            for (int i = 0; i < polygons.Count; i++)
            {
                _hWindow.DispCross(polygons[i].Y, polygons[i].X, size, 0);
            }
        }
        public void DispPoint(Point2d point, double size = 20)
        {
            _hWindow.DispCross(point.Y, point.X, size, 0);
        }
        public void DispPoint(Point2d point, string color, double size = 20)
        {
            SetColor(color);
            _hWindow.DispCross(point.Y, point.X, size, 0);
        }

        #endregion

        #region 坐标相关
        public void DispCross(double crossX, double crossY, double angle, double size = 20)
        {
            _hWindow.DispCross(crossY, crossX, size, angle);
        }
        public void DispCross(double crossX, double crossY, double angle, string color, double size = 20)
        {
            SetColor(color);
            _hWindow.DispCross(crossY, crossX, size, angle);
        }
        public void DispCross(Point2d point, double angle, double size = 20)
        {
            _hWindow.DispCross(point.Y, point.X, size, angle);
        }
        public void DispCross(Point2d point, double angle, string color, double size = 20)
        {
            SetColor(color);
            _hWindow.DispCross(point.Y, point.X, size, angle);
        }
        public void DispCross(CvCoord coord, double size = 20)
        {
            _hWindow.DispCross(coord.Y, coord.X, size, coord.Angle.ToRadians());
        }
        public void DispCross(CvCoord coord, string color, double size = 20)
        {
            SetColor(color);
            _hWindow.DispCross(coord.Y, coord.X, size, coord.Angle.ToRadians());
        }

        #endregion

        #region 线相关
        public void DispLine(double startX, double startY, double endX, double endY)
        {
            _hWindow.DispLine(startY, startX, endY, endX);
        }
        public void DispLine(double startX, double startY, double endX, double endY, string color)
        {
            SetColor(color);
            _hWindow.DispLine(startY, startX, endY, endX);
        }
        public void DispLine(CvLine line)
        {
            _hWindow.DispLine(line.Start.Y, line.Start.X, line.End.Y, line.End.X);
        }
        public void DispLine(CvLine line, string color)
        {
            SetColor(color);
            _hWindow.DispLine(line.Start.Y, line.Start.X, line.End.Y, line.End.X);
        }
        public void DispLine(CvLine line, int radius)
        {
            _hWindow.DispLine(line.Start.Y, line.Start.X, line.End.Y, line.End.X);

            _hWindow.SetColor(HColor.Red);
            _hWindow.DispCircle(line.End.Y, line.End.X, radius);
        }
        public void DispLine(CvLine line, int radius, string color)
        {
            SetColor(color);
            _hWindow.DispLine(line.Start.Y, line.Start.X, line.End.Y, line.End.X);

            _hWindow.SetColor(HColor.Red);
            _hWindow.DispCircle(line.End.Y, line.End.X, radius);
        }


        /// <summary> 画两点一线 </summary>
        public void DispLine(Point2d point1, Point2d point2, int step)
        {
            {
                double x1 = point1.X - step;
                double y1 = point1.Y;
                double x2 = point1.X + step;
                double y2 = point1.Y;
                HOperatorSet.DispLine(_hWindow, y1, x1, y2, x2);

                double x3 = point1.X;
                double y3 = point1.Y - step;
                double x4 = point1.X;
                double y4 = point1.Y + step;
                HOperatorSet.DispLine(_hWindow, y3, x3, y4, x4);
            }
            {
                double x1 = point2.X - step;
                double y1 = point2.Y;
                double x2 = point2.X + step;
                double y2 = point2.Y;
                HOperatorSet.DispLine(_hWindow, y1, x1, y2, x2);

                double x3 = point2.X;
                double y3 = point2.Y - step;
                double x4 = point2.X;
                double y4 = point2.Y + step;
                HOperatorSet.DispLine(_hWindow, y3, x3, y4, x4);
            }

            HOperatorSet.DispLine(_hWindow, point1.Y, point1.X, point2.Y, point2.X);
        }

        /// <summary> 画两点一线 </summary>
        public void DispLine(Point2d point1, Point2d point2, int step, string color)
        {
            SetColor(color);

            {
                double x1 = point1.X - step;
                double y1 = point1.Y;
                double x2 = point1.X + step;
                double y2 = point1.Y;
                HOperatorSet.DispLine(_hWindow, y1, x1, y2, x2);

                double x3 = point1.X;
                double y3 = point1.Y - step;
                double x4 = point1.X;
                double y4 = point1.Y + step;
                HOperatorSet.DispLine(_hWindow, y3, x3, y4, x4);
            }
            {
                double x1 = point2.X - step;
                double y1 = point2.Y;
                double x2 = point2.X + step;
                double y2 = point2.Y;
                HOperatorSet.DispLine(_hWindow, y1, x1, y2, x2);

                double x3 = point2.X;
                double y3 = point2.Y - step;
                double x4 = point2.X;
                double y4 = point2.Y + step;
                HOperatorSet.DispLine(_hWindow, y3, x3, y4, x4);
            }

            HOperatorSet.DispLine(_hWindow, point1.Y, point1.X, point2.Y, point2.X);
        }

        #endregion

        #region 方向线
        public void DispArrow(double startX, double startY, double endX, double endY, double size = 20)
        {
            _hWindow.DispArrow(startY, startX, endY, endX, size);
        }
        public void DispArrow(double startX, double startY, double endX, double endY, string color, double size = 20)
        {
            SetColor(color);
            _hWindow.DispArrow(startY, startX, endY, endX, size);
        }
        public void DispArrow(CvLine line, double size = 20)
        {
            _hWindow.DispArrow(line.Start.Y, line.Start.X, line.End.Y, line.End.X, size);
        }
        public void DispArrow(CvLine line, string color, double size = 20)
        {
            SetColor(color);
            _hWindow.DispArrow(line.Start.Y, line.Start.X, line.End.Y, line.End.X, size);
        }
        public void DispArrow(CvArrow arrow)
        {
            _hWindow.DispArrow(arrow.Start.Y, arrow.Start.X, arrow.End.Y, arrow.End.X, arrow.HeadSize);
        }
        public void DispArrow(CvArrow arrow, string color)
        {
            SetColor(color);
            _hWindow.DispArrow(arrow.Start.Y, arrow.Start.X, arrow.End.Y, arrow.End.X, arrow.HeadSize);
        }

        #endregion

        #region 圆
        public void DispCircle(double crossX, double crossY, double radius)
        {
            _hWindow.DispCircle(crossY, crossX, radius);
        }
        public void DispCircle(double crossX, double crossY, double radius, string color)
        {
            SetColor(color);
            _hWindow.DispCircle(crossY, crossX, radius);
        }
        public void DispCircle(CvCircle circle)
        {
            _hWindow.DispCircle(circle.Center.Y, circle.Center.X, circle.Radius);
        }
        public void DispCircle(CvCircle circle, string color)
        {
            SetColor(color);
            _hWindow.DispCircle(circle.Center.Y, circle.Center.X, circle.Radius);
        }

        #endregion

        #region Draw Region

        /// <summary> 新建区域 </summary>
        public void DrawRegion(CvRegion hRegion)
        {
            DrawHelper.CancelDraw();

            switch (hRegion.Type)
            {
                case RectEnum.Rectangle:
                    {
                        DrawHelper.DrawRectangle1(_hWindow, out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2);
                        HOperatorSet.GenRectangle1(out HObject rectangle, row1, column1, row2, column2);

                        hRegion.Update2Point(row1, column1, row2, column2);
                        hRegion.HoRegion.Dispose();
                        hRegion.HoRegion = rectangle;
                    }
                    break;
                case RectEnum.AffRect:
                    {
                        DrawHelper.DrawRectangle2(_hWindow, out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2);
                        HOperatorSet.GenRectangle2(out HObject rectangle, row, column, phi, length1, length2);

                        hRegion.UpdateCenter(new Point2d(column.D, row.D), new Size2d(length1.D * 2, length2.D * 2));
                        hRegion.Phi = phi;
                        hRegion.HoRegion.Dispose();
                        hRegion.HoRegion = rectangle;
                    }
                    break;
                case RectEnum.Circle:
                    {
                        DrawHelper.DrawCircle(_hWindow, out HTuple row, out HTuple column, out HTuple radius);
                        HOperatorSet.GenCircle(out HObject circle, row, column, radius);

                        hRegion.UpdateCenter(new Point2d(column.D, row.D), new Size2d(radius.D * 2, radius.D * 2));
                        hRegion.HoRegion.Dispose();
                        hRegion.HoRegion = circle;
                    }
                    break;
                case RectEnum.Ellipse:
                    {
                        DrawHelper.DrawEllipse(_hWindow, out HTuple row, out HTuple column, out HTuple phi, out HTuple radius1, out HTuple radius2);
                        HOperatorSet.GenEllipse(out HObject ellipse, row, column, phi, radius1, radius2);

                        hRegion.UpdateCenter(new Point2d(column.D, row.D), new Size2d(radius1.D * 2, radius2.D * 2));
                        hRegion.Phi = phi;
                        hRegion.HoRegion.Dispose();
                        hRegion.HoRegion = ellipse;
                    }
                    break;
                case RectEnum.Polygon:
                    {
                        DrawHelper.DrawRegion(out HObject region, _hWindow);
                        HOperatorSet.GetRegionPolygon(region, 1, out HTuple rows, out HTuple columns);
                        HOperatorSet.AreaCenter(region, out HTuple area, out HTuple hv_Row, out HTuple hv_Column);
                        hRegion.PolygonX = columns;
                        hRegion.PolygonY = rows;
                        hRegion.Center = new Point2d(hv_Column.D, hv_Row.D);
                        hRegion.HoRegion.Dispose();
                        hRegion.HoRegion = region;
                    }
                    break;
            }
        }

        /// <summary> 修改区域 </summary>
        public void DrawRegionMod(CvRegion hRegion)
        {
            DrawHelper.CancelDraw();
            switch (hRegion.Type)
            {
                case RectEnum.Rectangle:
                    {
                        DrawHelper.DrawRectangle1Mod(_hWindow, hRegion.Top, hRegion.Left, hRegion.Bottom, hRegion.Right,
                                                  out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2);

                        HOperatorSet.GenRectangle1(out HObject rectangle, row1, column1, row2, column2);

                        hRegion.Update2Point(row1, column1, row2, column2);
                        hRegion.HoRegion.Dispose();
                        hRegion.HoRegion = rectangle;
                    }
                    break;
                case RectEnum.AffRect:
                    {
                        DrawHelper.DrawRectangle2Mod(_hWindow, hRegion.CenterY, hRegion.CenterX, hRegion.Phi,
                                                hRegion.Width / 2, hRegion.Height / 2,
                                                out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2);
                        HOperatorSet.GenRectangle2(out HObject rectangle, row, column, phi, length1, length2);

                        hRegion.UpdateCenter(new Point2d(column.D, row.D), new Size2d(length1.D * 2, length2.D * 2));
                        hRegion.Phi = phi;
                        hRegion.HoRegion.Dispose();
                        hRegion.HoRegion = rectangle;
                    }
                    break;
                case RectEnum.Circle:
                    {
                        DrawHelper.DrawCircleMod(_hWindow, hRegion.CenterY, hRegion.CenterX, hRegion.Width / 2,
                                           out HTuple row, out HTuple column, out HTuple radius);
                        HOperatorSet.GenCircle(out HObject circle, row, column, radius);

                        hRegion.UpdateCenter(new Point2d(column.D, row.D), new Size2d(radius.D * 2, radius.D * 2));
                        hRegion.HoRegion.Dispose();
                        hRegion.HoRegion = circle;
                    }
                    break;
                case RectEnum.Ellipse:
                    {
                        DrawHelper.DrawEllipseMod(_hWindow, hRegion.CenterY, hRegion.CenterX, hRegion.Phi,
                                                     hRegion.Width / 2, hRegion.Height / 2,
                                                     out HTuple row, out HTuple column, out HTuple phi, out HTuple radius1, out HTuple radius2);
                        HOperatorSet.GenEllipse(out HObject ellipse, row, column, phi, radius1, radius2);

                        hRegion.UpdateCenter(new Point2d(column.D, row.D), new Size2d(radius1.D * 2, radius2.D * 2));
                        hRegion.Phi = phi;
                        hRegion.HoRegion.Dispose();
                        hRegion.HoRegion = ellipse;
                    }
                    break;
                case RectEnum.Polygon:
                    {
                        DrawHelper.DrawRegion(out HObject region, _hWindow);
                        HOperatorSet.GetRegionPolygon(region, 1, out HTuple rows, out HTuple columns);
                        HOperatorSet.AreaCenter(region, out HTuple area, out HTuple hv_Row, out HTuple hv_Column);
                        hRegion.PolygonX = columns;
                        hRegion.PolygonY = rows;
                        hRegion.Center = new Point2d(hv_Column.D, hv_Row.D);
                        hRegion.HoRegion.Dispose();
                        hRegion.HoRegion = region;
                    }
                    break;
            }
        }

        /// <summary> 新建区域 </summary>
        public void DrawRegion(RectEnum type, out HObject rectangle)
        {
            DrawHelper.CancelDraw();
            HOperatorSet.GenEmptyObj(out rectangle);

            switch (type)
            {
                case RectEnum.Rectangle:
                    {
                        DrawHelper.DrawRectangle1(_hWindow, out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2);
                        HOperatorSet.GenRectangle1(out rectangle, row1, column1, row2, column2);
                    }
                    break;
                case RectEnum.AffRect:
                    {
                        DrawHelper.DrawRectangle2(_hWindow, out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2);
                        HOperatorSet.GenRectangle2(out rectangle, row, column, phi, length1, length2);
                    }
                    break;
                case RectEnum.Circle:
                    {
                        DrawHelper.DrawCircle(_hWindow, out HTuple row, out HTuple column, out HTuple radius);
                        HOperatorSet.GenCircle(out rectangle, row, column, radius);
                    }
                    break;
                case RectEnum.Ellipse:
                    {
                        DrawHelper.DrawEllipse(_hWindow, out HTuple row, out HTuple column, out HTuple phi, out HTuple radius1, out HTuple radius2);
                        HOperatorSet.GenEllipse(out rectangle, row, column, phi, radius1, radius2);
                    }
                    break;
                case RectEnum.Polygon:
                    {
                        DrawHelper.DrawRegion(out rectangle, _hWindow);
                    }
                    break;
            }
        }

        #endregion

        #region Region

        /// <summary> 显示ROI区域 </summary>
        public void DispRegion(HObject hRegion)
        {
            if (hRegion.NotNull())
                _hWindow.DispObj(hRegion);
        }

        /// <summary> 显示ROI区域 </summary>
        public void DispRegion(HObject hRegion, string color)
        {
            SetColor(color);

            if (hRegion.NotNull())
                _hWindow.DispObj(hRegion);
        }

        /// <summary> 显示橡皮筋区域 </summary>
        public void DispRegion(CvRegion hRegion)
        {
            if (hRegion.HoRegion.NotNull())
                _hWindow.DispObj(hRegion.HoRegion);
        }

        /// <summary> 显示橡皮筋区域 </summary>
        public void DispRegion(CvRegion hRegion, string color)
        {
            SetColor(color);

            if (hRegion.HoRegion.NotNull())
                _hWindow.DispObj(hRegion.HoRegion);
        }

        /// <summary> 显示橡皮筋区域 </summary>
        public void DispCvRegion(CvRegion hRegion)
        {
            switch (hRegion.Type)
            {
                case RectEnum.Rectangle:
                    {
                        HOperatorSet.DispRectangle1(_hWindow, hRegion.Top + 0.5, hRegion.Left + 1,
                                                            hRegion.Bottom, hRegion.Right);
                    }
                    break;
                case RectEnum.AffRect:
                    {
                        HOperatorSet.DispRectangle2(_hWindow, hRegion.CenterY + 0.5, hRegion.CenterX + 1, hRegion.Phi,
                                                 hRegion.Width / 2, hRegion.Height / 2);
                    }
                    break;
                case RectEnum.Circle:
                    {
                        HOperatorSet.DispCircle(_hWindow, hRegion.CenterY + 0.5, hRegion.CenterX + 1, hRegion.Width / 2);
                    }
                    break;
                case RectEnum.Ellipse:
                    {
                        HOperatorSet.DispEllipse(_hWindow, hRegion.CenterY + 0.5, hRegion.CenterX + 1, hRegion.Phi,
                                                 hRegion.Width / 2, hRegion.Height / 2);
                    }
                    break;
                case RectEnum.Polygon:
                    {
                        HOperatorSet.DispPolygon(_hWindow, hRegion.PolygonX, hRegion.PolygonY);
                    }
                    break;
                case RectEnum.Ring:
                    {
                        HObject circle1 = new HObject(); HOperatorSet.GenEmptyObj(out circle1);
                        HObject circle2 = new HObject(); HOperatorSet.GenEmptyObj(out circle2);
                        try
                        {
                            HOperatorSet.GenCircle(out circle1, hRegion.CenterY + 0.5, hRegion.CenterX + 1, hRegion.MaxRadius);
                            HOperatorSet.GenCircle(out circle2, hRegion.CenterY + 0.5, hRegion.CenterX + 1, hRegion.MinRadius);
                            HOperatorSet.Difference(circle1, circle2, out HObject regionDifference);
                            hRegion.HoRegion.Dispose();
                            hRegion.HoRegion = regionDifference;
                            _hWindow.DispObj(hRegion.HoRegion);
                        }
                        finally
                        {
                            circle1.Dispose();
                            circle2.Dispose();
                        }
                    }
                    break;
            }
        }

        public void DispRectangle2(HTuple centerRow, HTuple centerCol, HTuple phi, HTuple length1, HTuple length2)
        {
            _hWindow.DispRectangle2(centerRow, centerCol, phi, length1, length2);
        }

        public void DispRectangle2(HTuple centerRow, HTuple centerCol, HTuple phi, HTuple length1, HTuple length2, string color)
        {
            SetColor(color);
            _hWindow.DispRectangle2(centerRow, centerCol, phi, length1, length2);
        }

        #endregion

    }
}
