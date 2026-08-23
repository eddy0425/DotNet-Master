using DotNet.Data.dao;
using DotNet.Data.daoImpl;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DotNet.Data.action
{
    /// <summary>
    /// XML操作类
    /// </summary>
    public class XmlAction : XmlDao
    {
        XmlDao xml = new XmlDaoImpl();

        /// <summary>
        /// 构造函数
        /// </summary>
        public XmlAction() { /*if (!DotNet.Licensing.Client.LicensingLib.validation()) return;*/ }

        /// <summary>
        /// 构造函数设置路径
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public XmlAction(string filePath)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return;
            init(filePath);
        }

        /// <summary>
        /// 初始化XML文件
        /// </summary>
        /// <param name="filePath">XML文件路径</param>
        /// <returns></returns>
        public int init(string filePath)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return xml.init(filePath);
        }

        #region 数据操作-写

        /// <summary>
        /// 将数组写入到XML
        /// </summary>
        /// <param name="array">数据</param>
        /// <returns></returns>
        public int write_array(string[] array)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return xml.write_array(array);
        }

        /// <summary>
        /// 将ListBox数据写入到XML
        /// </summary>
        /// <param name="listBox">ListBox界面控件</param>
        /// <returns></returns>
        public int write_listBox(ListBox listBox)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return xml.write_listBox(listBox);
        }

        /// <summary>
        /// 将ArrayList集合写入到XML
        /// </summary>
        /// <param name="arrayList">数据</param>
        /// <returns></returns>
        public int write_arrayList(ArrayList arrayList)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return xml.write_arrayList(arrayList);
        }
        /// <summary>
        /// 将ArrayList集合写入到XML2
        /// </summary>
        /// <param name="arrayList">数据</param>
        /// <returns></returns>
        public int write_arrayList2(ArrayList arrayList)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return xml.write_arrayList2(arrayList);
        }

        /// <summary>
        /// 将listT集合写入到XML
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="listT">数据</param>
        /// <returns></returns>
        public int write_listT<T>(List<T> listT)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return xml.write_listT(listT);
        }

        /// <summary>
        /// 将listT集合写入到XML2
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="listT">数据</param>
        /// <returns></returns>
        public int write_listT2<T>(List<T> listT)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return xml.write_listT2(listT);
        }

        /// <summary>
        /// 将listObj集合写入到XML
        /// </summary>
        /// <param name="listObj">数据</param>
        /// <returns></returns>
        public int write_listObj(List<object> listObj)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return xml.write_listObj(listObj);
        }

        /// <summary>
        /// 将listObj集合写入到XML2
        /// </summary>
        /// <param name="listObj">数据</param>
        /// <returns></returns>
        public int write_listObj2(List<object> listObj)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return xml.write_listObj2(listObj);
        }

        /// <summary>
        /// 将listControl集合写入到XML
        /// </summary>
        /// <param name="listControl"></param>
        /// <returns></returns>
        public int write_listControl(List<Control> listControl)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return xml.write_listControl(listControl);
        }

        /// <summary>
        /// 将界面数据写入到XML
        /// </summary>
        /// <param name="form">界面数据</param>
        /// <returns></returns>
        public int write_Control(Form form)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return xml.write_Control(form);
        }

        /// <summary>
        /// 将DataGridView数据写入到XML
        /// </summary>
        /// <param name="dataGridView">DataGridView控件数据</param>
        /// <returns></returns>
        public int write_dataGridView(DataGridView dataGridView)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return xml.write_dataGridView(dataGridView);
        }

        /// <summary>
        /// 将DataTable数据写入到XML
        /// </summary>
        /// <param name="dataTable">DataTable数据</param>
        /// <returns></returns>
        public int write_dataTable(DataTable dataTable)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return xml.write_dataTable(dataTable);
        }

        #endregion

        #region 数据操作-读

        /// <summary>
        /// 读取XML数据-Array
        /// </summary>
        /// <param name="array">数组</param>
        /// <returns></returns>
        public int read_array(ref string[] array)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return xml.read_array(ref array);
        }

        /// <summary>
        /// 读取XML数据-ListBox
        /// </summary>
        /// <param name="listBox">ListBox界面控件</param>
        /// <returns></returns>
        public int read_listBox(ref ListBox listBox)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return xml.read_listBox(ref listBox);
        }

        /// <summary>
        /// 读取XML数据-ArrayList
        /// </summary>
        /// <param name="arrayList">ArrayList集合</param>
        /// <returns></returns>
        public int read_arrayList(ref ArrayList arrayList)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return xml.read_arrayList(ref arrayList);
        }

        /// <summary>
        /// 读取XML数据-ArrayList2
        /// </summary>
        /// <param name="arrayList">ArrayList集合</param>
        /// <returns></returns>
        public int read_arrayList2(ref ArrayList arrayList)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return xml.read_arrayList2(ref arrayList);
        }

        /// <summary>
        /// 读取XML数据-ListT
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="listT">listT集合</param>
        /// <returns></returns>
        public int read_listT<T>(ref List<T> listT) where T : new()
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return xml.read_listT(ref listT);
        }

        /// <summary>
        /// 读取XML数据-ListT
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="listT">listT集合</param>
        /// <returns></returns>
        public int read_listT2<T>(ref List<T> listT) where T : new()
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return xml.read_listT2(ref listT);
        }

        /// <summary>
        /// 读取XML数据-ListObj
        /// </summary>
        /// <param name="listObj">listObj集合</param>
        /// <returns></returns>
        public int read_listObj(ref List<object> listObj)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return xml.read_listObj(ref listObj);
        }

        /// <summary>
        /// 读取XML数据-ListObj
        /// </summary>
        /// <param name="listObj">listObj集合</param>
        /// <returns></returns>
        public int read_listObj2(ref List<object> listObj)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return xml.read_listObj2(ref listObj);
        }

        /// <summary>
        /// 读取XML数据-ListObj2
        /// </summary>
        /// <param name="listObj">listObj集合</param>
        /// <param name="assembly">程序集</param>
        /// <returns></returns>
        public int read_listObj2(ref List<object> listObj, Assembly assembly)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return xml.read_listObj2(ref listObj, assembly);
        }

        /// <summary>
        /// 读取XML数据-ListControl
        /// </summary>
        /// <param name="listControl"></param>
        /// <returns></returns>
        public int read_listControl(ref List<Control> listControl)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return xml.read_listControl(ref listControl);
        }

        /// <summary>
        /// 读取XML数据-界面
        /// </summary>
        /// <param name="form">界面</param>
        /// <returns></returns>
        public int read_Control(Form form)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return xml.read_Control(form);
        }

        /// <summary>
        /// 读取XML数据-DataGridView
        /// </summary>
        /// <param name="dataGridView">DataGridView控件</param>
        /// <returns></returns>
        public int read_dataGridView(ref DataGridView dataGridView)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return xml.read_dataGridView(ref dataGridView);
        }

        /// <summary>
        /// 读取XML数据-DataTable
        /// </summary>
        /// <param name="dataTable">DataTable数据集</param>
        /// <returns></returns>
        public int read_dataTable(ref DataTable dataTable)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return xml.read_dataTable(ref dataTable);
        }

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

        #endregion
    }
}
