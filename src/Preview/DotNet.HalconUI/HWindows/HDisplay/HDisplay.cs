using DotNet.Drawing;
using HalconDotNet;
using System;
using System.Collections.Generic;

namespace DotNet.HalconUI
{
    public class HDisplay : IHDisplay
    {
        string _color;

        /// <summary> 获取颜色 </summary>
        public string GetColor()
        {
            return _color;
        }

        /// <summary> 设置颜色 </summary>
        public void SetColor(HWindow hWindow, string color)
        {
            // 确保 hWindow 不是 null
            if (hWindow == null)
            {
                throw new ArgumentNullException(nameof(hWindow), "HALCON window handle is null.");
            }

            // 使用 IsInitialized 检查 hWindow 是否有效
            if (!hWindow.IsInitialized())
            {
                throw new InvalidOperationException("HALCON window handle is not initialized.");
            }

            // 确保颜色字符串有效
            if (color == null)
            {
                color = HColor.Red; // 默认颜色
            }

            _color = color;
            hWindow.SetColor(color);
        }

        public void ClearWinDisp(HWindow hWindow, HObject objectVal)
        {
            if (objectVal.NotNull())
            {
                hWindow.ClearWindow();
                hWindow.DispObj(objectVal);
            }
        }

        #region 区域相关

        /// <summary> 显示橡皮筋区域 </summary>
        public void DispGenRegion(HWindow hWindow, CvRegion hRegion)
        {
            hRegion.GenRegion();
            hWindow.DispObj(hRegion.HoRegion);
        }

        /// <summary> 获取坐标区域并显示 </summary>
        public void GenCoordsRegion(HWindow hWindow, CvRegion hRegion, List<CvCoord> coords)
        {
            hRegion.GenCoordsRegion(coords);
            DispRegion(hWindow, hRegion.HoRegion, HColor.Green);
        }

        #endregion

        #region 点相关
        public void DispPoint(HWindow hWindow, double crossX, double crossY, double size = 20)
        {
            hWindow.DispCross(crossY, crossX, size, 0);
        }
        public void DispPoint(HWindow hWindow, double crossX, double crossY, string color, int size = 20)
        {
            SetColor(hWindow, color);
            hWindow.DispCross(crossY, crossX, size, 0);
        }
        public void DispPoint(HWindow hWindow, HTuple crossX, HTuple crossY, double size = 20)
        {
            hWindow.DispCross(crossY, crossX, size, 0);
        }
        public void DispPoint(HWindow hWindow, HTuple crossX, HTuple crossY, string color, int size = 20)
        {
            SetColor(hWindow, color);
            hWindow.DispCross(crossY, crossX, size, 0);
        }
        public void DispPoint(HWindow hWindow, double[] rowPoints, double[] columnPoints, int size = 20)
        {
            if (rowPoints.Length != columnPoints.Length) return;

            for (int i = 0; i < rowPoints.Length; i++)
            {
                hWindow.DispCross(rowPoints[i], columnPoints[i], size, 0);
            }
        }
        public void DispPoint(HWindow hWindow, double[] rowPoints, double[] columnPoints, string color, int size = 20)
        {
            SetColor(hWindow, color);

            if (rowPoints.Length != columnPoints.Length) return;

            for (int i = 0; i < rowPoints.Length; i++)
            {
                hWindow.DispCross(rowPoints[i], columnPoints[i], size, 0);
            }
        }
        public void DispPoint(HWindow hWindow, List<Point2d> polygons, int size = 20)
        {
            if (polygons.Count == 0) return;

            for (int i = 0; i < polygons.Count; i++)
            {
                hWindow.DispCross(polygons[i].Y, polygons[i].X, size, 0);
            }
        }
        public void DispPoint(HWindow hWindow, List<Point2d> polygons, string color, int size = 20)
        {
            SetColor(hWindow, color);

            if (polygons.Count == 0) return;

            for (int i = 0; i < polygons.Count; i++)
            {
                hWindow.DispCross(polygons[i].Y, polygons[i].X, size, 0);
            }
        }
        public void DispPoint(HWindow hWindow, Point2d point, double size = 20)
        {
            hWindow.DispCross(point.Y, point.X, size, 0);
        }
        public void DispPoint(HWindow hWindow, Point2d point, string color, double size = 20)
        {
            SetColor(hWindow, color);
            hWindow.DispCross(point.Y, point.X, size, 0);
        }

        #endregion

        #region 坐标相关
        public void DispCross(HWindow hWindow, double crossX, double crossY, double angle, double size = 20)
        {
            hWindow.DispCross(crossY, crossX, size, angle);
        }
        public void DispCross(HWindow hWindow, double crossX, double crossY, double angle, string color, double size = 20)
        {
            SetColor(hWindow, color);
            hWindow.DispCross(crossY, crossX, size, angle);
        }
        public void DispCross(HWindow hWindow, Point2d point, double angle, double size = 20)
        {
            hWindow.DispCross(point.Y, point.X, size, angle);
        }
        public void DispCross(HWindow hWindow, Point2d point, double angle, string color, double size = 20)
        {
            SetColor(hWindow, color);
            hWindow.DispCross(point.Y, point.X, size, angle);
        }
        public void DispCross(HWindow hWindow, CvCoord coord, double size = 20)
        {
            hWindow.DispCross(coord.Y, coord.X, size, coord.Angle.ToRadians());
        }
        public void DispCross(HWindow hWindow, CvCoord coord, string color, double size = 20)
        {
            SetColor(hWindow, color);
            hWindow.DispCross(coord.Y, coord.X, size, coord.Angle.ToRadians());
        }

        #endregion

        #region 线相关
        public void DispLine(HWindow hWindow, double startX, double startY, double endX, double endY)
        {
            hWindow.DispLine(startY, startX, endY, endX);
        }
        public void DispLine(HWindow hWindow, double startX, double startY, double endX, double endY, string color)
        {
            SetColor(hWindow, color);
            hWindow.DispLine(startY, startX, endY, endX);
        }
        public void DispLine(HWindow hWindow, CvLine line)
        {
            hWindow.DispLine(line.Start.Y, line.Start.X, line.End.Y, line.End.X);
        }
        public void DispLine(HWindow hWindow, CvLine line, string color)
        {
            SetColor(hWindow, color);
            hWindow.DispLine(line.Start.Y, line.Start.X, line.End.Y, line.End.X);
        }
        public void DispLine(HWindow hWindow, CvLine line, int radius)
        {
            hWindow.DispLine(line.Start.Y, line.Start.X, line.End.Y, line.End.X);

            hWindow.SetColor(HColor.Red);
            hWindow.DispCircle(line.End.Y, line.End.X, radius);
        }
        public void DispLine(HWindow hWindow, CvLine line, int radius, string color)
        {
            SetColor(hWindow, color);
            hWindow.DispLine(line.Start.Y, line.Start.X, line.End.Y, line.End.X);

            hWindow.SetColor(HColor.Red);
            hWindow.DispCircle(line.End.Y, line.End.X, radius);
        }


        /// <summary> 画两点一线 </summary>
        public void DispLine(HWindow hWindow, Point2d point1, Point2d point2, int step)
        {
            {
                double x1 = point1.X - step;
                double y1 = point1.Y;
                double x2 = point1.X + step;
                double y2 = point1.Y;
                HOperatorSet.DispLine(hWindow, y1, x1, y2, x2);

                double x3 = point1.X;
                double y3 = point1.Y - step;
                double x4 = point1.X;
                double y4 = point1.Y + step;
                HOperatorSet.DispLine(hWindow, y3, x3, y4, x4);
            }
            {
                double x1 = point2.X - step;
                double y1 = point2.Y;
                double x2 = point2.X + step;
                double y2 = point2.Y;
                HOperatorSet.DispLine(hWindow, y1, x1, y2, x2);

                double x3 = point2.X;
                double y3 = point2.Y - step;
                double x4 = point2.X;
                double y4 = point2.Y + step;
                HOperatorSet.DispLine(hWindow, y3, x3, y4, x4);
            }

            HOperatorSet.DispLine(hWindow, point1.Y, point1.X, point2.Y, point2.X);
        }

        /// <summary> 画两点一线 </summary>
        public void DispLine(HWindow hWindow, Point2d point1, Point2d point2, int step, string color)
        {
            SetColor(hWindow, color);

            {
                double x1 = point1.X - step;
                double y1 = point1.Y;
                double x2 = point1.X + step;
                double y2 = point1.Y;
                HOperatorSet.DispLine(hWindow, y1, x1, y2, x2);

                double x3 = point1.X;
                double y3 = point1.Y - step;
                double x4 = point1.X;
                double y4 = point1.Y + step;
                HOperatorSet.DispLine(hWindow, y3, x3, y4, x4);
            }
            {
                double x1 = point2.X - step;
                double y1 = point2.Y;
                double x2 = point2.X + step;
                double y2 = point2.Y;
                HOperatorSet.DispLine(hWindow, y1, x1, y2, x2);

                double x3 = point2.X;
                double y3 = point2.Y - step;
                double x4 = point2.X;
                double y4 = point2.Y + step;
                HOperatorSet.DispLine(hWindow, y3, x3, y4, x4);
            }

            HOperatorSet.DispLine(hWindow, point1.Y, point1.X, point2.Y, point2.X);
        }

        #endregion

        #region 方向线
        public void DispArrow(HWindow hWindow, double startX, double startY, double endX, double endY, double size = 20)
        {
            hWindow.DispArrow(startY, startX, endY, endX, size);
        }
        public void DispArrow(HWindow hWindow, double startX, double startY, double endX, double endY, string color, double size = 20)
        {
            SetColor(hWindow, color);
            hWindow.DispArrow(startY, startX, endY, endX, size);
        }
        public void DispArrow(HWindow hWindow, CvLine line, double size = 20)
        {
            hWindow.DispArrow(line.Start.Y, line.Start.X, line.End.Y, line.End.X, size);
        }
        public void DispArrow(HWindow hWindow, CvLine line, string color, double size = 20)
        {
            SetColor(hWindow, color);
            hWindow.DispArrow(line.Start.Y, line.Start.X, line.End.Y, line.End.X, size);
        }
        public void DispArrow(HWindow hWindow, CvArrow arrow)
        {
            hWindow.DispArrow(arrow.Start.Y, arrow.Start.X, arrow.End.Y, arrow.End.X, arrow.HeadSize);
        }
        public void DispArrow(HWindow hWindow, CvArrow arrow, string color)
        {
            SetColor(hWindow, color);
            hWindow.DispArrow(arrow.Start.Y, arrow.Start.X, arrow.End.Y, arrow.End.X, arrow.HeadSize);
        }

        #endregion

        #region 圆
        public void DispCircle(HWindow hWindow, double crossX, double crossY, double radius)
        {
            hWindow.DispCircle(crossY, crossX, radius);
        }
        public void DispCircle(HWindow hWindow, double crossX, double crossY, double radius, string color)
        {
            SetColor(hWindow, color);
            hWindow.DispCircle(crossY, crossX, radius);
        }
        public void DispCircle(HWindow hWindow, CvCircle circle)
        {
            hWindow.DispCircle(circle.Center.Y, circle.Center.X, circle.Radius);
        }
        public void DispCircle(HWindow hWindow, CvCircle circle, string color)
        {
            SetColor(hWindow, color);
            hWindow.DispCircle(circle.Center.Y, circle.Center.X, circle.Radius);
        }

        #endregion

        #region Region

        /// <summary> 显示ROI区域 </summary>
        public void DispRegion(HWindow hWindow, HObject hRegion)
        {
            if (hRegion.NotNull())
                hWindow.DispObj(hRegion);
        }

        /// <summary> 显示ROI区域 </summary>
        public void DispRegion(HWindow hWindow, HObject hRegion, string color)
        {
            SetColor(hWindow, color);

            if (hRegion.NotNull())
                hWindow.DispObj(hRegion);
        }

        /// <summary> 显示橡皮筋区域 </summary>
        public void DispRegion(HWindow hWindow, CvRegion hRegion)
        {
            if (hRegion.HoRegion.NotNull())
                hWindow.DispObj(hRegion.HoRegion);
        }

        /// <summary> 显示橡皮筋区域 </summary>
        public void DispRegion(HWindow hWindow, CvRegion hRegion, string color)
        {
            SetColor(hWindow, color);

            if (hRegion.HoRegion.NotNull())
                hWindow.DispObj(hRegion.HoRegion);
        }

        /// <summary> 显示橡皮筋区域 </summary>
        public void DispCvRegion(HWindow hWindow, CvRegion hRegion)
        {
            switch (hRegion.Type)
            {
                case RectEnum.Rectangle:
                    {
                        HOperatorSet.DispRectangle1(hWindow, hRegion.Top + 0.5, hRegion.Left + 1,
                                                            hRegion.Bottom, hRegion.Right);
                    }
                    break;
                case RectEnum.AffRect:
                    {
                        HOperatorSet.DispRectangle2(hWindow, hRegion.CenterY + 0.5, hRegion.CenterX + 1, hRegion.Phi,
                                                 hRegion.Width / 2, hRegion.Height / 2);
                    }
                    break;
                case RectEnum.Circle:
                    {
                        HOperatorSet.DispCircle(hWindow, hRegion.CenterY + 0.5, hRegion.CenterX + 1, hRegion.Width / 2);
                    }
                    break;
                case RectEnum.Ellipse:
                    {
                        HOperatorSet.DispEllipse(hWindow, hRegion.CenterY + 0.5, hRegion.CenterX + 1, hRegion.Phi,
                                                 hRegion.Width / 2, hRegion.Height / 2);
                    }
                    break;
                case RectEnum.Polygon:
                    {
                        HOperatorSet.DispPolygon(hWindow, hRegion.PolygonX, hRegion.PolygonY);
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
                            hWindow.DispObj(hRegion.HoRegion);
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

        /// <summary> 绘制（创建）橡皮筋区域 </summary>
        public void DrawRegion(HWindow hWindow, CvRegion hRegion)
        {
            HalconAPI.CancelDraw();

            switch (hRegion.Type)
            {
                case RectEnum.Rectangle:
                    {
                        HOperatorSet.DrawRectangle1(hWindow, out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2);
                        HOperatorSet.GenRectangle1(out HObject rectangle, row1, column1, row2, column2);
                        hRegion.HoRegion.Dispose();
                        hRegion.HoRegion = rectangle;
                        hRegion.Update2Point(row1, column1, row2, column2);
                    }
                    break;
                case RectEnum.AffRect:
                    {
                        HOperatorSet.DrawRectangle2(hWindow, out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2);
                        HOperatorSet.GenRectangle2(out HObject rectangle, row, column, phi, length1, length2);
                        hRegion.HoRegion.Dispose();
                        hRegion.HoRegion = rectangle;
                        hRegion.UpdateCenter(new Point2d(column.D, row.D), new Size2d(length1.D * 2, length2.D * 2));
                        hRegion.Phi = phi;
                    }
                    break;
                case RectEnum.Ring:
                case RectEnum.Circle:
                    {
                        HOperatorSet.DrawCircle(hWindow, out HTuple row, out HTuple column, out HTuple radius);
                        HOperatorSet.GenCircle(out HObject circle, row, column, radius);
                        hRegion.HoRegion.Dispose();
                        hRegion.HoRegion = circle;
                        hRegion.UpdateCenter(new Point2d(column.D, row.D), new Size2d(radius.D * 2, radius.D * 2));
                    }
                    break;
                case RectEnum.Ellipse:
                    {
                        HOperatorSet.DrawEllipse(hWindow, out HTuple row, out HTuple column, out HTuple phi, out HTuple radius1, out HTuple radius2);
                        HOperatorSet.GenEllipse(out HObject ellipse, row, column, phi, radius1, radius2);
                        hRegion.HoRegion.Dispose();
                        hRegion.HoRegion = ellipse;
                        hRegion.UpdateCenter(new Point2d(column.D, row.D), new Size2d(radius1.D * 2, radius2.D * 2));
                        hRegion.Phi = phi;
                    }
                    break;
                case RectEnum.Polygon:
                    {
                        HOperatorSet.DrawRegion(out HObject region, hWindow);
                        hRegion.HoRegion.Dispose();
                        hRegion.HoRegion = region;
                        HOperatorSet.GetRegionPolygon(hRegion.HoRegion, 1, out HTuple rows, out HTuple columns);
                        hRegion.PolygonX = rows;
                        hRegion.PolygonY = columns;
                        HOperatorSet.AreaCenter(hRegion.HoRegion, out HTuple area, out HTuple hv_Row, out HTuple hv_Column);
                        hRegion.Center = new Point2d(hv_Row.D, hv_Column.D);
                    }
                    break;
            }
        }

        /// <summary> 绘制（创建）橡皮筋区域 </summary>
        public void DrawRegionMod(HWindow hWindow, CvRegion hRegion)
        {
            HalconAPI.CancelDraw();

            switch (hRegion.Type)
            {
                case RectEnum.Rectangle:
                    {
                        HOperatorSet.DrawRectangle1Mod(hWindow, hRegion.Top, hRegion.Left, hRegion.Bottom, hRegion.Right,
                                                  out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2);

                        HOperatorSet.GenRectangle1(out HObject rectangle, row1, column1, row2, column2);
                        hRegion.HoRegion.Dispose();
                        hRegion.HoRegion = rectangle;
                        hRegion.Update2Point(row1, column1, row2, column2);
                    }
                    break;
                case RectEnum.AffRect:
                    {
                        HOperatorSet.DrawRectangle2Mod(hWindow, hRegion.CenterY, hRegion.CenterX, hRegion.Phi,
                                                 hRegion.Width / 2, hRegion.Height / 2,
                                                 out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2);
                        HOperatorSet.GenRectangle2(out HObject rectangle, row, column, phi, length1, length2);
                        hRegion.HoRegion.Dispose();
                        hRegion.HoRegion = rectangle;
                        hRegion.UpdateCenter(new Point2d(column.D, row.D), new Size2d(length1.D * 2, length2.D * 2));
                        hRegion.Phi = phi;
                    }
                    break;
                case RectEnum.Ring:
                case RectEnum.Circle:
                    {
                        HOperatorSet.DrawCircleMod(hWindow, hRegion.CenterY, hRegion.CenterX, hRegion.Width / 2,
                                                  out HTuple row, out HTuple column, out HTuple radius);
                        HOperatorSet.GenCircle(out HObject circle, row, column, radius);
                        hRegion.HoRegion.Dispose();
                        hRegion.HoRegion = circle;
                        hRegion.UpdateCenter(new Point2d(column.D, row.D), new Size2d(radius.D * 2, radius.D * 2));
                    }
                    break;
                case RectEnum.Ellipse:
                    {
                        HOperatorSet.DrawEllipseMod(hWindow, hRegion.CenterY, hRegion.CenterX, hRegion.Phi,
                                                      hRegion.Width / 2, hRegion.Height / 2,
                                                      out HTuple row, out HTuple column, out HTuple phi, out HTuple radius1, out HTuple radius2);
                        HOperatorSet.GenEllipse(out HObject ellipse, row, column, phi, radius1, radius2);
                        hRegion.HoRegion.Dispose();
                        hRegion.HoRegion = ellipse;
                        hRegion.UpdateCenter(new Point2d(column.D, row.D), new Size2d(radius1.D * 2, radius2.D * 2));
                        hRegion.Phi = phi;
                    }
                    break;
                case RectEnum.Polygon:
                    {
                        HOperatorSet.DrawRegion(out HObject region, hWindow);
                        hRegion.HoRegion.Dispose();
                        hRegion.HoRegion = region;
                        HOperatorSet.GetRegionPolygon(hRegion.HoRegion, 1, out HTuple rows, out HTuple columns);
                        hRegion.PolygonX = rows;
                        hRegion.PolygonY = columns;

                        HOperatorSet.AreaCenter(hRegion.HoRegion, out HTuple area, out HTuple hv_Row, out HTuple hv_Column);
                        hRegion.Center = new Point2d(hv_Row.D, hv_Column.D);
                    }
                    break;
            }
        }

        public void DispRectangle2(HWindow hWindow, HTuple centerRow, HTuple centerCol, HTuple phi, HTuple length1, HTuple length2)
        {
            hWindow.DispRectangle2(centerRow, centerCol, phi, length1, length2);
        }

        public void DispRectangle2(HWindow hWindow, HTuple centerRow, HTuple centerCol, HTuple phi, HTuple length1, HTuple length2, string color)
        {
            SetColor(hWindow, color);
            hWindow.DispRectangle2(centerRow, centerCol, phi, length1, length2);
        }

        #endregion

    }
}
