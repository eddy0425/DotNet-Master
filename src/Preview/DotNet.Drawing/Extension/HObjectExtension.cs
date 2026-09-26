using HalconDotNet;
using System;
using System.Diagnostics.CodeAnalysis;

namespace DotNet.Drawing
{
    public static class HObjectExtension
    {
        /// <summary>
        /// 句柄非 null 且已初始化。
        /// </summary>
        /// <remarks>
        /// 参数刻意标成可空: 这两个扩展方法的全部价值就在于"允许传 null 进来问一句"。
        /// 标成非空会让每个调用点在调用判空方法之前先自己判一次空。
        /// <para>
        /// <c>NotNullWhen(true)</c> 把判空结果告诉编译器: 判过之后调用点不必再补 <c>!</c>。
        /// 该特性在 net45 的 BCL 里不存在, 由本程序集的 NullableAttributes.cs 补齐。
        /// </para>
        /// </remarks>
        public static bool NotNull([NotNullWhen(true)] this HObject? image)
        {
            // is object 而不是原来的 (object)image != null: 两者都是绕开可能的 == 重载做纯引用判空,
            // 但前者能让编译器在 true 分支里把 image 收窄成非空, 后者会在转换处报 CS8600。
            if (image is object)
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
        public static bool IsUsableRegion([NotNullWhen(true)] this HObject? region)
        {
            return region.NotNull() && region.CountObj() > 0;
        }

        /// <summary>
        /// 取图像句柄，空句柄时抛出指明工具名的异常。
        /// </summary>
        /// <remarks>
        /// <c>IHDisplay.HoImage</c> 在「窗口已释放 / 尚未载入图像」时就是 null，
        /// 直接往下送会在 reduce_domain 之类的算子里炸出与真实原因无关的 HALCON 原生异常，
        /// 或者更糟 —— 一个只有堆栈没有说明的 NRE。各策略原本零散写着
        /// <c>if (ho_Image == null || !ho_Image.NotNull()) throw new NullReferenceException("图像来源为空！")</c>，
        /// 这里统一成一处，并把异常类型从 NullReferenceException（应由运行时抛出，不该手写）换成
        /// <see cref="InvalidOperationException"/>，与 ROI 未绘制时的守卫同一口径。
        /// </remarks>
        /// <param name="toolName">出错时写进消息的工具名，通常传策略的 <c>Name</c>。</param>
        public static HObject RequireImage(this HObject? image, string toolName)
        {
            if (!image.NotNull())
                throw new InvalidOperationException($"{toolName} : 图像来源为空，无法执行！");

            return image;
        }
    }
}
