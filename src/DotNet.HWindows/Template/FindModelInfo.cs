using System;
using System.Reflection;
using System.Windows.Forms;
//using DotNet.Data.action;

namespace DotNet.HWindows
{
    public class FindModelInfo : FindModel
    {
        string _name;
        string _configPath;
        string _modelPath;
        public string Name { set { _name = value; } get { return _name; } }
        public string ConfigPath { set { _configPath = value; } get { return _configPath; } }
        public string ModelPath { set { _modelPath = value; } get { return _modelPath; } }

        private void SetInfo<T>(T info)
        {
            foreach (PropertyInfo pi in this.GetType().GetProperties())
            {
                object value = info.GetType().GetProperty(pi.Name).GetValue(info);
                object v = Convert.ChangeType(value, pi.PropertyType);
                pi.SetValue(this, v, null);
            }
        }

        public void SetPath(string _systemFolder)
        {
            ConfigPath = _systemFolder + "\\ModelInfo.json";
            ModelPath = _systemFolder + "\\ModelInfo.bmp";
        }

        public void LoadParameter(string _systemFolder, string _name)
        {
            SetPath(_systemFolder + "\\" + _name);

            //FindModelInfo info = this;
            //if (!CHFDoc.load(ConfigPath, ref info))
            //{
            //    SetPath(_systemFolder + "\\" + _name);
            //    CHFDoc.save(ConfigPath, info);
            //    MessageBox.Show($"加载{_name}参数失败！！！");
            //}

            //info.Name = _name;
            //info.FindModelExists();
            //if (info.ConfigPath == null || info.ModelPath == null) info.SetPath(_systemFolder + "\\" + _name);

            //FindROI.Color = HColor.Blue;
            //SetROI.Color = HColor.Red;

            //SetInfo(info);
        }

        public bool SaveParameter()
        {
            //if (!CHFDoc.save(ConfigPath, this))
            //{
            //    MessageBox.Show($"保存{Name}参数失败！！！");
            //    return false;
            //}
            return true;
        }
    }
}
