namespace DotNet.HalconUI
{
    public enum ModelType
    {
        /// <summary> 形状匹配 </summary>
        ShapeModel = 1,

        /// <summary> 灰度匹配 </summary>
        NccModel = 0,

        /// <summary> 缩放匹配 </summary>
        ScaledModel = 2,

        /// <summary> 通用形状匹配 </summary>
        GenericModel = 3,
    }
}
