using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DotNet.Drawing
{
    /// <summary>
    /// 二维浮点尺寸（用于 OpenCV 互操作）
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Size2f : IEquatable<Size2f>
    {
        /// <summary>
        /// 宽度
        /// </summary>
        public float Width;

        /// <summary>
        /// 高度
        /// </summary>
        public float Height;

        /// <summary>
        /// 构造函数
        /// </summary>
        public Size2f(float width, float height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// 从 double 构造（精度截断为 float）
        /// </summary>
        public Size2f(double width, double height)
        {
            Width = (float)width;
            Height = (float)height;
        }

        #region Operators

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Size2f other)
        {
            return Width == other.Width && Height == other.Height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Size2f lhs, Size2f rhs)
        {
            return lhs.Equals(rhs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Size2f lhs, Size2f rhs)
        {
            return !lhs.Equals(rhs);
        }

        #endregion

        #region Override

        public override bool Equals(object obj)
        {
            return obj is Size2f other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Width, Height);
        }

        public override string ToString()
        {
            return $"({Width}, {Height})";
        }

        #endregion
    }
}
