using System;
using System.Collections.Generic;


namespace DotNet.Excel
{
    /// <summary>
    /// Excel 单元格位置（不可变）
    /// </summary>
    public readonly struct Cell : IEquatable<Cell>
    {
        public int Row { get; }
        public int Col { get; }

        internal int ExcelRow => Row + 1;
        internal int ExcelCol => Col + 1;

        public Cell(int row, int col)
        {
            Row = row;
            Col = col;
        }

        /// <summary>
        /// 从 Excel 地址创建（如 "A1", "B2"）
        /// </summary>
        public static Cell Parse(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                throw new ArgumentException("地址不能为空", nameof(address));

            int col = 0, row = 0, i = 0;

            while (i < address.Length && char.IsLetter(address[i]))
                col = col * 26 + (char.ToUpper(address[i++]) - 'A' + 1);

            while (i < address.Length && char.IsDigit(address[i]))
                row = row * 10 + (address[i++] - '0');

            if (col == 0 || row == 0)
                throw new ArgumentException($"无效地址: {address}", nameof(address));

            return new Cell(row - 1, col - 1);
        }

        /// <summary>
        /// 转为 Excel 地址（如 "A1"）
        /// </summary>
        public string ToAddress()
        {
            var colName = "";
            var colNum = Col + 1;
            while (colNum > 0)
            {
                colNum--;
                colName = (char)('A' + colNum % 26) + colName;
                colNum /= 26;
            }
            return $"{colName}{Row + 1}";
        }

        public Cell Offset(int rows, int cols) => new Cell(Row + rows, Col + cols);

        public override string ToString() => ToAddress();
        public override int GetHashCode() => unchecked(((Row << 5) + Row) ^ Col);
        public override bool Equals(object obj) => obj is Cell c && Equals(c);
        public bool Equals(Cell other) => Row == other.Row && Col == other.Col;
        public static bool operator ==(Cell a, Cell b) => a.Equals(b);
        public static bool operator !=(Cell a, Cell b) => !a.Equals(b);

        public static Cell Origin => default;
    }

    /// <summary>
    /// 写入选项
    /// </summary>
    public sealed class WriteOptions
    {
        public bool WriteHeaders { get; set; } = true;
        public bool AutoFit { get; set; } = true;
        public bool BoldHeaders { get; set; } = true;
        public string DateFormat { get; set; } = "yyyy-MM-dd";
        public string NumberFormat { get; set; }
        public string[] BoolFormat { get; set; } = { "是", "否" };

        public static WriteOptions Default => new WriteOptions();
    }

    /// <summary>
    /// 读取选项
    /// </summary>
    public sealed class ReadOptions
    {
        public bool HasHeaders { get; set; } = true;
        public bool SkipEmptyRows { get; set; } = true;

        public static ReadOptions Default => new ReadOptions();
    }
}
