using DotNet.Data.dao;
using DotNet.Data.daoImpl;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DotNet.Data.action
{
    /// <summary>
    /// ini操作类
    /// </summary>
    public class IniAction : IniDao
    {
        IniDao ini = new IniDaoImpl();

        /// <summary>
        /// 构造函数设置路径
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public IniAction(string filePath)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return;
            init(filePath);
        }

        /// <summary>
        /// 构造函数设置路径
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="section">项目名称(如 [section])</param>
        /// <returns></returns>
        public IniAction(string filePath, string section)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return;
            init(filePath, section);
        }

        /// <summary>
        /// 设置项目名称(如 [section])
        /// </summary>
        /// <param name="section">项目名称(如 [section])</param>
        /// <returns></returns>
        public void setSection(string section)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return;
            ini.setSection(section);
        }

        /// <summary>
        /// 初始化INI文件
        /// </summary>
        /// <param name="filePath">INI文件路径</param>
        /// <returns></returns>
        public int init(string filePath)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return ini.init(filePath);
        }

        /// <summary>
        /// 初始化INI文件
        /// </summary>
        /// <param name="filePath">INI文件路径</param>
        /// <param name="section">项目名称(如 [section])</param>
        /// <returns></returns>
        public int init(string filePath, string section)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return ini.init(filePath, section);
        }

        /// <summary>
        /// 判断INI节点是否存在
        /// </summary>
        /// <param name="section">项目名称(如 [section])</param>
        /// <param name="key">键</param>
        /// <returns></returns>
        public int KeyExists(string section, string key)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return ini.KeyExists(section,key);
        }

        #region 数据操作-写

        /// <summary>
        /// 将字符串写入到INI
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public int write_string(string key, string value)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return ini.write_string(key, value);
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
        /// 将数组写入到INI
        /// </summary>
        /// <param name="array">数组数据</param>
        /// <returns></returns>
        public int write_array(string[] array)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return ini.write_array(array);
        }

        /// <summary>
        /// 将ListBox写入到INI
        /// </summary>
        /// <param name="listBox">界面控件</param>
        /// <returns></returns>
        public int write_listBox(ListBox listBox)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return ini.write_listBox(listBox);
        }

        /// <summary>
        /// 将字典写入到INI
        /// </summary>
        /// <param name="data">字典数据</param>
        /// <returns></returns>
        public int write_dictionary(Dictionary<object, object> data)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return ini.write_dictionary(data);
        }

        /// <summary>
        /// 将ArrayList集合写入到INI
        /// </summary>
        /// <param name="arrayList">ArrayList集合</param>
        /// <returns></returns>
        public int write_arrayList(ArrayList arrayList)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return ini.write_arrayList(arrayList);
        }

        /// <summary>
        /// 将泛型集合写入到INI
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="listT">泛型集合</param>
        /// <returns></returns>
        public int write_listT<T>(List<T> listT)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return ini.write_listT(listT);
        }

        /// <summary>
        /// 将ListObj集合写入到INI
        /// </summary>
        /// <param name="listObj">ListObj集合</param>
        /// <returns></returns>
        public int write_listObj(List<object> listObj)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return ini.write_listObj(listObj);
        }

        /// <summary>
        /// 将界面控件参数写入到INI
        /// </summary>
        /// <param name="listControl">listControl集合</param>
        /// <returns></returns>
        public int write_listControl(List<Control> listControl)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return ini.write_listControl(listControl);
        }
        /// <summary>
        /// 将全部界面的参数写入到INI
        /// </summary>
        /// <param name="form">界面控件集合</param>
        public int write_Control(Form form)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return ini.write_Control(form);
        }

        /// <summary>
        /// 将DataGridView的值写入到INI
        /// </summary>
        /// <param name="dataGridView">dataGridView数据</param>
        public int write_dataGridView(DataGridView dataGridView)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return ini.write_dataGridView(dataGridView);
        }

        /// <summary>
        /// 将DataTable的值写入到INI
        /// </summary>
        /// <param name="dataTable">DataTable数据</param>
        public int write_dataTable(DataTable dataTable)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return ini.write_dataTable(dataTable);
        }

        #endregion

        #region 数据操作-读

        /// <summary>
        /// 读取INI的值-string
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        public int read_string(string key, ref string value)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return ini.read_string( key, ref value);
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
        /// 读取INI的值-string[]
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public int read_array(ref string[] array)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return ini.read_array(ref array);
        }

        /// <summary>
        /// 读取INI的值-listBox
        /// </summary>
        /// <param name="listBox">listBox界面控件</param>
        /// <returns></returns>
        public int read_listBox(ref ListBox listBox)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return ini.read_listBox(ref listBox);
        }

        /// <summary>
        /// 读取INI的值-字典
        /// </summary>
        /// <param name="data">字典数据</param>
        /// <returns></returns>
        public int read_dictionary(ref Dictionary<object, object> data)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return ini.read_dictionary(ref data);
        }

        /// <summary>
        /// 读取INI的值-ArrayList
        /// </summary>
        /// <param name="arrayList">ArrayList集合</param>
        /// <returns></returns>
        public int read_arrayList(ref ArrayList arrayList)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return ini.read_arrayList(ref arrayList);
        }

        /// <summary>
        /// 读取INI的值-listT
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="listT">泛型集合</param>
        /// <returns></returns>
        public int read_listT<T>(ref List<T> listT) where T : new()
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return ini.read_listT(ref listT);
        }

        /// <summary>
        /// 读取INI的值-Listobject
        /// </summary>
        /// <param name="listObj">listObj集合</param>
        /// <returns></returns>
        public int read_listObj(ref List<object> listObj)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return ini.read_listObj(ref listObj);
        }

        /// <summary>
        /// 读取INI的值-ListControl
        /// </summary>
        /// <param name="listControl">界面控件参数集合</param>
        /// <returns></returns>
        public int read_listControl(ref List<Control> listControl)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return ini.read_listControl(ref listControl);
        }

        /// <summary>
        /// 读取INI的值-界面
        /// </summary>
        /// <param name="form">界面</param>
        /// <returns></returns>
        public int read_Control(Form form)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return ini.read_Control(form);
        }

        /// <summary>
        /// 读取INI的值-DataGridView
        /// </summary>
        /// <param name="dataGridView">DataGridView界面控件</param>
        /// <returns></returns>
        public int read_dataGridView(ref DataGridView dataGridView)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return ini.read_dataGridView(ref dataGridView);
        }

        /// <summary>
        /// 读取INI的值-DataTable
        /// </summary>
        /// <param name="dataTable">DataTable数据集</param>
        /// <returns></returns>
        public int read_dataTable(ref DataTable dataTable)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return ini.read_dataTable(ref dataTable);
        }


        #endregion


    }
}
