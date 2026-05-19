using System;
using System.Windows.Forms;
using DotNet.Drawing;
using HalconDotNet;

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
    /// - 双击阈值原本是硬编码 2_000_000 ticks（200ms）；改成 <see cref="TimeSpan.TicksPerMillisecond"/>
    ///   计算的命名常量更清晰。
    /// </remarks>
    public class HWindowMouse : IDisposable
    {
        const int DoubleClickThresholdMs = 200;
        // 普通版 Halcon 能处理的图像最大尺寸 32K*32K，避免缩小过头导致 SetPart 崩溃
        const double MaxHalconViewArea = 32000d * 32000d;

        long clickTicks = 0;
        bool Mouse_hand = false;
        double RowDown;
        double ColDown;

        readonly HWindow _hWindow;
        readonly IHDisplay _display;
        readonly HWindowControl _hWindowControl;

        bool _disposed;

        public event Action<HTuple, HTuple, HTuple> RefreshUI;
        public bool MouseDown { get; set; }      //鼠标按下
        public bool MouseDouble { get; set; }    //鼠标双击按下

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

                long nowTicks = DateTime.Now.Ticks;
                bool doubleClick = (nowTicks - clickTicks) < DoubleClickThresholdMs * TimeSpan.TicksPerMillisecond;
                if (doubleClick && _display.HoImage.NotNull())
                {
                    _display.DispImage(_display.HoImage, true);
                    Mouse_hand = false;
                    MouseDouble = true;
                }
                clickTicks = nowTicks;

                if (e.Button == MouseButtons.Middle)
                {
                    Mouse_hand = true;
                }
                else if (e.Button == MouseButtons.Right)
                {
                    MouseDown = true;
                }
            }
            catch (Exception ex) { Console.WriteLine($"[HWindowMouse.OnHMouseDown] {ex.Message}"); }
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
                    catch
                    {
                        // 鼠标落在图像范围外 GetGrayval 会失败，属于正常路径，吞掉即可
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"[HWindowMouse.OnHMouseUp] {ex.Message}"); }
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
            catch (Exception ex) { Console.WriteLine($"[HWindowMouse.OnHMouseWheel] {ex.Message}"); }
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
