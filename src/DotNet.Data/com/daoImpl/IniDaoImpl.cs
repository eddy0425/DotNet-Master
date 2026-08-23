using DotNet.Data.dao;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace DotNet.Data.daoImpl
{
    class IniDaoImpl : IniDao
    {
        private const string LogTag = "IniDaoImpl";

        /// <summary>
        /// 将字符串复制到初始化文件的指定节中.
        /// </summary>
        /// <param name="section">项目名称(如 [section])</param>
        /// <param name="key">键</param>
        /// <param name="val">值</param>
        /// <param name="filePath">文件路径</param>
        /// <returns>
        /// 如果函数成功地将字符串复制到初始化文件中，则返回值为非零
        /// 如果函数失败，或者刷新最近访问的初始化文件的缓存版本，则返回值为零。
        /// </returns>
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);

        /// <summary>
        /// 从初始化文件中的指定节检索字符串.
        /// </summary>
        /// <param name="section">项目名称(如 [section])</param>
        /// <param name="key">键</param>
        /// <param name="def">无法读取时候时候的缺省数值</param>
        /// <param name="val">值</param>
        /// <param name="size">数值的大小</param>
        /// <param name="filePath">文件路径</param>
        /// <returns>
        /// 返回值是复制到缓冲区的字符数，不包括结束的空字符
        /// </returns>
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder val, int size, string filePath);

        /// <summary>
        /// 检索初始化文件的指定部分的所有键和值。
        /// </summary>
        /// <param name="lpAppName"></param>
        /// <param name="lpszReturnBuffer"></param>
        /// <param name="nSize"></param>
        /// <param name="lpFileName"></param>
        /// <returns>
        /// 返回值是复制到缓冲区的字符数，不包括结束的空字符。
        /// </returns>
        [DllImport("kernel32.dll")]
        private static extern int GetPrivateProfileSection(string lpAppName, byte[] lpszReturnBuffer, int nSize, string lpFileName);

        /// <summary>
        /// 获取所有节点名称
        /// </summary>
        /// <param name="lpszReturnBuffer">存放节点名称的内存地址,每个节点之间用\0分隔</param>
        /// <param name="nSize">内存大小(characters)</param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        [DllImport("kernel32", CharSet = CharSet.Auto)]
        private static extern uint GetPrivateProfileSectionNames(IntPtr lpszReturnBuffer, uint nSize, string filePath);


        string iniPath = null;  //文件路径
        const int SIZE = 255;
        string section;

        /// <summary>
        /// 设置项目名称(如 [section])
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        public void setSection(string section) 
        {
            this.section = section;
        }

        /// <summary>
        /// 初始化INI文件
        /// </summary>
        /// <param name="filePath">INI文件路径</param>
        /// <returns></returns>
        public int init(string filePath)
        {
            try
            {
                this.iniPath = filePath;

                int index = filePath.LastIndexOf("\\");
                string FilePath = filePath.Substring(0, index);
                if (!Directory.Exists(FilePath))
                {
                    Directory.CreateDirectory(FilePath);
                }
                if (!File.Exists(filePath))
                {
                    File.Create(filePath).Dispose();  //创建该文件
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "初始化INI文件出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 初始化INI文件
        /// </summary>
        /// <param name="filePath">INI文件路径</param>
        /// <param name="section">项目名称(如 [section])</param>
        /// <returns></returns>
        public int init(string filePath, string section)
        {
            try
            {
                this.iniPath = filePath;

                int index = filePath.LastIndexOf("\\");
                string FilePath = filePath.Substring(0, index);
                if (!Directory.Exists(FilePath))
                {
                    Directory.CreateDirectory(FilePath);
                }
                if (!File.Exists(filePath))
                {
                    File.Create(filePath).Dispose();  //创建该文件
                }
                this.section = section;
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "初始化INI文件出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 判断INI节点是否存在
        /// </summary>
        /// <param name="section">项目名称(如 [section])</param>
        /// <param name="key">键</param>
        /// <returns>
        /// i>0 == true
        /// </returns> 
        public int KeyExists(string section, string key)
        {
            int i = GetPrivateProfileString(section, key, null, new StringBuilder(SIZE), SIZE, this.iniPath);
            return i;
        }

        /// <summary>
        /// 获取指定ini文件中所有节点名称
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private string[] ReadIniAllSectionName(string filePath)
        {
            uint MaxBuffer = 32767;
            string[] sections = new string[0];  // 返回值
                                                // 申请内存
            IntPtr pReturnedString = Marshal.AllocCoTaskMem((int)MaxBuffer * sizeof(char));
            uint byteReturned = GetPrivateProfileSectionNames(pReturnedString, MaxBuffer, filePath);
            if (byteReturned != 0)
            {
                // 读取指定内存内容
                string local = Marshal.PtrToStringAuto(pReturnedString, (int)byteReturned).ToString();
                // 每个节点之间用\0分隔,末尾有一个\0 
                sections = local.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
            }

            return sections;
        }

        #region 数据操作-写

        /// <summary>
        /// 将字符串写入到INI
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns>
        /// i>0 == true
        /// </returns> 
        public int write_string(string key, string value)
        {
            try
            {
                long l = WritePrivateProfileString(section, key, value, this.iniPath);
                return (int)l;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将字符串写入到INI,出错！！");
                return -1;
            }

        }

        /// <summary>
        /// 将数组写入到INI
        /// </summary>
        /// <param name="array">数组数据</param>
        /// <returns></returns>
        public int write_array(string[] array)
        {
            try
            {
                for (int i = 0; i < array.Length; i++)
                {
                    WritePrivateProfileString(section, "key" + i, array[i].ToString(), this.iniPath);
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将数组写入到INI,出错！！");
                return -1;
            }
        }

        /// <summary>
        /// 将ListBox写入到INI
        /// </summary>
        /// <param name="listBox">界面控件</param>
        /// <returns></returns>
        public int write_listBox(ListBox listBox)
        {
            try
            {
                int i = 0;
                foreach (string row in listBox.Items)
                {
                    WritePrivateProfileString(section, "key" + i, row, this.iniPath);
                    i++;
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将ListBox写入到INI,出错！！");
                return -1;
            }
        }

        /// <summary>
        /// 将字典写入到INI
        /// </summary>
        /// <param name="data">字典数据</param>
        /// <returns></returns>
        public int write_dictionary(Dictionary<object, object> data)
        {
            try
            {
                object[] keys = data.Keys.ToArray();
                for (int i = 0; i < data.Count; i++)
                {
                    WritePrivateProfileString(section, keys[i].ToString(), data[keys[i]].ToString(), this.iniPath);
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将字典写入到INI,出错！！");
                return -1;
            }
        }

        /// <summary>
        /// 将ArrayList集合写入到INI
        /// </summary>
        /// <param name="arrayList">ArrayList集合</param>
        /// <returns></returns>
        public int write_arrayList(ArrayList arrayList)
        {
            try
            {
                for (int i = 0; i < arrayList.Count; i++)
                {
                    foreach (PropertyInfo pi in arrayList[i].GetType().GetProperties())
                    {
                        WritePrivateProfileString(arrayList[i].GetType().ToString() + "," + i, pi.Name, pi.GetValue(arrayList[i], null).ToString(), this.iniPath);
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将ArrayList集合写入到INI,出错！！");
                return -1;
            }
        }

        /// <summary>
        /// 将泛型集合写入到INI
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="listT">泛型集合</param>
        /// <returns></returns>
        public int write_listT<T>(List<T> listT)
        {
            try
            {
                for (int i = 0; i < listT.Count; i++)
                {
                    foreach (PropertyInfo pi in listT[i].GetType().GetProperties())
                    {
                        WritePrivateProfileString(listT[i].GetType().ToString() + "," + i, pi.Name, pi.GetValue(listT[i], null).ToString(), this.iniPath);
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将泛型集合写入到INI,出错！！");
                return -1;
            }
        }

        /// <summary>
        /// 将ListObj集合写入到INI
        /// </summary>
        /// <param name="listObj">ListObj集合</param>
        /// <returns></returns>
        public int write_listObj(List<object> listObj)
        {
            try
            {
                for (int i = 0; i < listObj.Count; i++)
                {
                    foreach (PropertyInfo pi in listObj[i].GetType().GetProperties())
                    {
                        WritePrivateProfileString(listObj[i].GetType().ToString() + "," + i, pi.Name, pi.GetValue(listObj[i], null).ToString(), this.iniPath);
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将ListObj集合写入到INI,出错！！");
                return -1;
            }
        }

        /// <summary>
        /// 将界面控件参数写入到INI
        /// </summary>
        /// <param name="listControl">listControl集合</param>
        /// <returns></returns>
        public int write_listControl(List<Control> listControl)
        {
            try
            {
                for (int i = 0; i < listControl.Count; i++)
                {
                    if (listControl[i].GetType().Name == "RadioButton")
                    {
                        RadioButton radioButton = (RadioButton)listControl[i];
                        WritePrivateProfileString(listControl[i].GetType().Name, radioButton.Name, radioButton.Checked.ToString(), this.iniPath);
                    }
                    else if (listControl[i].GetType().Name == "CheckBox")
                    {
                        CheckBox checkBox = (CheckBox)listControl[i];
                        WritePrivateProfileString(listControl[i].GetType().Name, checkBox.Name, checkBox.Checked.ToString(), this.iniPath);
                    }
                    else if (listControl[i].GetType().Name == "ToolStripMenuItem")
                    {
                        goto EndHandle;
                    }
                    else if (listControl[i].GetType().Name == "DataGridViewTextBoxColumn" || listControl[i].GetType().Name == "DataGridViewComboBoxColumn" || listControl[i].GetType().Name == "DataGridViewButtonColumn")
                    {
                        goto EndHandle;
                    }
                    else
                    {
                        WritePrivateProfileString(listControl[i].GetType().Name, listControl[i].Name, listControl[i].Text, this.iniPath);
                    }
                    EndHandle: { }
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将界面控件参数写入到INI,出错！！");
                return -1;
            }
        }

        /// <summary>
        /// 将全部界面的参数写入到INI
        /// </summary>
        /// <param name="form">界面控件集合</param>
        public int write_Control(Form form)
        {
            try
            {
                FieldInfo[] fieldInfo = form.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance); //反射

                for (int i = 0; i < fieldInfo.Length; i++)
                {
                    if (fieldInfo[i].FieldType.Namespace == "System.Windows.Forms")
                    {
                        if (fieldInfo[i].FieldType.Name == "RadioButton")
                        {
                            RadioButton con = (RadioButton)form.GetType().GetField(fieldInfo[i].Name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                            WritePrivateProfileString(fieldInfo[i].FieldType.Name, con.Name, con.Checked.ToString(), this.iniPath);
                        }
                        else if (fieldInfo[i].FieldType.Name == "CheckBox")
                        {
                            CheckBox con = (CheckBox)form.GetType().GetField(fieldInfo[i].Name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                            WritePrivateProfileString(fieldInfo[i].FieldType.Name, con.Name, con.Checked.ToString(), this.iniPath);
                        }
                        else if (fieldInfo[i].FieldType.Name == "ToolStripMenuItem")
                        {
                            goto EndHandle;
                        }
                        else
                        {
                            Control con = (Control)form.GetType().GetField(fieldInfo[i].Name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                            WritePrivateProfileString(fieldInfo[i].FieldType.Name, con.Name, con.Text, this.iniPath);
                        }
                        EndHandle: { }
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将全部界面的参数写入到INI,出错！！");
                return -1;
            }
        }

        /// <summary>
        /// 将DataGridView的值写入到INI
        /// </summary>
        /// <param name="dataGridView">dataGridView数据</param>
        public int write_dataGridView(DataGridView dataGridView)
        {
            try
            {
                for (int i = 0; i < dataGridView.Rows.Count; i++)
                {
                    for (int j = 0; j < dataGridView.Columns.Count; j++)
                    {
                        WritePrivateProfileString(section + "," + i, dataGridView.Columns[j].HeaderText, dataGridView[j, i].Value.ToString(), this.iniPath);
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将DataGridView的值写入到INI,出错！！");
                return -1;
            }
        }

        /// <summary>
        /// 将DataTable的值写入到INI
        /// </summary>
        /// <param name="dataTable">DataTable数据</param>
        public int write_dataTable(DataTable dataTable)
        {
            try
            {
                for (int i = 0; i < dataTable.Rows.Count; i++)
                {

                    for (int j = 0; j < dataTable.Columns.Count; j++)
                    {
                        WritePrivateProfileString(section + "," + i, dataTable.Columns[j].ToString(), dataTable.Rows[i].ItemArray[j].ToString(), this.iniPath);
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将DataTable的值写入到INI,出错！！");
                return -1;
            }
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
            try
            {
                long isOK = 0;
                if (KeyExists(section,key) > 0)
                {
                    var temp = new StringBuilder(SIZE);
                    isOK = GetPrivateProfileString(section, key, null, temp, SIZE, this.iniPath);
                    value = temp.ToString();
                }
                return (int)isOK;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取INI的值-string,出错！！");
                return -1;
            }

        }

        /// <summary>
        /// 读取INI的值-string[]
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public int read_array(ref string[] array)
        {
            try
            {
                var buffer = new byte[2048];
                GetPrivateProfileSection(section, buffer, 2048, this.iniPath);
                var tmp = Encoding.Default.GetString(buffer).Trim('\0').Split('\0');

                int i = 0;
                foreach (var entry in tmp)
                {
                    var s = entry.Split(new string[] { "=" }, 2, StringSplitOptions.None);
                    array[i] = s[1];
                    i++;
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取INI的值-string[],出错！！");
                return -1;
            }
        }

        /// <summary>
        /// 读取INI的值-listBox
        /// </summary>
        /// <param name="listBox">listBox界面控件</param>
        /// <returns></returns>
        public int read_listBox(ref ListBox listBox)
        {
            try
            {
                var buffer = new byte[2048];
                GetPrivateProfileSection(section, buffer, 2048, this.iniPath);
                var tmp = Encoding.Default.GetString(buffer).Trim('\0').Split('\0');

                int i = 0;
                foreach (var entry in tmp)
                {
                    var s = entry.Split(new string[] { "=" }, 2, StringSplitOptions.None);
                    listBox.Items.Add(s[1]);
                    i++;
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取INI的值-listBox,出错！！");
                return -1;
            }
        }

        /// <summary>
        /// 读取INI的值-字典
        /// </summary>
        /// <param name="data">字典数据</param>
        /// <returns></returns>
        public int read_dictionary(ref Dictionary<object, object> data)
        {
            try
            {
                var buffer = new byte[2048];
                GetPrivateProfileSection(section, buffer, 2048, this.iniPath);
                var tmp = Encoding.Default.GetString(buffer).Trim('\0').Split('\0');

                foreach (var entry in tmp)
                {
                    var s = entry.Split(new string[] { "=" }, 2, StringSplitOptions.None);
                    data.Add(s[0], s[1]);
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取INI的值-字典,出错！！");
                return -1;
            }
        }

        /// <summary>
        /// 读取INI的值-ArrayList
        /// </summary>
        /// <param name="arrayList">ArrayList集合</param>
        /// <returns></returns>
        public int read_arrayList(ref ArrayList arrayList)
        {
            try
            {
                string[] sections = ReadIniAllSectionName(this.iniPath);
                var nameSpaceAll = Assembly.GetEntryAssembly().GetTypes();

                for (int i = 0; i < sections.Length; i++)
                {
                    if (sections[i].Contains(","))
                    {
                        string className = sections[i].Remove(sections[i].IndexOf(','));
                        Type t = nameSpaceAll.Where(x => x.Name == className.ToString()).FirstOrDefault();

                        if (t != null)
                        {
                            object obj = Activator.CreateInstance(t);

                            var buffer = new byte[2048];
                            GetPrivateProfileSection(sections[i].ToString(), buffer, 2048, this.iniPath);
                            var tmp = Encoding.Default.GetString(buffer).Trim('\0').Split('\0');

                            string[][] keyValue = null;
                            Dictionary<string, string> values = null;
                            getKeyValue(tmp, ref keyValue, ref values);

                            foreach (PropertyInfo pi in obj.GetType().GetProperties())
                            {
                                if (pi.PropertyType.Name == "Int32")
                                {
                                    pi.SetValue(obj, int.Parse(values[pi.Name]), null);
                                }
                                else
                                {
                                    pi.SetValue(obj, values[pi.Name], null);
                                }
                            }
                            arrayList.Add(obj);
                        }
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取INI的值-ArrayList,出错！！");
                return -1;
            }
        }

        /// <summary>
        /// 读取INI的值-listT
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="listT">泛型集合</param>
        /// <returns></returns>
        public int read_listT<T>(ref List<T> listT) where T : new()
        {
            try
            {
                string[] sections = ReadIniAllSectionName(this.iniPath);

                T model1 = new T();

                for (int i = 0; i < sections.Length; i++)
                {
                    if (sections[i].Contains(model1.GetType().Name + ","))
                    {
                        var buffer = new byte[2048];
                        GetPrivateProfileSection(sections[i], buffer, 2048, this.iniPath);
                        var tmp = Encoding.Default.GetString(buffer).Trim('\0').Split('\0');

                        string[][] keyValue = null;
                        Dictionary<string, string> values = null;
                        getKeyValue(tmp, ref keyValue, ref values);

                        T model = new T();
                        foreach (PropertyInfo pi in model.GetType().GetProperties())
                        {
                            if (pi.PropertyType.Name == "Int32")
                            {
                                pi.SetValue(model, int.Parse(values[pi.Name]), null);
                            }
                            else
                            {
                                pi.SetValue(model, values[pi.Name], null);
                            }
                        }
                        listT.Add(model);
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取INI的值-listT,出错！！");
                return -1;
            }

        }

        /// <summary>
        /// 读取INI的值-Listobject
        /// </summary>
        /// <param name="listObj">listObj集合</param>
        /// <returns></returns>
        public int read_listObj(ref List<object> listObj)
        {
            try
            {
                string[] sections = ReadIniAllSectionName(this.iniPath);
                var nameSpaceAll = Assembly.GetEntryAssembly().GetTypes();

                for (int i = 0; i < sections.Length; i++)
                {
                    if (sections[i].Contains(","))
                    {
                        string className = sections[i].Remove(sections[i].IndexOf(','));
                        Type t = nameSpaceAll.Where(x => x.Name == className.ToString()).FirstOrDefault();

                        if (t != null)
                        {
                            object obj = Activator.CreateInstance(t);

                            var buffer = new byte[2048];
                            GetPrivateProfileSection(sections[i].ToString(), buffer, 2048, this.iniPath);
                            var tmp = Encoding.Default.GetString(buffer).Trim('\0').Split('\0');

                            string[][] keyValue = null;
                            Dictionary<string, string> values = null;
                            getKeyValue(tmp, ref keyValue, ref values);

                            foreach (PropertyInfo pi in obj.GetType().GetProperties())
                            {
                                if (pi.PropertyType.Name == "Int32")
                                {
                                    pi.SetValue(obj, int.Parse(values[pi.Name]), null);
                                }
                                else
                                {
                                    pi.SetValue(obj, values[pi.Name], null);
                                }
                            }
                            listObj.Add(obj);
                        }
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取INI的值-Listobject,出错！！");
                return -1;
            }
        }

        /// <summary>
        /// 读取INI的值-ListControl
        /// </summary>
        /// <param name="listControl">界面控件参数集合</param>
        /// <returns></returns>
        public int read_listControl(ref List<Control> listControl)
        {
            try
            {
                string[] sections = ReadIniAllSectionName(this.iniPath);

                for (int i = 0; i < listControl.Count; i++)
                {
                    if (sections.Contains(listControl[i].GetType().Name))
                    {
                        var buffer = new byte[2048];
                        GetPrivateProfileSection(listControl[i].GetType().Name, buffer, 2048, this.iniPath);
                        var tmp = Encoding.Default.GetString(buffer).Trim('\0').Split('\0');

                        //int index = 0;
                        //string[][] keyValue = new string[2][];
                        //keyValue[0] = new string[tmp.Length];
                        //keyValue[1] = new string[tmp.Length];

                        //Dictionary<string, string> values = new Dictionary<string, string>();
                        //foreach (string value in tmp)
                        //{
                        //    var s = value.Split(new string[] { "=" }, 2, StringSplitOptions.None);
                        //    keyValue[0][index] = s[0];
                        //    keyValue[1][index] = s[1];
                        //    values.Add(s[0], s[1]);
                        //    index++;
                        //}
                        string[][] keyValue = null;
                        Dictionary<string, string> values = null;
                        getKeyValue(tmp, ref keyValue, ref values);

                        if (keyValue[0].Contains(listControl[i].Name))
                        {
                            if (listControl[i].GetType().Name == "RadioButton")
                            {
                                RadioButton radioButton = (RadioButton)listControl[i];
                                radioButton.Checked = (values[listControl[i].Name] == "False") ? false : true;
                            }
                            else if (listControl[i].GetType().Name == "CheckBox")
                            {
                                CheckBox CheckBox = (CheckBox)listControl[i];
                                CheckBox.Checked = (values[listControl[i].Name] == "False") ? false : true;
                            }
                            else
                            {
                                listControl[i].Text = values[listControl[i].Name];
                            }
                        }
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取INI的值-ListControl,出错！！");
                return -1;
            }
        }

        private void getKeyValue(string[] tmp, ref string[][] keyValue, ref Dictionary<string, string> values)
        {
            int index = 0;
            keyValue = new string[2][];
            keyValue[0] = new string[tmp.Length];
            keyValue[1] = new string[tmp.Length];

            values = new Dictionary<string, string>();
            foreach (string value in tmp)
            {
                var s = value.Split(new string[] { "=" }, 2, StringSplitOptions.None);
                keyValue[0][index] = s[0];
                keyValue[1][index] = s[1];
                values.Add(s[0], s[1]);
                index++;
            }
        }

        /// <summary>
        /// 读取INI的值-界面
        /// </summary>
        /// <param name="form">界面</param>
        /// <returns></returns>
        public int read_Control(Form form)
        {
            try
            {
                read_ini_listControl_1("Button", form);
                read_ini_listControl_1("TextBox", form);
                read_ini_listControl_1("RadioButton", form);
                read_ini_listControl_1("CheckBox", form);
                read_ini_listControl_1("ComboBox", form);

                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取INI的值-界面,出错！！");
                return -1;
            }
        }
        private void read_ini_listControl_1(string name, Form form)
        {
            try
            {
                var buffer = new byte[2048];
                GetPrivateProfileSection(name, buffer, 2048, this.iniPath);
                var tmp = Encoding.Default.GetString(buffer).Trim('\0').Split('\0');

                FieldInfo[] fieldInfo = form.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance); //反射
                ArrayList array = new ArrayList();
                for (int i = 0; i < fieldInfo.Length; i++)
                {
                    array.Add(fieldInfo[i].Name);
                }

                foreach (var entry in tmp)
                {
                    var s = entry.Split(new string[] { "=" }, 2, StringSplitOptions.None);

                    if (s[0] != "" && s[1] != "")
                    {
                        if (array.Contains(s[0]))  //判断控件是否存在
                        {
                            if (name == "RadioButton")
                            {
                                RadioButton con1 = (RadioButton)form.GetType().GetField(s[0], BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).GetValue(form);
                                con1.Checked = (s[1] == "False") ? false : true;
                            }
                            else if (name == "CheckBox")
                            {
                                CheckBox con1 = (CheckBox)form.GetType().GetField(s[0], BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).GetValue(form);
                                con1.Checked = (s[1] == "False") ? false : true;
                            }
                            else
                            {
                                Control con = (Control)form.GetType().GetField(s[0], BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).GetValue(form);
                                con.Text = s[1];
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "read_ini_listControl_1出错！！");
            }
        }

        /// <summary>
        /// 读取INI的值-DataGridView
        /// </summary>
        /// <param name="dataGridView">DataGridView界面控件</param>
        /// <returns></returns>
        public int read_dataGridView(ref DataGridView dataGridView)
        {
            try
            {
                DataTable dataTable = new DataTable();
                string[] sections = ReadIniAllSectionName(this.iniPath);

                bool init = false;

                for (int i = 0; i < sections.Length; i++)
                {
                    if (sections[i].Contains("DataGridView,"))
                    {
                        //array.Add(sections[i]);
                        var buffer = new byte[2048];
                        GetPrivateProfileSection(sections[i], buffer, 2048, this.iniPath);
                        var tmp = Encoding.Default.GetString(buffer).Trim('\0').Split('\0');

                        string[][] keyValue = null;
                        Dictionary<string, string> values = null;
                        getKeyValue(tmp, ref keyValue, ref values);

                        if (!init)
                        {
                            //添加表头
                            dataTable.Columns.Clear(); //清除表格中的数据
                            for (int j = 0; j < keyValue[0].Length; j++)
                            {
                                Console.WriteLine(keyValue[0][j]);
                                dataTable.Columns.Add(keyValue[0][j]);  //获取标题
                            }
                            dataTable.Rows.Clear();
                            init = true;
                        }

                        ArrayList tenpList = new ArrayList();
                        for (int j = 0; j < keyValue[1].Length; j++)
                        {
                            tenpList.Add(keyValue[1][j]);
                        }
                        object[] array = tenpList.ToArray();
                        dataTable.LoadDataRow(array, true);

                    }
                }

                dataGridView.DataSource = dataTable;

                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取INI的值-DataGridView,出错！！");
                return -1;
            }
        }

        /// <summary>
        /// 读取INI的值-DataTable
        /// </summary>
        /// <param name="dataTable">DataTable数据集</param>
        /// <returns></returns>
        public int read_dataTable(ref DataTable dataTable)
        {
            try
            {
                string[] sections = ReadIniAllSectionName(this.iniPath);

                bool init = false;

                for (int i = 0; i < sections.Length; i++)
                {
                    if (sections[i].Contains("DataTable,"))
                    {
                        //array.Add(sections[i]);
                        var buffer = new byte[2048];
                        GetPrivateProfileSection(sections[i], buffer, 2048, this.iniPath);
                        var tmp = Encoding.Default.GetString(buffer).Trim('\0').Split('\0');

                        string[][] keyValue = null;
                        Dictionary<string, string> values = null;
                        getKeyValue(tmp, ref keyValue, ref values);

                        if (!init)
                        {
                            //添加表头
                            dataTable.Columns.Clear(); //清除表格中的数据
                            for (int j = 0; j < keyValue[0].Length; j++)
                            {
                                Console.WriteLine(keyValue[0][j]);
                                dataTable.Columns.Add(keyValue[0][j]);  //获取标题
                            }
                            dataTable.Rows.Clear();
                            init = true;
                        }

                        ArrayList tenpList = new ArrayList();
                        for (int j = 0; j < keyValue[1].Length; j++)
                        {
                            tenpList.Add(keyValue[1][j]);
                        }
                        object[] array = tenpList.ToArray();
                        dataTable.LoadDataRow(array, true);

                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取INI的值-DataTable,出错！！");
                return -1;
            }
        }

        public int write_string(string value)
        {
            throw new NotImplementedException();
        }

        public int read_string(ref string value)
        {
            throw new NotImplementedException();
        }

        #endregion


    }
}
