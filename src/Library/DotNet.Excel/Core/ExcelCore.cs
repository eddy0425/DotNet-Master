using System;
using System.IO;
using System.Linq;
using System.Data;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;
using OfficeOpenXml;


namespace DotNet.Excel
{
    /// <summary>
    /// Excel 操作核心类
    /// </summary>
    public sealed class Excel : IDisposable
    {
        private ExcelPackage _pkg;
        private FileInfo _file;
        private bool _disposed;

        private Excel() { }

        #region 工厂方法

        /// <summary>
        /// 打开已有文件
        /// </summary>
        public static Excel Open(string path)
        {
            var excel = new Excel { _file = new FileInfo(path) };
            if (!excel._file.Exists)
                throw new FileNotFoundException("文件不存在", path);
            excel._pkg = new ExcelPackage(excel._file);
            return excel;
        }

        /// <summary>
        /// 创建新文件
        /// </summary>
        public static Excel Create(string path)
        {
            var excel = new Excel
            {
                _file = new FileInfo(path),
                _pkg = new ExcelPackage()
            };
            return excel;
        }

        /// <summary>
        /// 打开或创建文件
        /// </summary>
        public static Excel OpenOrCreate(string path)
        {
            return File.Exists(path) ? Open(path) : Create(path);
        }

        #endregion

        #region 读取

        /// <summary>
        /// 读取单元格
        /// </summary>
        public string Read(Cell cell, string sheet = null)
        {
            var ws = GetSheet(sheet);
            if (ws == null) return null;
            var val = ws.Cells[cell.ExcelRow, cell.ExcelCol].Value;
            return val is bool b ? (b ? "TRUE" : "FALSE") : val?.ToString();
        }

        /// <summary>
        /// 读取单元格（地址格式）
        /// </summary>
        public string Read(string address, string sheet = null) => Read(Cell.Parse(address), sheet);

        /// <summary>
        /// 读取为 DataTable
        /// </summary>
        public DataTable ReadTable(Cell start = default, ReadOptions options = null, string sheet = null)
        {
            options = options ?? ReadOptions.Default;
            var ws = GetSheet(sheet);
            if (ws?.Dimension == null) return new DataTable();

            var table = new DataTable();
            int r0 = start.ExcelRow, c0 = start.ExcelCol;
            int rEnd = ws.Dimension.End.Row, cEnd = ws.Dimension.End.Column;

            // 列
            for (int c = c0; c <= cEnd; c++)
            {
                var name = options.HasHeaders ? ws.Cells[r0, c].Text : $"Col{c - c0}";
                if (string.IsNullOrEmpty(name)) name = $"Col{c - c0}";
                while (table.Columns.Contains(name)) name += "_";
                table.Columns.Add(name);
            }

            // 行
            int dataRow = options.HasHeaders ? r0 + 1 : r0;
            for (int r = dataRow; r <= rEnd; r++)
            {
                var row = table.NewRow();
                bool hasVal = false;
                for (int c = c0; c <= cEnd; c++)
                {
                    var v = ws.Cells[r, c].Value;
                    row[c - c0] = v is bool b ? (b ? "TRUE" : "FALSE") : (object)v ?? DBNull.Value;
                    if (v != null) hasVal = true;
                }
                if (!options.SkipEmptyRows || hasVal)
                    table.Rows.Add(row);
            }
            return table;
        }

        /// <summary>
        /// 读取为对象列表
        /// </summary>
        public List<T> ReadList<T>(Cell start = default, ReadOptions options = null, string sheet = null) where T : new()
        {
            var dt = ReadTable(start, options ?? new ReadOptions { HasHeaders = true }, sheet);
            return ToList<T>(dt);
        }

        /// <summary>
        /// 读取所有工作表为 DataSet
        /// </summary>
        public DataSet ReadAll(Cell start = default, ReadOptions options = null)
        {
            var ds = new DataSet();
            foreach (var ws in _pkg.Workbook.Worksheets)
            {
                if (ws.Dimension == null) continue;
                var dt = ReadTableFromWorksheet(ws, start, options ?? ReadOptions.Default);
                dt.TableName = ws.Name;
                ds.Tables.Add(dt);
            }
            return ds;
        }

        #endregion

        #region 写入

        /// <summary>
        /// 写入单元格
        /// </summary>
        public Excel Write(Cell cell, object value, string sheet = null)
        {
            var ws = GetOrCreateSheet(sheet);
            ws.Cells[cell.ExcelRow, cell.ExcelCol].Value = value;
            return this;
        }

        /// <summary>
        /// 写入单元格（地址格式）
        /// </summary>
        public Excel Write(string address, object value, string sheet = null) => Write(Cell.Parse(address), value, sheet);

        /// <summary>
        /// 写入 DataTable
        /// </summary>
        public Excel WriteTable(DataTable data, Cell start = default, WriteOptions options = null, string sheet = null)
        {
            if (data == null || data.Columns.Count == 0) return this;
            options = options ?? WriteOptions.Default;

            var ws = GetOrCreateSheet(sheet);
            int r = start.ExcelRow, c0 = start.ExcelCol;

            // 表头
            if (options.WriteHeaders)
            {
                for (int i = 0; i < data.Columns.Count; i++)
                {
                    var cell = ws.Cells[r, c0 + i];
                    cell.Value = data.Columns[i].ColumnName;
                    if (options.BoldHeaders) cell.Style.Font.Bold = true;
                }
                r++;
            }

            // 数据
            foreach (DataRow dr in data.Rows)
            {
                for (int i = 0; i < data.Columns.Count; i++)
                {
                    var v = dr[i];
                    var cell = ws.Cells[r, c0 + i];
                    if (v == DBNull.Value) continue;

                    cell.Value = v is bool b ? (b ? options.BoolFormat[0] : options.BoolFormat[1]) : v;
                    ApplyFormat(cell, v, options);
                }
                r++;
            }

            if (options.AutoFit)
                for (int i = c0; i < c0 + data.Columns.Count; i++)
                    ws.Column(i).AutoFit();

            return this;
        }

        /// <summary>
        /// 写入对象列表
        /// </summary>
        public Excel WriteList<T>(IEnumerable<T> data, Cell start = default, WriteOptions options = null, string sheet = null)
        {
            return WriteTable(ToDataTable(data), start, options, sheet);
        }

        /// <summary>
        /// 写入 DataSet（多工作表）
        /// </summary>
        public Excel WriteAll(DataSet data, Cell start = default, WriteOptions options = null)
        {
            if (data == null) return this;
            foreach (DataTable dt in data.Tables)
            {
                var name = string.IsNullOrEmpty(dt.TableName) ? $"Sheet{data.Tables.IndexOf(dt) + 1}" : dt.TableName;
                WriteTable(dt, start, options, name);
            }
            return this;
        }

        #endregion

        #region 保存

        /// <summary>
        /// 保存文件
        /// </summary>
        public Excel Save()
        {
            EnsureDir(_file.DirectoryName);
            _pkg.SaveAs(_file);
            return this;
        }

        /// <summary>
        /// 另存为
        /// </summary>
        public Excel SaveAs(string path)
        {
            _file = new FileInfo(path);
            return Save();
        }

        #endregion

        #region 静态便捷方法

        /// <summary>
        /// 快速读取单元格
        /// </summary>
        public static string ReadCell(string path, Cell cell, string sheet = null)
        {
            using (var e = Open(path)) return e.Read(cell, sheet);
        }

        /// <summary>
        /// 快速读取表格
        /// </summary>
        public static DataTable ReadDataTable(string path, Cell start = default, ReadOptions options = null, string sheet = null)
        {
            using (var e = Open(path)) return e.ReadTable(start, options, sheet);
        }

        /// <summary>
        /// 快速读取列表
        /// </summary>
        public static List<T> Read<T>(string path, Cell start = default, ReadOptions options = null, string sheet = null) where T : new()
        {
            using (var e = Open(path)) return e.ReadList<T>(start, options, sheet);
        }

        /// <summary>
        /// 快速写入列表
        /// </summary>
        public static void Write<T>(string path, IEnumerable<T> data, Cell start = default, WriteOptions options = null, string sheet = null)
        {
            using (var e = OpenOrCreate(path)) e.WriteList(data, start, options, sheet).Save();
        }

        /// <summary>
        /// 快速写入 DataTable
        /// </summary>
        public static void Write(string path, DataTable data, Cell start = default, WriteOptions options = null, string sheet = null)
        {
            using (var e = OpenOrCreate(path)) e.WriteTable(data, start, options, sheet).Save();
        }

        #endregion

        #region 转换方法

        /// <summary>
        /// DataTable 转对象列表
        /// </summary>
        public static List<T> ToList<T>(DataTable dt) where T : new()
        {
            if (dt == null || dt.Rows.Count == 0) return new List<T>();

            var props = typeof(T).GetProperties()
                .Where(p => p.CanWrite)
                .ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

            var list = new List<T>(dt.Rows.Count);
            foreach (DataRow row in dt.Rows)
            {
                var item = new T();
                foreach (DataColumn col in dt.Columns)
                {
                    if (!props.TryGetValue(col.ColumnName, out var prop)) continue;
                    var v = row[col];
                    if (v == DBNull.Value) continue;

                    prop.SetValue(item, ConvertValue(v, prop.PropertyType), null);
                }
                list.Add(item);
            }
            return list;
        }

        /// <summary>
        /// 对象列表转 DataTable
        /// </summary>
        public static DataTable ToDataTable<T>(IEnumerable<T> data)
        {
            var dt = new DataTable();
            if (data == null) return dt;

            var props = typeof(T).GetProperties().Where(p => p.CanRead).ToArray();
            foreach (var p in props)
                dt.Columns.Add(p.Name, Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType);

            foreach (var item in data)
            {
                var row = dt.NewRow();
                foreach (var p in props)
                    row[p.Name] = p.GetValue(item, null) ?? DBNull.Value;
                dt.Rows.Add(row);
            }
            return dt;
        }

        #endregion

        #region 私有方法

        private ExcelWorksheet GetSheet(string name)
        {
            if (_pkg.Workbook.Worksheets.Count == 0) return null;
            return string.IsNullOrEmpty(name)
                ? _pkg.Workbook.Worksheets[1]
                : _pkg.Workbook.Worksheets.FirstOrDefault(w => w.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private ExcelWorksheet GetOrCreateSheet(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return _pkg.Workbook.Worksheets.Count > 0
                    ? _pkg.Workbook.Worksheets[1]
                    : _pkg.Workbook.Worksheets.Add("Sheet1");
            }

            var ws = _pkg.Workbook.Worksheets.FirstOrDefault(w => w.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            return ws ?? _pkg.Workbook.Worksheets.Add(name);
        }

        private DataTable ReadTableFromWorksheet(ExcelWorksheet ws, Cell start, ReadOptions options)
        {
            var table = new DataTable();
            if (ws?.Dimension == null) return table;

            int r0 = start.ExcelRow, c0 = start.ExcelCol;
            int rEnd = ws.Dimension.End.Row, cEnd = ws.Dimension.End.Column;

            for (int c = c0; c <= cEnd; c++)
            {
                var name = options.HasHeaders ? ws.Cells[r0, c].Text : $"Col{c - c0}";
                if (string.IsNullOrEmpty(name)) name = $"Col{c - c0}";
                while (table.Columns.Contains(name)) name += "_";
                table.Columns.Add(name);
            }

            int dataRow = options.HasHeaders ? r0 + 1 : r0;
            for (int r = dataRow; r <= rEnd; r++)
            {
                var row = table.NewRow();
                bool hasVal = false;
                for (int c = c0; c <= cEnd; c++)
                {
                    var v = ws.Cells[r, c].Value;
                    row[c - c0] = v is bool b ? (b ? "TRUE" : "FALSE") : (object)v ?? DBNull.Value;
                    if (v != null) hasVal = true;
                }
                if (!options.SkipEmptyRows || hasVal)
                    table.Rows.Add(row);
            }
            return table;
        }

        private static void ApplyFormat(ExcelRange cell, object value, WriteOptions opt)
        {
            if (value is DateTime && !string.IsNullOrEmpty(opt.DateFormat))
                cell.Style.Numberformat.Format = opt.DateFormat;
            else if ((value is int || value is long || value is double || value is decimal) && !string.IsNullOrEmpty(opt.NumberFormat))
                cell.Style.Numberformat.Format = opt.NumberFormat;
        }

        private static object ConvertValue(object value, Type targetType)
        {
            var type = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (type == typeof(bool))
            {
                if (value is bool b) return b;
                var s = value.ToString().Trim().ToUpperInvariant();
                if (s == "TRUE" || s == "YES" || s == "Y" || s == "1" || s == "是") return true;
                if (s == "FALSE" || s == "NO" || s == "N" || s == "0" || s == "否") return false;
                throw new InvalidCastException($"无法转换为 bool: {value}");
            }

            if (type == typeof(DateTime))
            {
                if (value is DateTime dt) return dt;
                if (value is double d) return DateTime.FromOADate(d);
                var s = value.ToString().Trim();
                if (double.TryParse(s, out double od) && od > 0 && od < 2958466)
                    return DateTime.FromOADate(od);
                if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                    return result;
                throw new InvalidCastException($"无法转换为 DateTime: {value}");
            }

            return Convert.ChangeType(value, type);
        }

        private static void EnsureDir(string dir)
        {
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (!_disposed)
            {
                _pkg?.Dispose();
                _pkg = null;
                _disposed = true;
            }
        }

        #endregion
    }
}
