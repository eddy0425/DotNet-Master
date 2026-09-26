using HalconDotNet;
using System.Collections.Generic;


namespace DotNet.Drawing
{
    public static class RegionExtension
    {
        /// <summary>
        /// 换入新句柄并释放旧句柄。
        /// </summary>
        /// <remarks>
        /// 三步顺序不能调整：先换引用、后释放旧句柄。反过来（先 Dispose 再赋值）会留下
        /// 一个 <c>HoRegion</c> 指向已释放对象的窗口 —— 与 C16 在 <c>HWindowImage</c> 修掉的是同一类问题。
        /// <para>
        /// 旧句柄用 <c>?.</c>：<see cref="CvRegion.Dispose"/> 之后 <see cref="CvRegion.HoRegion"/> 为 null，
        /// 原来六处都是裸 <c>Dispose()</c>，对已释放的 <see cref="CvRegion"/> 调用本族方法必然 NRE。
        /// </para>
        /// </remarks>
        private static void ReplaceHandle(CvRegion hRegion, HObject newHandle)
        {
            var old = hRegion.HoRegion;
            hRegion.HoRegion = newHandle;
            old?.Dispose();
        }

        /// <summary>
        /// 根据区域类型和几何参数重新生成 Halcon 区域
        /// </summary>
        public static void RebuildRegion(this CvRegion hRegion)
        {
            if (hRegion == null) return;
            switch (hRegion.Type)
            {
                case RectEnum.Rectangle:
                    {
                        HOperatorSet.GenRectangle1(out HObject rectangle, hRegion.TopLeft.Y, hRegion.TopLeft.X,
                                               hRegion.BottomRight.Y, hRegion.BottomRight.X);
                        ReplaceHandle(hRegion, rectangle);
                    }
                    break;
                case RectEnum.AffRect:
                    {
                        HOperatorSet.GenRectangle2(out HObject rectangle, hRegion.CenterY, hRegion.CenterX, hRegion.Phi,
                                               hRegion.Width / 2, hRegion.Height / 2);
                        ReplaceHandle(hRegion, rectangle);
                    }
                    break;
                case RectEnum.Circle:
                    {
                        HOperatorSet.GenCircle(out HObject circle, hRegion.CenterY, hRegion.CenterX, hRegion.Width / 2);
                        ReplaceHandle(hRegion, circle);
                    }
                    break;
                case RectEnum.Ellipse:
                    {
                        HOperatorSet.GenEllipse(out HObject ellipse, hRegion.CenterY, hRegion.CenterX, hRegion.Phi,
                                               hRegion.Width / 2, hRegion.Height / 2);
                        ReplaceHandle(hRegion, ellipse);
                    }
                    break;
                case RectEnum.Polygon:
                    {
                        // 点集为空时直接放弃重建：原实现会把 null 交给 gen_region_polygon，
                        // 抛出的是与真实原因（多边形从未绘制 / 反序列化没带上点集）无关的 HALCON 原生异常。
                        if (hRegion.PolygonX == null || hRegion.PolygonY == null)
                        {
                            Log.Warn(nameof(RegionExtension), "多边形点集为空，跳过区域重建。");
                            return;
                        }
                        HOperatorSet.GenRegionPolygon(out HObject region, hRegion.PolygonX, hRegion.PolygonY);
                        ReplaceHandle(hRegion, region);
                    }
                    break;
                case RectEnum.Ring:
                    {
                        // 直接以 GenCircle 创建句柄：原实现先 GenEmptyObj 再被 GenCircle 覆盖，空对象句柄永不释放
                        HOperatorSet.GenCircle(out HObject circle1, hRegion.CenterY, hRegion.CenterX, hRegion.MaxRadius);
                        try
                        {
                            HOperatorSet.GenCircle(out HObject circle2, hRegion.CenterY, hRegion.CenterX, hRegion.MinRadius);
                            try
                            {
                                HOperatorSet.Difference(circle1, circle2, out HObject region);
                                ReplaceHandle(hRegion, region);
                            }
                            finally
                            {
                                circle2.Dispose();
                            }
                        }
                        finally
                        {
                            circle1.Dispose();
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// 以各坐标为中心、使用当前区域的宽高生成矩形，并合并到现有 Halcon 区域
        /// </summary>
        /// <param name="hRegion">要合并矩形的区域</param>
        /// <param name="coords">矩形的中心坐标集合</param>
        public static void GenCoordsRegion(this CvRegion hRegion, List<CvCoord> coords)
        {
            // hRegion 判空与 coords 同等对待：原实现只判了 coords，
            // 传 null 区域进来会在 hRegion.Height 处 NRE。
            if (hRegion == null || coords == null) return;
            // 已释放（HoRegion 为 null）的区域没法参与 union2，直接放弃而不是在算子里炸。
            if (hRegion.HoRegion == null) return;

            HObject imgReduced; HOperatorSet.GenEmptyObj(out imgReduced);

            try
            {
                for (int i = 0; i < coords.Count; i++)
                {
                    HTuple row1 = coords[i].Y - hRegion.Height / 2;
                    HTuple column1 = coords[i].X - hRegion.Width / 2;
                    HTuple row2 = coords[i].Y + hRegion.Height / 2;
                    HTuple column2 = coords[i].X + hRegion.Width / 2;

                    imgReduced.Dispose();
                    HOperatorSet.GenRectangle1(out imgReduced, row1, column1, row2, column2);
                    HOperatorSet.Union2(hRegion.HoRegion, imgReduced, out HObject regionUnion);
                    ReplaceHandle(hRegion, regionUnion);
                }
            }
            finally
            {
                imgReduced.Dispose();
            }
        }

        /// <summary>
        /// 设置区域中心点，并保持当前宽高不变
        /// </summary>
        /// <param name="center">新的中心点</param>
        public static void SetCenter(this CvRegion hRegion, Point2d center)
        {
            Point2d location = new Point2d(center.X - hRegion.Width / 2, center.Y - hRegion.Height / 2);
            hRegion.Bounds = new Rect2d(location, hRegion.Size);
        }

        /// <summary>
        /// 通过中心点和尺寸设置区域矩形
        /// </summary>
        /// <param name="center">中心点</param>
        /// <param name="size">矩形尺寸</param>
        public static void SetRectByCenter(this CvRegion hRegion, Point2d center, Size2d size)
        {
            Point2d TopLeft = new Point2d(center.X - size.Width / 2, center.Y - size.Height / 2);
            var rect = new Rect2d(TopLeft, size);
            hRegion.Bounds = rect;
        }

        /// <summary>
        /// 通过左上角和尺寸设置区域矩形
        /// </summary>
        /// <param name="topLeft">左上角</param>
        /// <param name="size">矩形尺寸</param>
        public static void SetRectByTopLeft(this CvRegion hRegion, Point2d topLeft, Size2d size)
        {
            var rect = new Rect2d(topLeft, size);
            hRegion.Bounds = rect;
        }
      
        /// <summary>
        /// 通过左上角和右下角设置区域矩形
        /// </summary>
        /// <param name="topLeft">左上角</param>
        /// <param name="bottomRight">右下角</param>
        public static void SetRectByCorners(this CvRegion hRegion, Point2d topLeft, Point2d bottomRight)
        {
            var rect = Rect2d.FromLTRB(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
            hRegion.Bounds = rect;
        }

        /// <summary>
        /// 通过左上角坐标和宽高设置区域矩形
        /// </summary>
        /// <param name="x">左上角 X 坐标</param>
        /// <param name="y">左上角 Y 坐标</param>
        /// <param name="width">矩形宽度</param>
        /// <param name="height">矩形高度</param>
        public static void SetRect(this CvRegion hRegion, double x, double y, double width, double height)
        {
            var rect = new Rect2d(x, y, width, height);
            hRegion.Bounds = rect;
        }

        /// <summary>
        /// 通过 Halcon 左上角和右下角的行列坐标设置区域矩形
        /// </summary>
        /// <param name="row1">左上角行坐标</param>
        /// <param name="column1">左上角列坐标</param>
        /// <param name="row2">右下角行坐标</param>
        /// <param name="column2">右下角列坐标</param>
        public static void SetRectByCorners(this CvRegion hRegion, HTuple row1, HTuple column1, HTuple row2, HTuple column2)
        {
            var rect = new Rect2d(row1, column1, row2, column2);
            hRegion.Bounds = rect;
        }

        /// <summary>
        /// 从指定区域复制位置、尺寸和 Halcon 区域对象
        /// </summary>
        /// <param name="hRegion">要更新的目标区域</param>
        /// <param name="inRegion">提供数据的源区域</param>
        public static void CopyFrom(this CvRegion hRegion, CvRegion inRegion)
        {
            if (hRegion == null || inRegion == null) return;
            if (ReferenceEquals(hRegion, inRegion)) return; // 自拷贝：无需换句柄

            hRegion.Bounds = inRegion.Bounds;

            // Dispose() 后 HoRegion 会置 null，这里补空并保持"HoRegion 非空"的不变式
            HObject cloned;
            if (inRegion.HoRegion.NotNull()) cloned = inRegion.HoRegion.Clone();
            else HOperatorSet.GenEmptyObj(out cloned);

            ReplaceHandle(hRegion, cloned);
        }

    }
}
