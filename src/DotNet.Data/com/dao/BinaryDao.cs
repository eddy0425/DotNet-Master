using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNet.Data.dao
{
    /// <summary>
    /// 
    /// </summary>
    interface BinaryDao
    {
        /// <summary>
        /// 初始化二进制文件
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        int init(string filePath);

        /// <summary>
        /// Obj转换程字符串格式输出
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        string ObjectToString<T>(T t);

        /// <summary>
        /// 字符串格式转换成类输出
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s"></param>
        /// <returns></returns>
        T StringToObject<T>(string s) where T : class;

        /// <summary>
        /// 列表类数据读取
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        List<T> read_listObj<T>() where T : class;

        /// <summary>
        /// 类数据读取
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T read_Class<T>() where T : class;

        /// <summary>
        /// 列表类数据写入
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        int write_listObj<T>(List<T> t);

        /// <summary>
        /// 类数据写入
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        int write_Class<T>(T t);
    }
}
