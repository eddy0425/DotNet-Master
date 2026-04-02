using System;
using System.IO;
using OpenCvSharp;
using HalconDotNet;
using DotNet.Library.Extension;


namespace DotNet.HWindows
{
    public static class CvOperatorSet
    {
        /// <summary>
        /// 排序方法
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static string[] FileSorts(string[] array)
        {
            int nuble = 0;
            string[] array1 = new string[0];
            string[] array2 = new string[0];
            string thisImagePath;  //当前图片路径
            string imageName;      //图片名称
            int i1 = 0;            //选择排序变量
            int i2 = 0;            //选择排序变量

            for (int i = 0; i < array.Length; i++)
            {
                thisImagePath = array[i];
                imageName = Path.GetFileNameWithoutExtension(thisImagePath);
                if (int.TryParse(imageName, out nuble))
                {
                    Array.Resize(ref array1, array1.Length + 1);
                    array1[array1.Length - 1] = thisImagePath;
                }
                else
                {
                    Array.Resize(ref array2, array2.Length + 1);
                    array2[array2.Length - 1] = thisImagePath;
                }
            }

            //数组1序号重排
            for (int i = 0; i < array1.Length - 1; i++)
            {
                for (int j = i + 1; j < array1.Length; j++)
                {
                    i1 = int.Parse(Path.GetFileNameWithoutExtension(array1[i]));
                    i2 = int.Parse(Path.GetFileNameWithoutExtension(array1[j]));
                    if (i2 < i1)
                    {
                        string temp = array1[i];
                        array1[i] = array1[j];
                        array1[j] = temp;
                    }
                }
            }

            for (int i = 0; i < array2.Length; i++)
            {
                Array.Resize(ref array1, array1.Length + 1);
                array1[array1.Length - 1] = array2[i];
            }
            return array1;
        }

        /// <summary>
        /// 计算经过刚体变换后的坐标
        /// </summary>
        /// <param name="Follow">初始参考点</param>
        /// <param name="matching">目标参考点</param>
        /// <param name="target">需要变换的目标点</param>
        /// <param name="result">输出变换后的点</param>
        public static void GetTransformedCoord(CvCoord Follow, CvCoord matching, CvCoord target, out CvCoord result)
        {
            // 初始化输出结果
            result = new CvCoord();

            // 计算角度差（弧度）
            double angleDiff = (matching.angle - Follow.angle).ToRadians();

            // 计算平移差
            double deltaX = matching.X - Follow.X;
            double deltaY = matching.Y - Follow.Y;

            // 旋转矩阵
            //double cosTheta = Math.Cos(angleDiff);
            //double sinTheta = Math.Sin(angleDiff);
            double cosTheta = Math.Cos(0);
            double sinTheta = Math.Sin(0);

            // 计算新坐标
            double transformedX = cosTheta * (target.X - Follow.X) - sinTheta * (target.Y - Follow.Y) + matching.X;
            double transformedY = sinTheta * (target.X - Follow.X) + cosTheta * (target.Y - Follow.Y) + matching.Y;

            // 更新结果
            result.X = transformedX;
            result.Y = transformedY;
            result.angle = target.angle + (matching.angle - Follow.angle); // 更新角度
        }


        #region NotNull

        /// <summary>
        /// 判断CvCoord是否为空，
        /// </summary>
        /// <returns> 空：false  不为空：true </returns>
        public static bool NotNull(this CvCoord coord)
        {
            return (coord == null);
        }

        /// <summary>
        /// 判断HObject是否为空，
        /// </summary>
        /// <returns> 空：false  不为空：true </returns>
        public static bool NotNull(this HObject image)
        {
            if ((object)image != null)
            {
                return image.IsInitialized();
            }
            return false;
        }

        /// <summary>
        /// 判断HTuple是否为空，
        /// </summary>
        /// <returns> 空：false  不为空：true </returns>
        public static bool NotNull(this HTuple hTuple)
        {
            if (hTuple.Type == HTupleType.EMPTY)
            {
                return hTuple.Length > 0;
            }
            return true;
        }

        #endregion

        #region AffineTrans

        /// <summary> 获取反射变换矩阵 </summary>
        public static void VectorAngleToRigid(CvCoord Follow, CvCoord matching, out HTuple hv_HomMat2D)
        {
            HOperatorSet.VectorAngleToRigid(Follow.Y, Follow.X, Follow.angle.ToRadians(),
                                            matching.Y, matching.X, matching.angle.ToRadians(),
                                            out hv_HomMat2D);
        }

        /// <summary> 获取反射变换区域 </summary>
        public static void TransRegion(CvCoord Follow, CvCoord matching, HObject inRegion, out HObject outRegion)
        {
            VectorAngleToRigid(Follow, matching, out HTuple hv_HomMat2D);
            HOperatorSet.AffineTransRegion(inRegion, out outRegion, hv_HomMat2D, "nearest_neighbor");
        }

        /// <summary> 获取反射变换轮廓 </summary>
        public static void TransContourXld(CvCoord Follow, CvCoord matching, HObject contours, out HObject contoursAffineTrans)
        {
            VectorAngleToRigid(Follow, matching, out HTuple hv_HomMat2D);
            HOperatorSet.AffineTransContourXld(contours, out contoursAffineTrans, hv_HomMat2D);
        }

        /// <summary> 获取反射变换点 </summary>
        public static void TransPixel(CvCoord Follow, CvCoord matching, HTuple row, HTuple col, out HTuple rowTrans, out HTuple colTrans)
        {
            VectorAngleToRigid(Follow, matching, out HTuple hv_HomMat2D);
            HOperatorSet.AffineTransPixel(hv_HomMat2D, row, col, out rowTrans, out colTrans);
        }

        /// <summary> 获取反射变换区域和点 </summary>
        public static void TransRegionAndPixel(CvCoord Follow, CvCoord matching, HObject inRegion, out HObject outRegion, HTuple row, HTuple col, out HTuple rowTrans, out HTuple colTrans)
        {
            VectorAngleToRigid(Follow, matching, out HTuple hv_HomMat2D);
            HOperatorSet.AffineTransRegion(inRegion, out outRegion, hv_HomMat2D, "nearest_neighbor");
            HOperatorSet.AffineTransPixel(hv_HomMat2D, row, col, out rowTrans, out colTrans);
        }


        //====================================

        /// <summary> 获取反射变换矩阵 </summary>
        public static void VectorAngleToRigid(Point2d Follow, Point2d matching, out HTuple hv_HomMat2D)
        {
            HOperatorSet.VectorAngleToRigid(Follow.Y, Follow.X, 0, matching.Y, matching.X, 0, out hv_HomMat2D);
        }

        /// <summary> 获取反射变换区域 </summary>
        public static void TransRegion(Point2d Follow, Point2d matching, HObject inRegion, out HObject outRegion)
        {
            VectorAngleToRigid(Follow, matching, out HTuple hv_HomMat2D);
            HOperatorSet.AffineTransRegion(inRegion, out outRegion, hv_HomMat2D, "nearest_neighbor");
        }

        /// <summary> 获取反射变换轮廓 </summary>
        public static void TransContourXld(Point2d Follow, Point2d matching, HObject contours, out HObject contoursAffineTrans)
        {
            VectorAngleToRigid(Follow, matching, out HTuple hv_HomMat2D);
            HOperatorSet.AffineTransContourXld(contours, out contoursAffineTrans, hv_HomMat2D);
        }

        /// <summary> 获取反射变换点 </summary>
        public static void TransPixel(Point2d Follow, Point2d matching, HTuple row, HTuple col, out HTuple rowTrans, out HTuple colTrans)
        {
            VectorAngleToRigid(Follow, matching, out HTuple hv_HomMat2D);
            HOperatorSet.AffineTransPixel(hv_HomMat2D, row, col, out rowTrans, out colTrans);
        }

        /// <summary> 获取反射变换区域和点 </summary>
        public static void TransRegionAndPixel(Point2d Follow, Point2d matching, HObject inRegion, out HObject outRegion, HTuple row, HTuple col, out HTuple rowTrans, out HTuple colTrans)
        {
            VectorAngleToRigid(Follow, matching, out HTuple hv_HomMat2D);
            HOperatorSet.AffineTransRegion(inRegion, out outRegion, hv_HomMat2D, "nearest_neighbor");
            HOperatorSet.AffineTransPixel(hv_HomMat2D, row, col, out rowTrans, out colTrans);
        }

        #endregion

        #region SaveImage

        /// <summary> 是否启用保存图片功能 </summary>
        public static bool SaveImageEnabled { get; set; } = false;

        /// <summary> 获取窗体图像 </summary>
        public static void GetCropWindow(HTuple hWindowHandle, out HObject image, string imageType = "tiff")
        {
            // 初始化输出图像
            image = new HObject(); HOperatorSet.GenEmptyObj(out image);

            try
            {
                // 验证参数：窗口句柄
                if (hWindowHandle == null || hWindowHandle.Length == 0)
                {
                    throw new ArgumentException("无效的窗口句柄！", nameof(hWindowHandle));
                }

                // 验证图片格式
                string[] validImageTypes = { "bmp", "tiff", "png", "jpg", "jpeg" };
                if (!Array.Exists(validImageTypes, format => format.Equals(imageType, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new ArgumentException($"不支持的图片格式: {imageType}", nameof(imageType));
                }

                // 从窗口裁剪图像
                HOperatorSet.DumpWindowImage(out image, hWindowHandle);

                // 验证裁剪结果
                if (image == null || !image.IsInitialized())
                {
                    throw new InvalidOperationException("无法从指定的窗口句柄裁剪图像！");
                }

                // 如果有特殊需求处理其他逻辑，可以在这添加额外的代码
            }
            catch (Exception ex)
            {
                if (image.NotNull()) image.Dispose();
                throw new Exception($"获取裁剪图像: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 保存图像
        /// </summary>
        /// <param name="imageType">图片格式："bmp", "tiff", "png", etc.</param>
        public static void SaveImage(HObject imgTemp, string folderPath, string imageType = "tiff")
        {
            try
            {
                // 如果保存图片功能未启用，直接返回
                if (!SaveImageEnabled) return;

                // 验证图像参数
                if (imgTemp == null || !imgTemp.IsInitialized())
                {
                    throw new ArgumentNullException(nameof(imgTemp), "传入的图像为空或未初始化！");
                }

                // 验证图片格式
                string[] validImageTypes = { "bmp", "tiff", "png", "jpg", "jpeg" };
                if (!Array.Exists(validImageTypes, format => format.Equals(imageType, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new ArgumentException($"不支持的图片格式: {imageType}", nameof(imageType));
                }

                // 生成文件名 时间戳
                string fileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_fffff"); // 更直观的日期时间格式

                // 拼接完整保存路径
                string saveFilePath = Path.Combine(folderPath, $"{fileName}.{imageType}");

                // 确保文件夹存在
                Directory.CreateDirectory(Path.GetDirectoryName(saveFilePath));

                // 保存图像
                HOperatorSet.WriteImage(imgTemp, imageType, 0, saveFilePath);
            }
            catch (Exception ex)
            {
                throw new Exception($"保存图像: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 从 Halcon 窗口中裁剪图像并保存到路径
        /// </summary>
        /// <param name="hWindowHandle">Halcon 窗口句柄</param>
        /// <param name="imageType">图片格式："bmp", "tiff", "png", etc.</param>
        public static void SaveCropWindow(HTuple hWindowHandle, string folderPath, string imageType = "tiff")
        {
            HObject croppedImage = new HObject(); HOperatorSet.GenEmptyObj(out croppedImage);

            try
            {
                // 如果保存图片功能未启用，直接返回
                if (!SaveImageEnabled) return;

                // 验证参数：窗口句柄
                if (hWindowHandle == null || hWindowHandle.Length == 0)
                {
                    throw new ArgumentException("无效的窗口句柄！", nameof(hWindowHandle));
                }

                // 验证图片格式
                string[] validImageTypes = { "bmp", "tiff", "png", "jpg", "jpeg" };
                if (!Array.Exists(validImageTypes, format => format.Equals(imageType, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new ArgumentException($"不支持的图片格式: {imageType}", nameof(imageType));
                }

                // 从窗口裁剪图像
                HOperatorSet.DumpWindowImage(out croppedImage, hWindowHandle);

                // 验证裁剪结果
                if (croppedImage == null || !croppedImage.IsInitialized())
                {
                    throw new InvalidOperationException("无法从指定的窗口句柄裁剪图像！");
                }

                // 生成文件名 时间戳
                string fileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_fffff"); // 更直观的日期时间格式

                // 拼接完整保存路径
                string saveFilePath = Path.Combine(folderPath, $"{fileName}.{imageType}");

                // 确保文件夹存在
                Directory.CreateDirectory(Path.GetDirectoryName(saveFilePath));

                // 保存图像
                HOperatorSet.WriteImage(croppedImage, imageType, 0, saveFilePath);

            }
            catch (Exception ex)
            {
                throw new Exception($"裁剪窗体图像: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                croppedImage.Dispose();
            }
        }

        /// <summary>
        /// 保存图像
        /// </summary>
        /// <param name="imgTemp">图像</param>
        /// <param name="imageType">图片格式："bmp", "tiff", "png", etc.</param>
        public static void SaveImage(HObject imgTemp, string cameraName, string folderPath = @"D:\Picture\SaveOriginalImages", string imageType = "tiff")
        {
            try
            {
                // 如果保存图片功能未启用，直接返回
                if (!SaveImageEnabled) return;

                // 验证图像参数
                if (imgTemp == null || !imgTemp.IsInitialized())
                {
                    throw new ArgumentNullException(nameof(imgTemp), "传入的图像为空或未初始化！");
                }

                // 验证图片格式
                string[] validImageTypes = { "bmp", "tiff", "png", "jpg", "jpeg" };
                if (!Array.Exists(validImageTypes, format => format.Equals(imageType, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new ArgumentException($"不支持的图片格式: {imageType}", nameof(imageType));
                }

                // 设置默认文件夹路径
                folderPath = string.IsNullOrWhiteSpace(folderPath)
                    ? Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
                    : folderPath;

                // 确保相机名称没有非法字符
                string sanitizedCameraName = string.Join("_", cameraName.Split(Path.GetInvalidFileNameChars()));

                // 生成文件名 时间戳
                string fileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_fffff"); // 更直观的日期时间格式

                // 拼接完整保存路径
                string savePath = Path.Combine(folderPath, sanitizedCameraName, $"{fileName}.{imageType}");

                // 确保文件夹存在
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));

                // 保存图像
                HOperatorSet.WriteImage(imgTemp, imageType, 0, savePath);
            }
            catch (Exception ex)
            {
                throw new Exception($"保存图像: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 从 Halcon 窗口中裁剪图像并保存到路径
        /// </summary>
        /// <param name="hWindowHandle">窗口句柄</param>
        /// <param name="imageType">图片格式："bmp", "tiff", "png", etc.</param>
        public static void SaveCropWindow(HTuple hWindowHandle, string cameraName, string folderPath = @"D:\Picture\SaveCropWindow", string imageType = "tiff")
        {
            HObject croppedImage = new HObject(); HOperatorSet.GenEmptyObj(out croppedImage);

            try
            {
                // 如果保存图片功能未启用，直接返回
                if (!SaveImageEnabled) return;

                // 验证窗口句柄是否有效
                if (hWindowHandle == null || hWindowHandle.Length == 0)
                {
                    throw new ArgumentException("无效的窗口句柄！", nameof(hWindowHandle));
                }

                // 验证图片格式
                string[] validImageTypes = { "bmp", "tiff", "png", "jpg", "jpeg" };
                if (!Array.Exists(validImageTypes, format => format.Equals(imageType, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new ArgumentException($"不支持的图片格式: {imageType}", nameof(imageType));
                }

                // 确保相机名称无非法字符
                string sanitizedCameraName = string.Join("_", cameraName.Split(Path.GetInvalidFileNameChars()));

                // 生成文件名 时间戳
                string fileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_fffff"); // 更直观的日期时间格式

                // 拼接完整保存路径
                string savePath = Path.Combine(folderPath, sanitizedCameraName, $"{fileName}.{imageType}");

                // 确保文件夹存在
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));

                // 在 Halcon 中利用 DumpWindowImage 提取窗口中的图像
                HOperatorSet.DumpWindowImage(out croppedImage, hWindowHandle);

                // 保存图像到目标路径
                HOperatorSet.WriteImage(croppedImage, imageType, 0, savePath);

            }
            catch (Exception ex)
            {
                throw new Exception($"截取窗口图像: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                croppedImage.Dispose();
            }
        }

        /// <summary>
        /// 保存小区域图像
        /// </summary>
        /// <param name="imgReduced">小区域</param>
        /// <param name="hImage">原图</param>
        /// <param name="ModelPath">保存路径</param>
        /// <param name="format">图片格式："bmp", "tiff", "png", etc.</param>
        public static void SaveSmallestRectImage(HObject hImage, HObject imgReduced, string ModelPath, string format = "bmp")
        {
            HObject saveImg = new HObject(); HOperatorSet.GenEmptyObj(out saveImg);

            try
            {
                HOperatorSet.SmallestRectangle1(imgReduced, out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2);
                HOperatorSet.GenRectangle1(out imgReduced, row1 - 20, column1 - 20, row2 + 20, column2 + 20);
                HOperatorSet.ReduceDomain(hImage, imgReduced, out saveImg);

                HOperatorSet.CropDomain(saveImg, out saveImg);
                ModelPath.EnsureFileDirectoryExists();
                HOperatorSet.WriteImage(saveImg, format, 0, ModelPath);
            }
            catch (Exception ex)
            {
                throw new Exception($"保存小区域图像: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                saveImg.Dispose();
            }
        }

        #endregion
    }
}
