using HalconDotNet;

namespace DotNet.HWindows
{
    public class ModelInfo
    {
        /// <summary>
        /// 锁定中心
        /// </summary>
        public bool LockCenter { get; set; } = true;
        /// <summary>
        /// 模板ID
        /// </summary>
        public HTuple modelID { get; set; }
        /// <summary>
        /// 起始角度
        /// </summary>
        public HTuple angleStart { get; set; } = -90;

        /// <summary>
        /// 终点角度
        /// </summary>
        public HTuple angleEnd { get; set; } = 180;

        /// <summary>
        /// 增量角度
        /// </summary>
        public HTuple angleExtent { get; set; } = 180;
        /// <summary>
        /// 得分
        /// </summary>
        public HTuple minScore { get; set; } = 0.6;
        /// <summary>
        /// 匹配数量
        /// </summary>
        public HTuple numMatches { get; set; } = 1;
        /// <summary>
        /// 最大重叠率
        /// </summary>
        public HTuple maxOverlap { get; set; } = 0.5;
        /// <summary>
        /// 子像素： 如果不等于“none”，则为亚像素精度。默认值：“最小二乘”
        /// </summary>
        public HTuple subPixel { get; set; } = "true";
        /// <summary>
        /// 金字塔层数
        /// </summary>
        public HTuple numLevels { get; set; } = 0;
        /// <summary>
        /// 贪婪系数 (形状匹配、缩放匹配)  
        /// 搜索启发式的“贪婪”（0：安全但缓慢；1：快速但匹配可能错过）。默认值：0.9
        /// </summary>
        public HTuple greediness { get; set; } = 0.7;
        /// <summary>
        /// 最小缩放 (缩放匹配)
        /// </summary>
        public HTuple scaleMin { get; set; } = 0.8;
        /// <summary>
        /// 最大缩放 (缩放匹配)
        /// </summary>
        public HTuple scaleMax { get; set; } = 1.2;

    }
}
