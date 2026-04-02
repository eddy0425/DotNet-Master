using System;
using System.IO;
using System.Text;

namespace DotNet.HWindows
{
    /// <summary>
    /// 高性能序列化器
    /// </summary>

    public static partial class SerializeConvert
    {
        #region Json序列化和反序列化

        /// <summary>
        /// 首先使用NewtonsoftJson.默认True。
        /// <para>
        /// 当设置True时，json序列化会优先使用NewtonsoftJson。
        /// 当设置为FALSE，netstandard2.0和net45平台将继续使用。
        /// 其他平台将使用System.Text.Json。
        /// </para>
        /// </summary>
        public static bool NewtonsoftJsonFirst { get; set; } = true;

        /// <summary>
        /// 转换为Json
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static string ToJson(this object item)
        {
            if (NewtonsoftJsonFirst)
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(item);
            }

#if NETCOREAPP3_1_OR_GREATER
            return System.Text.Json.JsonSerializer.Serialize(item);
#else
            return Newtonsoft.Json.JsonConvert.SerializeObject(item);
#endif
        }

        /// <summary>
        /// 从字符串到json
        /// </summary>
        /// <param name="json"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object FromJson(this string json, Type type)
        {
            if (NewtonsoftJsonFirst)
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject(json, type);
            }

#if NETCOREAPP3_1_OR_GREATER
            return System.Text.Json.JsonSerializer.Deserialize(json, type);
#else
            return Newtonsoft.Json.JsonConvert.DeserializeObject(json, type);
#endif
        }

        /// <summary>
        /// 从字符串到json
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json"></param>
        /// <returns></returns>
        public static T FromJson<T>(this string json)
        {
            return (T)FromJson(json, typeof(T));
        }

        /// <summary>
        /// Json序列化数据对象
        /// </summary>
        /// <param name="obj">数据对象</param>
        /// <returns></returns>
        public static byte[] JsonSerializeToBytes(object obj)
        {
            return Encoding.UTF8.GetBytes(ToJson(obj));
        }

        /// <summary>
        /// Json序列化至文件
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="path"></param>
        public static void JsonSerializeToFile(object obj, string path)
        {
            using (var fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                var date = JsonSerializeToBytes(obj);
                fileStream.Write(date, 0, date.Length);
                fileStream.Close();
            }
        }

        /// <summary>
        /// Json反序列化
        /// </summary>
        /// <typeparam name="T">反序列化类型</typeparam>
        /// <param name="datas">数据</param>
        /// <returns></returns>
        public static T JsonDeserializeFromBytes<T>(byte[] datas)
        {
            return (T)JsonDeserializeFromBytes(datas, typeof(T));
        }

        /// <summary>
        /// Xml反序列化
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object JsonDeserializeFromBytes(byte[] datas, Type type)
        {
            return FromJson(Encoding.UTF8.GetString(datas), type);
        }

        /// <summary>
        /// Json反序列化
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="json">json字符串</param>
        /// <returns></returns>
        public static T JsonDeserializeFromString<T>(string json)
        {
            return FromJson<T>(json);
        }

        /// <summary>
        /// Json反序列化
        /// </summary>
        /// <typeparam name="T">反序列化类型</typeparam>
        /// <param name="path">文件路径</param>
        /// <returns></returns>
        public static T JsonDeserializeFromFile<T>(string path)
        {
            return JsonDeserializeFromString<T>(File.ReadAllText(path));
        }

        #endregion Json序列化和反序列化
    }
}