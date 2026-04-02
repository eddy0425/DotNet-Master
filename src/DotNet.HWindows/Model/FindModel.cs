using System;

namespace DotNet.HWindows
{
    [Serializable]
    public class FindModel
    {
        public FindModel()
        {
            ModelInfo = new ModelInfo();
            FindROI = new DispDRegion(HColor.Blue);
            SetROI = new DispDRegion(HColor.Red);
        }

        public ModelInfo ModelInfo { set; get; }

        /// <summary>
        /// 查找模板区域
        /// </summary>
        public DispDRegion FindROI { set ; get ; }

        /// <summary>
        /// 设置模板区域
        /// </summary>
        public DispDRegion SetROI { set; get; }
    }
}
