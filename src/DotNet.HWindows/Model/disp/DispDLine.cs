using OpenCvSharp;

namespace DotNet.HWindows
{
    public class DispDLine : CvLine
    {
        /// <summary>
        /// 颜色
        /// </summary>
        public string Color { set; get; } = HColor.Red;

        public DispDLine(string _color)
            : base()
        {
            Color = _color;
        }
        public DispDLine(Point2d _start, Point2d _end, string _color)
            :base(_start, _end)
        {
            Color = _color;
        }
     
        public DispDLine(double _startX, double _startY, double _endX, double _endY, string _color)
            : base(_startX, _startY, _endX, _endY)
        {
            Color = _color;
        }
        public DispDLine(CvLine _line, string _color)
         : base(_line.start, _line.end)
        {
            Color = _color;
        }
    }
}
