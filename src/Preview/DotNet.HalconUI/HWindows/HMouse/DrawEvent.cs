using DotNet.Drawing;
using DotNet.HalconUI;
using HalconDotNet;
using System;
using DotNet.Vision.Abstractions;

namespace DotNet.HalconUI
{
    // 事件参数全部为不可变对象 (只读属性), 由 EventHandler<T> 标准委托承载.
    // 不再为每个 Args 单独定义委托类型, 直接使用 BCL 的 EventHandler<TEventArgs>.

    public class DrawModelUIArgs : EventArgs
    {
        public DrawModelUIArgs(string modelPath, HObject ho_ModeRect, HObject ho_Contour, ModelResult result)
        {
            ModelPath = modelPath;
            HoModeRect = ho_ModeRect;
            HoContour = ho_Contour;
            Result = result;
        }

        /// <summary> 模版路径 </summary>
        public string ModelPath { get; set; }

        /// <summary> 模版区域 </summary>
        public HObject HoModeRect { get; private set; }

        /// <summary> 模版轮廓 </summary>
        public HObject HoContour { get; private set; }

        /// <summary> 匹配结果 </summary>
        public ModelResult Result { get; set; }

    }

}
