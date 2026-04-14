using System;
using System.Windows.Forms;
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
                MessageBox.Show(ex.Message); 
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
                MessageBox.Show(ex.Message);
            }
        }
    }
}
