namespace DotNet.Vision.Abstractions
{
    public enum AlgoEnum
    {
        /// <summary> 未定义 </summary>
        Undefined,

        /// <summary> 创建ROI </summary>
        CreateROI,

        /// <summary> 文件图像 </summary>
        FileImage,

        /// <summary> 旋转图像 </summary>
        RotateImage,

        /// <summary> 直线旋转图像 </summary>
        LineRotImage,

        /// <summary> 拟合直线 </summary>
        FitLine,

        /// <summary> 圆弧中点 </summary>
        FitArcMidpoint,

        /// <summary> 合并区域 </summary>
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
