using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace DotNet.Json
{
    /// <summary>
    /// JSON 核心处理类 - 提供文件级别的 JSON 序列化/反序列化操作
    /// </summary>
    public sealed class JsonCore
    {
        private const string LogTag = "JsonCore";
        private readonly JsonSerializer _serializer;
        private readonly JsonOptions _options;

        /// <summary>
        /// 使用默认配置创建实例
        /// </summary>
        public JsonCore() : this(JsonOptions.Default) { }

        /// <summary>
        /// 使用指定配置创建实例
        /// </summary>
        /// <param name="options">配置选项</param>
        public JsonCore(JsonOptions options)
        {
            _options = options ?? JsonOptions.Default;
            _serializer = _options.CreateSerializer();
        }

        #region 同步操作

        /// <summary>
        /// 将对象序列化并写入 JSON 文件
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="data">要序列化的数据</param>
        /// <param name="filePath">文件路径</param>
        /// <returns>成功返回 true，失败返回 false</returns>
        public bool Write<T>(T data, string filePath)
        {
            if (data == null)
            {
                JsonLog.Warning(LogTag, $"写入失败: 数据为 null, 路径: {filePath}");
                return false;
            }

            try
            {
                if (!EnsureDirectoryExists(filePath))
                    return false;

                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                using (var jsonWriter = new JsonTextWriter(writer))
                {
                    _serializer.Serialize(jsonWriter, data);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogWriteException(ex, filePath, typeof(T).Name);
                return false;
            }
        }

        /// <summary>
        /// 从 JSON 文件反序列化为对象
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="filePath">文件路径</param>
        /// <returns>反序列化的对象，失败返回 default(T)</returns>
        public T Read<T>(string filePath)
        {
            try
            {
                if (!ValidateFileExists(filePath))
                    return default;

                using (var reader = new StreamReader(filePath, Encoding.UTF8))
                using (var jsonReader = new JsonTextReader(reader))
                {
                    return _serializer.Deserialize<T>(jsonReader);
                }
            }
            catch (Exception ex)
            {
                LogReadException(ex, filePath, typeof(T).Name);
                return default;
            }
        }

        /// <summary>
        /// 尝试从 JSON 文件反序列化为对象
        /// </summary>
        /// <typeparam name="T">目标类型（引用类型）</typeparam>
        /// <param name="filePath">文件路径</param>
        /// <param name="result">反序列化的结果</param>
        /// <returns>成功返回 true，失败返回 false</returns>
        public bool TryRead<T>(string filePath, out T result) where T : class
        {
            result = Read<T>(filePath);
            return result != null;
        }

        #endregion

        #region 异步操作

        /// <summary>
        /// 异步将对象序列化并写入 JSON 文件
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="data">要序列化的数据</param>
        /// <param name="filePath">文件路径</param>
        /// <returns>成功返回 true，失败返回 false</returns>
        public async Task<bool> WriteAsync<T>(T data, string filePath)
        {
            if (data == null)
            {
                JsonLog.Warning(LogTag, $"写入失败: 数据为 null, 路径: {filePath}");
                return false;
            }

            try
            {
                if (!EnsureDirectoryExists(filePath))
                    return false;

                // 先序列化到字符串，再异步写入文件
                var json = JsonConvert.SerializeObject(data, _options.ToSerializerSettings());
                using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
                {
                    await writer.WriteAsync(json).ConfigureAwait(false);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogWriteException(ex, filePath, typeof(T).Name);
                return false;
            }
        }

        /// <summary>
        /// 异步从 JSON 文件反序列化为对象
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="filePath">文件路径</param>
        /// <returns>反序列化的对象，失败返回 default(T)</returns>
        public async Task<T> ReadAsync<T>(string filePath)
        {
            try
            {
                if (!ValidateFileExists(filePath))
                    return default;

                string json;
                using (var reader = new StreamReader(filePath, Encoding.UTF8))
                {
                    json = await reader.ReadToEndAsync().ConfigureAwait(false);
                }
                return JsonConvert.DeserializeObject<T>(json, _options.ToSerializerSettings());
            }
            catch (Exception ex)
            {
                LogReadException(ex, filePath, typeof(T).Name);
                return default;
            }
        }

        #endregion

        #region 字符串序列化

        /// <summary>
        /// 将对象序列化为 JSON 字符串
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="data">要序列化的数据</param>
        /// <returns>JSON 字符串，失败返回 null</returns>
        public string Serialize<T>(T data)
        {
            if (data == null) return null;

            try
            {
                return JsonConvert.SerializeObject(data, _options.ToSerializerSettings());
            }
            catch (Exception ex)
            {
                JsonLog.Exception(LogTag, ex, $"序列化 {typeof(T).Name} 失败");
                return null;
            }
        }

        /// <summary>
        /// 将 JSON 字符串反序列化为对象
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="json">JSON 字符串</param>
        /// <returns>反序列化的对象，失败返回 default(T)</returns>
        public T Deserialize<T>(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return default;

            try
            {
                return JsonConvert.DeserializeObject<T>(json, _options.ToSerializerSettings());
            }
            catch (Exception ex)
            {
                JsonLog.Exception(LogTag, ex, $"反序列化 {typeof(T).Name} 失败");
                return default;
            }
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 确保目录存在
        /// </summary>
        private bool EnsureDirectoryExists(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                JsonLog.Error(LogTag, "文件路径为空");
                return false;
            }

            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                return true;
            }
            catch (Exception ex)
            {
                JsonLog.Exception(LogTag, ex, $"创建目录失败: {filePath}");
                return false;
            }
        }

        /// <summary>
        /// 验证文件是否存在
        /// </summary>
        private bool ValidateFileExists(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                JsonLog.Error(LogTag, "文件路径为空");
                return false;
            }

            if (!File.Exists(filePath))
            {
                JsonLog.Warning(LogTag, $"文件不存在: {filePath}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 记录写入异常
        /// </summary>
        private void LogWriteException(Exception ex, string filePath, string typeName)
        {
            var context = GetExceptionContext(ex);
            JsonLog.Exception(LogTag, ex, $"写入 JSON 失败 [{typeName}] -> {filePath}{context}");
        }

        /// <summary>
        /// 记录读取异常
        /// </summary>
        private void LogReadException(Exception ex, string filePath, string typeName)
        {
            var context = GetExceptionContext(ex);
            JsonLog.Exception(LogTag, ex, $"读取 JSON 失败 [{typeName}] <- {filePath}{context}");
        }

        /// <summary>
        /// 获取异常上下文信息
        /// </summary>
        private static string GetExceptionContext(Exception ex)
        {
            if (ex is IOException) return " [IO错误]";
            if (ex is UnauthorizedAccessException) return " [权限不足]";
            if (ex is JsonSerializationException) return " [序列化错误]";
            if (ex is JsonReaderException) return " [JSON格式错误]";
            return string.Empty;
        }

        #endregion
    }
}
