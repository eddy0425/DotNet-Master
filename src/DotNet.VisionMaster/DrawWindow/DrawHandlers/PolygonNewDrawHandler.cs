using DotNet.Drawing;
using HalconDotNet;
using System.Windows.Forms;
using System.Collections.Generic;
using DotNet.HalconUI;

namespace DotNet.VisionMaster
{
    /// <summary>
    /// 多边形绘图处理器1
    /// 用于绘制新的多边形轮廓
    /// </summary>
    public class PolygonNewDrawHandler : IDrawHandler
    {
        Point2d StartPoint  = new Point2d(0, 0); //起始点
        DrawContext context;
        DisplayForm display => context.display;
        public bool NeedReDispImage => true;

        public void SetUp(DrawContext _context)
        {
            context = _context;
            if (context.SetUp == SetUpEnum.None)
            {
                context.SetUp = SetUpEnum.Step1;
            }
        }

        public void OnMouseDown(DrawContext context, HMouseEventArgs e)
        {

            if (e.Button == MouseButtons.Left)
            {
                if (context.SetUp == SetUpEnum.Step1)
                {
                    // 开始绘制多边形，初始化点集合
                    context.Polygons = new List<Point2d>();
                    StartPoint = new Point2d(e.X, e.Y);
                    context.Polygons.Add(new Point2d(e.X, e.Y));
                }
                else if (context.SetUp == SetUpEnum.Step2)
                {
                    // 继续添加多边形顶点
                    context.Polygons.Add(new Point2d(e.X, e.Y));
                }
            }
        }

        public void OnMouseUp(DrawContext context, HMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (context.SetUp == SetUpEnum.Step1)
                {
                    // 第一个点确定后，进入连续绘制模式
                    context.SetUp = SetUpEnum.Step2;
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (context.SetUp == SetUpEnum.Step2)
                {
                    // 右键完成多边形绘制
                    context.HoContour = HalconHelper.GenContours(context.Polygons);
                    context.DrawPolygon(context.HoContour);
                    context.SetUp = SetUpEnum.Step3;
                }
            }
        }

        public void OnMouseWheel(DrawContext context, HMouseEventArgs e)
        {
            // 滚轮事件仅触发重绘
        }

        public void OnMouseMove(DrawContext context, HMouseEventArgs e)
        {
            OnReDisplay(context);

            if (context.SetUp == SetUpEnum.Step1)
            {
                // 显示光标十字
                display.DispPoint(e.X, e.Y, HColor.Red);
            }
            else if (context.SetUp == SetUpEnum.Step2)
            {
                // 获取最后一个点
                Point2d regPoint = new Point2d(0, 0);
                if (context.Polygons.Count > 0)
                {
                    regPoint = context.Polygons[context.Polygons.Count - 1];
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

        public void OnReDisplay(DrawContext context)
        {
            var points = context.Polygons;

            switch (context.SetUp)
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
                        display.DispRegion(context.HoRegion, HColor.Blue);
                        display.DispRegion(context.HoContour, HColor.Green);

                        if (context.Center != null)
                        {
                            display.DispPoint(context.Center, HColor.Yellow);
                        }
                    }
                    break;
            }
        }

    }
}
