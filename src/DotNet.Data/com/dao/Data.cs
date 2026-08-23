using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace DotNet.Data.dao
{
    interface Data
    {
        #region 数据操作-写

        /// <summary>
        /// 将字符串写入到文件
        /// </summary>
        /// <param name="value">值</param>
        /// <returns></returns>
        int write_string(string value);

        /// <summary>
        /// 将数组写入到文件
        /// </summary>
        /// <param name="array">数组数据</param>
        /// <returns></returns>
        int write_array(string[] array);

        /// <summary>
        /// 将ListBox写入到文件
        /// </summary>
        /// <param name="listBox">界面控件</param>
        /// <returns></returns>
        int write_listBox(ListBox listBox);

        /// <summary>
        /// 将字典写入到文件
        /// </summary>
        /// <param name="data">字典数据</param>
        /// <returns></returns>
        int write_dictionary(Dictionary<object, object> data);

        /// <summary>
        /// 将数组集合写入到文件
        /// </summary>
        /// <param name="arrayList">ArrayList集合</param>
        int write_arrayList(ArrayList arrayList);

        /// <summary>
        /// 将泛型实体类集合写入到文件
        /// </summary>
        /// <param name="listT">泛型集合数据</param>
        int write_listT<T>(List<T> listT);

        /// <summary>
        /// 将实体类集合写入到文件
        /// </summary>
        /// <param name="listObj">泛型集合数据</param>
        int write_listObj(List<object> listObj);

        /// <summary>
        /// 将界面的参数写入到文件
        /// </summary>
        /// <param name="listControl">界面控件集合</param>
        int write_listControl(List<Control> listControl);

        /// <summary>
        /// 将界面的参数写入到文件-自动获取界面所有参数
        /// </summary>
        /// <param name="form">界面控件集合</param>
        int write_Control(Form form);

        /// <summary>
        /// 将DataGridView的值写入到文件
        /// </summary>
        /// <param name="dataGridView">dataGridView数据</param>
        int write_dataGridView(DataGridView dataGridView);

        /// <summary>
        /// 将数组写入到文件
        /// </summary>
        /// <param name="dataTable">DataTable数据</param>
        int write_dataTable(DataTable dataTable);

        #endregion

        #region 数据操作-读

        /// <summary>
        /// 读取文件的值-string
        /// </summary>
        /// <param name="value">值</param>
        /// <returns></returns>
        int read_string(ref string value);

        /// <summary>
        /// 读取文件获取返回值-ArrayList
        /// </summary>
        /// <param name="array">文件返回值</param>
        int read_array(ref string[] array);

        /// <summary>
        /// 读取文件获取返回值-ArrayList
        /// </summary>
        /// <param name="arrayList">文件返回值</param>
        int read_arrayList(ref ArrayList arrayList);

        /// <summary>
        /// 读取文件的值-listBox
        /// </summary>
        /// <param name="listBox">listBox界面控件</param>
        /// <returns></returns>
        int read_listBox(ref ListBox listBox);

        /// <summary>
        /// 读取文件的值-字典
        /// </summary>
        /// <param name="data">字典数据</param>
        /// <returns></returns>
        int read_dictionary(ref Dictionary<object, object> data);

        /// <summary>
        /// 读取文件获取返回值-ListObject
        /// </summary>
        /// <param name="listT"></param>
        int read_listT<T>(ref List<T> listT) where T : new();

        /// <summary>
        /// 读取文件获取返回值-ListObject
        /// </summary>
        /// <param name="listObj"></param>
        int read_listObj(ref List<object> listObj);

        /// <summary>
        /// 读取文件获取界面相应的控件值
        /// </summary>
        /// <param name="listControl">界面控件</param>
        int read_listControl(ref List<Control> listControl);

        /// <summary>
        /// 读取文件获取界面相应的控件值
        /// </summary>
        /// <param name="form">界面</param>
        int read_Control(Form form);

        /// <summary>
        /// 读取文件获取返回值-DataGridView
        /// </summary>
        /// <param name="dataGridView"></param>
        int read_dataGridView(ref DataGridView dataGridView);

        /// <summary>
        /// 读取文件获取返回值-DataGridView
        /// </summary>
        /// <param name="dataTable"></param>
        int read_dataTable(ref DataTable dataTable);

        #endregion
    }
}
