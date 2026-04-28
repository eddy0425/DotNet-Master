using DotNet.Drawing;
using HalconDotNet;
using System;

namespace DotNet.HalconAlgo
{
    // 事件参数全部为不可变对象 (只读属性), 由 EventHandler<T> 标准委托承载.
    // 不再为每个 Args 单独定义委托类型, 直接使用 BCL 的 EventHandler<TEventArgs>.

    public class DrawPointArgs : EventArgs
    {
        public DrawPointArgs(double x, double y) { X = x; Y = y; }
        public double X { get; private set; }
        public double Y { get; private set; }
    }

    public class DrawLineArgs : EventArgs
    {
        public DrawLineArgs(double x1, double y1, double x2, double y2)
        {
            X1 = x1; Y1 = y1; X2 = x2; Y2 = y2;
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
            X1 = x1; Y1 = y1; X2 = x2; Y2 = y2;
        }
        public double X1 { get; private set; }
        public double Y1 { get; private set; }
        public double X2 { get; private set; }
        public double Y2 { get; private set; }
    }

    public class DrawPolygonArgs : EventArgs
    {
        public DrawPolygonArgs(HObject contour) { ho_Contour = contour; }
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

    public class DrawAffRectArgs : EventArgs
    {
        public DrawAffRectArgs(string name, Point2d center, Size2d rectSize, double phi)
        {
            Name = name;
            Center = center;
            RectSize = rectSize;
            Phi = phi;
        }

        /// <summary> 名称 </summary>
        public string Name { get; private set; }

        /// <summary> 矩形中心 </summary>
        public Point2d Center { get; private set; }

        /// <summary> 矩形大小 </summary>
        public Size2d RectSize { get; private set; }

        public double Phi { get; private set; }
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

        /// <summary> 矩形左上角点 </summary>
        public Point2d TopLeft { get; private set; }

        /// <summary> 矩形右下角点 </summary>
        public Point2d BottomRight { get; private set; }

    }
}
