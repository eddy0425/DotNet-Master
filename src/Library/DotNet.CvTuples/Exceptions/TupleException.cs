namespace DotNet.CvTuples;

/// <summary>
/// 元组操作异常基类
/// </summary>
public class TupleException : Exception
{
    public TupleException() : base() { }
    public TupleException(string message) : base(message) { }
    public TupleException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// 元组访问异常 - 索引越界、类型不匹配等
/// </summary>
public class TupleAccessException : TupleException
{
    /// <summary>尝试访问的索引</summary>
    public int? Index { get; }
    
    /// <summary>元组长度</summary>
    public int? Length { get; }
    
    /// <summary>期望的类型</summary>
    public TupleType? ExpectedType { get; }
    
    /// <summary>实际的类型</summary>
    public TupleType? ActualType { get; }

    public TupleAccessException(string message) : base(message) { }
    
    public TupleAccessException(string message, Exception innerException) 
        : base(message, innerException) { }
    
    public TupleAccessException(int index, int length)
        : base($"索引 {index} 超出范围。元组长度为 {length}，有效索引范围: 0 到 {length - 1}")
    {
        Index = index;
        Length = length;
    }
    
    public TupleAccessException(TupleType expected, TupleType actual)
        : base($"类型不匹配。期望 {expected}，实际为 {actual}")
    {
        ExpectedType = expected;
        ActualType = actual;
    }
}

/// <summary>
/// 元组长度不匹配异常 - 运算时两个元组长度不一致
/// </summary>
public class TupleLengthMismatchException : TupleException
{
    public int LeftLength { get; }
    public int RightLength { get; }
    
    public TupleLengthMismatchException(int leftLength, int rightLength)
        : base($"元组长度不匹配: {leftLength} vs {rightLength}。元素级运算要求两个元组具有相同长度。")
    {
        LeftLength = leftLength;
        RightLength = rightLength;
    }
}

/// <summary>
/// 元组类型转换异常
/// </summary>
public class TupleConversionException : TupleException
{
    public Type? SourceType { get; }
    public Type? TargetType { get; }
    
    public TupleConversionException(string message) : base(message) { }
    
    public TupleConversionException(Type sourceType, Type targetType)
        : base($"无法将 {sourceType.Name} 转换为 {targetType.Name}")
    {
        SourceType = sourceType;
        TargetType = targetType;
    }
    
    public TupleConversionException(Type sourceType, Type targetType, Exception innerException)
        : base($"无法将 {sourceType.Name} 转换为 {targetType.Name}", innerException)
    {
        SourceType = sourceType;
        TargetType = targetType;
    }
}

