using DotNet.Drawing;
using DotNet.HalconUI;
using HalconDotNet;
using System.Collections.Generic;

namespace DotNet.VisionMaster
{
    /// <summary>
    /// 设置步骤枚举
    /// </summary>
    public enum SetUpEnum
    {
        None,
        Step1,
        Step2,
        Step3,
        Step4,
        Step5
    }

    /// <summary>
    /// 循环移动状态枚举
    /// </summary>
    public enum CycleMoveEnum
    {
        None,
        Start,
        StartMove,
        End,
        EndMove,
        Center,
        CenterMove
    }

    /// <summary>
    /// 绘图上下文类
    /// 封装绘图所需的共享状态和工具方法
    /// </summary>
    public class DrawContext
    {
        #region Event

        public event DrawPointHandler PointEvent;
        private void DrawPoint(double x, double y)
        {
            if (PointEvent != null)
            {
                var e = new DrawPointArgs(x, y);
                PointEvent(this, e);
            }
        }


        public event DrawLineHandler LineEvent;
        private void DrawLine(double x1, double y1, double x2, double y2)
        {
            if (LineEvent != null)
            {
                var e = new DrawLineArgs(x1, y1, x2, y2);
                LineEvent(this, e);
            }
        }


        public event DrawCircleHandler CircleEvent;
        private void DrawCircle(double x1, double y1, double x2, double y2)
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

        public DrawContext(DisplayForm _display)
        {
            display = _display;
        }

        #region 共享状态
        public DisplayForm display { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// 当前设置步骤
        /// </summary>
        public SetUpEnum SetUp { get; set; } = SetUpEnum.None;

        /// <summary>
        /// 当前循环移动状态
        /// </summary>
        public CycleMoveEnum CycleMove { get; set; } = CycleMoveEnum.None;

        /// <summary>
        /// 源图像
        /// </summary>
        public HObject SrcImage { get; set; }

        /// <summary>
        /// 矩形区域
        /// </summary>
        public CvRegion HoRegion { get; set; } //HoRegion

        /// <summary>
        /// 轮廓对象
        /// </summary>
        public HObject HoContour { get; set; }

        /// <summary>
        /// 模版中心
        /// </summary>
        public Point2d Center { get; set; }

        /// <summary>
        /// 多边形点集合
        /// </summary>
        public List<Point2d> Polygons { get; set; } = new List<Point2d>();

        #endregion

    }
}
