using HalconDotNet;

namespace DotNet.Drawing
{
    public static class HObjectExtension
    {
        public static bool NotNull(this HObject image)
        {
            if ((object)image != null)
            {
                return image.IsInitialized();
            }
            return false;
        }

        /// <summary>
        /// 区域句柄是否可直接交给 HALCON 算子: 非 null、已初始化、且 count_obj 大于 0。
        /// </summary>
        /// <remarks>
        /// gen_empty_obj 产出的是"已初始化但长度为 0"的元组, <see cref="NotNull"/> 判不出来。
        /// 这种空元组送进 reduce_domain 会抛出与真实原因(ROI 未绘制 / 上游未运行)毫无关系的
        /// HALCON 原生异常; 在匹配里则因 count_obj 为 0 让循环一次都不进, 静默跑出 0 个结果。
        /// 上游解析路径由 <c>TryResolveRegionFrom</c> 复用本方法, 本地配置 ROI 需各策略自行调用。
        /// </remarks>
        public static bool IsUsableRegion(this HObject region)
        {
            return region.NotNull() && region.CountObj() > 0;
        }
    }
}
