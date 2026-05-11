using HalconDotNet;
using System;
using System.Windows.Forms;
using System.Collections.Generic;


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
        private enum Phase { Idle, Drawing, Editing }
        private enum Handle { None, P1, P2, Center, AxisEnd1, AxisEnd2, AxisEnd2Neg }

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
        private int _ended;

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
        private int _polyEditIdx = -1;

        // 缓存窗口像素尺寸, 避免 OnMouseMove 内反复 PInvoke 计算
        private double _cachedPixelSize = 1;

        // 进入绘图会话前的窗口/系统状态, End 时还原
        private HTuple _savedFlush;
        private HTuple _savedAutodraw;
        private bool _windowConfigured;

        private const double NearThreshold = 10;

        #endregion

        #region Static API

        /// <summary> 当前活动的绘图实例，用于转发鼠标事件 </summary>
        public static DrawHelper Active => _active;

        //public static void DrawPoint(HTuple windowHandle, out HTuple row, out HTuple column)
        //{ 
        
        //}
        //public static void DrawLine(HTuple windowHandle, out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2)
        //{ 
        
        //}

        public static void DrawRectangle1(HTuple windowHandle,
            out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2)
        {
            row1 = column1 = row2 = column2 = new HTuple();
            CancelDraw();
            var h = Begin(windowHandle, DrawType.Rect1);
            try
            {
                h.BlockUntilDone();
                NormalizeRect1(h._x1, h._y1, h._x2, h._y2,
                    out double left, out double top, out double right, out double bottom);
                row1 = top; column1 = left;
                row2 = bottom; column2 = right;
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
            // 默认输出空 region, 即使中间步骤抛异常调用方也能拿到可释放对象
            HOperatorSet.GenEmptyRegion(out region);
            CancelDraw();
            var h = Begin(windowHandle, DrawType.Region);
            HObject contour = null;
            try
            {
                h.BlockUntilDone();
                if (h._completed && h._polyRows.Count >= 3)
                {
                    HTuple rows = new HTuple(h._polyRows.ToArray());
                    HTuple cols = new HTuple(h._polyCols.ToArray());
                    rows = rows.TupleConcat(h._polyRows[0]);
                    cols = cols.TupleConcat(h._polyCols[0]);
                    HOperatorSet.GenContourPolygonXld(out contour, rows, cols);
                    var oldRegion = region;
                    HOperatorSet.GenRegionContourXld(contour, out region, "filled");
                    try { oldRegion.Dispose(); } catch { }
                }
            }
            finally
            {
                if (contour != null) { try { contour.Dispose(); } catch { } }
                End(h);
            }
        }

        /// <summary> 取消当前绘图操作 </summary>
        public static void CancelDraw()
        {
            try { HalconAPI.CancelDraw(); } catch { }
            // DoEvents 允许在旧 Draw* 尚未退出时重入新的 Draw*。
            // 这里立即结束旧会话，避免 flush/autodraw 状态被新旧会话交叉还原。
            var current = _active;
            if (current != null)
            {
                current._cancelled = true;
                End(current);
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
            // 顺序: Capture 背景 -> 配置窗口为双缓冲 -> 发布到 _active.
            // 在 SetupWindow 之前 Capture, 因为 DumpWindowImage 必须在 flush=true 下能看到当前画面.
            h.CaptureBackground();
            h.SetupWindow();
            _active = h;
            return h;
        }

        private static void End(DrawHelper h)
        {
            if (h == null || System.Threading.Interlocked.Exchange(ref h._ended, 1) == 1)
                return;

            try
            {
                // RestoreBackground 在 flush=false 下绘制到 backbuffer
                h.RestoreBackground();
            }
            finally
            {
                // RestoreWindow 内部会把 flush 切回 true, 自动触发一次 swap, 让背景可见
                h.RestoreWindow();
                h.ReleaseBackground();
                // 身份校验: 仅当当前活动实例仍是 h 时才清空, 防止 Application.DoEvents
                // 重入触发新的 Draw* 后, 旧的 End 错误地把新 _active 置空
                if (_active == h) _active = null;
            }
        }

        #endregion

        #region Mouse Event Handlers

        public void OnMouseDown(HMouseEventArgs e)
        {
            switch (_type)
            {
                case DrawType.Rect1: Down_Rect1(e); break;
                case DrawType.Rect2: Down_Rect2(e); break;
                case DrawType.Circle: Down_Circle(e); break;
                case DrawType.Ellipse: Down_Ellipse(e); break;
                case DrawType.Region: Down_Region(e); break;
            }
            // 宿主 (HDisplayUI.OnMouseDown) 会先 ReDispImage 把纯图像写入 backbuffer (无 ROI).
            // 若此处只 FlushBuffer, 屏幕会闪过一帧"无 ROI"画面, 直到下一次 MouseMove 才补上.
            // 因此在 swap 前按当前 phase 重绘一次 ROI, 与下一次 Move 帧无缝衔接.
            //
            // 关键: Down_xxx 在 Editing 阶段会把 _dragging 设为 true. 若直接走 EditXxx 的
            // dragging 分支, 会立即把控制点 "吸附" 到鼠标位置 —— 由于鼠标在 NearThreshold (10px)
            // 内但并不重合, 控制点会瞬间偏移. 对 Rect2/Ellipse 而言, _phi = Atan2(_cy - e.Y, e.X - _cx)
            // 在 10px 偏差下可能跳变 10°+, 矩形与箭头瞬间旋转, 这就是 "Rect2 ROI 闪烁" 的根因.
            // 临时清零 _dragging, 让本帧只按现有几何重绘; 真正的拖拽留给下一次 MouseMove.
            bool savedDragging = _dragging;
            _dragging = false;
            try { RedrawAndFlush(e); }
            finally { _dragging = savedDragging; }
        }

        public void OnMouseUp(HMouseEventArgs e)
        {
            switch (_type)
            {
                case DrawType.Rect1: Up_Rect1(e); break;
                case DrawType.Rect2: Up_Rect2(e); break;
                case DrawType.Circle: Up_Circle(e); break;
                case DrawType.Ellipse: Up_Ellipse(e); break;
                case DrawType.Region: Up_Region(e); break;
            }
            // 同 OnMouseDown: HDisplayUI.OnMouseUp 同样会先 ReDispImage, 必须重画 ROI 再 swap, 避免闪烁
            RedrawAndFlush(e);
        }

        public void OnMouseMove(HMouseEventArgs e)
        {
            // 进入会话时已设置 flush=false + autodraw=false, 所有绘图都在 backbuffer 中累积.
            // 这里不再切换 flush 状态, 避免 SetWindowParam("flush",...) 反复触发隐式刷新导致闪烁.
            RedrawAndFlush(e);
        }

        // 按当前 phase 把 ROI 完整画到 backbuffer, 再一次性 swap 到屏幕 (双缓冲核心).
        // OnMouseDown/Up/Move 共用同一条渲染路径, 保证任何鼠标事件后屏幕状态一致, 不会出现"先无 ROI、再有 ROI"的闪烁帧.
        private void RedrawAndFlush(HMouseEventArgs e)
        {
            try
            {
                // 单次刷新内只计算一次, 避免每个 WDispCross/IsNear 都触发 GetPart+GetWindowExtents
                _cachedPixelSize = ComputePixelSize();
                switch (_type)
                {
                    case DrawType.Rect1: Move_Rect1(e); break;
                    case DrawType.Rect2: Move_Rect2(e); break;
                    case DrawType.Circle: Move_Circle(e); break;
                    case DrawType.Ellipse: Move_Ellipse(e); break;
                    case DrawType.Region: Move_Region(e); break;
                }
            }
            finally
            {
                FlushBuffer();
            }
        }

        public void OnMouseWheel(HMouseEventArgs e) { }

        #endregion

        #region Rectangle1

        private void Down_Rect1(HMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            if (_phase == Phase.Idle)
            {
                _x1 = e.X; _y1 = e.Y;
                _phase = Phase.Drawing;
            }
            else if (_phase == Phase.Drawing)
            {
                _x2 = e.X; _y2 = e.Y;
                _cx = (_x1 + _x2) / 2; _cy = (_y1 + _y2) / 2;
                _phase = Phase.Editing;
            }
            else if (_phase == Phase.Editing && _hover != Handle.None)
            {
                _dragging = true;
            }
        }

        private void Up_Rect1(HMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (_phase == Phase.Drawing && Dist(_x1, _y1, e.X, e.Y) > 2)
                {
                    _x2 = e.X; _y2 = e.Y;
                    _cx = (_x1 + _x2) / 2; _cy = (_y1 + _y2) / 2;
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

        private void Move_Rect1(HMouseEventArgs e)
        {
            RestoreBackground();

            switch (_phase)
            {
                case Phase.Idle:
                    WDispCross(e.X, e.Y, "orange");
                    break;

                case Phase.Drawing:
                    WDispCross(_x1, _y1, "orange");
                    WDispCross(e.X, e.Y, "orange");
                    WDispRect1(_x1, _y1, e.X, e.Y, "red");
                    break;

                case Phase.Editing:
                    EditRect1(e);
                    break;
            }
        }

        private void EditRect1(HMouseEventArgs e)
        {
            if (_dragging)
            {
                switch (_hover)
                {
                    case Handle.P1:
                        _x1 = e.X; _y1 = e.Y;
                        _cx = (_x1 + _x2) / 2; _cy = (_y1 + _y2) / 2;
                        break;
                    case Handle.P2:
                        _x2 = e.X; _y2 = e.Y;
                        _cx = (_x1 + _x2) / 2; _cy = (_y1 + _y2) / 2;
                        break;
                    case Handle.Center:
                        double dx = e.X - _cx, dy = e.Y - _cy;
                        _x1 += dx; _y1 += dy;
                        _x2 += dx; _y2 += dy;
                        _cx = e.X; _cy = e.Y;
                        break;
                }
            }
            else
            {
                if (IsNear(_x1, _y1, e.X, e.Y)) _hover = Handle.P1;
                else if (IsNear(_x2, _y2, e.X, e.Y)) _hover = Handle.P2;
                else if (IsNear(_cx, _cy, e.X, e.Y)) _hover = Handle.Center;
                else _hover = Handle.None;
            }

            WDispCross(_x1, _y1, _hover == Handle.P1 ? "green" : "orange", 50);
            WDispCross(_x2, _y2, _hover == Handle.P2 ? "green" : "orange", 50);
            WDispCross(_cx, _cy, _hover == Handle.Center ? "green" : "orange", 50);
            WDispRect1(_x1, _y1, _x2, _y2, "red");
        }

        #endregion

        #region Rectangle2 (可旋转矩形)

        /// <summary>
        /// 交互流程 (与 Rect1/Circle 一致):
        /// Idle → 左键按下设置中心 → Drawing
        /// Drawing → 拖拽定义主轴方向(phi)和长度(halfLen1) → 释放 → Editing
        /// Editing → 拖拽控制点(中心/轴端点) → 右键确认
        /// </summary>
        private void Down_Rect2(HMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            if (_phase == Phase.Idle)
            {
                _cx = e.X; _cy = e.Y;
                _phi = 0; _halfLen1 = 0; _halfLen2 = 0;
                _phase = Phase.Drawing;
            }
            else if (_phase == Phase.Drawing)
            {
                // 处理"按下→释放未越过阈值→再次按下"的情况, 直接落定为可编辑矩形
                double dx = e.X - _cx, dy = e.Y - _cy;
                double len = Math.Sqrt(dx * dx + dy * dy);
                _halfLen1 = Math.Max(1, len);
                _phi = len > 1 ? Math.Atan2(_cy - e.Y, e.X - _cx) : 0;
                // 与 Drawing 阶段预览的短轴宽度一致(Move_Rect2 中使用 len / 5),
                // 避免释放瞬间矩形突然变宽
                _halfLen2 = Math.Max(1, _halfLen1 / 5);
                _phase = Phase.Editing;
            }
            else if (_phase == Phase.Editing && _hover != Handle.None)
            {
                _dragging = true;
            }
        }

        private void Up_Rect2(HMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (_phase == Phase.Drawing)
                {
                    double dx = e.X - _cx, dy = e.Y - _cy;
                    double len = Math.Sqrt(dx * dx + dy * dy);
                    if (len > 2)
                    {
                        _halfLen1 = len;
                        _phi = Math.Atan2(_cy - e.Y, e.X - _cx);
                        // 与 Drawing 阶段预览的短轴宽度一致(Move_Rect2 中使用 len / 5),
                        // 避免释放瞬间矩形突然变宽
                        _halfLen2 = Math.Max(1, _halfLen1 / 5);
                        _phase = Phase.Editing;
                    }
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

        private void Move_Rect2(HMouseEventArgs e)
        {
            RestoreBackground();

            switch (_phase)
            {
                case Phase.Idle:
                    WDispCross(e.X, e.Y, "orange");
                    break;

                case Phase.Drawing:
                    {
                        double dx = e.X - _cx, dy = e.Y - _cy;
                        double len = Math.Sqrt(dx * dx + dy * dy);
                        WDispCross(_cx, _cy, "orange");
                        if (len > 1)
                        {
                            double phi = Math.Atan2(_cy - e.Y, e.X - _cx);
                            WDispLine(_cx, _cy, e.X, e.Y, "orange");
                            WDispRect2Arrow(_cx, _cy, phi, len, Math.Max(1, len / 5), "red");
                        }
                    }
                    break;

                case Phase.Editing:
                    EditRect2(e);
                    break;
            }
        }

        private void EditRect2(HMouseEventArgs e)
        {
            if (_dragging)
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
                    case Handle.AxisEnd2Neg:
                        _halfLen2 = Math.Max(1, Math.Abs(-dx * Math.Sin(_phi) - dy * Math.Cos(_phi)));
                        break;
                }
            }
            else
            {
                GetAxisEnd(_cx, _cy, _phi, _halfLen1, out double ax1, out double ay1);
                GetAxisEndPerp(_cx, _cy, _phi, _halfLen2, out double ax2, out double ay2);
                GetAxisEndPerp(_cx, _cy, _phi, -_halfLen2, out double ax2n, out double ay2n);

                if (IsNear(_cx, _cy, e.X, e.Y)) _hover = Handle.Center;
                else if (IsNear(ax1, ay1, e.X, e.Y)) _hover = Handle.AxisEnd1;
                else if (IsNear(ax2, ay2, e.X, e.Y)) _hover = Handle.AxisEnd2;
                else if (IsNear(ax2n, ay2n, e.X, e.Y)) _hover = Handle.AxisEnd2Neg;
                else _hover = Handle.None;
            }

            GetAxisEnd(_cx, _cy, _phi, _halfLen1, out double a1x, out double a1y);
            GetAxisEndPerp(_cx, _cy, _phi, _halfLen2, out double a2x, out double a2y);
            GetAxisEndPerp(_cx, _cy, _phi, -_halfLen2, out double a2nx, out double a2ny);
            WDispCross(_cx, _cy, _hover == Handle.Center ? "green" : "orange", 50);
            WDispCross(a1x, a1y, _hover == Handle.AxisEnd1 ? "green" : "orange", 30);
            WDispCross(a2x, a2y, _hover == Handle.AxisEnd2 ? "green" : "orange", 30);
            WDispCross(a2nx, a2ny, _hover == Handle.AxisEnd2Neg ? "green" : "orange", 30);
            WDispRect2Arrow(_cx, _cy, _phi, _halfLen1, _halfLen2, "red");
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
            else if (_phase == Phase.Drawing)
            {
                _circR = Math.Max(1, Dist(_circCX, _circCY, e.X, e.Y));
                _phase = Phase.Editing;
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
                if (_phase == Phase.Drawing && Dist(_circCX, _circCY, e.X, e.Y) > 2)
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
                    WDispCross(e.X, e.Y, "orange");
                    break;

                case Phase.Drawing:
                    {
                        double r = Math.Max(1, Dist(_circCX, _circCY, e.X, e.Y));
                        WDispCross(_circCX, _circCY, "orange");
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
            else if (_phase == Phase.Drawing)
            {
                // 处理"按下→释放未越过阈值→再次按下"的情况, 直接落定为可编辑椭圆
                double dx = e.X - _ellCX, dy = e.Y - _ellCY;
                double len = Math.Sqrt(dx * dx + dy * dy);
                _ellR1 = Math.Max(1, len);
                _ellPhi = len > 1 ? Math.Atan2(_ellCY - e.Y, e.X - _ellCX) : 0;
                _ellR2 = _ellR1 / 2;
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
                    double len = Math.Sqrt(dx * dx + dy * dy);
                    if (len > 2)
                    {
                        _ellR1 = len;
                        _ellPhi = Math.Atan2(_ellCY - e.Y, e.X - _ellCX);
                        _ellR2 = _ellR1 / 2;
                        _phase = Phase.Editing;
                    }
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

        private void Move_Ellipse(HMouseEventArgs e)
        {
            RestoreBackground();

            switch (_phase)
            {
                case Phase.Idle:
                    WDispCross(e.X, e.Y, "orange");
                    break;

                case Phase.Drawing:
                    {
                        double dx = e.X - _ellCX, dy = e.Y - _ellCY;
                        double r1 = Math.Sqrt(dx * dx + dy * dy);
                        if (r1 > 1)
                        {
                            double phi = Math.Atan2(_ellCY - e.Y, e.X - _ellCX);
                            WDispCross(_ellCX, _ellCY, "orange");
                            WDispEllipse(_ellCX, _ellCY, phi, r1, r1 / 2, "red");
                        }
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
                    WDispCross(e.X, e.Y, "orange");
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
            // 使用临时变量, 避免 DumpWindowImage 抛出后 _bgImage 处于不确定状态
            HObject img = null;
            try
            {
                HOperatorSet.GetPart(_windowHandle, out _partR1, out _partC1, out _partR2, out _partC2);
                HOperatorSet.DumpWindowImage(out img, _windowHandle);
                _bgImage = img;
                img = null;
            }
            catch
            {
                _bgImage = null;
            }
            finally
            {
                if (img != null) { try { img.Dispose(); } catch { } }
            }
        }

        private void RestoreBackground()
        {
            if (_bgImage == null) return;
            // 重新捕获时使用临时变量, 防止 DumpWindowImage 异常导致 _bgImage 引用泄漏或悬空
            HObject newImg = null;
            bool shouldRestorePart = false;
            try
            {
                HOperatorSet.GetPart(_windowHandle, out HTuple r1, out HTuple c1, out HTuple r2, out HTuple c2);
                if (!IsSamePart(r1, c1, r2, c2, _partR1, _partC1, _partR2, _partC2))
                {
                    HOperatorSet.DumpWindowImage(out newImg, _windowHandle);
                    var old = _bgImage;
                    _bgImage = newImg;
                    newImg = null;
                    _partR1 = r1; _partC1 = c1; _partR2 = r2; _partC2 = c2;
                    try { old.Dispose(); } catch { }
                }

                HOperatorSet.GetImageSize(_bgImage, out HTuple w, out HTuple h);
                HOperatorSet.SetPart(_windowHandle, 0, 0, h - 1, w - 1);
                shouldRestorePart = true;
                HOperatorSet.DispObj(_bgImage, _windowHandle);
            }
            catch { }
            finally
            {
                if (shouldRestorePart)
                {
                    try { HOperatorSet.SetPart(_windowHandle, _partR1, _partC1, _partR2, _partC2); } catch { }
                }
                if (newImg != null) { try { newImg.Dispose(); } catch { } }
            }
        }

        private void ReleaseBackground()
        {
            var img = _bgImage;
            _bgImage = null;
            if (img != null)
            {
                try { img.Dispose(); } catch { }
            }
        }

        #endregion

        #region Window Display Helpers

        private void WDispCross(double col, double row, string color, double screenSize = 20)
        {
            try
            {
                double half = screenSize / 2 * GetPixelSize();
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
                NormalizeRect1(col1, row1, col2, row2,
                    out double left, out double top, out double right, out double bottom);
                HOperatorSet.SetColor(_windowHandle, color);
                HOperatorSet.SetDraw(_windowHandle, "margin");
                HOperatorSet.DispRectangle1(_windowHandle, top, left, bottom, right);
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

        // 矩形 + 沿 phi 方向(主轴方向)的箭头: 起点在矩形中心, 终点在主轴端点 (axis end 1)
        private void WDispRect2Arrow(double cx, double cy, double phi, double len1, double len2, string color)
        {
            WDispRect2(cx, cy, phi, len1, len2, color);

            double endCol = cx + len1 * Math.Cos(phi);
            double endRow = cy - len1 * Math.Sin(phi);

            WDispArrow(cx, cy, endCol, endRow, color);
        }

        private void WDispArrow(double col1, double row1, double col2, double row2, string color)
        {
            try
            {
                HOperatorSet.SetColor(_windowHandle, color);
                HOperatorSet.SetLineWidth(_windowHandle, 1);
                // disp_arrow 的 Size 为图像坐标下箭头头长度, 用屏幕固定像素换算保持视觉稳定
                double size = 2 * GetPixelSize();
                HOperatorSet.DispArrow(_windowHandle, row1, col1, row2, col2, size);
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
                HOperatorSet.DispEllipse(_windowHandle, cy, cx, adjPhi, major, minor);
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
            // 提升 sleep 到 10ms (~100Hz) 即可流畅响应鼠标, 同时显著降低 CPU 占用
            while (!_completed && !_cancelled)
            {
                Application.DoEvents();
                System.Threading.Thread.Sleep(10);
            }
        }

        private static double Dist(double x1, double y1, double x2, double y2)
        {
            double dx = x1 - x2, dy = y1 - y2;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static void NormalizeRect1(double col1, double row1, double col2, double row2,
            out double left, out double top, out double right, out double bottom)
        {
            left = Math.Min(col1, col2);
            top = Math.Min(row1, row2);
            right = Math.Max(col1, col2);
            bottom = Math.Max(row1, row2);
        }

        private static bool IsSamePart(HTuple r1, HTuple c1, HTuple r2, HTuple c2,
            HTuple savedR1, HTuple savedC1, HTuple savedR2, HTuple savedC2)
        {
            return IsSameTupleValue(r1, savedR1)
                && IsSameTupleValue(c1, savedC1)
                && IsSameTupleValue(r2, savedR2)
                && IsSameTupleValue(c2, savedC2);
        }

        private static bool IsSameTupleValue(HTuple current, HTuple saved)
        {
            return current != null
                && saved != null
                && Math.Abs(current.D - saved.D) < 0.000001;
        }

        private bool IsNear(double x1, double y1, double x2, double y2)
        {
            double threshold = NearThreshold * _cachedPixelSize;
            double dx = x1 - x2, dy = y1 - y2;
            return dx * dx + dy * dy < threshold * threshold;
        }

        // 读取缓存值 (由 OnMouseMove 在每帧开头刷新一次)
        private double GetPixelSize() => _cachedPixelSize;

        // 实际通过 PInvoke 计算窗口缩放比例, 比较昂贵
        private double ComputePixelSize()
        {
            try
            {
                HOperatorSet.GetPart(_windowHandle, out HTuple r1, out HTuple c1, out HTuple r2, out HTuple c2);
                HOperatorSet.GetWindowExtents(_windowHandle, out _, out _, out HTuple ww, out HTuple wh);
                double scaleX = (c2.D - c1.D + 1) / Math.Max(1, ww.D);
                double scaleY = (r2.D - r1.D + 1) / Math.Max(1, wh.D);
                return Math.Max(scaleX, scaleY);
            }
            catch { return 1; }
        }

        // 进入交互会话: 一次性切换为双缓冲模式, 整个会话保持稳定.
        // - flush=false : 禁用自动刷新, 所有 disp_* 累积到 backbuffer
        // - autodraw=false : 阻止 set_part 等操作触发 Halcon 内部的隐式 redraw
        // 参考 Halcon 官方文档 set_window_param 中关于 'flush' 的双缓冲建议.
        private void SetupWindow()
        {
            if (_windowConfigured) return;

            try { HOperatorSet.GetSystem("autodraw", out _savedAutodraw); }
            catch { _savedAutodraw = null; }

            try { HOperatorSet.GetWindowParam(_windowHandle, "flush", out _savedFlush); }
            catch { _savedFlush = null; }

            try { HOperatorSet.SetSystem("autodraw", "false"); } catch { }
            try { HOperatorSet.SetWindowParam(_windowHandle, "flush", "false"); } catch { }

            _windowConfigured = true;
        }

        // 离开交互会话: 还原 flush / autodraw. 把 flush 切回 'true' 会顺带触发一次刷新,
        // 让 RestoreBackground 恢复的背景立即可见.
        private void RestoreWindow()
        {
            if (!_windowConfigured) return;

            try
            {
                if (_savedFlush != null)
                    HOperatorSet.SetWindowParam(_windowHandle, "flush", _savedFlush);
                else
                    HOperatorSet.SetWindowParam(_windowHandle, "flush", "true");
            }
            catch { }

            try
            {
                if (_savedAutodraw != null)
                    HOperatorSet.SetSystem("autodraw", _savedAutodraw);
            }
            catch { }

            _savedFlush = null;
            _savedAutodraw = null;
            _windowConfigured = false;
        }

        // 一帧绘制完毕, 把 backbuffer 一次性 swap 到屏幕.
        // 这是 Halcon 推荐的"双缓冲"操作 (set_window_param flush=false + flush_buffer).
        private void FlushBuffer()
        {
            if (!_windowConfigured) return;
            try { HOperatorSet.FlushBuffer(_windowHandle); } catch { }
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
