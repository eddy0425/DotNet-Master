using HalconDotNet;

namespace DotNet.Drawing
{
    public static class HTupleExtension
    {
        /// <summary>
        /// 判断HTuple是否为空，
        /// </summary>
        /// <returns> 空：false  不为空：true </returns>
        public static bool NotNull(this HTuple hTuple)
        {
            if (hTuple.Type == HTupleType.EMPTY)
            {
                return hTuple.Length > 0;
            }
            return true;
        }

    }
}
