using HalconDotNet;
using OpenCvSharp;
using DotNet.Library.Extension;
using System;
using System.Collections.Generic;

namespace DotNet.HWindows
{
    public static class HWindowExtension
    {
        public static void SetColorExists(this HWindow hWindow, string color)
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

            hWindow.SetColor(color);
        }

        public static void DispObj(this HWindow hWindow, HObject objectVal, string color)
        {
            if (objectVal.NotNull())
            {
                hWindow.SetColorExists(color);
                hWindow.DispObj(objectVal);
            }
        }

        public static void ReDisplayImage(this HWindow hWindow, HObject scrImage)
        {
            if (scrImage.NotNull())
            {
                hWindow.ClearWindow();
                hWindow.DispObj(scrImage);
            }
        }

        #region 区域相关

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        public static void DispRegion(this HWindow hWindow, CvRegion cvRegion)
        {
            if (cvRegion.HoRegion.NotNull())
                hWindow.DispObj(cvRegion.HoRegion);
        }

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        public static void DispRegion(this HWindow hWindow, DispDRegion dispDRegion)
        {
            hWindow.DispObj(dispDRegion.HoRegion, dispDRegion.Color);
        }

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        public static void DispRegion(this HWindow hWindow, CvRegion hRegion, string color)
        {
            hWindow.SetColorExists(color);
            hWindow.DispRegion(hRegion);
        }
      
        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        public static void DispRegion2(this HWindow hWindow, DispDRegion hRegion)
        {
            hWindow.SetColorExists(hRegion.Color);
            switch (hRegion.Type)
            {
                case DrawForm.矩形:
                    {
                        HOperatorSet.DispRectangle1(hWindow, hRegion.Top + 0.5, hRegion.Left + 1,
                                                            hRegion.Bottom, hRegion.Right);
                    }
                    break;
                case DrawForm.仿射矩形:
                    {
                        HOperatorSet.DispRectangle2(hWindow, hRegion.CentreY + 0.5, hRegion.CentreX + 1, hRegion.Phi,
                                                 hRegion.Width / 2, hRegion.Height / 2);
                    }
                    break;
                case DrawForm.圆:
                    {
                        HOperatorSet.DispCircle(hWindow, hRegion.CentreY + 0.5, hRegion.CentreX + 1, hRegion.Width / 2);
                    }
                    break;
                case DrawForm.椭圆:
                    {
                        HOperatorSet.DispEllipse(hWindow, hRegion.CentreY + 0.5, hRegion.CentreX + 1, hRegion.Phi,
                                                 hRegion.Width / 2, hRegion.Height / 2);
                    }
                    break;
                case DrawForm.多边型:
                    {
                        HOperatorSet.DispPolygon(hWindow, hRegion.PolygonX, hRegion.PolygonY);
                    }
                    break;
                case DrawForm.圆环:
                    {
                        HObject circle1 = new HObject(); HOperatorSet.GenEmptyObj(out circle1);
                        HObject circle2 = new HObject(); HOperatorSet.GenEmptyObj(out circle2);
                        HOperatorSet.GenCircle(out circle1, hRegion.CentreY + 0.5, hRegion.CentreX + 1, hRegion.MaxRadius);
                        HOperatorSet.GenCircle(out circle2, hRegion.CentreY + 0.5, hRegion.CentreX + 1, hRegion.MinRadius);
                        HOperatorSet.Difference(circle1, circle2, out hRegion.InRegion);
                        hWindow.DispObj(hRegion.HoRegion);
                        circle1.Dispose();
                        circle2.Dispose();
                    }
                    break;
            }
        }

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        public static void DispGenRegion(this HWindow hWindow, CvRegion hRegion)
        {
            hRegion.GenRegion();
            hWindow.DispObj(hRegion.HoRegion);
        }

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        public static void DispGenRegion(this HWindow hWindow, DispDRegion hRegion)
        {
            hRegion.GenRegion();
            DispObj(hWindow, hRegion.HoRegion, hRegion.Color);
        }

        /// <summary>
        /// 绘制（创建）橡皮筋区域
        /// </summary>
        public static void DrawRegion(this HWindow hWindow, CvRegion hRegion)
        {
            HalconAPI.CancelDraw();

            switch (hRegion.Type)
            {
                case DrawForm.矩形:
                    {
                        HOperatorSet.DrawRectangle1(hWindow, out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2);
                        HOperatorSet.GenRectangle1(out hRegion.InRegion, row1, column1, row2, column2);

                        hRegion.Update2Point(row1, column1, row2, column2);
                    }
                    break;
                case DrawForm.仿射矩形:
                    {
                        HOperatorSet.DrawRectangle2(hWindow, out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2);
                        HOperatorSet.GenRectangle2(out hRegion.InRegion, row, column, phi, length1, length2);

                        hRegion.UpdateCentre(new Point2d(column.D, row.D), new Size2d(length1.D * 2, length2.D * 2));
                        hRegion.Phi = phi;
                    }
                    break;
                case DrawForm.圆环:
                case DrawForm.圆:
                    {
                        HOperatorSet.DrawCircle(hWindow, out HTuple row, out HTuple column, out HTuple radius);
                        HOperatorSet.GenCircle(out hRegion.InRegion, row, column, radius);

                        hRegion.UpdateCentre(new Point2d(column.D, row.D), new Size2d(radius.D * 2, radius.D * 2));
                    }
                    break;
                case DrawForm.椭圆:
                    {
                        HOperatorSet.DrawEllipse(hWindow, out HTuple row, out HTuple column, out HTuple phi, out HTuple radius1, out HTuple radius2);
                        HOperatorSet.GenEllipse(out hRegion.InRegion, row, column, phi, radius1, radius2);
                    
                        hRegion.UpdateCentre(new Point2d(column.D, row.D), new Size2d(radius1.D * 2, radius2.D * 2));
                        hRegion.Phi = phi;
                    }
                    break;
                case DrawForm.多边型:
                    {
                        HOperatorSet.DrawRegion(out hRegion.InRegion, hWindow);
                        HOperatorSet.GetRegionPolygon(hRegion.HoRegion, 1, out HTuple rows, out HTuple columns);
                        hRegion.PolygonX = rows;
                        hRegion.PolygonY = columns;
                        HOperatorSet.AreaCenter(hRegion.HoRegion, out HTuple area, out HTuple hv_Row, out HTuple hv_Column);
                        hRegion.Centre = new Point2d(hv_Row.D, hv_Column.D);
                    }
                    break;
            }
        }

        /// <summary>
        /// 绘制（创建）橡皮筋区域
        /// </summary>
        public static void DrawRegionMod(this HWindow hWindow, CvRegion hRegion)
        {
            HalconAPI.CancelDraw();

            switch (hRegion.Type)
            {
                case DrawForm.矩形:
                    {
                        HOperatorSet.DrawRectangle1Mod(hWindow, hRegion.Top, hRegion.Left, hRegion.Bottom, hRegion.Right,
                                                  out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2);
                      
                        HOperatorSet.GenRectangle1(out hRegion.InRegion, row1, column1, row2, column2);

                        hRegion.Update2Point(row1, column1, row2, column2);
                    }
                    break;
                case DrawForm.仿射矩形:
                    {
                        HOperatorSet.DrawRectangle2Mod(hWindow, hRegion.CentreY, hRegion.CentreX, hRegion.Phi,
                                                 hRegion.Width / 2, hRegion.Height / 2,
                                                 out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2);
                        HOperatorSet.GenRectangle2(out hRegion.InRegion, row, column, phi, length1, length2);

                        hRegion.UpdateCentre(new Point2d(column.D, row.D), new Size2d(length1.D * 2, length2.D * 2));
                        hRegion.Phi = phi;
                    }
                    break;
                case DrawForm.圆环:
                case DrawForm.圆:
                    {
                        HOperatorSet.DrawCircleMod(hWindow, hRegion.CentreY, hRegion.CentreX, hRegion.Width / 2,
                                                  out HTuple row, out HTuple column, out HTuple radius);
                        HOperatorSet.GenCircle(out hRegion.InRegion, row, column, radius);

                        hRegion.UpdateCentre(new Point2d(column.D, row.D), new Size2d(radius.D * 2, radius.D * 2));
                    }
                    break;
                case DrawForm.椭圆:
                    {
                        HOperatorSet.DrawEllipseMod(hWindow, hRegion.CentreY, hRegion.CentreX, hRegion.Phi,
                                                      hRegion.Width / 2, hRegion.Height / 2,
                                                      out HTuple row, out HTuple column, out HTuple phi, out HTuple radius1, out HTuple radius2);
                        HOperatorSet.GenEllipse(out hRegion.InRegion, row, column, phi, radius1, radius2);

                        hRegion.UpdateCentre(new Point2d(column.D, row.D), new Size2d(radius1.D * 2, radius2.D * 2));
                        hRegion.Phi = phi;
                    }
                    break;
                case DrawForm.多边型:
                    {
                        HOperatorSet.DrawRegion(out hRegion.InRegion, hWindow);
                        HOperatorSet.GetRegionPolygon(hRegion.HoRegion, 1, out HTuple rows, out HTuple columns);
                        hRegion.PolygonX = rows;
                        hRegion.PolygonY = columns;

                        HOperatorSet.AreaCenter(hRegion.HoRegion, out HTuple area, out HTuple hv_Row, out HTuple hv_Column);
                        hRegion.Centre = new Point2d(hv_Row.D, hv_Column.D);
                    }
                    break;
            }
        }

        /// <summary>
        /// 获取坐标区域并显示
        /// </summary>
        public static void GenCoordsRegion(this HWindow hWindow, CvRegion hRegion, List<CvCoord> coords)
        {
            hRegion.GenCoordsRegion(coords);
            hWindow.DispObj(hRegion.HoRegion, HColor.Green);
        }

        public static void DispRectangle2(this HWindow hWindow, HTuple centerRow, HTuple centerCol, HTuple phi, HTuple length1, HTuple length2, string color)
        {
            hWindow.SetColorExists(color);
            hWindow.DispRectangle2(centerRow, centerCol, phi, length1, length2);
        }
        #endregion

        #region 点相关

        public static void DispPoint(this HWindow hWindow, Point2d point)
        {
            hWindow.DispCross(point.Y, point.X, crossSize, 0);
        }

        public static void DispPoint(this HWindow hWindow, Point2d point, HTuple size)
        {
            hWindow.DispCross(point.Y, point.X, size, 0);
        }

        public static void DispPoint(this HWindow hWindow, Point2d point, HTuple size, string color)
        {
            hWindow.SetColorExists(color);
            hWindow.DispCross(point.Y, point.X, size, 0);
        }

        #endregion

        #region 坐标相关

        static int crossSize = 20;

        public static void DispCross(this HWindow hWindow, CvCoord hCoord)
        {
            hWindow.DispCross(hCoord.Y, hCoord.X, crossSize, hCoord.angle.ToRadians());
        }
        public static void DispCross(this HWindow hWindow, CvCoord hCoord, HTuple size)
        {
            hWindow.DispCross(hCoord.Y, hCoord.X, size, hCoord.angle.ToRadians());
        }
        public static void DispCross(this HWindow hWindow, CvCoord hCoord, HTuple size, string color)
        {
            hWindow.SetColorExists(color);
            hWindow.DispCross(hCoord.Y, hCoord.X, size, hCoord.angle.ToRadians());
        }
        public static void DispCross(this HWindow hWindow, HTuple crossX, HTuple crossY, string color)
        {
            hWindow.SetColorExists(color);
            hWindow.DispCross(crossX, crossY, 0);
        }
        public static void DispCross(this HWindow hWindow, HTuple crossX, HTuple crossY, HTuple angle)
        {
            hWindow.DispCross(crossY, crossX, crossSize, angle);
        }
        public static void DispCross(this HWindow hWindow, HTuple crossX, HTuple crossY, HTuple angle, string color)
        {
            hWindow.SetColorExists(color);
            hWindow.DispCross(crossY, crossX, crossSize, angle);
        }
        public static void DispCross(this HWindow hWindow, HTuple crossX, HTuple crossY, HTuple angle, HTuple size)
        {
            hWindow.DispCross(crossY, crossX, size, angle);
        }
        public static void DispCross(this HWindow hWindow, HTuple crossX, HTuple crossY, HTuple angle, HTuple size, string color)
        {
            hWindow.SetColorExists(color);
            hWindow.DispCross(crossY, crossX, size, angle);
        }
        public static void DispCross(this HWindow hWindow, Point2d point, HTuple size)
        {
            hWindow.DispCross(point.Y, point.X, size, 0);
        }
        public static void DispCross(this HWindow hWindow, Point2d point, HTuple size, string color)
        {
            hWindow.SetColorExists(color);
            hWindow.DispCross(point.Y, point.X, size, 0);
        }
        public static void DispCross(this HWindow hWindow, DispPoint2d dispPoint)
        {
            hWindow.SetColorExists(dispPoint.Color);
            hWindow.DispCross(dispPoint.Y, dispPoint.X, dispPoint.Size, 0);
        }
        public static void DispCross(this HWindow hWindow, DispDCoord dispCoord)
        {
            hWindow.SetColorExists(dispCoord.Color);
            hWindow.DispCross(dispCoord.Y, dispCoord.X, dispCoord.Size, dispCoord.angle.ToRadians());
        }

        #endregion

        #region 线相关
        public static void DispLine(this HWindow hWindow, CvLine line)
        {
            hWindow.DispLine(line.start.Y, line.start.X, line.end.Y, line.end.X);
        }
        public static void DispLine(this HWindow hWindow, CvLine line, string color)
        {
            hWindow.SetColorExists(color);
            hWindow.DispLine(line.start.Y, line.start.X, line.end.Y, line.end.X);
        }
        public static void DispLine(this HWindow hWindow, double startX, double startY, double endX, double endY, string color)
        {
            hWindow.SetColorExists(color);
            hWindow.DispLine(startY, startX, endY, endX);
        }
        public static void DispLine(this HWindow hWindow, CvLine hLine, int r)
        {
            hWindow.DispLine(hLine.start.Y, hLine.start.X, hLine.end.Y, hLine.end.X);
            hWindow.SetColor(HColor.Red);
            hWindow.DispCircle(hLine.end.Y, hLine.end.X, r);
        }
        public static void DispLine(this HWindow hWindow, CvLine hLine, int r, string color)
        {
            hWindow.SetColorExists(color);
            hWindow.DispLine(hLine.start.Y, hLine.start.X, hLine.end.Y, hLine.end.X);

            hWindow.SetColor(HColor.Red);
            hWindow.DispCircle(hLine.end.Y, hLine.end.X, r);
        }

        /// <summary>
        /// 画两点一线
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="step"></param>
        /// <param name="color"></param>
        public static void draw_2poin_line(this HWindow hWindow, Point2d point1, Point2d point2, int step)
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

        /// <summary>
        /// 画两点一线
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="step"></param>
        /// <param name="color"></param>
        public static void draw_2poin_line(this HWindow hWindow, Point2d point1, Point2d point2, int step, string color)
        {
            hWindow.SetColorExists(color);

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
        public static void DispLine(this HWindow hWindow, DispDLine line)
        {
            hWindow.SetColorExists(line.Color);
            hWindow.DispLine(line.start.Y, line.start.X, line.end.Y, line.end.X);
        }

        #endregion

        #region 方向线
        public static void DispArrow(this HWindow hWindow, CvLine line, double arrowSize)
        {
            hWindow.DispArrow(line.start.Y, line.start.X, line.end.Y, line.end.X, arrowSize);
        }
        public static void DispArrow(this HWindow hWindow, CvLine line, double arrowSize, string color)
        {
            hWindow.SetColorExists(color);
            hWindow.DispArrow(line, arrowSize);
        }
        public static void DispArrow(this HWindow hWindow, DispArrow arrow)
        {
            hWindow.SetColorExists(arrow.Color);
            hWindow.DispArrow(arrow.start.Y, arrow.start.X, arrow.end.Y, arrow.end.X, arrow.Size);
        }
        public static void DispArrow(this HWindow hWindow, double startX, double startY, double endX, double endY, double arrowSize, string color)
        {
            hWindow.SetColorExists(color);
            hWindow.DispArrow(startY, startX, endY, endX, arrowSize);
        }
        #endregion

        #region 圆
        public static void DispCircle(this HWindow hWindow, CvCircle circle)
        {
            hWindow.DispCircle(circle.Y, circle.X, circle.radius);
        }
        public static void DispCircle(this HWindow hWindow, CvCircle circle, string color)
        {
            hWindow.SetColorExists(color);
            hWindow.DispCircle(circle.Y, circle.X, circle.radius);
        }
        public static void DispCircle(this HWindow hWindow, HTuple crossX, HTuple crossY, HTuple radius, string color)
        {
            hWindow.SetColorExists(color);
            hWindow.DispCircle(crossY, crossX, radius);
        }
        public static void DispCircle(this HWindow hWindow, DispDCircle dispDCircle)
        {
            hWindow.SetColorExists(dispDCircle.Color);
            hWindow.DispCircle(dispDCircle.Y, dispDCircle.X, dispDCircle.radius);
        }

        #endregion
    }
}
