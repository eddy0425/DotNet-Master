using System;
using System.IO;

namespace DotNet.Library.Extension
{
    /// <summary>
    /// 文件夹复制和删除属于 I/O 操作
    /// </summary>
    public static class DirectoryExtension
    {
        /// <summary>
        /// 复制文件夹及其内容到目标路径
        /// </summary>
        /// <param name="sourceFolder">原始文件夹路径</param>
        /// <param name="destFolder">目标文件夹路径</param>
        /// <returns>成功返回 true，失败返回 false</returns>
        public static bool CopyFolder(string sourceFolder, string destFolder)
        {
            try
            {
                // 参数验证
                if (string.IsNullOrWhiteSpace(sourceFolder) || string.IsNullOrWhiteSpace(destFolder))
                    throw new ArgumentException("源文件夹或目标文件夹路径不能为空。");

                // 如果原始文件夹不存在，直接返回 false
                if (!Directory.Exists(sourceFolder))
                    throw new DirectoryNotFoundException($"源文件夹不存在：{sourceFolder}");

                // 如果目标文件夹不存在，则创建它
                if (!Directory.Exists(destFolder))
                    Directory.CreateDirectory(destFolder);

                // 复制文件
                string[] files = Directory.GetFiles(sourceFolder);
                foreach (string file in files)
                {
                    string name = Path.GetFileName(file);
                    string dest = Path.Combine(destFolder, name);

                    File.Copy(file, dest, true); // 支持覆盖
                }

                // 递归复制子文件夹
                string[] folders = Directory.GetDirectories(sourceFolder);
                foreach (string folder in folders)
                {
                    string name = Path.GetFileName(folder);
                    string dest = Path.Combine(destFolder, name);

                    CopyFolder(folder, dest);
                }

                return true;
            }
            catch (Exception ex)
            {
                // 可以记录日志或抛出异常
                Console.WriteLine($"复制文件夹失败：{ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 删除文件夹及其内容
        /// </summary>
        /// <param name="dir">文件夹路径</param>
        /// <returns>成功返回 true，失败返回 false</returns>
        public static bool DeleteFolder(string dir)
        {
            try
            {
                // 参数验证
                if (string.IsNullOrWhiteSpace(dir))
                    throw new ArgumentException("文件夹路径不能为空。");

                // 如果文件夹不存在，直接返回 true
                if (!Directory.Exists(dir))
                    return true;

                // 删除文件和子文件夹
                foreach (string f in Directory.GetFileSystemEntries(dir))
                {
                    // 删除文件
                    if (File.Exists(f))
                    {
                        FileInfo fi = new FileInfo(f);

                        // 如果文件是只读状态，先修改其属性
                        if ((fi.Attributes & FileAttributes.ReadOnly) != 0)
                        {
                            fi.Attributes = FileAttributes.Normal;
                        }

                        File.Delete(f);
                    }
                    else if (Directory.Exists(f))
                    {
                        // 删除子文件夹
                        DeleteFolder(f);
                    }
                }

                // 删除空文件夹
                Directory.Delete(dir);

                return true;
            }
            catch (Exception ex)
            {
                // 可以记录日志或抛出异常
                Console.WriteLine($"删除文件夹失败：{ex.Message}");
                return false;
            }
        }
    }
}


