using HalconDotNet;
using System.Collections.Generic;


namespace DotNet.Drawing
{
    public class HalconHelper
    {
        private static HalconController controller = new HalconController();

        /// <summary>
        /// 获取文件路径
        /// </summary>
        public static string[] GetPaths(string imageFolder)
        {
            return controller.GetPaths(imageFolder);
        }

        /// <summary>
        /// 计算两点的中心点
        /// </summary>
        public static Point2d Cal2P(double x1, double y1, double x2, double y2)
        {
            return controller.Cal2P(x1, y1, x2, y2);
        }

        /// <summary>
        /// 计算两点之间的距离是否小于阈值
        /// </summary>
        public static bool IsNearPoint(double x1, double y1, double x2, double y2, double threshold = 3)
        {
            return controller.IsNearPoint(x1, y1, x2, y2, threshold);
        }

        /// <summary>
        /// 根据点坐标生成 XLD 轮廓 (Point2d: X=Column, Y=Row)
        /// </summary>
        public static void GenContours(List<Point2d> points, out HObject contour)
        {
            controller.GenContours(points, out contour);
        }

        /// <summary>
        /// 根据行点和列点生成多边形
        /// </summary>
        /// <param name="rowPoints">行点</param>
        /// <param name="columnPoints">列点</param>
        /// <returns>多边形</returns>
        public static List<Point2d> GetPolygons(double[] rowPoints, double[] columnPoints)
        {
            return controller.GetPolygons(rowPoints, columnPoints);
        }

        #region AffineTrans

        /// <summary> 获取反射变换矩阵 </summary>
        public static void VectorAngleToRigid(Point2d point, Point2d pointTrans, out HTuple hv_HomMat2D)
        {
            controller.VectorAngleToRigid(point, pointTrans, out hv_HomMat2D);
        }

        /// <summary> 获取反射变换矩阵 </summary>
        public static void VectorAngleToRigid(CvCoord coord, CvCoord coordTrans, out HTuple hv_HomMat2D)
        {
            controller.VectorAngleToRigid(coord, coordTrans, out hv_HomMat2D);
        }

        /// <summary> 获取反射变换点 </summary>
        public static void TransPixel(Point2d point, Point2d pointTrans, HTuple row, HTuple col, out HTuple rowTrans, out HTuple colTrans)
        {
            controller.TransPixel(point, pointTrans, row, col, out rowTrans, out colTrans);
        }

        /// <summary> 获取反射变换点 </summary>
        public static void TransPixel(CvCoord coord, CvCoord coordTrans, HTuple row, HTuple col, out HTuple rowTrans, out HTuple colTrans)
        {
            controller.TransPixel(coord, coordTrans, row, col, out rowTrans, out colTrans);
        }

        /// <summary> 获取反射变换区域 </summary>
        public static void TransRegion(Point2d point, Point2d pointTrans, HObject inRegion, out HObject outRegion)
        {
            controller.TransRegion(point, pointTrans, inRegion, out outRegion);
        }

        /// <summary> 获取反射变换区域 </summary>
        public static void TransRegion(CvCoord coord, CvCoord coordTrans, HObject inRegion, out HObject outRegion)
        {
            controller.TransRegion(coord, coordTrans, inRegion, out outRegion);
        }

        /// <summary> 获取反射变换轮廓 </summary>
        public static void TransContourXld(Point2d point, Point2d pointTrans, HObject contours, out HObject contoursAffineTrans)
        {
            controller.TransContourXld(point, pointTrans, contours, out contoursAffineTrans);
        }

        /// <summary> 获取反射变换轮廓 </summary>
        public static void TransContourXld(CvCoord coord, CvCoord coordTrans, HObject contours, out HObject contoursAffineTrans)
        {
            controller.TransContourXld(coord, coordTrans, contours, out contoursAffineTrans);
        }

        /// <summary> 获取反射变换区域和点 </summary>
        public static void TransRegionAndPixel(Point2d point, Point2d pointTrans, HObject inRegion, out HObject outRegion, HTuple row, HTuple col, out HTuple rowTrans, out HTuple colTrans)
        {
            controller.TransRegionAndPixel(point, pointTrans, inRegion, out outRegion, row, col, out rowTrans, out colTrans);
        }

        /// <summary> 获取反射变换区域和点 </summary>
        public static void TransRegionAndPixel(CvCoord coord, CvCoord coordTrans, HObject inRegion, out HObject outRegion, HTuple row, HTuple col, out HTuple rowTrans, out HTuple colTrans)
        {
            controller.TransRegionAndPixel(coord, coordTrans, inRegion, out outRegion, row, col, out rowTrans, out colTrans);
        }

        /// <summary>
        /// 计算经过刚体变换后的坐标
        /// </summary>
        /// <param name="follow">初始参考点</param>
        /// <param name="matching">目标参考点</param>
        /// <param name="target">需要变换的目标点</param>
        /// <param name="result">输出变换后的点</param>
        public static void GetTransformedCoord(Point2d point, Point2d pointTrans, CvCoord target, out CvCoord result)
        {
            controller.GetTransformedCoord(point, pointTrans, target, out result);
        }

        /// <summary>
        /// 计算经过刚体变换后的坐标
        /// </summary>
        /// <param name="follow">初始参考点</param>
        /// <param name="matching">目标参考点</param>
        /// <param name="target">需要变换的目标点</param>
        /// <param name="result">输出变换后的点</param>
        public static void GetTransformedCoord(CvCoord coord, CvCoord coordTrans, CvCoord target, out CvCoord result)
        {
            controller.GetTransformedCoord(coord, coordTrans, target, out result);
        }

        #endregion

        #region SaveImage

        /// <summary>
        /// 检查文件路径对应的文件夹是否存在，不存在则创建
        /// </summary>
        /// <param name="filePath">文件路径</param>
        public static void FileExists(string filePath)
        {
            controller.FileExists(filePath);
        }

        /// <summary> 保存图像 </summary>
        public static void SaveImage(HObject imgTemp, string filePath)
        {
            controller.SaveImage(imgTemp, filePath);
        }

        /// <summary>
        /// 保存图像
        /// </summary>
        /// <param name="imageType">图片格式："bmp", "tiff", "png", etc.</param>
        public static void SaveImage(HObject imgTemp, string folderPath, string imageType = "tiff")
        {
            controller.SaveImage(imgTemp, folderPath, imageType);
        }

        /// <summary>
        /// 保存图像
        /// </summary>
        /// <param name="imgTemp">图像</param>
        /// <param name="imageType">图片格式："bmp", "tiff", "png", etc.</param>
        public static void SaveImage(HObject imgTemp, string cameraName, string folderPath = @"D:\Picture\SaveOriginalImages", string imageType = "tiff")
        {
            controller.SaveImage(imgTemp, cameraName, folderPath, imageType);
        }

        /// <summary> 获取窗体图像 </summary>
        public static void GetCropWindow(HTuple hWindowHandle, out HObject image, string imageType = "tiff")
        {
            controller.GetCropWindow(hWindowHandle, out image, imageType);
        }

        /// <summary>
        /// 从 Halcon 窗口中裁剪图像并保存到路径
        /// </summary>
        /// <param name="hWindowHandle">Halcon 窗口句柄</param>
        /// <param name="imageType">图片格式："bmp", "tiff", "png", etc.</param>
        public static void SaveCropWindow(HTuple hWindowHandle, string folderPath, string imageType = "tiff")
        {
            controller.SaveCropWindow(hWindowHandle, folderPath, imageType);
        }

        /// <summary>
        /// 从 Halcon 窗口中裁剪图像并保存到路径
        /// </summary>
        /// <param name="hWindowHandle">窗口句柄</param>
        /// <param name="imageType">图片格式："bmp", "tiff", "png", etc.</param>
        public static void SaveCropWindow(HTuple hWindowHandle, string cameraName, string folderPath = @"D:\Picture\SaveCropWindow", string imageType = "tiff")
        {
            controller.SaveCropWindow(hWindowHandle, cameraName, folderPath, imageType);
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
            controller.SaveSmallestRectImage(hImage, imgReduced, ModelPath, format);
        }

        #endregion

        /// <summary> 由 XLD 轮廓生成"白底黑色填充"的掩膜图像 </summary>
        public static void GetContourImage(HObject hImage, HObject contour, out HObject ho_ResultImage)
        {
            controller.GetContourImage(hImage, contour, out ho_ResultImage);
        }

    }
}
