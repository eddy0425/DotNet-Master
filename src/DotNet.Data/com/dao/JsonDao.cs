using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNet.Data.dao
{
    interface JsonDao : Data
    {

        /// <summary>
        /// 设置Json格式
        /// </summary>
        /// <param name="format">是否格式化json字符串</param>
        /// <returns></returns>
        void setFormatting(bool format);

        /// <summary>
        /// 初始化JSON文件
        /// </summary>
        /// <param name="filePath">JSON文件路径</param>
        /// <returns></returns>
        int init(string filePath);

        /// <summary>
        /// 初始化JSON文件
        /// </summary>
        /// <param name="filePath">JSON文件路径</param>
        /// <param name="format">是否格式化json字符串</param>
        /// <returns></returns>
        int init(string filePath, bool format);


        int write_list_string(List<string> value);


        int read_list_string(ref List<string> value);

        int write_class<T>(T model);

        int read_class<T>(ref T model);
        

    }
}
