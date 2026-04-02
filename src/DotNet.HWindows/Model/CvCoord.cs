using System;
using OpenCvSharp;

namespace DotNet.HWindows
{
    /// <summary>
    /// 坐标系
    /// </summary>
    public class CvCoord : ICloneable, IEquatable<CvCoord>
    {
        public static CvCoord Empty = new CvCoord();

        /// <summary>
        /// 原点
        /// </summary>
        public Point2d center;
        public double X { get { return center.X; } set { center.X = value; } }
        public double Y { get { return center.Y; } set { center.Y = value; } }

        /// <summary>
        /// 角度
        /// </summary>
        public double angle;

        public CvCoord()
        {
            center = new Point2d();
            angle = 0;
        }

        public CvCoord(Point2d _center)
        {
            center = _center;
            angle = 0;
        }

        public CvCoord(Point2d _center, double _angle)
        {
            center = _center;
            angle = _angle;
        }

        public CvCoord(double _X, double _Y)
        {
            center = new Point2d(_X, _Y);
            angle = 0;
        }

        public CvCoord(double _X, double _Y, double _angle)
        {
            center = new Point2d(_X, _Y);
            angle = _angle;
        }

        public override string ToString()
        {
            return $"X:{X.ToString("F2")} Y:{Y.ToString("F2")} 角度:{angle.ToString("F2")}";
        }

        object ICloneable.Clone() => (object)this.Clone();
        public CvCoord Clone()
        {
            return TransExpV2<CvCoord, CvCoord>.Trans(this);
        }

        #region == / !=
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + center.X.GetHashCode();
            hash = hash * 31 + center.Y.GetHashCode();
            hash = hash * 31 + angle.GetHashCode();
            return hash;
        }

        public bool Equals(CvCoord other)
        {
            if (other == null) return false;

            return this.center.X == other.center.X &&
                   this.center.Y == other.center.Y &&
                   this.angle == other.angle;
        }

        public static bool operator ==(CvCoord lhs, CvCoord rhs)
        {
            if (ReferenceEquals(lhs, null))
                return ReferenceEquals(rhs, null);

            return lhs.Equals(rhs);
        }

        public static bool operator !=(CvCoord lhs, CvCoord rhs)
        {
            if (ReferenceEquals(lhs, null))
                return !ReferenceEquals(rhs, null);

            return !lhs.Equals(rhs);
        }

        public override bool Equals(object obj)
        {
            if (obj is CvCoord coord)
            {
                return Equals(coord);
            }
            return false;
        }

      
        #endregion
    }
}
