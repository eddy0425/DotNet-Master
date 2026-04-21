using DotNet.Drawing;
using DotNet.HalconUI;
using HalconDotNet;
using System.Windows.Forms;
using System.Collections.Generic;


namespace DotNet.HalconAlgo
{
    public partial class DisplayUI : DisplayForm
    {
        #region Event

        public event DrawPointHandler PointEvent;
        public void DrawPoint(double x, double y)
        {
            if (PointEvent != null)
            {
                var e = new DrawPointArgs(x, y);
                PointEvent(this, e);
            }
        }


        public event DrawLineHandler LineEvent;
        public void DrawLine(double x1, double y1, double x2, double y2)
        {
            if (LineEvent != null)
            {
                var e = new DrawLineArgs(x1, y1, x2, y2);
                LineEvent(this, e);
            }
        }


        public event DrawCircleHandler CircleEvent;
        public void DrawCircle(double x1, double y1, double x2, double y2)
        {
            if (CircleEvent != null)
            {
                var e = new DrawCircleArgs(x1, y1, x2, y2);
                CircleEvent(this, e);
            }
        }


        public event DrawPolygonHandler PolygonEvent;
        public void DrawPolygon(HObject contour)
        {
            if (PolygonEvent != null)
            {
                var e = new DrawPolygonArgs(contour);
                PolygonEvent(this, e);
            }
        }


        public event DrawRectangleHandler RectangleEvent;
        public void DrawRectangle(string name, Point2d topLeft, Point2d bottomRight)
        {
            if (RectangleEvent != null)
            {
                var e = new DrawRectangleArgs(name, topLeft, bottomRight);
                RectangleEvent(this, e);
            }
        }

        public event DrawAffRectHandler AffRectEvent;
        public void DrawAffRect(string name, Point2d center, Size2d rectSize, double phi)
        {
            if (AffRectEvent != null)
            {
                var e = new DrawAffRectArgs(name, center, rectSize, phi);
                AffRectEvent(this, e);
            }
        }


        public event DrawSetModelHandler SetModelEvent;
        public void DrawSetModel(string name, Point2d topLeft, Point2d bottomRight, DisplayUI display)
        {
            if (SetModelEvent != null)
            {
                var e = new DrawSetModelArgs(name, topLeft, bottomRight, display);
                SetModelEvent(this, e);
            }
        }

        public event DrawDispModelHandler DispModelEvent;
        public void DrawDispModel(string name, DisplayUI display)
        {
            if (DispModelEvent != null)
            {
                var e = new DrawDispModelArgs(name, display);
                DispModelEvent(this, e);
            }
        }


        public event DrawSynthethicHandler SynthethicEvent;
        public void DrawSynthethicn(HObject contour, Point2d topLeft, Point2d bottomRight)
        {
            if (SynthethicEvent != null)
            {
                var e = new DrawSynthethicArgs(contour, topLeft, bottomRight);
                SynthethicEvent(this, e);
            }
        }

        #endregion


        #region 共享状态 Share → Shr ; Context → Ctx ; Algorithms → Algo 

        /// <summary> 算法名称 </summary> Context
        public string AlgoName;

        /// <summary> 当前设置步骤 </summary>
        public SetUpEnum SetUp = SetUpEnum.None;

        /// <summary> 当前循环移动状态  </summary>
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

        #region DrawHandler
        private void _drawContext_RectangleEvent(object sender, DrawRectangleArgs e)
        {
            //// 更新模板中心
            //UnSetTemplateCenter();
            //SetTemplateCenter(e.Center);

            //// 查找形状模型
            //HalconHelper.FindShapeModel(srcImage, ho_Rectangle, objectParam, out center);
            //_drawContext.Center = center;

            //// 计算反向仿射变换
            //UnHomMat2D = new HTuple();
            //var p = e.Center;
            //if (p != null && center != null)
            //{
            //    HOperatorSet.VectorAngleToRigid(p.Y, p.X, 0, center.Y, center.X, 0, out UnHomMat2D);
            //}
        }

        private void _drawContext_PolygonEvent(object sender, DrawPolygonArgs e)
        {
            ShrContour = e.ho_Contour;
            GetContourImage(ShrContour, out HObject ho_ResultImage);

            //try
            //{
            //    ho_Contour.Dispose();
            //    HalconHelper.SetModelID(ho_ResultImage, ho_Rectangle, objectParam, out ho_Contour, out center);
            //    _drawContext.HoContour = ho_Contour;
            //}
            //catch (Exception ex)
            //{
            //    System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
            //}
            //finally
            //{
            //    ho_ResultImage.Dispose();
            //}
        }

        private void _drawContext_SynthethicEvent(object sender, DrawSynthethicArgs e)
        {
            //ho_Contour = e.ho_Contour;
            //GetContourImage(ho_Contour, out HObject ho_ResultImage);

            //try
            //{
            //    ho_Contour.Dispose();
            //    HalconHelper.SetModelID(ho_ResultImage, ho_Rectangle, objectParam, out ho_Contour, out center);
            //    HalconHelper.FindShapeModel(srcImage, ho_Rectangle, objectParam, out center); // 查找形状模型

            //    SetTemplateCenter(e.Center);

            //    HalconHelper.FindShapeModel(srcImage, ho_Rectangle, objectParam, out center); // 查找形状模型
            //    _drawContext.Polygons = new List<Point2d>();
            //    _drawContext.HoContour = ho_Contour;
            //    _drawContext.Center = center;

            //    // 计算反向仿射变换
            //    UnHomMat2D = new HTuple();
            //    var p = e.Center;
            //    if (p != null && center != null)
            //    {
            //        HOperatorSet.VectorAngleToRigid(p.Y, p.X, 0, center.Y, center.X, 0, out UnHomMat2D);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
            //}
            //finally
            //{
            //    ho_ResultImage.Dispose();
            //}
        }

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
        public void SetDrawMode(string algorithmsName, DrawEnum type)
        {
            DrawType = type;
            AlgoName = algorithmsName;
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
        public void SetDrawMode(string algorithmsName, CvRegion hRegion, DrawEnum type)
        {
            DrawType = type;
            AlgoName = algorithmsName;
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
