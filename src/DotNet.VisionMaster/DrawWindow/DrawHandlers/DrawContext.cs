using DotNet.HWindows;
using HalconDotNet;
using OpenCvSharp;
using System;
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
        public class DrawPointArgs : EventArgs
        {
            public DrawPointArgs(double x, double y)
            {
                X = x;
                Y = y;
            }
            public double X { get; private set; }
            public double Y { get; private set; }
        }
        public event DrawPointHandler PointEvent;
        public delegate void DrawPointHandler(object sender, DrawPointArgs e);
        private void DrawPoint(double x, double y)
        {
            if (PointEvent != null)
            {
                var e = new DrawPointArgs(x, y);
                PointEvent(this, e);
            }
        }

        public class DrawLineArgs : EventArgs
        {
            public DrawLineArgs(double x1, double y1, double x2, double y2)
            {
                X1 = x1;
                Y1 = y1;
                X2 = x2;
                Y2 = y2;
            }
            public double X1 { get; private set; }
            public double Y1 { get; private set; }
            public double X2 { get; private set; }
            public double Y2 { get; private set; }
        }
        public event DrawLineHandler LineEvent;
        public delegate void DrawLineHandler(object sender, DrawLineArgs e);
        private void DrawLine(double x1, double y1, double x2, double y2)
        {
            if (LineEvent != null)
            {
                var e = new DrawLineArgs(x1, y1, x2, y2);
                LineEvent(this, e);
            }
        }

        public class DrawCircleArgs : EventArgs
        {
            public DrawCircleArgs(double x1, double y1, double x2, double y2)
            {
                X1 = x1;
                Y1 = y1;
                X2 = x2;
                Y2 = y2;
            }
            public double X1 { get; private set; }
            public double Y1 { get; private set; }
            public double X2 { get; private set; }
            public double Y2 { get; private set; }
        }
        public event DrawCircleHandler CircleEvent;
        public delegate void DrawCircleHandler(object sender, DrawCircleArgs e);
        private void DrawCircle(double x1, double y1, double x2, double y2)
        {
            if (CircleEvent != null)
            {
                var e = new DrawCircleArgs(x1, y1, x2, y2);
                CircleEvent(this, e);
            }
        }

        public class DrawPolygonArgs : EventArgs
        {
            public DrawPolygonArgs(HObject contour)
            {
                ho_Contour = contour;
            }

            public HObject ho_Contour { get; private set; }
        }
        public event DrawPolygonHandler PolygonEvent;
        public delegate void DrawPolygonHandler(object sender, DrawPolygonArgs e);
        public void DrawPolygon(HObject contour)
        {
            if (PolygonEvent != null)
            {
                var e = new DrawPolygonArgs(contour);
                PolygonEvent(this, e);
            }
        }


        public class DrawRectangleArgs : EventArgs
        {
            public DrawRectangleArgs(string name, Point2d topLeft, Point2d bottomRight)
            {
                Name = name;
                TopLeft = topLeft;
                BottomRight = bottomRight;
            }

            /// <summary> 名称 </summary>
            public string Name { get; private set; }

            /// <summary> 矩形左上角点 </summary>
            public Point2d TopLeft { get; private set; }

            /// <summary> 矩形右下角点 </summary>
            public Point2d BottomRight { get; private set; }

        }
        public event DrawRectangleHandler RectangleEvent;
        public delegate void DrawRectangleHandler(object sender, DrawRectangleArgs e);
        public void DrawRectangle(string name, Point2d topLeft, Point2d bottomRight)
        {
            if (RectangleEvent != null)
            {
                var e = new DrawRectangleArgs(name, topLeft, bottomRight);
                RectangleEvent(this, e);
            }
        }

        public class DrawSynthethicArgs : EventArgs
        {
            public DrawSynthethicArgs(HObject contour, Point2d topLeft, Point2d bottomRight)
            {
                ho_Contour = contour;
                TopLeft = topLeft;
                BottomRight = bottomRight;
            }

            public HObject ho_Contour { get; private set; }

            /// <summary>
            /// 矩形左上角点
            /// </summary>
            public Point2d TopLeft { get; private set; }

            /// <summary>
            /// 矩形右下角点
            /// </summary>
            public Point2d BottomRight { get; private set; }

            /// <summary>
            /// 中心
            /// </summary>
            public Point2d Center => new Point2d((TopLeft.X + BottomRight.X) / 2, (TopLeft.Y + BottomRight.Y) / 2);
        }
        public event DrawSynthethicHandler SynthethicEvent;
        public delegate void DrawSynthethicHandler(object sender, DrawSynthethicArgs e);
        public void DrawSynthethicn(HObject contour, Point2d topLeft, Point2d bottomRight)
        {
            if (SynthethicEvent != null)
            {
                var e = new DrawSynthethicArgs(contour, topLeft, bottomRight);
                SynthethicEvent(this, e);
            }
        }

        #endregion

        public DrawContext(HWindow _hWindow)
        {
            hWindow = _hWindow;
        }

        #region 共享状态
        public HWindow hWindow { get; set; }

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
        public HObjContext HContext { get; set; }

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

        #region Window

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

        public void DispLine(CvLine line, string color)
        {
            SetColor(color);
            hWindow.DispLine(line.start.Y, line.start.X, line.end.Y, line.end.X);
        }

        #endregion


        #region 辅助方法

        /// <summary>
        /// 计算两点的中心点
        /// </summary>
        public Point2d Cal2P(double X1, double Y1, double X2, double Y2)
        {
            return new Point2d((X1 + X2) / 2, (Y1 + Y2) / 2);
        }

        /// <summary>
        /// 计算两点之间的距离是否小于阈值
        /// </summary>
        public bool IsNearPoint(double x1, double y1, double x2, double y2, double threshold = 3)
        {
            return Math.Abs(x1 - x2) < threshold && Math.Abs(y1 - y2) < threshold;
        }

        /// <summary>
        /// 根据点坐标生成 XLD 轮廓
        /// </summary>
        /// <param name="points">坐标数组 (X=Column, Y=Row)</param>
        /// <returns>生成的 XLD 轮廓对象</returns>
        public HObject GenContours(List<Point2d> points)
        {
            HObject contour;
            HOperatorSet.GenEmptyObj(out contour);

            if (points == null || points.Count < 2) return contour;

            try
            {
                // 提取所有点的坐标 (Point2d: X=Column, Y=Row)
                double[] rows = new double[points.Count];
                double[] columns = new double[points.Count];

                for (int i = 0; i < points.Count; i++)
                {
                    rows[i] = points[i].Y;      // Row
                    columns[i] = points[i].X;   // Column
                }

                // 将 double[] 转换为 HTuple
                HTuple hv_Rows = new HTuple(rows);
                HTuple hv_Columns = new HTuple(columns);

                // 使用 gen_contour_polygon_xld 生成闭合的多边形轮廓
                contour.Dispose();
                HOperatorSet.GenContourPolygonXld(out contour, hv_Rows, hv_Columns);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GenContours Error: {ex.Message}");
            }

            return contour;
        }

        public List<Point2d> GetPolygons(double[] rowPoints, double[] columnPoints)
        {
            if (rowPoints.Length != columnPoints.Length) return null;

            var polygons = new List<Point2d>();

            for (int i = 0; i < rowPoints.Length; i++)
            {
                polygons.Add(new Point2d(columnPoints[i], rowPoints[i]));
            }

            return polygons;
        }


        #endregion
    }
}
