using System;
using HalconDotNet;

namespace DotNet.HWindows
{
    public class HWindowFontFactory
    {
        public static HWindowFontBase CreateFont(HWindow hWindow, string version)
        {
            switch (version)
            {
                case "2018":
                    return new HWindowFont2018(hWindow);
                case "2022":
                    return new HWindowFont2022(hWindow);
                default:
                    throw new ArgumentException("Unsupported version");
            }
        }
    }
}
