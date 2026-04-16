using DotNet.Drawing;
using DotNet.VisionMaster;
using HalconDotNet;
using System;


namespace DotNet.VisionMaster
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

    public class DrawPolygonArgs : EventArgs
    {
        public DrawPolygonArgs(HObject contour)
        {
            ho_Contour = contour;
        }

        public HObject ho_Contour { get; private set; }
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

    public class DrawSetModelArgs : EventArgs
    {
        public DrawSetModelArgs(string name, Point2d topLeft, Point2d bottomRight, DisplayUI display)
        {
            Name = name;
            TopLeft = topLeft;
            BottomRight = bottomRight;
            Display = display;
        }

        /// <summary> 名称 </summary>
        public string Name { get; private set; }

        /// <summary> 矩形左上角点 </summary>
        public Point2d TopLeft { get; private set; }

        /// <summary> 矩形右下角点 </summary>
        public Point2d BottomRight { get; private set; }

        /// <summary> 显示控件 </summary>
        public DisplayUI Display { get; private set; }
    }

    public class DrawDispModelArgs : EventArgs
    {
        public DrawDispModelArgs(string name, DisplayUI display)
        {
            Name = name;
            Display = display;
        }

        /// <summary> 名称 </summary>
        public string Name { get; private set; }

        /// <summary> 显示控件 </summary>
        public DisplayUI Display { get; private set; }
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

    #endregion

    #region Delegates

    public delegate void DrawPointHandler(object sender, DrawPointArgs e);

    public delegate void DrawLineHandler(object sender, DrawLineArgs e);

    public delegate void DrawCircleHandler(object sender, DrawCircleArgs e);

    public delegate void DrawPolygonHandler(object sender, DrawPolygonArgs e);

    public delegate void DrawRectangleHandler(object sender, DrawRectangleArgs e);

    public delegate void DrawSetModelHandler(object sender, DrawSetModelArgs e);

    public delegate void DrawDispModelHandler(object sender, DrawDispModelArgs e);

    public delegate void DrawSynthethicHandler(object sender, DrawSynthethicArgs e);

    #endregion
}
