using DotNet.Data.dao;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace DotNet.Data.daoImpl
{
    class JsonDaoImpl : JsonDao
    {
        private const string LogTag = "JsonDaoImpl";

        string jsonPath = null;  //文件路径
        bool formatting = false; //是否格式化json字符串
        JsonSerializer serializer = new JsonSerializer();

        /// <summary>
        /// 设置Json格式
        /// </summary>
        /// <param name="format">是否格式化json字符串</param>
        /// <returns></returns>
        public void setFormatting(bool format)  
        {
            this.formatting = format;
        }

        /// <summary>
        /// 初始化JSON文件
        /// </summary>
        /// <param name="filePath">JSON文件路径</param>
        /// <returns></returns>
        public int init(string filePath)
        {
            try
            {
                this.jsonPath = filePath;

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
                DataLog.Exception(LogTag, ex, "初始化JSON文件出错！！！");
                return -1;
            }
        }


        /// <summary>
        /// 初始化JSON文件
        /// </summary>
        /// <param name="filePath">JSON文件路径</param>
        /// <param name="format">是否格式化json字符串</param>
        /// <returns></returns>
        public int init(string filePath, bool format)
        {
            try
            {
                this.jsonPath = filePath;

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
                this.formatting = format;
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "初始化JSON文件出错！！！");
                return -1;
            }
        }

        #region 数据操作-写

        /// <summary>
        /// 将数组写入到JSON
        /// </summary>
        /// <param name="array">数据</param>
        /// <returns></returns>
        public int write_array(string[] array)
        {
            try
            {
                using (StringWriter sw = new StringWriter())
                {
                    using (JsonTextWriter writer = new JsonTextWriter(sw))
                    {
                        //writer.WriteStartObject();           //   {  （Json数据的大括号左边 ）
                        //writer.WritePropertyName("Json_Items");
                        writer.WriteStartArray();            //   [      (Json数据的大括号左边) 
                        for (int i = 0; i < array.Length; i++)
                        {
                            writer.WriteStartObject();
                            writer.WritePropertyName("id");
                            writer.WriteValue(i);
                            writer.WritePropertyName("value");
                            writer.WriteValue(array[i]);
                            writer.WriteEndObject();
                        }
                        writer.WriteEndArray();    //    ]   （多组json数据结束标记）
                        //writer.WriteEndObject();   //    }

                        if (formatting)
                        {
                            //格式化json字符串
                            TextReader tr = new StringReader(sw.ToString());
                            JsonTextReader jtr = new JsonTextReader(tr);
                            object obj = serializer.Deserialize(jtr);

                            using (StreamWriter wtyeu = new StreamWriter(this.jsonPath))
                            {
                                wtyeu.Write(obj.ToString());
                                wtyeu.Flush();
                            }
                        }
                        else
                        {
                            //2、直接写入法 无格式化json
                            using (StreamWriter wtyeu = new StreamWriter(this.jsonPath))
                            {
                                wtyeu.Write(sw.ToString());
                                wtyeu.Flush();
                            }
                        }
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将数组写入到JSON出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 将ListBox数据写入到JSON
        /// </summary>
        /// <param name="listBox">ListBox界面控件</param>
        /// <returns></returns>
        public int write_listBox(ListBox listBox)
        {
            try
            {
                using (StringWriter sw = new StringWriter())
                {
                    using (JsonTextWriter writer = new JsonTextWriter(sw))
                    {
                        int i = 0;
                        writer.WriteStartArray();            //   [      (Json数据的大括号左边) 
                        foreach (string row in listBox.Items)
                        {
                            writer.WriteStartObject();
                            writer.WritePropertyName("id");
                            writer.WriteValue(i);
                            writer.WritePropertyName("value");
                            writer.WriteValue(row);
                            writer.WriteEndObject();
                        }
                        writer.WriteEndArray();               //    ]   （多组json数据结束标记）

                        if (formatting)
                        {
                            //格式化json字符串
                            TextReader tr = new StringReader(sw.ToString());
                            JsonTextReader jtr = new JsonTextReader(tr);
                            object obj = serializer.Deserialize(jtr);

                            using (StreamWriter wtyeu = new StreamWriter(this.jsonPath))
                            {
                                wtyeu.Write(obj.ToString());
                                wtyeu.Flush();
                            }
                        }
                        else
                        {
                            //2、直接写入法 无格式化json
                            using (StreamWriter wtyeu = new StreamWriter(this.jsonPath))
                            {
                                wtyeu.Write(sw.ToString());
                                wtyeu.Flush();
                            }
                        }
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将数组写入到JSON出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 将ArrayList集合写入到JSON
        /// </summary>
        /// <param name="arrayList">数据</param>
        /// <returns></returns>
        public int write_arrayList(ArrayList arrayList)
        {
            try
            {
                using (StringWriter sw = new StringWriter())
                {
                    serializer.Serialize(new JsonTextWriter(sw), arrayList);

                    if (formatting)
                    {
                        using (StreamWriter wtyeu = new StreamWriter(this.jsonPath))
                        {
                            TextReader tr = new StringReader(sw.ToString());
                            JsonTextReader jtr = new JsonTextReader(tr);
                            object obj = serializer.Deserialize(jtr);

                            wtyeu.Write(obj.ToString());
                            wtyeu.Flush();
                        }
                    }
                    else
                    {
                        using (StreamWriter wtyeu = new StreamWriter(this.jsonPath))
                        {
                            wtyeu.Write(sw.GetStringBuilder().ToString());
                            wtyeu.Flush();
                        }
                    }

                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将ArrayList集合写入到JSON出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 将T集合写入到JSON
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        public int write_class<T>(T model)
        {
            try
            {
                using (StringWriter sw = new StringWriter())
                {
                    serializer.Serialize(new JsonTextWriter(sw), model);

                    if (formatting)
                    {
                        using (StreamWriter wtyeu = new StreamWriter(this.jsonPath))
                        {
                            TextReader tr = new StringReader(sw.ToString());
                            JsonTextReader jtr = new JsonTextReader(tr);
                            object obj = serializer.Deserialize(jtr);

                            wtyeu.Write(obj.ToString());
                            wtyeu.Flush();
                        }
                    }
                    else
                    {
                        using (StreamWriter wtyeu = new StreamWriter(this.jsonPath))
                        {
                            wtyeu.Write(sw.GetStringBuilder().ToString());
                            wtyeu.Flush();
                        }
                    }

                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将ArrayList集合写入到JSON出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 将listT集合写入到JSON
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="listT">数据</param>
        /// <returns></returns>
        public int write_listT<T>(List<T> listT)
        {
            try
            {
                using (StringWriter sw = new StringWriter())
                {
                    serializer.Serialize(new JsonTextWriter(sw), listT);

                    if (formatting)
                    {
                        using (StreamWriter wtyeu = new StreamWriter(this.jsonPath))
                        {
                            TextReader tr = new StringReader(sw.ToString());
                            JsonTextReader jtr = new JsonTextReader(tr);
                            object obj = serializer.Deserialize(jtr);

                            wtyeu.Write(obj.ToString());
                            wtyeu.Flush();
                        }
                    }
                    else
                    {
                        using (StreamWriter wtyeu = new StreamWriter(this.jsonPath))
                        {
                            wtyeu.Write(sw.GetStringBuilder().ToString());
                            wtyeu.Flush();
                        }
                    }

                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将ArrayList集合写入到JSON出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 将listObj集合写入到JSON
        /// </summary>
        /// <param name="listObj">数据</param>
        /// <returns></returns>
        public int write_listObj(List<object> listObj)
        {
            try
            {
                using (StringWriter sw = new StringWriter())
                {
                    serializer.Serialize(new JsonTextWriter(sw), listObj);

                    if (formatting)
                    {
                        using (StreamWriter wtyeu = new StreamWriter(this.jsonPath))
                        {
                            TextReader tr = new StringReader(sw.ToString());
                            JsonTextReader jtr = new JsonTextReader(tr);
                            object obj = serializer.Deserialize(jtr);

                            wtyeu.Write(obj.ToString());
                            wtyeu.Flush();
                        }
                    }
                    else
                    {
                        using (StreamWriter wtyeu = new StreamWriter(this.jsonPath))
                        {
                            wtyeu.Write(sw.GetStringBuilder().ToString());
                            wtyeu.Flush();
                        }
                    }

                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将ArrayList集合写入到JSON出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 将listControl集合写入到JSON
        /// </summary>
        /// <param name="listControl"></param>
        /// <returns></returns>
        public int write_listControl(List<Control> listControl)
        {
            try
            {
                #region 获取控件的值
                ArrayList arrayList = new ArrayList();
                for (int i = 0; i < listControl.Count; i++)
                {
                    Items_Control control_Items = new Items_Control();
                    control_Items.ID = i;
                    control_Items.type = listControl[i].GetType().Name;
                    control_Items.name = listControl[i].Name;
                    control_Items.describe = listControl[i].AccessibleName;
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
                        control_Items.value = radioButton.Checked.ToString();
                    }
                    else if (listControl[i].GetType().Name == "CheckBox")
                    {
                        CheckBox checkBox = (CheckBox)listControl[i];
                        control_Items.value = checkBox.Checked.ToString();
                    }
                    else
                    {
                        control_Items.value = listControl[i].Text;
                    }
                    EndHandle: { };
                    arrayList.Add(control_Items);
                }
                #endregion

                using (StringWriter sw = new StringWriter())
                {
                    serializer.Serialize(new JsonTextWriter(sw), arrayList);

                    if (formatting)
                    {
                        using (StreamWriter wtyeu = new StreamWriter(this.jsonPath))
                        {
                            TextReader tr = new StringReader(sw.ToString());
                            JsonTextReader jtr = new JsonTextReader(tr);
                            object obj = serializer.Deserialize(jtr);

                            wtyeu.Write(obj.ToString());
                            wtyeu.Flush();
                        }
                    }
                    else
                    {
                        using (StreamWriter wtyeu = new StreamWriter(this.jsonPath))
                        {
                            wtyeu.Write(sw.GetStringBuilder().ToString());
                            wtyeu.Flush();
                        }
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将ArrayList集合写入到JSON出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 将界面数据写入到JSON
        /// </summary>
        /// <param name="form">界面数据</param>
        /// <returns></returns>
        public int write_Control(Form form)
        {
            try
            {
                #region 获取控件的值
                ArrayList arrayList = new ArrayList();

                FieldInfo[] fieldInfo = form.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance); //反射

                for (int i = 0; i < fieldInfo.Length; i++)
                {
                    if (fieldInfo[i].FieldType.Namespace == "System.Windows.Forms")
                    {
                        if (fieldInfo[i].FieldType.Name == "TextBox[]" || 
                            fieldInfo[i].FieldType.Name == "CheckBox[]" || 
                            fieldInfo[i].FieldType.Name == "SaveFileDialog" ||
                            fieldInfo[i].FieldType.Name == "ToolStripMenuItem" ||
                            fieldInfo[i].FieldType.Name == "Panel" ||
                            fieldInfo[i].FieldType.Name == "TableLayoutPanel" ||
                            fieldInfo[i].FieldType.Name == "Button") 
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

                            Items_Control control_Items = new Items_Control();
                            control_Items.ID = i;
                            control_Items.type = fieldInfo[i].FieldType.Name;
                            control_Items.name = con.Name;
                            control_Items.value = con.Checked.ToString();
                            control_Items.describe = con.AccessibleName;
                            arrayList.Add(control_Items);
                        }
                        else if (fieldInfo[i].FieldType.Name == "CheckBox")
                        {
                            CheckBox con = (CheckBox)form.GetType().GetField(fieldInfo[i].Name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);

                            Items_Control control_Items = new Items_Control();
                            control_Items.ID = i;
                            control_Items.type = fieldInfo[i].FieldType.Name;
                            control_Items.name = con.Name;
                            control_Items.value = con.Checked.ToString();
                            control_Items.describe = con.AccessibleName;
                            arrayList.Add(control_Items);
                        }
                        else
                        {
                            Control con = (Control)form.GetType().GetField(fieldInfo[i].Name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);

                            Items_Control control_Items = new Items_Control();
                            control_Items.ID = i;
                            control_Items.type = fieldInfo[i].FieldType.Name;
                            control_Items.name = con.Name;
                            control_Items.value = con.Text;
                            control_Items.describe = con.AccessibleName;
                            arrayList.Add(control_Items);
                        }
                        EndHandle: { };

                    }
                }
                #endregion

                using (StringWriter sw = new StringWriter())
                {
                    serializer.Serialize(new JsonTextWriter(sw), arrayList);

                    if (formatting)
                    {
                        using (StreamWriter wtyeu = new StreamWriter(this.jsonPath))
                        {
                            TextReader tr = new StringReader(sw.ToString());
                            JsonTextReader jtr = new JsonTextReader(tr);
                            object obj = serializer.Deserialize(jtr);

                            wtyeu.Write(obj.ToString());
                            wtyeu.Flush();
                        }
                    }
                    else
                    {
                        using (StreamWriter wtyeu = new StreamWriter(this.jsonPath))
                        {
                            wtyeu.Write(sw.GetStringBuilder().ToString());
                            wtyeu.Flush();
                        }
                    }

                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将ArrayList集合写入到JSON出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 将DataGridView数据写入到JSON
        /// </summary>
        /// <param name="dataGridView">DataGridView控件数据</param>
        /// <returns></returns>
        public int write_dataGridView(DataGridView dataGridView)
        {
            try
            {
                DataTable dataTable = (dataGridView.DataSource as DataTable);
                //DataTable dataTable = FormToolsAction.dataGridView_to_dataTable(dataGridView);

                using (StringWriter sw = new StringWriter())
                {
                    serializer.Serialize(new JsonTextWriter(sw), dataTable);

                    if (formatting)
                    {
                        using (StreamWriter wtyeu = new StreamWriter(this.jsonPath))
                        {
                            TextReader tr = new StringReader(sw.ToString());
                            JsonTextReader jtr = new JsonTextReader(tr);
                            object obj = serializer.Deserialize(jtr);

                            wtyeu.Write(obj.ToString());
                            wtyeu.Flush();
                        }
                    }
                    else
                    {
                        using (StreamWriter wtyeu = new StreamWriter(this.jsonPath))
                        {
                            wtyeu.Write(sw.GetStringBuilder().ToString());
                            wtyeu.Flush();
                        }
                    }

                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将DataTable数据写入到JSON出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 将DataTable数据写入到JSON
        /// </summary>
        /// <param name="dataTable">DataTable数据</param>
        /// <returns></returns>
        public int write_dataTable(DataTable dataTable)
        {
            try
            {
                using (StringWriter sw = new StringWriter())
                {
                    serializer.Serialize(new JsonTextWriter(sw), dataTable);

                    if (formatting)
                    {
                        using (StreamWriter wtyeu = new StreamWriter(this.jsonPath))
                        {
                            TextReader tr = new StringReader(sw.ToString());
                            JsonTextReader jtr = new JsonTextReader(tr);
                            object obj = serializer.Deserialize(jtr);

                            wtyeu.Write(obj.ToString());
                            wtyeu.Flush();
                        }
                    }
                    else
                    {
                        using (StreamWriter wtyeu = new StreamWriter(this.jsonPath))
                        {
                            wtyeu.Write(sw.GetStringBuilder().ToString());
                            wtyeu.Flush();
                        }
                    }

                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将DataTable数据写入到JSON出错！！！");
                return -1;
            }
        }

        #endregion

        #region 数据操作-读

        /// <summary>
        /// 读取JSON数据-Array
        /// </summary>
        /// <param name="array">数组</param>
        /// <returns></returns>
        public int read_array(ref string[] array)
        {
            try
            {
                using (System.IO.StreamReader file = System.IO.File.OpenText(jsonPath))
                {
                    string text = file.ReadToEnd();
                    List<Items_Json> json_Items = JsonConvert.DeserializeObject<List<Items_Json>>(text);
                    for (int i = 0; i < json_Items.Count; i++)
                    {
                        array[i] = json_Items[i].value;
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取JSON数据-Array出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 读取JSON数据-ListBox
        /// </summary>
        /// <param name="listBox">ListBox界面控件</param>
        /// <returns></returns>
        public int read_listBox(ref ListBox listBox)
        {
            try
            {
                using (System.IO.StreamReader file = System.IO.File.OpenText(jsonPath))
                {
                    string text = file.ReadToEnd();
                    List<Items_Json> json_Items = JsonConvert.DeserializeObject<List<Items_Json>>(text);
                    for (int i = 0; i < json_Items.Count; i++)
                    {
                        listBox.Items.Add(json_Items[i].value);
                    }
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取JSON数据-ListBox出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 读取JSON数据-ArrayList
        /// </summary>
        /// <param name="arrayList">ArrayList集合</param>
        /// <returns></returns>
        public int read_arrayList(ref ArrayList arrayList)
        {
            try
            {
                using (System.IO.StreamReader file = System.IO.File.OpenText(jsonPath))
                {
                    string text = file.ReadToEnd();
                    arrayList = JsonConvert.DeserializeObject<ArrayList>(text);
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取JSON数据-List<T>出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 读取JSON数据-T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        public int read_class<T>(ref T model)
        {
            try
            {
                using (System.IO.StreamReader file = System.IO.File.OpenText(jsonPath))
                {
                    string text = file.ReadToEnd();
                    model = JsonConvert.DeserializeObject<T>(text);
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取JSON数据-T出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 读取JSON数据-ListT
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="listT">listT集合</param>
        /// <returns></returns>
        public int read_listT<T>(ref List<T> listT) where T : new()
        {
            try
            {
                using (System.IO.StreamReader file = System.IO.File.OpenText(jsonPath))
                {
                    string text = file.ReadToEnd();
                    listT = JsonConvert.DeserializeObject<List<T>>(text);
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取JSON数据-List<T>出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 读取JSON数据-ListObj
        /// </summary>
        /// <param name="listObj">listObj集合</param>
        /// <returns></returns>
        public int read_listObj(ref List<object> listObj)
        {
            try
            {
                using (System.IO.StreamReader file = System.IO.File.OpenText(jsonPath))
                {
                    string text = file.ReadToEnd();
                    listObj = JsonConvert.DeserializeObject<List<object>>(text);
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取JSON数据-List<T>出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 读取JSON数据-ListControl
        /// </summary>
        /// <param name="listControl">list界面控件</param>
        /// <returns></returns>
        public int read_listControl(ref List<Control> listControl)
        {
            try
            {
                using (System.IO.StreamReader file = System.IO.File.OpenText(jsonPath))
                {
                    string text = file.ReadToEnd();
                    listControl = JsonConvert.DeserializeObject<List<Control>>(text);
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取JSON数据-List<Control>出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 读取JSON数据-界面
        /// </summary>
        /// <param name="form">界面</param>
        /// <returns></returns>
        public int read_Control(Form form)
        {
            try
            {
                FieldInfo[] fieldInfo = form.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance); //反射
                ArrayList array = new ArrayList();
                for (int i = 0; i < fieldInfo.Length; i++)
                {
                    array.Add(fieldInfo[i].Name);
                }

                using (System.IO.StreamReader file = System.IO.File.OpenText(jsonPath))
                {
                    string text = file.ReadToEnd();
                    List<Items_Control> listControl = JsonConvert.DeserializeObject<List<Items_Control>>(text);

                    for (int i = 0; i < listControl.Count; i++)
                    {
                        if (listControl[i].value != "")
                        {
                            if (array.Contains(listControl[i].name))  //判断控件是否存在
                            {
                                if (listControl[i].type == "Button")
                                {
                                    Button con = (Button)form.GetType().GetField(listControl[i].name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).GetValue(form);
                                    con.Text = listControl[i].value;
                                }
                                else if (listControl[i].type == "TextBox")
                                {
                                    TextBox con = (TextBox)form.GetType().GetField(listControl[i].name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).GetValue(form);
                                    con.Text = listControl[i].value;
                                }
                                else if (listControl[i].type == "RadioButton")
                                {
                                    RadioButton con = (RadioButton)form.GetType().GetField(listControl[i].name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).GetValue(form);
                                    con.Checked = (listControl[i].value == "False") ? false : true;
                                }
                                else if (listControl[i].type == "CheckBox")
                                {
                                    CheckBox con = (CheckBox)form.GetType().GetField(listControl[i].name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).GetValue(form);
                                    con.Checked = (listControl[i].value == "False") ? false : true;
                                }
                                else if (listControl[i].type == "ComboBox")
                                {
                                    ComboBox con = (ComboBox)form.GetType().GetField(listControl[i].name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).GetValue(form);
                                    con.Text = listControl[i].value;
                                }
                            }
                        }
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取JSON数据-界面出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 读取JSON数据-DataGridView
        /// </summary>
        /// <param name="dataGridView">DataGridView控件</param>
        /// <returns></returns>
        public int read_dataGridView(ref DataGridView dataGridView)
        {
            try
            {
                using (System.IO.StreamReader file = System.IO.File.OpenText(jsonPath))
                {
                    string text = file.ReadToEnd();
                    DataTable dataTable = JsonConvert.DeserializeObject<DataTable>(text);
                    dataGridView.DataSource = dataTable;
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取JSON数据-DataGridView出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// 读取JSON数据-DataTable
        /// </summary>
        /// <param name="dataTable">DataTable数据集</param>
        /// <returns></returns>
        public int read_dataTable(ref DataTable dataTable)
        {
            try
            {
                using (System.IO.StreamReader file = System.IO.File.OpenText(jsonPath))
                {
                    string text = file.ReadToEnd();
                    dataTable = JsonConvert.DeserializeObject<DataTable>(text);
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取JSON数据-DataTable出错！！！");
                return -1;
            }
        }

        public int write_string(string value)
        {
            throw new NotImplementedException();
        }

        public int write_dictionary(Dictionary<object, object> data)
        {
            throw new NotImplementedException();
        }

        public int read_string(ref string value)
        {
            throw new NotImplementedException();
        }

        public int read_dictionary(ref Dictionary<object, object> data)
        {
            throw new NotImplementedException();
        }

        public int write_list_string(List<string> value)
        {
            try
            {
                using (StringWriter sw = new StringWriter())
                {
                    serializer.Serialize(new JsonTextWriter(sw), value);

                    if (formatting)
                    {
                        using (StreamWriter wtyeu = new StreamWriter(this.jsonPath))
                        {
                            TextReader tr = new StringReader(sw.ToString());
                            JsonTextReader jtr = new JsonTextReader(tr);
                            object obj = serializer.Deserialize(jtr);

                            wtyeu.Write(obj.ToString());
                            wtyeu.Flush();
                        }
                    }
                    else
                    {
                        using (StreamWriter wtyeu = new StreamWriter(this.jsonPath))
                        {
                            wtyeu.Write(sw.GetStringBuilder().ToString());
                            wtyeu.Flush();
                        }
                    }

                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将ArrayList集合写入到JSON出错！！！");
                return -1;
            }
        }

        public int read_list_string(ref List<string> value)
        {
            try
            {
                using (System.IO.StreamReader file = System.IO.File.OpenText(jsonPath))
                {
                    string text = file.ReadToEnd();
                    value = JsonConvert.DeserializeObject<List<string>>(text);
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取JSON数据-List<T>出错！！！");
                return -1;
            }
        }

        #endregion

    }

    #region  序列化实体类
    class Items_Json
    {
        private int _id;             //ID
        private string _value;       //值
        public int id { set { _id = value; } get { return _id; } }
        public string value { set { _value = value; } get { return _value; } }
    }
    class Items_Control
    {
        private int _id;            //ID
        private string _type;       //类型
        private string _name;       //名称
        private string _value;      //值
        private string _describe;   //描述
        public int ID { set { _id = value; } get { return _id; } }
        public string type { set { _type = value; } get { return _type; } }
        public string name { set { _name = value; } get { return _name; } }
        public string value { set { _value = value; } get { return _value; } }
        public string describe { set { _describe = value; } get { return _describe; } }
    }

    #endregion

}
