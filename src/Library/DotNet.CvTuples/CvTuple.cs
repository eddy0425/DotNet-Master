using System.Runtime.CompilerServices;

namespace DotNet.CvTuples;

/// <summary>
/// CvTuple 统一入口 - 提供工厂方法和类型别名
/// 这是用户主要使用的API入口点
/// </summary>
public static class CvTuple
{
    /// <summary>64位平台标志</summary>
    public static readonly bool IsPlatform64 = IntPtr.Size > 4;

    #region 工厂方法 - 创建同质元组

    /// <summary>创建空元组</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Tuple<int> Empty() => new();

    /// <summary>从布尔值创建</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Tuple<bool> Create(bool value) => new(value);

    /// <summary>从布尔数组创建</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Tuple<bool> Create(params bool[] values) => new(values);

    /// <summary>从UInt16创建</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Tuple<ushort> Create(ushort value) => new(value);

    /// <summary>从UInt16数组创建</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Tuple<ushort> Create(params ushort[] values) => new(values);

    /// <summary>从Int32创建</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Tuple<int> Create(int value) => new(value);

    /// <summary>从Int32数组创建</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Tuple<int> Create(params int[] values) => new(values);

    /// <summary>从Int64创建</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Tuple<long> Create(long value) => new(value);

    /// <summary>从Int64数组创建</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Tuple<long> Create(params long[] values) => new(values);

    /// <summary>从Float创建</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Tuple<float> Create(float value) => new(value);

    /// <summary>从Float数组创建</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Tuple<float> Create(params float[] values) => new(values);

    /// <summary>从Double创建</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Tuple<double> Create(double value) => new(value);

    /// <summary>从Double数组创建</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Tuple<double> Create(params double[] values) => new(values);

    /// <summary>从IntPtr创建</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Tuple<nint> Create(nint value) => new(value);

    /// <summary>从IntPtr数组创建</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Tuple<nint> Create(params nint[] values) => new(values);

    #endregion

    #region 工厂方法 - 创建混合元组

    /// <summary>创建混合类型元组</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MixedTuple CreateMixed(params object?[] values) => new(values);

    /// <summary>创建混合类型元组（从TupleValue）</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static MixedTuple CreateMixed(params TupleValue[] values) => new(values);

    #endregion

    #region 工厂方法 - 生成序列

    /// <summary>创建整数序列 [start, start+count)</summary>
    public static Tuple<int> Range(int start, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return new Tuple<int>(count, i => start + i);
    }

    /// <summary>创建整数序列 [0, count)</summary>
    public static Tuple<int> Range(int count) => Range(0, count);

    /// <summary>创建重复值序列</summary>
    public static Tuple<T> Repeat<T>(T value, int count) where T : unmanaged, IEquatable<T>
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var data = new T[count];
        Array.Fill(data, value);
        return new Tuple<T>(data);
    }

    /// <summary>创建线性空间序列</summary>
    public static Tuple<double> LinSpace(double start, double end, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count == 0) return new Tuple<double>();
        if (count == 1) return new Tuple<double>(start);
        
        double step = (end - start) / (count - 1);
        return new Tuple<double>(count, i => start + i * step);
    }

    /// <summary>创建全零元组</summary>
    public static Tuple<T> Zeros<T>(int length) where T : unmanaged, IEquatable<T>
    {
        return new Tuple<T>(new T[length]);
    }

    /// <summary>创建全一元组</summary>
    public static Tuple<int> Ones(int length) => Repeat(1, length);

    /// <summary>创建全一元组（Double）</summary>
    public static Tuple<double> OnesDouble(int length) => Repeat(1.0, length);

    #endregion

    #region 连接操作

    /// <summary>连接多个元组</summary>
    public static Tuple<T> Concat<T>(params Tuple<T>[] tuples) where T : unmanaged, IEquatable<T>
    {
        if (tuples.Length == 0) return new Tuple<T>();
        if (tuples.Length == 1) return tuples[0];
        
        int totalLength = 0;
        foreach (var t in tuples) totalLength += t.Length;
        
        var result = new T[totalLength];
        int offset = 0;
        foreach (var t in tuples)
        {
            t.Span.CopyTo(result.AsSpan(offset));
            offset += t.Length;
        }
        
        return new Tuple<T>(result);
    }

    /// <summary>连接多个混合元组</summary>
    public static MixedTuple Concat(params MixedTuple[] tuples)
    {
        if (tuples.Length == 0) return new MixedTuple();
        if (tuples.Length == 1) return tuples[0];
        
        var result = tuples[0];
        for (int i = 1; i < tuples.Length; i++)
        {
            result = result.Concat(tuples[i]);
        }
        return result;
    }

    #endregion

    #region 类型转换

    /// <summary>从同质元组转换为混合元组</summary>
    public static MixedTuple ToMixed<T>(Tuple<T> tuple) where T : unmanaged, IEquatable<T>
    {
        return MixedTuple.FromTuple(tuple);
    }

    /// <summary>尝试将混合元组转换为同质元组</summary>
    public static Tuple<T>? TryFromMixed<T>(MixedTuple mixed) where T : unmanaged, IEquatable<T>
    {
        return mixed.TryToTuple<T>();
    }

    #endregion
}

#region 类型别名（便捷使用）- 使用组合而非继承

/// <summary>布尔元组包装器</summary>
public static class BoolTuple
{
    public static Tuple<bool> Create(bool value) => new(value);
    public static Tuple<bool> Create(params bool[] values) => new(values);
    public static Tuple<bool> Empty() => new();
}

/// <summary>Int32元组包装器</summary>
public static class IntTuple
{
    public static Tuple<int> Create(int value) => new(value);
    public static Tuple<int> Create(params int[] values) => new(values);
    public static Tuple<int> Empty() => new();
    public static Tuple<int> Range(int count) => CvTuple.Range(count);
    public static Tuple<int> Range(int start, int count) => CvTuple.Range(start, count);
}

/// <summary>Int64元组包装器</summary>
public static class LongTuple
{
    public static Tuple<long> Create(long value) => new(value);
    public static Tuple<long> Create(params long[] values) => new(values);
    public static Tuple<long> Empty() => new();
}

/// <summary>Float元组包装器</summary>
public static class FloatTuple
{
    public static Tuple<float> Create(float value) => new(value);
    public static Tuple<float> Create(params float[] values) => new(values);
    public static Tuple<float> Empty() => new();
}

/// <summary>Double元组包装器</summary>
public static class DoubleTuple
{
    public static Tuple<double> Create(double value) => new(value);
    public static Tuple<double> Create(params double[] values) => new(values);
    public static Tuple<double> Empty() => new();
    public static Tuple<double> LinSpace(double start, double end, int count) => CvTuple.LinSpace(start, end, count);
}

#endregion
