using System;
using System.Reflection;

namespace DotNet.Data
{
    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = false)]
    public class TextAttribute : Attribute
    {
        private string _name;
        /// <summary>
        /// 
        /// </summary>
        public string Name
        {
            set { _name = value; }
            get { return _name; }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        public TextAttribute(string name)
        {
            _name = name;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class GetAttribute
    {
        /// <summary>
        /// 获取某特定属性名称特性
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="proName"></param>
        /// <returns></returns>
        public static string Fun_getPropertyTextAttr<T>(T obj, string proName)
        {
            try
            {
                if (obj == null || string.IsNullOrEmpty(proName)) return "";
                PropertyInfo pInfo = obj.GetType().GetProperty(proName);
                if (pInfo == null) return "";
                var attsName = pInfo.GetCustomAttributes(typeof(TextAttribute), true);
                if (attsName.Length == 0) return "";
                return ((TextAttribute)attsName[0]).Name;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// 获取某特定类型特性
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string Fun_getClassTextAttr(Type type)
        {
            try
            {
                if (type == null) return "";
                var attsName = type.GetCustomAttributes(typeof(TextAttribute), true);
                if (attsName.Length == 0) return "";
                return ((TextAttribute)attsName[0]).Name;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// 获取某特定字段名称特性
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="proName"></param>
        /// <returns></returns>
        public static string Fun_getFieldTextAttr<T>(T obj, string proName)
        {
            try
            {
                if (obj == null || string.IsNullOrEmpty(proName)) return "";
                FieldInfo fInfo = obj.GetType().GetField(proName);
                if (fInfo == null) return "";
                var attsName = fInfo.GetCustomAttributes(typeof(TextAttribute), true);
                if (attsName.Length == 0) return "";
                return ((TextAttribute)attsName[0]).Name;
            }
            catch
            {
                return "";
            }
        }
    }
}
