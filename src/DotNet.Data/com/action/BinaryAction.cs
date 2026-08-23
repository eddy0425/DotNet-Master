using DotNet.Data.dao;
using DotNet.Data.daoImpl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNet.Data.action
{
    /// <summary>
    /// 二进制文件操作
    /// </summary>
    public class BinaryAction: BinaryDao
    {
        BinaryDao binaryDao = new BinaryDaoImpl();

        /// <summary>
        /// 二进制接口初始化申请
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public BinaryAction(string filePath)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return;
            init(filePath);
        }
        
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public int init(string filePath)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return binaryDao.init(filePath);
        }

        /// <summary>
        /// 任意类型转字符串
        /// </summary>
        /// <typeparam name="T">类名</typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public string ObjectToString<T>(T t)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return null;
            return binaryDao.ObjectToString<T>(t);
        }

        /// <summary>
        /// 类数据读取
        /// </summary>
        /// <typeparam name="T">类名</typeparam>
        /// <returns></returns>
        public T read_Class<T>() where T : class
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return null;
            return binaryDao.read_Class<T>();
        }

        /// <summary>
        /// 列表类数据读取
        /// </summary>
        /// <typeparam name="T">类名</typeparam>
        /// <returns></returns>
        public List<T> read_listObj<T>() where T : class
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return null;
            return binaryDao.read_listObj<T>();
        }

        /// <summary>
        /// 字符串格式转换成类输出
        /// </summary>
        /// <typeparam name="T">类名</typeparam>
        /// <param name="s">字符串</param>
        /// <returns></returns>
        public T StringToObject<T>(string s) where T : class
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return null;
            return binaryDao.StringToObject<T>(s);
        }

        /// <summary>
        /// 类数据写入文件
        /// </summary>
        /// <typeparam name="T">类名</typeparam>
        /// <param name="t">类的变量名</param>
        /// <returns></returns>
        public int write_Class<T>(T t)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return binaryDao.write_Class<T>(t);
        }

        /// <summary>
        /// 列表类数据写入文件
        /// </summary>
        /// <typeparam name="T">类名</typeparam>
        /// <param name="t">类的变量名</param>
        /// <returns></returns>
        public int write_listObj<T>(List<T> t)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return -1;
            return binaryDao.write_listObj<T>(t);
        }
    }
}
