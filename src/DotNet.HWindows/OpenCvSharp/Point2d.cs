using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenCvSharp
{
    [StructLayout(LayoutKind.Sequential)]
    public class Point2d : IEquatable<Point2d>
    {
        public static readonly Point2d Empty = new Point2d();
        public static readonly Point2d[] Array1D = new Point2d[0];
        public static readonly Point2d[][] Array2D = new Point2d[0][];

        /// <summary>
        /// 
        /// </summary>
        public double X;

        /// <summary>
        /// 
        /// </summary>
        public double Y;

        /// <summary>
        /// 
        /// </summary>
        public const int SizeOf = sizeof (double) + sizeof (double);
        
        public Point2d()
        {
            X = 0;
            Y = 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public Point2d(double x, double y)
        {
            X = x;
            Y = y;
        }

        #region Cast

        /// <summary>
        /// 
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static explicit operator Point(Point2d self)
        {
            return new Point((int) self.X, (int) self.Y);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static implicit operator Point2d(Point point)
        {
            return new Point2d(point.X, point.Y);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public static implicit operator Vec2d(Point2d point)
        {
            return new Vec2d(point.X, point.Y);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static implicit operator Point2d(Vec2d vec)
        {
            return new Point2d(vec.Item0, vec.Item1);
        }

        #endregion

        #region Operators

        #region == / !=

#if LANG_JP
    /// <summary>
    /// 指定したオブジェクトと等しければtrueを返す 
    /// </summary>
    /// <param name="obj">比較するオブジェクト</param>
    /// <returns>型が同じで、メンバの値が等しければtrue</returns>
#else
        /// <summary>
        /// Specifies whether this object contains the same members as the specified Object.
        /// </summary>
        /// <param name="obj">The Object to test.</param>
        /// <returns>This method returns true if obj is the same type as this object and has the same members as this object.</returns>
#endif
        public bool Equals(Point2d obj)
        {
            return (this.X == obj.X && this.Y == obj.Y);
        }

#if LANG_JP
    /// <summary>
    /// == 演算子のオーバーロード。x,y座標値が等しければtrueを返す 
    /// </summary>
    /// <param name="lhs">左辺値</param>
    /// <param name="rhs">右辺値</param>
    /// <returns>等しければtrue</returns>
#else
        /// <summary>
        /// Compares two CvPoint objects. The result specifies whether the values of the X and Y properties of the two CvPoint objects are equal.
        /// </summary>
        /// <param name="lhs">A Point to compare.</param>
        /// <param name="rhs">A Point to compare.</param>
        /// <returns>This operator returns true if the X and Y values of left and right are equal; otherwise, false.</returns>
#endif
        public static bool operator ==(Point2d lhs, Point2d rhs)
        {
            //return lhs.Equals(rhs);
            if (ReferenceEquals(lhs, rhs))
            {
                return true; // 如果两个引用指向同一个对象，返回 true
            }

            if (lhs is null || rhs is null)
            {
                return false; // 如果其中一个是 null 而另一个不是，返回 false
            }

            return lhs.Equals(rhs); // 如果两个对象都不为 null，调用 Equals 方法比较
        }

#if LANG_JP
    /// <summary>
    /// != 演算子のオーバーロード。x,y座標値が等しくなければtrueを返す 
    /// </summary>
    /// <param name="lhs">左辺値</param>
    /// <param name="rhs">右辺値</param>
    /// <returns>等しくなければtrue</returns>
#else
        /// <summary>
        /// Compares two CvPoint2D32f objects. The result specifies whether the values of the X or Y properties of the two CvPoint2D32f objects are unequal.
        /// </summary>
        /// <param name="lhs">A Point to compare.</param>
        /// <param name="rhs">A Point to compare.</param>
        /// <returns>This operator returns true if the values of either the X properties or the Y properties of left and right differ; otherwise, false.</returns>
#endif
        public static bool operator !=(Point2d lhs, Point2d rhs)
        {
            //return !lhs.Equals(rhs);
            if (ReferenceEquals(lhs, rhs))
            {
                return false; // 如果两个引用指向同一个对象（包括同时为 null），返回 false
            }

            if (lhs is null || rhs is null)
            {
                return true; // 如果其中一个是 null 而另一个不是，返回 true
            }

            return !lhs.Equals(rhs); // 如果两个对象都不为 null，调用 Equals 方法进行内容比较并取反
        }

        #endregion

        #region + / -

#if LANG_JP
    /// <summary>
    /// 単項プラス演算子
    /// </summary>
    /// <param name="pt"></param>
    /// <returns></returns>
#else
        /// <summary>
        /// Unary plus operator
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
#endif
        public static Point2d operator +(Point2d pt)
        {
            if (pt is null)
            {
                throw new ArgumentNullException(nameof(pt), "The point cannot be null.");
            }
            return pt;
        }

#if LANG_JP
    /// <summary>
    /// 単項マイナス演算子
    /// </summary>
    /// <param name="pt"></param>
    /// <returns></returns>
#else
        /// <summary>
        /// Unary minus operator
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
#endif
        public static Point2d operator -(Point2d pt)
        {
            if (pt is null)
            {
                throw new ArgumentNullException(nameof(pt), "The point cannot be null.");
            }
            return new Point2d(-pt.X, -pt.Y);
        }

#if LANG_JP
    /// <summary>
    /// あるオフセットで点を移動させる
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <returns></returns>
#else
        /// <summary>
        /// Shifts point by a certain offset
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
#endif
        public static Point2d operator +(Point2d p1, Point2d p2)
        {
            if (p1 is null)
            {
                throw new ArgumentNullException(nameof(p1), "The first point cannot be null.");
            }

            if (p2 is null)
            {
                throw new ArgumentNullException(nameof(p2), "The second point cannot be null.");
            }

            return new Point2d(p1.X + p2.X, p1.Y + p2.Y);
            //return new Point2d(p1.X + p2.X, p1.Y + p2.Y);
        }

#if LANG_JP
    /// <summary>
    /// あるオフセットで点を移動させる
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <returns></returns>
#else
        /// <summary>
        /// Shifts point by a certain offset
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
#endif
        public static Point2d operator -(Point2d p1, Point2d p2)
        {
            if (p1 is null)
            {
                throw new ArgumentNullException(nameof(p1), "The first point cannot be null.");
            }

            if (p2 is null)
            {
                throw new ArgumentNullException(nameof(p2), "The second point cannot be null.");
            }

            return new Point2d(p1.X - p2.X, p1.Y - p2.Y);
        }

#if LANG_JP
    /// <summary>
    /// あるオフセットで点を移動させる
    /// </summary>
    /// <param name="pt"></param>
    /// <param name="scale"></param>
    /// <returns></returns>
#else
        /// <summary>
        /// Shifts point by a certain offset
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
#endif
        public static Point2d operator *(Point2d pt, double scale)
        {
            if (pt is null)
            {
                throw new ArgumentNullException(nameof(pt), "The point cannot be null.");
            }

            return new Point2d((float) (pt.X*scale), (float) (pt.Y*scale));
        }

        #endregion

        #endregion

        #region Override

#if LANG_JP
    /// <summary>
    /// Equalsのオーバーライド
    /// </summary>
    /// <param name="obj">比較するオブジェクト</param>
    /// <returns></returns>
#else
        /// <summary>
        /// Specifies whether this object contains the same members as the specified Object.
        /// </summary>
        /// <param name="obj">The Object to test.</param>
        /// <returns>This method returns true if obj is the same type as this object and has the same members as this object.</returns>
#endif
        public override bool Equals(object obj)
        {
            if (obj is Point2d other)
            {
                return this.X == other.X && this.Y == other.Y;
            }

            return false;
            //return base.Equals(obj);
        }

#if LANG_JP
    /// <summary>
    /// GetHashCodeのオーバーライド
    /// </summary>
    /// <returns>このオブジェクトのハッシュ値を指定する整数値。</returns>
#else
        /// <summary>
        /// Returns a hash code for this object.
        /// </summary>
        /// <returns>An integer value that specifies a hash value for this object.</returns>
#endif
        public override int GetHashCode()
        {
            unchecked // 防止溢出，但继续计算
            {
                int hash = 17;
                hash = hash * 31 + X.GetHashCode();
                hash = hash * 31 + Y.GetHashCode();
                return hash;
            }
        }

        //public override int GetHashCode()
        //{
        //    return X.GetHashCode() ^ Y.GetHashCode();
        //}

#if LANG_JP
    /// <summary>
    /// 文字列形式を返す 
    /// </summary>
    /// <returns>文字列形式</returns>
#else
        /// <summary>
        /// Converts this object to a human readable string.
        /// </summary>
        /// <returns>A string that represents this object.</returns>
#endif
        public override string ToString()
        {
            return $"X:{X.ToString("F2")} Y:{Y.ToString("F2")}";
        }

        #endregion

        #region Methods

#if LANG_JP
    /// <summary>
    /// 2点間の距離を求める
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <returns></returns>
#else
        /// <summary>
        /// Returns the distance between the specified two points
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
#endif
        public static double Distance(Point2d p1, Point2d p2)
        {
            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            return Math.Sqrt(dx * dx + dy * dy);
            //return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

#if LANG_JP
    /// <summary>
    /// 2点間の距離を求める
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
#else
        /// <summary>
        /// Returns the distance between the specified two points
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
#endif
        public double DistanceTo(Point2d p)
        {
            return Distance(this, p);
        }

#if LANG_JP
    /// <summary>
    /// ベクトルの内積を求める
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <returns></returns>
#else
        /// <summary>
        /// Calculates the dot product of two 2D vectors.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
#endif
        public static double DotProduct(Point2d p1, Point2d p2)
        {
            return p1.X*p2.X + p1.Y*p2.Y;
        }

#if LANG_JP
    /// <summary>
    /// ベクトルの内積を求める
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
#else
        /// <summary>
        /// Calculates the dot product of two 2D vectors.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
#endif
        public double DotProduct(Point2d p)
        {
            return DotProduct(this, p);
        }

#if LANG_JP
    /// <summary>
    /// ベクトルの外積を求める
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <returns></returns>
#else
        /// <summary>
        /// Calculates the cross product of two 2D vectors.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
#endif
        public static double CrossProduct(Point2d p1, Point2d p2)
        {
            return p1.X*p2.Y - p2.X*p1.Y;
        }

#if LANG_JP
    /// <summary>
    /// ベクトルの外積を求める
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
#else
        /// <summary>
        /// Calculates the cross product of two 2D vectors.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
#endif
        public double CrossProduct(Point2d p)
        {
            return CrossProduct(this, p);
        }

        #endregion
    }
}
