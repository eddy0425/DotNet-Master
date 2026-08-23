using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using DotNet.Data.dao;

namespace DotNet.Data.daoImpl
{
    class BinaryDaoImpl: BinaryDao
    {
        string FilePath = null;  //文件路径
        private const string LogTag = "BinaryDaoImpl";

        /// <summary>
        /// 初始化二进制文件
        /// </summary>
        /// <param name="filePath">二进制文件路径</param>
        /// <returns></returns>
        public int init(string filePath)
        {
            try
            {
                this.FilePath = filePath;

                int index = filePath.LastIndexOf("\\");
                string FilePath = filePath.Substring(0, index);
                if (!Directory.Exists(FilePath))
                {
                    Directory.CreateDirectory(FilePath);
                }

                //不能创建2进制文件，否则会导致数据报错
                //if (!File.Exists(filePath))
                //{
                //    File.Create(filePath).Dispose();  //创建该文件
                //}

                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "初始化二进制文件出错！！！");
                return -1;
            }
        }

        /// <summary>
        /// Obj转换程字符串格式输出
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public string ObjectToString<T>(T t)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, t);
                string result = System.Text.Encoding.UTF8.GetString(stream.ToArray());
                return result;
            }
        }
        
        /// <summary>
        /// 字符串格式转换成类输出
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="s"></param>
        /// <returns></returns>
        public T StringToObject<T>(string s) where T : class
        {
            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(s);
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                T result = formatter.Deserialize(stream) as T;
                return result;
            }
        }

        /// <summary>
        /// 列表类数据读取
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public List<T> read_listObj<T>() where T : class
        {
            if (!File.Exists(FilePath)) { List<T> result = null; return result; }
            using (FileStream stream = new FileStream(FilePath, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                List<T> result = formatter.Deserialize(stream) as List<T>;
                return result;
            }
        }

        /// <summary>
        /// 类数据读取
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T read_Class<T>() where T : class
        {
            if (!File.Exists(FilePath)) { T result = null; return result; }
            using (FileStream stream = new FileStream(FilePath, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                T result = formatter.Deserialize(stream) as T;
                return result;
            }
        }

        /// <summary>
        /// 列表类数据写入
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        /// <returns></returns>
        public int write_listObj<T>(List<T> t)
        {
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                using (FileStream stream = new FileStream(FilePath, FileMode.OpenOrCreate))
                {
                    formatter.Serialize(stream, t);
                    stream.Flush();
                }
                return 0;
            }
            catch(Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将ArrayList集合写入到二进制文件出错！！！");
                return 1;
            }
        }

        /// <summary>
        /// 类数据写入
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="t"></param>
        public int write_Class<T>(T t)
        {
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                using (FileStream stream = new FileStream(FilePath, FileMode.OpenOrCreate))
                {
                    formatter.Serialize(stream, t);
                    stream.Flush();
                }
                return 0;
            }
            catch (Exception ex)
            {
                DataLog.Exception(LogTag, ex, "将Class类写入到二进制文件出错！！！");
                return 1;
            }
        }
    }
}
