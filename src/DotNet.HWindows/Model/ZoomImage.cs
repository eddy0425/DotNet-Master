using System;
using System.Windows.Forms;
using HalconDotNet;

namespace DotNet.HWindows
{
    public class ZoomImage : ICloneable
    {
        public HTuple width = 1248;
        public HTuple height = 2200;
        public Control parent;

        public ZoomImage()
        {
            parent = new Control();
        }

        object ICloneable.Clone() => (object)this.Clone();
        public ZoomImage Clone()
        {
            return TransExpV2<ZoomImage, ZoomImage>.Trans(this);
        }

    }
}
