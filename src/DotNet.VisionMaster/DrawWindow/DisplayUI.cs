using DotNet.Drawing;
using DotNet.HalconUI;
using HalconDotNet;
using System.Windows.Forms;

namespace DotNet.VisionMaster
{
    public partial class DisplayUI : DisplayForm
    {
        public delegate void TestImageRun(out double time);
        public TestImageRun testImageRun;

        public delegate void PageReturn();
        public PageReturn pageReturn;

        public delegate void SetModelIDEven(out int count, out HObject rectangle);
        public SetModelIDEven setModelIDEven;

        public delegate void FindGenericShapeModel(out double time);
        public FindGenericShapeModel findGenericShapeModel;


        #region 属性

        HObject ho_Contour;
        HObject ho_Rectangle;

        // 绘图处理器相关
        private WinDrawType _drawType = WinDrawType.None;
        private IDrawHandler _currentHandler;
        private DrawContext _drawContext;
        private DrawHandlerFactory _handlerFactory;

        #endregion

        #region HWindowMouse - 插件模式

        /// <summary>
        /// 当前绘图类型
        /// </summary>
        public WinDrawType DrawType
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

        /// <summary>
        /// 当前绘图类型
        /// </summary>
        public DrawContext DrawContext
        {
            get => _drawContext;
        }

        public void FormDispose()
        {
            ho_Contour.Dispose();
            ho_Rectangle.Dispose();
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
            _drawContext = new DrawContext(this);
            _drawContext.RectangleEvent += _drawContext_RectangleEvent;
            _drawContext.PolygonEvent += _drawContext_PolygonEvent;
            _drawContext.SynthethicEvent += _drawContext_SynthethicEvent;
            _currentHandler = _handlerFactory.GetHandler(WinDrawType.None);

            ho_Contour = new HObject(); HOperatorSet.GenEmptyObj(out ho_Contour); // 创建初始空对象
            ho_Rectangle = new HObject(); HOperatorSet.GenEmptyObj(out ho_Rectangle); // 创建初始空对象
        }

        #region HMouse
        private void WindowMouse_HMouseDown(object sender, HMouseEventArgs e)
        {
            ReDisplay();
            _currentHandler.OnMouseDown(_drawContext, e);
        }

        private void WindowMouse_HMouseUp(object sender, HMouseEventArgs e)
        {
            ReDisplay();
            _currentHandler.OnMouseUp(_drawContext, e);
        }

        private void WindowMouse_HMouseWheel(object sender, HMouseEventArgs e)
        {
            ReDisplay();
            _currentHandler.OnMouseWheel(_drawContext, e);
        }

        private void WindowMouse_HMouseMove(object sender, HMouseEventArgs e)
        {
            ReDisplay();
            _currentHandler.OnMouseMove(_drawContext, e);
        }

        private void ReDisplay()
        {
            if (_currentHandler != null && _currentHandler.NeedReDispImage)
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
            ho_Contour = e.ho_Contour;
            GetContourImage(ho_Contour, out HObject ho_ResultImage);

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
        public void RegisterDrawHandler(WinDrawType type, IDrawHandler handler)
        {
            _handlerFactory.Register(type, handler);
        }

        /// <summary>
        /// 设置绘图模式
        /// </summary>
        /// <param name="type">绘图类型</param>
        public void SetDrawMode(string Nmae, WinDrawType type)
        {
            ReDisplay();
            Reset();
            DrawType = type;
            _drawContext.Name = Nmae;
            _drawContext.SetUp = SetUpEnum.None;           //设置步骤
            _drawContext.CycleMove = CycleMoveEnum.None;   //循环移动状态
            _currentHandler.SetUp(_drawContext);
        }

        /// <summary>
        /// 设置绘图模式
        /// </summary>
        /// <param name="type">绘图类型</param>
        public void SetDrawMode(string Nmae, CvRegion HoRegion, WinDrawType type)
        {
            ReDisplay();
            Reset();
            DrawType = type;
            _drawContext.Name = Nmae;
            _drawContext.HoRegion = HoRegion;
            _drawContext.SetUp = SetUpEnum.None;           //设置步骤
            _drawContext.CycleMove = CycleMoveEnum.None;   //循环移动状态
            _currentHandler.SetUp(_drawContext);
        }

        #endregion
    }
}
