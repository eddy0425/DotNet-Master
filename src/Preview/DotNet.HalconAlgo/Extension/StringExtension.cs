using System;


namespace DotNet.HalconAlgo
{
    public static class StringExtension
    {
        public static string ToTmplPoint(this string path)
        {
            return path.Substring(0, path.LastIndexOf('/') + 1) + "TmplPoint";
        }

    }
}
