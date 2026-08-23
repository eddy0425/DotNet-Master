using DotNet.Drawing;
using DotNet.HalconUI;
using HalconDotNet;
using System;
using System.Collections.Generic;

namespace DotNet.HalconAlgo
{
    /// <summary>
    /// Map 相机的一帧显示内容：图像副本 + 可选的拟合叠加数据（教导/取像流程为 null）。
    /// 创建后不再修改，整帧随 Dispose 一次性释放。
    /// </summary>
    public sealed class FitArcMidpointRenderFrame : IDisposable
    {
        public HObject Image { get; }
        public FitArcMidpointRenderData Overlay { get; }

        private FitArcMidpointRenderFrame(HObject image, FitArcMidpointRenderData overlay)
        {
            Image = image;
            Overlay = overlay;
        }

        /// <summary>
        /// 从相机原图复制出显示帧，并接管 overlay 的所有权；复制失败时 overlay 一并释放。
        /// </summary>
        public static FitArcMidpointRenderFrame Create(HObject sourceImage, FitArcMidpointRenderData overlay)
        {
            HObject image = null;
            try
            {
                image = sourceImage.CopyObj(1, -1);
                return new FitArcMidpointRenderFrame(image, overlay);
            }
            catch
            {
                image?.Dispose();
                overlay?.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            Overlay?.Dispose();
            Image?.Dispose();
        }
    }

    /// <summary>
    /// 圆弧中点拟合的一帧显示数据：拟合线程构建并发布，发布后视为只读。
    /// 样式与可见性开关在拟合时从 inPara 捕获，因此可在任意线程独立于策略实例绘制。
    /// 其中的 HObject 归本对象所有，随 Dispose 释放。
    /// </summary>
    public sealed class FitArcMidpointRenderData : IDisposable
    {
        /// <summary> 查找区域（蓝） </summary>
        public HObject SearchRegion;

        /// <summary> 拟合出的圆弧轮廓（红） </summary>
        public HObject ArcContour;

        /// <summary> 逐步测量矩形中心（拟合区域，蓝），姿态与尺寸各步相同 </summary>
        public List<double> MeasureRows = new List<double>();
        public List<double> MeasureCols = new List<double>();
        public double MeasurePhi;
        public double MeasureLen1;
        public double MeasureLen2;

        /// <summary> 参与拟合的点（绿） </summary>
        public List<double> UsedRows = new List<double>();
        public List<double> UsedCols = new List<double>();

        /// <summary> 被剔除的点（红） </summary>
        public List<double> RemovedRows = new List<double>();
        public List<double> RemovedCols = new List<double>();

        /// <summary> 圆弧中点（橙红），HasMidpoint 为 true 时有效 </summary>
        public double MidRow;
        public double MidCol;
        public bool HasMidpoint;

        /// <summary> 结果文本（绿），拟合失败时为 null </summary>
        public string Message;

        public int PointSize;
        public int FontX;
        public int FontY;
        public int FontSize;

        public bool ShowRegion;
        public bool ShowFixRegion;
        public bool ShowPoints;
        public bool ShowResult;
        public bool ShowText;

        /// <summary>
        /// 唯一的叠加层绘制入口：编辑器同步显示与机台异步刷新共用。
        /// 只绘制已生成的元素，拟合中途失败时呈现部分数据便于排查。
        /// </summary>
        public void DrawTo(IHDisplay display)
        {
            if (ShowRegion && SearchRegion != null)
            {
                display.DispRegion(SearchRegion, HColor.Blue);
            }

            if (ShowFixRegion)
            {
                for (int i = 0; i < MeasureRows.Count; i++)
                {
                    display.DispRectangle2(MeasureRows[i], MeasureCols[i], MeasurePhi, MeasureLen1, MeasureLen2, HColor.Blue);
                }
            }

            if (ShowPoints)
            {
                for (int i = 0; i < RemovedRows.Count; i++)
                {
                    display.DispPoint(RemovedCols[i], RemovedRows[i], HColor.Red, PointSize);
                }
                for (int i = 0; i < UsedRows.Count; i++)
                {
                    display.DispPoint(UsedCols[i], UsedRows[i], HColor.Green, PointSize);
                }
            }

            if (ShowResult)
            {
                if (ArcContour != null) display.DispRegion(ArcContour, HColor.Red);
                if (HasMidpoint) display.DispPoint(MidCol, MidRow, HColor.OrangeRed, PointSize + 50);
            }

            if (ShowText && !string.IsNullOrEmpty(Message))
            {
                display.DispText(Message, FontX, FontY, FontSize, HColor.Green);
            }
        }

        public void Dispose()
        {
            SearchRegion?.Dispose();
            ArcContour?.Dispose();
            SearchRegion = null;
            ArcContour = null;
        }
    }

}
