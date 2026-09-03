using DotNet.HalconUI.Draw;
using HalconDotNet;
using System;
using System.Threading;

namespace DotNet.HalconUI
{
    /// <summary>
    /// 交互式 ROI 绘制的静态门面，行为对齐 HALCON 的 <c>draw_*</c> 算子，
    /// 但支持在 <see cref="HWindowControl"/> 上正常工作，并且可以取消 / 超时。
    /// </summary>
    /// <remarks>
    /// <para>职责已按单一职责原则拆分到 <c>HWindows/HMouse/Draw/</c> 下：</para>
    /// <list type="bullet">
    /// <item><description><see cref="DrawRenderer"/> —— 窗口双缓冲、背景快照与全部绘制图元；</description></item>
    /// <item><description><see cref="DrawShape"/> 及其子类 —— 每种 ROI 的状态机、命中测试与渲染；</description></item>
    /// <item><description><see cref="DrawSession"/> —— 会话生命周期、鼠标分发、超时/取消的阻塞等待；</description></item>
    /// <item><description><see cref="DrawGeometry"/> —— 与 HALCON 无关的纯几何计算(可单测)。</description></item>
    /// </list>
    /// <para>
    /// 本类只剩“参数换算 + 调用模板”，12 个入口共用 <see cref="Run{TShape}"/>，不再逐个复制会话样板代码。
    /// </para>
    /// </remarks>
    public static class DrawHelper
    {
        /// <summary>
        /// 阻塞等待的默认上限，超时后自动结束绘制并返回当前几何。
        /// 设为 <see cref="TimeSpan.Zero"/> 或负值表示不限时(退回旧行为)。
        /// </summary>
        public static TimeSpan Timeout { get; set; } = DrawSession.DefaultTimeout;

        #region 调用模板

        /// <summary>
        /// 所有 <c>Draw*</c> / <c>Draw*Mod</c> 的公共骨架：
        /// 取消旧会话 → 开新会话 → (可选)按初始几何渲染一帧 → 阻塞等待 → 释放。
        /// </summary>
        /// <param name="window">目标窗口。会话按该对象注册，多窗口互不干扰。</param>
        /// <param name="shape">已填好初始几何的图元状态机。</param>
        /// <param name="edit">true 表示 <c>Mod</c> 语义：直接进入编辑阶段并先画一帧。</param>
        /// <param name="token">调用方的取消令牌。</param>
        /// <returns>同一个 <paramref name="shape"/>，此时其几何字段即为用户确认的结果。</returns>
        private static TShape Run<TShape>(HWindow window, TShape shape, bool edit, CancellationToken token)
            where TShape : DrawShape
        {
            if (window == null) throw new ArgumentNullException(nameof(window));

            CancelDraw(window);
            using (var session = DrawSession.Begin(window, shape))
            {
                if (edit)
                {
                    shape.BeginEdit();
                    session.RenderInitial();
                }
                session.WaitForCompletion(Timeout, token);
            }
            return shape;
        }

        #endregion

        #region 新建 ROI

        /// <summary>
        /// 交互式绘制一个点 ROI: 左键点击设置位置后进入编辑, 可拖拽十字调整, 右键确认.
        /// </summary>
        public static void DrawPoint(HWindow window,
            out HTuple row, out HTuple column, CancellationToken token = default)
        {
            var s = Run(window, new PointShape(), edit: false, token);
            row = s.Y; column = s.X;
        }

        /// <summary>
        /// 交互式绘制一条线段 ROI: 左键按下→拖拽→释放定义两端点后进入编辑,
        /// 可拖拽端点或中点平移整条线, 右键确认.
        /// </summary>
        public static void DrawLine(HWindow window,
            out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2,
            CancellationToken token = default)
        {
            var s = Run(window, new LineShape(), edit: false, token);
            row1 = s.Y1; column1 = s.X1;
            row2 = s.Y2; column2 = s.X2;
        }

        /// <summary>交互式绘制一个轴对齐矩形 ROI, 右键确认。输出已归一化为左上/右下。</summary>
        public static void DrawRectangle1(HWindow window,
            out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2,
            CancellationToken token = default)
        {
            var s = Run(window, new Rect1Shape(), edit: false, token);
            DrawGeometry.NormalizeRect1(s.X1, s.Y1, s.X2, s.Y2,
                out double left, out double top, out double right, out double bottom);
            row1 = top; column1 = left;
            row2 = bottom; column2 = right;
        }

        /// <summary>交互式绘制一个可旋转矩形 ROI, 右键确认。</summary>
        public static void DrawRectangle2(HWindow window,
            out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2,
            CancellationToken token = default)
        {
            var s = Run(window, new Rect2Shape(), edit: false, token);
            row = s.CY; column = s.CX;
            phi = s.Phi; length1 = s.HalfLen1; length2 = s.HalfLen2;
        }

        /// <summary>交互式绘制一个圆 ROI, 右键确认。</summary>
        public static void DrawCircle(HWindow window,
            out HTuple row, out HTuple column, out HTuple radius, CancellationToken token = default)
        {
            var s = Run(window, new CircleShape(), edit: false, token);
            row = s.CY; column = s.CX; radius = s.Radius;
        }

        /// <summary>交互式绘制一个椭圆 ROI, 右键确认。输出 radius1 &gt;= radius2, phi 为长轴方向。</summary>
        public static void DrawEllipse(HWindow window,
            out HTuple row, out HTuple column, out HTuple phi, out HTuple radius1, out HTuple radius2,
            CancellationToken token = default)
        {
            var s = Run(window, new EllipseShape(), edit: false, token);
            NormalizeEllipse(s, out row, out column, out phi, out radius1, out radius2);
        }

        /// <summary>交互式绘制一个多边形区域 ROI: 左键逐点添加顶点, 右键闭合并确认。</summary>
        public static void DrawRegion(out HObject region, HWindow window, CancellationToken token = default)
        {
            // 默认输出空 region, 即使中间步骤抛异常调用方也能拿到可释放对象
            HOperatorSet.GenEmptyRegion(out region);

            var s = Run(window, new RegionShape(), edit: false, token);
            if (!s.Completed || s.Rows.Count < 3) return;

            HObject? contour = null;
            try
            {
                HTuple rows = new HTuple(ToArray(s.Rows));
                HTuple cols = new HTuple(ToArray(s.Cols));
                // 首尾相接才能围成闭合轮廓
                rows = rows.TupleConcat(s.Rows[0]);
                cols = cols.TupleConcat(s.Cols[0]);
                HOperatorSet.GenContourPolygonXld(out contour, rows, cols);

                var empty = region;
                HOperatorSet.GenRegionContourXld(contour, out region, "filled");
                DrawSafe.Dispose(empty);
            }
            finally
            {
                DrawSafe.Dispose(contour);
            }
        }

        #endregion

        #region 修改已有 ROI

        /// <summary>
        /// 修改一个已有的点 ROI: 以 (rowIn, columnIn) 为初始位置进入编辑模式,
        /// 用户可拖拽十字调整位置, 右键确认.
        /// </summary>
        public static void DrawPointMod(HWindow window,
            HTuple rowIn, HTuple columnIn,
            out HTuple row, out HTuple column, CancellationToken token = default)
        {
            var s = Run(window, new PointShape { X = columnIn.D, Y = rowIn.D }, edit: true, token);
            row = s.Y; column = s.X;
        }

        /// <summary>
        /// 修改一条已有的线段 ROI: 以两端点为初始几何进入编辑模式,
        /// 可拖拽两端点或中点平移整条线, 右键确认.
        /// </summary>
        public static void DrawLineMod(HWindow window,
            HTuple row1In, HTuple column1In, HTuple row2In, HTuple column2In,
            out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2,
            CancellationToken token = default)
        {
            var shape = new LineShape
            {
                X1 = column1In.D, Y1 = row1In.D,
                X2 = column2In.D, Y2 = row2In.D,
            };
            var s = Run(window, shape, edit: true, token);
            row1 = s.Y1; column1 = s.X1;
            row2 = s.Y2; column2 = s.X2;
        }

        /// <summary>
        /// 修改一个轴对齐矩形 ROI: 以 (row1,col1)-(row2,col2) 为初始对角点进入编辑模式,
        /// 可拖拽两个角点或中心点平移, 右键确认.
        /// </summary>
        public static void DrawRectangle1Mod(HWindow window,
            HTuple row1In, HTuple column1In, HTuple row2In, HTuple column2In,
            out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2,
            CancellationToken token = default)
        {
            var shape = new Rect1Shape
            {
                X1 = column1In.D, Y1 = row1In.D,
                X2 = column2In.D, Y2 = row2In.D,
            };
            shape.SyncCenter();

            var s = Run(window, shape, edit: true, token);
            DrawGeometry.NormalizeRect1(s.X1, s.Y1, s.X2, s.Y2,
                out double left, out double top, out double right, out double bottom);
            row1 = top; column1 = left;
            row2 = bottom; column2 = right;
        }

        /// <summary>
        /// 修改一个可旋转矩形 ROI: 以中心 (rowIn, columnIn)、方向 phiIn (弧度)、
        /// 半轴长 length1In/length2In 为初始几何进入编辑模式,
        /// 可拖拽中心 / 主轴端点 / 短轴端点, 右键确认.
        /// </summary>
        public static void DrawRectangle2Mod(HWindow window,
            HTuple rowIn, HTuple columnIn, HTuple phiIn, HTuple length1In, HTuple length2In,
            out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2,
            CancellationToken token = default)
        {
            var shape = new Rect2Shape
            {
                CX = columnIn.D,
                CY = rowIn.D,
                Phi = phiIn.D,
                HalfLen1 = Math.Max(1, length1In.D),
                HalfLen2 = Math.Max(1, length2In.D),
            };
            var s = Run(window, shape, edit: true, token);
            row = s.CY; column = s.CX;
            phi = s.Phi; length1 = s.HalfLen1; length2 = s.HalfLen2;
        }

        /// <summary>
        /// 修改一个圆 ROI: 以中心 (rowIn, columnIn)、半径 radiusIn 为初始几何进入编辑模式,
        /// 可拖拽圆心或半径端点, 右键确认.
        /// </summary>
        public static void DrawCircleMod(HWindow window,
            HTuple rowIn, HTuple columnIn, HTuple radiusIn,
            out HTuple row, out HTuple column, out HTuple radius, CancellationToken token = default)
        {
            var shape = new CircleShape
            {
                CX = columnIn.D,
                CY = rowIn.D,
                Radius = Math.Max(1, radiusIn.D),
            };
            var s = Run(window, shape, edit: true, token);
            row = s.CY; column = s.CX; radius = s.Radius;
        }

        /// <summary>
        /// 修改一个椭圆 ROI: 以中心 (rowIn, columnIn)、方向 phiIn (弧度, 与 radius1 同向)、
        /// 长/短半径 radius1In/radius2In 为初始几何进入编辑模式,
        /// 可拖拽中心 / 主轴端点 / 副轴端点, 右键确认.
        /// 输出 radius1 始终 &gt;= radius2, phi 自动调整为长轴方向.
        /// </summary>
        public static void DrawEllipseMod(HWindow window,
            HTuple rowIn, HTuple columnIn, HTuple phiIn, HTuple radius1In, HTuple radius2In,
            out HTuple row, out HTuple column, out HTuple phi, out HTuple radius1, out HTuple radius2,
            CancellationToken token = default)
        {
            var shape = new EllipseShape
            {
                CX = columnIn.D,
                CY = rowIn.D,
                Phi = phiIn.D,
                R1 = Math.Max(1, radius1In.D),
                R2 = Math.Max(1, radius2In.D),
            };
            var s = Run(window, shape, edit: true, token);
            NormalizeEllipse(s, out row, out column, out phi, out radius1, out radius2);
        }

        #endregion

        #region 取消 / 事件转发

        /// <summary>取消全部窗口上正在进行的交互绘制。</summary>
        public static void CancelDraw() => CancelDraw(null);

        /// <summary>取消指定窗口上正在进行的交互绘制；<paramref name="window"/> 为 null 时取消全部。</summary>
        public static void CancelDraw(HWindow? window)
        {
            // 兼容仍在使用 HALCON 原生 draw_* 的路径
            DrawSafe.WindowOp("CancelDraw", () => HalconAPI.CancelDraw());
            DrawSession.CancelAll(window);
        }

        /// <summary>指定窗口上是否有正在进行的交互绘制会话。</summary>
        public static bool IsDrawing(HWindow? window) => DrawSession.ActiveFor(window) != null;

        /// <summary>把鼠标按下事件转发给该窗口当前的绘制会话；没有会话时静默忽略。</summary>
        public static void ForwardMouseDown(HWindow? window, HMouseEventArgs e)
            => DrawSession.ActiveFor(window)?.OnMouseDown(e);

        /// <summary>把鼠标抬起事件转发给该窗口当前的绘制会话；没有会话时静默忽略。</summary>
        public static void ForwardMouseUp(HWindow? window, HMouseEventArgs e)
            => DrawSession.ActiveFor(window)?.OnMouseUp(e);

        /// <summary>把鼠标移动事件转发给该窗口当前的绘制会话；没有会话时静默忽略。</summary>
        public static void ForwardMouseMove(HWindow? window, HMouseEventArgs e)
            => DrawSession.ActiveFor(window)?.OnMouseMove(e);

        /// <summary>把滚轮事件转发给该窗口当前的绘制会话；没有会话时静默忽略。</summary>
        public static void ForwardMouseWheel(HWindow? window, HMouseEventArgs e)
            => DrawSession.ActiveFor(window)?.OnMouseWheel(e);

        #endregion

        #region 输出换算

        // HALCON 约定 radius1 为长半轴, 因此长短轴颠倒时要交换并把 phi 旋转 90°
        private static void NormalizeEllipse(EllipseShape s,
            out HTuple row, out HTuple column, out HTuple phi, out HTuple radius1, out HTuple radius2)
        {
            row = s.CY; column = s.CX;
            phi = s.R1 >= s.R2 ? s.Phi : s.Phi + Math.PI / 2;
            radius1 = Math.Max(s.R1, s.R2);
            radius2 = Math.Min(s.R1, s.R2);
        }

        private static double[] ToArray(System.Collections.Generic.IReadOnlyList<double> src)
        {
            var arr = new double[src.Count];
            for (int i = 0; i < src.Count; i++) arr[i] = src[i];
            return arr;
        }

        #endregion
    }
}
