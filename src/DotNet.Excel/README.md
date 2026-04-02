# DotNet.Excel

极简 Excel 操作库，基于 EPPlus 4.5.3。

## 特性

- **单一入口**：一个 `Excel` 类完成所有操作
- **流式 API**：链式调用，代码简洁
- **双模式**：静态方法（单次操作）+ 实例方法（批量操作）
- **零配置**：默认参数覆盖 90% 场景

## 快速开始

### 单次操作（静态方法）

```csharp
using DotNet.Excel;

// 读取
string val = Excel.ReadCell("data.xlsx", new Cell(0, 0));
DataTable dt = Excel.ReadDataTable("data.xlsx");
List<Person> list = Excel.Read<Person>("data.xlsx");

// 写入
Excel.Write("output.xlsx", myList);
Excel.Write("output.xlsx", myDataTable);
```

### 批量操作（实例方法）

```csharp
using (var excel = Excel.Create("report.xlsx"))
{
    excel.Write(new Cell(0, 0), "标题")
         .Write("A2", "数据")
         .WriteList(people, new Cell(2, 0))
         .Save();
}

using (var excel = Excel.Open("data.xlsx"))
{
    var table1 = excel.ReadTable();
    var table2 = excel.ReadTable(sheet: "Sheet2");
}
```

### Cell 单元格

```csharp
var cell = new Cell(0, 0);          // 行0, 列0
var cell = Cell.Parse("A1");        // 从地址解析
var addr = cell.ToAddress();        // "A1"
var next = cell.Offset(1, 1);       // 偏移
```

## API

### 工厂方法

```csharp
Excel.Open(path)           // 打开已有文件
Excel.Create(path)         // 创建新文件
Excel.OpenOrCreate(path)   // 打开或创建
```

### 读取

```csharp
// 实例方法
excel.Read(cell)                           // 读取单元格
excel.Read("A1")                           // 地址格式
excel.ReadTable(start, options, sheet)     // 读取 DataTable
excel.ReadList<T>(start, options, sheet)   // 读取对象列表
excel.ReadAll(start, options)              // 读取所有工作表

// 静态方法
Excel.ReadCell(path, cell, sheet)
Excel.ReadDataTable(path, start, options, sheet)
Excel.Read<T>(path, start, options, sheet)
```

### 写入

```csharp
// 实例方法（链式）
excel.Write(cell, value)                   // 写入单元格
excel.Write("A1", value)                   // 地址格式
excel.WriteTable(data, start, options)     // 写入 DataTable
excel.WriteList(data, start, options)      // 写入对象列表
excel.WriteAll(dataSet, start, options)    // 写入 DataSet

// 静态方法
Excel.Write(path, list, start, options)
Excel.Write(path, dataTable, start, options)
```

### 保存

```csharp
excel.Save()         // 保存
excel.SaveAs(path)   // 另存为
```

### 转换

```csharp
Excel.ToList<T>(dataTable)     // DataTable → List<T>
Excel.ToDataTable<T>(list)     // List<T> → DataTable
```

## 配置选项

### WriteOptions

```csharp
var options = new WriteOptions
{
    WriteHeaders = true,      // 写入表头
    AutoFit = true,           // 自动列宽
    BoldHeaders = true,       // 表头加粗
    DateFormat = "yyyy-MM-dd",
    NumberFormat = "#,##0.00",
    BoolFormat = new[] { "是", "否" }
};

excel.WriteList(data, options: options);
```

### ReadOptions

```csharp
var options = new ReadOptions
{
    HasHeaders = true,        // 首行是表头
    SkipEmptyRows = true      // 跳过空行
};

excel.ReadTable(options: options);
```

## 完整示例

```csharp
// 导出报表
public void ExportReport(List<Order> orders, string path)
{
    var options = new WriteOptions
    {
        DateFormat = "yyyy-MM-dd HH:mm",
        NumberFormat = "#,##0.00"
    };

    using (var excel = Excel.Create(path))
    {
        excel.Write(new Cell(0, 0), "订单报表")
             .Write(new Cell(1, 0), $"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm}")
             .WriteList(orders, new Cell(3, 0), options)
             .Save();
    }
}

// 导入数据
public List<Product> ImportProducts(string path)
{
    return Excel.Read<Product>(path, new Cell(1, 0)); // 跳过标题行
}

// 多表处理
public void ProcessMultiSheet(string path)
{
    using (var excel = Excel.Open(path))
    {
        var ds = excel.ReadAll();
        foreach (DataTable dt in ds.Tables)
        {
            Console.WriteLine($"工作表: {dt.TableName}, 行数: {dt.Rows.Count}");
        }
    }
}
```

## 注意

- 行列索引从 **0** 开始
- 批量操作记得调用 `Save()`
- 使用 `using` 确保资源释放
