using DotNet.Data.dao;
using DotNet.Data.daoImpl;
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

namespace DotNet.Data.action
{
    /// <summary>
    /// TXT操作类
    /// </summary>
    public class TxtAction : TxtDao
    {
        TxtDao txt = new TxtDaoImpl();

        public TxtAction() { }

        /// <summary>
        /// 构造函数设置路径
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public TxtAction(string filePath)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return;
            init(filePath);
        }

        /// <summary>
        /// 构造函数设置路径
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="isCover">写入是否覆盖</param>
        /// <returns></returns>
        public TxtAction(string filePath, bool isCover)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return;
            init(filePath, isCover);
        }

        /// <summary>
        /// 设置是否覆盖
        /// </summary>
        /// <param name="isCover">写入是否覆盖</param>
        /// <returns></returns>
        public void setCover(bool isCover)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return;
            txt.setCover(isCover);
        }

        /// <summary>
        /// 初始化TXT文件
        /// </summary>
        /// <param name="filePath">TXT文件路径</param>
        /// <returns></returns>
        public int init(string filePath)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.init(filePath);
        }

        /// <summary>
        /// 初始化TXT文件
        /// </summary>
        /// <param name="filePath">TXT文件路径</param>
        /// <param name="isCover">写入是否覆盖</param>
        /// <returns></returns>
        public int init(string filePath, bool isCover)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.init(filePath, isCover);
        }

        /// <summary>
        /// 打开log文本
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public int open_log_text(string filePath)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.open_log_text(filePath);
        }

        /// <summary>
        /// 关闭log文本
        /// </summary>
        /// <returns></returns>
        public int close_log_text()
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.close_log_text();
        }

        /// <summary>
        /// 写入log
        /// </summary>
        /// <param name="value">值</param>
        /// <returns></returns>
        public int write_txt_log(string value)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.write_txt_log(value);
        }

        #region 数据操作-写

        /// <summary>
        /// 将字符串写入到TXT
        /// </summary>
        /// <param name="value">值</param>
        /// <returns></returns>
        public int write_string(string value)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.write_string(value);
        }

        /// <summary>
        /// 将数组写入到TXT
        /// </summary>
        /// <param name="array">数组数据</param>
        /// <returns></returns>
        public int write_array(string[] array)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.write_array(array);
        }

        /// <summary>
        /// 将界面ListBox写入到TXT
        /// </summary>
        /// <param name="listBox">界面ListBox</param>
        /// <returns></returns>
        public int write_listBox(ListBox listBox)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.write_listBox(listBox);
        }

        /// <summary>
        /// 将字典写入到TXT
        /// </summary>
        /// <param name="data">字典数据</param>
        /// <returns></returns>
        public int write_dictionary(Dictionary<object, object> data)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.write_dictionary(data);
        }

        /// <summary>
        /// 将ArrayList集合写入到TXT
        /// </summary>
        /// <param name="arrayList">ArrayList集合</param>
        /// <returns></returns>
        public int write_arrayList(ArrayList arrayList)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.write_arrayList(arrayList);
        }

        /// <summary>
        /// 写入表头
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        public int write_DataHeader<T>(T model)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.write_DataHeader(model);
        }
            /// <summary>
            /// 将泛型集合写入到TXT
            /// </summary>
            /// <typeparam name="T">泛型</typeparam>
            /// <param name="listT">泛型集合</param>
            /// <returns></returns>
        public int write_listT<T>(List<T> listT)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.write_listT(listT);
        }

        /// <summary>
        /// 将ListObj集合写入到TXT
        /// </summary>
        /// <param name="listObj">ListObj集合</param>
        /// <returns></returns>
        public int write_listObj(List<object> listObj)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.write_listObj(listObj);
        }

        /// <summary>
        /// 将界面控件参数写入到TXT
        /// </summary>
        /// <param name="listControl">listControl集合</param>
        /// <returns></returns>
        public int write_listControl(List<Control> listControl)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.write_listControl(listControl);
        }

        /// <summary>
        /// 将全部界面的参数写入到TXT
        /// </summary>
        /// <param name="form">界面控件集合</param>
        public int write_Control(Form form)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.write_Control(form);
        }

        /// <summary>
        /// 将DataGridView的值写入到TXT
        /// </summary>
        /// <param name="dataGridView">dataGridView数据</param>
        /// <returns></returns>
        public int write_dataGridView(DataGridView dataGridView)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.write_dataGridView(dataGridView);
        }

        /// <summary>
        /// 将DataTable的值写入到TXT
        /// </summary>
        /// <param name="dataTable">DataTable数据</param>
        /// <returns></returns>
        public int write_dataTable(DataTable dataTable)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.write_dataTable(dataTable);
        }


        #endregion

        #region 数据操作-读

        /// <summary>
        /// 读取TXT的值-string
        /// </summary>
        /// <param name="value">值</param>
        /// <returns></returns>
        public int read_string(ref string value)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.read_string(ref value);
        }

        /// <summary>
        /// 读取TXT的值-string[]
        /// </summary>
        /// <param name="array">数组</param>
        /// <returns></returns>
        public int read_array(ref string[] array)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.read_array(ref array);
        }

        /// <summary>
        /// 读取TXT的值-listBox
        /// </summary>
        /// <param name="listBox">listBox界面控件</param>
        /// <returns></returns>
        public int read_listBox(ref ListBox listBox)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.read_listBox(ref listBox);
        }

        /// <summary>
        /// 读取TXT的值-字典
        /// </summary>
        /// <param name="data">字典数据</param>
        /// <returns></returns>
        public int read_dictionary(ref Dictionary<object, object> data)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.read_dictionary(ref data);
        }

        /// <summary>
        /// 读取TXT的值-ArrayList
        /// </summary>
        /// <param name="arrayList">ArrayList集合</param>
        /// <returns></returns>
        public int read_arrayList(ref ArrayList arrayList)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.read_arrayList(ref arrayList);
        }

        /// <summary>
        /// 读取TXT的值-listT
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="listT">泛型集合</param>
        /// <returns></returns>
        public int read_listT<T>(ref List<T> listT) where T : new()
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.read_listT(ref listT);
        }

        /// <summary>
        /// 读取TXT的值-Listobject
        /// </summary>
        /// <param name="listObj">listObj集合</param>
        /// <returns></returns>
        public int read_listObj(ref List<object> listObj)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.read_listObj(ref listObj);
        }

        /// <summary>
        /// 读取TXT的值-ListControl
        /// </summary>
        /// <param name="listControl">界面控件参数集合</param>
        /// <returns></returns>
        public int read_listControl(ref List<Control> listControl)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.read_listControl(ref listControl);
        }

        /// <summary>
        /// 读取TXT的值-界面
        /// </summary>
        /// <param name="form">界面</param>
        /// <returns></returns>
        public int read_Control(Form form)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.read_Control(form);
        }

        /// <summary>
        /// 读取TXT的值-DataGridView
        /// </summary>
        /// <param name="dataGridView">DataGridView界面控件</param>
        /// <returns></returns>
        public int read_dataGridView(ref DataGridView dataGridView)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.read_dataGridView(ref dataGridView);
        }

        /// <summary>
        /// 读取TXT的值-DataTable
        /// </summary>
        /// <param name="dataTable">DataTable数据集</param>
        /// <returns></returns>
        public int read_dataTable(ref DataTable dataTable)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return txt.read_dataTable(ref dataTable);
        }

        #endregion

    }
}
