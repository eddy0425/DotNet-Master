using System;
using System.Windows.Forms;
using HalconDotNet;

namespace DotNet.Drawing
{
    public struct ZoomImage
    {
        public HTuple width = 1248;
        public HTuple height = 2200;
        public Control parent;

        public ZoomImage()
        {
            parent = new Control();
        }
    }
}
