using System;
using System.IO;
using System.Data;
using System.Collections;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace DotNet.Data
{
    /// <summary>
    /// JSON核心处理类
    /// </summary>
    public class JsonCore
    {
        private readonly JsonSerializer _serializer;

        private bool _format = true;
        public bool FormatOutput
        {
            get { return _format; }
            set
            {
                _format = value;
                _serializer.Formatting = _format ? Formatting.Indented : Formatting.None;
            }
        }

        /// <summary> 构造函数 </summary>
        public JsonCore()
        {
            _serializer = new JsonSerializer
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore // 忽略循环引用
            };
        }
       
        #region 辅助方法

        /// <summary>
        /// 确保文件路径有效
        /// </summary>
        /// <returns>成功返回true，失败返回false</returns>
        private bool EnsureValidFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                DataLog.Error("JsonCore", "JSON文件路径未设置");
                return false;
            }

            // 检查路径是否包含无效字符
            try
            {
                // 使用Path.GetFullPath检测无效路径字符
                Path.GetFullPath(filePath);
            }
            catch (Exception ex)
            {
                DataLog.Exception("JsonCore", ex, $"JSON文件路径无效: {filePath}");
                return false;
            }

            // 确保目录存在
            string directoryPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                try
                {
                    Directory.CreateDirectory(directoryPath);
                }
                catch (Exception ex)
                {
                    DataLog.Exception("JsonCore", ex, $"创建JSON文件目录失败: {directoryPath}");
                    return false;
                }
            }
            
            return true;
        }

        /// <summary>
        /// 处理写入操作异常
        /// </summary>
        /// <param name="operation">操作名称</param>
        /// <param name="ex">异常</param>
        /// <returns>失败返回false</returns>
        private bool HandleWriteException(string operation, Exception ex)
        {
            string message = $"将数据写入到JSON出错: {operation}";
            
            // 针对不同类型的异常提供更具体的信息
            if (ex is IOException)
                message += " - 文件IO错误";
            else if (ex is UnauthorizedAccessException)
                message += " - 文件访问权限不足";
            else if (ex is JsonSerializationException)
                message += " - JSON序列化错误，可能存在循环引用";
            else if (ex is JsonWriterException)
                message += " - JSON写入错误";

            DataLog.Exception("JsonCore", ex, message);
            return false;
        }

        /// <summary>
        /// 处理读取操作异常
        /// </summary>
        /// <param name="operation">操作名称</param>
        /// <param name="ex">异常</param>
        /// <returns>失败返回false</returns>
        private bool HandleReadException(string operation, Exception ex)
        {
            string message = $"从JSON读取数据出错: {operation}";
            
            // 针对不同类型的异常提供更具体的信息
            if (ex is IOException)
                message += " - 文件IO错误";
            else if (ex is UnauthorizedAccessException)
                message += " - 文件访问权限不足";
            else if (ex is JsonSerializationException)
                message += " - JSON反序列化错误，数据格式可能不匹配";
            else if (ex is JsonReaderException)
                message += " - JSON读取错误，文件可能不是有效的JSON格式";

            DataLog.Exception("JsonCore", ex, message);
            return false;
        }

        /// <summary>
        /// 验证JSON文件是否存在可读
        /// </summary>
        /// <returns>成功返回true，失败返回false</returns>
        private bool ValidateJsonFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                DataLog.Error("JsonCore", "JSON文件路径未设置");
                return false;
            }

            if (!File.Exists(filePath))
            {
                DataLog.Error("JsonCore", $"JSON文件不存在: {filePath}");
                return false;
            }

            // 检查文件是否可读
            try
            {
                using (File.OpenRead(filePath))
                {
                    // 只是测试打开文件，确保它可以被读取
                }
                return true;
            }
            catch (Exception ex)
            {
                DataLog.Exception("JsonCore", ex, $"无法访问JSON文件: {filePath}");
                return false;
            }
        }

        #endregion

        #region 通用序列化与反序列化方法

        /// <summary>
        /// 通用写入方法 - 将对象序列化为JSON并写入文件
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="data">要序列化的数据</param>
        /// <param name="operationName">操作名称（用于日志）</param>
        /// <returns>成功返回true，失败返回false</returns>
        private bool WriteJson<T>(T data, string filePath, string operationName)
        {
            try
            {
                if (!EnsureValidFilePath(filePath))
                {
                    return false;
                }

                using (StreamWriter writer = new StreamWriter(filePath))
                using (JsonTextWriter jsonWriter = new JsonTextWriter(writer))
                {
                    _serializer.Serialize(jsonWriter, data);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                return HandleWriteException(operationName, ex);
            }
        }

        /// <summary>
        /// 通用读取方法 - 从JSON文件反序列化对象
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="operationName">操作名称（用于日志）</param>
        /// <returns>反序列化的对象，失败返回默认值</returns>
        private T ReadJson<T>(string filePath,string operationName)
        {
            try
            {
                if (!ValidateJsonFile(filePath))
                {
                    return default;
                }

                using (StreamReader reader = File.OpenText(filePath))
                using (JsonTextReader jsonReader = new JsonTextReader(reader))
                {
                    return _serializer.Deserialize<T>(jsonReader);
                }
            }
            catch (Exception ex)
            {
                HandleReadException(operationName, ex);
                return default;
            }
        }

        #endregion

        #region 数据操作-写入

        /// <summary>
        /// 将数组写入到JSON
        /// </summary>
        /// <param name="array">数据</param>
        /// <returns>成功返回true，失败返回false</returns>
        public bool WriteArray(string[] array, string filePath)
        {
            if (array == null)
                return false;

            // 直接序列化数组，不进行转换
            return WriteJson(array, filePath, "数组");
        }

        /// <summary>
        /// 将ArrayList集合写入到JSON
        /// </summary>
        /// <param name="arrayList">数据</param>
        /// <returns>成功返回true，失败返回false</returns>
        public bool WriteArrayList(ArrayList arrayList, string filePath)
        {
            if (arrayList == null)
                return false;
                
            return WriteJson(arrayList, filePath, "ArrayList");
        }

        /// <summary>
        /// 将泛型对象写入到JSON
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="model">数据对象</param>
        /// <returns>成功返回true，失败返回false</returns>
        public bool WriteObject<T>(T model, string filePath)
        {
            if (model == null)
                return false;
                
            return WriteJson(model, filePath, $"类型 {typeof(T).Name}");
        }

        /// <summary>
        /// 将List集合写入到JSON
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="list">数据列表</param>
        /// <returns>成功返回true，失败返回false</returns>
        public bool WriteList<T>(List<T> list, string filePath)
        {
            if (list == null)
                return false;
                
            return WriteJson(list, filePath, $"List<{typeof(T).Name}>");
        }

        /// <summary>
        /// 将DataTable数据写入到JSON
        /// </summary>
        /// <param name="dataTable">DataTable数据</param>
        /// <returns>成功返回true，失败返回false</returns>
        public bool WriteDataTable(DataTable dataTable, string filePath)
        {
            if (dataTable == null)
                return false;
                
            return WriteJson(dataTable, filePath, "DataTable");
        }

        /// <summary>
        /// 将字符串写入到JSON
        /// </summary>
        /// <param name="value">字符串</param>
        /// <returns>成功返回true，失败返回false</returns>
        public bool WriteString(string value, string filePath)
        {
            if (value == null)
                return false;
                
            return WriteJson(value, filePath, "字符串");
        }

        /// <summary>
        /// 将字典写入到JSON
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <param name="data">字典数据</param>
        /// <returns>成功返回true，失败返回false</returns>
        public bool WriteDictionary<TKey, TValue>(Dictionary<TKey, TValue> data, string filePath)
        {
            if (data == null)
                return false;
                
            return WriteJson(data, filePath, "字典");
        }

        #endregion

        #region 数据操作-读取

        /// <summary>
        /// 读取JSON数据为数组
        /// </summary>
        /// <returns>字符串数组，失败返回null</returns>
        public string[] ReadArray(string filePath)
        {
            return ReadJson<string[]>(filePath, "数组");
        }

        /// <summary>
        /// 读取JSON数据为ArrayList
        /// </summary>
        /// <returns>ArrayList集合，失败返回null</returns>
        public ArrayList ReadArrayList(string filePath)
        {
            return ReadJson<ArrayList>(filePath, "ArrayList");
        }

        /// <summary>
        /// 读取JSON数据为对象
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <returns>对象实例，失败返回默认值</returns>
        public T ReadObject<T>(string filePath)
        {
            return ReadJson<T>(filePath, $"类型 {typeof(T).Name}");
        }

        /// <summary>
        /// 读取JSON数据为List集合
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <returns>List集合，失败返回null</returns>
        public List<T> ReadList<T>(string filePath)
        {
            return ReadJson<List<T>>(filePath, $"List<{typeof(T).Name}>");
        }

        /// <summary>
        /// 读取JSON数据为DataTable
        /// </summary>
        /// <returns>DataTable数据集，失败返回null</returns>
        public DataTable ReadDataTable(string filePath)
        {
            return ReadJson<DataTable>(filePath, "DataTable");
        }

        /// <summary>
        /// 读取JSON数据为字符串
        /// </summary>
        /// <returns>字符串，失败返回null</returns>
        public string ReadString(string filePath)
        {
            return ReadJson<string>(filePath, "字符串");
        }

        /// <summary>
        /// 读取JSON数据为字典
        /// </summary>
        /// <typeparam name="TKey">键类型</typeparam>
        /// <typeparam name="TValue">值类型</typeparam>
        /// <returns>字典，失败返回null</returns>
        public Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(string filePath)
        {
            return ReadJson<Dictionary<TKey, TValue>>(filePath, "字典");
        }

        #endregion

    }
}