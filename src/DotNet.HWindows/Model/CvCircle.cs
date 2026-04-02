using OpenCvSharp;
using System;

namespace DotNet.HWindows
{
    /// <summary>
    /// 圆
    /// </summary>
    public class CvCircle : ICloneable, IEquatable<CvCircle>
    {
        /// <summary>
        /// 圆心
        /// </summary>
        public Point2d center;
        public double X { get { return center.X; } set { center.X = value; } }
        public double Y { get { return center.Y; } set { center.Y = value; } }
        public Point2d Center { get { return center; } }

        /// <summary>
        /// 半径
        /// </summary>
        public double radius;

        /// <summary>
        /// 开始角度
        /// </summary>
        public double StartPhi;

        /// <summary>
        /// 结束角度
        /// </summary>
        public double EndPhi;

        /// <summary>
        /// 三点构成圆
        /// </summary>
        public Point2d[] points = new Point2d[3];

        public static readonly CvCircle Empty = new CvCircle();
        public CvCircle()
        {
            center = new Point2d();
            radius = 0;
        }

        public CvCircle(double _X, double _Y, double _radius)
        {
            center = new Point2d(_X, _Y);
            radius = _radius;
        }
        public CvCircle(double _X, double _Y, double _radius, double _startPhi, double _endPhi)
        {
            center = new Point2d(_X, _Y);
            radius = _radius;
            StartPhi = _startPhi;
            EndPhi = _endPhi;
        }

        public CvCircle(Point2d StartPoint, Point2d EndPoint)
        {
            double deltaX = StartPoint.X - EndPoint.X;
            double deltaY = StartPoint.Y - EndPoint.Y;

            center = new Point2d(StartPoint.X, StartPoint.Y);
            radius = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        public void UpdataCenter(Point2d StartPoint)
        {
            center = new Point2d(StartPoint.X, StartPoint.Y);
        }

        public void UpdataCenter(double _X, double _Y)
        {
            center = new Point2d(_X, _Y);
        }

        public override string ToString()
        {
            return $"圆心:{center.X.ToString("F2")},{center.Y.ToString("F2")} 半径:{radius.ToString("F2")}";
        }

        object ICloneable.Clone() => (object)this.Clone();
        public CvCircle Clone()
        {
            return TransExpV2<CvCircle, CvCircle>.Trans(this);
        }

        #region == / !=

        public override int GetHashCode()
        {
            // 使用各个重要字段的哈希码生成最终的哈希码
            int hash = 17;
            hash = hash * 31 + center.X.GetHashCode();
            hash = hash * 31 + center.Y.GetHashCode();
            hash = hash * 31 + radius.GetHashCode();
            hash = hash * 31 + StartPhi.GetHashCode();
            hash = hash * 31 + EndPhi.GetHashCode();
            return hash;
        }

        public bool Equals(CvCircle obj)
        {
            if (obj == null)
                return false;

            return (this.center.X == obj.center.X && this.center.Y == obj.center.Y &&
                    this.radius == obj.radius && this.StartPhi == obj.StartPhi && this.EndPhi == obj.EndPhi);
        }

        public static bool operator ==(CvCircle lhs, CvCircle rhs)
        {
            if (ReferenceEquals(lhs, null))
                return ReferenceEquals(rhs, null);

            return lhs.Equals(rhs);
        }

        public static bool operator !=(CvCircle lhs, CvCircle rhs)
        {
            if (ReferenceEquals(lhs, null))
                return !ReferenceEquals(rhs, null);

            return !lhs.Equals(rhs);
        }

       
        #endregion
    }
}
