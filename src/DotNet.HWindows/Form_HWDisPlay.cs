using System;
using System.Windows.Forms;
using DotNet.HWindows.WinDraw;
using HalconDotNet;
using OpenCvSharp;


namespace DotNet.HWindows
{
    public partial class Form_HWDisPlay : Form
    {
        // 2.字段命名:将字段的命名改为私有并使用下划线作为前缀，以便于区分
        HWindowImage _hWindowImage;
        HWindowMouse _hWindowMouse;
        HWindowFontBase _hWindowFont;

        HObject srcImage;
        bool _isCross;          //是否画十字
        bool _adaptive = true;  //自适应

        #region 属性

        //1.属性封装:使用了表达式体属性来简化对属性的定义
        //3.方法命名:方法名称简化，并使用了更清晰的名称，比如 FunRedisplay 和`FunDispImage
        public double HoWidth => _hWindowImage.Width;
        public double HoHeight => _hWindowImage.Height;
        public Point2d HoCentre => new Point2d(_hWindowImage.Width / 2, _hWindowImage.Height / 2);
        public Size2d HoSize => new Size2d(_hWindowImage.Width, _hWindowImage.Height);
        public HObject HoImage => _hWindowImage.HoImage;  //图像
        public HWindow HoWindow => hWindowControl1.HalconWindow;  //窗体句柄
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

            _hWindowFont = HWindowFontFactory.CreateFont(HoWindow, "2018");
            _hWindowImage = new HWindowImage(HoWindow, hWindowControl1);
            _hWindowMouse = new HWindowMouse(HoWindow, hWindowControl1, _hWindowImage);

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
            hWindowControl1.Dispose();
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

        #region

       

        #region 鼠标绘画事件

        //public WinDrawType drawType = WinDrawType.None;

        //DrawCircle drawCircle = DrawCircle.None;
        //DrawPolygon drawPolygon = DrawPolygon.None;

        //public WinDrawModel drawModel = new WinDrawModel();

       

        //Point2d StartPoint = new Point2d(0, 0);
        //Point2d EndPoint = new Point2d(0, 0);
        //Point2d OffsetPoint = new Point2d(0, 0);

        //DCircle dispCircle = new DCircle();

        //List<Point2d> Polygons = new List<Point2d>();
        //int PolygonsIndex = 0;

        #endregion


        //private void WindowMouse_HMouseDown(object sender, HMouseEventArgs e)
        //{
        //    ReDisplay();

        //    if (e.Button == MouseButtons.Left) // 检查用户是否按下了鼠标右键
        //    {
        //        switch (drawType)
        //        {
        //            case WinDrawType.Point:
        //                DrawAction.DrawPoint(e.X, e.Y);
        //                break;

        //            case WinDrawType.Line:
        //                StartPoint.X = e.X;
        //                StartPoint.Y = e.Y;
        //                break;

        //            case WinDrawType.Line_2:
        //                DrawAction.DrawLine(StartPoint.X, StartPoint.Y, e.X, e.Y);
        //                break;

        //            case WinDrawType.Circle:
        //                StartPoint.X = e.X;
        //                StartPoint.Y = e.Y;
        //                drawType = WinDrawType.Circle_2;
        //                break;

        //            case WinDrawType.Circle_3:
        //                {
        //                    if (drawCircle == DrawCircle.Start)
        //                    {
        //                        drawCircle = DrawCircle.StartMove;
        //                    }
        //                    else if (drawCircle == DrawCircle.End)
        //                    {
        //                        drawCircle = DrawCircle.EndMove;
        //                    }
        //                }
        //                break;

        //            case WinDrawType.Polygon1:
        //                Polygons = new List<Point2d>();

        //                StartPoint.X = e.X;
        //                StartPoint.Y = e.Y;
        //                Polygons.Add(new Point2d(e.X, e.Y));
        //                break;

        //            case WinDrawType.Polygon1_2:
        //                Polygons.Add(new Point2d(e.X, e.Y));
        //                break;

        //            case WinDrawType.Polygon2:
        //                if (drawPolygon == DrawPolygon.Start)
        //                {
        //                    drawPolygon = DrawPolygon.StartMove;
        //                }
        //                break;
        //        }
        //    }
        //    else if (e.Button == MouseButtons.Right)
        //    {
        //        switch (drawType)
        //        {
        //            case WinDrawType.Circle_3:
        //                DrawAction.DrawCircle(StartPoint.X, StartPoint.Y, EndPoint.X, EndPoint.Y);
        //                break;

        //            case WinDrawType.Polygon1_2:
        //                DrawAction.DrawPolygon(Polygons);
        //                break;

        //            case WinDrawType.Polygon2:
        //                DrawAction.DrawPolygon(Polygons);
        //                break;
        //        }
        //    }
        //}

        //private void WindowMouse_HMouseUp(object sender, HMouseEventArgs e)
        //{
        //    ReDisplay();

        //    if (e.Button == MouseButtons.Left) // 检查用户是否按下了鼠标右键
        //    {
        //        switch (drawType)
        //        {
        //            case WinDrawType.Point:
        //                drawType = WinDrawType.None;
        //                break;

        //            case WinDrawType.Line:
        //                drawType = WinDrawType.Line_2;
        //                break;

        //            case WinDrawType.Line_2:
        //                drawType = WinDrawType.None;
        //                break;

        //            case WinDrawType.Circle_2:
        //                drawType = WinDrawType.Circle_3;
        //                break;

        //            case WinDrawType.Circle_3:
        //                drawCircle = DrawCircle.None;
        //                break;

        //            case WinDrawType.Polygon1:
        //                drawType = WinDrawType.Polygon1_2;
        //                break;

        //            case WinDrawType.Polygon2:
        //                if (drawPolygon == DrawPolygon.StartMove)
        //                {
        //                    drawPolygon = DrawPolygon.None;
        //                }
        //                break;
        //        }

        //    }
        //    else if (e.Button == MouseButtons.Right)
        //    {
        //        switch (drawType)
        //        {
        //            case WinDrawType.Circle_3:
        //                drawType = WinDrawType.None;
        //                break;

        //            case WinDrawType.Polygon1_2:
        //                drawType = WinDrawType.None;
        //                break;

        //            case WinDrawType.Polygon2:
        //                drawType = WinDrawType.None;
        //                break;
        //        }
        //    }
        //}

        //private void WindowMouse_HMouseWheel(object sender, HMouseEventArgs e)
        //{
        //    ReDisplay();
        //}

        //private void WindowMouse_HMouseMove(object sender, HMouseEventArgs e)
        //{
        //    Invoke(new Action(() =>
        //    {
        //        lbl_result.Text = $"X:{e.X:F2} Y:{e.Y:F2} 灰度:-";
        //    }));

        //    ReDisplay();

        //    switch (drawType)
        //    {
        //        case WinDrawType.Point:
        //            this.DispCross(new DCoord(e.X, e.Y), drawModel.PointSize, HColor.OrangeRed);
        //            break;

        //        case WinDrawType.Line:
        //            this.DispCross(new DCoord(e.X, e.Y), drawModel.PointSize, HColor.OrangeRed);
        //            break;

        //        case WinDrawType.Line_2:
        //            this.DispArrow(new DLine(StartPoint.X, StartPoint.Y, e.X, e.Y), drawModel.ArrowSize, HColor.OrangeRed);
        //            break;

        //        case WinDrawType.Circle:
        //            this.DispCross(new DCoord(e.X, e.Y), drawModel.PointSize, HColor.Red);
        //            break;

        //        case WinDrawType.Circle_2:
        //            EndPoint.X = e.X;
        //            EndPoint.Y = e.Y;
        //            dispCircle = new DCircle(StartPoint, EndPoint);
        //            break;

        //        case WinDrawType.Circle_3:
        //            {
        //                if (drawCircle == DrawCircle.StartMove)
        //                {
        //                    OffsetPoint = new Point2d(EndPoint.X - StartPoint.X, EndPoint.Y - StartPoint.Y);
        //                    StartPoint.X = e.X;
        //                    StartPoint.Y = e.Y;

        //                    EndPoint = new Point2d(StartPoint.X + OffsetPoint.X, StartPoint.Y + OffsetPoint.Y);
        //                    dispCircle = new DCircle(StartPoint, EndPoint);
        //                }
        //                else if (drawCircle == DrawCircle.EndMove)
        //                {
        //                    EndPoint.X = e.X;
        //                    EndPoint.Y = e.Y;
        //                    dispCircle = new DCircle(StartPoint, EndPoint);
        //                }
        //                else if (Math.Abs(StartPoint.X - e.X) < 3 && Math.Abs(StartPoint.Y - e.Y) < 3)
        //                {
        //                    this.DispPoint(StartPoint, 10, HColor.Green);
        //                    drawCircle = DrawCircle.Start;
        //                }
        //                else if (Math.Abs(EndPoint.X - e.X) < 3 && Math.Abs(EndPoint.Y - e.Y) < 3)
        //                {
        //                    this.DispPoint(EndPoint, 10, HColor.Green);
        //                    drawCircle = DrawCircle.End;
        //                }
        //                else
        //                {
        //                    drawCircle = DrawCircle.None;
        //                }
        //            }
        //            break;

        //        case WinDrawType.Polygon1:
        //            this.DispPoint(new Point2d(e.X, e.Y), drawModel.PointSize, HColor.Red);
        //            break;

        //        case WinDrawType.Polygon1_2:
        //            {
        //                Point2d regPoint = new Point2d(0, 0);
        //                if (Polygons.Count > 0)
        //                {
        //                    regPoint = Polygons[Polygons.Count - 1];
        //                }
        //                else
        //                {
        //                    regPoint = StartPoint;
        //                }

        //                this.DispArrow(new DLine(regPoint.X, regPoint.Y, e.X, e.Y), 1, HColor.Red);
        //            }
        //            break;

        //        case WinDrawType.Polygon2:
        //            {
        //                if (drawPolygon == DrawPolygon.StartMove)
        //                {
        //                    Polygons[PolygonsIndex] = new Point2d(e.X, e.Y);
        //                }
        //                else
        //                {
        //                    drawPolygon = DrawPolygon.None;
        //                    for (int i = 0; i < Polygons.Count; i++)
        //                    {
        //                        if (Math.Abs(Polygons[i].X - e.X) < 3 && Math.Abs(Polygons[i].Y - e.Y) < 3)
        //                        {
        //                            this.DispPoint(Polygons[i], 10, HColor.Red);
        //                            PolygonsIndex = i;
        //                            drawPolygon = DrawPolygon.Start;
        //                        }
        //                    }
        //                }
        //            }
        //            break;

        //    }
        //}

        //private void ReDisplay()
        //{
        //    this.ReDispImage();

        //    switch (drawType)
        //    {
        //        case WinDrawType.Circle_2:
        //        case WinDrawType.Circle_3:
        //            {
        //                this.DispPoint(StartPoint, 10, HColor.OrangeRed);
        //                this.DispPoint(EndPoint, 10, HColor.OrangeRed);
        //                this.DispCircle(dispCircle, HColor.Red);
        //            }
        //            break;
        //        case WinDrawType.Polygon1:
        //        case WinDrawType.Polygon1_2:
        //            {
        //                this.DispPoint(StartPoint, drawModel.PointSize, HColor.Green);

        //                List<Point2d> points = Polygons;
        //                string color = drawType == WinDrawType.Polygon1_2 ? HColor.OrangeRed : HColor.Blue;

        //                for (int i = 0; i < Polygons.Count; i++)
        //                {
        //                    this.DispPoint(Polygons[i], 5, HColor.Green);

        //                    if (i < Polygons.Count - 1)
        //                    {
        //                        this.DispLine(new DLine(points[i].X, points[i].Y, points[i + 1].X, points[i + 1].Y), 2, color);
        //                    }
        //                }

        //                if (drawType != WinDrawType.Polygon1_2)
        //                {
        //                    if (Polygons.Count > 2)
        //                    {
        //                        this.DispLine(new DLine(points[0].X, points[0].Y, points[points.Count - 1].X, points[points.Count - 1].Y), 2, color);
        //                    }
        //                }
        //            }
        //            break;
        //        case WinDrawType.Polygon2:
        //            {
        //                for (int i = 0; i < Polygons.Count; i++)
        //                {
        //                    this.DispPoint(Polygons[i], 5, HColor.Blue);

        //                    if (i < Polygons.Count - 1)
        //                    {
        //                        this.DispLine(new DLine(Polygons[i].X, Polygons[i].Y, Polygons[i + 1].X, Polygons[i + 1].Y), 2, HColor.Red);
        //                    }
        //                }

        //                if (Polygons.Count > 2)
        //                {
        //                    this.DispLine(new DLine(Polygons[0].X, Polygons[0].Y, Polygons[Polygons.Count - 1].X, Polygons[Polygons.Count - 1].Y), 2, HColor.Red);
        //                }
        //            }
        //            break;
        //    }
        //}

        #endregion


        #region 方法

        public void SetFontSize(HTuple hv_Size)
        {
            _hWindowFont.SetFontSize(hv_Size);
        }

        public void DispText(DispText dispText)
        {
            _hWindowFont.DispText(dispText.hoText, dispText.FontY, dispText.FontX, dispText.FontSize, dispText.Color);
        }

        public void DispText(string message, HTuple FontX, HTuple FontY, string color)
        {
            _hWindowFont.DispText(message, FontY, FontX, color);
        }

        public void DispText(string message, HTuple FontX, HTuple FontY, HTuple size, string color)
        {
            _hWindowFont.DispText(message, FontY, FontX, size, color);
        }

        public void DispText(string[] mesLines, HTuple FontY, HTuple FontX, HTuple size, string color, double lineSpacing = 1.5)
        {
            _hWindowFont.DispText(mesLines, FontY, FontX, size, color, lineSpacing);
        }

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

        #region

        public void DispRegion(DispDRegion dispDRegion)
        {
            HoWindow.DispRegion(dispDRegion);
        }

        public void DispCross(DispPoint2d dispPoint)
        {
            HoWindow.DispCross(dispPoint);
        }
        public void DispCross(DispDCoord dispCoord)
        {
            HoWindow.DispCross(dispCoord);
        }

        public void DispLine(DispDLine line)
        {
            HoWindow.DispLine(line);
        }

        public void DispArrow(DispArrow arrow)
        {
            HoWindow.DispArrow(arrow);
        }
        public void DispCircle(DispDCircle dispDCircle)
        {
            HoWindow.DispCircle(dispDCircle);
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
