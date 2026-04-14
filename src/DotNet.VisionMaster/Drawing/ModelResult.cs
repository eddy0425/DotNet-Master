using DotNet.Drawing;
using HalconDotNet;
using Newtonsoft.Json;


namespace DotNet.VisionMaster
{
    public struct ModelResult
    {
        /// <summary> 行 </summary>
        public HTuple Row { get; set; }

        /// <summary> 列 </summary>
        public HTuple Column { get; set; }

        /// <summary> 角度 </summary>
        public HTuple Angle { get; set; }

        /// <summary> 分数 </summary>
        public HTuple Score { get; set; }

        public ModelResult( HTuple row,  HTuple column,  HTuple angle,  HTuple score)
        {
            Row = row;
            Column = column;
            Angle = angle;
            Score = score;
        }

        [JsonIgnore]
        public double X { get { return Column.D; } }
        [JsonIgnore]
        public double Y { get { return Row.D; } }
        [JsonIgnore]
        public double ToAngle { get { return Angle.D.ToDegrees(); } }
        [JsonIgnore]
        public CvCoord coord { get { return new CvCoord(Column.D, Row.D, Angle.D); } }
        [JsonIgnore]
        public CvCoord coordAngle { get { return new CvCoord(Column.D, Row.D, Angle.D.ToDegrees()); } }

    }
}
