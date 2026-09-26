using System;
using System.Runtime.InteropServices;

namespace DotNet.Drawing
{
    /// <summary>二维浮点尺寸（用于 OpenCV 互操作）</summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Size2f : IEquatable<Size2f>
    {
        /// <summary>宽度</summary>
        public float Width;

        /// <summary>高度</summary>
        public float Height;

        /// <summary>用单精度宽、高构造尺寸。</summary>
        public Size2f(float width, float height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>从双精度宽、高构造尺寸（转换为单精度）。</summary>
        public Size2f(double width, double height) : this((float)width, (float)height) { }

        /// <summary>按宽、高精确比较两个尺寸。</summary>
        public bool Equals(Size2f other) => Width.Equals(other.Width) && Height.Equals(other.Height);
        public override bool Equals(object? obj) => obj is Size2f other && Equals(other);
        public override int GetHashCode() => unchecked(Width.GetHashCode() * 397 ^ Height.GetHashCode());
        public static bool operator ==(Size2f left, Size2f right) => left.Equals(right);
        public static bool operator !=(Size2f left, Size2f right) => !left.Equals(right);
    }
}
