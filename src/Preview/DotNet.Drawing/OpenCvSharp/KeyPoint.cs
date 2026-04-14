using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DotNet.Drawing
{
    /// <summary>
    /// 特征点检测器数据结构
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct KeyPoint : IEquatable<KeyPoint>
    {
        #region Properties

        /// <summary>
        /// 特征点坐标
        /// </summary>
        public Point2f Pt;

        /// <summary>
        /// 特征点大小
        /// </summary>
        public float Size;

        /// <summary>
        /// 特征点方向（度数），未定义时为负值
        /// </summary>
        public float Angle;

        /// <summary>
        /// 特征点强度
        /// </summary>
        public float Response;

        /// <summary>
        /// 所在的尺度空间 octave
        /// </summary>
        public int Octave;

        /// <summary>
        /// 特征点分类ID
        /// </summary>
        public int ClassId;

        #endregion

        #region Constructors

        /// <summary>
        /// 完整构造函数
        /// </summary>
        public KeyPoint(Point2f pt, float size, float angle = -1, float response = 0, int octave = 0,
            int classId = -1)
        {
            Pt = pt;
            Size = size;
            Angle = angle;
            Response = response;
            Octave = octave;
            ClassId = classId;
        }

        /// <summary>
        /// 从坐标构造
        /// </summary>
        public KeyPoint(float x, float y, float size, float angle = -1, float response = 0, int octave = 0,
            int classId = -1)
            : this(new Point2f(x, y), size, angle, response, octave, classId)
        {
        }

        #endregion

        #region Operators

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(KeyPoint other)
        {
            return Pt == other.Pt &&
                   Size == other.Size &&
                   Angle == other.Angle &&
                   Response == other.Response &&
                   Octave == other.Octave &&
                   ClassId == other.ClassId;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(KeyPoint lhs, KeyPoint rhs)
        {
            return lhs.Equals(rhs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(KeyPoint lhs, KeyPoint rhs)
        {
            return !lhs.Equals(rhs);
        }

        #endregion

        #region Override

        public override bool Equals(object obj)
        {
            return obj is KeyPoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Pt, Size, Angle, Response, Octave, ClassId);
        }

        public override string ToString()
        {
            return $"[Pt:{Pt}, Size:{Size}, Angle:{Angle}, Response:{Response}, Octave:{Octave}, ClassId:{ClassId}]";
        }

        #endregion
    }
}
