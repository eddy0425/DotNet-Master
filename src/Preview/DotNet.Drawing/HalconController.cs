using HalconDotNet;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;


namespace DotNet.Drawing
{
    public class HalconController
    {
        /// <summary>
        /// 获取文件路径
        /// </summary>
        public string[] GetPaths(string imageFolder)
        {
            var imagePaths = Directory.GetFiles(imageFolder);
            if (imagePaths.Length == 0)
                throw new InvalidOperationException("图片路径为空！");

            var numericFiles = new List<Tuple<int, string>>();
            var nonNumericFiles = new List<string>();

            foreach (var path in imagePaths)
            {
                string fileName = Path.GetFileNameWithoutExtension(path);
                int number;
                if (int.TryParse(fileName, out number))
                    numericFiles.Add(Tuple.Create(number, path));
                else
                    nonNumericFiles.Add(path);
            }

            var sorted = numericFiles.OrderBy(x => x.Item1)
                                    .Select(x => x.Item2)
                                    .Concat(nonNumericFiles)
                                    .ToArray();
            return sorted;
        }

        /// <summary>
        /// 计算两点的中心点
        /// </summary>
        public Point2d Cal2P(double x1, double y1, double x2, double y2)
        {
            return new Point2d((x1 + x2) / 2, (y1 + y2) / 2);
        }

        /// <summary>
        /// 计算两点之间的距离是否小于阈值
        /// </summary>
        public bool IsNearPoint(double x1, double y1, double x2, double y2, double threshold = 3)
        {
            return Math.Abs(x1 - x2) < threshold && Math.Abs(y1 - y2) < threshold;
        }

        /// <summary>
        /// 根据点坐标生成 XLD 轮廓
        /// </summary>
        /// <param name="points">坐标数组 (X=Column, Y=Row)</param>
        /// <returns>生成的 XLD 轮廓对象</returns>
        public HObject GenContours(List<Point2d> points)
        {
            HObject contour;
            HOperatorSet.GenEmptyObj(out contour);

            if (points == null || points.Count < 2) return contour;

            try
            {
                // 提取所有点的坐标 (Point2d: X=Column, Y=Row)
                double[] rows = new double[points.Count];
                double[] columns = new double[points.Count];

                for (int i = 0; i < points.Count; i++)
                {
                    rows[i] = points[i].Y;      // Row
                    columns[i] = points[i].X;   // Column
                }

                // 将 double[] 转换为 HTuple
                HTuple hv_Rows = new HTuple(rows);
                HTuple hv_Columns = new HTuple(columns);

                // 使用 gen_contour_polygon_xld 生成闭合的多边形轮廓
                contour.Dispose();
                HOperatorSet.GenContourPolygonXld(out contour, hv_Rows, hv_Columns);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GenContours Error: {ex.Message}");
            }

            return contour;
        }

        /// <summary>
        /// 根据行点和列点生成多边形
        /// </summary>
        /// <param name="rowPoints">行点</param>
        /// <param name="columnPoints">列点</param>
        /// <returns>多边形</returns>
        public List<Point2d>? GetPolygons(double[] rowPoints, double[] columnPoints)
        {
            if (rowPoints.Length != columnPoints.Length) return null;

            var polygons = new List<Point2d>();

            for (int i = 0; i < rowPoints.Length; i++)
            {
                polygons.Add(new Point2d(columnPoints[i], rowPoints[i]));
            }

            return polygons;
        }

        #region AffineTrans

        /// <summary> 获取反射变换矩阵 </summary>
        public void VectorAngleToRigid(Point2d point, Point2d pointTrans, out HTuple hv_HomMat2D)
        {
            HOperatorSet.VectorAngleToRigid(point.Y, point.X, 0, pointTrans.Y, pointTrans.X, 0, out hv_HomMat2D);
        }

        /// <summary> 获取反射变换矩阵 </summary>
        public void VectorAngleToRigid(CvCoord coord, CvCoord coordTrans, out HTuple hv_HomMat2D)
        {
            HOperatorSet.VectorAngleToRigid(coord.Y, coord.X, coord.Angle.ToRadians(),
                                            coordTrans.Y, coordTrans.X, coordTrans.Angle.ToRadians(),
                                            out hv_HomMat2D);
        }

        /// <summary> 获取反射变换点 </summary>
        public void TransPixel(Point2d point, Point2d pointTrans, HTuple row, HTuple col, out HTuple rowTrans, out HTuple colTrans)
        {
            VectorAngleToRigid(point, pointTrans, out HTuple hv_HomMat2D);
            HOperatorSet.AffineTransPixel(hv_HomMat2D, row, col, out rowTrans, out colTrans);
        }

        /// <summary> 获取反射变换点 </summary>
        public void TransPixel(CvCoord coord, CvCoord coordTrans, HTuple row, HTuple col, out HTuple rowTrans, out HTuple colTrans)
        {
            VectorAngleToRigid(coord, coordTrans, out HTuple hv_HomMat2D);
            HOperatorSet.AffineTransPixel(hv_HomMat2D, row, col, out rowTrans, out colTrans);
        }

        /// <summary> 获取反射变换区域 </summary>
        public void TransRegion(Point2d point, Point2d pointTrans, HObject region, out HObject regionTrans)
        {
            VectorAngleToRigid(point, pointTrans, out HTuple hv_HomMat2D);
            HOperatorSet.AffineTransRegion(region, out regionTrans, hv_HomMat2D, "nearest_neighbor");
        }

        /// <summary> 获取反射变换区域 </summary>
        public void TransRegion(CvCoord coord, CvCoord coordTrans, HObject region, out HObject regionTrans)
        {
            VectorAngleToRigid(coord, coordTrans, out HTuple hv_HomMat2D);
            HOperatorSet.AffineTransRegion(region, out regionTrans, hv_HomMat2D, "nearest_neighbor");
        }

        /// <summary> 获取反射变换轮廓 </summary>
        public void TransContourXld(Point2d point, Point2d pointTrans, HObject contours, out HObject contoursTrans)
        {
            VectorAngleToRigid(point, pointTrans, out HTuple hv_HomMat2D);
            HOperatorSet.AffineTransContourXld(contours, out contoursTrans, hv_HomMat2D);
        }

        /// <summary> 获取反射变换轮廓 </summary>
        public void TransContourXld(CvCoord coord, CvCoord coordTrans, HObject contours, out HObject contoursTrans)
        {
            VectorAngleToRigid(coord, coordTrans, out HTuple hv_HomMat2D);
            HOperatorSet.AffineTransContourXld(contours, out contoursTrans, hv_HomMat2D);
        }

        /// <summary> 获取反射变换区域和点 </summary>
        public void TransRegionAndPixel(Point2d point, Point2d pointTrans, HObject inRegion, out HObject outRegion, HTuple row, HTuple col, out HTuple rowTrans, out HTuple colTrans)
        {
            VectorAngleToRigid(point, pointTrans, out HTuple hv_HomMat2D);
            HOperatorSet.AffineTransRegion(inRegion, out outRegion, hv_HomMat2D, "nearest_neighbor");
            HOperatorSet.AffineTransPixel(hv_HomMat2D, row, col, out rowTrans, out colTrans);
        }

        /// <summary> 获取反射变换区域和点 </summary>
        public void TransRegionAndPixel(CvCoord coord, CvCoord coordTrans, HObject inRegion, out HObject outRegion, HTuple row, HTuple col, out HTuple rowTrans, out HTuple colTrans)
        {
            VectorAngleToRigid(coord, coordTrans, out HTuple hv_HomMat2D);
            HOperatorSet.AffineTransRegion(inRegion, out outRegion, hv_HomMat2D, "nearest_neighbor");
            HOperatorSet.AffineTransPixel(hv_HomMat2D, row, col, out rowTrans, out colTrans);
        }

        /// <summary>
        /// 计算经过刚体变换后的坐标
        /// </summary>
        /// <param name="follow">初始参考点</param>
        /// <param name="matching">目标参考点</param>
        /// <param name="target">需要变换的目标点</param>
        /// <param name="result">输出变换后的点</param>
        public void GetTransformedCoord(Point2d point, Point2d pointTrans, CvCoord target, out CvCoord result)
        {
            // 计算角度差（弧度）
            double angleDiff = (pointTrans.Angle - point.Angle).ToRadians();

            // 计算平移差
            double deltaX = pointTrans.X - point.X;
            double deltaY = pointTrans.Y - point.Y;

            // 旋转矩阵
            double cosTheta = Math.Cos(0);
            double sinTheta = Math.Sin(0);

            // 计算新坐标
            double transformedX = cosTheta * (target.X - point.X) - sinTheta * (target.Y - point.Y) + pointTrans.X;
            double transformedY = sinTheta * (target.X - point.X) + cosTheta * (target.Y - point.Y) + pointTrans.Y;

            // 更新结果
            var angle = target.Angle + (pointTrans.Angle - point.Angle); // 更新角度
            result = new CvCoord(transformedX, transformedY, angle);
        }

        /// <summary>
        /// 计算经过刚体变换后的坐标
        /// </summary>
        /// <param name="follow">初始参考点</param>
        /// <param name="matching">目标参考点</param>
        /// <param name="target">需要变换的目标点</param>
        /// <param name="result">输出变换后的点</param>
        public void GetTransformedCoord(CvCoord coord, CvCoord coordTrans, CvCoord target, out CvCoord result)
        {
            // 计算角度差（弧度）
            double angleDiff = (coordTrans.Angle - coord.Angle).ToRadians();

            // 计算平移差
            double deltaX = coordTrans.X - coord.X;
            double deltaY = coordTrans.Y - coord.Y;

            // 旋转矩阵
            double cosTheta = Math.Cos(angleDiff);
            double sinTheta = Math.Sin(angleDiff);

            // 计算新坐标
            double transformedX = cosTheta * (target.X - coord.X) - sinTheta * (target.Y - coord.Y) + coordTrans.X;
            double transformedY = sinTheta * (target.X - coord.X) + cosTheta * (target.Y - coord.Y) + coordTrans.Y;

            // 更新结果
            var angle = target.Angle + (coordTrans.Angle - coord.Angle); // 更新角度
            result = new CvCoord(transformedX, transformedY, angle);
        }

        #endregion

        #region SaveImage

        /// <summary>
        /// 检查文件路径对应的文件夹是否存在，不存在则创建
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public void FileExists(string filePath)
        {
            // Null或空字符串检查
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath), "文件路径不能为空");
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("文件路径不能为空白字符", nameof(filePath));
            }

            try
            {
                var directoryPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
            }
            catch (PathTooLongException ex)
            {
                // 提供中文提示：路径太长
                throw new InvalidOperationException(
                    $"路径超出了系统定义的最大长度。当前路径长度为 {filePath.Length} 个字符，路径不能超过 260 个字符。",
                    ex
                );
            }
            catch (ArgumentException ex)
            {
                // 提供中文提示：路径中包含非法字符
                throw new ArgumentException("路径中包含非法字符", nameof(filePath), ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                // 提供中文提示：无权限访问路径
                throw new UnauthorizedAccessException("没有权限访问指定的路径，请检查权限设置。", ex);
            }
            catch (Exception ex)
            {
                // 捕获其他异常，提供统一的中文提示
                throw new InvalidOperationException($"创建文件目录时发生未知错误：{ex.Message}", ex);
            }
        }

        /// <summary> 保存图像 </summary>
        public void SaveImage(HObject imgTemp, string filePath)
        {
            try
            {
                // 验证图像参数
                if (imgTemp == null || !imgTemp.IsInitialized())
                {
                    throw new ArgumentNullException(nameof(imgTemp), "传入的图像为空或未初始化！");
                }

                // 从文件路径中提取扩展名作为图像格式
                // Path.GetExtension 返回带点的扩展名，如 ".png"，TrimStart('.') 去掉点
                string imageType = Path.GetExtension(filePath)?.TrimStart('.').ToLowerInvariant();

                // 验证图片格式
                string[] validImageTypes = { "bmp", "tiff", "png", "jpg", "jpeg" };
                if (!Array.Exists(validImageTypes, format => format.Equals(imageType, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new ArgumentException($"不支持的图片格式: {imageType}", nameof(imageType));
                }

                // 生成文件名 时间戳
                string fileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_fffff"); // 更直观的日期时间格式

                // 确保文件夹存在
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                // 保存图像
                HOperatorSet.WriteImage(imgTemp, imageType, 0, filePath);
            }
            catch (Exception ex)
            {
                throw new Exception($"保存图像: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 保存图像
        /// </summary>
        /// <param name="imageType">图片格式："bmp", "tiff", "png", etc.</param>
        public void SaveImage(HObject imgTemp, string folderPath, string imageType = "tiff")
        {
            try
            {
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
        /// 保存图像
        /// </summary>
        /// <param name="imgTemp">图像</param>
        /// <param name="imageType">图片格式："bmp", "tiff", "png", etc.</param>
        public void SaveImage(HObject imgTemp, string cameraName, string folderPath = @"D:\Picture\SaveOriginalImages", string imageType = "tiff")
        {
            try
            {
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

        /// <summary> 获取窗体图像 </summary>
        public void GetCropWindow(HTuple hWindowHandle, out HObject image, string imageType = "tiff")
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
        /// 从 Halcon 窗口中裁剪图像并保存到路径
        /// </summary>
        /// <param name="hWindowHandle">Halcon 窗口句柄</param>
        /// <param name="imageType">图片格式："bmp", "tiff", "png", etc.</param>
        public void SaveCropWindow(HTuple hWindowHandle, string folderPath, string imageType = "tiff")
        {
            HObject croppedImage = new HObject(); HOperatorSet.GenEmptyObj(out croppedImage);

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
        /// 从 Halcon 窗口中裁剪图像并保存到路径
        /// </summary>
        /// <param name="hWindowHandle">窗口句柄</param>
        /// <param name="imageType">图片格式："bmp", "tiff", "png", etc.</param>
        public void SaveCropWindow(HTuple hWindowHandle, string cameraName, string folderPath = @"D:\Picture\SaveCropWindow", string imageType = "tiff")
        {
            HObject croppedImage = new HObject(); HOperatorSet.GenEmptyObj(out croppedImage);

            try
            {
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
        public void SaveSmallestRectImage(HObject hImage, HObject imgReduced, string ModelPath, string format = "bmp")
        {
            HObject saveImg = new HObject(); HOperatorSet.GenEmptyObj(out saveImg);

            try
            {
                HOperatorSet.SmallestRectangle1(imgReduced, out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2);
                HOperatorSet.GenRectangle1(out imgReduced, row1 - 20, column1 - 20, row2 + 20, column2 + 20);
                HOperatorSet.ReduceDomain(hImage, imgReduced, out saveImg);

                HOperatorSet.CropDomain(saveImg, out saveImg);
                FileExists(ModelPath);
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
