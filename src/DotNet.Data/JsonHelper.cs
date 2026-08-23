using System.Collections.Generic;


namespace DotNet.Data
{
    /// <summary>
    /// 文件读写工具类
    /// </summary>
    public class JsonHelper
    {
        private const string LogTag = "JsonHelper";
        private static readonly JsonCore _json = new JsonCore();

        /// <summary>
        /// 保存对象到JSON文件
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="path">文件路径</param>
        /// <param name="model">对象实例</param>
        /// <returns>成功返回true，失败返回false</returns>
        public static bool Save<T>(string path, T model)
        {
            if (model == null)
            {
                DataLog.Error(LogTag, typeof(T).Name + "保存异常,类为空异常！！！");
                return false;
            }

            bool result = _json.WriteObject(model, path);
            if (!result)
            {
                DataLog.Error(LogTag, model.GetType().Name + "保存失败,写入JSON文件出错！！！");
            }
            return result;
        }

        /// <summary>
        /// 保存列表到JSON文件
        /// </summary>
        /// <typeparam name="T">列表元素类型</typeparam>
        /// <param name="path">文件路径</param>
        /// <param name="list">列表实例</param>
        /// <returns>成功返回true，失败返回false</returns>
        public static bool Save<T>(string path, List<T> list) where T : new()
        {
            bool result = _json.WriteList(list, path);
            if (!result)
            {
                T model = new T();
                DataLog.Error(LogTag, model.GetType().Name + "保存异常,类集合为空异常！！！");
            }
            return result;
        }

        /// <summary>
        /// 从JSON文件加载对象
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="path">文件路径</param>
        /// <param name="model">输出的对象实例</param>
        /// <param name="showMessage">是否显示错误消息</param>
        /// <returns>成功返回true，失败返回false</returns>
        public static bool Load<T>(string path, out T model, bool showMessage = true) where T : new()
        {
            model = _json.ReadObject<T>(path);
            
            if (model == null)
            {
                model = new T();
                if (showMessage) DataLog.Error(LogTag, model.GetType().Name + "加载异常,类为空异常！！！");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 从JSON文件加载列表
        /// </summary>
        /// <typeparam name="T">列表元素类型</typeparam>
        /// <param name="path">文件路径</param>
        /// <param name="list">输出的列表实例</param>
        /// <param name="showMessage">是否显示错误消息</param>
        /// <returns>成功返回true，失败返回false</returns>
        public static bool Load<T>(string path, out List<T> list, bool showMessage = true) where T : new()
        {
            list = _json.ReadList<T>(path);
            
            if (list == null)
            {
                if (showMessage) DataLog.Error(LogTag, typeof(T).Name + "加载异常,类集合为空异常！！！");
                list = new List<T>();
                return false;
            }
            
            return list.Count > 0;
        }
    }
}