using HalconDotNet;
using OpenCvSharp;
using System;
using System.Windows.Forms;
using System.Collections.Generic;

namespace DotNet.HWindows.WinDraw
{
    public class WinDrawAction
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
            public DrawPolygonArgs(List<Point2d> polygons)
            {
                Polygons = polygons;
            }

            public List<Point2d> Polygons { get; private set; }
        }

        public delegate void DrawPointHandler(object sender, DrawPointArgs e);
        
        private void DrawPoint(double x, double y)
        {
            if (PointEvent != null)
            {
                var e = new DrawPointArgs(x, y);
                PointEvent(this, e);
            }
        }

        public delegate void DrawLineHandler(object sender, DrawLineArgs e);
       
        private void DrawLine(double x1, double y1, double x2, double y2)
        {
            if (LineEvent != null)
            {
                var e = new DrawLineArgs(x1, y1, x2, y2);
                LineEvent(this, e);
            }
        }

        public delegate void DrawCircleHandler(object sender, DrawCircleArgs e);
       
        private void DrawCircle(double x1, double y1, double x2, double y2)
        {
            if (CircleEvent != null)
            {
                var e = new DrawCircleArgs(x1, y1, x2, y2);
                CircleEvent(this, e);
            }
        }

        public delegate void DrawPolygonHandler(object sender, DrawPolygonArgs e);
        
        private void DrawPolygon(List<Point2d> polygons)
        {
            if (PolygonEvent != null)
            {
                var e = new DrawPolygonArgs(polygons);
                PolygonEvent(this, e);
            }
        }

        #endregion

        #region 鼠标绘画事件

        Form_HWDisPlay disPlay;

        DrawCircleType drawCircle = DrawCircleType.None;
        DrawPolygonType drawPolygon = DrawPolygonType.None;

        WinDrawModel drawModel = new WinDrawModel();

        Point2d StartPoint = new Point2d(0, 0);
        Point2d EndPoint = new Point2d(0, 0);
        Point2d OffsetPoint = new Point2d(0, 0);

        CvCircle dispCircle = new CvCircle();

        List<Point2d> Polygons = new List<Point2d>();
        int SelectIndex = 0;

        #endregion

        public event DrawPointHandler PointEvent;
        public event DrawLineHandler LineEvent;
        public event DrawCircleHandler CircleEvent;
        public event DrawPolygonHandler PolygonEvent;
        public WinDrawType drawType = WinDrawType.None;

        public WinDrawAction(Form_HWDisPlay _disPlay) 
        {
            disPlay = _disPlay;

            disPlay.HoMouseEvent.HMouseDown += WindowMouse_HMouseDown;
            disPlay.HoMouseEvent.HMouseUp += WindowMouse_HMouseUp;
            disPlay.HoMouseEvent.HMouseWheel += WindowMouse_HMouseWheel;
            disPlay.HoMouseEvent.HMouseMove += WindowMouse_HMouseMove;
        }


        private void WindowMouse_HMouseDown(object sender, HMouseEventArgs e)
        {
            ReDisplay();

            if (e.Button == MouseButtons.Left) // 检查用户是否按下了鼠标右键
            {
                switch (drawType)
                {
                    case WinDrawType.Line:
                        StartPoint.X = e.X;
                        StartPoint.Y = e.Y;
                        break;

                    case WinDrawType.Circle:
                        StartPoint.X = e.X;
                        StartPoint.Y = e.Y;
                        drawType = WinDrawType.Circle_2;
                        break;

                    case WinDrawType.Circle_3:
                        {
                            if (drawCircle == DrawCircleType.Start)
                            {
                                drawCircle = DrawCircleType.StartMove;
                            }
                            else if (drawCircle == DrawCircleType.End)
                            {
                                drawCircle = DrawCircleType.EndMove;
                            }
                        }
                        break;

                    case WinDrawType.Polygon1:
                        Polygons = new List<Point2d>();

                        StartPoint.X = e.X;
                        StartPoint.Y = e.Y;
                        Polygons.Add(new Point2d(e.X, e.Y));
                        break;

                    case WinDrawType.Polygon1_2:
                        Polygons.Add(new Point2d(e.X, e.Y));
                        break;

                    case WinDrawType.Polygon2:
                        if (drawPolygon == DrawPolygonType.Start)
                        {
                            drawPolygon = DrawPolygonType.StartMove;
                        }
                        break;
                }
            }
        }

        private void WindowMouse_HMouseUp(object sender, HMouseEventArgs e)
        {
            ReDisplay();

            if (e.Button == MouseButtons.Left) // 检查用户是否按下了鼠标右键
            {
                switch (drawType)
                {
                    case WinDrawType.Point:
                        DrawPoint(e.X, e.Y);
                        drawType = WinDrawType.None;
                        break;

                    case WinDrawType.Line:
                        drawType = WinDrawType.Line_2;
                        break;

                    case WinDrawType.Line_2:
                        DrawLine(StartPoint.X, StartPoint.Y, e.X, e.Y);
                        drawType = WinDrawType.None;
                        break;

                    case WinDrawType.Circle_2:
                        drawType = WinDrawType.Circle_3;
                        break;

                    case WinDrawType.Circle_3:
                        drawCircle = DrawCircleType.None;
                        break;

                    case WinDrawType.Polygon1:
                        drawType = WinDrawType.Polygon1_2;
                        break;

                    case WinDrawType.Polygon2:
                        if (drawPolygon == DrawPolygonType.StartMove)
                        {
                            drawPolygon = DrawPolygonType.None;
                        }
                        break;
                }

            }
            else if (e.Button == MouseButtons.Right)
            {
                switch (drawType)
                {
                    case WinDrawType.Circle_3:
                        DrawCircle(StartPoint.X, StartPoint.Y, EndPoint.X, EndPoint.Y);
                        drawType = WinDrawType.None;
                        break;

                    case WinDrawType.Polygon1_2:
                        DrawPolygon(Polygons);
                        drawType = WinDrawType.None;
                        break;

                    case WinDrawType.Polygon2:
                        DrawPolygon(Polygons);
                        drawType = WinDrawType.None;
                        break;
                }
            }
        }

        private void WindowMouse_HMouseWheel(object sender, HMouseEventArgs e)
        {
            ReDisplay();
        }

        private void WindowMouse_HMouseMove(object sender, HMouseEventArgs e)
        {
            ReDisplay();

            switch (drawType)
            {
                case WinDrawType.Point:
                    disPlay.DispCross(new CvCoord(e.X, e.Y), drawModel.PointSize, HColor.OrangeRed);
                    break;

                case WinDrawType.Line:
                    disPlay.DispCross(new CvCoord(e.X, e.Y), drawModel.PointSize, HColor.OrangeRed);
                    break;

                case WinDrawType.Line_2:
                    disPlay.DispArrow(new CvLine(StartPoint.X, StartPoint.Y, e.X, e.Y), drawModel.ArrowSize, HColor.OrangeRed);
                    break;

                case WinDrawType.Circle:
                    disPlay.DispCross(new CvCoord(e.X, e.Y), drawModel.PointSize, HColor.Red);
                    break;

                case WinDrawType.Circle_2:
                    EndPoint.X = e.X;
                    EndPoint.Y = e.Y;
                    dispCircle = new CvCircle(StartPoint, EndPoint);
                    break;

                case WinDrawType.Circle_3:
                    {
                        if (drawCircle == DrawCircleType.StartMove)
                        {
                            OffsetPoint = new Point2d(EndPoint.X - StartPoint.X, EndPoint.Y - StartPoint.Y);
                            StartPoint.X = e.X;
                            StartPoint.Y = e.Y;

                            EndPoint = new Point2d(StartPoint.X + OffsetPoint.X, StartPoint.Y + OffsetPoint.Y);
                            dispCircle = new CvCircle(StartPoint, EndPoint);
                        }
                        else if (drawCircle == DrawCircleType.EndMove)
                        {
                            EndPoint.X = e.X;
                            EndPoint.Y = e.Y;
                            dispCircle = new CvCircle(StartPoint, EndPoint);
                        }
                        else if (Math.Abs(StartPoint.X - e.X) < 3 && Math.Abs(StartPoint.Y - e.Y) < 3)
                        {
                            disPlay.DispPoint(StartPoint, 10, HColor.Green);
                            drawCircle = DrawCircleType.Start;
                        }
                        else if (Math.Abs(EndPoint.X - e.X) < 3 && Math.Abs(EndPoint.Y - e.Y) < 3)
                        {
                            disPlay.DispPoint(EndPoint, 10, HColor.Green);
                            drawCircle = DrawCircleType.End;
                        }
                        else
                        {
                            drawCircle = DrawCircleType.None;
                        }
                    }
                    break;

                case WinDrawType.Polygon1:
                    disPlay.DispPoint(new Point2d(e.X, e.Y), drawModel.PointSize, HColor.Red);
                    break;

                case WinDrawType.Polygon1_2:
                    {
                        Point2d regPoint = new Point2d(0, 0);
                        if (Polygons.Count > 0)
                        {
                            regPoint = Polygons[Polygons.Count - 1];
                        }
                        else
                        {
                            regPoint = StartPoint;
                        }

                        //disPlay.DispArrow(new DLine(regPoint.X, regPoint.Y, e.X, e.Y), 1, HColor.Red);

                        disPlay.DispPoint(new Point2d(e.X, e.Y), 10, HColor.Red);
                        disPlay.DispLine(new CvLine(regPoint.X, regPoint.Y, e.X, e.Y), 1, HColor.Red);
                    }
                    break;

                case WinDrawType.Polygon2:
                    {
                        if (drawPolygon == DrawPolygonType.StartMove)
                        {
                            Polygons[SelectIndex] = new Point2d(e.X, e.Y);
                        }
                        else
                        {
                            drawPolygon = DrawPolygonType.None;
                            for (int i = 0; i < Polygons.Count; i++)
                            {
                                if (Math.Abs(Polygons[i].X - e.X) < 3 && Math.Abs(Polygons[i].Y - e.Y) < 3)
                                {
                                    disPlay.DispPoint(Polygons[i], 10, HColor.Red);
                                    SelectIndex = i;
                                    drawPolygon = DrawPolygonType.Start;
                                }
                            }
                        }
                    }
                    break;

            }
        }

        private void ReDisplay()
        {
            if(drawType != WinDrawType.None)
                disPlay.ReDispImage();

            switch (drawType)
            {
                case WinDrawType.Circle_2:
                case WinDrawType.Circle_3:
                    {
                        disPlay.DispPoint(StartPoint, 10, HColor.OrangeRed);
                        disPlay.DispPoint(EndPoint, 10, HColor.OrangeRed);
                        disPlay.DispCircle(dispCircle, HColor.Red);
                    }
                    break;
                case WinDrawType.Polygon1:
                case WinDrawType.Polygon1_2:
                    {
                        disPlay.DispPoint(StartPoint, 10, HColor.Green);

                        List<Point2d> points = Polygons;
                        string color = drawType == WinDrawType.Polygon1_2 ? HColor.OrangeRed : HColor.Blue;

                        for (int i = 0; i < Polygons.Count; i++)
                        {
                            disPlay.DispPoint(Polygons[i], 5, HColor.Green);

                            if (i < Polygons.Count - 1)
                            {
                                disPlay.DispLine(new CvLine(points[i].X, points[i].Y, points[i + 1].X, points[i + 1].Y), 2, color);
                            }
                        }

                        if (drawType != WinDrawType.Polygon1_2)
                        {
                            if (Polygons.Count > 2)
                            {
                                disPlay.DispLine(new CvLine(points[0].X, points[0].Y, points[points.Count - 1].X, points[points.Count - 1].Y), 2, color);
                            }
                        }
                    }
                    break;
                case WinDrawType.Polygon2:
                    {
                        for (int i = 0; i < Polygons.Count; i++)
                        {
                            disPlay.DispPoint(Polygons[i], 5, HColor.Blue);

                            if (i < Polygons.Count - 1)
                            {
                                disPlay.DispLine(new CvLine(Polygons[i].X, Polygons[i].Y, Polygons[i + 1].X, Polygons[i + 1].Y), 2, HColor.Red);
                            }
                        }

                        if (Polygons.Count > 2)
                        {
                            disPlay.DispLine(new CvLine(Polygons[0].X, Polygons[0].Y, Polygons[Polygons.Count - 1].X, Polygons[Polygons.Count - 1].Y), 2, HColor.Red);
                        }
                    }
                    break;
            }
        }


    }
}
