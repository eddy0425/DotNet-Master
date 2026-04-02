using OpenCvSharp;

namespace DotNet.HWindows
{
    public class DispPoint2d : Point2d
    {
        /// <summary>
        /// 颜色
        /// </summary>
        public string Color { set; get; } = HColor.Red;

        /// <summary>
        /// 点大小
        /// </summary>
        public int Size { set; get; } = 15;

        public DispPoint2d(int _size, string _color)
            : base()
        {
            Size = _size;
            Color = _color;
        }

        public DispPoint2d(double x, double y, int _size, string _color)
            : base(x,y)    
        {
            Size = _size;
            Color = _color;
        }

        public DispPoint2d(Point2d point, int _size, string _color)
            : base(point.X, point.Y)
        {
            Size = _size;
            Color = _color;
        }
    }
}
