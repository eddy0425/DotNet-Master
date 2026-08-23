using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace DotNet.Data.dao
{
    interface TxtDao : Data
    {
        /// <summary>
        /// 设置是否覆盖
        /// </summary>
        /// <param name="isCover">写入是否覆盖</param>
        void setCover(bool isCover);

        /// <summary>
        /// 初始化TXT文件
        /// </summary>
        /// <param name="filePath">TXT文件路径</param>
        /// <returns></returns>
        int init(string filePath);

        /// <summary>
        /// 初始化TXT文件
        /// </summary>
        /// <param name="filePath">TXT文件路径</param>
        /// <param name="isCover">写入是否覆盖</param>
        /// <returns></returns>
        int init(string filePath, bool isCover);

        /// <summary>
        /// 打开log文本
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        int open_log_text(string filePath);

        /// <summary>
        /// 关闭log文本
        /// </summary>
        /// <returns></returns>
        int close_log_text();

        /// <summary>
        /// 写入log
        /// </summary>
        /// <param name="value">值</param>
        /// <returns></returns>
        int write_txt_log(string value);

        /// <summary>
        /// 写入表头
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        int write_DataHeader<T>(T model);
    }
}
