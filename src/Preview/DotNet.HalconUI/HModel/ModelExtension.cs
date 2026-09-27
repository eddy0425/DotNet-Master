using System;
using HalconDotNet;
using DotNet.HalconCore;


namespace DotNet.HalconUI
{
    public static class ModelExtension
    {
        /// <summary> 获取模板轮廓 </summary>
        /// <remarks>
        /// 模板原点处的轮廓只是中间结果：仿射变换后必须释放，否则每次显示模板都泄漏一份
        /// （原先直接用变换结果覆盖引用，中间对象从未释放）。变换失败时同样释放后再抛出。
        /// 缩放模型：<see cref="ModelResult"/> 不带缩放系数，只能给出 1 倍大小的轮廓（位置、角度正确）；
        /// 需要按匹配缩放显示时由调用方自行按 scale 变换。
        /// 通用模型：只取 <see cref="ModelResult.ResultID"/> 里的匹配结果，不使用 Row/Column/Angle。
        /// </remarks>
        /// <exception cref="ArgumentException">通用模型且 ResultID 不是单个句柄。</exception>
        public static void GetModelContours(this ModelType type, HTuple modelID, ModelResult result, out HObject ho_Contours)
        {
            switch (type)
            {
                case ModelType.NccModel:
                    {
                        HOperatorSet.GetNccModelRegion(out HObject modelRegion, modelID);
                        try
                        {
                            HOperatorSet.VectorAngleToRigid(0, 0, 0, result.Row, result.Column, result.Angle, out HTuple hv_HomMat2D);
                            HOperatorSet.AffineTransRegion(modelRegion, out ho_Contours, hv_HomMat2D, "nearest_neighbor");
                        }
                        finally { modelRegion.Dispose(); }
                    }
                    break;
                case ModelType.ShapeModel:
                case ModelType.ScaledModel:
                    {
                        HOperatorSet.GetShapeModelContours(out HObject modelContours, modelID, 1);
                        try
                        {
                            HOperatorSet.VectorAngleToRigid(0, 0, 0, result.Row, result.Column, result.Angle, out HTuple hv_HomMat2D);
                            HOperatorSet.AffineTransContourXld(modelContours, out ho_Contours, hv_HomMat2D);
                        }
                        finally { modelContours.Dispose(); }
                    }
                    break;
                case ModelType.GenericModel:
                    // 通用模型不看 Row/Column/Angle，轮廓只能从匹配结果句柄里取；
                    // 句柄缺失时 HALCON 只报 #1401 "参数个数错误"，看不出是调用方漏传了 ResultID
                    if (result.ResultID == null || result.ResultID.Length != 1)
                        throw new ArgumentException("通用模型需要 ModelResult.ResultID（find_generic_shape_model 返回的单个结果句柄）。", nameof(result));
                    HOperatorSet.GetGenericShapeModelResultObject(out ho_Contours, result.ResultID, "all", "contours");
                    break;
                default:
                    HOperatorSet.GenEmptyObj(out ho_Contours);
                    break;
            }
        }

    }
}
