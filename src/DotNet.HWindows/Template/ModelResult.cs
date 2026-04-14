using DotNet.Drawing;
using HalconDotNet;
using Newtonsoft.Json;

namespace DotNet.HWindows
{
    public class ModelResult
    {
        public HTuple row { get; set; }
        public HTuple column { get; set; }
        public HTuple angle { get; set; }
        public HTuple scale { get; set; } = 1;
        public HTuple score { get; set; }

        public HTuple ResultID { get; set; }
        public HTuple NumResult { get; set; }

        public ModelResult() { }

        public ModelResult(HTuple _row, HTuple _column, HTuple _angle, HTuple _score)
        {
            row = _row;
            column = _column;
            angle = _angle; 
            score = _score;
        }

        public ModelResult( HTuple _row,  HTuple _column,  HTuple _angle,  HTuple _scale,  HTuple _score) 
        {
            row = _row;
            column = _column;
            angle = _angle;
            scale = _scale;
            score = _score;
        }
        public ModelResult(HTuple _row, HTuple _column, HTuple _angle, HTuple _score, HTuple matchResultID, HTuple numMatchResult)
        {
            row = _row;
            column = _column;
            angle = _angle;
            score = _score;
            ResultID = matchResultID;
            NumResult = numMatchResult;
        }

        public override string ToString()
        {
            return $"X:{column.D.ToString("F2")} Y:{row.D.ToString("F2")} 角度:{angle.D.ToDegrees().ToString("F2")}";
        }

        [JsonIgnore]
        public double X { get { return column.D; } }
        [JsonIgnore]
        public double Y { get { return row.D; } }
        [JsonIgnore]
        public double ToAngle { get { return angle.D.ToDegrees(); } }
        [JsonIgnore]
        public CvCoord coord { get { return new CvCoord(column.D, row.D, angle.D); } }
        [JsonIgnore]
        public CvCoord coordAngle { get { return new CvCoord(column.D, row.D, angle.D.ToDegrees()); } }
    }
}
