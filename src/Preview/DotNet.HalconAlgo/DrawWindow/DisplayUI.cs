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

        #region 属性

        // 绘图处理器相关
        private DrawEnum _drawType = DrawEnum.None;
        private IDrawHandler _currentHandler;
        private DrawHandlerFactory _handlerFactory;

        #endregion

        #region HWindowMouse - 插件模式

        /// <summary> 当前绘图类型 </summary>
        public DrawEnum DrawType
        {
            get => _drawType;
            set
            {
                if (_drawType != value)
                {
                    _drawType = value;
                    _currentHandler = _handlerFactory.GetHandler(value);
                }
            }
        }

        public DisplayUI()
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;

            HMouseDown += WindowMouse_HMouseDown;
            HMouseUp += WindowMouse_HMouseUp;
            HMouseWheel += WindowMouse_HMouseWheel;
            HMouseMove += WindowMouse_HMouseMove;

            _handlerFactory = new DrawHandlerFactory();
            _currentHandler = _handlerFactory.GetHandler(DrawEnum.None);

            RectangleEvent += _drawContext_RectangleEvent;
            PolygonEvent += _drawContext_PolygonEvent;
            SynthethicEvent += _drawContext_SynthethicEvent;

            ShrContour = new HObject(); HOperatorSet.GenEmptyObj(out ShrContour); // 创建初始空对象
        }

        #region HMouse
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
            HObject ho_Region; HOperatorSet.GenEmptyObj(out ho_Region);
            HObject ho_WhiteImage; HOperatorSet.GenEmptyObj(out ho_WhiteImage);
            HOperatorSet.GenEmptyObj(out ho_ResultImage);
            try
            {
                // 获取源图像尺寸
                HOperatorSet.GetImageSize(HoImage, out HTuple hv_Width, out HTuple hv_Height);

                // 将 XLD 轮廓转换为区域（"filled" 表示填充内部）
                ho_Region.Dispose();
                HOperatorSet.GenRegionContourXld(contour, out ho_Region, "filled");

                // 创建白色背景图像（灰度值 255）
                ho_WhiteImage.Dispose();
                HOperatorSet.GenImageProto(HoImage, out ho_WhiteImage, 255);

                // 将轮廓区域涂黑（灰度值 0）
                ho_ResultImage.Dispose();
                HOperatorSet.PaintRegion(ho_Region, ho_WhiteImage, out ho_ResultImage, 0, "fill");

                //// 显示结果图像
                //DispImage(ho_ResultImage);

                // 可选：保存结果图像
                // HOperatorSet.WriteImage(ho_ResultImage, "png", 0, "MaskImage.png");

            }
            finally
            {
                ho_Region.Dispose();
                ho_WhiteImage.Dispose();
            }
        }

        #endregion

        /// <summary>
        /// 注册自定义绘图处理器
        /// 用于扩展新的绘图类型
        /// </summary>
        /// <param name="type">绘图类型</param>
        /// <param name="handler">处理器实例</param>
        public void RegisterDrawHandler(DrawEnum type, IDrawHandler handler)
        {
            _handlerFactory.Register(type, handler);
        }

        /// <summary>
        /// 设置绘图模式
        /// </summary>
        /// <param name="type">绘图类型</param>
        public void SetDrawMode(string algoName, DrawEnum type)
        {
            DrawType = type;
            AlgoName = algoName;
            SetUp = SetUpEnum.None;        //设置步骤
            CycleMove = CycleMoveEnum.None;   //循环移动状态

            ReDisplay();
            Reset();
            _currentHandler.SetUp(this);
        }

        /// <summary>
        /// 设置绘图模式
        /// </summary>
        /// <param name="type">绘图类型</param>
        public void SetDrawMode(string algoName, CvRegion hRegion, DrawEnum type)
        {
            DrawType = type;
            AlgoName = algoName;
            ShrRegion = hRegion;
            SetUp = SetUpEnum.None;           //设置步骤
            CycleMove = CycleMoveEnum.None;      //循环移动状态

            ReDisplay();
            Reset();
            _currentHandler.SetUp(this);
        }

        #endregion
    }
}
