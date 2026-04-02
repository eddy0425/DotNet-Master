# DotNet.CvTuples

## 概述

高性能 C# 元组库，使用 C# 12 和 .NET 8 的最新特性实现最优设计。

## 设计目标

1. **性能优先** - SIMD加速、零分配路径、值类型优化
2. **类型安全** - 泛型设计、编译时类型检查
3. **API简洁** - 统一入口、直观命名、流畅调用
4. **内存高效** - Span/Memory支持、避免装箱

## 架构

```
DotNet.CvTuples (7 文件, ~1500 行代码):
├── CvTuple.cs          # 统一入口 + 工厂方法
├── Core/
│   ├── Tuple.cs        # 泛型元组 Tuple<T>
│   ├── TupleOperators.cs # SIMD加速运算
│   ├── TupleType.cs    # 类型枚举
│   ├── TupleValue.cs   # 值类型联合
│   └── MixedTuple.cs   # 混合类型元组
└── Exceptions/
    └── TupleException.cs
```

**优势:**
- 单一泛型类处理所有数值类型
- SIMD 向量化加速运算
- `TupleValue` 结构避免装箱
- Span/Memory 支持零拷贝操作
- 现代 C# 语法（Index/Range/模式匹配）

## 核心类型

### `Tuple<T>` - 泛型同质元组

```csharp
// 创建
var intTuple = new Tuple<int>(1, 2, 3, 4, 5);
var doubleTuple = new Tuple<double>(1.0, 2.0, 3.0);

// 使用工厂方法
var t1 = CvTuple.Create(1, 2, 3);
var t2 = CvTuple.Range(0, 100);  // [0, 1, 2, ..., 99]
var t3 = CvTuple.LinSpace(0, 1, 11);  // [0, 0.1, 0.2, ..., 1.0]

// 索引访问 (支持 C# 8 Index/Range)
int first = intTuple[0];
int last = intTuple[^1];
var slice = intTuple[1..4];  // [2, 3, 4]

// Span 访问 (零拷贝)
ReadOnlySpan<int> span = intTuple.Span;

// SIMD 加速运算
var sum = intTuple.Add(intTuple);     // 元素级加法
var product = intTuple.Multiply(2);    // 标量乘法

// 聚合
double total = doubleTuple.Sum();
double avg = doubleTuple.Average();
double min = doubleTuple.Min();
```

### `MixedTuple` - 异构混合元组

```csharp
// 创建混合类型
var mixed = CvTuple.CreateMixed(1, 2.5, "hello", true);

// 类型安全访问
int i = mixed.GetInt32(0);
double d = mixed.GetDouble(1);
string s = mixed.GetString(2);

// TupleValue 避免装箱
TupleValue val = mixed[0];
if (val.IsNumeric) 
{
    double num = val.AsDouble;
}

// 运算
var result = mixed + mixed;
```

### `TupleValue` - 零装箱值容器

```csharp
// 值类型，无装箱
TupleValue v1 = 42;           // int
TupleValue v2 = 3.14;         // double
TupleValue v3 = "hello";      // string

// 类型安全转换
int i = v1.AsInt32;
double d = v1.AsDouble;  // 自动转换

// 运算
TupleValue sum = v1.Add(v2);
```

## 性能特性

### SIMD 向量化

```csharp
// 自动使用 SIMD 指令加速
var a = CvTuple.Create(Enumerable.Range(0, 1000).ToArray());
var b = CvTuple.Create(Enumerable.Range(0, 1000).ToArray());

// 底层使用 Vector<T> 实现
// 在支持 AVX2 的 CPU 上可同时处理 8 个 int
var c = a.Add(b);  
```

### Span 零拷贝

```csharp
var tuple = CvTuple.Create(1, 2, 3, 4, 5);

// 直接访问底层内存，无拷贝
ReadOnlySpan<int> span = tuple.Span;

// 切片也是零拷贝视图
var slice = tuple[1..4];
```

## API 参考

| 操作 | 方法 |
|------|------|
| 创建 | `CvTuple.Create(1, 2, 3)` |
| 索引 | `tuple[0]` |
| 切片 | `tuple[1..4]` |
| 加法 | `tuple.Add(other)` 或 `tuple + other` |
| 类型 | `tuple.Type` |
| 转数组 | `tuple.ToArray()` |
| 遍历 | `foreach` 或 `Span` |

## 要求

- .NET 8.0+
- C# 12.0+
- 支持 SIMD 的 CPU（可选，自动降级）

## 设计原则

1. **泛型优于继承** - 单一 `Tuple<T>` 替代多个具体类型
2. **组合优于继承** - 使用扩展方法而非虚方法
3. **值类型优于引用** - `TupleValue` 结构避免GC
4. **Span优于数组** - 零拷贝切片和访问
5. **编译时优于运行时** - 泛型约束捕获类型错误
