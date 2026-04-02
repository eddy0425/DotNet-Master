
namespace DotNet.HWindows
{
    public class DispDCircle : CvCircle
    {
        /// <summary>
        /// 颜色
        /// </summary>
        public string Color { set; get; } = HColor.Red;

        public DispDCircle(string _color) 
            : base()
        {
            Color = _color;
        }
        public DispDCircle(double _X, double _Y, double _radius, string _color) 
            : base(_X, _Y, _radius)
        {
            Color = _color;
        }
        public DispDCircle(double _X, double _Y, double _radius, double _startPhi, double _endPhi, string _color)
            : base(_X, _Y, _radius, _startPhi, _endPhi)    
        {
            Color = _color;
        }

        public DispDCircle(CvCircle circle, string _color)
           : base(circle.X, circle.Y, circle.radius)
        {
            Color = _color;
        }
    }
}
