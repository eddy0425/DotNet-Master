using System;
using System.Windows.Forms;
using DotNet.Drawing;
using HalconDotNet;

namespace DotNet.HalconUI
{
    public class HWindowMouse
    {
        long clickTicks = 0;                      //程序栏鼠标点击计时 
        bool Mouse_hand = false;                  //是否移动
        double RowDown;                           //鼠标按下时的行坐标
        double ColDown;                           //鼠标按下时的列坐标
        HWindow hWindow;
        HWindowControl hWindowControl;
        HWindowImage hWindowImage;

        public event Action<HTuple, HTuple, HTuple> RefreshUI;
        public bool MouseDown { get; set; }      //鼠标按下
        public bool MouseDouble { get; set; }    //鼠标双击按下

        public HWindowMouse(HWindow _hWindow, HWindowControl _hWindowControl, HWindowImage _hWindowImage)
        {
            hWindow = _hWindow;
            hWindowControl = _hWindowControl;
            hWindowImage = _hWindowImage;

            hWindowControl.HMouseUp += HWindowControl_HMouseUp;
            //hWindowControl.HMouseMove += HWindowControl_HMouseMove;
            hWindowControl.HMouseDown += HWindowControl_HMouseDown;
            hWindowControl.HMouseWheel += HWindowControl_HMouseWheel;
        }

        private void HWindowControl_HMouseDown(object sender, HMouseEventArgs e)  //鼠标指针在组件上方并释放鼠标按钮时发生
        {
            try
            {
                HTuple Row = e.Y, Column = e.X;
                RowDown = Row;    //鼠标按下时的行坐标
                ColDown = Column; //鼠标按下时的列坐标

                //判断是否为双击
                bool doubleClick = (DateTime.Now.Ticks - clickTicks) < 2000000;   //200ms                    
                if (doubleClick)
                {
                    hWindowImage.Fun_DispImage(hWindowImage.HoImage, true);
                    Mouse_hand = false;
                    MouseDouble = true;
                }
                clickTicks = DateTime.Now.Ticks;

                if (e.Button == MouseButtons.Middle)
                {
                    Mouse_hand = true;
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

                if (Mouse_hand)
                {
                    HTuple row1, col1, row2, col2;
                    double RowMove = Row - RowDown;   //鼠标弹起时的行坐标减去按下时的行坐标，得到行坐标的移动值
                    double ColMove = Column - ColDown;//鼠标弹起时的列坐标减去按下时的列坐标，得到列坐标的移动值
                    HOperatorSet.GetPart(hWindow, out row1, out col1, out row2, out col2);//得到当前的窗口坐标
                    HOperatorSet.SetPart(hWindow, row1 - RowMove, col1 - ColMove, row2 - RowMove, col2 - ColMove);//这里可能有些不好理解。以左上角原点为参考点
                    HOperatorSet.ClearWindow(hWindow);
                    if (hWindowImage.HoImage != null)
                    {
                        HOperatorSet.DispObj(hWindowImage.HoImage, hWindow);
                    }
                    else
                    {
                        MessageBox.Show("请加载一张图片");
                    }
                }

                var handler = RefreshUI;
                if (handler != null && hWindowImage.HoImage.NotNull())
                {
                    HTuple egray;
                    HOperatorSet.GetGrayval(hWindowImage.HoImage, Row, Column, out egray);
                    handler?.Invoke(Row, Column, egray);
                }
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
                HOperatorSet.GetPart(hWindow, out Row0, out Column0, out Row00, out Column00);
                Ht = Row00 - Row0;
                Wt = Column00 - Column0;
                if (Ht * Wt < 32000 * 32000 || Zoom == 1.5)//普通版halcon能处理的图像最大尺寸是32K*32K。如果无限缩小原图像，导致显示的图像超出限制，则会造成程序崩溃
                {
                    r1 = (Row0 + ((1 - (1.0 / Zoom)) * (Row - Row0)));
                    c1 = (Column0 + ((1 - (1.0 / Zoom)) * (Column - Column0)));
                    r2 = r1 + (Ht / Zoom);
                    c2 = c1 + (Wt / Zoom);
                    HOperatorSet.SetPart(hWindow, r1, c1, r2, c2);
                    HOperatorSet.ClearWindow(hWindow);
                    HOperatorSet.DispObj(hWindowImage.HoImage, hWindow);
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        //private void HWindowControl_HMouseMove(object sender, HMouseEventArgs e)  //鼠标滑轮滚动事件调用函数
        //{
        //    //SetCallBack_HMouseMove(sender, e);
        //}

        
    }
}
