using DotNet.Drawing;
using Newtonsoft.Json;


namespace DotNet.VisionMaster
{
    public struct ModelResult
    {
        /// <summary> 行 </summary>
        public double Row { get; set; }

        /// <summary> 列 </summary>
        public double Column { get; set; }

        /// <summary> 角度 </summary>
        public double Angle { get; set; }

        /// <summary> 分数 </summary>
        public double Score { get; set; }

        public ModelResult(double row, double column, double angle, double score)
        {
            Row = row;
            Column = column;
            Angle = angle;
            Score = score;
        }

        [JsonIgnore]
        public double X { get { return Column; } }
        [JsonIgnore]
        public double Y { get { return Row; } }
        [JsonIgnore]
        public double ToDegrees { get { return Angle.ToDegrees(); } }
        [JsonIgnore]
        public CvCoord Coord { get { return new CvCoord(Column, Row, Angle); } }

    }
}
