using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace DotNet.HalconUI
{
    /// <summary>
    /// 独立的交互式绘图工具。
    /// 基于 HWindowControl 鼠标事件，替代 Halcon 阻塞式绘图 API。
    /// 不依赖 IDrawHandler 或 DisplayUI。
    ///
    /// 使用方式:
    ///   1. 将鼠标事件转发给 DrawHelper.Active:
    ///      hWindowControl.HMouseDown  += (s,e) => DrawHelper.Active?.OnMouseDown(e);
    ///      hWindowControl.HMouseUp    += (s,e) => DrawHelper.Active?.OnMouseUp(e);
    ///      hWindowControl.HMouseMove  += (s,e) => DrawHelper.Active?.OnMouseMove(e);
    ///
    ///   2. 调用静态绘图方法 (阻塞直到用户右键确认):
    ///      DrawHelper.DrawRectangle1(windowHandle, out row1, out col1, out row2, out col2);
    ///
    /// 交互: 左键按下→拖拽→释放→编辑(拖拽控制点)→右键确认
    /// </summary>
    public class DrawHelper
    {
        #region Internal Types

        private enum DrawType { None, Rect1, Rect2, Circle, Ellipse, Region }
        private enum Phase { Idle, Drawing, Adjusting, Editing }
        private enum Handle { None, P1, P2, Center, AxisEnd1, AxisEnd2 }

        #endregion

        #region Fields

        private static DrawHelper _active;

        private HTuple _windowHandle;
        private HObject _bgImage;
        private HTuple _partR1, _partC1, _partR2, _partC2;

        private DrawType _type;
        private Phase _phase;
        private Handle _hover;
        private bool _dragging;
        private bool _completed;
        private bool _cancelled;

        // Rect1/2 (e.X=Col, e.Y=Row)
        private double _x1, _y1, _x2, _y2;
        private double _cx, _cy, _phi, _halfLen1, _halfLen2;

        // Circle
        private double _circCX, _circCY, _circR;

        // Ellipse
        private double _ellCX, _ellCY, _ellPhi, _ellR1, _ellR2;

        // Region (polygon)
        private readonly List<double> _polyCols = new List<double>();
        private readonly List<double> _polyRows = new List<double>();
        private int _polyEditIdx;

        private const double NearThreshold = 10;

        #endregion

        #region Static API

        /// <summary> 当前活动的绘图实例，用于转发鼠标事件 </summary>
        public static DrawHelper Active => _active;

        public static void DrawRectangle1(HTuple windowHandle,
            out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2)
        {
            row1 = column1 = row2 = column2 = new HTuple();
            CancelDraw();
            var h = Begin(windowHandle, DrawType.Rect1);
            try
            {
                h.BlockUntilDone();
                row1 = h._y1; column1 = h._x1;
                row2 = h._y2; column2 = h._x2;
            }
            finally { End(h); }
        }

        public static void DrawRectangle2(HTuple windowHandle,
            out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2)
        {
            row = column = phi = length1 = length2 = new HTuple();
            CancelDraw();
            var h = Begin(windowHandle, DrawType.Rect2);
            try
            {
                h.BlockUntilDone();
                row = h._cy; column = h._cx;
                phi = h._phi; length1 = h._halfLen1; length2 = h._halfLen2;
            }
            finally { End(h); }
        }

        public static void DrawCircle(HTuple windowHandle,
            out HTuple row, out HTuple column, out HTuple radius)
        {
            row = column = radius = new HTuple();
            CancelDraw();
            var h = Begin(windowHandle, DrawType.Circle);
            try
            {
                h.BlockUntilDone();
                row = h._circCY; column = h._circCX; radius = h._circR;
            }
            finally { End(h); }
        }

        public static void DrawEllipse(HTuple windowHandle,
            out HTuple row, out HTuple column, out HTuple phi, out HTuple radius1, out HTuple radius2)
        {
            row = column = phi = radius1 = radius2 = new HTuple();
            CancelDraw();
            var h = Begin(windowHandle, DrawType.Ellipse);
            try
            {
                h.BlockUntilDone();
                double major = Math.Max(h._ellR1, h._ellR2);
                double minor = Math.Min(h._ellR1, h._ellR2);
                double adjPhi = h._ellR1 >= h._ellR2 ? h._ellPhi : h._ellPhi + Math.PI / 2;
                row = h._ellCY; column = h._ellCX;
                phi = adjPhi; radius1 = major; radius2 = minor;
            }
            finally { End(h); }
        }

        public static void DrawRegion(out HObject region, HTuple windowHandle)
        {
            HOperatorSet.GenEmptyRegion(out region);
            CancelDraw();
            var h = Begin(windowHandle, DrawType.Region);
            try
            {
                h.BlockUntilDone();
                if (h._completed && h._polyRows.Count >= 3)
                {
                    region.Dispose();
                    HTuple rows = new HTuple(h._polyRows.ToArray());
                    HTuple cols = new HTuple(h._polyCols.ToArray());
                    rows = rows.TupleConcat(h._polyRows[0]);
                    cols = cols.TupleConcat(h._polyCols[0]);
                    HOperatorSet.GenContourPolygonXld(out HObject contour, rows, cols);
                    HOperatorSet.GenRegionContourXld(contour, out region, "filled");
                    contour.Dispose();
                }
            }
            finally { End(h); }
        }

        /// <summary> 取消当前绘图操作 </summary>
        public static void CancelDraw()
        {
            try { HalconAPI.CancelDraw(); } catch { }
            if (_active != null)
            {
                _active._cancelled = true;
                _active.ReleaseBackground();
                _active = null;
            }
        }

        private static DrawHelper Begin(HTuple windowHandle, DrawType type)
        {
            var h = new DrawHelper
            {
                _windowHandle = windowHandle,
                _type = type,
                _phase = Phase.Idle,
            };
            _active = h;
            h.CaptureBackground();
            return h;
        }

        private static void End(DrawHelper h)
        {
            h.RestoreBackground();
            h.ReleaseBackground();
            _active = null;
        }

        #endregion

        #region Mouse Event Handlers

        public void OnMouseDown(HMouseEventArgs e)
        {
            switch (_type)
            {
                case DrawType.Rect1:
                case DrawType.Rect2:
                    Down_Rect(e); break;
                case DrawType.Circle:
                    Down_Circle(e); break;
                case DrawType.Ellipse:
                    Down_Ellipse(e); break;
                case DrawType.Region:
                    Down_Region(e); break;
            }
        }

        public void OnMouseUp(HMouseEventArgs e)
        {
            switch (_type)
            {
                case DrawType.Rect1:
                case DrawType.Rect2:
                    Up_Rect(e); break;
                case DrawType.Circle:
                    Up_Circle(e); break;
                case DrawType.Ellipse:
                    Up_Ellipse(e); break;
                case DrawType.Region:
                    Up_Region(e); break;
            }
        }

        public void OnMouseMove(HMouseEventArgs e)
        {
            switch (_type)
            {
                case DrawType.Rect1:
                case DrawType.Rect2:
                    Move_Rect(e); break;
                case DrawType.Circle:
                    Move_Circle(e); break;
                case DrawType.Ellipse:
                    Move_Ellipse(e); break;
                case DrawType.Region:
                    Move_Region(e); break;
            }
        }

        public void OnMouseWheel(HMouseEventArgs e) { }

        #endregion

        #region Rectangle

        private void Down_Rect(HMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            if (_phase == Phase.Idle)
            {
                _x1 = e.X; _y1 = e.Y;
                _phi = 0;
                _phase = Phase.Drawing;
            }
            else if (_phase == Phase.Editing && _hover != Handle.None)
            {
                _dragging = true;
            }
        }

        private void Up_Rect(HMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (_phase == Phase.Drawing)
                {
                    _x2 = e.X; _y2 = e.Y;
                    SyncRectCenter();
                    _phase = Phase.Editing;
                }
                else if (_phase == Phase.Editing)
                {
                    _dragging = false;
                    _hover = Handle.None;
                }
            }
            else if (e.Button == MouseButtons.Right && _phase == Phase.Editing)
            {
                _completed = true;
            }
        }

        private void Move_Rect(HMouseEventArgs e)
        {
            RestoreBackground();

            switch (_phase)
            {
                case Phase.Idle:
                    WDispCross(e.X, e.Y, "yellow");
                    break;

                case Phase.Drawing:
                    WDispCross(_x1, _y1, "yellow");
                    WDispCross(e.X, e.Y, "yellow");
                    WDispRect1(_x1, _y1, e.X, e.Y, "red");
                    break;

                case Phase.Editing:
                    EditRect(e);
                    break;
            }
        }

        private void EditRect(HMouseEventArgs e)
        {
            bool rotated = _type == DrawType.Rect2 && _phi != 0;

            if (_dragging)
            {
                if (rotated) DragRect2(e);
                else DragRect1(e);
            }
            else
            {
                DetectHoverRect(e, rotated);
            }

            if (rotated)
            {
                GetAxisEnd(_cx, _cy, _phi, _halfLen1, out double ax1, out double ay1);
                GetAxisEndPerp(_cx, _cy, _phi, _halfLen2, out double ax2, out double ay2);
                WDispCross(_cx, _cy, _hover == Handle.Center ? "green" : "orange", 50);
                WDispCross(ax1, ay1, _hover == Handle.AxisEnd1 ? "green" : "orange", 30);
                WDispCross(ax2, ay2, _hover == Handle.AxisEnd2 ? "green" : "orange", 30);
                WDispRect2(_cx, _cy, _phi, _halfLen1, _halfLen2, "red");
            }
            else
            {
                WDispCross(_x1, _y1, _hover == Handle.P1 ? "green" : "orange", 50);
                WDispCross(_x2, _y2, _hover == Handle.P2 ? "green" : "orange", 50);
                WDispCross(_cx, _cy, _hover == Handle.Center ? "green" : "orange", 50);
                WDispRect1(_x1, _y1, _x2, _y2, "red");
            }
        }

        private void DragRect1(HMouseEventArgs e)
        {
            switch (_hover)
            {
                case Handle.P1:
                    _x1 = e.X; _y1 = e.Y;
                    SyncRectCenter();
                    break;
                case Handle.P2:
                    _x2 = e.X; _y2 = e.Y;
                    SyncRectCenter();
                    break;
                case Handle.Center:
                    double dx = e.X - _cx, dy = e.Y - _cy;
                    _x1 += dx; _y1 += dy;
                    _x2 += dx; _y2 += dy;
                    _cx = e.X; _cy = e.Y;
                    break;
            }
        }

        private void DragRect2(HMouseEventArgs e)
        {
            double dx = e.X - _cx, dy = e.Y - _cy;
            switch (_hover)
            {
                case Handle.Center:
                    _cx = e.X; _cy = e.Y;
                    break;
                case Handle.AxisEnd1:
                    _halfLen1 = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
                    _phi = Math.Atan2(_cy - e.Y, e.X - _cx);
                    break;
                case Handle.AxisEnd2:
                    _halfLen2 = Math.Max(1, Math.Abs(-dx * Math.Sin(_phi) - dy * Math.Cos(_phi)));
                    break;
            }
        }

        private void DetectHoverRect(HMouseEventArgs e, bool rotated)
        {
            if (rotated)
            {
                GetAxisEnd(_cx, _cy, _phi, _halfLen1, out double ax1, out double ay1);
                GetAxisEndPerp(_cx, _cy, _phi, _halfLen2, out double ax2, out double ay2);
                if (IsNear(_cx, _cy, e.X, e.Y)) _hover = Handle.Center;
                else if (IsNear(ax1, ay1, e.X, e.Y)) _hover = Handle.AxisEnd1;
                else if (IsNear(ax2, ay2, e.X, e.Y)) _hover = Handle.AxisEnd2;
                else _hover = Handle.None;
            }
            else
            {
                if (IsNear(_x1, _y1, e.X, e.Y)) _hover = Handle.P1;
                else if (IsNear(_x2, _y2, e.X, e.Y)) _hover = Handle.P2;
                else if (IsNear(_cx, _cy, e.X, e.Y)) _hover = Handle.Center;
                else _hover = Handle.None;
            }
        }

        private void SyncRectCenter()
        {
            _cx = (_x1 + _x2) / 2;
            _cy = (_y1 + _y2) / 2;
            _halfLen1 = Math.Abs(_x2 - _x1) / 2;
            _halfLen2 = Math.Abs(_y2 - _y1) / 2;
        }

        #endregion

        #region Circle

        private void Down_Circle(HMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            if (_phase == Phase.Idle)
            {
                _circCX = e.X; _circCY = e.Y;
                _circR = 0;
                _phase = Phase.Drawing;
            }
            else if (_phase == Phase.Editing && _hover != Handle.None)
            {
                _dragging = true;
            }
        }

        private void Up_Circle(HMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (_phase == Phase.Drawing)
                {
                    _circR = Math.Max(1, Dist(_circCX, _circCY, e.X, e.Y));
                    _phase = Phase.Editing;
                }
                else if (_phase == Phase.Editing)
                {
                    _dragging = false;
                    _hover = Handle.None;
                }
            }
            else if (e.Button == MouseButtons.Right && _phase == Phase.Editing)
            {
                _completed = true;
            }
        }

        private void Move_Circle(HMouseEventArgs e)
        {
            RestoreBackground();

            switch (_phase)
            {
                case Phase.Idle:
                    WDispCross(e.X, e.Y, "yellow");
                    break;

                case Phase.Drawing:
                    {
                        double r = Math.Max(1, Dist(_circCX, _circCY, e.X, e.Y));
                        WDispCross(_circCX, _circCY, "yellow");
                        WDispCircle(_circCX, _circCY, r, "red");
                    }
                    break;

                case Phase.Editing:
                    EditCircle(e);
                    break;
            }
        }

        private void EditCircle(HMouseEventArgs e)
        {
            double edgeX = _circCX + _circR, edgeY = _circCY;

            if (_dragging)
            {
                switch (_hover)
                {
                    case Handle.Center:
                        _circCX = e.X; _circCY = e.Y;
                        break;
                    case Handle.AxisEnd1:
                        _circR = Math.Max(1, Dist(_circCX, _circCY, e.X, e.Y));
                        break;
                }
                edgeX = _circCX + _circR;
                edgeY = _circCY;
            }
            else
            {
                if (IsNear(_circCX, _circCY, e.X, e.Y)) _hover = Handle.Center;
                else if (IsNear(edgeX, edgeY, e.X, e.Y)) _hover = Handle.AxisEnd1;
                else _hover = Handle.None;
            }

            WDispCross(_circCX, _circCY, _hover == Handle.Center ? "green" : "orange", 50);
            WDispCross(edgeX, edgeY, _hover == Handle.AxisEnd1 ? "green" : "orange", 30);
            WDispCircle(_circCX, _circCY, _circR, "red");
        }

        #endregion

        #region Ellipse

        private void Down_Ellipse(HMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            if (_phase == Phase.Idle)
            {
                _ellCX = e.X; _ellCY = e.Y;
                _ellPhi = 0; _ellR1 = 0; _ellR2 = 0;
                _phase = Phase.Drawing;
            }
            else if (_phase == Phase.Adjusting)
            {
                _phase = Phase.Editing;
            }
            else if (_phase == Phase.Editing && _hover != Handle.None)
            {
                _dragging = true;
            }
        }

        private void Up_Ellipse(HMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (_phase == Phase.Drawing)
                {
                    double dx = e.X - _ellCX, dy = e.Y - _ellCY;
                    _ellR1 = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
                    _ellPhi = Math.Atan2(_ellCY - e.Y, e.X - _ellCX);
                    _ellR2 = _ellR1 / 2;
                    _phase = Phase.Adjusting;
                }
                else if (_phase == Phase.Editing)
                {
                    _dragging = false;
                    _hover = Handle.None;
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (_phase == Phase.Adjusting || _phase == Phase.Editing)
                    _completed = true;
            }
        }

        private void Move_Ellipse(HMouseEventArgs e)
        {
            RestoreBackground();

            switch (_phase)
            {
                case Phase.Idle:
                    WDispCross(e.X, e.Y, "yellow");
                    break;

                case Phase.Drawing:
                    {
                        double dx = e.X - _ellCX, dy = e.Y - _ellCY;
                        double r1 = Math.Sqrt(dx * dx + dy * dy);
                        if (r1 > 1)
                        {
                            double phi = Math.Atan2(_ellCY - e.Y, e.X - _ellCX);
                            WDispCross(_ellCX, _ellCY, "yellow");
                            WDispEllipse(_ellCX, _ellCY, phi, r1, r1 / 2, "red");
                        }
                    }
                    break;

                case Phase.Adjusting:
                    {
                        double dx = e.X - _ellCX, dy = e.Y - _ellCY;
                        _ellR2 = Math.Max(1, Math.Abs(-dx * Math.Sin(_ellPhi) - dy * Math.Cos(_ellPhi)));
                        WDispCross(_ellCX, _ellCY, "yellow");
                        WDispEllipse(_ellCX, _ellCY, _ellPhi, _ellR1, _ellR2, "red");
                    }
                    break;

                case Phase.Editing:
                    EditEllipse(e);
                    break;
            }
        }

        private void EditEllipse(HMouseEventArgs e)
        {
            if (_dragging)
            {
                double dx = e.X - _ellCX, dy = e.Y - _ellCY;
                switch (_hover)
                {
                    case Handle.Center:
                        _ellCX = e.X; _ellCY = e.Y;
                        break;
                    case Handle.AxisEnd1:
                        _ellR1 = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
                        _ellPhi = Math.Atan2(_ellCY - e.Y, e.X - _ellCX);
                        break;
                    case Handle.AxisEnd2:
                        _ellR2 = Math.Max(1, Math.Abs(-dx * Math.Sin(_ellPhi) - dy * Math.Cos(_ellPhi)));
                        break;
                }
            }
            else
            {
                GetAxisEnd(_ellCX, _ellCY, _ellPhi, _ellR1, out double ax1, out double ay1);
                GetAxisEndPerp(_ellCX, _ellCY, _ellPhi, _ellR2, out double ax2, out double ay2);

                if (IsNear(_ellCX, _ellCY, e.X, e.Y)) _hover = Handle.Center;
                else if (IsNear(ax1, ay1, e.X, e.Y)) _hover = Handle.AxisEnd1;
                else if (IsNear(ax2, ay2, e.X, e.Y)) _hover = Handle.AxisEnd2;
                else _hover = Handle.None;
            }

            GetAxisEnd(_ellCX, _ellCY, _ellPhi, _ellR1, out double a1x, out double a1y);
            GetAxisEndPerp(_ellCX, _ellCY, _ellPhi, _ellR2, out double a2x, out double a2y);
            WDispCross(_ellCX, _ellCY, _hover == Handle.Center ? "green" : "orange", 50);
            WDispCross(a1x, a1y, _hover == Handle.AxisEnd1 ? "green" : "orange", 30);
            WDispCross(a2x, a2y, _hover == Handle.AxisEnd2 ? "green" : "orange", 30);
            WDispEllipse(_ellCX, _ellCY, _ellPhi, _ellR1, _ellR2, "red");
        }

        #endregion

        #region Region (Polygon)

        private void Down_Region(HMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            if (_phase == Phase.Idle || _phase == Phase.Drawing)
            {
                _polyCols.Add(e.X);
                _polyRows.Add(e.Y);
                if (_phase == Phase.Idle) _phase = Phase.Drawing;
            }
            else if (_phase == Phase.Editing && _hover == Handle.P1)
            {
                _dragging = true;
            }
        }

        private void Up_Region(HMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && _phase == Phase.Editing)
            {
                _dragging = false;
                _hover = Handle.None;
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (_phase == Phase.Drawing && _polyRows.Count >= 3)
                    _phase = Phase.Editing;
                else if (_phase == Phase.Editing)
                    _completed = true;
            }
        }

        private void Move_Region(HMouseEventArgs e)
        {
            RestoreBackground();

            switch (_phase)
            {
                case Phase.Idle:
                    WDispCross(e.X, e.Y, "yellow");
                    break;

                case Phase.Drawing:
                    DispPolyLines("orange");
                    WDispCross(e.X, e.Y, "red");
                    if (_polyCols.Count > 0)
                    {
                        int last = _polyCols.Count - 1;
                        WDispLine(_polyCols[last], _polyRows[last], e.X, e.Y, "red");
                    }
                    break;

                case Phase.Editing:
                    EditRegion(e);
                    break;
            }
        }

        private void EditRegion(HMouseEventArgs e)
        {
            DispPolyLines("red");
            for (int i = 0; i < _polyCols.Count; i++)
                WDispCross(_polyCols[i], _polyRows[i], "green", 10);

            if (_dragging && _polyEditIdx >= 0 && _polyEditIdx < _polyCols.Count)
            {
                _polyCols[_polyEditIdx] = e.X;
                _polyRows[_polyEditIdx] = e.Y;
            }
            else
            {
                _hover = Handle.None;
                for (int i = 0; i < _polyCols.Count; i++)
                {
                    if (IsNear(_polyCols[i], _polyRows[i], e.X, e.Y))
                    {
                        WDispCross(_polyCols[i], _polyRows[i], "red", 15);
                        _polyEditIdx = i;
                        _hover = Handle.P1;
                        break;
                    }
                }
            }
        }

        private void DispPolyLines(string color)
        {
            for (int i = 0; i < _polyCols.Count - 1; i++)
                WDispLine(_polyCols[i], _polyRows[i], _polyCols[i + 1], _polyRows[i + 1], color);
            if (_polyCols.Count > 2)
                WDispLine(_polyCols[_polyCols.Count - 1], _polyRows[_polyRows.Count - 1],
                          _polyCols[0], _polyRows[0], color);
        }

        #endregion

        #region Background Management

        private void CaptureBackground()
        {
            try
            {
                HOperatorSet.GetPart(_windowHandle, out _partR1, out _partC1, out _partR2, out _partC2);
                HOperatorSet.DumpWindowImage(out _bgImage, _windowHandle);
            }
            catch { _bgImage = null; }
        }

        private void RestoreBackground()
        {
            if (_bgImage == null) return;
            try
            {
                HOperatorSet.GetImageSize(_bgImage, out HTuple w, out HTuple h);
                HOperatorSet.SetPart(_windowHandle, 0, 0, h - 1, w - 1);
                HOperatorSet.DispObj(_bgImage, _windowHandle);
                HOperatorSet.SetPart(_windowHandle, _partR1, _partC1, _partR2, _partC2);
            }
            catch { }
        }

        private void ReleaseBackground()
        {
            if (_bgImage != null)
            {
                _bgImage.Dispose();
                _bgImage = null;
            }
        }

        #endregion

        #region Window Display Helpers

        private void WDispCross(double col, double row, string color, double size = 20)
        {
            try
            {
                double half = size / 2;
                HOperatorSet.SetColor(_windowHandle, color);
                HOperatorSet.SetLineWidth(_windowHandle, 1);
                HOperatorSet.DispLine(_windowHandle, row - half, col, row + half, col);
                HOperatorSet.DispLine(_windowHandle, row, col - half, row, col + half);
            }
            catch { }
        }

        private void WDispRect1(double col1, double row1, double col2, double row2, string color)
        {
            try
            {
                HOperatorSet.SetColor(_windowHandle, color);
                HOperatorSet.SetDraw(_windowHandle, "margin");
                HOperatorSet.DispRectangle1(_windowHandle, row1, col1, row2, col2);
            }
            catch { }
        }

        private void WDispRect2(double cx, double cy, double phi, double len1, double len2, string color)
        {
            try
            {
                HOperatorSet.SetColor(_windowHandle, color);
                HOperatorSet.SetDraw(_windowHandle, "margin");
                HOperatorSet.DispRectangle2(_windowHandle, cy, cx, phi, len1, len2);
            }
            catch { }
        }

        private void WDispCircle(double col, double row, double radius, string color)
        {
            try
            {
                HOperatorSet.SetColor(_windowHandle, color);
                HOperatorSet.SetDraw(_windowHandle, "margin");
                HOperatorSet.DispCircle(_windowHandle, row, col, radius);
            }
            catch { }
        }

        private void WDispEllipse(double cx, double cy, double phi, double r1, double r2, string color)
        {
            try
            {
                double major = Math.Max(r1, r2);
                double minor = Math.Min(r1, r2);
                double adjPhi = r1 >= r2 ? phi : phi + Math.PI / 2;
                HOperatorSet.SetColor(_windowHandle, color);
                HOperatorSet.SetDraw(_windowHandle, "margin");
                HOperatorSet.GenEllipse(out HObject region, cy, cx, adjPhi, major, minor);
                HOperatorSet.DispObj(region, _windowHandle);
                region.Dispose();
            }
            catch { }
        }

        private void WDispLine(double col1, double row1, double col2, double row2, string color)
        {
            try
            {
                HOperatorSet.SetColor(_windowHandle, color);
                HOperatorSet.DispLine(_windowHandle, row1, col1, row2, col2);
            }
            catch { }
        }

        #endregion

        #region Utilities

        private void BlockUntilDone()
        {
            while (!_completed && !_cancelled)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(5);
            }
        }

        private static double Dist(double x1, double y1, double x2, double y2)
        {
            double dx = x1 - x2, dy = y1 - y2;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static bool IsNear(double x1, double y1, double x2, double y2)
        {
            double dx = x1 - x2, dy = y1 - y2;
            return dx * dx + dy * dy < NearThreshold * NearThreshold;
        }

        /// <summary> 沿 phi 方向的端点: (cos(phi), -sin(phi)) </summary>
        private static void GetAxisEnd(double cx, double cy, double phi, double length,
            out double ex, out double ey)
        {
            ex = cx + length * Math.Cos(phi);
            ey = cy - length * Math.Sin(phi);
        }

        /// <summary> 垂直于 phi 的端点: (-sin(phi), -cos(phi)) </summary>
        private static void GetAxisEndPerp(double cx, double cy, double phi, double length,
            out double ex, out double ey)
        {
            ex = cx - length * Math.Sin(phi);
            ey = cy - length * Math.Cos(phi);
        }

        #endregion
    }
}
