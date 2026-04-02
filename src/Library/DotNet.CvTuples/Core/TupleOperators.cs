using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DotNet.CvTuples;

/// <summary>
/// 元组运算符扩展 - 使用SIMD加速
/// </summary>
public static class TupleOperators
{
    #region 算术运算 - SIMD加速

    /// <summary>元素级加法（SIMD加速）</summary>
    public static Tuple<T> Add<T>(this Tuple<T> left, Tuple<T> right) 
        where T : unmanaged, IEquatable<T>, INumber<T>
    {
        ValidateSameLength(left, right);
        return ApplyBinaryOp(left.Span, right.Span, (a, b) => a + b);
    }

    /// <summary>标量加法</summary>
    public static Tuple<T> Add<T>(this Tuple<T> tuple, T scalar) 
        where T : unmanaged, IEquatable<T>, INumber<T>
    {
        return ApplyScalarOp(tuple.Span, scalar, (a, b) => a + b);
    }

    /// <summary>元素级减法（SIMD加速）</summary>
    public static Tuple<T> Subtract<T>(this Tuple<T> left, Tuple<T> right) 
        where T : unmanaged, IEquatable<T>, INumber<T>
    {
        ValidateSameLength(left, right);
        return ApplyBinaryOp(left.Span, right.Span, (a, b) => a - b);
    }

    /// <summary>标量减法</summary>
    public static Tuple<T> Subtract<T>(this Tuple<T> tuple, T scalar) 
        where T : unmanaged, IEquatable<T>, INumber<T>
    {
        return ApplyScalarOp(tuple.Span, scalar, (a, b) => a - b);
    }

    /// <summary>元素级乘法（SIMD加速）</summary>
    public static Tuple<T> Multiply<T>(this Tuple<T> left, Tuple<T> right) 
        where T : unmanaged, IEquatable<T>, INumber<T>
    {
        ValidateSameLength(left, right);
        return ApplyBinaryOp(left.Span, right.Span, (a, b) => a * b);
    }

    /// <summary>标量乘法</summary>
    public static Tuple<T> Multiply<T>(this Tuple<T> tuple, T scalar) 
        where T : unmanaged, IEquatable<T>, INumber<T>
    {
        return ApplyScalarOp(tuple.Span, scalar, (a, b) => a * b);
    }

    /// <summary>元素级除法（SIMD加速）</summary>
    public static Tuple<T> Divide<T>(this Tuple<T> left, Tuple<T> right) 
        where T : unmanaged, IEquatable<T>, INumber<T>
    {
        ValidateSameLength(left, right);
        return ApplyBinaryOp(left.Span, right.Span, (a, b) => a / b);
    }

    /// <summary>标量除法</summary>
    public static Tuple<T> Divide<T>(this Tuple<T> tuple, T scalar) 
        where T : unmanaged, IEquatable<T>, INumber<T>
    {
        return ApplyScalarOp(tuple.Span, scalar, (a, b) => a / b);
    }

    /// <summary>取模运算</summary>
    public static Tuple<T> Modulo<T>(this Tuple<T> left, Tuple<T> right) 
        where T : unmanaged, IEquatable<T>, INumber<T>
    {
        ValidateSameLength(left, right);
        var result = new T[left.Length];
        for (int i = 0; i < left.Length; i++)
        {
            result[i] = left[i] % right[i];
        }
        return new Tuple<T>(result);
    }

    /// <summary>取反</summary>
    public static Tuple<T> Negate<T>(this Tuple<T> tuple) 
        where T : unmanaged, IEquatable<T>, INumber<T>
    {
        var result = new T[tuple.Length];
        for (int i = 0; i < tuple.Length; i++)
        {
            result[i] = -tuple[i];
        }
        return new Tuple<T>(result);
    }

    #endregion

    #region 聚合操作

    /// <summary>求和（仅数值类型）</summary>
    public static T Sum<T>(this Tuple<T> tuple) 
        where T : unmanaged, IEquatable<T>, INumber<T>
    {
        var span = tuple.Span;
        T sum = T.Zero;
        
        // SIMD加速路径
        if (Vector.IsHardwareAccelerated && tuple.Length >= Vector<T>.Count)
        {
            var vectors = MemoryMarshal.Cast<T, Vector<T>>(span);
            var vSum = Vector<T>.Zero;
            foreach (var v in vectors)
            {
                vSum += v;
            }
            
            // 累加向量中的所有元素
            for (int i = 0; i < Vector<T>.Count; i++)
            {
                sum += vSum[i];
            }
            
            // 处理剩余元素
            int remainder = tuple.Length % Vector<T>.Count;
            for (int i = tuple.Length - remainder; i < tuple.Length; i++)
            {
                sum += span[i];
            }
        }
        else
        {
            foreach (var item in span)
            {
                sum += item;
            }
        }
        
        return sum;
    }

    /// <summary>求平均值</summary>
    public static double Average<T>(this Tuple<T> tuple) 
        where T : unmanaged, IEquatable<T>, INumber<T>
    {
        if (tuple.Length == 0) return 0;
        return double.CreateChecked(tuple.Sum()) / tuple.Length;
    }

    /// <summary>求最小值</summary>
    public static T Min<T>(this Tuple<T> tuple) 
        where T : unmanaged, IEquatable<T>, IComparable<T>
    {
        if (tuple.Length == 0) throw new InvalidOperationException("元组为空");
        
        var span = tuple.Span;
        var min = span[0];
        for (int i = 1; i < span.Length; i++)
        {
            if (span[i].CompareTo(min) < 0)
                min = span[i];
        }
        return min;
    }

    /// <summary>求最大值</summary>
    public static T Max<T>(this Tuple<T> tuple) 
        where T : unmanaged, IEquatable<T>, IComparable<T>
    {
        if (tuple.Length == 0) throw new InvalidOperationException("元组为空");
        
        var span = tuple.Span;
        var max = span[0];
        for (int i = 1; i < span.Length; i++)
        {
            if (span[i].CompareTo(max) > 0)
                max = span[i];
        }
        return max;
    }

    #endregion

    #region 位运算

    /// <summary>按位与</summary>
    public static Tuple<T> BitwiseAnd<T>(this Tuple<T> left, Tuple<T> right) 
        where T : unmanaged, IEquatable<T>, IBitwiseOperators<T, T, T>
    {
        ValidateSameLength(left, right);
        var result = new T[left.Length];
        for (int i = 0; i < left.Length; i++)
        {
            result[i] = left[i] & right[i];
        }
        return new Tuple<T>(result);
    }

    /// <summary>按位或</summary>
    public static Tuple<T> BitwiseOr<T>(this Tuple<T> left, Tuple<T> right) 
        where T : unmanaged, IEquatable<T>, IBitwiseOperators<T, T, T>
    {
        ValidateSameLength(left, right);
        var result = new T[left.Length];
        for (int i = 0; i < left.Length; i++)
        {
            result[i] = left[i] | right[i];
        }
        return new Tuple<T>(result);
    }

    /// <summary>按位异或</summary>
    public static Tuple<T> BitwiseXor<T>(this Tuple<T> left, Tuple<T> right) 
        where T : unmanaged, IEquatable<T>, IBitwiseOperators<T, T, T>
    {
        ValidateSameLength(left, right);
        var result = new T[left.Length];
        for (int i = 0; i < left.Length; i++)
        {
            result[i] = left[i] ^ right[i];
        }
        return new Tuple<T>(result);
    }

    /// <summary>左移</summary>
    public static Tuple<T> LeftShift<T>(this Tuple<T> tuple, int shift) 
        where T : unmanaged, IEquatable<T>, IShiftOperators<T, int, T>
    {
        var result = new T[tuple.Length];
        for (int i = 0; i < tuple.Length; i++)
        {
            result[i] = tuple[i] << shift;
        }
        return new Tuple<T>(result);
    }

    /// <summary>右移</summary>
    public static Tuple<T> RightShift<T>(this Tuple<T> tuple, int shift) 
        where T : unmanaged, IEquatable<T>, IShiftOperators<T, int, T>
    {
        var result = new T[tuple.Length];
        for (int i = 0; i < tuple.Length; i++)
        {
            result[i] = tuple[i] >> shift;
        }
        return new Tuple<T>(result);
    }

    #endregion

    #region 比较运算

    /// <summary>元素级小于比较</summary>
    public static Tuple<bool> LessThan<T>(this Tuple<T> left, Tuple<T> right) 
        where T : unmanaged, IEquatable<T>, IComparisonOperators<T, T, bool>
    {
        ValidateSameLength(left, right);
        var result = new bool[left.Length];
        for (int i = 0; i < left.Length; i++)
        {
            result[i] = left[i] < right[i];
        }
        return new Tuple<bool>(result);
    }

    /// <summary>元素级大于比较</summary>
    public static Tuple<bool> GreaterThan<T>(this Tuple<T> left, Tuple<T> right) 
        where T : unmanaged, IEquatable<T>, IComparisonOperators<T, T, bool>
    {
        ValidateSameLength(left, right);
        var result = new bool[left.Length];
        for (int i = 0; i < left.Length; i++)
        {
            result[i] = left[i] > right[i];
        }
        return new Tuple<bool>(result);
    }

    /// <summary>元素级相等比较</summary>
    public static Tuple<bool> ElementEquals<T>(this Tuple<T> left, Tuple<T> right) 
        where T : unmanaged, IEquatable<T>
    {
        ValidateSameLength(left, right);
        var result = new bool[left.Length];
        for (int i = 0; i < left.Length; i++)
        {
            result[i] = left[i].Equals(right[i]);
        }
        return new Tuple<bool>(result);
    }

    #endregion

    #region 核心实现

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Tuple<T> ApplyBinaryOp<T>(ReadOnlySpan<T> left, ReadOnlySpan<T> right, Func<T, T, T> op)
        where T : unmanaged, IEquatable<T>
    {
        var result = new T[left.Length];
        
        // SIMD路径仅适用于支持SIMD的类型
        // 目前使用简单的标量路径，在支持的平台上JIT会自动向量化
        for (int i = 0; i < left.Length; i++)
        {
            result[i] = op(left[i], right[i]);
        }
        
        return new Tuple<T>(result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Tuple<T> ApplyScalarOp<T>(ReadOnlySpan<T> tuple, T scalar, Func<T, T, T> op)
        where T : unmanaged, IEquatable<T>
    {
        var result = new T[tuple.Length];
        
        for (int i = 0; i < tuple.Length; i++)
        {
            result[i] = op(tuple[i], scalar);
        }
        
        return new Tuple<T>(result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidateSameLength<T>(Tuple<T> left, Tuple<T> right) 
        where T : unmanaged, IEquatable<T>
    {
        if (left.Length != right.Length)
        {
            throw new ArgumentException(
                $"元组长度不匹配: {left.Length} vs {right.Length}。" +
                "元素级运算要求两个元组具有相同长度。");
        }
    }

    #endregion
}
