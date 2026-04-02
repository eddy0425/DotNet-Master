using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DotNet.Library.Extension
{
    public static class DataTableExtension
    {
        /// <summary>
        /// 将DataTable 转成 DataGridViews
        /// </summary>
        public static DataGridView ToDgv(this DataTable dataTable)
        {
            DataGridView dataGridView = new DataGridView();
            dataGridView.DataSource = dataTable;
            return dataGridView;
        }

        /// <summary>
        /// 将DataGridView 转成 DataTable
        /// </summary>
        public static DataTable ToDataTable(this DataGridView dataGridView)
        {
            DataTable dt = new DataTable();

            // 列强制转换
            for (int count = 0; count < dataGridView.Columns.Count; count++)
            {
                DataColumn dc = new DataColumn(dataGridView.Columns[count].Name.ToString());
                dt.Columns.Add(dc);
            }

            // 循环行
            for (int count = 0; count < dataGridView.Rows.Count; count++)
            {
                DataRow dr = dt.NewRow();
                for (int countsub = 0; countsub < dataGridView.Columns.Count; countsub++)
                {
                    dr[countsub] = Convert.ToString(dataGridView.Rows[count].Cells[countsub].Value);
                }
                dt.Rows.Add(dr);
            }
            return dt;
        }

        /// <summary>
        /// 将List集合 转换成 DataTable
        /// </summary>
        /// <param name="HasRows">表标题</param>
        /// <returns></returns>
        public static DataTable ToDataTable<T>(this List<T> list, string[] HasRows) where T : new()
        {
            try
            {
                DataTable dtresult = new DataTable();
                if (list != null && list.Count > 0)
                {
                    dtresult.Columns.Clear(); //清除表格中的数据

                    for (int i = 0; i < HasRows.Length; i++)
                    {
                        dtresult.Columns.Add(HasRows[i]);  //获取标题
                    }

                    dtresult.Rows.Clear();  //清除表格中的数据

                    for (int i = 0; i < list.Count; i++)
                    {
                        ArrayList tenpList = new ArrayList();
                        foreach (PropertyInfo pi in list[0].GetType().GetProperties())
                        {
                            object obj = pi.GetValue(list[i], null);
                            tenpList.Add(obj);
                        }
                        object[] array = tenpList.ToArray();
                        dtresult.LoadDataRow(array, true);
                    }
                }
                return dtresult;
            }
            catch (Exception ex)
            {
                throw new Exception($"将List集合 转换成 DataTable出错!", ex);
            }
        }

        /// <summary>
        /// 将ArrayList集合 转换成 DataTable
        /// </summary>
        /// <param name="HasRows">表标题</param>
        /// <returns></returns>
        public static DataTable ToDataTable(this ArrayList list, string[] HasRows)
        {
            try
            {
                DataTable dtresult = new DataTable();
                if (list != null && list.Count > 0)
                {
                    dtresult.Columns.Clear(); //清除表格中的数据

                    for (int i = 0; i < HasRows.Length; i++)
                    {
                        dtresult.Columns.Add(HasRows[i]);  //获取标题
                    }

                    dtresult.Rows.Clear();  //清除表格中的数据

                    for (int i = 0; i < list.Count; i++)
                    {
                        ArrayList tenpList = new ArrayList();
                        foreach (PropertyInfo pi in list[i].GetType().GetProperties())
                        {
                            object obj = pi.GetValue(list[i], null);
                            tenpList.Add(obj);
                        }
                        object[] array = tenpList.ToArray();
                        dtresult.LoadDataRow(array, true);
                    }
                }
                return dtresult;
            }
            catch (Exception ex)
            {
                throw new Exception($"将List集合 转换成 DataTable出错!", ex);
            }
        }

        /// <summary>
        /// 将ArrayList集合 转换成 DataTable
        /// </summary>
        /// <param name="HasRows">表标题</param>
        /// <returns></returns>
        public static DataTable ToDataTable<T>(this ArrayList list, string[] HasRows, T model) where T : new()
        {
            try
            {
                DataTable dtresult = new DataTable();
                if (list != null && list.Count > 0)
                {
                    dtresult.Columns.Clear(); //清除表格中的数据

                    for (int i = 0; i < HasRows.Length; i++)
                    {
                        dtresult.Columns.Add(HasRows[i]);  //获取标题
                    }

                    dtresult.Rows.Clear();  //清除表格中的数据

                    for (int i = 0; i < list.Count; i++)
                    {
                        ArrayList tenpList = new ArrayList();
                        foreach (PropertyInfo pi in model.GetType().GetProperties())
                        {
                            object obj = pi.GetValue(list[i], null);
                            tenpList.Add(obj);
                        }
                        object[] array = tenpList.ToArray();
                        dtresult.LoadDataRow(array, true);
                    }
                }
                return dtresult;
            }
            catch (Exception ex)
            {
                throw new Exception($"将List集合 转换成 DataTable出错!", ex);
            }
        }

        public static DataTable ToDataTable<T>(this T item)
        {
            DataTable dt = new DataTable();
            Type type = typeof(T);
            PropertyInfo[] properties = type.GetProperties();

            // 创建列
            foreach (PropertyInfo property in properties)
            {
                dt.Columns.Add(property.Name, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);
            }

            // 添加行
            DataRow row = dt.NewRow();
            foreach (PropertyInfo property in properties)
            {
                row[property.Name] = property.GetValue(item) ?? DBNull.Value;
            }
            dt.Rows.Add(row);

            return dt;
        }

    }
}
