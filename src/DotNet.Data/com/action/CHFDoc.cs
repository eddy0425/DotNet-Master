using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;

namespace DotNet.Data.action
{
    /// <summary>
    /// 文件读写类
    /// </summary>
    public class CHFDoc
    {
        static JsonAction json = new JsonAction();

        /// <summary>
        /// 保存参数
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="path">参数路径</param>
        /// <param name="model">类</param>
        /// <returns></returns>
        public static bool save<T>(string path, T model)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return false;
            int rtu = json.write_class(model, path);
            if (rtu == 0)
            {
                return true;
            }
            else
            {
                MessageBox.Show(model.GetType().Name + "保存异常,类为空异常！！！");
                return false;
            }
        }

        /// <summary>
        /// 保存参数
        /// </summary>
        /// <typeparam name="T">泛型</typeparam>
        /// <param name="path">参数路径</param>
        /// <param name="list">类</param>
        /// <returns></returns>
        public static bool save<T>(string path, List<T> list) where T : new()
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return false;
            int rtu = json.write_listT(list, path);
            if (rtu == 0)
            {
                return true;
            }
            else
            {
                T model = new T();
                MessageBox.Show(model.GetType().Name + "保存异常,类集合为空异常！！！");
                return false;
            }
        }

        /// <summary>
        /// 加载参数
        /// </summary>
        /// <param name="path">参数路径</param>
        /// <param name="model">类对象</param>
        /// <returns></returns>
        public static bool load<T>(string path, ref T model, bool showMessage = true) where T : new()
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return false;
            int rtu = json.read_class(ref model, path);
            if (model == null)
            {
                model = new T();
                if (showMessage) MessageBox.Show(model.GetType().Name + "加载异常,类为空异常！！！");
                return false;
            }
            return rtu == 0;
        }

        /// <summary>
        /// 加载参数
        /// </summary>
        /// <param name="path">参数路径</param>
        /// <param name="list">类对象</param>
        /// <returns></returns>
        public static bool load<T>(string path, ref List<T> list,bool showMessage = true) where T : new()
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return false;
            int rtu = json.read_listT(ref list, path);
            if (list == null)
            {
                T model = new T();
                if(showMessage) MessageBox.Show(model.GetType().Name + "加载异常,类集合为空异常！！！");
                list = new List<T>();
                return false;
            }
            if (list.Count == 0) return false;

            return rtu == 0;
        }

        /// <summary>
        /// 加载参数
        /// </summary>
        /// <param name="path">参数路径</param>
        /// <param name="list">类对象</param>
        /// <returns></returns>
        public static bool load2<T>(string path, ref List<T> list) where T : new()
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return false;
            int rtu = json.read_listT(ref list, path);
            if (list == null)
            {
                T model = new T();
                MessageBox.Show(model.GetType().Name + "加载异常,类集合为空异常！！！");
                list = new List<T>();
                return false;
            }

            return rtu == 0;
        }

        /// <summary>
        /// 文件的文件夹校验，没有就创建
        /// </summary>
        /// <param name="filePath">文件</param>
        public static void FileFolderExists(string filePath)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return;
            int index = filePath.LastIndexOf("\\");
            string FilePath = filePath.Substring(0, index);
            if (!Directory.Exists(FilePath))
            {
                Directory.CreateDirectory(FilePath);
            }
        }

        /// <summary>
        /// 文件夹校验，没有就创建
        /// </summary>
        /// <param name="folderPath">文件夹</param>
        /// <returns></returns>
        public static void FolderExists(string folderPath)
        {
            //if (!DotNet.Licensing.Client.LicensingLib.validation()) return;
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        }

    }
}
