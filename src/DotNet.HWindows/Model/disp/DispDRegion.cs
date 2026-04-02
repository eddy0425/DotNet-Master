using HalconDotNet;

namespace DotNet.HWindows
{
    public class DispDRegion : CvRegion
    {
        /// <summary>
        /// 颜色
        /// </summary>
        public string Color { set; get; } = HColor.Red;

        public DispDRegion(string _color)
            : base()
        {
            Color = _color;
        }
        //HObject objectVal

        public DispDRegion(HObject objectVal, string _color)
        {
            HoRegion = objectVal.Clone();
            Color = _color;
        }
    }
}
