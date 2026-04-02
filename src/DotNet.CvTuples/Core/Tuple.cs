using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace DotNet.CvTuples;

/// <summary>
/// 高性能泛型元组 - 核心数据结构
/// 使用单一泛型类型替代多个具体实现类
/// </summary>
/// <typeparam name="T">元素类型，必须是非托管类型</typeparam>
public class Tuple<T> : ITuple, IDisposable, IEnumerable<T>, IEquatable<Tuple<T>>
    where T : unmanaged, IEquatable<T>
{
    private T[] _data;
    private int _length;
    private readonly TupleType _type;
    
    /// <summary>获取元组类型</summary>
    public TupleType Type => _type;
    
    /// <summary>获取元组长度</summary>
    public int Length => _length;
    
    /// <summary>获取底层数组的只读视图</summary>
    public ReadOnlySpan<T> Span => _data.AsSpan(0, _length);
    
    /// <summary>获取底层数组的可写视图（内部使用）</summary>
    internal Span<T> WritableSpan => _data.AsSpan(0, _length);
    
    /// <summary>获取底层数组的Memory表示</summary>
    public ReadOnlyMemory<T> Memory => _data.AsMemory(0, _length);

    /// <summary>获取底层数组（用于高级操作）</summary>
    internal T[] Data => _data;

    #region 构造函数

    /// <summary>创建空元组</summary>
    public Tuple()
    {
        _data = [];
        _length = 0;
        _type = GetTupleType();
    }

    /// <summary>创建包含单个元素的元组</summary>
    public Tuple(T value)
    {
        _data = [value];
        _length = 1;
        _type = GetTupleType();
    }

    /// <summary>创建包含多个元素的元组</summary>
    public Tuple(params T[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _data = new T[values.Length];
        values.CopyTo(_data, 0);
        _length = values.Length;
        _type = GetTupleType();
    }

    /// <summary>从Span创建元组（零拷贝不可能，会复制数据）</summary>
    public Tuple(ReadOnlySpan<T> values)
    {
        _data = values.ToArray();
        _length = values.Length;
        _type = GetTupleType();
    }

    /// <summary>创建指定容量的元组（预分配）</summary>
    public Tuple(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(capacity);
        _data = new T[capacity];
        _length = 0;
        _type = GetTupleType();
    }

    /// <summary>使用工厂方法创建并填充元组</summary>
    public Tuple(int length, Func<int, T> factory)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentNullException.ThrowIfNull(factory);
        
        _data = new T[length];
        for (int i = 0; i < length; i++)
        {
            _data[i] = factory(i);
        }
        _length = length;
        _type = GetTupleType();
    }

    #endregion

    #region 索引器

    /// <summary>获取或设置指定索引处的元素</summary>
    public T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)index >= (uint)_length)
                ThrowIndexOutOfRange(index);
            return _data[index];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if ((uint)index >= (uint)_length)
                ThrowIndexOutOfRange(index);
            _data[index] = value;
        }
    }

    /// <summary>支持Index类型的索引器（C# 8+）</summary>
    public T this[Index index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => this[index.GetOffset(_length)];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set => this[index.GetOffset(_length)] = value;
    }

    /// <summary>支持Range类型的切片（C# 8+）</summary>
    public Tuple<T> this[Range range]
    {
        get
        {
            var (offset, length) = range.GetOffsetAndLength(_length);
            return new Tuple<T>(_data.AsSpan(offset, length));
        }
    }

    /// <summary>多索引访问</summary>
    public Tuple<T> this[params int[] indices]
    {
        get
        {
            var result = new T[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                var idx = indices[i];
                if ((uint)idx >= (uint)_length)
                    ThrowIndexOutOfRange(idx);
                result[i] = _data[idx];
            }
            return new Tuple<T>(result);
        }
    }

    #endregion

    #region ITuple 实现

    object? ITuple.this[int index] => this[index];
    int ITuple.Length => _length;

    #endregion

    #region 类型转换

    /// <summary>转换为指定类型的元组</summary>
    public Tuple<TTarget> Cast<TTarget>() where TTarget : unmanaged, IEquatable<TTarget>
    {
        var result = new TTarget[_length];
        for (int i = 0; i < _length; i++)
        {
            result[i] = ConvertValue<TTarget>(_data[i]);
        }
        return new Tuple<TTarget>(result);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TTarget ConvertValue<TTarget>(T value) where TTarget : unmanaged
    {
        // 使用泛型数学进行类型转换
        if (typeof(T) == typeof(int) && typeof(TTarget) == typeof(double))
            return (TTarget)(object)(double)(int)(object)value;
        if (typeof(T) == typeof(int) && typeof(TTarget) == typeof(long))
            return (TTarget)(object)(long)(int)(object)value;
        if (typeof(T) == typeof(float) && typeof(TTarget) == typeof(double))
            return (TTarget)(object)(double)(float)(object)value;
        if (typeof(T) == typeof(double) && typeof(TTarget) == typeof(float))
            return (TTarget)(object)(float)(double)(object)value;
        
        // 通用转换路径
        return (TTarget)Convert.ChangeType(value, typeof(TTarget));
    }

    /// <summary>转换为数组</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T[] ToArray()
    {
        var result = new T[_length];
        _data.AsSpan(0, _length).CopyTo(result);
        return result;
    }

    #endregion

    #region 修改操作

    /// <summary>追加元素</summary>
    public void Append(T value)
    {
        EnsureCapacity(_length + 1);
        _data[_length++] = value;
    }

    /// <summary>追加多个元素</summary>
    public void Append(ReadOnlySpan<T> values)
    {
        EnsureCapacity(_length + values.Length);
        values.CopyTo(_data.AsSpan(_length));
        _length += values.Length;
    }

    /// <summary>在指定位置插入元素</summary>
    public void Insert(int index, T value)
    {
        if ((uint)index > (uint)_length)
            ThrowIndexOutOfRange(index);
        
        EnsureCapacity(_length + 1);
        
        // 移动后续元素
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
        _data[_length] = default;
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
        
        var newData = new T[newCapacity];
        _data.AsSpan(0, _length).CopyTo(newData);
        _data = newData;
    }

    #endregion

    #region 变换操作

    /// <summary>映射每个元素</summary>
    public Tuple<TResult> Select<TResult>(Func<T, TResult> selector) 
        where TResult : unmanaged, IEquatable<TResult>
    {
        var result = new TResult[_length];
        for (int i = 0; i < _length; i++)
        {
            result[i] = selector(_data[i]);
        }
        return new Tuple<TResult>(result);
    }

    /// <summary>过滤元素</summary>
    public Tuple<T> Where(Func<T, bool> predicate)
    {
        var result = new List<T>(_length);
        for (int i = 0; i < _length; i++)
        {
            if (predicate(_data[i]))
                result.Add(_data[i]);
        }
        return new Tuple<T>([.. result]);
    }

    /// <summary>反转元组</summary>
    public Tuple<T> Reverse()
    {
        var result = new T[_length];
        for (int i = 0; i < _length; i++)
        {
            result[i] = _data[_length - 1 - i];
        }
        return new Tuple<T>(result);
    }

    /// <summary>连接另一个元组</summary>
    public Tuple<T> Concat(Tuple<T> other)
    {
        var result = new T[_length + other._length];
        _data.AsSpan(0, _length).CopyTo(result);
        other._data.AsSpan(0, other._length).CopyTo(result.AsSpan(_length));
        return new Tuple<T>(result);
    }

    #endregion

    #region 比较和相等

    public bool Equals(Tuple<T>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (_length != other._length) return false;
        
        return Span.SequenceEqual(other.Span);
    }

    public override bool Equals(object? obj) => obj is Tuple<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(_length);
        hash.Add(_type);
        
        // 只哈希前几个元素以提高性能
        int count = Math.Min(_length, 8);
        for (int i = 0; i < count; i++)
        {
            hash.Add(_data[i]);
        }
        
        return hash.ToHashCode();
    }

    #endregion

    #region IEnumerable

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < _length; i++)
        {
            yield return _data[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>高性能枚举器（避免装箱）</summary>
    public Enumerator GetStructEnumerator() => new(this);

    public ref struct Enumerator
    {
        private readonly Tuple<T> _tuple;
        private int _index;

        internal Enumerator(Tuple<T> tuple)
        {
            _tuple = tuple;
            _index = -1;
        }

        public T Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _tuple._data[_index];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext() => ++_index < _tuple._length;

        public void Reset() => _index = -1;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _data = [];
        _length = 0;
        GC.SuppressFinalize(this);
    }

    #endregion

    #region 辅助方法

    private static TupleType GetTupleType()
    {
        if (typeof(T) == typeof(bool)) return TupleType.Bool;
        if (typeof(T) == typeof(ushort)) return TupleType.UInt16;
        if (typeof(T) == typeof(int)) return TupleType.Int32;
        if (typeof(T) == typeof(long)) return TupleType.Int64;
        if (typeof(T) == typeof(float)) return TupleType.Float;
        if (typeof(T) == typeof(double)) return TupleType.Double;
        if (typeof(T) == typeof(nint)) return TupleType.IntPtr;
        return TupleType.Mixed;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ThrowIndexOutOfRange(int index)
    {
        throw new IndexOutOfRangeException(
            $"索引 {index} 超出范围。有效范围: 0 到 {_length - 1}");
    }

    public override string ToString()
    {
        if (_length == 0) return "[]";
        if (_length == 1) return _data[0].ToString() ?? "null";
        
        return $"[{string.Join(", ", Span.ToArray())}]";
    }

    #endregion
}

/// <summary>
/// 数值类型元组 - 支持算术运算
/// </summary>
public class NumericTuple<T> : Tuple<T>
    where T : unmanaged, IEquatable<T>, INumber<T>
{
    public NumericTuple() : base() { }
    public NumericTuple(T value) : base(value) { }
    public NumericTuple(params T[] values) : base(values) { }
    public NumericTuple(ReadOnlySpan<T> values) : base(values) { }
    public NumericTuple(int capacity) : base(capacity) { }
    public NumericTuple(int length, Func<int, T> factory) : base(length, factory) { }

    #region 运算符重载

    public static NumericTuple<T> operator +(NumericTuple<T> left, NumericTuple<T> right) 
        => new(left.Add(right).ToArray());
    
    public static NumericTuple<T> operator -(NumericTuple<T> left, NumericTuple<T> right) 
        => new(left.Subtract(right).ToArray());
    
    public static NumericTuple<T> operator *(NumericTuple<T> left, NumericTuple<T> right) 
        => new(left.Multiply(right).ToArray());
    
    public static NumericTuple<T> operator /(NumericTuple<T> left, NumericTuple<T> right) 
        => new(left.Divide(right).ToArray());

    #endregion
}
