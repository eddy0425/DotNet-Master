using DotNet.Drawing;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Windows.Forms;


namespace DotNet.HalconUI
{
    public partial class HDisplayUI : UserControl, IHDisplay
    {
        readonly HDisplayCore display;
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
        public double HoWidth => display.HoWidth;
        public double HoHeight => display.HoHeight;
        public Size2d HoSize => display.Size;
        public Point2d HoCentre => display.Centre;
        public HObject HoImage => display.HoImage;  //图像
        public HWindow HoWindow => hWindowControl.HalconWindow;  //窗体句柄
        public bool HoMouseDown { get { return display.MouseDown; } set { display.MouseDown = value; } } //鼠标按下
        public bool HoMouseDouble { get { return display.MouseDouble; } set { display.MouseDouble = value; } }  //鼠标双击按下
        public bool IsCross { get { return display.IsCross; } set { display.IsCross = value; } } //是否画十字
        public bool Adaptive { get { return display.Adaptive; } set { display.Adaptive = value; } } //自适应

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

            display = new HDisplayCore(hWindowControl);
            display.RefreshUI += Display_RefreshUI;

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
            Adaptive = !Adaptive;
        }

        private void but_IsCross_Click(object sender, EventArgs e)
        {
            IsCross = !IsCross;
        }

        public void Reset()
        {
            hWindowControl.Focus();
        }


        /// <summary> 设置绘制模式 </summary>
        /// <param name="mode">"margin" 外接矩形, "fill" 填充矩形</param>
        public void SetDraw(string mode)
        {
            display.SetDraw(mode);
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

            // 2) 释放 HDisplayCore（它会级联释放 HWindowMouse / HWindowImage / HDisplay 中持有的 HObject）
            //    HDisplayCore.Dispose 自身已具备幂等性；这里不把 display 字段置 null，
            //    避免外部在控件销毁后仍访问属性时引发 NullReferenceException——
            //    Disposed 后属性会通过 HDisplayCore 内部的 _disposed 保护返回安全默认值。
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
            dispRect.SetUp(this, shrRegion);
        }

        public void SetModelPara(HObject shrFindMode, HObject shrContour, CvCoord shrCoord)
        {
            Reset();
            ReDispImage();
            DrawType = DrawEnum.DispModel;
            dispModel.SetUp(this, shrFindMode, shrContour, shrCoord);
        }

        #endregion

        #region IHWindowFont

        /// <summary> 获取颜色 </summary>
        public string GetColor()
        {
            return display.GetColor();
        }

        /// <summary> 设置颜色 </summary>
        public void SetColor(string color)
        {
            display.SetColor(color);
        }

        /// <summary> 设置字体大小 </summary>
        public void SetFontSize(HTuple hv_Size)
        {
            display.SetFontSize(hv_Size);
        }

        /// <summary> 显示字体 </summary>
        public void DispText(string message, HTuple FontX, HTuple FontY, string color)
        {
            display.DispText(message, FontX, FontY, color);
        }

        /// <summary> 显示字体 </summary>
        public void DispText(string message, HTuple FontX, HTuple FontY, HTuple size, string color)
        {
            display.DispText(message, FontX, FontY, size, color);
        }

        #endregion

        #region DispImage

        /// <summary> 设置图像 </summary>
        public void SetImage(HObject image)
        {
            display?.SetImage(image);
        }

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

        public void ClearWinDisp(HObject objectVal)
        {
            display?.ClearWinDisp(objectVal);
        }

        #endregion


        #region 点相关
        public void DispPoint(double crossX, double crossY, double size = 20)
        {
            display.DispPoint(crossX, crossY, size);
        }
        public void DispPoint(double crossX, double crossY, string color, int size = 20)
        {
            display.DispPoint(crossX, crossY, color, size);
        }
        public void DispPoint(HTuple crossX, HTuple crossY, double size = 20)
        {
            display.DispPoint(crossX, crossY, size);
        }
        public void DispPoint(HTuple crossX, HTuple crossY, string color, int size = 20)
        {
            display.DispPoint(crossX, crossY, color, size);
        }
        public void DispPoint(double[] rowPoints, double[] columnPoints, int size = 20)
        {
            display.DispPoint(rowPoints, columnPoints, size);
        }
        public void DispPoint(double[] rowPoints, double[] columnPoints, string color, int size = 20)
        {
            display.DispPoint(rowPoints, columnPoints, color, size);
        }
        public void DispPoint(List<Point2d> polygons, int size = 20)
        {
            display.DispPoint(polygons, size);
        }
        public void DispPoint(List<Point2d> polygons, string color, int size = 20)
        {
            display.DispPoint(polygons, color, size);
        }
        public void DispPoint(Point2d point, double size = 20)
        {
            display.DispPoint(point, size);
        }
        public void DispPoint(Point2d point, string color, double size = 20)
        {
            display.DispPoint(point, color, size);
        }

        #endregion

        #region 坐标相关
        public void DispCross(double crossX, double crossY, double angle, double size = 20)
        {
            display.DispCross(crossX, crossY, angle, size);
        }
        public void DispCross(double crossX, double crossY, double angle, string color, double size = 20)
        {
            display.DispCross(crossX, crossY, angle, color, size);
        }
        public void DispCross(Point2d point, double angle, double size = 20)
        {
            display.DispCross(point, angle, size);
        }
        public void DispCross(Point2d point, double angle, string color, double size = 20)
        {
            display.DispCross(point, angle, color, size);
        }
        public void DispCross(CvCoord coord, double size = 20)
        {
            display.DispCross(coord, size);
        }
        public void DispCross(CvCoord coord, string color, double size = 20)
        {
            display.DispCross(coord, color, size);
        }

        #endregion

        #region 线相关
        public void DispLine(double startX, double startY, double endX, double endY)
        {
            display.DispLine(startX, startY, endX, endY);
        }
        public void DispLine(double startX, double startY, double endX, double endY, string color)
        {
            display.DispLine(startX, startY, endX, endY, color);
        }
        public void DispLine(CvLine line)
        {
            display.DispLine(line);
        }
        public void DispLine(CvLine line, string color)
        {
            display.DispLine(line, color);
        }
        public void DispLine(CvLine line, int radius)
        {
            display.DispLine(line, radius);
        }
        public void DispLine(CvLine line, int radius, string color)
        {
            display.DispLine(line, radius, color);
        }

        /// <summary>
        /// 画两点一线
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="step"></param>
        /// <param name="color"></param>
        public void DispLine(Point2d point1, Point2d point2, int step)
        {
            display.DispLine(point1, point2, step);
        }

        /// <summary>
        /// 画两点一线
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <param name="step"></param>
        /// <param name="color"></param>
        public void DispLine(Point2d point1, Point2d point2, int step, string color)
        {
            display.DispLine(point1, point2, step, color);
        }

        #endregion

        #region 方向线
        public void DispArrow(double startX, double startY, double endX, double endY, double size = 20)
        {
            display.DispArrow(startX, startY, endX, endY, size);
        }
        public void DispArrow(double startX, double startY, double endX, double endY, string color, double size = 20)
        {
            display.DispArrow(startX, startY, endX, endY, color, size);
        }
        public void DispArrow(CvLine line, double size = 20)
        {
            display.DispArrow(line, size);
        }
        public void DispArrow(CvLine line, string color, double size = 20)
        {
            display.DispArrow(line, color, size);
        }
        public void DispArrow(CvArrow arrow)
        {
            display.DispArrow(arrow);
        }
        public void DispArrow(CvArrow arrow, string color)
        {
            display.DispArrow(arrow, color);
        }

        #endregion

        #region 圆
        public void DispCircle(double crossX, double crossY, double radius)
        {
            display.DispCircle(crossX, crossY, radius);
        }
        public void DispCircle(double crossX, double crossY, double radius, string color)
        {
            display.DispCircle(crossX, crossY, radius, color);
        }
        public void DispCircle(CvCircle circle)
        {
            display.DispCircle(circle);
        }
        public void DispCircle(CvCircle circle, string color)
        {
            display.DispCircle(circle, color);
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

        #region 区域相关

        /// <summary> 显示橡皮筋区域 </summary>
        public void DispGenRegion(CvRegion hRegion)
        {
            display.DispGenRegion(hRegion);
        }

        /// <summary> 获取坐标区域并显示 </summary>
        public void GenCoordsRegion(CvRegion hRegion, List<CvCoord> coords)
        {
            display.GenCoordsRegion(hRegion, coords);
        }

        #endregion


        #region Region

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        public void DispRegion(HObject hRegion)
        {
            display.DispRegion(hRegion);
        }

        /// <summary>
        /// 显示ROI区域
        /// </summary>
        public void DispRegion(HObject hRegion, string color)
        {
            display.DispRegion(hRegion, color);
        }

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        public void DispRegion(CvRegion hRegion)
        {
            display.DispRegion(hRegion);
        }

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        public void DispRegion(CvRegion hRegion, string color)
        {
            display.DispRegion(hRegion, color);
        }

        /// <summary>
        /// 显示橡皮筋区域
        /// </summary>
        public void DispCvRegion(CvRegion hRegion)
        {
            display.DispCvRegion(hRegion);
        }

        public void DispRectangle2(HTuple centerRow, HTuple centerCol, HTuple phi, HTuple length1, HTuple length2)
        {
            display.DispRectangle2(centerRow, centerCol, phi, length1, length2);
        }

        public void DispRectangle2(HTuple centerRow, HTuple centerCol, HTuple phi, HTuple length1, HTuple length2, string color)
        {
            display.DispRectangle2(centerRow, centerCol, phi, length1, length2, color);
        }

        #endregion

    }
}
