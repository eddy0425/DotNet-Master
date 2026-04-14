using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNet.Drawing
{
    public static class CoordExtension
    {
        /// <summary>
        /// 判断CvCoord是否为空，
        /// </summary>
        /// <returns> 空：false  不为空：true </returns>
        public static bool NotNull(this CvCoord coord)
        {
            return (coord == null);
        }

    }
}
