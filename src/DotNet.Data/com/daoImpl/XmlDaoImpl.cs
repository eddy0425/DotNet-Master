using DotNet.Data.dao;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;

namespace DotNet.Data.daoImpl
{
    class XmlDaoImpl : XmlDao
    {
        private const string LogTag = "XmlDaoImpl";

        string xmlPath = null;  //文件路径

        /// <summary>
        /// 初始化XML文件
        /// </summary>
        /// <param name="filePath">XML文件路径</param>
        /// <returns></returns>
        public int init(string filePath)
        {
            try
            {
                this.xmlPath = filePath;

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
                DataLog.Exception(LogTag, ex, "初始化XML文件,出错！！！");
                return -1;
            }
        }

        #region 数据操作-写

        /// <summary>
        /// 将数组写入到XML
        /// </summary>
        /// <param name="array">数据</param>
        /// <returns></returns>
        public int write_array(string[] array)
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                XmlDeclaration xmldecl = xml.CreateXmlDeclaration("1.0", "utf-8", null); //加入XML的声明段落,<?xml version="1.0" encoding="utf-8"?>
                xml.AppendChild(xmldecl);
                xmldecl.Clone();

                XmlElement root = xml.CreateElement("root");
                xml.AppendChild(root);
                xml.Save(xmlPath);
                xml.Load(xmlPath);
                root = xml.DocumentElement;
                root.RemoveAll();

                for (int i = 0; i < array.Length; i++)
                {
                    XmlElement tempElt = xml.CreateElement("Node");
                    tempElt.InnerText = array[i].ToString();
                    root.AppendChild(tempElt);
                    tempElt.Clone();
                }

                xml.Save(xmlPath);
                root.Clone();
                xml.Clone();
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将数组写入到XML,出错！！！");
                xml.Clone();
                return -1;
            }
        }

        /// <summary>
        /// 将ListBox数据写入到XML
        /// </summary>
        /// <param name="listBox">ListBox界面控件</param>
        /// <returns></returns>
        public int write_listBox(ListBox listBox)
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                XmlDeclaration xmldecl = xml.CreateXmlDeclaration("1.0", "utf-8", null); //加入XML的声明段落,<?xml version="1.0" encoding="utf-8"?>
                xml.AppendChild(xmldecl);
                xmldecl.Clone();

                XmlElement root = xml.CreateElement("root");
                xml.AppendChild(root);
                xml.Save(xmlPath);
                xml.Load(xmlPath);
                root = xml.DocumentElement;
                root.RemoveAll();

                foreach (string row in listBox.Items)
                {
                    XmlElement tempElt = xml.CreateElement("Node");
                    tempElt.InnerText = row;
                    root.AppendChild(tempElt);
                    tempElt.Clone();
                }

                xml.Save(xmlPath);
                root.Clone();
                xml.Clone();
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将ListBox数据写入到XML,出错！！！");
                xml.Clone();
                return -1;
            }
        }

        /// <summary>
        /// 将ArrayList集合写入到XML
        /// </summary>
        /// <param name="arrayList">数据</param>
        /// <returns></returns>
        public int write_arrayList(ArrayList arrayList)
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                XmlDeclaration xmldecl = xml.CreateXmlDeclaration("1.0", "utf-8", null); //加入XML的声明段落,<?xml version="1.0" encoding="utf-8"?>
                xml.AppendChild(xmldecl);
                xmldecl.Clone();

                XmlElement root = xml.CreateElement("root");
                xml.AppendChild(root);
                xml.Save(xmlPath);
                xml.Load(xmlPath);
                root = xml.DocumentElement;
                root.RemoveAll();

                for (int i = 0; i < arrayList.Count; i++)
                {
                    XmlElement xNode = xml.CreateElement(arrayList[i].GetType().Name);

                    xNode.SetAttribute("Number", i.ToString());
                    foreach (PropertyInfo pi in arrayList[i].GetType().GetProperties())
                    {
                        //xNode.SetAttribute(GetAttribute.Fun_getPropertyTextAttr(arrayList[i], pi.Name), pi.GetValue(arrayList[i], null).ToString());
                        xNode.SetAttribute(pi.Name, pi.GetValue(arrayList[i], null).ToString());
                    }
                    root.AppendChild(xNode);
                    xNode.Clone();
                }

                xml.Save(xmlPath);
                root.Clone();
                xml.Clone();
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将ArrayList集合写入到XML,出错！！！");
                xml.Clone();
                return -1;
            }
        }

        /// <summary>
        /// 将ArrayList集合写入到XML2
        /// </summary>
        /// <param name="arrayList">数据</param>
        /// <returns></returns>
        public int write_arrayList2(ArrayList arrayList)
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                XmlDeclaration xmldecl = xml.CreateXmlDeclaration("1.0", "utf-8", null); //加入XML的声明段落,<?xml version="1.0" encoding="utf-8"?>
                xml.AppendChild(xmldecl);
                xmldecl.Clone();

                XmlElement root = xml.CreateElement("root");
                xml.AppendChild(root);
                xml.Save(xmlPath);
                xml.Load(xmlPath);
                root = xml.DocumentElement;
                root.RemoveAll();

                for (int i = 0; i < arrayList.Count; i++)
                {
                    XmlElement xNode = xml.CreateElement(arrayList[i].GetType().Name);
                    xNode.SetAttribute("Number", i.ToString());
                    foreach (PropertyInfo pi in arrayList[i].GetType().GetProperties())
                    {
                        XmlElement tempElt = xml.CreateElement(pi.Name);
                        tempElt.SetAttribute("Text", GetAttribute.Fun_getPropertyTextAttr(arrayList[i], pi.Name));
                        tempElt.SetAttribute("value", pi.GetValue(arrayList[i], null).ToString());
                        //tempElt.InnerText = pi.GetValue(arrayList[i], null).ToString();
                        xNode.AppendChild(tempElt);
                    }
                    root.AppendChild(xNode);
                }

                xml.Save(xmlPath);
                root.Clone();
                xml.Clone();
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将ArrayList集合写入到XML2,出错！！！");
                xml.Clone();
                return -1;
            }
        }

        /// <summary>
        /// 将listT集合写入到XML
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="listT">数据</param>
        /// <returns></returns>
        public int write_listT<T>(List<T> listT)
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                XmlDeclaration xmldecl = xml.CreateXmlDeclaration("1.0", "utf-8", null); //加入XML的声明段落,<?xml version="1.0" encoding="utf-8"?>
                xml.AppendChild(xmldecl);
                xmldecl.Clone();

                XmlElement root = xml.CreateElement("root");
                xml.AppendChild(root);
                xml.Save(xmlPath);
                xml.Load(xmlPath);
                root = xml.DocumentElement;
                root.RemoveAll();

                for (int i = 0; i < listT.Count; i++)
                {
                    XmlElement xNode = xml.CreateElement(listT[i].GetType().Name);

                    xNode.SetAttribute("Number", i.ToString());
                    foreach (PropertyInfo pi in listT[i].GetType().GetProperties())
                    {
                        //xNode.SetAttribute(GetAttribute.Fun_getPropertyTextAttr(listT[i], pi.Name), pi.GetValue(listT[i], null).ToString());
                        xNode.SetAttribute(pi.Name, pi.GetValue(listT[i], null).ToString());
                    }
                    root.AppendChild(xNode);
                    xNode.Clone();
                }

                xml.Save(xmlPath);
                root.Clone();
                xml.Clone();
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将listT集合写入到XML,出错！！！");
                xml.Clone();
                return -1;
            }
        }

        /// <summary>
        /// 将listT集合写入到XML2
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="listT">数据</param>
        /// <returns></returns>
        public int write_listT2<T>(List<T> listT)
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                XmlDeclaration xmldecl = xml.CreateXmlDeclaration("1.0", "utf-8", null); //加入XML的声明段落,<?xml version="1.0" encoding="utf-8"?>
                xml.AppendChild(xmldecl);
                xmldecl.Clone();

                XmlElement root = xml.CreateElement("root");
                xml.AppendChild(root);
                xml.Save(xmlPath);
                xml.Load(xmlPath);
                root = xml.DocumentElement;
                root.RemoveAll();

                for (int i = 0; i < listT.Count; i++)
                {
                    XmlElement xNode = xml.CreateElement(listT[i].GetType().Name);
                    xNode.SetAttribute("Number", i.ToString());
                    foreach (PropertyInfo pi in listT[i].GetType().GetProperties())
                    {
                        XmlElement tempElt = xml.CreateElement(pi.Name);
                        tempElt.SetAttribute("Text", GetAttribute.Fun_getPropertyTextAttr(listT[i], pi.Name));
                        tempElt.SetAttribute("value", pi.GetValue(listT[i], null).ToString());
                        //tempElt.InnerText = pi.GetValue(listT[i], null).ToString();
                        xNode.AppendChild(tempElt);
                    }
                    root.AppendChild(xNode);
                }

                xml.Save(xmlPath);
                root.Clone();
                xml.Clone();
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将listT集合写入到XML2,出错！！！");
                xml.Clone();
                return -1;
            }
        }

        /// <summary>
        /// 将listObj集合写入到XML
        /// </summary>
        /// <param name="listObj">数据</param>
        /// <returns></returns>
        public int write_listObj(List<object> listObj)
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                XmlDeclaration xmldecl = xml.CreateXmlDeclaration("1.0", "utf-8", null); //加入XML的声明段落,<?xml version="1.0" encoding="utf-8"?>
                xml.AppendChild(xmldecl);
                xmldecl.Clone();

                XmlElement root = xml.CreateElement("root");
                xml.AppendChild(root);
                xml.Save(xmlPath);
                xml.Load(xmlPath);
                root = xml.DocumentElement;
                root.RemoveAll();

                for (int i = 0; i < listObj.Count; i++)
                {
                    XmlElement xNode = xml.CreateElement(listObj[i].GetType().Name);

                    xNode.SetAttribute("Number", i.ToString());
                    foreach (PropertyInfo pi in listObj[i].GetType().GetProperties())
                    {
                        //xNode.SetAttribute(GetAttribute.Fun_getPropertyTextAttr(listObj[i], pi.Name), pi.GetValue(listObj[i], null).ToString());
                        xNode.SetAttribute(pi.Name, pi.GetValue(listObj[i], null).ToString());
                    }
                    root.AppendChild(xNode);
                    xNode.Clone();
                }
                xml.Save(xmlPath);
                root.Clone();
                xml.Clone();
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将listObj集合写入到XML,出错！！！");
                xml.Clone();
                return -1;
            }
        }

        /// <summary>
        /// 将listObj集合写入到XML2
        /// </summary>
        /// <param name="listObj">数据</param>
        /// <returns></returns>
        public int write_listObj2(List<object> listObj)
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                XmlDeclaration xmldecl = xml.CreateXmlDeclaration("1.0", "utf-8", null); //加入XML的声明段落,<?xml version="1.0" encoding="utf-8"?>
                xml.AppendChild(xmldecl);
                xmldecl.Clone();

                XmlElement root = xml.CreateElement("root");
                xml.AppendChild(root);
                xml.Save(xmlPath);
                xml.Load(xmlPath);
                root = xml.DocumentElement;
                root.RemoveAll();

                for (int i = 0; i < listObj.Count; i++)
                {
                    XmlElement xNode = xml.CreateElement(listObj[i].GetType().Name);
                    xNode.SetAttribute("Number", i.ToString());
                    foreach (PropertyInfo pi in listObj[i].GetType().GetProperties())
                    {
                        XmlElement tempElt = xml.CreateElement(pi.Name);
                        tempElt.SetAttribute("Text", GetAttribute.Fun_getPropertyTextAttr(listObj[i], pi.Name));

                        if (listObj[i].GetType().GetProperty(pi.Name).PropertyType.IsPrimitive ||
                            listObj[i].GetType().GetProperty(pi.Name).PropertyType == typeof(string))
                        {
                            if (listObj[i].GetType().GetProperty(pi.Name).PropertyType != typeof(IntPtr))
                            {
                                tempElt.SetAttribute("value", pi.GetValue(listObj[i], null).ToString());
                                xNode.AppendChild(tempElt);
                            }
                        }
                        //else { tempElt.SetAttribute("value", ""); }
                    }
                    root.AppendChild(xNode);
                }

                //for (int i = 0; i < listObj.Count; i++)
                //{
                //    XmlElement xNode = xml.CreateElement(listObj[i].GetType().ToString());

                //    xNode.SetAttribute("Number", i.ToString());
                //    foreach (PropertyInfo pi in listObj[i].GetType().GetProperties())
                //    {
                //        xNode.SetAttribute(GetAttribute.Fun_getPropertyTextAttr(listObj[i], pi.Name), pi.GetValue(listObj[i], null).ToString());
                //    }
                //    root.AppendChild(xNode);
                //    xNode.Clone();
                //}

                xml.Save(xmlPath);
                root.Clone();
                xml.Clone();
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将listObj集合写入到XML2,出错！！！");
                xml.Clone();
                return -1;
            }
        }

        /// <summary>
        /// 将listControl集合写入到XML
        /// </summary>
        /// <param name="listControl"></param>
        /// <returns></returns>
        public int write_listControl(List<Control> listControl)
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                XmlDeclaration xmldecl = xml.CreateXmlDeclaration("1.0", "utf-8", null); //加入XML的声明段落,<?xml version="1.0" encoding="utf-8"?>
                xml.AppendChild(xmldecl);
                xmldecl.Clone();

                XmlElement root = xml.CreateElement("root");
                xml.AppendChild(root);
                xml.Save(xmlPath);
                xml.Load(xmlPath);
                root = xml.DocumentElement;
                root.RemoveAll();

                for (int i = 0; i < listControl.Count; i++)
                {
                    if (listControl[i].GetType().Name == "RadioButton")
                    {
                        XmlElement tempElt = xml.CreateElement("Node");
                        tempElt.SetAttribute("type", listControl[i].GetType().Name);
                        tempElt.SetAttribute("name", listControl[i].Name);
                        RadioButton radioButton = (RadioButton)listControl[i];
                        //tempElt.InnerText = radioButton.Checked.ToString();
                        tempElt.SetAttribute("value", radioButton.Checked.ToString());
                        tempElt.SetAttribute("describe", listControl[i].AccessibleName);
                        root.AppendChild(tempElt);
                        tempElt.Clone();
                    }
                    else if (listControl[i].GetType().Name == "CheckBox")
                    {
                        XmlElement tempElt = xml.CreateElement("Node");
                        tempElt.SetAttribute("type", listControl[i].GetType().Name);
                        tempElt.SetAttribute("name", listControl[i].Name);
                        CheckBox radioButton = (CheckBox)listControl[i];
                        //tempElt.InnerText = radioButton.Checked.ToString();
                        tempElt.SetAttribute("value", radioButton.Checked.ToString());
                        tempElt.SetAttribute("describe", listControl[i].AccessibleName);
                        root.AppendChild(tempElt);
                        tempElt.Clone();
                    }
                    else
                    {
                        XmlElement tempElt = xml.CreateElement("Node");
                        tempElt.SetAttribute("type", listControl[i].GetType().Name);
                        tempElt.SetAttribute("name", listControl[i].Name);
                        //tempElt.InnerText = listControl[i].Text;
                        tempElt.SetAttribute("value", listControl[i].Text);
                        tempElt.SetAttribute("describe", listControl[i].AccessibleName);
                        root.AppendChild(tempElt);
                        tempElt.Clone();
                    }
                }
                xml.Save(xmlPath);
                root.Clone();
                xml.Clone();
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将listControl集合写入到XML,出错！！！");
                xml.Clone();
                return -1;
            }
        }

        /// <summary>
        /// 将界面数据写入到XML
        /// </summary>
        /// <param name="form">界面数据</param>
        /// <returns></returns>
        public int write_Control(Form form)
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                XmlDeclaration xmldecl = xml.CreateXmlDeclaration("1.0", "utf-8", null); //加入XML的声明段落,<?xml version="1.0" encoding="utf-8"?>
                xml.AppendChild(xmldecl);
                xmldecl.Clone();

                XmlElement root = xml.CreateElement("root");
                xml.AppendChild(root);
                xml.Save(xmlPath);
                xml.Load(xmlPath);
                root = xml.DocumentElement;
                root.RemoveAll();

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

                            XmlElement tempElt = xml.CreateElement("Node");
                            tempElt.SetAttribute("type", fieldInfo[i].FieldType.Name);
                            tempElt.SetAttribute("name", con.Name);
                            tempElt.SetAttribute("value", con.Checked.ToString());
                            tempElt.SetAttribute("describe", con.AccessibleName);
                            //tempElt.InnerText = con.Checked.ToString();
                            root.AppendChild(tempElt);
                            tempElt.Clone();
                        }
                        else if (fieldInfo[i].FieldType.Name == "CheckBox")
                        {
                            CheckBox con = (CheckBox)form.GetType().GetField(fieldInfo[i].Name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);

                            XmlElement tempElt = xml.CreateElement("Node");
                            tempElt.SetAttribute("type", fieldInfo[i].FieldType.Name);
                            tempElt.SetAttribute("name", con.Name);
                            tempElt.SetAttribute("value", con.Checked.ToString());
                            tempElt.SetAttribute("describe", con.AccessibleName);
                            //tempElt.InnerText = con.Checked.ToString();
                            root.AppendChild(tempElt);
                            tempElt.Clone();
                        }
                        else
                        {
                            Control con = (Control)form.GetType().GetField(fieldInfo[i].Name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);

                            XmlElement tempElt = xml.CreateElement("Node");
                            tempElt.SetAttribute("type", fieldInfo[i].FieldType.Name);
                            tempElt.SetAttribute("name", con.Name);
                            tempElt.SetAttribute("value", con.Text);
                            tempElt.SetAttribute("describe", con.AccessibleName);
                            //tempElt.InnerText = con.Text;
                            root.AppendChild(tempElt);
                            tempElt.Clone();
                        }
                        EndHandle: { };

                    }
                }

                xml.Save(xmlPath);
                root.Clone();
                xml.Clone();
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将界面数据写入到XML,出错！！！");
                xml.Clone();
                return -1;
            }
        }

        /// <summary>
        /// 将DataGridView数据写入到XML
        /// </summary>
        /// <param name="dataGridView">DataGridView控件数据</param>
        /// <returns></returns>
        public int write_dataGridView(DataGridView dataGridView)
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                XmlDeclaration xmldecl = xml.CreateXmlDeclaration("1.0", "utf-8", null); //加入XML的声明段落,<?xml version="1.0" encoding="utf-8"?>
                xml.AppendChild(xmldecl);
                xmldecl.Clone();

                XmlElement root = xml.CreateElement("root");
                xml.AppendChild(root);
                xml.Save(xmlPath);
                xml.Load(xmlPath);
                root = xml.DocumentElement;
                root.RemoveAll();

                for (int i = 0; i < dataGridView.Rows.Count; i++)
                {
                    XmlElement xNode = xml.CreateElement("Node");

                    for (int j = 0; j < dataGridView.Columns.Count; j++)
                    {
                        xNode.SetAttribute(dataGridView.Columns[j].HeaderText, dataGridView[j, i].Value.ToString());
                    }
                    root.AppendChild(xNode);
                    xNode.Clone();
                }
                xml.Save(xmlPath);
                root.Clone();
                xml.Clone();
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将DataGridView数据写入到XML,出错！！！");
                xml.Clone();
                return -1;
            }
        }

        /// <summary>
        /// 将DataTable数据写入到XML
        /// </summary>
        /// <param name="dataTable">DataTable数据</param>
        /// <returns></returns>
        public int write_dataTable(DataTable dataTable)
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                XmlDeclaration xmldecl = xml.CreateXmlDeclaration("1.0", "utf-8", null); //加入XML的声明段落,<?xml version="1.0" encoding="utf-8"?>
                xml.AppendChild(xmldecl);
                xmldecl.Clone();

                XmlElement root = xml.CreateElement("root");
                xml.AppendChild(root);
                xml.Save(xmlPath);
                xml.Load(xmlPath);
                root = xml.DocumentElement;
                root.RemoveAll();

                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    XmlElement xNode = xml.CreateElement("Node");

                    for (int j = 0; j < dataTable.Columns.Count; j++)
                    {
                        xNode.SetAttribute(dataTable.Columns[j].ToString(), dataTable.Rows[i].ItemArray[j].ToString());
                    }
                    root.AppendChild(xNode);
                    xNode.Clone();
                }

                xml.Save(xmlPath);
                root.Clone();
                xml.Clone();
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将DataTable数据写入到XML,出错！！！");
                xml.Clone();
                return -1;
            }
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
            XmlDocument xml = new XmlDocument();
            try
            {
                xml.Load(xmlPath);
                XmlElement root = xml.DocumentElement;

                if (root == null || root.ChildNodes.Count < 1) throw new Exception();
                XmlNodeList rootList = root.ChildNodes;
                for (int i = 0; i < rootList.Count; i++)
                {
                    array[i] = rootList[i].InnerText;
                }
                xml.Clone();
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取XML文件获取返回值-Array,出错！！！");
                xml.Clone();
                return -1;
            }
        }

        /// <summary>
        /// 读取XML数据-ListBox
        /// </summary>
        /// <param name="listBox">ListBox界面控件</param>
        /// <returns></returns>
        public int read_listBox(ref ListBox listBox)
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                xml.Load(xmlPath);
                XmlElement root = xml.DocumentElement;

                if (root == null || root.ChildNodes.Count < 1) throw new Exception();
                XmlNodeList rootList = root.ChildNodes;
                for (int i = 0; i < rootList.Count; i++)
                {
                    listBox.Items.Add(rootList[i].InnerText);
                }
                xml.Clone();
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取XML数据-ListBox,出错！！！");
                xml.Clone();
                return -1;
            }
        }

        /// <summary>
        /// 读取XML数据-ArrayList
        /// </summary>
        /// <param name="arrayList">ArrayList集合</param>
        /// <returns></returns>
        public int read_arrayList(ref ArrayList arrayList)
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                xml.Load(xmlPath);
                XmlElement root = xml.DocumentElement;

                if (root == null || root.ChildNodes.Count < 1) return -2;
                XmlNodeList classList = root.ChildNodes;
                for (int i = 0; i < classList.Count; i++)
                {
                    //XmlNodeList rootList = classList[i].ChildNodes;
                    //命名空间
                    var nameSpaceAll = Assembly.GetEntryAssembly().GetTypes();
                    //var nameSpaceAll = Assembly.GetExecutingAssembly().GetTypes();
                    Type t = nameSpaceAll.Where(x => x.Name == classList[i].Name).FirstOrDefault();

                    if (t != null)
                    {
                        object obj = Activator.CreateInstance(t);

                        for (int j = 0; j < classList[i].Attributes.Count; j++)
                        {
                            string strName = classList[i].Attributes[j].Name;
                            string strValue = classList[i].Attributes[j].Value;
                            if (strName != "Number")
                            {
                                object v = Convert.ChangeType(strValue, obj.GetType().GetProperty(strName).PropertyType);
                                obj.GetType().GetProperty(strName).SetValue(obj, v, null);
                            }
                        }
                        arrayList.Add(obj);
                    }
                    else { MessageBox.Show("没有找到实体类！！！"); }

                }
                xml.Clone();
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取XML数据-ArrayList,出错！！！");
                xml.Clone();
                return -1;
            }
        }

        /// <summary>
        /// 读取XML数据-ArrayList2
        /// </summary>
        /// <param name="arrayList">ArrayList集合</param>
        /// <returns></returns>
        public int read_arrayList2(ref ArrayList arrayList)
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                xml.Load(xmlPath);
                XmlElement root = xml.DocumentElement;

                if (root == null || root.ChildNodes.Count < 1) return -2;
                XmlNodeList classList = root.ChildNodes;
                for (int i = 0; i < classList.Count; i++)
                {
                    XmlNodeList rootList = classList[i].ChildNodes;
                    //命名空间
                    var nameSpaceAll = Assembly.GetEntryAssembly().GetTypes();
                    //var nameSpaceAll = Assembly.GetExecutingAssembly().GetTypes();
                    Type t = nameSpaceAll.Where(x => x.Name == classList[i].Name).FirstOrDefault();

                    if (t != null)
                    {
                        //Type type = Type.GetType(t.FullName, true, true);
                        object obj = Activator.CreateInstance(t);

                        for (int j = 0; j < rootList.Count; j++)
                        {
                            string strName = rootList[j].Name;
                            string strValue = rootList[j].Attributes["value"].Value;

                            object v = Convert.ChangeType(strValue, obj.GetType().GetProperty(strName).PropertyType);
                            obj.GetType().GetProperty(strName).SetValue(obj, v, null);
                        }
                        arrayList.Add(obj);
                    }
                    else { MessageBox.Show("没有找到实体类！！！"); }

                }
                xml.Clone();
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取XML数据-ArrayList2,出错！！！");
                xml.Clone();
                return -1;
            }
        }

        /// <summary>
        /// 读取XML数据-ListT
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="listT">listT集合</param>
        /// <returns></returns>
        public int read_listT<T>(ref List<T> listT) where T : new()
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                xml.Load(xmlPath);
                XmlElement root = xml.DocumentElement;

                if (root == null || root.ChildNodes.Count < 1) return -2;
                XmlNodeList classList = root.ChildNodes;

                for (int i = 0; i < classList.Count; i++)
                {
                    T model = new T();
                    //object obj = Activator.CreateInstance(listT[i].GetType());

                    for (int j = 0; j < classList[i].Attributes.Count; j++)
                    {
                        string strName = classList[i].Attributes[j].Name;
                        string strValue = classList[i].Attributes[j].Value;
                        if (strName != "Number")
                        {
                            object v = Convert.ChangeType(strValue, model.GetType().GetProperty(strName).PropertyType);
                            model.GetType().GetProperty(strName).SetValue(model, v, null);
                            
                        }
                    }
                    listT.Add(model);

                }
                xml.Clone();
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取XML数据-ListT,出错！！！");
                xml.Clone();
                return -1;
            }
        }

        /// <summary>
        /// 读取XML数据-ListT2
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="listT">listT集合</param>
        /// <returns></returns>
        public int read_listT2<T>(ref List<T> listT) where T : new()
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                xml.Load(xmlPath);
                XmlElement root = xml.DocumentElement;

                if (root == null || root.ChildNodes.Count < 1) return -2;
                XmlNodeList classList = root.ChildNodes;
                for (int i = 0; i < classList.Count; i++)
                {
                    XmlNodeList rootList = classList[i].ChildNodes;

                    T model = new T();

                    for (int j = 0; j < rootList.Count; j++)
                    {
                        string strName = rootList[j].Name;
                        string strValue = rootList[j].Attributes["value"].Value;

                        object v = Convert.ChangeType(strValue, model.GetType().GetProperty(strName).PropertyType);
                        model.GetType().GetProperty(strName).SetValue(model, v, null);
                    }
                    listT.Add(model);

                }
                xml.Clone();
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取XML数据-ListT2,出错！！！");
                xml.Clone();
                return -1;
            }
        }

        /// <summary>
        /// 读取XML数据-ListObj
        /// </summary>
        /// <param name="listObj">listObj集合</param>
        /// <returns></returns>
        public int read_listObj(ref List<object> listObj)
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                xml.Load(xmlPath);
                XmlElement root = xml.DocumentElement;

                if (root == null || root.ChildNodes.Count < 1) return -2;
                XmlNodeList classList = root.ChildNodes;
                for (int i = 0; i < classList.Count; i++)
                {
                    //XmlNodeList rootList = classList[i].ChildNodes;
                    //命名空间
                    var nameSpaceAll = Assembly.GetEntryAssembly().GetTypes();
                    //var nameSpaceAll = Assembly.GetExecutingAssembly().GetTypes();
                    Type t = nameSpaceAll.Where(x => x.Name == classList[i].Name).FirstOrDefault();

                    if (t != null)
                    {
                        object obj = Activator.CreateInstance(t);

                        for (int j = 0; j < classList[i].Attributes.Count; j++)
                        {
                            string strName = classList[i].Attributes[j].Name;
                            string strValue = classList[i].Attributes[j].Value;
                            if (strName != "Number")
                            {
                                object v = Convert.ChangeType(strValue, obj.GetType().GetProperty(strName).PropertyType);
                                obj.GetType().GetProperty(strName).SetValue(obj, v, null);
                            }
                        }
                        listObj.Add(obj);
                    }
                    else { MessageBox.Show("没有找到实体类！！！"); }

                }
                xml.Clone();
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取XML数据-ListObj,出错！！！");
                xml.Clone();
                return -1;
            }
        }

        /// <summary>
        /// 读取XML数据-ListObj2
        /// </summary>
        /// <param name="listObj">listObj集合</param>
        /// <returns></returns>
        public int read_listObj2(ref List<object> listObj)
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                xml.Load(xmlPath);
                XmlElement root = xml.DocumentElement;

                if (root == null || root.ChildNodes.Count < 1) return -2;
                XmlNodeList classList = root.ChildNodes;
                for (int i = 0; i < classList.Count; i++)
                {
                    XmlNodeList rootList = classList[i].ChildNodes;
                    //命名空间
                    var nameSpaceAll = Assembly.GetEntryAssembly().GetTypes();
                    //var nameSpaceAll = Assembly.GetExecutingAssembly().GetTypes();
                    Type t = nameSpaceAll.Where(x => x.Name == classList[i].Name).FirstOrDefault();

                    if (t != null)
                    {
                        //Type type = Type.GetType(t.FullName, true, true);
                        object obj = Activator.CreateInstance(t);

                        for (int j = 0; j < rootList.Count; j++)
                        {
                            string strName = rootList[j].Name;
                            if (obj.GetType().GetProperty(strName).PropertyType.IsPrimitive ||
                                obj.GetType().GetProperty(strName).PropertyType == typeof(string))
                            {
                                if (obj.GetType().GetProperty(strName).PropertyType != typeof(IntPtr))
                                {
                                    string strValue = rootList[j].Attributes["value"].Value;

                                    object v = Convert.ChangeType(strValue, obj.GetType().GetProperty(strName).PropertyType);
                                    obj.GetType().GetProperty(strName).SetValue(obj, v, null);
                                }
                            }
                        }
                        listObj.Add(obj);
                    }
                    else { MessageBox.Show("没有找到实体类！！！"); }

                }
                xml.Clone();
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取XML数据-ListObj2,出错！！！");
                xml.Clone();
                return -1;
            }
        }

        /// <summary>
        /// 读取XML数据-ListObj2
        /// </summary>
        /// <param name="listObj">listObj集合</param>
        /// <param name="assembly">程序集</param>
        /// <returns></returns>
        public int read_listObj2(ref List<object> listObj, Assembly assembly)
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                xml.Load(xmlPath);
                XmlElement root = xml.DocumentElement;

                if (root == null || root.ChildNodes.Count < 1) return -2;
                XmlNodeList classList = root.ChildNodes;
                for (int i = 0; i < classList.Count; i++)
                {
                    XmlNodeList rootList = classList[i].ChildNodes;
                    //命名空间
                    var nameSpaceAll = assembly.GetTypes();
                    //var nameSpaceAll = Assembly.GetExecutingAssembly().GetTypes();
                    Type t = nameSpaceAll.Where(x => x.Name == classList[i].Name).FirstOrDefault();

                    if (t != null)
                    {
                        //Type type = Type.GetType(t.FullName, true, true);
                        object obj = Activator.CreateInstance(t);

                        for (int j = 0; j < rootList.Count; j++)
                        {
                            string strName = rootList[j].Name;
                            //if (obj.GetType().GetProperty(strName).PropertyType.IsPrimitive ||
                            //    obj.GetType().GetProperty(strName).PropertyType == typeof(string))
                            //{
                            //    if (obj.GetType().GetProperty(strName).PropertyType != typeof(IntPtr))
                            //    {
                            //        string strValue = rootList[j].Attributes["value"].Value;

                            //        object v = Convert.ChangeType(strValue, obj.GetType().GetProperty(strName).PropertyType);
                            //        obj.GetType().GetProperty(strName).SetValue(obj, v, null);
                            //    }
                            //}
                            try
                            {
                                if (obj.GetType().GetProperty(strName).PropertyType.IsPrimitive ||
                                    obj.GetType().GetProperty(strName).PropertyType == typeof(string))
                                {
                                    if (obj.GetType().GetProperty(strName).PropertyType != typeof(IntPtr))
                                    {
                                        string strValue = rootList[j].Attributes["value"].Value;

                                        object v = Convert.ChangeType(strValue, obj.GetType().GetProperty(strName).PropertyType);
                                        obj.GetType().GetProperty(strName).SetValue(obj, v, null);
                                    }
                                }
                            }
                            catch { continue; }
                        }
                        listObj.Add(obj);
                    }
                    else { MessageBox.Show("没有找到实体类！！！"); }

                }
                xml.Clone();
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取XML数据-ListObj2,出错！！！");
                xml.Clone();
                return -1;
            }
        }

        /// <summary>
        /// 将listControl集合写入到XML
        /// </summary>
        /// <param name="listControl"></param>
        /// <returns></returns>
        public int read_listControl(ref List<Control> listControl)
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                xml.Load(xmlPath);
                XmlElement root = xml.DocumentElement;

                if (root == null || root.ChildNodes.Count < 1) return -2;
                XmlNodeList rootList = root.ChildNodes;

                for (int i = 0; i < listControl.Count; i++)
                {
                    for (int j = 0; j < rootList.Count; j++)
                    {
                        if (listControl[i].Name == rootList[j].Attributes["name"].Value && rootList[j].Attributes["value"].Value != "")
                        {
                            string type = rootList[j].Attributes["type"].Value;
                            string name = rootList[j].Attributes["name"].Value;
                            string value = rootList[j].Attributes["value"].Value;
                            string describe = rootList[j].Attributes["describe"].Value;

                            if (type == "Button")
                            {
                                Button con = (Button)listControl[i];
                                con.Text = value;
                            }
                            if (type == "TextBox")
                            {
                                TextBox con = (TextBox)listControl[i];
                                con.Text = value;
                            }
                            if (type == "RadioButton")
                            {
                                RadioButton con = (RadioButton)listControl[i];
                                con.Checked = (value == "False") ? false : true;
                            }
                            if (type == "CheckBox")
                            {
                                CheckBox con = (CheckBox)listControl[i];
                                con.Checked = (value == "False") ? false : true;
                            }
                            if (type == "ComboBox")
                            {
                                ComboBox con = (ComboBox)listControl[i];
                                con.Text = value;
                            }
                        }
                    }
                }

                xml.Clone();
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将listControl集合写入到XML,出错！！！");
                xml.Clone();
                return -1;
            }
        }

        /// <summary>
        /// 读取XML数据-界面
        /// </summary>
        /// <param name="form">界面</param>
        /// <returns></returns>
        public int read_Control(Form form)
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                xml.Load(xmlPath);
                XmlElement root = xml.DocumentElement;

                if (root == null || root.ChildNodes.Count < 1) return -2;
                XmlNodeList rootList = root.ChildNodes;

                FieldInfo[] fieldInfo = form.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance); //反射
                ArrayList array = new ArrayList();
                for (int i = 0; i < fieldInfo.Length; i++)
                {
                    array.Add(fieldInfo[i].Name);
                }

                for (int i = 0; i < rootList.Count; i++)
                {
                    if (rootList[i].Attributes["value"].Value != "")
                    {
                        //string value = rootList[i].InnerText;
                        string type = rootList[i].Attributes["type"].Value;
                        string name = rootList[i].Attributes["name"].Value;
                        string value = rootList[i].Attributes["value"].Value;
                        //string describe = rootList[i].Attributes["describe"].Value;
                        //Console.WriteLine(value);
                        if (array.Contains(name))  //判断控件是否存在
                        {
                            if (type == "Button")
                            {
                                Button con = (Button)form.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).GetValue(form);
                                con.Text = value;
                            }
                            if (type == "TextBox")
                            {
                                TextBox con = (TextBox)form.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).GetValue(form);
                                con.Text = value;
                            }
                            if (type == "RadioButton")
                            {
                                RadioButton con = (RadioButton)form.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).GetValue(form);
                                con.Checked = (value == "False") ? false : true;
                            }
                            if (type == "CheckBox")
                            {
                                CheckBox con = (CheckBox)form.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).GetValue(form);
                                con.Checked = (value == "False") ? false : true;
                            }
                            if (type == "ComboBox")
                            {
                                ComboBox con = (ComboBox)form.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase).GetValue(form);
                                con.Text = value;
                            }
                        }
                    }
                }
                xml.Clone();
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取XML数据-界面,出错！！！");
                xml.Clone();
                return -1;
            }
        }

        /// <summary>
        /// 读取XML数据-DataGridView
        /// </summary>
        /// <param name="dataGridView">DataGridView控件</param>
        /// <returns></returns>
        public int read_dataGridView(ref DataGridView dataGridView)
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                xml.Load(xmlPath);
                XmlElement root = xml.DocumentElement;

                if (root == null || root.ChildNodes.Count < 1) return -2;
                XmlNodeList classList = root.ChildNodes;

                DataTable dataTable = new DataTable();
                //添加表头
                dataTable.Columns.Clear(); //清除表格中的数据
                for (int i = 0; i < classList[0].Attributes.Count; i++)
                {
                    dataTable.Columns.Add(classList[0].Attributes[i].Name);  //获取标题
                }

                //添加表内容
                dataTable.Rows.Clear();
                for (int i = 0; i < classList.Count; i++)
                {
                    //dataTable.Rows.Add();
                    ArrayList tenpList = new ArrayList();
                    for (int j = 0; j < classList[i].Attributes.Count; j++)
                    {
                        //dataTable.Rows[i].ItemArray[j] = classList[i].Attributes[j].Value;
                        //Console.WriteLine(classList[i].Attributes[j].Value);
                        tenpList.Add(classList[i].Attributes[j].Value);
                    }
                    object[] array = tenpList.ToArray();
                    dataTable.LoadDataRow(array, true);
                }

                dataGridView.DataSource = dataTable;

                xml.Clone();
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取XML数据-DataGridView,出错！！！");
                xml.Clone();
                return -1;
            }
        }

        /// <summary>
        /// 读取XML数据-DataTable
        /// </summary>
        /// <param name="dataTable">DataTable数据集</param>
        /// <returns></returns>
        public int read_dataTable(ref DataTable dataTable)
        {
            XmlDocument xml = new XmlDocument();
            try
            {
                xml.Load(xmlPath);
                XmlElement root = xml.DocumentElement;

                if (root == null || root.ChildNodes.Count < 1) return -2;
                XmlNodeList classList = root.ChildNodes;

                //添加表头
                dataTable.Columns.Clear(); //清除表格中的数据
                for (int i = 0; i < classList[0].Attributes.Count; i++)
                {
                    dataTable.Columns.Add(classList[0].Attributes[i].Name);  //获取标题
                }

                //添加表内容
                dataTable.Rows.Clear();
                for (int i = 0; i < classList.Count; i++)
                {
                    //dataTable.Rows.Add();
                    ArrayList tenpList = new ArrayList();
                    for (int j = 0; j < classList[i].Attributes.Count; j++)
                    {
                        //dataTable.Rows[i].ItemArray[j] = classList[i].Attributes[j].Value;
                        //Console.WriteLine(classList[i].Attributes[j].Value);
                        tenpList.Add(classList[i].Attributes[j].Value);
                    }
                    object[] array = tenpList.ToArray();
                    dataTable.LoadDataRow(array, true);
                }

                xml.Clone();
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "读取XML数据-DataTable,出错！！！");
                xml.Clone();
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

        #endregion

    }
}
