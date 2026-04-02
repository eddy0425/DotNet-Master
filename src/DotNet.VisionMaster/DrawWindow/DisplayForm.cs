using System;
using System.Windows.Forms;
using System.Collections.Generic;
using OpenCvSharp;
using HalconDotNet;
using DotNet.HWindows;

namespace DotNet.VisionMaster
{
    public partial class DisplayForm : Form
    {
        bool _isUpdating = false;

        public HObject srcImage;

        HObject ho_Rectangle;
        HObject ho_Contour;
        Point2d center;


        HWindowImage hWindowImage;
        HWindowMouse hWindowMouse;
        HWindowFont2018 hWindowFont;

        public delegate void TestImageRun(out double time);
        public TestImageRun testImageRun;

        public delegate void PageReturn();
        public PageReturn pageReturn;

        public delegate void SetModelIDEven(out int count, out HObject rectangle);
        public SetModelIDEven setModelIDEven;

        public delegate void FindGenericShapeModel(out double time);
        public FindGenericShapeModel findGenericShapeModel;

        public DisplayForm()
        {
            InitializeComponent();

            srcImage = new HObject(); HOperatorSet.GenEmptyObj(out srcImage); // 创建初始空对象
            ho_Rectangle = new HObject(); HOperatorSet.GenEmptyObj(out ho_Rectangle); // 创建初始空对象
            ho_Contour = new HObject(); HOperatorSet.GenEmptyObj(out ho_Contour); // 创建初始空对象

            hWindowFont = new HWindowFont2018(hWindow);
            hWindowImage = new HWindowImage(hWindow, hWindowControl1);
            hWindowMouse = new HWindowMouse(hWindow, hWindowControl1, hWindowImage);

            // 初始化绘图处理器系统
            InitializeDrawHandlers();

            hWindowMouse.HMouseDown += WindowMouse_HMouseDown;
            hWindowMouse.HMouseUp += WindowMouse_HMouseUp;
            hWindowMouse.HMouseWheel += WindowMouse_HMouseWheel;
            hWindowMouse.HMouseMove += WindowMouse_HMouseMove;

            InitHalcon();
        }

        public void InitHalcon()
        {
            //5120x3840  //512 × 512
            HImage hImage = new HImage("byte", 800, 600);
            try
            {
                DispImage(hImage);
            }
            finally
            {
                hImage.Dispose();
            }
        }

        private void FormDispose()
        {
            hWindow.Dispose();
            hWindowControl1.Dispose();
            hWindowImage.Dispose();
            srcImage.Dispose();
            ho_Contour.Dispose();
        }


        #region Halcon

        bool IsCross;          //是否画十字
        bool Adaptive = true;  //自适应

        HWindow hWindow { get { return hWindowControl1.HalconWindow; } }  //窗体句柄
        HWindowMouse MouseEvent { get { return hWindowMouse; } }  //窗体句柄
        double ho_Width { get { return hWindowImage.Width; } }
        double ho_Height { get { return hWindowImage.Height; } }

        public void Reset()
        {
            hWindowControl1.Focus();
        }

        /// <summary> 设置字体大小 </summary>
        public void SetFontSize(HTuple hv_Size)
        {
            hWindowFont.SetFontSize(hv_Size);
        }

        /// <summary> 设置颜色 </summary>
        public void SetColor(string color)
        {
            // 确保 hWindow 不是 null
            if (hWindow == null)
            {
                throw new ArgumentNullException(nameof(hWindow), "HALCON window handle is null.");
            }

            // 使用 IsInitialized 检查 hWindow 是否有效
            if (!hWindow.IsInitialized())
            {
                throw new InvalidOperationException("HALCON window handle is not initialized.");
            }

            // 确保颜色字符串有效
            if (color == null)
            {
                color = HColor.Red; // 默认颜色
            }

            hWindow.SetColor(color);
        }


        /// <summary> 显示字体 </summary>
        public void DispText(string message, HTuple FontX, HTuple FontY, string color)
        {
            hWindowFont.DispText(message, FontY, FontX, color);
        }

        /// <summary> 显示字体 </summary>
        public void DispText(string message, HTuple FontX, HTuple FontY, HTuple size, string color)
        {
            hWindowFont.DispText(message, FontY, FontX, size, color);
        }

        /// <summary> 显示区域 </summary>
        public void DispRegion(HObject hRegion)
        {
            if (hRegion.NotNull()) hWindow.DispObj(hRegion);
        }

        /// <summary> 显示区域 </summary>
        public void DispRegion(HObject hRegion, string color)
        {
            SetColor(color);
            if (hRegion.NotNull()) hWindow.DispObj(hRegion);
        }

        /// <summary> 重新显示图片 </summary>
        public void ReDispImage()
        {
            hWindowImage.Fun_Redisplay();
        }

        /// <summary> 显示图片 </summary>
        public void DispImage(HObject image)
        {
            try
            {
                HOperatorSet.GenEmptyObj(out srcImage); srcImage.Dispose();
                srcImage = image.Clone();
                DispImage(srcImage, Adaptive);
            }
            catch
            {

            }
        }

        /// <summary> 显示图片 </summary>
        public void DispImage(HObject image, bool isSetPart)
        {
            hWindowImage.Fun_DispImage(image, isSetPart);

            if (IsCross)
            {
                double size = ho_Width > ho_Height ? ho_Width : ho_Height;
                HOperatorSet.DispCross(hWindow, ho_Height / 2, ho_Width / 2, size, 0);
            }
        }

        int crossSize = 20;

        public void DispCross(double row, double column)
        {
            hWindow.DispCross(row, column, crossSize, 0);
        }

        public void DispCross(double row, double column, string color)
        {
            SetColor(color);
            hWindow.DispCross(row, column, crossSize, 0);
        }

        public void DispCross(double row, double column, int crossSize, string color)
        {
            SetColor(color);
            hWindow.DispCross(row, column, crossSize, 0);
        }

        public void DispCross(double[] rowPoints, double[] columnPoints, string color)
        {
            SetColor(color);

            if (rowPoints.Length != columnPoints.Length) return;

            for (int i = 0; i < rowPoints.Length; i++)
            {
                hWindow.DispCross(rowPoints[i], columnPoints[i], crossSize, 0);
            }
        }

        public void DispCross(List<Point2d> Polygons, string color)
        {
            SetColor(color);

            if (Polygons.Count == 0) return;

            for (int i = 0; i < Polygons.Count; i++)
            {
                hWindow.DispCross(Polygons[i].Y, Polygons[i].X, crossSize, 0);
            }
        }
        public void DispLine(CvLine line, string color)
        {
            SetColor(color);
            hWindow.DispLine(line.start.Y, line.start.X, line.end.Y, line.end.X);
        }

        #endregion

        #region UI

        private void ObjectParamForm_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible)
            {
                try
                {
                    _isUpdating = true;

                    HOperatorSet.ReadImage(out srcImage, "PR.png");
                    DispImage(srcImage);
                }
                finally
                {
                    _isUpdating = false;
                }
            }
        }

        #endregion

        #region HWindowMouse - 插件模式

        // 绘图处理器相关
        private WinDrawType _drawType = WinDrawType.None;
        private IDrawHandler _currentHandler;
        private DrawContext _drawContext;
        private DrawHandlerFactory _handlerFactory;

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

        /// <summary>
        /// 初始化绘图处理器系统
        /// </summary>
        private void InitializeDrawHandlers()
        {
            _handlerFactory = new DrawHandlerFactory();
            _drawContext = new DrawContext(hWindow);
            _drawContext.RectangleEvent += _drawContext_RectangleEvent;
            _drawContext.PolygonEvent += _drawContext_PolygonEvent;
            _drawContext.SynthethicEvent += _drawContext_SynthethicEvent;
            _currentHandler = _handlerFactory.GetHandler(WinDrawType.None);
        }

        private void _drawContext_RectangleEvent(object sender, DrawContext.DrawRectangleArgs e)
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

        private void _drawContext_PolygonEvent(object sender, DrawContext.DrawPolygonArgs e)
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

        private void _drawContext_SynthethicEvent(object sender, DrawContext.DrawSynthethicArgs e)
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


        private void GetContourImage(HObject contour, out HObject ho_ResultImage)
        {
            HObject ho_Region; HOperatorSet.GenEmptyObj(out ho_Region);
            HObject ho_WhiteImage; HOperatorSet.GenEmptyObj(out ho_WhiteImage);
            HOperatorSet.GenEmptyObj(out ho_ResultImage);
            try
            {
                // 获取源图像尺寸
                HOperatorSet.GetImageSize(srcImage, out HTuple hv_Width, out HTuple hv_Height);

                // 将 XLD 轮廓转换为区域（"filled" 表示填充内部）
                ho_Region.Dispose();
                HOperatorSet.GenRegionContourXld(contour, out ho_Region, "filled");

                // 创建白色背景图像（灰度值 255）
                ho_WhiteImage.Dispose();
                HOperatorSet.GenImageProto(srcImage, out ho_WhiteImage, 255);

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
            hWindowControl1.Focus();
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
        public void SetDrawMode(string Nmae, HObjContext HContext, WinDrawType type)
        {
            ReDisplay();
            hWindowControl1.Focus();
            DrawType = type;
            _drawContext.Name = Nmae;
            _drawContext.HContext = HContext;
            _drawContext.SetUp = SetUpEnum.None;           //设置步骤
            _drawContext.CycleMove = CycleMoveEnum.None;   //循环移动状态
            _currentHandler.SetUp(_drawContext);
        }

        HTuple UnHomMat2D = null;

        private void UnSetTemplateCenter(HTuple hv_ModelID)
        {
            if (UnHomMat2D == null) return;

            // 获取当前模版的 origin
            HOperatorSet.GetGenericShapeModelParam(hv_ModelID, "origin_row", out HTuple modeRow);
            HOperatorSet.GetGenericShapeModelParam(hv_ModelID, "origin_column", out HTuple modeColumn);

            // 应用反向变换还原原来的中心
            HOperatorSet.AffineTransPixel(UnHomMat2D, modeRow, modeColumn, out HTuple rowTrans, out HTuple colTrans);
            HOperatorSet.SetGenericShapeModelParam(hv_ModelID, "origin_row", rowTrans);
            HOperatorSet.SetGenericShapeModelParam(hv_ModelID, "origin_column", colTrans);
        }
        private void SetTemplateCenter(Point2d p,HTuple hv_ModelID)
        {
            //修改模版中心
            HTuple hv_HomMat2D = new HTuple();
            HOperatorSet.VectorAngleToRigid(center.Y, center.X, 0, p.Y, p.X, 0, out hv_HomMat2D);
            HOperatorSet.GetGenericShapeModelParam(hv_ModelID, "origin_row", out HTuple modeRow);
            HOperatorSet.GetGenericShapeModelParam(hv_ModelID, "origin_column", out HTuple modeColumn);

            HOperatorSet.AffineTransPixel(hv_HomMat2D, modeRow, modeColumn, out HTuple rowTrans, out HTuple colTrans);
            HOperatorSet.SetGenericShapeModelParam(hv_ModelID, "origin_row", rowTrans);
            HOperatorSet.SetGenericShapeModelParam(hv_ModelID, "origin_column", colTrans);
        }

        #endregion

    }
}
