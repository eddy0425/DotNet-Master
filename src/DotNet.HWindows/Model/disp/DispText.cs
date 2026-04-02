using HalconDotNet;

namespace DotNet.HWindows
{
    public class DispText
    {
        /// <summary>
        /// 字符内容
        /// </summary>
        public string hoText { set; get; }

        /// <summary>
        /// 字体X坐标
        /// </summary>
        public HTuple FontX { set; get; } = 50;

        /// <summary>
        /// 字体Y坐标
        /// </summary>
        public HTuple FontY { set; get; } = 50;

        /// <summary>
        /// 字体大小
        /// </summary>
        public HTuple FontSize { set; get; } = 15;

        /// <summary>
        /// 颜色
        /// </summary>
        public string Color { set; get; } = HColor.Red;

        public DispText(string message, HTuple _fontX, HTuple _fontY, HTuple _size, string _color)
        {
            hoText = message;
            FontX = _fontX;
            FontY = _fontY;
            FontSize = _size;
            Color = _color;
        }

        public DispText(string message, HTuple _fontX, HTuple _offsetX, HTuple _fontY, HTuple _offsetY, HTuple _size, string _color)
        {
            hoText = message;
            FontX = _fontX + _offsetX;
            FontY = _fontY + _offsetY;
            FontSize = _size;
            Color = _color;
        }
    }
}
