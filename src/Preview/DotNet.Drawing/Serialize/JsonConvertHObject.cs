using System;
using HalconDotNet;
using Newtonsoft.Json;

namespace DotNet.Drawing
{
    /// <summary>
    /// 自定义序列化转换器
    /// </summary>
    public class JsonConvertHObject : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            HObject hObject = new HObject();
            try
            {
                if ((string)reader.Value != "Destroyed")
                {
                    hObject = (HObject)serializer.Deserialize(reader, objectType);
                }
                return hObject;
            }
            catch (Exception ex)
            {
                // 反序列化失败退化为空 HObject: 这是库层代码, 不能弹窗阻塞调用线程.
                Log.Warn(nameof(JsonConvertHObject), "反序列化 HObject 失败, 返回空对象.", ex);
                return hObject;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            try
            {
                HObject hObject = (HObject)value;
                if (hObject.NotNull())
                {
                    serializer.Serialize(writer, value);
                }
                else
                {
                    serializer.Serialize(writer, "Destroyed");
                }
            }
            catch (Exception ex)
            {
                // 序列化失败不能静默丢数据: 记录后抛出, 由上层决定提示还是重试.
                Log.Error(nameof(JsonConvertHObject), "序列化 HObject 失败.", ex);
                throw;
            }
        }
    }
}
