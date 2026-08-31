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

        /// <summary>
        /// DispCvRegion 显示时叠加到 Row 上的像素偏移。
        /// </summary>
        /// <remarks>
        /// 这两个偏移是从原实现原样保留下来的经验值，仅用于"显示"，不参与任何几何计算。
        /// 需要说明的是：它们并非对称（Row 加 0.5、Column 加 1），原代码没有留下任何推导依据，
        /// 按 HALCON 的像素中心约定，理论上两个方向应当取同一个值（0.5）。
        /// 这里先提为具名常量，使其可被检索、可被讨论；是否修正为对称值需要用实际图像比对后再定，
        /// 因此本次不改变数值，避免在没有验证手段的情况下引入肉眼可见的位移。
        /// </remarks>
        const double DispRowOffset = 0.5;

        /// <summary> DispCvRegion 显示时叠加到 Column 上的像素偏移，见 <see cref="DispRowOffset"/> </summary>
        const double DispColOffset = 1;

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

        /// <summary> 设置绘制模式 </summary>
        /// <param name="mode">"margin" 外接矩形, "fill" 填充矩形</param>
        public void SetDraw(string mode)
        {
            if (string.IsNullOrWhiteSpace(mode)) mode = "margin";
            if (mode != "margin" && mode != "fill")
            {
                Log.Warn(nameof(HDisplay), $"未知绘制模式 '{mode}'，回退为 margin.");
                mode = "margin";
            }

            if (!IsWindowUsable()) return;

            try
            {
                HOperatorSet.SetDraw(_hWindow, mode);
            }
            catch (Exception ex)
            {
                Log.Error(nameof(HDisplay), "设置绘制模式失败.", ex);
            }
        }

        #region HWindowImage

        /// <summary> 设置图像：内部复制一份图像，避免持有外部对象的悬挂引用 </summary>
        public void SetImage(HObject image)
        {
            if (_disposed) throw new NullReferenceException("HDisplay已释放！");
            if (!image.NotNull()) throw new NullReferenceException("图像为空！");
            if (_hWindowImage == null) throw new NullReferenceException("HWindowImage为空！");

            _hoImage.Dispose();
            HOperatorSet.CopyImage(image, out _hoImage);
            _hWindowImage.Fun_SetImage(_hoImage);
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
                            Log.Warn(nameof(HDisplay), "显示图像时叠加十字失败.", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(nameof(HDisplay), "显示图像失败.", ex);
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
                            Log.Warn(nameof(HDisplay), "显示图像时叠加十字失败.", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(nameof(HDisplay), "显示图像失败.", ex);
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
            catch (Exception ex) { Log.Warn(nameof(HDisplay), "设置字号失败.", ex); }
        }

        /// <summary> 显示字体 (FontX=Column, FontY=Row，沿用项目历史语义) </summary>
        public void DispText(string message, HTuple FontX, HTuple FontY, string color)
        {
            if (!IsWindowUsable() || _hWindowFont == null) return;
            try { _hWindowFont.DispText(message, FontY, FontX, color); }
            catch (Exception ex) { Log.Warn(nameof(HDisplay), "显示文本失败.", ex); }
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
            catch (Exception ex) { Log.Warn(nameof(HDisplay), "显示文本失败.", ex); }
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
                Log.Warn(nameof(HDisplay), "设置颜色失败.", ex);
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
                Log.Error(nameof(HDisplay), "清空窗口失败.", ex);
            }
        }

        #region 区域相关

        /// <summary> 显示橡皮筋区域 </summary>
        public void DispGenRegion(CvRegion hRegion)
        {
            if (!IsWindowUsable() || hRegion == null) return;

            try
            {
                hRegion.RebuildRegion();
                if (hRegion.HoRegion.NotNull())
                {
                    _hWindow.DispObj(hRegion.HoRegion);
                }
            }
            catch (Exception ex)
            {
                Log.Error(nameof(HDisplay), "生成并显示区域失败.", ex);
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
                Log.Error(nameof(HDisplay), "由坐标生成区域失败.", ex);
            }
        }

        #endregion

        #region 点相关

        // 本区域（以及下方的坐标/线/方向线/圆/Region）统一遵循两条约定：
        // 1) 每族重载只保留一个真正调用 Halcon 的实现入口，由该入口做 IsWindowUsable() 防护；
        //    带 color 的重载只负责 SetColor 后转发，避免防护逻辑散落成几十份。
        // 2) 参数校验一律前置于任何副作用（含 SetColor）；非法入参抛异常而不是静默 return。

        public void DispPoint(double crossX, double crossY, double size = 20)
        {
            if (!IsWindowUsable()) return;
            _hWindow.DispCross(crossY, crossX, size, 0);
        }
        public void DispPoint(double crossX, double crossY, string color, int size = 20)
        {
            SetColor(color);
            DispPoint(crossX, crossY, (double)size);
        }
        public void DispPoint(HTuple crossX, HTuple crossY, double size = 20)
        {
            if (!IsWindowUsable()) return;
            _hWindow.DispCross(crossY, crossX, size, 0);
        }
        public void DispPoint(HTuple crossX, HTuple crossY, string color, int size = 20)
        {
            SetColor(color);
            DispPoint(crossX, crossY, (double)size);
        }
        public void DispPoint(double[] rowPoints, double[] columnPoints, int size = 20)
        {
            ValidatePointArrays(rowPoints, columnPoints);

            if (!IsWindowUsable()) return;

            for (int i = 0; i < rowPoints.Length; i++)
            {
                _hWindow.DispCross(rowPoints[i], columnPoints[i], size, 0);
            }
        }
        public void DispPoint(double[] rowPoints, double[] columnPoints, string color, int size = 20)
        {
            // 原实现先 SetColor 再校验：长度不一致时颜色已被改掉，且静默 return，
            // 调用方既看不到错误，后续绘制的颜色还被污染。这里把校验提到 SetColor 之前。
            ValidatePointArrays(rowPoints, columnPoints);

            SetColor(color);
            DispPoint(rowPoints, columnPoints, size);
        }
        public void DispPoint(List<Point2d> polygons, int size = 20)
        {
            if (polygons is null) throw new ArgumentNullException(nameof(polygons));

            if (!IsWindowUsable()) return;

            for (int i = 0; i < polygons.Count; i++)
            {
                _hWindow.DispCross(polygons[i].Y, polygons[i].X, size, 0);
            }
        }
        public void DispPoint(List<Point2d> polygons, string color, int size = 20)
        {
            if (polygons is null) throw new ArgumentNullException(nameof(polygons));

            SetColor(color);
            DispPoint(polygons, size);
        }
        public void DispPoint(Point2d point, double size = 20)
        {
            if (!IsWindowUsable()) return;
            _hWindow.DispCross(point.Y, point.X, size, 0);
        }
        public void DispPoint(Point2d point, string color, double size = 20)
        {
            SetColor(color);
            DispPoint(point, size);
        }

        /// <summary> 行列坐标数组的前置校验：数量必须一一对应 </summary>
        static void ValidatePointArrays(double[] rowPoints, double[] columnPoints)
        {
            if (rowPoints == null) throw new ArgumentNullException(nameof(rowPoints));
            if (columnPoints == null) throw new ArgumentNullException(nameof(columnPoints));
            if (rowPoints.Length != columnPoints.Length)
                throw new ArgumentException(
                    $"行列坐标数量不一致: rowPoints={rowPoints.Length}, columnPoints={columnPoints.Length}",
                    nameof(columnPoints));
        }

        #endregion

        #region 坐标相关
        public void DispCross(double crossX, double crossY, double angle, double size = 20)
        {
            if (!IsWindowUsable()) return;
            _hWindow.DispCross(crossY, crossX, size, angle);
        }
        public void DispCross(double crossX, double crossY, double angle, string color, double size = 20)
        {
            SetColor(color);
            DispCross(crossX, crossY, angle, size);
        }
        public void DispCross(Point2d point, double angle, double size = 20)
        {
            DispCross(point.X, point.Y, angle, size);
        }
        public void DispCross(Point2d point, double angle, string color, double size = 20)
        {
            SetColor(color);
            DispCross(point, angle, size);
        }
        public void DispCross(CvCoord coord, double size = 20)
        {
            // CvCoord 是 readonly struct，不需要（也不可能）做 null 校验
            // CvCoord.Angle 已是弧度，不可再做角度→弧度转换
            DispCross(coord.X, coord.Y, coord.Angle, size);
        }
        public void DispCross(CvCoord coord, string color, double size = 20)
        {
            SetColor(color);
            DispCross(coord, size);
        }

        #endregion

        #region 线相关
        public void DispLine(double startX, double startY, double endX, double endY)
        {
            if (!IsWindowUsable()) return;
            _hWindow.DispLine(startY, startX, endY, endX);
        }
        public void DispLine(double startX, double startY, double endX, double endY, string color)
        {
            SetColor(color);
            DispLine(startX, startY, endX, endY);
        }
        public void DispLine(CvLine line)
        {
            if (line is null) throw new ArgumentNullException(nameof(line));

            DispLine(line.Start.X, line.Start.Y, line.End.X, line.End.Y);
        }
        public void DispLine(CvLine line, string color)
        {
            if (line is null) throw new ArgumentNullException(nameof(line));

            SetColor(color);
            DispLine(line);
        }
        public void DispLine(CvLine line, int radius)
        {
            if (line is null) throw new ArgumentNullException(nameof(line));
            if (!IsWindowUsable()) return;

            _hWindow.DispLine(line.Start.Y, line.Start.X, line.End.Y, line.End.X);

            SetColor(HColor.Red);
            _hWindow.DispCircle(line.End.Y, line.End.X, radius);
        }
        public void DispLine(CvLine line, int radius, string color)
        {
            if (line is null) throw new ArgumentNullException(nameof(line));

            SetColor(color);
            DispLine(line, radius);
        }


        /// <summary> 画两点一线 </summary>
        public void DispLine(Point2d point1, Point2d point2, int step)
        {
            if (!IsWindowUsable()) return;

            DispStepCross(point1, step);
            DispStepCross(point2, step);

            HOperatorSet.DispLine(_hWindow, point1.Y, point1.X, point2.Y, point2.X);
        }

        /// <summary> 画两点一线 </summary>
        public void DispLine(Point2d point1, Point2d point2, int step, string color)
        {
            // 原来这里逐字复制了上面 30 行的十字绘制代码，仅多一行 SetColor；改为转发。
            SetColor(color);
            DispLine(point1, point2, step);
        }

        /// <summary> 以 point 为中心画一个臂长为 step 的十字（两条正交线段） </summary>
        void DispStepCross(Point2d point, int step)
        {
            HOperatorSet.DispLine(_hWindow, point.Y, point.X - step, point.Y, point.X + step);
            HOperatorSet.DispLine(_hWindow, point.Y - step, point.X, point.Y + step, point.X);
        }

        #endregion

        #region 方向线
        public void DispArrow(double startX, double startY, double endX, double endY, double size = 20)
        {
            if (!IsWindowUsable()) return;
            _hWindow.DispArrow(startY, startX, endY, endX, size);
        }
        public void DispArrow(double startX, double startY, double endX, double endY, string color, double size = 20)
        {
            SetColor(color);
            DispArrow(startX, startY, endX, endY, size);
        }
        public void DispArrow(CvLine line, double size = 20)
        {
            if (line is null) throw new ArgumentNullException(nameof(line));

            DispArrow(line.Start.X, line.Start.Y, line.End.X, line.End.Y, size);
        }
        public void DispArrow(CvLine line, string color, double size = 20)
        {
            if (line is null) throw new ArgumentNullException(nameof(line));

            SetColor(color);
            DispArrow(line, size);
        }
        public void DispArrow(CvArrow arrow)
        {
            if (arrow is null) throw new ArgumentNullException(nameof(arrow));

            DispArrow(arrow.Start.X, arrow.Start.Y, arrow.End.X, arrow.End.Y, arrow.HeadSize);
        }
        public void DispArrow(CvArrow arrow, string color)
        {
            if (arrow is null) throw new ArgumentNullException(nameof(arrow));

            SetColor(color);
            DispArrow(arrow);
        }

        #endregion

        #region 圆
        public void DispCircle(double crossX, double crossY, double radius)
        {
            if (!IsWindowUsable()) return;
            _hWindow.DispCircle(crossY, crossX, radius);
        }
        public void DispCircle(double crossX, double crossY, double radius, string color)
        {
            SetColor(color);
            DispCircle(crossX, crossY, radius);
        }
        public void DispCircle(CvCircle circle)
        {
            if (circle is null) throw new ArgumentNullException(nameof(circle));

            DispCircle(circle.Center.X, circle.Center.Y, circle.Radius);
        }
        public void DispCircle(CvCircle circle, string color)
        {
            if (circle is null) throw new ArgumentNullException(nameof(circle));

            SetColor(color);
            DispCircle(circle);
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
                                hRegion.SetRectByCorners(row1, column1, row2, column2);
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
                                hRegion.SetRectByCenter(new Point2d(column.D, row.D), new Size2d(length1.D * 2, length2.D * 2));
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
                                hRegion.SetRectByCenter(new Point2d(column.D, row.D), new Size2d(radius.D * 2, radius.D * 2));
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
                                hRegion.SetRectByCenter(new Point2d(column.D, row.D), new Size2d(radius1.D * 2, radius2.D * 2));
                                hRegion.Phi = phi;
                                ReplaceRegion(hRegion, ref ellipse);
                            }
                            finally { ellipse?.Dispose(); }
                        }
                        break;
                    case RectEnum.Polygon:
                        DrawPolygonInto(hRegion);
                        break;
                    case RectEnum.Ring:
                        DrawRingInto(hRegion, modify: false);
                        break;
                    default:
                        throw new NotSupportedException($"DrawRegion: 不支持的 ROI 类型: {hRegion.Type}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(nameof(HDisplay), "DrawRegion 失败.", ex);
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
                                hRegion.SetRectByCorners(row1, column1, row2, column2);
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
                                hRegion.SetRectByCenter(new Point2d(column.D, row.D), new Size2d(length1.D * 2, length2.D * 2));
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
                                hRegion.SetRectByCenter(new Point2d(column.D, row.D), new Size2d(radius.D * 2, radius.D * 2));
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
                                hRegion.SetRectByCenter(new Point2d(column.D, row.D), new Size2d(radius1.D * 2, radius2.D * 2));
                                hRegion.Phi = phi;
                                ReplaceRegion(hRegion, ref ellipse);
                            }
                            finally { ellipse?.Dispose(); }
                        }
                        break;
                    case RectEnum.Polygon:
                        DrawPolygonInto(hRegion);
                        break;
                    case RectEnum.Ring:
                        DrawRingInto(hRegion, modify: true);
                        break;
                    default:
                        throw new NotSupportedException($"DrawRegionMod: 不支持的 ROI 类型: {hRegion.Type}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(nameof(HDisplay), "DrawRegionMod 失败.", ex);
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

        /// <summary>
        /// 生成同心圆环区域 (外圆减内圆). 调用方获得返回句柄的所有权.
        /// 内外半径顺序不敏感: 自动取大者为外圆.
        /// </summary>
        static HObject GenRing(HTuple row, HTuple column, double radiusA, double radiusB)
        {
            double outer = Math.Max(radiusA, radiusB);
            double inner = Math.Min(radiusA, radiusB);

            HObject outerCircle = null;
            HObject innerCircle = null;
            try
            {
                HOperatorSet.GenCircle(out outerCircle, row, column, outer);
                HOperatorSet.GenCircle(out innerCircle, row, column, inner);
                HObject ring;
                HOperatorSet.Difference(outerCircle, innerCircle, out ring);
                return ring;
            }
            finally
            {
                outerCircle?.Dispose();
                innerCircle?.Dispose();
            }
        }

        /// <summary>
        /// 圆环 ROI 的共享实现: DrawRegion / DrawRegionMod 都走这里.
        /// 交互分两步 —— 先画(或调整)外圆, 再调整内圆半径.
        /// 第二步用外圆的圆心作为内圆圆心, 强制保持同心; 用户在第二步移动圆心的操作会被忽略,
        /// 因为 <see cref="CvRegion"/> 的圆环模型 (MaxRadius / MinRadius) 只能表达同心圆环.
        /// </summary>
        void DrawRingInto(CvRegion hRegion, bool modify)
        {
            HTuple row, column, outerRadius;
            if (modify)
            {
                DrawHelper.DrawCircleMod(_hWindow, hRegion.CenterY, hRegion.CenterX, hRegion.MaxRadius,
                                         out row, out column, out outerRadius);
            }
            else
            {
                DrawHelper.DrawCircle(_hWindow, out row, out column, out outerRadius);
            }

            // 新建时给内圆一个可见的初值(外圆一半), 修改时沿用已有的 MinRadius.
            double innerSeed = modify ? hRegion.MinRadius : outerRadius.D / 2;
            DrawHelper.DrawCircleMod(_hWindow, row, column, innerSeed, out HTuple _, out HTuple _, out HTuple innerRadius);

            double outer = Math.Max(outerRadius.D, innerRadius.D);
            double inner = Math.Min(outerRadius.D, innerRadius.D);

            HObject ring = GenRing(row, column, outer, inner);
            try
            {
                // 外接框按外圆直径写入, 保证 Width/Height/BoundingBox 与其它 ROI 类型语义一致.
                hRegion.SetRectByCenter(new Point2d(column.D, row.D), new Size2d(outer * 2, outer * 2));
                hRegion.MaxRadius = outer;
                hRegion.MinRadius = inner;
                hRegion.RingWidth = outer - inner;
                ReplaceRegion(hRegion, ref ring);
            }
            finally { ring?.Dispose(); }
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
                    case RectEnum.Ring:
                        {
                            DrawHelper.DrawCircle(_hWindow, out HTuple row, out HTuple column, out HTuple outerRadius);
                            DrawHelper.DrawCircleMod(_hWindow, row, column, outerRadius.D / 2,
                                                     out HTuple _, out HTuple _, out HTuple innerRadius);
                            created = GenRing(row, column, outerRadius.D, innerRadius.D);
                        }
                        break;
                    default:
                        throw new NotSupportedException($"DrawRegion: 不支持的 ROI 类型: {type}");
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
                Log.Error(nameof(HDisplay), "交互绘制区域失败.", ex);
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
            if (!IsWindowUsable()) return;

            if (hRegion.NotNull())
                _hWindow.DispObj(hRegion);
        }

        /// <summary> 显示ROI区域 </summary>
        public void DispRegion(HObject hRegion, string color)
        {
            SetColor(color);
            DispRegion(hRegion);
        }

        /// <summary> 显示橡皮筋区域 </summary>
        public void DispRegion(CvRegion hRegion)
        {
            // 原实现直接解引用 hRegion.HoRegion，hRegion 为 null 时是 NRE 而不是可忽略的空绘制。
            if (!IsWindowUsable() || hRegion == null) return;

            if (hRegion.HoRegion.NotNull())
                _hWindow.DispObj(hRegion.HoRegion);
        }

        /// <summary> 显示橡皮筋区域 </summary>
        public void DispRegion(CvRegion hRegion, string color)
        {
            SetColor(color);
            DispRegion(hRegion);
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
                        HOperatorSet.DispRectangle1(_hWindow, hRegion.Top + DispRowOffset, hRegion.Left + DispColOffset,
                                                            hRegion.Bottom, hRegion.Right);
                        break;
                    case RectEnum.AffRect:
                        HOperatorSet.DispRectangle2(_hWindow, hRegion.CenterY + DispRowOffset, hRegion.CenterX + DispColOffset, hRegion.Phi,
                                                 hRegion.Width / 2, hRegion.Height / 2);
                        break;
                    case RectEnum.Circle:
                        HOperatorSet.DispCircle(_hWindow, hRegion.CenterY + DispRowOffset, hRegion.CenterX + DispColOffset, hRegion.Width / 2);
                        break;
                    case RectEnum.Ellipse:
                        HOperatorSet.DispEllipse(_hWindow, hRegion.CenterY + DispRowOffset, hRegion.CenterX + DispColOffset, hRegion.Phi,
                                                 hRegion.Width / 2, hRegion.Height / 2);
                        break;
                    case RectEnum.Polygon:
                        // disp_polygon(Window, Row, Column)：首参为 Row，而 PolygonX/PolygonY 分别存 Column/Row
                        HOperatorSet.DispPolygon(_hWindow, hRegion.PolygonY, hRegion.PolygonX);
                        break;
                    case RectEnum.Ring:
                        DispRingInternal(hRegion);
                        break;
                    default:
                        throw new NotSupportedException($"DispCvRegion: 不支持的 ROI 类型: {hRegion.Type}");
                }
            }
            catch (Exception ex)
            {
                Log.Error(nameof(HDisplay), "DispCvRegion 失败.", ex);
            }
        }

        void DispRingInternal(CvRegion hRegion)
        {
            HObject circle1 = null;
            HObject circle2 = null;
            HObject regionDifference = null;
            try
            {
                HOperatorSet.GenCircle(out circle1, hRegion.CenterY + DispRowOffset, hRegion.CenterX + DispColOffset, hRegion.MaxRadius);
                HOperatorSet.GenCircle(out circle2, hRegion.CenterY + DispRowOffset, hRegion.CenterX + DispColOffset, hRegion.MinRadius);
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
            if (!IsWindowUsable()) return;
            _hWindow.DispRectangle2(centerRow, centerCol, phi, length1, length2);
        }

        public void DispRectangle2(HTuple centerRow, HTuple centerCol, HTuple phi, HTuple length1, HTuple length2, string color)
        {
            SetColor(color);
            DispRectangle2(centerRow, centerCol, phi, length1, length2);
        }

        #endregion

    }
}
