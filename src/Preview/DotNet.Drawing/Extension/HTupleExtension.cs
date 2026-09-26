using HalconDotNet;
using System.Diagnostics.CodeAnalysis;

namespace DotNet.Drawing
{
    public static class HTupleExtension
    {
        /// <summary>
        /// 元组非 null 且至少有一个元素。
        /// </summary>
        /// <returns> 空：false  不为空：true </returns>
        /// <remarks>
        /// 原实现对 <c>HTupleType.EMPTY</c> 返回 <c>Length &gt; 0</c>（EMPTY 的长度必为 0，恒为 false），
        /// 其余类型一律返回 true，绕了一圈等价于 <c>Length &gt; 0</c>；且传 null 直接 NRE。
        /// 参数标可空的理由同 <see cref="HObjectExtension.NotNull"/>。
        /// </remarks>
        public static bool NotNull([NotNullWhen(true)] this HTuple? hTuple)
        {
            return hTuple is object && hTuple.Length > 0;
        }

    }
}
