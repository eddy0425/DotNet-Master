using DotNet.Drawing;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace DotNet.HalconUI.Draw
{
    /// <summary>
    /// 一次交互绘图会话：把 <see cref="DrawRenderer"/>(画布) 与 <see cref="DrawShape"/>(图元状态机)
    /// 组合起来，负责鼠标事件分发、阻塞等待与资源释放。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>多窗口安全</b>：会话按窗口对象注册在 <see cref="Sessions"/> 里，
    /// <see cref="ActiveFor"/> 只返回同一个 <see cref="HWindow"/> 上最新的会话。
    /// 原实现用单个静态 <c>_active</c> 字段，第二个窗口发起绘制会顶掉第一个窗口的会话。
    /// </para>
    /// <para>
    /// <b>阻塞等待</b>：<see cref="WaitForCompletion"/> 仍然是 <c>Application.DoEvents()</c> 轮询
    /// (彻底改成 async 需要连带改造 IRoiHost / 各策略的 DrawROI，属于另一阶段的工作)，
    /// 但现在有超时与 <see cref="CancellationToken"/> 兜底：宿主忘记转发鼠标事件时不会再永久假死。
    /// </para>
    /// </remarks>
    internal sealed class DrawSession : IDisposable
    {
        /// <summary>阻塞等待的默认上限。宿主没有转发鼠标事件时靠它兜底，不会让 UI 永久假死。</summary>
        internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

        // 轮询间隔 10ms (~100Hz) 即可流畅响应鼠标, 同时显著降低 CPU 占用
        private const int PollIntervalMs = 10;

        private static readonly object Gate = new object();
        private static readonly List<DrawSession> Sessions = new List<DrawSession>();

        private readonly DrawRenderer _renderer;
        private readonly DrawShape _shape;

        private volatile bool _cancelled;
        private int _disposed;

        private DrawSession(DrawRenderer renderer, DrawShape shape)
        {
            _renderer = renderer;
            _shape = shape;
            shape.Attach(renderer);
        }

        internal HWindow Window => _renderer.Window;

        /// <summary>用户是否右键确认（相对于超时 / 取消 / 被新会话顶掉）。</summary>
        internal bool Completed => _shape.Completed;

        #region 会话注册表

        /// <summary>开启一次会话：抓背景 → 切双缓冲 → 注册为该窗口的当前会话。</summary>
        internal static DrawSession Begin(HWindow window, DrawShape shape)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));
            if (shape == null) throw new ArgumentNullException(nameof(shape));

            var session = new DrawSession(new DrawRenderer(window), shape);
            lock (Gate) { Sessions.Add(session); }
            return session;
        }

        /// <summary>取该窗口上最新的活动会话；没有则返回 null。</summary>
        internal static DrawSession? ActiveFor(HWindow? window)
        {
            if (window == null) return null;
            lock (Gate)
            {
                for (int i = Sessions.Count - 1; i >= 0; i--)
                {
                    if (ReferenceEquals(Sessions[i].Window, window)) return Sessions[i];
                }
            }
            return null;
        }

        /// <summary>取消指定窗口上的全部会话；<paramref name="window"/> 为 null 时取消所有窗口。</summary>
        internal static void CancelAll(HWindow? window)
        {
            List<DrawSession> victims;
            lock (Gate)
            {
                victims = new List<DrawSession>();
                foreach (var s in Sessions)
                {
                    if (window == null || ReferenceEquals(s.Window, window)) victims.Add(s);
                }
            }

            // DoEvents 允许在旧 Draw* 尚未退出时重入新的 Draw*。
            // 这里立即结束旧会话，避免 flush/autodraw 状态被新旧会话交叉还原。
            foreach (var s in victims)
            {
                s._cancelled = true;
                s.Dispose();
            }
        }

        #endregion

        #region 鼠标事件

        internal void OnMouseDown(HMouseEventArgs e)
        {
            _shape.OnDown(e);

            // 宿主 (HDisplayUI.OnMouseDown) 会先 ReDispImage 把纯图像写入 backbuffer (无 ROI).
            // 若此处只 Flush, 屏幕会闪过一帧"无 ROI"画面, 直到下一次 MouseMove 才补上.
            // 因此在 swap 前按当前 phase 重绘一次 ROI, 与下一次 Move 帧无缝衔接.
            //
            // 关键: OnDown 在 Editing 阶段会把 Dragging 设为 true. 若直接走 Edit 的 dragging 分支,
            // 会立即把控制点 "吸附" 到鼠标位置 —— 由于鼠标在命中阈值 (10px) 内但并不重合, 控制点会瞬间偏移.
            // 对 Rect2/Ellipse 而言, Phi = Atan2(CY - e.Y, e.X - CX) 在 10px 偏差下可能跳变 10°+,
            // 矩形与箭头瞬间旋转, 这就是 "Rect2 ROI 闪烁" 的根因.
            // 临时清零 Dragging, 让本帧只按现有几何重绘; 真正的拖拽留给下一次 MouseMove.
            bool savedDragging = _shape.Dragging;
            _shape.Dragging = false;
            try { RedrawAndFlush(e); }
            finally { _shape.Dragging = savedDragging; }
        }

        internal void OnMouseUp(HMouseEventArgs e)
        {
            _shape.OnUp(e);
            // 同 OnMouseDown: HDisplayUI.OnMouseUp 同样会先 ReDispImage, 必须重画 ROI 再 swap, 避免闪烁
            RedrawAndFlush(e);
        }

        internal void OnMouseMove(HMouseEventArgs e)
        {
            // 进入会话时已设置 flush=false + autodraw=false, 所有绘图都在 backbuffer 中累积.
            // 这里不再切换 flush 状态, 避免 SetWindowParam("flush",...) 反复触发隐式刷新导致闪烁.
            RedrawAndFlush(e);
        }

        internal void OnMouseWheel(HMouseEventArgs e) { }

        // 按当前 phase 把 ROI 完整画到 backbuffer, 再一次性 swap 到屏幕 (双缓冲核心).
        // OnMouseDown/Up/Move 共用同一条渲染路径, 保证任何鼠标事件后屏幕状态一致,
        // 不会出现"先无 ROI、再有 ROI"的闪烁帧.
        private void RedrawAndFlush(HMouseEventArgs e)
        {
            try
            {
                // 单次刷新内只计算一次, 避免每个 Cross/IsNear 都触发 GetPart+GetWindowExtents
                _renderer.RefreshPixelSize();
                _shape.Render(e);
            }
            finally
            {
                _renderer.Flush();
            }
        }

        /// <summary>
        /// Draw*Mod 在阻塞等待之前调用一次，用传入的初始几何把 ROI 画出来。
        /// 否则用户必须先移动鼠标才能看到初始 ROI，体验割裂。
        /// </summary>
        internal void RenderInitial()
        {
            try
            {
                _renderer.RefreshPixelSize();
                _renderer.RestoreBackground();
                _shape.Hover = DrawHandle.None;
                _shape.Dragging = false;
                _shape.RenderStatic();
            }
            finally
            {
                _renderer.Flush();
            }
        }

        #endregion

        /// <summary>
        /// 阻塞直到用户右键确认、会话被取消、<paramref name="token"/> 取消或超时。
        /// </summary>
        /// <param name="timeout">等待上限；小于等于 <see cref="TimeSpan.Zero"/> 表示不限时。</param>
        /// <returns>用户确认返回 true；取消 / 超时返回 false。</returns>
        internal bool WaitForCompletion(TimeSpan timeout, CancellationToken token)
        {
            var watch = Stopwatch.StartNew();
            bool infinite = timeout <= TimeSpan.Zero;

            while (!_shape.Completed && !_cancelled)
            {
                if (token.IsCancellationRequested)
                {
                    _cancelled = true;
                    Log.Info(DrawSafe.Category, "交互绘制被调用方取消.");
                    break;
                }

                if (!infinite && watch.Elapsed > timeout)
                {
                    _cancelled = true;
                    Log.Warn(DrawSafe.Category,
                        $"交互绘制等待超过 {timeout.TotalSeconds:F0}s 未确认, 自动结束. " +
                        "常见原因: 宿主没有把 HMouseDown/Up/Move 转发给当前绘图会话.");
                    break;
                }

                Application.DoEvents();
                Thread.Sleep(PollIntervalMs);
            }

            return _shape.Completed;
        }

        /// <summary>结束会话：注销、还原窗口状态、释放背景快照。幂等。</summary>
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

            // 按引用移除: 即使 DoEvents 重入后同一窗口上又开了新会话, 也只会摘掉自己这一条
            lock (Gate) { Sessions.Remove(this); }

            _renderer.Dispose();
        }
    }
}
