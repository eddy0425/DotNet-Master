using DotNet.Drawing;
using DotNet.HalconUI;
using HalconDotNet;
using System;
using System.Windows.Forms;
using System.Collections.Generic;


namespace DotNet.HalconAlgo
{
    public partial class DisplayUI : DisplayForm
    {
        #region Events

        // 全部使用 BCL 的 EventHandler<TArgs> 标准委托;
        // 旧的自定义 DrawXxxHandler 委托类型已删除, 外部 +=/= 方法引用兼容性不变.

        public event EventHandler<DrawPointArgs>      PointEvent;
        public event EventHandler<DrawLineArgs>       LineEvent;
        public event EventHandler<DrawCircleArgs>     CircleEvent;
        public event EventHandler<DrawPolygonArgs>    PolygonEvent;
        public event EventHandler<DrawRectangleArgs>  RectangleEvent;
        public event EventHandler<DrawAffRectArgs>    AffRectEvent;
        public event EventHandler<DrawSetModelArgs>   SetModelEvent;
        public event EventHandler<DrawDispModelArgs>  DispModelEvent;
        public event EventHandler<DrawSynthethicArgs> SynthethicEvent;

        public void DrawPoint(double x, double y)
        {
            var handler = PointEvent;
            if (handler != null) handler(this, new DrawPointArgs(x, y));
        }

        public void DrawLine(double x1, double y1, double x2, double y2)
        {
            var handler = LineEvent;
            if (handler != null) handler(this, new DrawLineArgs(x1, y1, x2, y2));
        }

        public void DrawCircle(double x1, double y1, double x2, double y2)
        {
            var handler = CircleEvent;
            if (handler != null) handler(this, new DrawCircleArgs(x1, y1, x2, y2));
        }

        public void DrawPolygon(HObject contour)
        {
            var handler = PolygonEvent;
            if (handler != null) handler(this, new DrawPolygonArgs(contour));
        }

        public void DrawRectangle(string name, Point2d topLeft, Point2d bottomRight)
        {
            var handler = RectangleEvent;
            if (handler != null) handler(this, new DrawRectangleArgs(name, topLeft, bottomRight));
        }

        public void DrawAffRect(string name, Point2d center, Size2d rectSize, double phi)
        {
            var handler = AffRectEvent;
            if (handler != null) handler(this, new DrawAffRectArgs(name, center, rectSize, phi));
        }

        public void DrawSetModel(string name, Point2d topLeft, Point2d bottomRight, DisplayUI display)
        {
            var handler = SetModelEvent;
            if (handler != null) handler(this, new DrawSetModelArgs(name, topLeft, bottomRight, display));
        }

        public void DrawDispModel(string name, DisplayUI display)
        {
            var handler = DispModelEvent;
            if (handler != null) handler(this, new DrawDispModelArgs(name, display));
        }

        // 命名保留兼容: Synthethicn (尾部 n) 是历史 API
        public void DrawSynthethicn(HObject contour, Point2d topLeft, Point2d bottomRight)
        {
            var handler = SynthethicEvent;
            if (handler != null) handler(this, new DrawSynthethicArgs(contour, topLeft, bottomRight));
        }

        #endregion

        #region 共享状态 Share -> Shr; Context -> Ctx; Algorithms -> Algo

        /// <summary> 算法名称 </summary>
        public string AlgoName;

        /// <summary> 当前设置步骤 </summary>
        public SetUpEnum SetUp = SetUpEnum.None;

        /// <summary> 当前循环移动状态 </summary>
        public CycleMoveEnum CycleMove = CycleMoveEnum.None;

        /// <summary> 共享中心 </summary>
        public Point2d ShrCenter;

        /// <summary> 共享矩形区域 </summary>
        public CvRegion ShrRegion;

        /// <summary> 共享轮廓对象 </summary>
        public HObject ShrContour;

        /// <summary> 共享多边形点集合 </summary>
        public List<Point2d> ShrPolygons = new List<Point2d>();

        #endregion

        #region 绘图模式

        private DrawEnum _drawType = DrawEnum.None;
        private IDrawHandler _currentHandler;
        private readonly DrawHandlerFactory _handlerFactory = new DrawHandlerFactory();

        /// <summary> 当前绘图类型. setter 中切换 handler 实例 </summary>
        public DrawEnum DrawType
        {
            get { return _drawType; }
            set
            {
                if (_drawType == value && _currentHandler != null) return;
                _drawType = value;
                _currentHandler = _handlerFactory.Create(value);
            }
        }

        #endregion

        #region 构造 + 鼠标事件路由

        public DisplayUI()
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;

            HMouseDown  += WindowMouse_HMouseDown;
            HMouseUp    += WindowMouse_HMouseUp;
            HMouseWheel += WindowMouse_HMouseWheel;
            HMouseMove  += WindowMouse_HMouseMove;

            _currentHandler = _handlerFactory.Create(DrawEnum.None);

            // 控件销毁时主动释放 HObject, 避免依赖 GC + finalizer 的滞后回收
            HandleDestroyed += DisplayUI_HandleDestroyed;

            ShrRegion = new CvRegion();
            HOperatorSet.GenEmptyObj(out ShrContour);
        }

        private void DisplayUI_HandleDestroyed(object sender, EventArgs e)
        {
            if (ShrContour != null)
            {
                ShrContour.Dispose();
                ShrContour = null;
            }
        }

        private void WindowMouse_HMouseDown(object sender, HMouseEventArgs e)
        {
            ReDisplay();
            _currentHandler.OnMouseDown(this, e);
        }

        private void WindowMouse_HMouseUp(object sender, HMouseEventArgs e)
        {
            ReDisplay();
            _currentHandler.OnMouseUp(this, e);
        }

        private void WindowMouse_HMouseWheel(object sender, HMouseEventArgs e)
        {
            ReDisplay();
            _currentHandler.OnMouseWheel(this, e);
        }

        private void WindowMouse_HMouseMove(object sender, HMouseEventArgs e)
        {
            ReDisplay();
            _currentHandler.OnMouseMove(this, e);
        }

        private void ReDisplay()
        {
            if (_currentHandler != null && _currentHandler.NeedReDisp)
            {
                ReDispImage();
            }
        }

        #endregion

        #region 公共 API

        /// <summary> 由 XLD 轮廓生成"白底黑色填充"的掩膜图像 </summary>
        public void GetContourImage(HObject contour, out HObject ho_ResultImage)
        {
            HObject ho_Region = null;
            HObject ho_WhiteImage = null;
            ho_ResultImage = null;
            try
            {
                HOperatorSet.GenRegionContourXld(contour, out ho_Region, "filled");
                HOperatorSet.GenImageProto(HoImage, out ho_WhiteImage, 255);
                HOperatorSet.PaintRegion(ho_Region, ho_WhiteImage, out ho_ResultImage, 0, "fill");
            }
            finally
            {
                if (ho_Region      != null) ho_Region.Dispose();
                if (ho_WhiteImage  != null) ho_WhiteImage.Dispose();
            }
        }

        /// <summary>
        /// 注册 / 替换某 DrawType 的处理器实例 (兼容旧 API).
        /// 推荐使用 <see cref="RegisterDrawHandler(DrawEnum, Func{IDrawHandler})"/> 工厂版本.
        /// </summary>
        public void RegisterDrawHandler(DrawEnum type, IDrawHandler handler)
        {
            _handlerFactory.Register(type, handler);
        }

        /// <summary>
        /// 注册 / 替换某 DrawType 的处理器工厂 (推荐: 每次切换都创建新实例).
        /// </summary>
        public void RegisterDrawHandler(DrawEnum type, Func<IDrawHandler> factory)
        {
            _handlerFactory.Register(type, factory);
        }

        /// <summary> 设置绘图模式 </summary>
        public void SetDrawMode(string algoName, DrawEnum type)
        {
            DrawType = type;
            AlgoName = algoName;
            SetUp = SetUpEnum.None;
            CycleMove = CycleMoveEnum.None;

            ReDisplay();
            Reset();
            _currentHandler.SetUp(this);
        }

        /// <summary> 设置绘图模式 + 共享区域 </summary>
        public void SetDrawMode(string algoName, CvRegion hRegion, DrawEnum type)
        {
            if (hRegion != CvRegion.Empty && hRegion != null)
            {
                ShrRegion.Type = hRegion.Type;
                ShrRegion.Update2Point(hRegion.TopLeft, hRegion.BottomRight);
                ShrRegion.GenRegion();
            }
 
            DrawType  = type;
            AlgoName  = algoName;
            SetUp     = SetUpEnum.None;
            CycleMove = CycleMoveEnum.None;

            ReDisplay();
            Reset();
            _currentHandler.SetUp(this);
        }

        #endregion

    }
}
