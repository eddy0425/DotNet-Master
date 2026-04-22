namespace DotNet.HalconAlgo
{
    public enum AlgoEnum
    {
        /// <summary> 未定义 </summary>
        Undefined,

        /// <summary> 创建ROI </summary>
        CreateROI,

        /// <summary> 文件图像 </summary>
        FileImage,

        /// <summary> 拟合直线 </summary>
        FitLine,

        /// <summary> 圆弧中点 </summary>
        FitArcMidpoint,

        /// <summary> 对象 </summary>
        MergeRegion,

        /// <summary> 形状匹配 </summary>
        ShapeModel,

        /// <summary> 灰度匹配 </summary>
        NccModel,

        /// <summary> 缩放匹配 </summary>
        ScaledModel,

        /// <summary> 通用匹配 </summary>
        GenericModel,
    }
}
