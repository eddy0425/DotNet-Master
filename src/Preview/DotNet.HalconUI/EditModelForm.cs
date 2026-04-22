using DotNet.Drawing;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using static DotNet.HalconUI.ModelHandlerFactory;


namespace DotNet.HalconUI
{
    public partial class EditModelForm : Form
    {
        bool EraseRegion = false;
        bool EnableEdit = false;


        #region 共享状态 Share → Shr ; Context → Ctx ; Algorithms → Algo 

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

        /// <summary> 擦除区域 </summary>
        public HObject ShrErase;

        /// <summary> 查找模版区域 </summary>
        public HObject ShrFindMode;

        /// <summary> 共享多边形点集合 </summary>
        public List<Point2d> ShrPolygons = new List<Point2d>();

        #endregion

        public string ShrColor => CB_ApplyColor.Text;
        public int ShrLineWidth => CB_ApplyLineWidth.Text.ExtractNumber();

        HObject srcImage;

        #region 属性

        // 绘图处理器相关
        private DrawEnum _drawType = DrawEnum.None;
        private IModelHandler _currentHandler;
        private ModelHandlerFactory _handlerFactory;

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

        #endregion

        public EditModelForm()
        {
            InitializeComponent();

            display.HMouseDown += DisPlay_HMouseDown;
            display.HMouseUp += DisPlay_HMouseUp;
            display.HMouseWheel += DisPlay_HMouseWheel;
            display.HMouseMove += DisPlay_HMouseMove;

            _handlerFactory = new ModelHandlerFactory();
            _currentHandler = _handlerFactory.GetHandler(DrawEnum.None);

            ShrContour = new HObject(); HOperatorSet.GenEmptyObj(out ShrContour);
            ShrErase = new HObject(); HOperatorSet.GenEmptyObj(out ShrErase);
            ShrFindMode = new HObject(); HOperatorSet.GenEmptyObj(out ShrFindMode);

            srcImage = new HObject(); HOperatorSet.GenEmptyObj(out srcImage);

            ShrRegion = new CvRegion();
        }

        private void FormDispose()
        {
            display.HMouseDown -= DisPlay_HMouseDown;
            display.HMouseUp -= DisPlay_HMouseUp;
            display.HMouseWheel -= DisPlay_HMouseWheel;
            display.HMouseMove -= DisPlay_HMouseMove;
        }

        #region HMouse
        private void DisPlay_HMouseDown(object sender, HMouseEventArgs e)
        {
            ReDisplay();
            _currentHandler.OnMouseDown(this, e);
        }

        private void DisPlay_HMouseUp(object sender, HMouseEventArgs e)
        {
            ReDisplay();
            _currentHandler.OnMouseUp(this, e);
        }

        private void DisPlay_HMouseWheel(object sender, HMouseEventArgs e)
        {
            ReDisplay();
            _currentHandler.OnMouseWheel(this, e);
        }

        private void DisPlay_HMouseMove(object sender, HMouseEventArgs e)
        {
            ReDisplay();
            _currentHandler.OnMouseMove(this, e);
        }

        private void ReDisplay()
        {
            if (_currentHandler != null && _currentHandler.NeedReDisp)
            {
                display.ReDispImage();
            }
        }

        #endregion

        /// <summary>
        /// 注册自定义绘图处理器
        /// 用于扩展新的绘图类型
        /// </summary>
        /// <param name="type">绘图类型</param>
        /// <param name="handler">处理器实例</param>
        public void RegisterDrawHandler(DrawEnum type, IModelHandler handler)
        {
            _handlerFactory.Register(type, handler);
        }

        /// <summary>
        /// 设置绘图模式
        /// </summary>
        /// <param name="type">绘图类型</param>
        public void SetDrawMode(DrawEnum type)
        {
            DrawType = type;
            SetUp = SetUpEnum.None;        //设置步骤
            CycleMove = CycleMoveEnum.None;   //循环移动状态

            ReDisplay();
            display.Reset();
            _currentHandler.SetUp(this);
        }

        /// <summary>
        /// 设置绘图模式
        /// </summary>
        /// <param name="type">绘图类型</param>
        public void SetDrawMode(string algoName, CvRegion hRegion, DrawEnum type)
        {
            DrawType = type;
            ShrRegion = hRegion;
            SetUp = SetUpEnum.None;           //设置步骤
            CycleMove = CycleMoveEnum.None;      //循环移动状态

            ReDisplay();
            display.Reset();
            _currentHandler.SetUp(this);
        }

        public DisplayForm GetDisplay() => display;

        private void EditModelForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }

        private void EditModelForm_Load(object sender, System.EventArgs e)
        {
            CB_ModifyShape.Items.Clear();
            CB_ModifyShape.Items.Add("矩形"); CB_ModifyShape.Items.Add("仿射矩形"); CB_ModifyShape.Items.Add("圆"); CB_ModifyShape.Items.Add("椭圆"); CB_ModifyShape.Items.Add("多边型");
            CB_ModifyShape.SelectedIndex = 0;

            for (int i = 1; i < 10; i++) CB_ApplyLineWidth.Items.Add($"线宽{i}");
            for (int i = 10; i < 100; i = i + 10) CB_ApplyLineWidth.Items.Add($"线宽{i}");

            CB_ApplyLineWidth.SelectedIndex = 0;
            CB_ApplyColor.SelectedIndex = 0;
        }

        private void but_addRegion_Click(object sender, System.EventArgs e)
        {
            SetDrawMode(DrawEnum.NewRect);
        }

        private void btn_deleteRegion_Click(object sender, System.EventArgs e)
        {

        }

        private void but_ApplyRegion_Click(object sender, System.EventArgs e)
        {
            SetDrawMode(DrawEnum.EraseRect);
        }

        public void ShowModifyTemplate(string modelPath)
        {
            try
            {
                //IsOk = false;
                //if (this.Visible)
                //{
                //    this.Close();
                //}

                //this.Show();
                //info = _info;
                //CB_LockCenter.Checked = _info.LockCenter;
                //m_type = _type;

                //if (srcImage.NotNull()) { srcImage.Dispose(); HOperatorSet.GenEmptyObj(out srcImage); }

                HOperatorSet.GenEmptyObj(out srcImage);
                HOperatorSet.ReadImage(out srcImage, modelPath);
                display.DispImage(srcImage);

                //findModeRegion = _findRegion;
                //setModeCenter = _setModeCenter;

                //CvOperatorSet.TransRegion(setModeCenter, disPlay.HoCentre, findModeRegion.HoRegion, out findModeRegion.InRegion);

                ////参数备份
                //oldModelID = info.modelID.Clone();

                //m_type.FindModel2(srcImage, info, 1, out ModelResult oldResult); if (oldResult.score.Length <= 0) throw new Exception($"查找模板失败！!");
                //LockCenter = new CvCoord(oldResult.X, oldResult.Y, oldResult.angle);

                //SetTemplate(srcImage, findModeRegion, info, m_type);
            }
            catch
            {
                this.Hide();
                throw;
            }
        }
    }
}
