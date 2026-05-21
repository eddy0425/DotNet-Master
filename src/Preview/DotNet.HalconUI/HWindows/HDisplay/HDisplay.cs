using DotNet.Drawing;
using HalconDotNet;
using System;
using System.Collections.Generic;

namespace DotNet.HalconUI
{
    public class HDisplay : IHDisplay
    {
        bool _disposed;
        // 初始化为空字符串，避免在 GetColor() 之后被外部当字符串使用时出现 NPE / 引用未赋值的歧义
        string _color = string.Empty;

        HObject _hoImage;
        readonly HWindow _hWindow;
        readonly IHWindowFont _hWindowFont;
        readonly HWindowImage _hWindowImage;

        public bool IsCross { get; set; }           //是否画十字
        public bool Adaptive { get; set; } = true;   //自适应
        public double HoWidth => _hWindowImage?.HoWidth ?? 0;
        public double HoHeight => _hWindowImage?.HoHeight ?? 0;
        public HObject HoImage => _hWindowImage?.HoImage;  //图像

        public HDisplay(HWindowControl hWindowControl)
        {
            if (hWindowControl == null) throw new ArgumentNullException(nameof(hWindowControl));
            HOperatorSet.GenEmptyObj(out _hoImage);

            _hWindow = hWindowControl.HalconWindow;
            _hWindowFont = new HWindowFont2018(_hWindow);
            _hWindowImage = new HWindowImage(hWindowControl);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // 释放顺序：先解绑事件订阅，再释放本类持有所有权的图像。
            // 注意：hWindow / _hWindowControl 由宿主 UserControl (HDisplayUI) 通过 Designer 的 Dispose(bool) 负责释放，本类不再主动释放，避免双重释放。

            try { _hWindowImage?.Dispose(); } catch { /* swallow: release-time best effort */ }

            // 显式置空便于检查；HObject 的 Dispose 自身已具备幂等性
            var img = _hoImage;
            _hoImage = null;
            try { img?.Dispose(); } catch { /* swallow */ }

            GC.SuppressFinalize(this);
        }

        /// <summary> 窗口/控件是否仍可用于 Halcon 调用 </summary>
        bool IsWindowUsable()
        {
            if (_disposed) return false;
            if (_hWindow == null) return false;
            try
            {
                return _hWindow.IsInitialized();
            }
            catch
            {
                return false;
            }
        }

        #region HWindowImage

        /// <summary> 设置图像：内部复制一份图像，避免持有外部对象的悬挂引用 </summary>
        public void SetImage(HObject image)
        {
            if (_disposed) return;
            if (!image.NotNull()) return;
            if (_hWindowImage == null) return;

            try
            {
                _hoImage.Dispose();
                HOperatorSet.CopyImage(image, out _hoImage);
                _hWindowImage.Fun_SetImage(_hoImage);
            }
            finally
            {
                image.Dispose();
            }
        }

        /// <summary> 显示图片：内部复制一份图像，避免持有外部对象的悬挂引用 </summary>
        public void DispImage(HObject image)
        {
            if (_disposed) return;
            if (!image.NotNull()) return;
            if (_hWindowImage == null) return;

            try
            {
                _hoImage.Dispose();
                HOperatorSet.CopyImage(image, out _hoImage);
                _hWindowImage.Fun_DispImage(_hoImage, Adaptive);

                if (IsCross && IsWindowUsable())
                {
                    if (!string.Equals(GetColor(), HColor.Red, StringComparison.Ordinal))
                    {
                        SetColor(HColor.Red);
                    }

                    double w = HoWidth;
                    double h = HoHeight;
                    if (w > 0 && h > 0)
                    {
                        double size = w > h ? w : h;
                        try
                        {
                            HOperatorSet.DispCross(_hWindow, h / 2, w / 2, size, 0);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[HDisplay.DispImage] DispCross failed: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HDisplay.DispImage] display failed: {ex.Message}");
            }
            finally
            {
                image.Dispose();
            }
        }

        /// <summary> 显示图片 </summary>
        public void DispImage(HObject image, bool isSetPart)
        {
            if (_disposed) return;
            if (!image.NotNull()) return;
            if (_hWindowImage == null) return;

            try
            {
                _hoImage.Dispose();
                HOperatorSet.CopyImage(image, out _hoImage);
                _hWindowImage.Fun_DispImage(_hoImage, isSetPart);

                if (IsCross && IsWindowUsable())
                {
                    if (!string.Equals(GetColor(), HColor.Red, StringComparison.Ordinal))
                    {
                        SetColor(HColor.Red);
                    }

                    double w = HoWidth;
                    double h = HoHeight;
                    if (w > 0 && h > 0)
                    {
                        double size = w > h ? w : h;
                        try
                        {
                            HOperatorSet.DispCross(_hWindow, h / 2, w / 2, size, 0);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[HDisplay.DispImage] DispCross failed: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HDisplay.DispImage] display failed: {ex.Message}");
            }
            finally
            {
                image.Dispose();
            }
        }

        /// <summary> 重新显示图片 </summary>
        public void ReDispImage()
        {
            if (_disposed) return;
            _hWindowImage?.Fun_ReDisplay();
        }

        #endregion

        #region IHWindowFont

        /// <summary> 设置字体大小 </summary>
        public void SetFontSize(HTuple hv_Size)
        {
            if (!IsWindowUsable() || _hWindowFont == null) return;
            try { _hWindowFont.SetFontSize(hv_Size); }
            catch (Exception ex) { Console.WriteLine($"[HDisplay.SetFontSize] {ex.Message}"); }
        }

        /// <summary> 显示字体 (FontX=Column, FontY=Row，沿用项目历史语义) </summary>
        public void DispText(string message, HTuple FontX, HTuple FontY, string color)
        {
            if (!IsWindowUsable() || _hWindowFont == null) return;
            try { _hWindowFont.DispText(message, FontY, FontX, color); }
            catch (Exception ex) { Console.WriteLine($"[HDisplay.DispText] {ex.Message}"); }
        }

        /// <summary> 显示字体 </summary>
        public void DispText(string message, HTuple FontX, HTuple FontY, HTuple size, string color)
        {
            if (!IsWindowUsable() || _hWindowFont == null) return;
            try
            {
                _hWindowFont.SetFontSize(size);
                _hWindowFont.DispText(message, FontY, FontX, color);
            }
            catch (Exception ex) { Console.WriteLine($"[HDisplay.DispText] {ex.Message}"); }
        }

        #endregion

        /// <summary> 获取当前颜色（永不返回 null） </summary>
        public string GetColor()
        {
            return _color ?? string.Empty;
        }

        /// <summary>
        /// 设置画笔颜色。
        /// 设计要点：本方法被几乎所有 Disp* 显示重载在内部调用——一旦它抛硬异常，
        /// 整个上层调用栈会被打穿。这里改为"窗口不可用 → 静默忽略并记录日志"，
        /// 同时在颜色相同的快速路径下跳过 PInvoke，降低高频鼠标事件下的开销。
        /// </summary>
        public void SetColor(string color)
        {
            if (color == null) color = HColor.Red;

            if (!IsWindowUsable())
            {
                _color = color;     // 即便窗口尚未就绪也缓存意图，下次窗口就绪即可恢复
                return;
            }

            if (string.Equals(_color, color, StringComparison.Ordinal))
            {
                // 颜色未变化，无需重复 PInvoke
                return;
            }

            try
            {
                _hWindow.SetColor(color);
                _color = color;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HDisplay.SetColor] {ex.Message}");
            }
        }

        public void ClearWinDisp(HObject objectVal)
        {
            if (!IsWindowUsable()) return;
            if (!objectVal.NotNull()) return;

            try
            {
                _hWindow.ClearWindow();
                _hWindow.DispObj(objectVal);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HDisplay.ClearWinDisp] {ex.Message}");
            }
        }

        #region 区域相关

        /// <summary> 显示橡皮筋区域 </summary>
        public void DispGenRegion(CvRegion hRegion)
        {
            if (!IsWindowUsable() || hRegion == null) return;

            try
            {
                hRegion.GenRegion();
                if (hRegion.HoRegion.NotNull())
                {
                    _hWindow.DispObj(hRegion.HoRegion);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HDisplay.DispGenRegion] {ex.Message}");
            }
        }

        /// <summary> 获取坐标区域并显示 </summary>
        public void GenCoordsRegion(CvRegion hRegion, List<CvCoord> coords)
        {
            if (!IsWindowUsable() || hRegion == null || coords == null) return;

            try
            {
                hRegion.GenCoordsRegion(coords);
                DispRegion(hRegion.HoRegion, HColor.Green);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HDisplay.GenCoordsRegion] {ex.Message}");
            }
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
            if (!IsWindowUsable() || hRegion == null) return;

            DrawHelper.CancelDraw();

            try
            {
                switch (hRegion.Type)
                {
                    case RectEnum.Rectangle:
                        {
                            DrawHelper.DrawRectangle1(_hWindow, out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2);
                            HObject rectangle = null;
                            try
                            {
                                HOperatorSet.GenRectangle1(out rectangle, row1, column1, row2, column2);
                                hRegion.Update2Point(row1, column1, row2, column2);
                                ReplaceRegion(hRegion, ref rectangle);
                            }
                            finally { rectangle?.Dispose(); }
                        }
                        break;
                    case RectEnum.AffRect:
                        {
                            DrawHelper.DrawRectangle2(_hWindow, out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2);
                            HObject rectangle = null;
                            try
                            {
                                HOperatorSet.GenRectangle2(out rectangle, row, column, phi, length1, length2);
                                hRegion.UpdateCenter(new Point2d(column.D, row.D), new Size2d(length1.D * 2, length2.D * 2));
                                hRegion.Phi = phi;
                                ReplaceRegion(hRegion, ref rectangle);
                            }
                            finally { rectangle?.Dispose(); }
                        }
                        break;
                    case RectEnum.Circle:
                        {
                            DrawHelper.DrawCircle(_hWindow, out HTuple row, out HTuple column, out HTuple radius);
                            HObject circle = null;
                            try
                            {
                                HOperatorSet.GenCircle(out circle, row, column, radius);
                                hRegion.UpdateCenter(new Point2d(column.D, row.D), new Size2d(radius.D * 2, radius.D * 2));
                                ReplaceRegion(hRegion, ref circle);
                            }
                            finally { circle?.Dispose(); }
                        }
                        break;
                    case RectEnum.Ellipse:
                        {
                            DrawHelper.DrawEllipse(_hWindow, out HTuple row, out HTuple column, out HTuple phi, out HTuple radius1, out HTuple radius2);
                            HObject ellipse = null;
                            try
                            {
                                HOperatorSet.GenEllipse(out ellipse, row, column, phi, radius1, radius2);
                                hRegion.UpdateCenter(new Point2d(column.D, row.D), new Size2d(radius1.D * 2, radius2.D * 2));
                                hRegion.Phi = phi;
                                ReplaceRegion(hRegion, ref ellipse);
                            }
                            finally { ellipse?.Dispose(); }
                        }
                        break;
                    case RectEnum.Polygon:
                        DrawPolygonInto(hRegion);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HDisplay.DrawRegion] {ex.Message}");
            }
        }

        /// <summary> 修改区域 </summary>
        public void DrawRegionMod(CvRegion hRegion)
        {
            if (!IsWindowUsable() || hRegion == null) return;

            DrawHelper.CancelDraw();

            try
            {
                switch (hRegion.Type)
                {
                    case RectEnum.Rectangle:
                        {
                            DrawHelper.DrawRectangle1Mod(_hWindow, hRegion.Top, hRegion.Left, hRegion.Bottom, hRegion.Right,
                                                      out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2);
                            HObject rectangle = null;
                            try
                            {
                                HOperatorSet.GenRectangle1(out rectangle, row1, column1, row2, column2);
                                hRegion.Update2Point(row1, column1, row2, column2);
                                ReplaceRegion(hRegion, ref rectangle);
                            }
                            finally { rectangle?.Dispose(); }
                        }
                        break;
                    case RectEnum.AffRect:
                        {
                            DrawHelper.DrawRectangle2Mod(_hWindow, hRegion.CenterY, hRegion.CenterX, hRegion.Phi,
                                                    hRegion.Width / 2, hRegion.Height / 2,
                                                    out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2);
                            HObject rectangle = null;
                            try
                            {
                                HOperatorSet.GenRectangle2(out rectangle, row, column, phi, length1, length2);
                                hRegion.UpdateCenter(new Point2d(column.D, row.D), new Size2d(length1.D * 2, length2.D * 2));
                                hRegion.Phi = phi;
                                ReplaceRegion(hRegion, ref rectangle);
                            }
                            finally { rectangle?.Dispose(); }
                        }
                        break;
                    case RectEnum.Circle:
                        {
                            DrawHelper.DrawCircleMod(_hWindow, hRegion.CenterY, hRegion.CenterX, hRegion.Width / 2,
                                               out HTuple row, out HTuple column, out HTuple radius);
                            HObject circle = null;
                            try
                            {
                                HOperatorSet.GenCircle(out circle, row, column, radius);
                                hRegion.UpdateCenter(new Point2d(column.D, row.D), new Size2d(radius.D * 2, radius.D * 2));
                                ReplaceRegion(hRegion, ref circle);
                            }
                            finally { circle?.Dispose(); }
                        }
                        break;
                    case RectEnum.Ellipse:
                        {
                            DrawHelper.DrawEllipseMod(_hWindow, hRegion.CenterY, hRegion.CenterX, hRegion.Phi,
                                                         hRegion.Width / 2, hRegion.Height / 2,
                                                         out HTuple row, out HTuple column, out HTuple phi, out HTuple radius1, out HTuple radius2);
                            HObject ellipse = null;
                            try
                            {
                                HOperatorSet.GenEllipse(out ellipse, row, column, phi, radius1, radius2);
                                hRegion.UpdateCenter(new Point2d(column.D, row.D), new Size2d(radius1.D * 2, radius2.D * 2));
                                hRegion.Phi = phi;
                                ReplaceRegion(hRegion, ref ellipse);
                            }
                            finally { ellipse?.Dispose(); }
                        }
                        break;
                    case RectEnum.Polygon:
                        DrawPolygonInto(hRegion);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HDisplay.DrawRegionMod] {ex.Message}");
            }
        }

        /// <summary>
        /// 把新生成的 HObject 转移到 <paramref name="hRegion"/> 的 HoRegion 上：
        /// 释放旧 HoRegion → 转移所有权 → 把 <paramref name="created"/> 置 null，
        /// 这样调用方的 finally 不会再次释放（即"所有权已转移"语义）。
        /// </summary>
        static void ReplaceRegion(CvRegion hRegion, ref HObject created)
        {
            if (hRegion == null || created == null) return;
            try { hRegion.HoRegion?.Dispose(); } catch { /* swallow */ }
            hRegion.HoRegion = created;
            created = null;
        }

        /// <summary>
        /// 多边形 ROI 的共享实现：DrawRegion / DrawRegionMod 都走这里。
        /// 使用 try/finally 兜底，避免 GetRegionPolygon 或 AreaCenter 出错时 region 句柄泄漏。
        /// </summary>
        void DrawPolygonInto(CvRegion hRegion)
        {
            HObject region = null;
            try
            {
                DrawHelper.DrawRegion(out region, _hWindow);
                if (!region.NotNull()) return;

                HOperatorSet.GetRegionPolygon(region, 1, out HTuple rows, out HTuple columns);
                HOperatorSet.AreaCenter(region, out HTuple _, out HTuple hv_Row, out HTuple hv_Column);

                hRegion.PolygonX = columns;
                hRegion.PolygonY = rows;
                hRegion.Center = new Point2d(hv_Column.D, hv_Row.D);

                ReplaceRegion(hRegion, ref region);
            }
            finally
            {
                region?.Dispose();
            }
        }

        /// <summary> 新建区域 </summary>
        /// <remarks>
        /// 修复点：原实现先 <c>GenEmptyObj(out rectangle)</c> 再被各 case 的 <c>GenXxx(out rectangle, …)</c> 覆盖，
        /// 第一次创建的空 HObject 句柄丢失 → 句柄泄漏。
        /// 现在改为先在 case 内创建临时变量，全部成功后再赋给 <paramref name="rectangle"/>；
        /// 任何失败路径都会保证 <paramref name="rectangle"/> 是一个合法（可 Dispose）的空对象。
        /// </remarks>
        public void DrawRegion(RectEnum type, out HObject rectangle)
        {
            HOperatorSet.GenEmptyObj(out rectangle);
            if (!IsWindowUsable()) return;

            DrawHelper.CancelDraw();

            HObject created = null;
            try
            {
                switch (type)
                {
                    case RectEnum.Rectangle:
                        {
                            DrawHelper.DrawRectangle1(_hWindow, out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2);
                            HOperatorSet.GenRectangle1(out created, row1, column1, row2, column2);
                        }
                        break;
                    case RectEnum.AffRect:
                        {
                            DrawHelper.DrawRectangle2(_hWindow, out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2);
                            HOperatorSet.GenRectangle2(out created, row, column, phi, length1, length2);
                        }
                        break;
                    case RectEnum.Circle:
                        {
                            DrawHelper.DrawCircle(_hWindow, out HTuple row, out HTuple column, out HTuple radius);
                            HOperatorSet.GenCircle(out created, row, column, radius);
                        }
                        break;
                    case RectEnum.Ellipse:
                        {
                            DrawHelper.DrawEllipse(_hWindow, out HTuple row, out HTuple column, out HTuple phi, out HTuple radius1, out HTuple radius2);
                            HOperatorSet.GenEllipse(out created, row, column, phi, radius1, radius2);
                        }
                        break;
                    case RectEnum.Polygon:
                        {
                            DrawHelper.DrawRegion(out created, _hWindow);
                        }
                        break;
                }

                if (created.NotNull())
                {
                    rectangle.Dispose();    // 释放占位的空对象
                    rectangle = created;
                    created = null;         // 所有权已转移
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HDisplay.DrawRegion] {ex.Message}");
            }
            finally
            {
                // 失败路径或中途异常下未转移所有权的对象兜底释放
                created?.Dispose();
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

        /// <summary>
        /// 显示橡皮筋区域。
        /// </summary>
        /// <remarks>
        /// 修复点（Ring 分支）：
        /// 1) 原代码 <c>new HObject(); GenEmptyObj(out circle1);</c> 会立刻覆盖第一次创建的句柄→泄漏；之后 <c>GenCircle(out circle1, …)</c>
        ///    又再次覆盖了 GenEmptyObj 的句柄。本次改为直接使用 <c>GenCircle(out …)</c>，并用 finally 兜底释放。
        /// 2) "Display" 方法不应有副作用——原代码偷偷改了 <c>hRegion.HoRegion</c>，让调用方拿不到原始几何对象的所有权。
        ///    现在只显示，差集 region 用完即弃；如果调用方需要持久化差集 region，应由专门的 Gen 方法负责。
        /// </remarks>
        public void DispCvRegion(CvRegion hRegion)
        {
            if (!IsWindowUsable() || hRegion == null) return;

            try
            {
                switch (hRegion.Type)
                {
                    case RectEnum.Rectangle:
                        HOperatorSet.DispRectangle1(_hWindow, hRegion.Top + 0.5, hRegion.Left + 1,
                                                            hRegion.Bottom, hRegion.Right);
                        break;
                    case RectEnum.AffRect:
                        HOperatorSet.DispRectangle2(_hWindow, hRegion.CenterY + 0.5, hRegion.CenterX + 1, hRegion.Phi,
                                                 hRegion.Width / 2, hRegion.Height / 2);
                        break;
                    case RectEnum.Circle:
                        HOperatorSet.DispCircle(_hWindow, hRegion.CenterY + 0.5, hRegion.CenterX + 1, hRegion.Width / 2);
                        break;
                    case RectEnum.Ellipse:
                        HOperatorSet.DispEllipse(_hWindow, hRegion.CenterY + 0.5, hRegion.CenterX + 1, hRegion.Phi,
                                                 hRegion.Width / 2, hRegion.Height / 2);
                        break;
                    case RectEnum.Polygon:
                        HOperatorSet.DispPolygon(_hWindow, hRegion.PolygonX, hRegion.PolygonY);
                        break;
                    case RectEnum.Ring:
                        DispRingInternal(hRegion);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HDisplay.DispCvRegion] {ex.Message}");
            }
        }

        void DispRingInternal(CvRegion hRegion)
        {
            HObject circle1 = null;
            HObject circle2 = null;
            HObject regionDifference = null;
            try
            {
                HOperatorSet.GenCircle(out circle1, hRegion.CenterY + 0.5, hRegion.CenterX + 1, hRegion.MaxRadius);
                HOperatorSet.GenCircle(out circle2, hRegion.CenterY + 0.5, hRegion.CenterX + 1, hRegion.MinRadius);
                HOperatorSet.Difference(circle1, circle2, out regionDifference);
                _hWindow.DispObj(regionDifference);
            }
            finally
            {
                circle1?.Dispose();
                circle2?.Dispose();
                regionDifference?.Dispose();
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
