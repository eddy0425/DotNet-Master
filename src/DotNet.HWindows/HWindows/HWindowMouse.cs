using HalconDotNet;
using System;
using System.Windows.Forms;

namespace DotNet.HWindows
{
    public class HWindowMouse : IDisposable
    {
        long _clickTicks = 0;                      //程序栏鼠标点击计时 
        bool _mouseHand = false;                   //是否移动
        double _rowDown;                           //鼠标按下时的行坐标
        double _colDown;                           //鼠标按下时的列坐标
        readonly HWindow _hWindow;
        readonly HWindowControl _hWindowControl;
        readonly HWindowImage _hWindowImage;

        public volatile bool MouseDown;      //鼠标按下
        public volatile bool MouseDouble;    //鼠标双击按下

        public event HMouseEventHandler HMouseUp { add => _hWindowControl.HMouseUp += value; remove => _hWindowControl.HMouseUp -= value; }
        public event HMouseEventHandler HMouseMove { add => _hWindowControl.HMouseMove += value; remove => _hWindowControl.HMouseMove -= value; }
        public event HMouseEventHandler HMouseDown { add => _hWindowControl.HMouseDown += value; remove => _hWindowControl.HMouseDown -= value; }
        public event HMouseEventHandler HMouseWheel { add => _hWindowControl.HMouseWheel += value; remove => _hWindowControl.HMouseWheel -= value; }

        public HWindowMouse(HWindow hWindow, HWindowControl hWindowControl, HWindowImage hWindowImage)
        {
            _hWindow = hWindow;
            _hWindowControl = hWindowControl;
            _hWindowImage = hWindowImage;

            _hWindowControl.HMouseUp += HWindowControl_HMouseUp;
            //_hWindowControl.HMouseMove += HWindowControl_HMouseMove;
            _hWindowControl.HMouseDown += HWindowControl_HMouseDown;
            _hWindowControl.HMouseWheel += HWindowControl_HMouseWheel;
        }

        private void HWindowControl_HMouseDown(object sender, HMouseEventArgs e)  //鼠标指针在组件上方并释放鼠标按钮时发生
        {
            try
            {
                HTuple Row = e.Y, Column = e.X;
                _rowDown = Row;    //鼠标按下时的行坐标
                _colDown = Column; //鼠标按下时的列坐标

                //判断是否为双击
                bool doubleClick = (DateTime.Now.Ticks - _clickTicks) < 2000000;   //200ms                    
                if (doubleClick)
                {
                    _hWindowImage.Fun_DispImage(_hWindowImage.HoImage, true);
                    _mouseHand = false;
                    MouseDouble = true;
                }
                _clickTicks = DateTime.Now.Ticks;

                if (e.Button == MouseButtons.Middle)
                {
                    _mouseHand = true;
                }
                else if (e.Button == MouseButtons.Right)
                {
                    MouseDown = true;
                }

                //info.MouseType = e.Button;
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
        private void HWindowControl_HMouseUp(object sender, HMouseEventArgs e)  //鼠标移动事件调用函数
        {
            try
            {
                HTuple Row = e.Y, Column = e.X;

                if (_mouseHand)
                {
                    HTuple row1, col1, row2, col2;
                    double RowMove = Row - _rowDown;   //鼠标弹起时的行坐标减去按下时的行坐标，得到行坐标的移动值
                    double ColMove = Column - _colDown;//鼠标弹起时的列坐标减去按下时的列坐标，得到列坐标的移动值
                    HOperatorSet.GetPart(_hWindow, out row1, out col1, out row2, out col2);//得到当前的窗口坐标
                    HOperatorSet.SetPart(_hWindow, row1 - RowMove, col1 - ColMove, row2 - RowMove, col2 - ColMove);//这里可能有些不好理解。以左上角原点为参考点
                    HOperatorSet.ClearWindow(_hWindow);
                    if (_hWindowImage.HoImage != null)
                    {
                        HOperatorSet.DispObj(_hWindowImage.HoImage, _hWindow);
                    }
                    else
                    {
                        MessageBox.Show("请加载一张图片");
                    }
                }

                HTuple egray = new HTuple();
                HOperatorSet.GetGrayval(_hWindowImage.HoImage, Row, Column, out egray);
                //lbl_result.Text = string.Format("坐标[X:{0} Y:{1}]  灰度:{2} ", Column.D.ToString(), Row.D.ToString(), egray.D.ToString());
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
        private void HWindowControl_HMouseWheel(object sender, HMouseEventArgs e)  //鼠标指针在组件上方并按下鼠标按钮时发生
        {
            try
            {
                HTuple Zoom;
                HTuple Row = e.Y, Column = e.X;
                HTuple Row0, Column0, Row00, Column00, Ht, Wt, r1, c1, r2, c2;
                if (e.Delta > 0)
                {
                    Zoom = 1.5;
                }
                else
                {
                    Zoom = 0.5;
                }
                HOperatorSet.GetPart(_hWindow, out Row0, out Column0, out Row00, out Column00);
                Ht = Row00 - Row0;
                Wt = Column00 - Column0;
                if (Ht * Wt < 32000 * 32000 || Zoom == 1.5)//普通版halcon能处理的图像最大尺寸是32K*32K。如果无限缩小原图像，导致显示的图像超出限制，则会造成程序崩溃
                {
                    r1 = (Row0 + ((1 - (1.0 / Zoom)) * (Row - Row0)));
                    c1 = (Column0 + ((1 - (1.0 / Zoom)) * (Column - Column0)));
                    r2 = r1 + (Ht / Zoom);
                    c2 = c1 + (Wt / Zoom);
                    HOperatorSet.SetPart(_hWindow, r1, c1, r2, c2);
                    HOperatorSet.ClearWindow(_hWindow);
                    HOperatorSet.DispObj(_hWindowImage.HoImage, _hWindow);
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        //private void HWindowControl_HMouseMove(object sender, HMouseEventArgs e)  //鼠标滑轮滚动事件调用函数
        //{
        //    //SetCallBack_HMouseMove(sender, e);
        //}

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~HWindowMouse()
        {
            Dispose(false);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _hWindowControl.HMouseUp -= HWindowControl_HMouseUp;
                //_hWindowControl.HMouseMove += HWindowControl_HMouseMove;
                _hWindowControl.HMouseDown -= HWindowControl_HMouseDown;
                _hWindowControl.HMouseWheel -= HWindowControl_HMouseWheel;
            }
        }
    }
}
