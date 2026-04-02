using System.Runtime.CompilerServices;

namespace DotNet.CvTuples;

/// <summary>
/// 混合类型元组的元素值 - 使用区分联合（Discriminated Union）模式
/// 避免装箱，高效存储不同类型的值
/// </summary>
public readonly struct TupleValue : IEquatable<TupleValue>
{
    // 类型标记
    private readonly TupleType _type;
    
    // 值存储
    private readonly long _intValue;
    private readonly double _floatValue;
    private readonly string? _stringValue;

    #region 属性

    /// <summary>获取值的类型</summary>
    public TupleType Type => _type;

    /// <summary>是否为空</summary>
    public bool IsEmpty => _type == TupleType.Empty;

    /// <summary>是否为数值类型</summary>
    public bool IsNumeric => _type is TupleType.Int32 or TupleType.Int64 
        or TupleType.Float or TupleType.Double or TupleType.UInt16;

    /// <summary>是否为整数类型</summary>
    public bool IsInteger => _type is TupleType.Int32 or TupleType.Int64 or TupleType.UInt16;

    #endregion

    #region 构造函数

    private TupleValue(TupleType type, long intValue, double floatValue, string? stringValue)
    {
        _type = type;
        _intValue = intValue;
        _floatValue = floatValue;
        _stringValue = stringValue;
    }

    /// <summary>创建空值</summary>
    public static TupleValue Empty => new(TupleType.Empty, 0, 0, null);

    /// <summary>从布尔值创建</summary>
    public static TupleValue FromBool(bool value) => 
        new(TupleType.Bool, value ? 1 : 0, 0, null);

    /// <summary>从UInt16创建</summary>
    public static TupleValue FromUInt16(ushort value) => 
        new(TupleType.UInt16, value, 0, null);

    /// <summary>从Int32创建</summary>
    public static TupleValue FromInt32(int value) => 
        new(TupleType.Int32, value, 0, null);

    /// <summary>从Int64创建</summary>
    public static TupleValue FromInt64(long value) => 
        new(TupleType.Int64, value, 0, null);

    /// <summary>从Float创建</summary>
    public static TupleValue FromFloat(float value) => 
        new(TupleType.Float, 0, value, null);

    /// <summary>从Double创建</summary>
    public static TupleValue FromDouble(double value) => 
        new(TupleType.Double, 0, value, null);

    /// <summary>从String创建</summary>
    public static TupleValue FromString(string? value) => 
        new(TupleType.String, 0, 0, value ?? string.Empty);

    /// <summary>从IntPtr创建</summary>
    public static TupleValue FromIntPtr(nint value) => 
        new(TupleType.IntPtr, value, 0, null);

    /// <summary>从object创建（自动推断类型）</summary>
    public static TupleValue From(object? value) => value switch
    {
        null => Empty,
        bool b => FromBool(b),
        ushort u => FromUInt16(u),
        int i => FromInt32(i),
        long l => FromInt64(l),
        float f => FromFloat(f),
        double d => FromDouble(d),
        string s => FromString(s),
        nint n => FromIntPtr(n),
        _ => throw new ArgumentException($"不支持的类型: {value.GetType()}")
    };

    #endregion

    #region 值访问

    /// <summary>获取布尔值</summary>
    public bool AsBool => _type switch
    {
        TupleType.Bool => _intValue != 0,
        TupleType.Int32 or TupleType.Int64 or TupleType.UInt16 => _intValue != 0,
        TupleType.Float or TupleType.Double => _floatValue != 0,
        _ => throw new InvalidCastException($"无法将 {_type} 转换为 bool")
    };

    /// <summary>获取UInt16值</summary>
    public ushort AsUInt16 => _type switch
    {
        TupleType.UInt16 => (ushort)_intValue,
        TupleType.Int32 => (ushort)_intValue,
        TupleType.Int64 => (ushort)_intValue,
        TupleType.Float => (ushort)_floatValue,
        TupleType.Double => (ushort)_floatValue,
        _ => throw new InvalidCastException($"无法将 {_type} 转换为 ushort")
    };

    /// <summary>获取Int32值</summary>
    public int AsInt32 => _type switch
    {
        TupleType.Int32 => (int)_intValue,
        TupleType.Int64 => (int)_intValue,
        TupleType.UInt16 => (int)_intValue,
        TupleType.Float => (int)_floatValue,
        TupleType.Double => (int)_floatValue,
        TupleType.Bool => _intValue != 0 ? 1 : 0,
        _ => throw new InvalidCastException($"无法将 {_type} 转换为 int")
    };

    /// <summary>获取Int64值</summary>
    public long AsInt64 => _type switch
    {
        TupleType.Int64 => _intValue,
        TupleType.Int32 => _intValue,
        TupleType.UInt16 => _intValue,
        TupleType.Float => (long)_floatValue,
        TupleType.Double => (long)_floatValue,
        TupleType.Bool => _intValue != 0 ? 1 : 0,
        TupleType.IntPtr => _intValue,
        _ => throw new InvalidCastException($"无法将 {_type} 转换为 long")
    };

    /// <summary>获取Float值</summary>
    public float AsFloat => _type switch
    {
        TupleType.Float => (float)_floatValue,
        TupleType.Double => (float)_floatValue,
        TupleType.Int32 => _intValue,
        TupleType.Int64 => _intValue,
        TupleType.UInt16 => _intValue,
        TupleType.Bool => _intValue != 0 ? 1f : 0f,
        _ => throw new InvalidCastException($"无法将 {_type} 转换为 float")
    };

    /// <summary>获取Double值</summary>
    public double AsDouble => _type switch
    {
        TupleType.Double => _floatValue,
        TupleType.Float => _floatValue,
        TupleType.Int32 => _intValue,
        TupleType.Int64 => _intValue,
        TupleType.UInt16 => _intValue,
        TupleType.Bool => _intValue != 0 ? 1.0 : 0.0,
        _ => throw new InvalidCastException($"无法将 {_type} 转换为 double")
    };

    /// <summary>获取String值</summary>
    public string AsString => _type switch
    {
        TupleType.String => _stringValue ?? string.Empty,
        TupleType.Int32 or TupleType.Int64 or TupleType.UInt16 => _intValue.ToString(),
        TupleType.Float or TupleType.Double => _floatValue.ToString(),
        TupleType.Bool => (_intValue != 0).ToString(),
        TupleType.Empty => string.Empty,
        _ => throw new InvalidCastException($"无法将 {_type} 转换为 string")
    };

    /// <summary>获取IntPtr值</summary>
    public nint AsIntPtr => _type switch
    {
        TupleType.IntPtr => (nint)_intValue,
        TupleType.Int32 => (nint)_intValue,
        TupleType.Int64 => (nint)_intValue,
        _ => throw new InvalidCastException($"无法将 {_type} 转换为 IntPtr")
    };

    /// <summary>获取object值（装箱）</summary>
    public object? AsObject => _type switch
    {
        TupleType.Empty => null,
        TupleType.Bool => _intValue != 0,
        TupleType.UInt16 => (ushort)_intValue,
        TupleType.Int32 => (int)_intValue,
        TupleType.Int64 => _intValue,
        TupleType.Float => (float)_floatValue,
        TupleType.Double => _floatValue,
        TupleType.String => _stringValue,
        TupleType.IntPtr => (nint)_intValue,
        _ => throw new InvalidOperationException($"未知类型: {_type}")
    };

    #endregion

    #region 运算

    /// <summary>加法运算</summary>
    public TupleValue Add(TupleValue other)
    {
        if (_type == TupleType.String || other._type == TupleType.String)
        {
            return FromString(AsString + other.AsString);
        }

        // 类型提升规则: double > float > long > int > ushort
        if (_type == TupleType.Double || other._type == TupleType.Double)
        {
            return FromDouble(AsDouble + other.AsDouble);
        }
        if (_type == TupleType.Float || other._type == TupleType.Float)
        {
            return FromFloat(AsFloat + other.AsFloat);
        }
        if (_type == TupleType.Int64 || other._type == TupleType.Int64)
        {
            return FromInt64(AsInt64 + other.AsInt64);
        }
        return FromInt32(AsInt32 + other.AsInt32);
    }

    /// <summary>减法运算</summary>
    public TupleValue Subtract(TupleValue other)
    {
        if (_type == TupleType.Double || other._type == TupleType.Double)
        {
            return FromDouble(AsDouble - other.AsDouble);
        }
        if (_type == TupleType.Float || other._type == TupleType.Float)
        {
            return FromFloat(AsFloat - other.AsFloat);
        }
        if (_type == TupleType.Int64 || other._type == TupleType.Int64)
        {
            return FromInt64(AsInt64 - other.AsInt64);
        }
        return FromInt32(AsInt32 - other.AsInt32);
    }

    /// <summary>乘法运算</summary>
    public TupleValue Multiply(TupleValue other)
    {
        if (_type == TupleType.Double || other._type == TupleType.Double)
        {
            return FromDouble(AsDouble * other.AsDouble);
        }
        if (_type == TupleType.Float || other._type == TupleType.Float)
        {
            return FromFloat(AsFloat * other.AsFloat);
        }
        if (_type == TupleType.Int64 || other._type == TupleType.Int64)
        {
            return FromInt64(AsInt64 * other.AsInt64);
        }
        return FromInt32(AsInt32 * other.AsInt32);
    }

    /// <summary>除法运算</summary>
    public TupleValue Divide(TupleValue other)
    {
        if (_type == TupleType.Double || other._type == TupleType.Double)
        {
            return FromDouble(AsDouble / other.AsDouble);
        }
        if (_type == TupleType.Float || other._type == TupleType.Float)
        {
            return FromFloat(AsFloat / other.AsFloat);
        }
        // 整数除法返回double以保持精度
        return FromDouble((double)AsInt64 / other.AsInt64);
    }

    #endregion

    #region 比较和相等

    public bool Equals(TupleValue other)
    {
        if (_type != other._type) return false;
        
        return _type switch
        {
            TupleType.String => _stringValue == other._stringValue,
            TupleType.Float or TupleType.Double => _floatValue == other._floatValue,
            _ => _intValue == other._intValue
        };
    }

    public override bool Equals(object? obj) => obj is TupleValue other && Equals(other);

    public override int GetHashCode() => _type switch
    {
        TupleType.String => HashCode.Combine(_type, _stringValue),
        TupleType.Float or TupleType.Double => HashCode.Combine(_type, _floatValue),
        _ => HashCode.Combine(_type, _intValue)
    };

    public static bool operator ==(TupleValue left, TupleValue right) => left.Equals(right);
    public static bool operator !=(TupleValue left, TupleValue right) => !left.Equals(right);

    #endregion

    #region 隐式转换

    public static implicit operator TupleValue(bool value) => FromBool(value);
    public static implicit operator TupleValue(ushort value) => FromUInt16(value);
    public static implicit operator TupleValue(int value) => FromInt32(value);
    public static implicit operator TupleValue(long value) => FromInt64(value);
    public static implicit operator TupleValue(float value) => FromFloat(value);
    public static implicit operator TupleValue(double value) => FromDouble(value);
    public static implicit operator TupleValue(string value) => FromString(value);
    public static implicit operator TupleValue(nint value) => FromIntPtr(value);

    public static implicit operator bool(TupleValue value) => value.AsBool;
    public static implicit operator ushort(TupleValue value) => value.AsUInt16;
    public static implicit operator int(TupleValue value) => value.AsInt32;
    public static implicit operator long(TupleValue value) => value.AsInt64;
    public static implicit operator float(TupleValue value) => value.AsFloat;
    public static implicit operator double(TupleValue value) => value.AsDouble;
    public static implicit operator string(TupleValue value) => value.AsString;

    #endregion

    public override string ToString() => _type switch
    {
        TupleType.Empty => "empty",
        TupleType.String => $"\"{_stringValue}\"",
        _ => AsObject?.ToString() ?? "null"
    };
}
