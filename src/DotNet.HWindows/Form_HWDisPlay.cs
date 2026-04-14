using DotNet.Drawing;
using DotNet.Drawing.HWindows;
using DotNet.HWindows.WinDraw;
using HalconDotNet;
using System;
using System.Windows.Forms;


namespace DotNet.HWindows
{
    public partial class Form_HWDisPlay : Form
    {
        // 2.字段命名:将字段的命名改为私有并使用下划线作为前缀，以便于区分
        HWindowImage _hWindowImage;
        HWindowMouse _hWindowMouse;

        HObject srcImage;
        bool _isCross;          //是否画十字
        bool _adaptive = true;  //自适应

        HDisplayCore display;
        public event HMouseEventHandler HMouseUp { add => hWindowControl.HMouseUp += value; remove => hWindowControl.HMouseUp -= value; }
        public event HMouseEventHandler HMouseMove { add => hWindowControl.HMouseMove += value; remove => hWindowControl.HMouseMove -= value; }
        public event HMouseEventHandler HMouseDown { add => hWindowControl.HMouseDown += value; remove => hWindowControl.HMouseDown -= value; }
        public event HMouseEventHandler HMouseWheel { add => hWindowControl.HMouseWheel += value; remove => hWindowControl.HMouseWheel -= value; }

        #region 属性

        //1.属性封装:使用了表达式体属性来简化对属性的定义
        //3.方法命名:方法名称简化，并使用了更清晰的名称，比如 FunRedisplay 和`FunDispImage
        public double HoWidth => _hWindowImage.Width;
        public double HoHeight => _hWindowImage.Height;
        public Point2d HoCentre => new Point2d(_hWindowImage.Width / 2, _hWindowImage.Height / 2);
        public Size2d HoSize => new Size2d(_hWindowImage.Width, _hWindowImage.Height);
        public HObject HoImage => _hWindowImage.HoImage;  //图像
        public HWindow HoWindow => hWindowControl.HalconWindow;  //窗体句柄
        public HWindowMouse HoMouseEvent => _hWindowMouse;  //窗体句柄
        public bool HoMouseDown { get { return _hWindowMouse.MouseDown; } set { _hWindowMouse.MouseDown = value; } } //鼠标按下
        public bool HoMouseDouble { get { return _hWindowMouse.MouseDouble; } set { _hWindowMouse.MouseDouble = value; } }  //鼠标双击按下

        #endregion

        public WinDrawAction DrawAction;
        public WinDrawType drawType { get { return DrawAction.drawType; } set { DrawAction.drawType = value; } }

        public void HidtHead()
        {
            this.tableLayoutPanel_floor.RowStyles.RemoveAt(0);
            this.tableLayoutPanel_floor.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 0));
            this.tableLayoutPanel_floor.RowStyles.RemoveAt(0);
            this.tableLayoutPanel_floor.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 0));
        }

        public Form_HWDisPlay(bool noneFormBorder = true)
        {
            InitializeComponent();
            display = new HDisplayCore(hWindowControl);

            _hWindowImage = new HWindowImage(HoWindow, hWindowControl);
            _hWindowMouse = new HWindowMouse(HoWindow, hWindowControl, _hWindowImage);

            if (noneFormBorder)
            {
                this.FormBorderStyle = FormBorderStyle.None;     //无边框
                this.Dock = DockStyle.Fill;
                this.TopLevel = false;
            }

            this.InitHalcon();

            DrawAction = new WinDrawAction(this);
            //_hWindowMouse.HMouseDown += WindowMouse_HMouseDown;
            //_hWindowMouse.HMouseUp += WindowMouse_HMouseUp;
            //_hWindowMouse.HMouseWheel += WindowMouse_HMouseWheel;
            _hWindowMouse.HMouseMove += WindowMouse_HMouseMove;
           
        }
        private void FormDispose()
        {
            HoWindow.Dispose();
            hWindowControl.Dispose();
            _hWindowImage.Dispose();
            _hWindowMouse.Dispose();
        }

        private void WindowMouse_HMouseMove(object sender, HMouseEventArgs e)
        {
            Invoke(new Action(() =>
            {
                lbl_result.Text = $"X:{e.X:F2} Y:{e.Y:F2} 灰度:-";
            }));
        }


        #region 方法

        public void ReDispImage()
        {
            try
            {
                _hWindowImage.Fun_Redisplay();
            }
            catch
            {
                throw;
            }
        }
     
        public void DispImage(HObject image)
        {
            try
            {
                if (srcImage.NotNull()) { srcImage.Dispose(); HOperatorSet.GenEmptyObj(out srcImage); }
                srcImage = image.Clone();
                DispImage(srcImage, _adaptive);
            }
            catch
            { 
                throw;
            }
        }

        public void DispTempImage(HObject image)
        {
            try
            {
                DispImage(image, _adaptive);
            }
            catch
            {
                throw;
            }
        }

        public void DispImage(HObject image, bool isSetPart)
        {
            try
            {
                _hWindowImage.Fun_DispImage(image, isSetPart);

                if (_isCross)
                {
                    double size = HoWidth > HoHeight ? HoWidth : HoHeight;
                    HOperatorSet.DispCross(HoWindow, HoHeight / 2, HoWidth / 2, size, 0);
                }
            }
            catch
            {
                throw;
            }
        }

        #endregion

        private void btn_ReSetPart_Click(object sender, EventArgs e)
        {
            _adaptive = !_adaptive;
        }
        private void but_IsCross_Click(object sender, EventArgs e)
        {
            _isCross = !_isCross;
        }

    }

}
