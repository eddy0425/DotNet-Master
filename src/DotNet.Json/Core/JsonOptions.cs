using Newtonsoft.Json;

namespace DotNet.Json
{
    /// <summary>
    /// JSON 序列化配置选项
    /// </summary>
    public sealed class JsonOptions
    {
        /// <summary>
        /// 是否格式化输出（美化 JSON）
        /// </summary>
        public bool FormatOutput { get; set; } = true;

        /// <summary>
        /// 是否忽略空值
        /// </summary>
        public bool IgnoreNullValues { get; set; } = true;

        /// <summary>
        /// 是否忽略循环引用
        /// </summary>
        public bool IgnoreReferenceLoop { get; set; } = true;

        /// <summary>
        /// 默认配置
        /// </summary>
        public static JsonOptions Default => new JsonOptions();

        /// <summary>
        /// 紧凑输出配置（无格式化）
        /// </summary>
        public static JsonOptions Compact => new JsonOptions { FormatOutput = false };

        /// <summary>
        /// 转换为 Newtonsoft.Json 序列化器设置
        /// </summary>
        internal JsonSerializerSettings ToSerializerSettings()
        {
            return new JsonSerializerSettings
            {
                Formatting = FormatOutput ? Formatting.Indented : Formatting.None,
                NullValueHandling = IgnoreNullValues ? NullValueHandling.Ignore : NullValueHandling.Include,
                ReferenceLoopHandling = IgnoreReferenceLoop ? ReferenceLoopHandling.Ignore : ReferenceLoopHandling.Error
            };
        }

        /// <summary>
        /// 创建 JsonSerializer 实例
        /// </summary>
        internal JsonSerializer CreateSerializer()
        {
            return JsonSerializer.Create(ToSerializerSettings());
        }
    }
}

