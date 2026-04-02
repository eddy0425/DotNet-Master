using System;
using OpenCvSharp;

namespace DotNet.HWindows
{
    /// <summary>
    /// 线
    /// </summary>
    public class CvLine : ICloneable, IEquatable<CvLine>
    {
        /// <summary>
        /// 起点
        /// </summary>
        public Point2d start;

        /// <summary>
        /// 终点
        /// </summary>
        public Point2d end;

        public static readonly CvLine Empty = new CvLine();

        public CvLine()
        {
            start = new Point2d();
            end = new Point2d();
        }
        public CvLine(Point2d _start, Point2d _end)
        {
            start = _start;
            end = _end;
        }

        public CvLine(double _startX, double _startY, double _endX, double _endY)
        {
            start = new Point2d(_startX, _startY);
            end = new Point2d(_endX, _endY);
        }

        public override string ToString()
        {
            return $"起点:{start.X.ToString("F2")},{start.Y.ToString("F2")} 终点:{end.X.ToString("F2")},{end.Y.ToString("F2")}";
        }

        object ICloneable.Clone() => (object)this.Clone();
        public CvLine Clone()
        {
            return TransExpV2<CvLine, CvLine>.Trans(this);
        }

        #region == / !=
        public override int GetHashCode()
        {
            // 使用质数和逐个字段进行哈希计算
            int hash = 17;
            hash = hash * 31 + start.X.GetHashCode();
            hash = hash * 31 + start.Y.GetHashCode();
            hash = hash * 31 + end.X.GetHashCode();
            hash = hash * 31 + end.Y.GetHashCode();
            return hash;
        }

        public bool Equals(CvLine obj)
        {
            if (obj == null) return false;
            return (this.start.X == obj.start.X && this.start.Y == obj.start.Y &&
                    this.end.X == obj.end.X && this.end.Y == obj.end.Y);
        }

        public static bool operator ==(CvLine lhs, CvLine rhs)
        {
            if (ReferenceEquals(lhs, null)) return ReferenceEquals(rhs, null);
            return lhs.Equals(rhs);
        }

        public static bool operator !=(CvLine lhs, CvLine rhs)
        {
            if (ReferenceEquals(lhs, null))
                return !ReferenceEquals(rhs, null); // 如果 lhs 是 null，检查 rhs 是否也是 null
            return !lhs.Equals(rhs); // lhs 不为 null 时，使用 Equals 方法进行比较
        }
        #endregion
    }
}
