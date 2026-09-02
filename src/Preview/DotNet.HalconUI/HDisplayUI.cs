using DotNet.Drawing;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Windows.Forms;


namespace DotNet.HalconUI
{
    /// <summary>
    /// 承载 HALCON 显示窗口的用户控件。
    /// </summary>
    /// <remarks>
    /// 本类不再实现 <see cref="IHDisplay"/>：原先它把 46 个 <c>Disp*</c> 重载
    /// 逐个转发给内部的 <c>HDisplay</c>，一个绘制方法要在接口、HDisplay、HDisplayUI
    /// 三处各写一遍签名，改一次动三处。现在改为组合——绘制走 <see cref="Display"/>，
    /// 本类只保留「控件才有」的职责：鼠标交互模式、刷新通知、按钮状态。
    /// </remarks>
    public partial class HDisplayUI : UserControl
    {
        readonly HDisplay display;
        readonly HWindowMouse mouse;

        /// <summary> 绘制接口。所有 <c>Disp*</c> 调用都经由它，不再由本控件转发。 </summary>
        public IHDisplay Display => display;
        public event HMouseEventHandler HMouseUp { add => hWindowControl.HMouseUp += value; remove => hWindowControl.HMouseUp -= value; }
        public event HMouseEventHandler HMouseMove { add => hWindowControl.HMouseMove += value; remove => hWindowControl.HMouseMove -= value; }
        public event HMouseEventHandler HMouseDown { add => hWindowControl.HMouseDown += value; remove => hWindowControl.HMouseDown -= value; }
        public event HMouseEventHandler HMouseWheel { add => hWindowControl.HMouseWheel += value; remove => hWindowControl.HMouseWheel -= value; }

        public delegate void ShowDelegate();
        /// <summary>
        /// 图像刷新完成后触发。
        /// </summary>
        /// <remarks>
        /// 原为公开委托字段：外部可以直接整体覆盖（丢掉别人的订阅），也可以替本控件触发。
        /// 改成 event 后外部只能 += / -=，触发权保留在本类内部。
        /// </remarks>
        public event ShowDelegate OnShow;
        public event EventHandler<DrawModelUIArgs> DrawDoneEvent;
        public void DrawDone(string modelPath, HObject ho_ModeRect, HObject ho_Contour, ModelResult result)
        {
            DrawDoneEvent?.Invoke(this, new DrawModelUIArgs(modelPath, ho_ModeRect, ho_Contour, result));
        }

        #region 属性

        // 宽/高/尺寸/中心/图像/颜色/字号等一律经 Display 访问，本控件不再镜像一份。
        public HWindow HoWindow => hWindowControl.HalconWindow;  //窗体句柄
        public bool HoMouseDown { get { return mouse.MouseDown; } set { mouse.MouseDown = value; } } //鼠标按下
        public bool HoMouseDouble { get { return mouse.MouseDouble; } set { mouse.MouseDouble = value; } }  //鼠标双击按下

        #endregion

        private DrawEnum _drawType = DrawEnum.None;

        /// <summary>
        /// 当前鼠标交互模式。
        /// </summary>
        /// <remarks>原为公开可变字段，改成属性以便后续加入校验/通知，外部读写语义不变。</remarks>
        public DrawEnum DrawType
        {
            get { return _drawType; }
            set { _drawType = value; }
        }

        readonly NoneMouse dispNone = new NoneMouse();
        readonly DispRectMouse dispRect = new DispRectMouse();
        readonly DispModelMouse dispModel = new DispModelMouse();

        public HDisplayUI()
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;

            // 直接组合 HDisplay + HWindowMouse：原先夹在中间的 HDisplayCore 除了这两个字段的
            // 转发外没有任何行为，却让每个新增的绘制方法都要改三处签名。
            display = new HDisplay(hWindowControl);
            mouse = new HWindowMouse(hWindowControl, display);
            mouse.RefreshUI += Display_RefreshUI;

            //HMouseDown += (s, e) => DrawHelper.Active?.OnMouseDown(e);
            //HMouseUp += (s, e) => DrawHelper.Active?.OnMouseUp(e);
            //HMouseMove += (s, e) => DrawHelper.Active?.OnMouseMove(e);

            HMouseDown  += OnMouseDown;
            HMouseUp    += OnMouseUp;
            HMouseWheel += OnMouseWheel;
            HMouseMove  += OnMouseMove;
            // 资源释放走 Dispose(bool)（见 HDisplayUI.Designer.cs），不要用 HandleDestroyed：
            // 更换 Parent / 修改 Dock / TabPage 切换等场景会重建句柄并触发 HandleDestroyed，
            // 此时控件并未销毁，提前释放 display 会让显示功能永久失效。
        }

        private void Display_RefreshUI(HTuple Row, HTuple Column, HTuple egray)
        {
            lbl_result.Text = string.Format("坐标[X:{0} Y:{1}]  灰度:{2} ", Column.D.ToString("F2"), Row.D.ToString("F2"), egray.D.ToString());
        }

        private void btn_ReSetPart_Click(object sender, EventArgs e)
        {
            display.Adaptive = !display.Adaptive;
        }

        private void but_IsCross_Click(object sender, EventArgs e)
        {
            display.IsCross = !display.IsCross;
        }

        public void Reset()
        {
            hWindowControl.Focus();
        }

        #region Mouse Events

        /// <summary>
        /// 按当前模式取出应处理鼠标事件的处理器；返回 null 表示本控件不处理该模式。
        /// </summary>
        /// <remarks>
        /// 原来四个事件各写一份三分支 switch，既重复又漏掉了 <see cref="DrawEnum"/> 的另外两个取值，
        /// 且没有 default 兜底——枚举将来再加成员时会静默无响应。这里收敛成一处解析：
        /// <para>
        /// Erase 的处理器不由 HDisplayUI 持有：它由 <c>HEditModelUI</c> 自行订阅
        /// HMouseXxx 事件驱动 <c>EraseRectMouse</c>。
        /// 因此这里显式返回 null，保持“本控件不插手”的既有行为，而不是回落到 <c>dispNone</c>
        /// ——后者会把事件转发给 <c>DrawHelper.Active</c>，属于行为变更。
        /// </para>
        /// </remarks>
        private IMouseHandler ResolveMouseHandler()
        {
            switch (_drawType)
            {
                case DrawEnum.None:
                    return dispNone;
                case DrawEnum.DispRect:
                    return dispRect;
                case DrawEnum.DispModel:
                    return dispModel;
                case DrawEnum.Erase:
                    return null;
                default:
                    Log.Warn(nameof(HDisplayUI), $"未处理的鼠标交互模式: {_drawType}，本次鼠标事件被忽略。");
                    return null;
            }
        }

        private void OnMouseDown(object sender, HMouseEventArgs e)
        {
            ReDispImage();
            ResolveMouseHandler()?.OnMouseDown(e);
        }

        private void OnMouseUp(object sender, HMouseEventArgs e)
        {
            ReDispImage();
            ResolveMouseHandler()?.OnMouseUp(e);
        }

        private void OnMouseWheel(object sender, HMouseEventArgs e)
        {
            ReDispImage();
            ResolveMouseHandler()?.OnMouseWheel(e);
        }

        private void OnMouseMove(object sender, HMouseEventArgs e)
        {
            // 移动事件不做整幅重绘：拖拽期间每个像素都刷一次图像会明显掉帧，
            // 由各 handler 自行决定要不要重绘（DispRectMouse / EraseRectMouse 都是只重画自己的区域）。
            ResolveMouseHandler()?.OnMouseMove(e);
        }

        /// <summary>
        /// 释放显示相关的托管资源，由 <see cref="Dispose(bool)"/> 的 disposing 分支调用。
        /// </summary>
        private void ReleaseDisplayResources()
        {
            // 1) 先解绑自身订阅，确保后续即便 display 内部触发事件也不再回到当前实例
            HMouseDown -= OnMouseDown;
            HMouseUp -= OnMouseUp;
            HMouseWheel -= OnMouseWheel;
            HMouseMove -= OnMouseMove;

            // 2) 先释放鼠标交互（解除它对 hWindowControl 的事件订阅），再释放 HDisplay 持有的 HObject。
            //    两者的 Dispose 自身都具备幂等性；这里不把字段置 null，
            //    避免外部在控件销毁后仍访问属性时引发 NullReferenceException——
            //    Disposed 后属性会通过各自内部的 _disposed 保护返回安全默认值。
            try { mouse?.Dispose(); }
            catch (Exception ex) { Log.Error(nameof(HDisplayUI), "释放鼠标交互资源失败.", ex); }

            try { display?.Dispose(); }
            catch (Exception ex) { Log.Error(nameof(HDisplayUI), "释放显示资源失败.", ex); }
        }

        public void SetNonePara()
        {
            Reset();
            ReDispImage();
            DrawType = DrawEnum.None;
        }

        public void SetRectPara(CvRegion shrRegion)
        {
            Reset();
            ReDispImage();
            DrawType = DrawEnum.DispRect;
            dispRect.SetUp(Display, shrRegion);
        }

        public void SetModelPara(HObject shrFindMode, HObject shrContour, CvCoord shrCoord)
        {
            Reset();
            ReDispImage();
            DrawType = DrawEnum.DispModel;
            dispModel.SetUp(Display, shrFindMode, shrContour, shrCoord);
        }

        #endregion

        #region DispImage

        // 这三个方法之所以留在控件上而没有交给 Display：DispImage 需要在刷新后触发 OnShow，
        // ReDispImage 是鼠标事件处理的内部依赖。其余图像操作（SetImage / ClearWinDisp）
        // 是纯转发，已删除，请改用 Display。

        /// <summary> 重新显示图片 </summary>
        public void ReDispImage()
        {
            display?.ReDispImage();
        }

        /// <summary> 显示图片 </summary>
        public void DispImage(HObject image)
        {
            display?.DispImage(image);
            OnShow?.Invoke();
        }

        /// <summary> 显示图片 </summary>
        public void DispImage(HObject image, bool isSetPart)
        {
            display?.DispImage(image, isSetPart);
            OnShow?.Invoke();
        }

        #endregion

        #region Draw Region

        /// <summary>
        /// 绘制（创建）橡皮筋区域
        /// </summary>
        public void DrawRegion(CvRegion hRegion)
        {
            DrawType = DrawEnum.None;
            Reset();
            display.DrawRegion(hRegion);
        }

        /// <summary>
        /// 绘制（修改）橡皮筋区域
        /// </summary>
        public void DrawRegionMod(CvRegion hRegion)
        {
            DrawType = DrawEnum.None;
            Reset();
            display.DrawRegionMod(hRegion);
        }

        /// <summary> 绘制区域 </summary>
        public void DrawRegion(RectEnum type, out HObject rectangle)
        {
            DrawType = DrawEnum.None;
            Reset();
            display.DrawRegion(type, out rectangle);
        }

        #endregion

    }
}
