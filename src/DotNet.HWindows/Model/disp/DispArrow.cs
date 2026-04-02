using OpenCvSharp;

namespace DotNet.HWindows
{
    public class DispArrow : CvLine
    {
        /// <summary>
        /// 颜色
        /// </summary>
        public string Color { set; get; } = HColor.Red;

        /// <summary>
        /// 箭头大小
        /// </summary>
        public double Size { set; get; } = 20;

        public DispArrow(Point2d _start, Point2d _end, double _size, string _color)
            : base(_start, _end)
        {
            Size = _size;
            Color = _color;
        }

        public DispArrow(double _startX, double _startY, double _endX, double _endY, double _size, string _color)
            : base(_startX, _startY, _endX, _endY)
        {
            Size = _size;
            Color = _color;
        }
        public DispArrow(CvLine _line, double _size, string _color)
         : base(_line.start, _line.end)
        {
            Size = _size;
            Color = _color;
        }
    }
}
