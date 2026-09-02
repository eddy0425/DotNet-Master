using HalconDotNet;
using DotNet.Vision.Abstractions;


namespace DotNet.HalconUI
{
    public static class ModelExtension
    {
        /// <summary> 获取模板轮廓 </summary>
        public static void GetModelContours(this ModelType type, HTuple modelID, ModelResult result, out HObject ho_Contours)
        {
            HOperatorSet.GenEmptyObj(out ho_Contours);

            switch (type)
            {
                case ModelType.NccModel:
                    {
                        ho_Contours.Dispose();
                        HOperatorSet.GetNccModelRegion(out ho_Contours, modelID);
                        HOperatorSet.VectorAngleToRigid(0, 0, 0, result.Row, result.Column, result.Angle, out HTuple hv_HomMat2D);
                        HOperatorSet.AffineTransRegion(ho_Contours, out HObject regionAffineTrans, hv_HomMat2D, "nearest_neighbor");
                        ho_Contours = regionAffineTrans;
                    }
                    break;
                case ModelType.ShapeModel:
                case ModelType.ScaledModel:
                    {
                        ho_Contours.Dispose();
                        HOperatorSet.GetShapeModelContours(out ho_Contours, modelID, 1);
                        HOperatorSet.VectorAngleToRigid(0, 0, 0, result.Row, result.Column, result.Angle, out HTuple hv_HomMat2D);
                        HOperatorSet.AffineTransContourXld(ho_Contours, out HObject regionAffineTrans, hv_HomMat2D);
                        ho_Contours = regionAffineTrans;
                    }
                    break;
                case ModelType.GenericModel:
                    {
                        ho_Contours.Dispose();
                        HOperatorSet.GetGenericShapeModelResultObject(out ho_Contours, result.ResultID, "all", "contours");
                    }
                    break;
            }
        }

    }
}
