using System.Collections;
using System.Runtime.CompilerServices;

namespace DotNet.CvTuples;

/// <summary>
/// 混合类型元组 - 支持存储不同类型的元素
/// 使用 TupleValue 结构避免装箱开销
/// </summary>
public sealed class MixedTuple : IEnumerable<TupleValue>, IDisposable, ITuple
{
    private TupleValue[] _data;
    private int _length;

    /// <summary>获取元组长度</summary>
    public int Length => _length;

    /// <summary>获取元组类型（始终返回Mixed）</summary>
    public TupleType Type => TupleType.Mixed;

    /// <summary>获取只读视图</summary>
    public ReadOnlySpan<TupleValue> Span => _data.AsSpan(0, _length);

    #region 构造函数

    /// <summary>创建空的混合元组</summary>
    public MixedTuple()
    {
        _data = [];
        _length = 0;
    }

    /// <summary>创建包含单个值的混合元组</summary>
    public MixedTuple(TupleValue value)
    {
        _data = [value];
        _length = 1;
    }

    /// <summary>创建包含多个值的混合元组</summary>
    public MixedTuple(params TupleValue[] values)
    {
        _data = new TupleValue[values.Length];
        values.CopyTo(_data, 0);
        _length = values.Length;
    }

    /// <summary>从object数组创建</summary>
    public MixedTuple(params object?[] values)
    {
        _data = new TupleValue[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            _data[i] = TupleValue.From(values[i]);
        }
        _length = values.Length;
    }

    /// <summary>从同质元组创建混合元组</summary>
    public static MixedTuple FromTuple<T>(Tuple<T> tuple) where T : unmanaged, IEquatable<T>
    {
        var result = new MixedTuple { _data = new TupleValue[tuple.Length], _length = tuple.Length };
        
        for (int i = 0; i < tuple.Length; i++)
        {
            result._data[i] = TupleValue.From(tuple[i]);
        }
        
        return result;
    }

    /// <summary>创建指定容量的混合元组</summary>
    public MixedTuple(int capacity)
    {
        _data = new TupleValue[capacity];
        _length = 0;
    }

    #endregion

    #region 索引器

    /// <summary>获取或设置指定索引处的元素</summary>
    public ref TupleValue this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)index >= (uint)_length)
                ThrowIndexOutOfRange(index);
            return ref _data[index];
        }
    }

    /// <summary>支持Index类型</summary>
    public ref TupleValue this[Index index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref this[index.GetOffset(_length)];
    }

    /// <summary>支持Range类型切片</summary>
    public MixedTuple this[Range range]
    {
        get
        {
            var (offset, length) = range.GetOffsetAndLength(_length);
            var result = new MixedTuple(length);
            _data.AsSpan(offset, length).CopyTo(result._data);
            result._length = length;
            return result;
        }
    }

    /// <summary>多索引访问</summary>
    public MixedTuple this[params int[] indices]
    {
        get
        {
            var result = new TupleValue[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                var idx = indices[i];
                if ((uint)idx >= (uint)_length)
                    ThrowIndexOutOfRange(idx);
                result[i] = _data[idx];
            }
            return new MixedTuple(result);
        }
    }

    #endregion

    #region ITuple 实现

    object? ITuple.this[int index] => this[index].AsObject;
    int ITuple.Length => _length;

    #endregion

    #region 类型访问器

    /// <summary>获取指定索引的布尔值</summary>
    public bool GetBool(int index) => this[index].AsBool;

    /// <summary>获取指定索引的Int32值</summary>
    public int GetInt32(int index) => this[index].AsInt32;

    /// <summary>获取指定索引的Int64值</summary>
    public long GetInt64(int index) => this[index].AsInt64;

    /// <summary>获取指定索引的Float值</summary>
    public float GetFloat(int index) => this[index].AsFloat;

    /// <summary>获取指定索引的Double值</summary>
    public double GetDouble(int index) => this[index].AsDouble;

    /// <summary>获取指定索引的String值</summary>
    public string GetString(int index) => this[index].AsString;

    /// <summary>设置指定索引的值</summary>
    public void Set(int index, TupleValue value)
    {
        if ((uint)index >= (uint)_length)
            ThrowIndexOutOfRange(index);
        _data[index] = value;
    }

    #endregion

    #region 修改操作

    /// <summary>追加元素</summary>
    public void Append(TupleValue value)
    {
        EnsureCapacity(_length + 1);
        _data[_length++] = value;
    }

    /// <summary>追加多个元素</summary>
    public void Append(ReadOnlySpan<TupleValue> values)
    {
        EnsureCapacity(_length + values.Length);
        values.CopyTo(_data.AsSpan(_length));
        _length += values.Length;
    }

    /// <summary>在指定位置插入元素</summary>
    public void Insert(int index, TupleValue value)
    {
        if ((uint)index > (uint)_length)
            ThrowIndexOutOfRange(index);
        
        EnsureCapacity(_length + 1);
        
        if (index < _length)
        {
            Array.Copy(_data, index, _data, index + 1, _length - index);
        }
        
        _data[index] = value;
        _length++;
    }

    /// <summary>移除指定位置的元素</summary>
    public void RemoveAt(int index)
    {
        if ((uint)index >= (uint)_length)
            ThrowIndexOutOfRange(index);
        
        _length--;
        if (index < _length)
        {
            Array.Copy(_data, index + 1, _data, index, _length - index);
        }
        _data[_length] = TupleValue.Empty;
    }

    /// <summary>清空元组</summary>
    public void Clear()
    {
        Array.Clear(_data, 0, _length);
        _length = 0;
    }

    private void EnsureCapacity(int capacity)
    {
        if (_data.Length >= capacity) return;
        
        int newCapacity = _data.Length == 0 ? 4 : _data.Length * 2;
        if (newCapacity < capacity) newCapacity = capacity;
        
        var newData = new TupleValue[newCapacity];
        _data.AsSpan(0, _length).CopyTo(newData);
        _data = newData;
    }

    #endregion

    #region 运算操作

    /// <summary>元素级加法</summary>
    public MixedTuple Add(MixedTuple other)
    {
        ValidateSameLength(this, other);
        var result = new TupleValue[_length];
        for (int i = 0; i < _length; i++)
        {
            result[i] = _data[i].Add(other._data[i]);
        }
        return new MixedTuple(result);
    }

    /// <summary>元素级减法</summary>
    public MixedTuple Subtract(MixedTuple other)
    {
        ValidateSameLength(this, other);
        var result = new TupleValue[_length];
        for (int i = 0; i < _length; i++)
        {
            result[i] = _data[i].Subtract(other._data[i]);
        }
        return new MixedTuple(result);
    }

    /// <summary>元素级乘法</summary>
    public MixedTuple Multiply(MixedTuple other)
    {
        ValidateSameLength(this, other);
        var result = new TupleValue[_length];
        for (int i = 0; i < _length; i++)
        {
            result[i] = _data[i].Multiply(other._data[i]);
        }
        return new MixedTuple(result);
    }

    /// <summary>元素级除法</summary>
    public MixedTuple Divide(MixedTuple other)
    {
        ValidateSameLength(this, other);
        var result = new TupleValue[_length];
        for (int i = 0; i < _length; i++)
        {
            result[i] = _data[i].Divide(other._data[i]);
        }
        return new MixedTuple(result);
    }

    /// <summary>连接另一个混合元组</summary>
    public MixedTuple Concat(MixedTuple other)
    {
        var result = new TupleValue[_length + other._length];
        _data.AsSpan(0, _length).CopyTo(result);
        other._data.AsSpan(0, other._length).CopyTo(result.AsSpan(_length));
        return new MixedTuple(result);
    }

    private static void ValidateSameLength(MixedTuple left, MixedTuple right)
    {
        if (left._length != right._length)
        {
            throw new ArgumentException(
                $"元组长度不匹配: {left._length} vs {right._length}");
        }
    }

    #endregion

    #region 聚合操作

    /// <summary>求和（所有元素转换为double）</summary>
    public double Sum()
    {
        double sum = 0;
        for (int i = 0; i < _length; i++)
        {
            sum += _data[i].AsDouble;
        }
        return sum;
    }

    /// <summary>求平均值</summary>
    public double Average() => _length == 0 ? 0 : Sum() / _length;

    /// <summary>求最小值</summary>
    public double Min()
    {
        if (_length == 0) throw new InvalidOperationException("元组为空");
        double min = _data[0].AsDouble;
        for (int i = 1; i < _length; i++)
        {
            var val = _data[i].AsDouble;
            if (val < min) min = val;
        }
        return min;
    }

    /// <summary>求最大值</summary>
    public double Max()
    {
        if (_length == 0) throw new InvalidOperationException("元组为空");
        double max = _data[0].AsDouble;
        for (int i = 1; i < _length; i++)
        {
            var val = _data[i].AsDouble;
            if (val > max) max = val;
        }
        return max;
    }

    #endregion

    #region 转换方法

    /// <summary>尝试转换为同质元组</summary>
    public Tuple<T>? TryToTuple<T>() where T : unmanaged, IEquatable<T>
    {
        if (_length == 0) return new Tuple<T>();
        
        try
        {
            var result = new T[_length];
            for (int i = 0; i < _length; i++)
            {
                result[i] = ConvertTo<T>(_data[i]);
            }
            return new Tuple<T>(result);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>转换为Int32数组</summary>
    public int[] ToInt32Array()
    {
        var result = new int[_length];
        for (int i = 0; i < _length; i++)
        {
            result[i] = _data[i].AsInt32;
        }
        return result;
    }

    /// <summary>转换为Double数组</summary>
    public double[] ToDoubleArray()
    {
        var result = new double[_length];
        for (int i = 0; i < _length; i++)
        {
            result[i] = _data[i].AsDouble;
        }
        return result;
    }

    /// <summary>转换为String数组</summary>
    public string[] ToStringArray()
    {
        var result = new string[_length];
        for (int i = 0; i < _length; i++)
        {
            result[i] = _data[i].AsString;
        }
        return result;
    }

    /// <summary>转换为Object数组</summary>
    public object?[] ToObjectArray()
    {
        var result = new object?[_length];
        for (int i = 0; i < _length; i++)
        {
            result[i] = _data[i].AsObject;
        }
        return result;
    }

    private static T ConvertTo<T>(TupleValue value) where T : unmanaged
    {
        if (typeof(T) == typeof(bool)) return (T)(object)value.AsBool;
        if (typeof(T) == typeof(ushort)) return (T)(object)value.AsUInt16;
        if (typeof(T) == typeof(int)) return (T)(object)value.AsInt32;
        if (typeof(T) == typeof(long)) return (T)(object)value.AsInt64;
        if (typeof(T) == typeof(float)) return (T)(object)value.AsFloat;
        if (typeof(T) == typeof(double)) return (T)(object)value.AsDouble;
        throw new InvalidCastException($"无法转换为 {typeof(T)}");
    }

    #endregion

    #region IEnumerable

    public IEnumerator<TupleValue> GetEnumerator()
    {
        for (int i = 0; i < _length; i++)
        {
            yield return _data[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    #region 运算符重载

    public static MixedTuple operator +(MixedTuple left, MixedTuple right) => left.Add(right);
    public static MixedTuple operator -(MixedTuple left, MixedTuple right) => left.Subtract(right);
    public static MixedTuple operator *(MixedTuple left, MixedTuple right) => left.Multiply(right);
    public static MixedTuple operator /(MixedTuple left, MixedTuple right) => left.Divide(right);

    #endregion

    #region 辅助方法

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowIndexOutOfRange(int index)
    {
        throw new IndexOutOfRangeException(
            $"索引 {index} 超出范围。有效范围: 0 到 {_length - 1}");
    }

    public void Dispose()
    {
        _data = [];
        _length = 0;
        GC.SuppressFinalize(this);
    }

    public override string ToString()
    {
        if (_length == 0) return "[]";
        if (_length == 1) return _data[0].ToString();
        
        var elements = new string[_length];
        for (int i = 0; i < _length; i++)
        {
            elements[i] = _data[i].ToString();
        }
        return $"[{string.Join(", ", elements)}]";
    }

    #endregion
}
