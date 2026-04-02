using HalconDotNet;
using System.Collections.Generic;

namespace DotNet.HWindows
{
    public static class ModelTypeExtension
    {
        public static void FindModelExists(this FindModel findModel)
        {
            if (findModel.ModelInfo == null) findModel.ModelInfo = new ModelInfo();
            if (findModel.FindROI == null) findModel.FindROI = new DispDRegion(HColor.Blue);
            if (findModel.SetROI == null) findModel.SetROI = new DispDRegion(HColor.Red);
        }

        #region ModelOrigin
        /// <summary>
        /// 设置模板原点
        /// </summary>
        public static void SetModelOrigin(this ModelType type, HTuple modelID, HTuple rowTrans, HTuple colTrans)
        {
            switch (type)
            {
                case ModelType.NccModel:
                    HOperatorSet.SetNccModelOrigin(modelID, rowTrans, colTrans);
                    break;
                case ModelType.ShapeModel:
                    HOperatorSet.SetShapeModelOrigin(modelID, rowTrans, colTrans);
                    break;
                case ModelType.ScaledShapeModel:
                    HOperatorSet.SetShapeModelOrigin(modelID, rowTrans, colTrans);
                    break;
                case ModelType.GenericShapeModel:
                    HOperatorSet.SetGenericShapeModelParam(modelID, "origin_row", rowTrans);
                    HOperatorSet.SetGenericShapeModelParam(modelID, "origin_column", colTrans);
                    break;
            }
        }

        /// <summary>
        /// 获取模板原点  
        /// </summary>
        public static void GetModelOrigin(this ModelType type, HTuple modelID, out HTuple row, out HTuple column)
        {
            switch (type)
            {
                case ModelType.NccModel:
                    HOperatorSet.GetNccModelOrigin(modelID, out row, out column);
                    break;
                case ModelType.ShapeModel:
                    HOperatorSet.GetShapeModelOrigin(modelID, out row, out column);
                    break;
                case ModelType.ScaledShapeModel:
                    HOperatorSet.GetShapeModelOrigin(modelID, out row, out column);
                    break;
                case ModelType.GenericShapeModel:
                    HOperatorSet.GetGenericShapeModelParam(modelID, "origin_row", out row);
                    HOperatorSet.GetGenericShapeModelParam(modelID, "origin_column", out column);
                    break;
                default:
                    row = default(HTuple); column = default(HTuple);
                    break;
            }
        }

        #endregion

        #region CreateModel
       
        /// <summary>
        /// 创建模板
        /// </summary>
        public static void CreateModel(this ModelType type, HObject imgReduced, ModelInfo info, int numLevels = 0)
        {
            switch (type)
            {
                case ModelType.NccModel:
                    {
                        //imgReduced 图片
                        //numLevels 金字塔层数    -》0
                        //info.angleStart 起始角  -》-90
                        //info.angleExtent 增量角 -》180
                        //modelID 模板句柄
                        HOperatorSet.CreateNccModel(imgReduced, numLevels, info.angleStart.TupleRad(), info.angleExtent.TupleRad(),
                                                    "auto", "use_polarity", out HTuple modelID);
                        info.modelID = modelID;
                    }
                    break;
                case ModelType.ShapeModel:
                    {
                        //imgReduced 图片
                        //numLevels 金字塔层数    -》0
                        //info.angleStart 起始角  -》-90
                        //info.angleExtent 增量角 -》180
                        //modelID 模板句柄
                        HOperatorSet.CreateShapeModel(imgReduced, numLevels, info.angleStart.TupleRad(), info.angleExtent.TupleRad(),
                                                     "auto", "auto", "use_polarity", "auto", "auto", out HTuple modelID);
                        info.modelID = modelID;
                    }
                    break;
                case ModelType.ScaledShapeModel:
                    {
                        HOperatorSet.CreateScaledShapeModel(imgReduced, numLevels, info.angleStart.TupleRad(), info.angleExtent.TupleRad(), "auto", info.scaleMin, info.scaleMax,
                                                    "auto", "auto", "use_polarity", "auto", "auto", out HTuple modelID);
                        info.modelID = modelID;
                    }
                    break;
                case ModelType.GenericShapeModel:
                    {
                        HOperatorSet.CreateGenericShapeModel(out HTuple modelID);
                        HOperatorSet.SetGenericShapeModelParam(modelID, "iso_scale_max", info.scaleMax);
                        HOperatorSet.SetGenericShapeModelParam(modelID, "iso_scale_min", info.scaleMin);

                        HOperatorSet.TrainGenericShapeModel(imgReduced, modelID);

                        HOperatorSet.SetGenericShapeModelParam(modelID, "num_matches", info.numMatches);
                        HOperatorSet.SetGenericShapeModelParam(modelID, "min_score", info.minScore);
                        HOperatorSet.SetGenericShapeModelParam(modelID, "angle_start", info.angleStart);
                        HOperatorSet.SetGenericShapeModelParam(modelID, "angle_end", info.angleEnd);

                        info.modelID = modelID;
                    }
                    break;
            }
        }

        #endregion

        #region FindModel2
        public static void FindNccModel2(this HObject image, HTuple modelID, HTuple angleStart, HTuple angleExtent, HTuple minScore, HTuple numMatches, HTuple maxOverlap, HTuple subPixel, HTuple numLevels, out HTuple row, out HTuple column, out HTuple angle, out HTuple score)
        {
            HOperatorSet.FindNccModel(image, modelID, angleStart, angleExtent, minScore, numMatches, maxOverlap, subPixel,
                                      numLevels, out row, out column, out angle, out score);

            if (!row.NotNull() || !column.NotNull() || !angle.NotNull() || !score.NotNull())
            {
                int m_numLevels = 2;
                HOperatorSet.FindNccModel(image, modelID, angleStart, angleExtent, minScore, numMatches, maxOverlap, subPixel,
                                      m_numLevels, out row, out column, out angle, out score);
            }
        }
        public static void FindShapeModel2(this HObject image, HTuple modelID, HTuple angleStart, HTuple angleExtent, HTuple minScore, HTuple numMatches, HTuple maxOverlap, HTuple subPixel, HTuple numLevels, HTuple greediness, out HTuple row, out HTuple column, out HTuple angle, out HTuple score)
        {
            HOperatorSet.FindShapeModel(image, modelID, angleStart, angleExtent, minScore, numMatches, maxOverlap, subPixel,
                                      numLevels, greediness, out row, out column, out angle, out score);

            if (!row.NotNull() || !column.NotNull() || !angle.NotNull() || !score.NotNull())
            {
                int m_numLevels = 2;
                HOperatorSet.FindShapeModel(image, modelID, angleStart, angleExtent, minScore, numMatches, maxOverlap, subPixel,
                                      m_numLevels, greediness, out row, out column, out angle, out score);
            }
        }
        public static void FindScaledShapeModel2(this HObject image, HTuple modelID, HTuple angleStart, HTuple angleExtent, HTuple scaleMin, HTuple scaleMax, HTuple minScore, HTuple numMatches, HTuple maxOverlap, HTuple subPixel, HTuple numLevels, HTuple greediness, out HTuple row, out HTuple column, out HTuple angle, out HTuple scale, out HTuple score)
        {
            HOperatorSet.FindScaledShapeModel(image, modelID, angleStart, angleExtent, scaleMin, scaleMax, minScore, numMatches, maxOverlap, subPixel,
                                            numLevels, greediness, out row, out column, out angle, out scale, out score);

            if (!row.NotNull() || !column.NotNull() || !angle.NotNull() || !score.NotNull())
            {
                int m_numLevels = 2;
                HOperatorSet.FindScaledShapeModel(image, modelID, angleStart, angleExtent, scaleMin, scaleMax, minScore, numMatches, maxOverlap, subPixel,
                                          m_numLevels, greediness, out row, out column, out angle, out scale, out score);
            }
        }

        #endregion

        #region FindModel

        /// <summary>
        /// 查找模板结果
        /// </summary>
        public static void FindModel(this ModelType type, HObject hImage, ModelInfo info, out List<ModelResult> results)
        {
            results = new List<ModelResult>();
            if (info.modelID == null) return;

            switch (type)
            {
                case ModelType.NccModel:
                    {
                        info.subPixel = "true";
                        HOperatorSet.FindNccModel(hImage, info.modelID, info.angleStart.TupleRad(), info.angleExtent.TupleRad(), info.minScore,
                                                 info.numMatches, info.maxOverlap, info.subPixel, info.numLevels,
                                                 out HTuple row, out HTuple column, out HTuple angle, out HTuple score);
                        if (score.NotNull())
                        {
                            for (int i = 0; i < score.DArr.Length; i++)
                            {
                                results.Add(new ModelResult(row.DArr[i], column.DArr[i], angle.DArr[i], score.DArr[i]));
                            }
                        }
                    }
                    break;
                case ModelType.ShapeModel:
                    {
                        info.subPixel = "least_squares";
                        HOperatorSet.FindShapeModel(hImage, info.modelID, info.angleStart.TupleRad(), info.angleExtent.TupleRad(), info.minScore,
                                                  info.numMatches, info.maxOverlap, info.subPixel, info.numLevels, info.greediness,
                                                  out HTuple row, out HTuple column, out HTuple angle, out HTuple score);
                        if (score.NotNull())
                        {
                            for (int i = 0; i < score.DArr.Length; i++)
                            {
                                results.Add(new ModelResult(row.DArr[i], column.DArr[i], angle.DArr[i], score.DArr[i]));
                            }
                        }
                    }
                    break;
                case ModelType.ScaledShapeModel:
                    {
                        info.subPixel = "least_squares";
                        HOperatorSet.FindScaledShapeModel(hImage, info.modelID, info.angleStart.TupleRad(), info.angleExtent.TupleRad(),
                                           info.scaleMin, info.scaleMax, info.minScore,
                                           info.numMatches, info.maxOverlap, info.subPixel, info.numLevels, info.greediness,
                                           out HTuple row, out HTuple column, out HTuple angle, out HTuple scale, out HTuple score);
                        if (score.NotNull())
                        {
                            for (int i = 0; i < score.DArr.Length; i++)
                            {
                                results.Add(new ModelResult(row.DArr[i], column.DArr[i], angle.DArr[i], scale.DArr[i], score.DArr[i]));
                            }
                        }
                    }
                    break;
                case ModelType.GenericShapeModel:
                    {
                        HOperatorSet.FindGenericShapeModel(hImage, info.modelID, out HTuple matchResultID, out HTuple numMatchResult);

                        HOperatorSet.GetGenericShapeModelResult(matchResultID, "all", "row", out HTuple row);
                        HOperatorSet.GetGenericShapeModelResult(matchResultID, "all", "column", out HTuple column);
                        HOperatorSet.GetGenericShapeModelResult(matchResultID, "all", "angle", out HTuple angle);
                        HOperatorSet.GetGenericShapeModelResult(matchResultID, "all", "score", out HTuple score);
                        //get_generic_shape_model_result (MatchResultID, 'all', 'score', GenParamValue)

                        if (score.NotNull())
                        {
                            for (int i = 0; i < numMatchResult; i++)
                            {
                                results.Add(new ModelResult(row.DArr[i], column.DArr[i], angle.DArr[i], score.DArr[i], matchResultID, numMatchResult));
                            }
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// 查找模板结果
        /// </summary>
        public static void FindModel(this ModelType type, HObject hImage, ModelInfo info, int numMatches, out ModelResult result)
        {
            if (info.modelID == null) { result = new ModelResult(); return; }

            switch (type)
            {
                case ModelType.NccModel:
                    {
                        info.subPixel = "true";

                        HOperatorSet.FindNccModel(hImage, info.modelID, info.angleStart.TupleRad(), info.angleExtent.TupleRad(), info.minScore,
                                                 numMatches, info.maxOverlap, info.subPixel, info.numLevels,
                                                 out HTuple row, out HTuple column, out HTuple angle, out HTuple score);
                       
                        result = new ModelResult(row, column, angle, score);
                    }
                    break;
                case ModelType.ShapeModel:
                    {
                        info.subPixel = "least_squares";

                        HOperatorSet.FindShapeModel(hImage, info.modelID, info.angleStart.TupleRad(), info.angleExtent.TupleRad(), info.minScore,
                                                    numMatches, info.maxOverlap, info.subPixel, info.numLevels, info.greediness,
                                                    out HTuple row, out HTuple column, out HTuple angle, out HTuple score);

                        result = new ModelResult(row, column, angle, score);
                    }
                    break;
                case ModelType.ScaledShapeModel:
                    {
                        info.subPixel = "least_squares";

                        HOperatorSet.FindScaledShapeModel(hImage, info.modelID, info.angleStart.TupleRad(), info.angleExtent.TupleRad(),
                                           info.scaleMin, info.scaleMax, info.minScore,
                                           numMatches, info.maxOverlap, info.subPixel, info.numLevels, info.greediness,
                                           out HTuple row, out HTuple column, out HTuple angle, out HTuple scale, out HTuple score);

                        result = new ModelResult(row, column, angle, scale, score);
                    }
                    break;
                case ModelType.GenericShapeModel:
                    {
                        HOperatorSet.FindGenericShapeModel(hImage, info.modelID, out HTuple matchResultID, out HTuple numMatchResult);

                        HOperatorSet.GetGenericShapeModelResult(matchResultID, "all", "row", out HTuple row);
                        HOperatorSet.GetGenericShapeModelResult(matchResultID, "all", "column", out HTuple column);
                        HOperatorSet.GetGenericShapeModelResult(matchResultID, "all", "angle", out HTuple angle);
                        HOperatorSet.GetGenericShapeModelResult(matchResultID, "all", "score", out HTuple score);
                        //get_generic_shape_model_result (MatchResultID, 'all', 'score', GenParamValue)

                        result = new ModelResult(row, column, angle, score, matchResultID, numMatchResult);
                    }
                    break;
                default:
                    result = new ModelResult();
                    break;
            }
        }

        /// <summary>
        /// 查找模板结果
        /// </summary>
        public static void FindModel2(this ModelType type, HObject hImage, ModelInfo info, int numMatches, out ModelResult result)
        {
            if (info.modelID == null) { result = new ModelResult(); return; }

            switch (type)
            {
                case ModelType.NccModel:
                    {
                        info.subPixel = "true";

                        hImage.FindNccModel2(info.modelID, info.angleStart.TupleRad(), info.angleExtent.TupleRad(), info.minScore,
                                                 numMatches, info.maxOverlap, info.subPixel, info.numLevels,
                                                 out HTuple row, out HTuple column, out HTuple angle, out HTuple score);

                        result = new ModelResult(row, column, angle, score);
                    }
                    break;
                case ModelType.ShapeModel:
                    {
                        info.subPixel = "least_squares";

                        hImage.FindShapeModel2(info.modelID, info.angleStart.TupleRad(), info.angleExtent.TupleRad(), info.minScore,
                                                    numMatches, info.maxOverlap, info.subPixel, info.numLevels, info.greediness,
                                                    out HTuple row, out HTuple column, out HTuple angle, out HTuple score);

                        result = new ModelResult(row, column, angle, score);
                    }
                    break;
                case ModelType.ScaledShapeModel:
                    {
                        info.subPixel = "least_squares";

                        hImage.FindScaledShapeModel2(info.modelID, info.angleStart.TupleRad(), info.angleExtent.TupleRad(),
                                           info.scaleMin, info.scaleMax, info.minScore,
                                           numMatches, info.maxOverlap, info.subPixel, info.numLevels, info.greediness,
                                           out HTuple row, out HTuple column, out HTuple angle, out HTuple scale, out HTuple score);

                        result = new ModelResult(row, column, angle, scale, score);
                    }
                    break;
                case ModelType.GenericShapeModel:
                    {
                        HOperatorSet.FindGenericShapeModel(hImage, info.modelID, out HTuple matchResultID, out HTuple numMatchResult);

                        HOperatorSet.GetGenericShapeModelResult(matchResultID, "all", "row", out HTuple row);
                        HOperatorSet.GetGenericShapeModelResult(matchResultID, "all", "column", out HTuple column);
                        HOperatorSet.GetGenericShapeModelResult(matchResultID, "all", "angle", out HTuple angle);
                        HOperatorSet.GetGenericShapeModelResult(matchResultID, "all", "score", out HTuple score);
                        //get_generic_shape_model_result (MatchResultID, 'all', 'score', GenParamValue)

                        result = new ModelResult(row, column, angle, score, matchResultID, numMatchResult);
                    }
                    break;
                default:
                    result = new ModelResult();
                    break;
            }
        }

        /// <summary>
        /// 查找模板结果
        /// </summary>
        public static void FindModelF(this ModelType type, HObject hImage, HTuple modelID, out ModelResult result)
        {
            if (modelID == null) { result = new ModelResult(); return; }

            switch (type)
            {
                case ModelType.NccModel:
                    {
                        HOperatorSet.GetNccModelParams(modelID, out HTuple numLevels, out HTuple angleStart, out HTuple angleExtent, out HTuple angleStep, out HTuple metric);

                        ModelInfo info = new ModelInfo();
                        info.minScore = 0.4;
                        for (int i = numLevels; i > -1; i--)
                        {
                            HOperatorSet.FindNccModel(hImage, modelID, angleStart, angleExtent,
                                                    info.minScore, info.numMatches, info.maxOverlap, info.subPixel, i,
                                                    out HTuple row, out HTuple column, out HTuple angle, out HTuple score);

                            if (row.NotNull() && column.NotNull() && angle.NotNull() && score.NotNull())
                            {
                                result = new ModelResult(row.D, column.D, angle.D, score.D);
                                return;
                            }
                        }
                    }
                    break;
                case ModelType.ShapeModel:
                    {
                        HOperatorSet.GetShapeModelParams(modelID, out HTuple numLevels, out HTuple angleStart, out HTuple angleExtent, out HTuple angleStep, out HTuple scaleMin, out HTuple scaleMax, out HTuple scaleStep, out HTuple metric, out HTuple minContrast);

                        ModelInfo info = new ModelInfo();
                        info.minScore = 0.4;
                        for (int i = numLevels; i > -1; i--)
                        {
                            HOperatorSet.FindShapeModel(hImage, modelID, angleStart, angleExtent, info.minScore,
                                info.numMatches, info.maxOverlap, info.subPixel, i, info.greediness,
                                out HTuple row, out HTuple column, out HTuple angle, out HTuple score);
                            if (row.NotNull() && column.NotNull() && angle.NotNull() && score.NotNull())
                            {
                                result = new ModelResult(row.D, column.D, angle.D, score.D);
                                return;
                            }
                        }
                    }
                    break;
                case ModelType.ScaledShapeModel:
                    {
                        HOperatorSet.GetShapeModelParams(modelID, out HTuple numLevels, out HTuple angleStart, out HTuple angleExtent, out HTuple angleStep, out HTuple scaleMin, out HTuple scaleMax, out HTuple scaleStep, out HTuple metric, out HTuple minContrast);

                        ModelInfo info = new ModelInfo();
                        info.minScore = 0.4;
                        for (int i = numLevels; i > -1; i--)
                        {
                            HOperatorSet.FindScaledShapeModel(hImage, modelID, angleStart, angleExtent, scaleMin, scaleMax,
                                info.minScore, info.numMatches, info.maxOverlap, info.subPixel, i, info.greediness,
                                out HTuple row, out HTuple column, out HTuple angle, out HTuple scale, out HTuple score);

                            if (row.NotNull() && column.NotNull() && angle.NotNull() && score.NotNull())
                            {
                                result = new ModelResult(row.D, column.D, angle.D, score.D);
                                return;
                            }
                        }
                    }
                    break;
                case ModelType.GenericShapeModel:
                    {
                        HOperatorSet.FindGenericShapeModel(hImage, modelID, out HTuple matchResultID, out HTuple numMatchResult);

                        HOperatorSet.GetGenericShapeModelResult(matchResultID, "all", "row", out HTuple row);
                        HOperatorSet.GetGenericShapeModelResult(matchResultID, "all", "column", out HTuple column);
                        HOperatorSet.GetGenericShapeModelResult(matchResultID, "all", "angle", out HTuple angle);
                        HOperatorSet.GetGenericShapeModelResult(matchResultID, "all", "score", out HTuple score);
                        //get_generic_shape_model_result (MatchResultID, 'all', 'score', GenParamValue)

                        if (row.NotNull() && column.NotNull() && angle.NotNull() && score.NotNull())
                        {
                            result = new ModelResult(row, column, angle, score, matchResultID, numMatchResult);
                            return;
                        }
                    }
                    break;
            }
            result = new ModelResult();
        }

        #endregion

        /// <summary>
        /// 获取模板中心
        /// </summary>
        /// <returns></returns>
        public static CvCoord GetModelCentre(this ModelType type, HObject imgReduced, ModelInfo info)
        {
            type.FindModelF(imgReduced, info.modelID,out ModelResult result);
            return result.coord;
        }

        /// <summary>
        /// 获取模板轮廓
        /// </summary>
        public static void GetModelContours(this ModelType type, HTuple modelID, ModelResult result, out HObject modelContours, int level = 1)
        {
            HOperatorSet.GenEmptyObj(out modelContours);

            switch (type)
            {
                case ModelType.NccModel:
                    {
                        HTuple hv_HomMat2D = new HTuple();
                        HOperatorSet.GetNccModelRegion(out modelContours, modelID);
                        HOperatorSet.VectorAngleToRigid(0, 0, 0, result.row, result.column, result.angle, out hv_HomMat2D);
                        HOperatorSet.AffineTransRegion(modelContours, out modelContours, hv_HomMat2D, "nearest_neighbor");
                    }
                    break;
                case ModelType.ShapeModel:
                case ModelType.ScaledShapeModel:
                    {
                        HTuple hv_HomMat2D = new HTuple();
                        HOperatorSet.GetShapeModelContours(out modelContours, modelID, level);
                        HOperatorSet.VectorAngleToRigid(0, 0, 0, result.row, result.column, result.angle, out hv_HomMat2D);
                        HOperatorSet.AffineTransContourXld(modelContours, out modelContours, hv_HomMat2D);
                    }
                    break;
                case ModelType.GenericShapeModel:
                    {
                        HOperatorSet.GetGenericShapeModelResultObject(out modelContours, result.ResultID, "all", "contours");
                    }
                    break;
                default:
                    modelContours = new HObject();
                    break;
            }
        }


       
    }
}
