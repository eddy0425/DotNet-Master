namespace DotNet.CvTuples;

/// <summary>
/// 元组元素类型枚举
/// </summary>
public enum TupleType : byte
{
    /// <summary>空元组</summary>
    Empty = 0,
    
    /// <summary>布尔类型 (System.Boolean)</summary>
    Bool = 1,
    
    /// <summary>16位无符号整数 (System.UInt16)</summary>
    UInt16 = 2,
    
    /// <summary>32位整数 (System.Int32)</summary>
    Int32 = 3,
    
    /// <summary>64位整数 (System.Int64)</summary>
    Int64 = 4,
    
    /// <summary>单精度浮点 (System.Single)</summary>
    Float = 5,
    
    /// <summary>双精度浮点 (System.Double)</summary>
    Double = 6,
    
    /// <summary>字符串 (System.String)</summary>
    String = 7,
    
    /// <summary>混合类型</summary>
    Mixed = 8,
    
    /// <summary>指针类型 (System.IntPtr)</summary>
    IntPtr = 9
}

