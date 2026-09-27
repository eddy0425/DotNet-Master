using HalconDotNet;
using DotNet.Vision.Abstractions;


namespace DotNet.HalconUI
{
    public static class ModelExtension
    {
        /// <summary> 获取模板轮廓 </summary>
        /// <remarks>
        /// 模板原点处的轮廓只是中间结果：仿射变换后必须释放，否则每次显示模板都泄漏一份
        /// （原先直接用变换结果覆盖引用，中间对象从未释放）。变换失败时同样释放后再抛出。
        /// </remarks>
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
                    HOperatorSet.GetGenericShapeModelResultObject(out ho_Contours, result.ResultID, "all", "contours");
                    break;
                default:
                    HOperatorSet.GenEmptyObj(out ho_Contours);
                    break;
            }
        }

    }
}
