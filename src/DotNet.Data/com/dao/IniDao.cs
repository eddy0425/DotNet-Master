using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNet.Data.dao
{
    interface IniDao: Data
    {
        /// <summary>
        /// 设置项目名称(如 [section])
        /// </summary>
        /// <param name="section">项目名称(如 [section])</param>
        /// <returns></returns>
        void setSection(string section);

        /// <summary>
        /// 初始化INI文件
        /// </summary>
        /// <param name="filePath">INI文件路径</param>
        /// <returns></returns>
        int init(string filePath);

        /// <summary>
        /// 初始化INI文件
        /// </summary>
        /// <param name="filePath">INI文件路径</param>
        /// <param name="section">项目名称(如 [section])</param>
        /// <returns></returns>
        int init(string filePath, string section);

        /// <summary>
        /// 判断INI节点是否存在
        /// </summary>
        /// <param name="section">项目名称(如 [section])</param>
        /// <param name="key">键</param>
        /// <returns>
        /// i>0 == true
        /// </returns> 
        int KeyExists(string section, string key);

        /// <summary>
        /// 将字符串写入到INI
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        int write_string(string key, string value);

        /// <summary>
        /// 读取INI的值-string
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns></returns>
        int read_string(string key, ref string value);

    }
}
