using DotNet.Data.dao;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace DotNet.Data.daoImpl
{
    class TxtDaoImpl : TxtDao
    {
        private const string LogTag = "TxtDaoImpl";

        string txtPath = null;   //路径
        bool isCover = false;    //是否覆盖
        FileStream fs;           //文件流
        StreamWriter sw;         //写

        /// <summary>
        /// 设置是否覆盖
        /// </summary>
        /// <param name="isCover">写入是否覆盖</param>
        public void setCover(bool isCover)
        {
            this.isCover = isCover;
        }

        /// <summary>
        /// 初始化TXT文件
        /// </summary>
        /// <param name="filePath">TXT文件路径</param>
        /// <returns></returns>
        public int init(string filePath)
        {
            try
            {
                this.txtPath = filePath;

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
                DataLog.Exception(LogTag, ex, "初始化TXT文件,出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 初始化TXT文件
        /// </summary>
        /// <param name="filePath">TXT文件路径</param>
        /// <param name="isCover">写入是否覆盖</param>
        /// <returns></returns>
        public int init(string filePath, bool isCover)
        {
            try
            {
                this.txtPath = filePath;

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
                this.isCover = isCover;
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "初始化TXT文件,出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 打开log文本
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public int open_log_text(string filePath)
        {
            try
            {
                this.txtPath = filePath;
                if (!File.Exists(filePath))
                {
                    File.Create(filePath).Dispose();
                }

                fs = new FileStream(filePath, FileMode.Append);
                sw = new StreamWriter(fs);
                sw.WriteLine("[log]");
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "打开log文本,出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 关闭log文本
        /// </summary>
        /// <returns></returns>
        public int close_log_text()
        {
            try
            {
                sw.Close();
                fs.Close();
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "关闭log文本,出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 写入log
        /// </summary>
        /// <param name="value">值</param>
        /// <returns></returns>
        public int write_txt_log(string value)
        {
            try
            {
                sw.WriteLine(value);
                sw.Flush();
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "写入log,出错！！！");
                return -1;
            }
        }

        #region 数据操作-写

        /// <summary>
        /// 将字符串写入到TXT
        /// </summary>
        /// <param name="value">值</param>
        /// <returns></returns>
        public int write_string(string value)
        {
            try
            {
                if (!isCover)
                {
                    using (FileStream fileStream = new FileStream(this.txtPath, FileMode.Append, FileAccess.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(fileStream))
                        {
                            sw.WriteLine("[]" + value.GetType().Name);
                            sw.WriteLine(value);
                            sw.Flush();
                        }
                    }
                }
                else
                {
                    using (StreamWriter sw = new StreamWriter(this.txtPath))
                    {
                        sw.WriteLine("[]" + value.GetType().Name);
                        sw.WriteLine(value);
                        sw.Flush();
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将字符串写入到TXT,出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 将数组写入到TXT
        /// </summary>
        /// <param name="array">数组数据</param>
        /// <returns></returns>
        public int write_array(string[] array)
        {
            try
            {
                if (!isCover)
                {
                    using (FileStream fileStream = new FileStream(this.txtPath, FileMode.Append, FileAccess.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(fileStream))
                        {
                            sw.WriteLine("[]" + array.GetType().Name);
                            for (int i = 0; i < array.Length; i++)
                            {
                                sw.WriteLine(array[i]);
                            }
                            sw.Flush();
                        }
                    }
                }
                else
                {
                    using (StreamWriter sw = new StreamWriter(this.txtPath))
                    {
                        sw.WriteLine("[]" + array.GetType().Name);
                        for (int i = 0; i < array.Length; i++)
                        {
                            sw.WriteLine(array[i]);
                        }
                        sw.Flush();
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将数组写入到TXT,出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 将界面ListBox写入到TXT
        /// </summary>
        /// <param name="listBox">界面ListBox</param>
        /// <returns></returns>
        public int write_listBox(ListBox listBox)
        {
            try
            {
                if (!isCover)
                {
                    using (FileStream fileStream = new FileStream(this.txtPath, FileMode.Append, FileAccess.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(fileStream))
                        {
                            sw.WriteLine("[]" + listBox.GetType().Name);
                            foreach (string str in listBox.Items)
                            {
                                sw.WriteLine(str);
                            }
                            sw.Flush();
                        }
                    }
                }
                else
                {
                    using (StreamWriter sw = new StreamWriter(this.txtPath))
                    {
                        sw.WriteLine("[]" + listBox.GetType().Name);
                        foreach (string str in listBox.Items)
                        {
                            sw.WriteLine(str);
                        }
                        sw.Flush();
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将界面ListBox写入到TXT,出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 将字典写入到TXT
        /// </summary>
        /// <param name="data">字典数据</param>
        /// <returns></returns>
        public int write_dictionary(Dictionary<object, object> data)
        {
            try
            {
                if (!isCover)
                {
                    using (FileStream fileStream = new FileStream(this.txtPath, FileMode.Append, FileAccess.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(fileStream))
                        {
                            sw.WriteLine("[]" + data.GetType().Name);
                            object[] keys = data.Keys.ToArray();
                            for (int i = 0; i < data.Count; i++)
                            {
                                sw.WriteLine(keys[i] + ":" + data[keys[i]]);
                            }
                            sw.Flush();
                        }
                    }
                }
                else
                {
                    using (StreamWriter sw = new StreamWriter(this.txtPath))
                    {
                        sw.WriteLine("[]" + data.GetType().Name);
                        object[] keys = data.Keys.ToArray();
                        for (int i = 0; i < data.Count; i++)
                        {
                            sw.WriteLine(keys[i] + ":" + data[keys[i]]);
                        }
                        sw.Flush();
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将字典写入到TXT,出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 将ArrayList集合写入到TXT
        /// </summary>
        /// <param name="arrayList">ArrayList集合</param>
        /// <returns></returns>
        public int write_arrayList(ArrayList arrayList)
        {
            try
            {
                if (!isCover)
                {
                    using (FileStream fileStream = new FileStream(this.txtPath, FileMode.Append, FileAccess.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(fileStream))
                        {
                            for (int i = 0; i < arrayList.Count; i++)
                            {
                                sw.WriteLine("[]" + arrayList[i].GetType().Name);
                                foreach (PropertyInfo pi in arrayList[i].GetType().GetProperties())
                                {
                                    sw.Write(pi.Name + ":" + pi.GetValue(arrayList[i], null) + ";");
                                }
                                sw.WriteLine();
                            }
                            sw.Flush();
                        }
                    }
                }
                else
                {
                    using (StreamWriter sw = new StreamWriter(this.txtPath))
                    {
                        for (int i = 0; i < arrayList.Count; i++)
                        {
                            sw.WriteLine("[]" + arrayList[i].GetType().Name);
                            foreach (PropertyInfo pi in arrayList[i].GetType().GetProperties())
                            {
                                sw.Write(pi.Name + ":" + pi.GetValue(arrayList[i], null) + ";");
                            }
                            sw.WriteLine();
                        }
                        sw.Flush();
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将ArrayList集合写入到TXT,出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 将泛型集合写入到TXT
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="listT">泛型集合</param>
        /// <returns></returns>
        public int write_listT2<T>(List<T> listT)
        {
            try
            {
                if (!isCover)
                {
                    using (FileStream fileStream = new FileStream(this.txtPath, FileMode.Append, FileAccess.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(fileStream))
                        {
                            for (int i = 0; i < listT.Count; i++)
                            {
                                sw.WriteLine("[]" + listT[i].GetType().Name);
                                foreach (PropertyInfo pi in listT[i].GetType().GetProperties())
                                {
                                    sw.Write(pi.Name + ":" + pi.GetValue(listT[i], null) + ";");
                                }
                                sw.WriteLine();
                            }
                            sw.Flush();
                        }
                    }
                }
                else
                {
                    using (StreamWriter sw = new StreamWriter(this.txtPath))
                    {
                        for (int i = 0; i < listT.Count; i++)
                        {
                            sw.WriteLine("[]" + listT[i].GetType().Name);
                            foreach (PropertyInfo pi in listT[i].GetType().GetProperties())
                            {
                                sw.Write(pi.Name + ":" + pi.GetValue(listT[i], null) + ";");
                            }
                            sw.WriteLine();
                        }
                        sw.Flush();
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将泛型集合写入到TXT,出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 写入表头
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        public int write_DataHeader<T>(T model)
        {
            try
            {
                using (FileStream fileStream = new FileStream(this.txtPath, FileMode.Append, FileAccess.Write))
                {
                    using (StreamWriter sw = new StreamWriter(fileStream))
                    {
                        string header = null;
                        foreach (PropertyInfo pi in model.GetType().GetProperties())
                        {
                            header += pi.Name + "\t";
                        }
                        sw.WriteLine(header);
                        sw.Flush();
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将泛型集合写入到TXT,出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 将泛型集合写入到TXT
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="listT">泛型集合</param>
        /// <returns></returns>
        public int write_listT<T>(List<T> listT)
        {
            try
            {
                if (!isCover)
                {
                    using (FileStream fileStream = new FileStream(this.txtPath, FileMode.Append, FileAccess.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(fileStream))
                        {
                            for (int i = 0; i < listT.Count; i++)
                            {
                                foreach (PropertyInfo pi in listT[i].GetType().GetProperties())
                                {
                                    sw.Write(pi.GetValue(listT[i], null) + "\t");
                                }
                                sw.WriteLine();
                            }
                            sw.Flush();
                        }
                    }
                }
                else
                {
                    using (StreamWriter sw = new StreamWriter(this.txtPath))
                    {
                        for (int i = 0; i < listT.Count; i++)
                        {
                            foreach (PropertyInfo pi in listT[i].GetType().GetProperties())
                            {
                                sw.Write(pi.GetValue(listT[i], null) + "\t");
                            }
                            sw.WriteLine();
                        }
                        sw.Flush();
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将泛型集合写入到TXT,出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 将ListObj集合写入到TXT
        /// </summary>
        /// <param name="listObj">ListObj集合</param>
        /// <returns></returns>
        public int write_listObj(List<object> listObj)
        {
            try
            {
                if (!isCover)
                {
                    using (FileStream fileStream = new FileStream(this.txtPath, FileMode.Append, FileAccess.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(fileStream))
                        {
                            for (int i = 0; i < listObj.Count; i++)
                            {
                                sw.WriteLine("[]" + listObj[i].GetType().Name);
                                foreach (PropertyInfo pi in listObj[i].GetType().GetProperties())
                                {
                                    sw.Write(pi.Name + ":" + pi.GetValue(listObj[i], null) + ";");
                                }
                                sw.WriteLine();
                            }
                            sw.Flush();
                        }
                    }
                }
                else
                {
                    using (StreamWriter sw = new StreamWriter(this.txtPath))
                    {
                        for (int i = 0; i < listObj.Count; i++)
                        {
                            sw.WriteLine("[]" + listObj[i].GetType().Name);
                            foreach (PropertyInfo pi in listObj[i].GetType().GetProperties())
                            {
                                sw.Write(pi.Name + ":" + pi.GetValue(listObj[i], null) + ";");
                            }
                            sw.WriteLine();
                        }
                        sw.Flush();
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将ListObj集合写入到TXT,出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 将界面控件参数写入到TXT
        /// </summary>
        /// <param name="listControl">listControl集合</param>
        /// <returns></returns>
        public int write_listControl(List<Control> listControl)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(this.txtPath))
                {
                    sw.WriteLine("[]" + listControl[0].GetType().Name);

                    for (int i = 0; i < listControl.Count; i++)
                    {

                        if (listControl[i].GetType().Name == "ToolStripMenuItem")
                        {
                            goto EndHandle;
                        }
                        else if (listControl[i].GetType().Name == "DataGridViewTextBoxColumn" || listControl[i].GetType().Name == "DataGridViewComboBoxColumn" || listControl[i].GetType().Name == "DataGridViewButtonColumn")
                        {
                            goto EndHandle;
                        }
                        else if (listControl[i].GetType().Name == "RadioButton")
                        {
                            RadioButton radioButton = (RadioButton)listControl[i];
                            sw.WriteLine(i + ";" + listControl[i].GetType().Name + ";" + radioButton.Name + ";" + radioButton.Checked.ToString() + ";" + radioButton.AccessibleName);
                        }
                        else if (listControl[i].GetType().Name == "CheckBox")
                        {
                            CheckBox checkBox = (CheckBox)listControl[i];
                            sw.WriteLine(i + ";" + listControl[i].GetType().Name + ";" + checkBox.Name + ";" + checkBox.Checked.ToString() + ";" + checkBox.AccessibleName);
                        }
                        else
                        {
                            sw.WriteLine(i + ";" + listControl[i].GetType().Name + ";" + listControl[i].Name + ";" + listControl[i].Text + ";" + listControl[i].AccessibleName);
                        }
                        EndHandle: { }
                    }
                    sw.Flush();
                    return 0;
                }
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将界面控件参数写入到TXT,出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 将全部界面的参数写入到TXT
        /// </summary>
        /// <param name="form">界面控件集合</param>
        public int write_Control(Form form)
        {
            try
            {
                using (StreamWriter sw = new StreamWriter(this.txtPath))
                {
                    sw.WriteLine("[]" + form.Name);

                    FieldInfo[] fieldInfo = form.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance); //反射

                    for (int i = 0; i < fieldInfo.Length; i++)
                    {
                        if (fieldInfo[i].FieldType.Namespace == "System.Windows.Forms")
                        {
                            if (fieldInfo[i].FieldType.Name == "ToolStripMenuItem")
                            {
                                goto EndHandle;
                            }
                            else if (fieldInfo[i].FieldType.Name == "DataGridViewTextBoxColumn" || fieldInfo[i].FieldType.Name == "DataGridViewComboBoxColumn" || fieldInfo[i].FieldType.Name == "DataGridViewButtonColumn")
                            {
                                goto EndHandle;
                            }
                            else if (fieldInfo[i].FieldType.Name == "RadioButton")
                            {
                                RadioButton con = (RadioButton)form.GetType().GetField(fieldInfo[i].Name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                                sw.WriteLine(i + ";" + fieldInfo[i].FieldType.Name + ";" + con.Name + ";" + con.Checked.ToString() + ";" + con.AccessibleName);
                            }
                            else if (fieldInfo[i].FieldType.Name == "CheckBox")
                            {
                                CheckBox con = (CheckBox)form.GetType().GetField(fieldInfo[i].Name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                                sw.WriteLine(i + ";" + fieldInfo[i].FieldType.Name + ";" + con.Name + ";" + con.Checked.ToString() + ";" + con.AccessibleName);
                            }
                            else
                            {
                                Control con = (Control)form.GetType().GetField(fieldInfo[i].Name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
                                sw.WriteLine(i + ";" + fieldInfo[i].FieldType.Name + ";" + con.Name + ";" + con.Text + ";" + con.AccessibleName);
                            }
                            EndHandle: { };

                        }
                    }
                    sw.Flush();
                    return 0;
                }
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将全部界面的参数写入到TXT,出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 将DataGridView的值写入到TXT
        /// </summary>
        /// <param name="dataGridView">dataGridView数据</param>
        /// <returns></returns>
        public int write_dataGridView(DataGridView dataGridView)
        {
            try
            {
                if (!isCover)
                {
                    using (FileStream fileStream = new FileStream(this.txtPath, FileMode.Append, FileAccess.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(fileStream))
                        {
                            sw.WriteLine("[]" + dataGridView.GetType().Name);
                            for (int i = 0; i < dataGridView.Rows.Count; i++)
                            {
                                for (int j = 0; j < dataGridView.Columns.Count; j++)
                                {
                                    sw.Write(dataGridView.Columns[j].HeaderText + ":" + dataGridView[j, i].Value.ToString() + ";");
                                }
                                sw.WriteLine();
                            }
                            sw.Flush();
                        }
                    }
                }
                else
                {
                    using (StreamWriter sw = new StreamWriter(this.txtPath))
                    {
                        sw.WriteLine("[]" + dataGridView.GetType().Name);
                        for (int i = 0; i < dataGridView.Rows.Count; i++)
                        {
                            for (int j = 0; j < dataGridView.Columns.Count; j++)
                            {
                                sw.Write(dataGridView.Columns[j].HeaderText + ":" + dataGridView[j, i].Value.ToString() + ";");
                            }
                            sw.WriteLine();
                        }
                        sw.Flush();
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将DataGridView的值写入到TXT,出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 将DataTable的值写入到TXT
        /// </summary>
        /// <param name="dataTable">DataTable数据</param>
        /// <returns></returns>
        public int write_dataTable(DataTable dataTable)
        {
            try
            {
                if (!isCover)
                {
                    using (FileStream fileStream = new FileStream(this.txtPath, FileMode.Append, FileAccess.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(fileStream))
                        {
                            sw.WriteLine("[]" + dataTable.GetType().Name);
                            for (int i = 0; i < dataTable.Rows.Count; i++)
                            {
                                for (int j = 0; j < dataTable.Columns.Count; j++)
                                {
                                    sw.Write(dataTable.Columns[j].ToString() + ":" + dataTable.Rows[i].ItemArray[j].ToString() + ";");
                                }
                                sw.WriteLine();
                            }
                            sw.Flush();
                        }
                    }
                }
                else
                {
                    using (StreamWriter sw = new StreamWriter(this.txtPath))
                    {
                        sw.WriteLine("[]" + dataTable.GetType().Name);
                        for (int i = 0; i < dataTable.Rows.Count; i++)
                        {
                            for (int j = 0; j < dataTable.Columns.Count; j++)
                            {
                                sw.Write(dataTable.Columns[j].ToString() + ":" + dataTable.Rows[i].ItemArray[j].ToString() + ";");
                            }
                            sw.WriteLine();
                        }
                        sw.Flush();
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将DataTable的值写入到TXT,出错！！！");
                return -1;
            }
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
            try
            {
                using (StreamReader sr = new StreamReader(this.txtPath))
                {
                    sr.ReadLine();
                    value = sr.ReadLine();
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取TXT的值-string,出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 读取TXT的值-string[]
        /// </summary>
        /// <param name="array">数组</param>
        /// <returns></returns>
        public int read_array(ref string[] array)
        {
            try
            {
                using (StreamReader sr = new StreamReader(this.txtPath))
                {
                    sr.ReadLine();

                    int i = 0;
                    string value = null;
                    while ((value = sr.ReadLine()) != null)
                    {
                        array[i] = value;
                        i++;
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取TXT的值-string[],出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 读取TXT的值-listBox
        /// </summary>
        /// <param name="listBox">listBox界面控件</param>
        /// <returns></returns>
        public int read_listBox(ref ListBox listBox)
        {
            try
            {
                using (StreamReader sr = new StreamReader(this.txtPath))
                {
                    sr.ReadLine();

                    string value = null;
                    while ((value = sr.ReadLine()) != null)
                    {
                        listBox.Items.Add(value);
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取TXT的值-listBox,出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 读取TXT的值-字典
        /// </summary>
        /// <param name="data">字典数据</param>
        /// <returns></returns>
        public int read_dictionary(ref Dictionary<object, object> data)
        {
            try
            {
                using (StreamReader sr = new StreamReader(this.txtPath))
                {
                    string value = null;
                    while ((value = sr.ReadLine()) != null)
                    {
                        if (!value.Contains("[]") && value.Contains(":"))
                        {
                            string[] strs = value.Split(':');
                            data.Add(strs[0], strs[1]);
                        }
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取TXT的值-字典,出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 读取TXT的值-ArrayList
        /// </summary>
        /// <param name="arrayList">ArrayList集合</param>
        /// <returns></returns>
        public int read_arrayList(ref ArrayList arrayList)
        {
            try
            {
                var nameSpaceAll = Assembly.GetEntryAssembly().GetTypes();

                using (StreamReader sr = new StreamReader(this.txtPath))
                {
                    string value = null;
                    string className = null;

                    while ((value = sr.ReadLine()) != null)
                    {
                        if (value.Contains("[]"))
                        {
                            className = value.Substring(2);
                        }
                        else
                        {
                            string[] proper = value.Split(';');

                            Type t = nameSpaceAll.Where(x => x.Name == className.ToString()).FirstOrDefault();

                            if (t != null)
                            {
                                object obj = Activator.CreateInstance(t);

                                for (int i = 0; i < proper.Length; i++)
                                {
                                    if (proper[i] != "")
                                    {
                                        string[] values = proper[i].Split(':');
                                        object v = Convert.ChangeType(values[1], obj.GetType().GetProperty(values[0]).PropertyType);
                                        obj.GetType().GetProperty(values[0]).SetValue(obj, v, null);
                                    }
                                }

                                //遍历方法2
                                //int index = 0;
                                //foreach (PropertyInfo pi in obj.GetType().GetProperties())
                                //{
                                //    string[] values = proper[index].Split(':');

                                //    if (pi.PropertyType.Name == "Int32")
                                //    {
                                //        pi.SetValue(obj, int.Parse(values[1]), null);
                                //    }
                                //    else
                                //    {
                                //        pi.SetValue(obj, values[1], null);
                                //    }
                                //}

                                arrayList.Add(obj);
                            }

                        }
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取TXT的值-ArrayList,出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 读取TXT的值-listT
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="listT">泛型集合</param>
        /// <returns></returns>
        public int read_listT<T>(ref List<T> listT) where T : new()
        {
            try
            {
                using (StreamReader sr = new StreamReader(this.txtPath))
                {
                    string value = null;
                    string className = null;

                    while ((value = sr.ReadLine()) != null)
                    {
                        if (value.Contains("[]"))
                        {
                            className = value.Substring(2);
                        }
                        else
                        {
                            T model = new T();
                            string[] proper = value.Split(';');
                            for (int i = 0; i < proper.Length; i++)
                            {
                                if (proper[i] != "")
                                {
                                    string[] values = proper[i].Split(':');
                                    object v = Convert.ChangeType(values[1], model.GetType().GetProperty(values[0]).PropertyType);
                                    model.GetType().GetProperty(values[0]).SetValue(model, v, null);
                                }
                            }
                            listT.Add(model);
                        }
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取TXT的值-listT,出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 读取TXT的值-Listobject
        /// </summary>
        /// <param name="listObj">listObj集合</param>
        /// <returns></returns>
        public int read_listObj(ref List<object> listObj)
        {
            try
            {
                var nameSpaceAll = Assembly.GetEntryAssembly().GetTypes();

                using (StreamReader sr = new StreamReader(this.txtPath))
                {
                    string value = null;
                    string className = null;

                    while ((value = sr.ReadLine()) != null)
                    {
                        if (value.Contains("[]"))
                        {
                            className = value.Substring(2);
                        }
                        else
                        {
                            string[] proper = value.Split(';');

                            Type t = nameSpaceAll.Where(x => x.Name == className.ToString()).FirstOrDefault();

                            if (t != null)
                            {
                                object obj = Activator.CreateInstance(t);

                                for (int i = 0; i < proper.Length; i++)
                                {
                                    if (proper[i] != "")
                                    {
                                        string[] values = proper[i].Split(':');
                                        object v = Convert.ChangeType(values[1], obj.GetType().GetProperty(values[0]).PropertyType);
                                        obj.GetType().GetProperty(values[0]).SetValue(obj, v, null);
                                    }
                                }

                                //遍历方法2
                                //int index = 0;
                                //foreach (PropertyInfo pi in obj.GetType().GetProperties())
                                //{
                                //    string[] values = proper[index].Split(':');

                                //    if (pi.PropertyType.Name == "Int32")
                                //    {
                                //        pi.SetValue(obj, int.Parse(values[1]), null);
                                //    }
                                //    else
                                //    {
                                //        pi.SetValue(obj, values[1], null);
                                //    }
                                //}

                                listObj.Add(obj);
                            }

                        }
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取TXT的值-Listobject,出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 读取TXT的值-ListControl
        /// </summary>
        /// <param name="listControl">界面控件参数集合</param>
        /// <returns></returns>
        public int read_listControl(ref List<Control> listControl)
        {
            try
            {
                using (StreamReader sr = new StreamReader(this.txtPath))
                {
                    sr.ReadLine();
                    string value = null;

                    List<Items_Control> ItemsControl = new List<Items_Control>();

                    while ((value = sr.ReadLine()) != null)
                    {
                        string[] proper = value.Split(';');
                        if (proper[3] != "" && proper[3] != null)
                        {
                            Items_Control itemsControl = new Items_Control();
                            itemsControl.ID = int.Parse(proper[0]);
                            itemsControl.type = proper[1];
                            itemsControl.name = proper[2];
                            itemsControl.value = proper[3];
                            itemsControl.describe = proper[4];
                            ItemsControl.Add(itemsControl);
                        }
                    }

                    for (int i = 0; i < listControl.Count; i++)
                    {
                        for (int j = 0; j < ItemsControl.Count; j++)
                        {
                            if (listControl[i].Name == ItemsControl[j].name)
                            {
                                if (listControl[i].GetType().Name == "RadioButton")
                                {
                                    RadioButton radioButton = (RadioButton)listControl[i];
                                    radioButton.Checked = (ItemsControl[j].value == "False") ? false : true;
                                }
                                else if (listControl[i].GetType().Name == "CheckBox")
                                {
                                    CheckBox CheckBox = (CheckBox)listControl[i];
                                    CheckBox.Checked = (ItemsControl[j].value == "False") ? false : true;
                                }
                                else
                                {
                                    listControl[i].Text = ItemsControl[j].value;
                                }
                            }
                        }

                    }

                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取TXT的值-ListControl,出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 读取TXT的值-界面
        /// </summary>
        /// <param name="form">界面</param>
        /// <returns></returns>
        public int read_Control(Form form)
        {
            try
            {
                using (StreamReader sr = new StreamReader(this.txtPath))
                {
                    sr.ReadLine();
                    string value = null;

                    List<Items_Control> ItemsControl = new List<Items_Control>();

                    while ((value = sr.ReadLine()) != null)
                    {
                        string[] proper = value.Split(';');
                        if (proper[3] != "" && proper[3] != null)
                        {
                            Items_Control itemsControl = new Items_Control();
                            itemsControl.ID = int.Parse(proper[0]);
                            itemsControl.type = proper[1];
                            itemsControl.name = proper[2];
                            itemsControl.value = proper[3];
                            itemsControl.describe = proper[4];
                            ItemsControl.Add(itemsControl);
                        }
                    }

                    FieldInfo[] fieldInfo = form.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance); //反射
                    for (int i = 0; i < fieldInfo.Length; i++)
                    {
                        for (int j = 0; j < ItemsControl.Count; j++)
                        {
                            if (fieldInfo[i].Name == ItemsControl[j].name)
                            {
                                string name = ItemsControl[j].name;
                                if (fieldInfo[i].FieldType.Name == "Button")
                                {
                                    Button con = (Button)form.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).GetValue(form);
                                    con.Text = ItemsControl[j].value;
                                    goto EndHandle;
                                }
                                if (fieldInfo[i].FieldType.Name == "TextBox")
                                {
                                    TextBox con = (TextBox)form.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).GetValue(form);
                                    con.Text = ItemsControl[j].value;
                                    goto EndHandle;
                                }
                                if (fieldInfo[i].FieldType.Name == "RadioButton")
                                {
                                    RadioButton con = (RadioButton)form.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).GetValue(form);
                                    con.Checked = (ItemsControl[j].value == "False") ? false : true;
                                    goto EndHandle;
                                }
                                if (fieldInfo[i].FieldType.Name == "CheckBox")
                                {
                                    CheckBox con = (CheckBox)form.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).GetValue(form);
                                    con.Checked = (ItemsControl[j].value == "False") ? false : true;
                                    goto EndHandle;
                                }
                                if (fieldInfo[i].FieldType.Name == "ComboBox")
                                {
                                    ComboBox con = (ComboBox)form.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).GetValue(form);
                                    con.Text = ItemsControl[j].value;
                                    goto EndHandle;
                                }
                            }
                        }
                        EndHandle: { };
                    }

                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取TXT的值-界面,出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 读取TXT的值-DataGridView
        /// </summary>
        /// <param name="dataGridView">DataGridView界面控件</param>
        /// <returns></returns>
        public int read_dataGridView(ref DataGridView dataGridView)
        {
            try
            {
                using (StreamReader sr = new StreamReader(this.txtPath))
                {
                    string value = null;
                    bool init = false;

                    sr.ReadLine();
                    DataTable dataTable = new DataTable();
                    while ((value = sr.ReadLine()) != null)
                    {
                        string[] proper = value.Split(';');

                        if (!init)
                        {
                            //添加表头
                            dataTable.Columns.Clear(); //清除表格中的数据
                            for (int i = 0; i < proper.Length; i++)
                            {
                                if (proper[i] != "")
                                {
                                    string[] values = proper[i].Split(':');
                                    dataTable.Columns.Add(values[0]);  //获取标题
                                }
                            }
                            dataTable.Rows.Clear();
                            init = true;
                        }

                        ArrayList tenpList = new ArrayList();
                        for (int i = 0; i < proper.Length; i++)
                        {
                            if (proper[i] != "")
                            {
                                string[] values = proper[i].Split(':');
                                tenpList.Add(values[1]);
                            }
                        }
                        object[] array = tenpList.ToArray();
                        dataTable.LoadDataRow(array, true);
                    }
                    dataGridView.DataSource = dataTable;
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取TXT的值-DataGridView,出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 读取TXT的值-DataTable
        /// </summary>
        /// <param name="dataTable">DataTable数据集</param>
        /// <returns></returns>
        public int read_dataTable(ref DataTable dataTable)
        {
            try
            {
                using (StreamReader sr = new StreamReader(this.txtPath))
                {
                    string value = null;
                    bool init = false;

                    sr.ReadLine();
                    while ((value = sr.ReadLine()) != null)
                    {
                        string[] proper = value.Split(';');

                        if (!init)
                        {
                            //添加表头
                            dataTable.Columns.Clear(); //清除表格中的数据
                            for (int i = 0; i < proper.Length; i++)
                            {
                                if (proper[i] != "")
                                {
                                    string[] values = proper[i].Split(':');
                                    dataTable.Columns.Add(values[0]);  //获取标题
                                }
                            }
                            dataTable.Rows.Clear();
                            init = true;
                        }

                        ArrayList tenpList = new ArrayList();
                        for (int i = 0; i < proper.Length; i++)
                        {
                            if (proper[i] != "")
                            {
                                string[] values = proper[i].Split(':');
                                tenpList.Add(values[1]);
                            }
                        }
                        object[] array = tenpList.ToArray();
                        dataTable.LoadDataRow(array, true);
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取TXT的值-DataTable,出错！！！");
                return -1;
            }
        }

        #endregion

    }
}
