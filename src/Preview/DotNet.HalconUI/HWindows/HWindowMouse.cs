using System;
using System.Windows.Forms;
using DotNet.Drawing;
using HalconDotNet;
using DotNet.Vision.Abstractions;

namespace DotNet.HalconUI
{
    /// <summary>
    /// 鼠标交互（双击复位 / 中键平移 / 滚轮缩放 / 灰度回显）。
    /// </summary>
    /// <remarks>
    /// 设计要点：
    /// - 所有事件处理都在 try/catch 内，绝不允许把异常抛回 HWindowControl 事件总线，以免冒泡到顶层 UI 崩溃。
    /// - 对外部传入的 <see cref="HObject"/> 一律使用 <see cref="HObjectExtension.NotNull"/> 检查，
    ///   而不是 <c>!= null</c>—— HObject 是 HalconDotNet 的 wrapper，可能"非 null 但未 Initialized"。
    /// - 不在鼠标事件里弹 <see cref="MessageBox"/>——会随着拖拽频率反复弹窗、阻塞 UI。
    /// - 双击判定用单调时钟 <see cref="Environment.TickCount"/> + 系统设置 <see cref="SystemInformation.DoubleClickTime"/>：
    ///   原实现用 <c>DateTime.Now.Ticks</c>（校时 / 夏令时会让差值跳变）配硬编码 200ms（与系统设置不一致）；
    ///   且判出双击后不复位，连续三击会被判成两次双击。
    /// - 原先公开的 <c>MouseDown</c> / <c>MouseDouble</c> 标志位本类只置位不复位、全仓也无人读取，已删除。
    /// </remarks>
    public class HWindowMouse : IDisposable
    {
        // 普通版 Halcon 能处理的图像最大尺寸 32K*32K，避免缩小过头导致 SetPart 崩溃
        const double MaxHalconViewArea = 32000d * 32000d;

        // 上一次（未被双击消耗的）按下时刻，Environment.TickCount 毫秒；null 表示没有可配对的单击
        int? _lastClickMs;
        bool Mouse_hand = false;
        double RowDown;
        double ColDown;

        readonly HWindow _hWindow;
        readonly IHDisplay _display;
        readonly HWindowControl _hWindowControl;

        bool _disposed;

        public event Action<HTuple, HTuple, HTuple>? RefreshUI;

        public HWindowMouse(HWindowControl hWindowControl, IHDisplay display)
        {
            if (hWindowControl == null) throw new ArgumentNullException(nameof(hWindowControl));
            if (display == null) throw new ArgumentNullException(nameof(display));

            _hWindow = hWindowControl.HalconWindow;
            _hWindowControl = hWindowControl;
            _display = display;

            hWindowControl.HMouseDown += OnHMouseDown;
            hWindowControl.HMouseUp += OnHMouseUp;
            hWindowControl.HMouseWheel += OnHMouseWheel;
        }

        bool IsUsable()
        {
            if (_disposed) return false;
            if (_hWindow == null) return false;
            if (_hWindowControl == null || _hWindowControl.IsDisposed) return false;
            try { return _hWindow.IsInitialized(); }
            catch { return false; }
        }

        public void OnHMouseDown(object sender, HMouseEventArgs e)
        {
            if (!IsUsable() || e == null) return;
            try
            {
                HTuple Row = e.Y, Column = e.X;
                RowDown = Row;
                ColDown = Column;

                // TickCount 约 24.9 天回绕一次，int 减法在回绕处仍得到正确的差值
                int nowMs = Environment.TickCount;
                bool doubleClick = _lastClickMs.HasValue
                                   && unchecked(nowMs - _lastClickMs.Value) <= SystemInformation.DoubleClickTime;
                // 双击消耗掉这次配对：第三击重新作为单击起点，而不是与第二击再凑成一次双击
                _lastClickMs = doubleClick ? (int?)null : nowMs;

                if (doubleClick && _display.HoImage.NotNull())
                {
                    HOperatorSet.SetPart(_hWindow, 0, 0, _display.HoHeight - 1, _display.HoWidth - 1);
                    HOperatorSet.ClearWindow(_hWindow);
                    HOperatorSet.DispObj(_display.HoImage, _hWindow);
                    Mouse_hand = false;
                }

                if (e.Button == MouseButtons.Middle)
                {
                    Mouse_hand = true;
                }
            }
            catch (Exception ex) { Log.Error(nameof(HWindowMouse), "处理鼠标按下失败.", ex); }
        }

        public void OnHMouseUp(object sender, HMouseEventArgs e)
        {
            if (!IsUsable() || e == null) return;
            try
            {
                HTuple Row = e.Y, Column = e.X;

                if (Mouse_hand)
                {
                    // 平移：始终重置 Mouse_hand，避免松开后状态卡住
                    Mouse_hand = false;

                    if (_display.HoImage.NotNull())
                    {
                        double RowMove = Row - RowDown;
                        double ColMove = Column - ColDown;
                        HOperatorSet.GetPart(_hWindow, out HTuple row1, out HTuple col1, out HTuple row2, out HTuple col2);
                        HOperatorSet.SetPart(_hWindow, row1 - RowMove, col1 - ColMove, row2 - RowMove, col2 - ColMove);
                        HOperatorSet.ClearWindow(_hWindow);
                        HOperatorSet.DispObj(_display.HoImage, _hWindow);
                    }
                    // 没有图像时静默忽略——鼠标事件不应该弹模态框
                }

                var handler = RefreshUI;
                if (handler != null && _display.HoImage.NotNull())
                {
                    try
                    {
                        HOperatorSet.GetGrayval(_display.HoImage, Row, Column, out HTuple egray);
                        handler.Invoke(Row, Column, egray);
                    }
                    catch (Exception ex)
                    {
                        // 鼠标落在图像范围外 GetGrayval 必然失败，属于正常路径，不升级为错误；
                        // 保留 Debug 级日志，便于排查"取灰度一直没反应"时区分是越界还是别的原因。
                        Log.Debug(nameof(HWindowMouse), $"取灰度失败(通常是鼠标在图像外): {ex.Message}");
                    }
                }
            }
            catch (Exception ex) { Log.Error(nameof(HWindowMouse), "处理鼠标抬起失败.", ex); }
        }

        public void OnHMouseWheel(object sender, HMouseEventArgs e)
        {
            if (!IsUsable() || e == null) return;
            if (!_display.HoImage.NotNull()) return;     // 没图像时滚轮无意义

            try
            {
                double zoom = e.Delta > 0 ? 1.5 : 0.5;
                HTuple Row = e.Y, Column = e.X;

                HOperatorSet.GetPart(_hWindow, out HTuple Row0, out HTuple Column0, out HTuple Row00, out HTuple Column00);
                HTuple Ht = Row00 - Row0;
                HTuple Wt = Column00 - Column0;

                // 仅允许放大；缩小时确保不会超出 Halcon 的视图上限
                if (zoom == 1.5 || (Ht.D * Wt.D) < MaxHalconViewArea)
                {
                    HTuple r1 = Row0 + ((1 - (1.0 / zoom)) * (Row - Row0));
                    HTuple c1 = Column0 + ((1 - (1.0 / zoom)) * (Column - Column0));
                    HTuple r2 = r1 + (Ht / zoom);
                    HTuple c2 = c1 + (Wt / zoom);

                    HOperatorSet.SetPart(_hWindow, r1, c1, r2, c2);
                    HOperatorSet.ClearWindow(_hWindow);
                    HOperatorSet.DispObj(_display.HoImage, _hWindow);
                }
            }
            catch (Exception ex) { Log.Error(nameof(HWindowMouse), "处理鼠标滚轮失败.", ex); }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_hWindowControl != null && !_hWindowControl.IsDisposed)
            {
                _hWindowControl.HMouseDown -= OnHMouseDown;
                _hWindowControl.HMouseUp -= OnHMouseUp;
                _hWindowControl.HMouseWheel -= OnHMouseWheel;
            }

            RefreshUI = null;
            GC.SuppressFinalize(this);
        }
    }
}
