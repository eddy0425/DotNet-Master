using System;

namespace DotNet.HWindows.WinDraw
{
    public enum WinDrawType
    {
        None,
        Point,
        Line,
        Line_2,
        Circle,
        Circle_2,
        Circle_3,
        Polygon1,
        Polygon1_2,
        Polygon2,
    }

    enum DrawCircleType
    {
        None,
        Start,
        StartMove,
        End,
        EndMove,
    }

    enum DrawPolygonType
    {
        None,
        Start,
        StartMove,
    }

}
