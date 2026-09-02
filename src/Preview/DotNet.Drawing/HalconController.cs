using HalconDotNet;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;


namespace DotNet.Drawing
{
    public static class HalconController
    {
        /// <summary>
        /// 获取文件路径
        /// </summary>
        public static string[] GetPaths(string imageFolder)
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

        // 已删除 Cal2P / IsNearPoint：两者都是散着 4 个 double 的裸坐标 API（C1 要消除的形态），
        // 且全工程无调用方，功能分别等价于 Point2d.Lerp(other, 0.5) 与 Point2d.DistanceTo。

        /// <summary>
        /// 根据点坐标生成 XLD 轮廓 (Point2d: X=Column, Y=Row)
        /// </summary>
        public static void GenContours(List<Point2d> points, out HObject contour)
        {
            HOperatorSet.GenEmptyObj(out contour);
            if (points == null || points.Count < 2) return;

            try
            {
                double[] rows = new double[points.Count];
                double[] columns = new double[points.Count];
                for (int i = 0; i < points.Count; i++)
                {
                    rows[i] = points[i].Y;
                    columns[i] = points[i].X;
                }

                contour.Dispose();
                HOperatorSet.GenContourPolygonXld(out contour, new HTuple(rows), new HTuple(columns));
            }
            catch (Exception ex)
            {
                // 异常路径下保证 contour 始终是可被调用方安全 Dispose 的有效空对象
                if (contour == null || !contour.IsInitialized())
                {
                    HOperatorSet.GenEmptyObj(out contour);
                }
                System.Diagnostics.Debug.WriteLine($"GenContours Error: {ex.Message}");
            }
        }

        #region AffineTrans

        /// <summary> 获取仿射变换矩阵 </summary>
        public static void VectorAngleToRigid(Point2d point, Point2d pointTrans, out HTuple hv_HomMat2D)
        {
            HOperatorSet.VectorAngleToRigid(point.Y, point.X, 0, pointTrans.Y, pointTrans.X, 0, out hv_HomMat2D);
        }

        /// <summary> 获取仿射变换矩阵 </summary>
        public static void VectorAngleToRigid(CvCoord coord, CvCoord coordTrans, out HTuple hv_HomMat2D)
        {
            // vector_angle_to_rigid 要求弧度；CvCoord.Angle 是强类型 Angle，取 .Radians 即可，
            // 不存在也不可能再出现「多补一次 ToRadians」的单位错误（B5）。
            HOperatorSet.VectorAngleToRigid(coord.Y, coord.X, coord.Angle.Radians,
                                            coordTrans.Y, coordTrans.X, coordTrans.Angle.Radians,
                                            out hv_HomMat2D);
        }

        /// <summary>
        /// 按刚体变换映射单点
        /// </summary>
        /// <remarks>
        /// 对应审查项 C1：对外只认 <see cref="Point2d"/>(X, Y)，HALCON 的 (Row, Column) 顺序
        /// 只在本方法内部翻转一次。原来的 <c>TransPixel(..., HTuple row, HTuple col, out ...)</c>
        /// 把行列序暴露给了每一个调用方，且两端都是 <c>HTuple</c>，传反了编译器不会报错。
        /// </remarks>
        public static Point2d TransPoint(Point2d origin, Point2d target, Point2d source)
        {
            VectorAngleToRigid(origin, target, out HTuple hv_HomMat2D);
            return AffineTransPoint(hv_HomMat2D, source);
        }

        /// <summary> 按刚体变换映射单点（含旋转） </summary>
        public static Point2d TransPoint(CvCoord origin, CvCoord target, Point2d source)
        {
            VectorAngleToRigid(origin, target, out HTuple hv_HomMat2D);
            return AffineTransPoint(hv_HomMat2D, source);
        }

        /// <summary> 按刚体变换批量映射点 </summary>
        public static Point2d[] TransPoints(Point2d origin, Point2d target, IReadOnlyList<Point2d> sources)
        {
            VectorAngleToRigid(origin, target, out HTuple hv_HomMat2D);
            return AffineTransPoints(hv_HomMat2D, sources);
        }

        /// <summary> 按刚体变换批量映射点（含旋转） </summary>
        public static Point2d[] TransPoints(CvCoord origin, CvCoord target, IReadOnlyList<Point2d> sources)
        {
            VectorAngleToRigid(origin, target, out HTuple hv_HomMat2D);
            return AffineTransPoints(hv_HomMat2D, sources);
        }

        /// <summary>
        /// (X, Y) → HALCON (Row, Column) 的唯一翻转点：单点。
        /// </summary>
        private static Point2d AffineTransPoint(HTuple homMat2D, Point2d source)
        {
            HOperatorSet.AffineTransPixel(homMat2D, source.Y, source.X, out HTuple rowTrans, out HTuple colTrans);
            return new Point2d(colTrans.D, rowTrans.D);
        }

        /// <summary>
        /// (X, Y) → HALCON (Row, Column) 的唯一翻转点：点集。
        /// </summary>
        private static Point2d[] AffineTransPoints(HTuple homMat2D, IReadOnlyList<Point2d> sources)
        {
            if (sources == null) throw new ArgumentNullException(nameof(sources));
            if (sources.Count == 0) return new Point2d[0];

            double[] rows = new double[sources.Count];
            double[] cols = new double[sources.Count];
            for (int i = 0; i < sources.Count; i++)
            {
                rows[i] = sources[i].Y;
                cols[i] = sources[i].X;
            }

            HOperatorSet.AffineTransPixel(homMat2D, new HTuple(rows), new HTuple(cols),
                                          out HTuple rowTrans, out HTuple colTrans);

            var result = new Point2d[sources.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new Point2d(colTrans[i].D, rowTrans[i].D);
            }
            return result;
        }

        /// <summary> 获取仿射变换区域 </summary>
        public static void TransRegion(Point2d point, Point2d pointTrans, HObject region, out HObject regionTrans)
        {
            VectorAngleToRigid(point, pointTrans, out HTuple hv_HomMat2D);
            HOperatorSet.AffineTransRegion(region, out regionTrans, hv_HomMat2D, "nearest_neighbor");
        }

        /// <summary> 获取仿射变换区域 </summary>
        public static void TransRegion(CvCoord coord, CvCoord coordTrans, HObject region, out HObject regionTrans)
        {
            VectorAngleToRigid(coord, coordTrans, out HTuple hv_HomMat2D);
            HOperatorSet.AffineTransRegion(region, out regionTrans, hv_HomMat2D, "nearest_neighbor");
        }

        /// <summary> 获取仿射变换轮廓 </summary>
        public static void TransContourXld(Point2d point, Point2d pointTrans, HObject contours, out HObject contoursTrans)
        {
            VectorAngleToRigid(point, pointTrans, out HTuple hv_HomMat2D);
            HOperatorSet.AffineTransContourXld(contours, out contoursTrans, hv_HomMat2D);
        }

        /// <summary> 获取仿射变换轮廓 </summary>
        public static void TransContourXld(CvCoord coord, CvCoord coordTrans, HObject contours, out HObject contoursTrans)
        {
            VectorAngleToRigid(coord, coordTrans, out HTuple hv_HomMat2D);
            HOperatorSet.AffineTransContourXld(contours, out contoursTrans, hv_HomMat2D);
        }

        /// <summary>
        /// 计算经过刚体变换后的坐标
        /// </summary>
        /// <remarks>
        /// Point2d 只有位置、没有朝向（其 <see cref="Point2d.Angle"/> 是"与原点连线的夹角"，
        /// 不代表坐标系旋转量），因此本重载与 <see cref="VectorAngleToRigid(Point2d, Point2d, out HTuple)"/>
        /// 保持一致：纯平移，不旋转，target 的角度原样保留。
        /// 需要含旋转的变换请使用 CvCoord 重载。
        /// </remarks>
        public static void GetTransformedCoord(Point2d point, Point2d pointTrans, CvCoord target, out CvCoord result)
        {
            double transformedX = target.X - point.X + pointTrans.X;
            double transformedY = target.Y - point.Y + pointTrans.Y;

            result = new CvCoord(transformedX, transformedY, target.Angle);
        }

        /// <summary>
        /// 计算经过刚体变换后的坐标
        /// </summary>
        public static void GetTransformedCoord(CvCoord coord, CvCoord coordTrans, CvCoord target, out CvCoord result)
        {
            // Angle 是强类型角度，相减即为旋转量；取三角函数时显式落到弧度
            Angle angleDiff = coordTrans.Angle - coord.Angle;

            double cosTheta = Math.Cos(angleDiff.Radians);
            double sinTheta = Math.Sin(angleDiff.Radians);

            double transformedX = cosTheta * (target.X - coord.X) - sinTheta * (target.Y - coord.Y) + coordTrans.X;
            double transformedY = sinTheta * (target.X - coord.X) + cosTheta * (target.Y - coord.Y) + coordTrans.Y;

            result = new CvCoord(transformedX, transformedY, target.Angle + angleDiff);
        }

        #endregion

        #region SaveImage

        /// <summary>
        /// 检查文件路径对应的文件夹是否存在，不存在则创建
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public static void FileExists(string filePath)
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
        public static void SaveImage(HObject imgTemp, string filePath)
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

                // 注意: 本重载的落盘路径完全由 filePath 决定, 不再生成时间戳文件名
                // (原来这里有一个 fileName 变量, 算完从未被使用).

                // 确保文件夹存在: 走 FileExists, 它对 Path.GetDirectoryName 返回 null/空的情况有兜底,
                // 原写法在 filePath 为纯文件名时会把 null 传给 CreateDirectory 直接 NRE.
                FileExists(filePath);

                // 保存图像
                HOperatorSet.WriteImage(imgTemp, imageType, 0, filePath);
            }
            catch (Exception ex)
            {
                throw new Exception($"保存图像失败：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 保存图像
        /// </summary>
        /// <param name="folderPath">保存目录，文件名由时间戳生成</param>
        /// <param name="imageType">图片格式："bmp", "tiff", "png", etc.</param>
        /// <remarks>
        /// 三个 SaveImage 重载原先都带默认参数，参数个数区间互相重叠（2~2 / 2~3 / 2~4），
        /// 同一个调用写法会落到不同重载上。现在去掉全部默认值，让三者的参数个数分别为 2 / 3 / 4，
        /// 调用点必须显式表达意图。
        /// </remarks>
        public static void SaveImage(HObject imgTemp, string folderPath, string imageType)
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
                FileExists(saveFilePath);

                // 保存图像
                HOperatorSet.WriteImage(imgTemp, imageType, 0, saveFilePath);
            }
            catch (Exception ex)
            {
                throw new Exception($"保存图像失败：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 保存图像
        /// </summary>
        /// <param name="imgTemp">图像</param>
        /// <param name="cameraName">相机名，作为 folderPath 下的一级子目录</param>
        /// <param name="folderPath">保存根目录，传空则回落到 <see cref="DrawingPaths.OriginalImageDir"/></param>
        /// <param name="imageType">图片格式："bmp", "tiff", "png", etc.</param>
        /// <remarks>原默认值是硬编码的 D:\Picture\SaveOriginalImages，换台机器就写不进去；改为由 DrawingPaths 统一配置。</remarks>
        public static void SaveImage(HObject imgTemp, string cameraName, string folderPath, string imageType)
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
                if (string.IsNullOrWhiteSpace(folderPath)) folderPath = DrawingPaths.OriginalImageDir;

                // 确保相机名称没有非法字符
                string sanitizedCameraName = string.Join("_", cameraName.Split(Path.GetInvalidFileNameChars()));

                // 生成文件名 时间戳
                string fileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_fffff"); // 更直观的日期时间格式

                // 拼接完整保存路径
                string savePath = Path.Combine(folderPath, sanitizedCameraName, $"{fileName}.{imageType}");

                // 确保文件夹存在
                FileExists(savePath);

                // 保存图像
                HOperatorSet.WriteImage(imgTemp, imageType, 0, savePath);
            }
            catch (Exception ex)
            {
                throw new Exception($"保存图像失败：{ex.Message}", ex);
            }
        }

        /// <summary> 获取窗体图像 </summary>
        public static void GetCropWindow(HTuple hWindowHandle, out HObject croppedImage, string imageType = "tiff")
        {
            HOperatorSet.GenEmptyObj(out croppedImage);

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
                croppedImage.Dispose();
                HOperatorSet.DumpWindowImage(out croppedImage, hWindowHandle);

                // 验证裁剪结果
                if (croppedImage == null || !croppedImage.IsInitialized())
                {
                    throw new InvalidOperationException("无法从指定的窗口句柄裁剪图像！");
                }

                // 如果有特殊需求处理其他逻辑，可以在这添加额外的代码
            }
            catch (Exception ex)
            {
                if (croppedImage.NotNull()) croppedImage.Dispose();
                throw new Exception($"获取裁剪图像失败：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 从 Halcon 窗口中裁剪图像并保存到路径
        /// </summary>
        /// <param name="hWindowHandle">Halcon 窗口句柄</param>
        /// <param name="folderPath">保存目录，文件名由时间戳生成</param>
        /// <param name="imageType">图片格式："bmp", "tiff", "png", etc.</param>
        /// <remarks>与 SaveImage 同理去掉默认值，使两个重载的参数个数分别为 3 / 4，不再互相遮蔽。</remarks>
        public static void SaveCropWindow(HTuple hWindowHandle, string folderPath, string imageType)
        {
            HObject croppedImage; HOperatorSet.GenEmptyObj(out croppedImage);

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
                croppedImage.Dispose();
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
                FileExists(saveFilePath);

                // 保存图像
                HOperatorSet.WriteImage(croppedImage, imageType, 0, saveFilePath);

            }
            catch (Exception ex)
            {
                throw new Exception($"裁剪窗体图像失败：{ex.Message}", ex);
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
        /// <param name="cameraName">相机名，作为 folderPath 下的一级子目录</param>
        /// <param name="folderPath">保存根目录，传空则回落到 <see cref="DrawingPaths.CropWindowDir"/></param>
        /// <param name="imageType">图片格式："bmp", "tiff", "png", etc.</param>
        /// <remarks>原默认值是硬编码的 D:\Picture\SaveCropWindow；改为由 DrawingPaths 统一配置。</remarks>
        public static void SaveCropWindow(HTuple hWindowHandle, string cameraName, string folderPath, string imageType)
        {
            HObject croppedImage; HOperatorSet.GenEmptyObj(out croppedImage);

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

                // 设置默认文件夹路径
                if (string.IsNullOrWhiteSpace(folderPath)) folderPath = DrawingPaths.CropWindowDir;

                // 确保相机名称无非法字符
                string sanitizedCameraName = string.Join("_", cameraName.Split(Path.GetInvalidFileNameChars()));

                // 生成文件名 时间戳
                string fileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_fffff"); // 更直观的日期时间格式

                // 拼接完整保存路径
                string savePath = Path.Combine(folderPath, sanitizedCameraName, $"{fileName}.{imageType}");

                // 确保文件夹存在
                FileExists(savePath);

                // 在 Halcon 中利用 DumpWindowImage 提取窗口中的图像
                croppedImage.Dispose();
                HOperatorSet.DumpWindowImage(out croppedImage, hWindowHandle);

                // 保存图像到目标路径
                HOperatorSet.WriteImage(croppedImage, imageType, 0, savePath);

            }
            catch (Exception ex)
            {
                throw new Exception($"裁剪窗体图像失败：{ex.Message}", ex);
            }
            finally
            {
                croppedImage.Dispose();
            }
        }

        /// <summary>
        /// 保存小区域图像
        /// </summary>
        public static void SaveSmallestRectImage(HObject hImage, HObject imgReduced, string ModelPath, string format = "bmp")
        {
            HObject rectangle; HOperatorSet.GenEmptyObj(out rectangle);
            HObject imageReduced; HOperatorSet.GenEmptyObj(out imageReduced);
            HObject imagePart; HOperatorSet.GenEmptyObj(out imagePart);

            try
            {
                HOperatorSet.SmallestRectangle1(imgReduced, out HTuple row1, out HTuple column1, out HTuple row2, out HTuple column2);
               
                rectangle.Dispose();
                HOperatorSet.GenRectangle1(out rectangle, row1 - 20, column1 - 20, row2 + 20, column2 + 20);

                imageReduced.Dispose();
                HOperatorSet.ReduceDomain(hImage, rectangle, out imageReduced);

                imagePart.Dispose();
                HOperatorSet.CropDomain(imageReduced, out imagePart);

                FileExists(ModelPath);
                HOperatorSet.WriteImage(imagePart, format, 0, ModelPath);
            }
            catch (Exception ex)
            {
                throw new Exception($"保存小区域图像失败：{ex.Message}", ex);
            }
            finally
            {
                rectangle.Dispose();
                imageReduced.Dispose();
                imagePart.Dispose();
            }
        }

        #endregion

        /// <summary> 由 XLD 轮廓生成"白底黑色填充"的掩膜图像 </summary>
        public static void GetContourImage(HObject hImage, HObject contour, out HObject ho_ResultImage)
        {
            HObject ho_Region = null;
            HObject ho_WhiteImage = null;
            ho_ResultImage = null;
            try
            {
                HOperatorSet.GenRegionContourXld(contour, out ho_Region, "filled");
                HOperatorSet.GenImageProto(hImage, out ho_WhiteImage, 255);
                HOperatorSet.PaintRegion(ho_Region, ho_WhiteImage, out ho_ResultImage, 0, "fill");
            }
            finally
            {
                if (ho_Region != null) ho_Region.Dispose();
                if (ho_WhiteImage != null) ho_WhiteImage.Dispose();
            }
        }
    }
}
