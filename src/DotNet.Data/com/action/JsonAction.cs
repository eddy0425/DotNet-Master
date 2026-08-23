using DotNet.Data.dao;
using DotNet.Data.daoImpl;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DotNet.Data.action
{
    /// <summary>
    /// Json操作类
    /// </summary>
    public class JsonAction : JsonDao
    {
        JsonDao json = new JsonDaoImpl();

        /// <summary>
        /// 构造函数
        /// </summary>
        public JsonAction() 
        { 
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return; 
            setFormatting(true); 
        }

        /// <summary>
        /// 构造函数设置路径
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public JsonAction(string filePath)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return;
            init(filePath);
        }

        /// <summary>
        /// 构造函数设置路径
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="format">是否格式化json字符串</param>
        /// <returns></returns>
        public JsonAction(string filePath, bool format)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return;
            init(filePath, format);
        }

        /// <summary>
        /// 设置Json格式
        /// </summary>
        /// <param name="format">是否格式化json字符串</param>
        /// <returns></returns>
        public void setFormatting(bool format)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return;
            json.setFormatting(format);
        }

        /// <summary>
        /// 初始化JSON文件
        /// </summary>
        /// <param name="filePath">JSON文件路径</param>
        /// <returns></returns>
        public int init(string filePath)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1; //加速Licensing验证
            return json.init(filePath);
        }

        /// <summary>
        /// 初始化JSON文件
        /// </summary>
        /// <param name="filePath">JSON文件路径</param>
        /// <param name="format">是否格式化json字符串</param>
        /// <returns></returns>
        public int init(string filePath, bool format)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return json.init(filePath, format);
        }


        #region 数据操作-写

        /// <summary>
        /// 未开发
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int write_string(string value)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            throw new NotImplementedException();
        }

        /// <summary>
        /// 未开发
        /// </summary>
        /// <param name="value"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public int write_string(string value,string filePath)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            init(filePath);
            return write_string(value);
        }

        /// <summary>
        /// 未开发
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public int write_dictionary(Dictionary<object, object> data)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            throw new NotImplementedException();
        }

        /// <summary>
        /// 未开发
        /// </summary>
        /// <param name="data"></param>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public int write_dictionary(Dictionary<object, object> data, string filePath)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            init(filePath);
            return write_dictionary(data);
        }

        /// <summary>
        /// 将数组写入到JSON
        /// </summary>
        /// <param name="array">数据</param>
        /// <returns></returns>
        public int write_array(string[] array)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return json.write_array(array);
        }

        /// <summary>
        /// 将数组写入到JSON
        /// </summary>
        /// <param name="array">数据</param>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public int write_array(string[] array, string filePath)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            init(filePath);
            return json.write_array(array);
        }

        /// <summary>
        /// 将ListBox数据写入到JSON
        /// </summary>
        /// <param name="listBox">ListBox界面控件</param>
        /// <returns></returns>
        public int write_listBox(ListBox listBox)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return json.write_listBox(listBox);
        }

        /// <summary>
        /// 将ArrayList集合写入到JSON
        /// </summary>
        /// <param name="arrayList">数据</param>
        /// <returns></returns>
        public int write_arrayList(ArrayList arrayList)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return json.write_arrayList(arrayList);
        }

        /// <summary>
        /// 将ArrayList集合写入到JSON
        /// </summary>
        /// <param name="arrayList">数据</param>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public int write_arrayList(ArrayList arrayList, string filePath)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            init(filePath);
            return json.write_arrayList(arrayList);
        }

        /// <summary>
        /// 将T集合写入到JSON
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        public int write_class<T>(T model)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return json.write_class(model);
        }

        /// <summary>
        /// 将T集合写入到JSON
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public int write_class<T>(T model,string filePath)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            init(filePath);
            return json.write_class(model);
        }

        /// <summary>
        /// 将listT集合写入到JSON
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="listT">数据</param>
        /// <returns></returns>
        public int write_listT<T>(List<T> listT)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return json.write_listT(listT);
        }

        /// <summary>
        /// 将listT集合写入到JSON
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="listT">数据</param>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public int write_listT<T>(List<T> listT, string filePath)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            init(filePath);
            return json.write_listT(listT);
        }

        /// <summary>
        /// 将listObj集合写入到JSON
        /// </summary>
        /// <param name="listObj">数据</param>
        /// <returns></returns>
        public int write_listObj(List<object> listObj)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return json.write_listObj(listObj);
        }

        /// <summary>
        /// 将listObj集合写入到JSON
        /// </summary>
        /// <param name="listObj">数据</param>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public int write_listObj(List<object> listObj, string filePath)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            init(filePath);
            return json.write_listObj(listObj);
        }

        /// <summary>
        /// 将listControl集合写入到JSON
        /// </summary>
        /// <param name="listControl"></param>
        /// <returns></returns>
        public int write_listControl(List<Control> listControl)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return json.write_listControl(listControl);
        }

        /// <summary>
        /// 将界面数据写入到JSON
        /// </summary>
        /// <param name="form">界面数据</param>
        /// <returns></returns>
        public int write_Control(Form form)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return json.write_Control(form);
        }

        /// <summary>
        /// 将DataGridView数据写入到JSON
        /// </summary>
        /// <param name="dataGridView">DataGridView控件数据</param>
        /// <returns></returns>
        public int write_dataGridView(DataGridView dataGridView)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return json.write_dataGridView(dataGridView);
        }

        /// <summary>
        /// 将DataTable数据写入到JSON
        /// </summary>
        /// <param name="dataTable">DataTable数据</param>
        /// <returns></returns>
        public int write_dataTable(DataTable dataTable)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return json.write_dataTable(dataTable);
        }

        #endregion

        #region 数据操作-读

        /// <summary>
        /// 未开发
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int read_string(ref string value)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            throw new NotImplementedException();
        }

        /// <summary>
        /// 未开发
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public int read_dictionary(ref Dictionary<object, object> data)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            throw new NotImplementedException();
        }

        /// <summary>
        /// 读取JSON数据-Array
        /// </summary>
        /// <param name="array">数组</param>
        /// <returns></returns>
        public int read_array(ref string[] array)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return json.read_array(ref array);
        }

        /// <summary>
        /// 读取JSON数据-Array
        /// </summary>
        /// <param name="array">数组</param>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public int read_array(ref string[] array,string filePath)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            init(filePath);
            return json.read_array(ref array);
        }

        /// <summary>
        /// 读取JSON数据-ListBox
        /// </summary>
        /// <param name="listBox">ListBox界面控件</param>
        /// <returns></returns>
        public int read_listBox(ref ListBox listBox)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return json.read_listBox(ref listBox);
        }

        /// <summary>
        /// 读取JSON数据-ArrayList
        /// </summary>
        /// <param name="arrayList">ArrayList集合</param>
        /// <returns></returns>
        public int read_arrayList(ref ArrayList arrayList)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return json.read_arrayList(ref arrayList);
        }

        /// <summary>
        /// 读取JSON数据-ArrayList
        /// </summary>
        /// <param name="arrayList">ArrayList集合</param>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public int read_arrayList(ref ArrayList arrayList, string filePath)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            init(filePath);
            return json.read_arrayList(ref arrayList);
        }

        /// <summary>
        /// 读取JSON数据-T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        public int read_class<T>(ref T model)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return json.read_class(ref model);
        }

        /// <summary>
        /// 读取JSON数据-T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public int read_class<T>(ref T model,string filePath)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            init(filePath);
            return json.read_class(ref model);
        }

        /// <summary>
        /// 读取JSON数据-ListT
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="listT">listT集合</param>
        /// <returns></returns>
        public int read_listT<T>(ref List<T> listT) where T : new()
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return json.read_listT(ref listT);
        }

        /// <summary>
        /// 读取JSON数据-ListT
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="listT">listT集合</param>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public int read_listT<T>(ref List<T> listT, string filePath) where T : new()
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            init(filePath);
            return json.read_listT(ref listT);
        }

        /// <summary>
        /// 读取JSON数据-ListObj
        /// </summary>
        /// <param name="listObj">listObj集合</param>
        /// <returns></returns>
        public int read_listObj(ref List<object> listObj)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return json.read_listObj(ref listObj);
        }

        /// <summary>
        /// 读取JSON数据-ListObj
        /// </summary>
        /// <param name="listObj">listObj集合</param>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public int read_listObj(ref List<object> listObj, string filePath)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            init(filePath);
            return json.read_listObj(ref listObj);
        }

        /// <summary>
        /// 读取JSON数据-ListControl
        /// </summary>
        /// <param name="listControl">list界面控件</param>
        /// <returns></returns>
        public int read_listControl(ref List<Control> listControl)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return json.read_listControl(ref listControl);
        }

        /// <summary>
        /// 读取JSON数据-界面
        /// </summary>
        /// <param name="form">界面</param>
        /// <returns></returns>
        public int read_Control(Form form)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return json.read_Control(form);
        }

        /// <summary>
        /// 读取JSON数据-DataGridView
        /// </summary>
        /// <param name="dataGridView">DataGridView控件</param>
        /// <returns></returns>
        public int read_dataGridView(ref DataGridView dataGridView)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return json.read_dataGridView(ref dataGridView);
        }

        /// <summary>
        /// 读取JSON数据-DataTable
        /// </summary>
        /// <param name="dataTable">DataTable数据集</param>
        /// <returns></returns>
        public int read_dataTable(ref DataTable dataTable)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return json.read_dataTable(ref dataTable);
        }

        #endregion

        /// <summary>
        /// 写ListString
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int write_list_string(List<string> value)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return json.write_list_string(value);
        }

        /// <summary>
        /// 写ListString
        /// </summary>
        /// <param name="value"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public int write_list_string(List<string> value,string filePath)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            init(filePath);
            return json.write_list_string(value);
        }

        /// <summary>
        ///  读ListString
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public int read_list_string(ref List<string> value)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return json.read_list_string(ref value);
        }

        /// <summary>
        ///  读ListString
        /// </summary>
        /// <param name="value"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public int read_list_string(ref List<string> value, string filePath)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            init(filePath);
            return json.read_list_string(ref value);
        }

    }
}
