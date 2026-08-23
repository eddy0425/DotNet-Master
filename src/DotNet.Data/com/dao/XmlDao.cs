using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DotNet.Data.dao
{
    interface XmlDao : Data
    {
        /// <summary>
        /// 初始化XML文件
        /// </summary>
        /// <param name="filePath">XML文件路径</param>
        /// <returns></returns>
        int init(string filePath);

        /// <summary>
        /// 将ArrayList集合写入到XML2
        /// </summary>
        /// <param name="arrayList">数据</param>
        /// <returns></returns>
        int write_arrayList2(ArrayList arrayList);

        /// <summary>
        /// 将listT集合写入到XML2
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="listT">数据</param>
        /// <returns></returns>
        int write_listT2<T>(List<T> listT);

        /// <summary>
        /// 将listObj集合写入到XML2
        /// </summary>
        /// <param name="listObj">数据</param>
        /// <returns></returns>
        int write_listObj2(List<object> listObj);

        /// <summary>
        /// 读取XML数据-ArrayList2
        /// </summary>
        /// <param name="arrayList">ArrayList集合</param>
        /// <returns></returns>
        int read_arrayList2(ref ArrayList arrayList);

        /// <summary>
        /// 读取XML数据-ListT
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="listT">listT集合</param>
        /// <returns></returns>
        int read_listT2<T>(ref List<T> listT) where T : new();


        /// <summary>
        /// 读取XML数据-ListObj
        /// </summary>
        /// <param name="listObj">listObj集合</param>
        /// <returns></returns>
        int read_listObj2(ref List<object> listObj);

        /// <summary>
        /// 读取XML数据-ListObj2
        /// </summary>
        /// <param name="listObj">listObj集合</param>
        /// <param name="assembly">程序集</param>
        int read_listObj2(ref List<object> listObj, Assembly assembly);

    }
}
