using DotNet.Drawing;
using HalconDotNet;
using System.Windows.Forms;
using System.Collections.Generic;


namespace DotNet.HalconAlgo
{
    /// <summary>
    /// 多边形绘图处理器1
    /// 用于绘制新的多边形轮廓
    /// </summary>
    public class PolygonNewDrawHandler : IDrawHandler
    {
        Point2d StartPoint  = new Point2d(0, 0); //起始点
        public bool NeedReDisp => true;

        public void SetUp(DisplayUI display)
        {
            if (display.SetUp == SetUpEnum.None)
            {
                display.Reset();
                display.ReDispImage();
                display.SetUp = SetUpEnum.Step1;
            }
        }

        public void OnMouseDown(DisplayUI display, HMouseEventArgs e)
        {

            if (e.Button == MouseButtons.Left)
            {
                if (display.SetUp == SetUpEnum.Step1)
                {
                    // 开始绘制多边形，初始化点集合
                    display.ShrPolygons = new List<Point2d>();
                    StartPoint = new Point2d(e.X, e.Y);
                    display.ShrPolygons.Add(new Point2d(e.X, e.Y));
                }
                else if (display.SetUp == SetUpEnum.Step2)
                {
                    // 继续添加多边形顶点
                    display.ShrPolygons.Add(new Point2d(e.X, e.Y));
                }
            }
        }

        public void OnMouseUp(DisplayUI display, HMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (display.SetUp == SetUpEnum.Step1)
                {
                    // 第一个点确定后，进入连续绘制模式
                    display.SetUp = SetUpEnum.Step2;
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (display.SetUp == SetUpEnum.Step2)
                {
                    // 右键完成多边形绘制
                    display.ShrContour = HalconHelper.GenContours(display.ShrPolygons);
                    display.DrawPolygon(display.ShrContour);
                    display.SetUp = SetUpEnum.Step3;
                }
            }
        }

        public void OnMouseWheel(DisplayUI display, HMouseEventArgs e)
        {
            // 滚轮事件仅触发重绘
        }

        public void OnMouseMove(DisplayUI display, HMouseEventArgs e)
        {
            OnReDisplay(display);

            if (display.SetUp == SetUpEnum.Step1)
            {
                // 显示光标十字
                display.DispPoint(e.X, e.Y, HColor.Red);
            }
            else if (display.SetUp == SetUpEnum.Step2)
            {
                // 获取最后一个点
                Point2d regPoint = new Point2d(0, 0);
                if (display.ShrPolygons.Count > 0)
                {
                    regPoint = display.ShrPolygons[display.ShrPolygons.Count - 1];
                }
                else
                {
                    regPoint = StartPoint;
                }

                // 显示光标和到最后一个点的连线
                display.DispPoint(e.X, e.Y, HColor.Red);
                display.DispLine(new CvLine(regPoint.X, regPoint.Y, e.X, e.Y), HColor.Red);
            }
        }

        public void OnReDisplay(DisplayUI display)
        {
            var points = display.ShrPolygons;

            switch (display.SetUp)
            {
                case SetUpEnum.Step1:
                    {
                        // 绘制已有的线段（蓝色）
                        var color = HColor.Blue;
                        for (int i = 0; i < points.Count; i++)
                        {
                            if (i < points.Count - 1)
                            {
                                display.DispLine(new CvLine(points[i].X, points[i].Y, points[i + 1].X, points[i + 1].Y), color);
                            }
                        }
                    }
                    break;
                case SetUpEnum.Step2:
                    {
                        // 绘制进行中的多边形（橙红色）
                        var color = HColor.OrangeRed;
                        for (int i = 0; i < points.Count; i++)
                        {
                            if (i < points.Count - 1)
                            {
                                display.DispLine(new CvLine(points[i].X, points[i].Y, points[i + 1].X, points[i + 1].Y), color);
                            }
                        }
                    }
                    break;
                case SetUpEnum.Step3:
                    {
                        // 显示模型相关区域
                        display.DispRegion(display.ShrRegion, HColor.Blue);
                        display.DispRegion(display.ShrContour, HColor.Green);

                        if (display.ShrCenter != null)
                        {
                            display.DispPoint(display.ShrCenter, HColor.Yellow);
                        }
                    }
                    break;
            }
        }

    }
}
