using System;
using System.Threading.Tasks;

namespace DotNet.Json
{
    /// <summary>
    /// JSON 工具类 - 提供便捷的静态方法进行 JSON 文件读写操作
    /// </summary>
    /// <remarks>
    /// 这是一个静态门面类，内部使用 <see cref="JsonCore"/> 实现功能。
    /// 如需更多配置选项，请直接使用 <see cref="JsonCore"/> 类。
    /// </remarks>
    public static class JsonHelper
    {
        private static readonly Lazy<JsonCore> _default = new Lazy<JsonCore>(() => new JsonCore());
        private static readonly Lazy<JsonCore> _compact = new Lazy<JsonCore>(() => new JsonCore(JsonOptions.Compact));

        /// <summary>
        /// 默认 JSON 核心实例（格式化输出）
        /// </summary>
        public static JsonCore Default => _default.Value;

        /// <summary>
        /// 紧凑输出的 JSON 核心实例
        /// </summary>
        public static JsonCore Compact => _compact.Value;

        #region 保存操作

        /// <summary>
        /// 保存对象到 JSON 文件
        /// </summary>
        public static bool Save<T>(string filePath, T data) => Default.Write(data, filePath);

        /// <summary>
        /// 异步保存对象到 JSON 文件
        /// </summary>
        public static Task<bool> SaveAsync<T>(string filePath, T data) => Default.WriteAsync(data, filePath);

        #endregion

        #region 加载操作

        /// <summary>
        /// 从 JSON 文件加载对象
        /// </summary>
        public static T Load<T>(string filePath) => Default.Read<T>(filePath);

        /// <summary>
        /// 从 JSON 文件加载对象，失败时返回默认实例
        /// </summary>
        public static T LoadOrDefault<T>(string filePath) where T : class, new()
        {
            var result = Default.Read<T>(filePath);
            return result ?? new T();
        }

        /// <summary>
        /// 从 JSON 文件加载对象，失败时使用工厂方法创建默认值
        /// </summary>
        public static T LoadOrDefault<T>(string filePath, Func<T> defaultFactory) where T : class
        {
            var result = Default.Read<T>(filePath);
            if (result != null) return result;
            return defaultFactory != null ? defaultFactory() : null;
        }

        /// <summary>
        /// 异步从 JSON 文件加载对象
        /// </summary>
        public static Task<T> LoadAsync<T>(string filePath) => Default.ReadAsync<T>(filePath);

        /// <summary>
        /// 异步从 JSON 文件加载对象，失败时返回默认实例
        /// </summary>
        public static async Task<T> LoadOrDefaultAsync<T>(string filePath) where T : class, new()
        {
            var result = await Default.ReadAsync<T>(filePath).ConfigureAwait(false);
            return result ?? new T();
        }

        #endregion

        #region 尝试加载操作

        /// <summary>
        /// 尝试从 JSON 文件加载对象
        /// </summary>
        public static bool TryLoad<T>(string filePath, out T result) where T : class
            => Default.TryRead(filePath, out result);

        #endregion

        #region 序列化/反序列化字符串

        /// <summary>
        /// 将对象序列化为 JSON 字符串
        /// </summary>
        public static string ToJson<T>(T data, bool formatted = true)
            => (formatted ? Default : Compact).Serialize(data);

        /// <summary>
        /// 将 JSON 字符串反序列化为对象
        /// </summary>
        public static T FromJson<T>(string json) => Default.Deserialize<T>(json);

        #endregion
    }
}
