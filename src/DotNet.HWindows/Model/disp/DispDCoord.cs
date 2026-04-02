using OpenCvSharp;

namespace DotNet.HWindows
{
    public class DispDCoord : CvCoord
    {
        /// <summary>
        /// 颜色
        /// </summary>
        public string Color { set; get; } = HColor.Red;

        /// <summary>
        /// 点大小
        /// </summary>
        public int Size { set; get; } = 15;

        public DispDCoord(int _size, string _color)
            : base()
        {
            Size = _size;
            Color = _color;
        }

        public DispDCoord(Point2d _center, double _angle, int _size, string _color)
            : base(_center, _angle)
        {
            Size = _size;
            Color = _color;
        }

        public DispDCoord(double _X, double _Y, int _size, string _color)
            :base(_X, _Y)
        {
            Size = _size;
            Color = _color;
        }

        public DispDCoord(double _X, double _Y, double _angle, int _size, string _color)
            : base(_X, _Y, _angle)
        {
            Size = _size;
            Color = _color;
        }

        public DispDCoord(CvCoord _coord, int _size, string _color)
           : base(_coord.X, _coord.Y, _coord.angle)
        {
            Size = _size;
            Color = _color;
        }
    }
}
