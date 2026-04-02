using DotNet.HWindows;
using HalconDotNet;
using OpenCvSharp;
using System.Collections.Generic;


namespace DotNet.VisionMaster
{
    public enum HContextType
    {
        None,
        Rectangle,
        Rectangle2,
        Circle,
        Ellipse,
        Polygon,
        Ring
    }

    public class HObjContext : Rect2d
    {

        /// <summary> 角度 </summary>
        public HTuple Phi { set; get; } = 0;

        /// <summary> 最大半径 </summary>
        public double MaxRadius { set; get; } = 300;

        /// <summary> 最小半径 </summary>
        public double MinRadius { set; get; } = 100;

        /// <summary> 区域 </summary>
        public HObject HoRect;

        /// <summary> 中心 </summary>
        public Point2d Center => new Point2d((TopLeft.X + BottomRight.X) / 2, (TopLeft.Y + BottomRight.Y) / 2);

        /// <summary> 多边形点集合 </summary>
        public List<Point2d> Polygons { get; set; } = new List<Point2d>();

        /// <summary> 类型 </summary>
        public HContextType Type { set; get; } = HContextType.Rectangle;

        public HObjContext()
        {
            HOperatorSet.GenEmptyObj(out HoRect);
        }

    }

    public static class HContextExtension
    {
        /// <summary>
        /// 获取区域
        /// </summary>
        public static void GenRegion(this HObjContext hRegion)
        {
            if (hRegion == null) return;
            HOperatorSet.GenEmptyObj(out hRegion.HoRect);
            switch (hRegion.Type)
            {
                case HContextType.Rectangle:
                    {
                        HOperatorSet.GenRectangle1(out hRegion.HoRect, hRegion.TopLeft.Y, hRegion.TopLeft.X,
                                               hRegion.BottomRight.Y, hRegion.BottomRight.X);
                    }
                    break;
                case HContextType.Rectangle2:
                    {
                        HOperatorSet.GenRectangle2(out hRegion.HoRect, hRegion.CentreY, hRegion.CentreX, hRegion.Phi,
                                               hRegion.Width / 2, hRegion.Height / 2);
                    }
                    break;
                case HContextType.Circle:
                    {
                        HOperatorSet.GenCircle(out hRegion.HoRect, hRegion.CentreY, hRegion.CentreX, hRegion.Width / 2);
                    }
                    break;
                case HContextType.Ellipse:
                    {
                        HOperatorSet.GenEllipse(out hRegion.HoRect, hRegion.CentreY, hRegion.CentreX, hRegion.Phi,
                                               hRegion.Width / 2, hRegion.Height / 2);
                    }
                    break;
                case HContextType.Polygon:
                    {
                        //HOperatorSet.GenRegionPolygon(out hRegion.HoRect, hRegion.PolygonX, hRegion.PolygonY);
                    }
                    break;
                case HContextType.Ring:
                    {
                        HObject circle1 = new HObject(); HOperatorSet.GenEmptyObj(out circle1);
                        HObject circle2 = new HObject(); HOperatorSet.GenEmptyObj(out circle2);
                        HOperatorSet.GenCircle(out circle1, hRegion.CentreY, hRegion.CentreX, hRegion.MaxRadius);
                        HOperatorSet.GenCircle(out circle2, hRegion.CentreY, hRegion.CentreX, hRegion.MinRadius);
                        HOperatorSet.Difference(circle1, circle2, out hRegion.HoRect);
                        circle1.Dispose();
                        circle2.Dispose();
                    }
                    break;
            }
        }

        /// <summary>
        /// 获取坐标区域
        /// </summary>
        public static void GenCoordsRegion(this HObjContext hRegion, List<CvCoord> coords)
        {
            if (coords == null) return;

            HObject imgReduced = new HObject();
            HOperatorSet.GenEmptyObj(out hRegion.HoRect);

            for (int i = 0; i < coords.Count; i++)
            {
                HTuple row1 = coords[i].Y - hRegion.Height / 2;
                HTuple column1 = coords[i].X - hRegion.Width / 2;
                HTuple row2 = coords[i].Y + hRegion.Height / 2;
                HTuple column2 = coords[i].X + hRegion.Width / 2;

                HOperatorSet.GenEmptyObj(out imgReduced);
                HOperatorSet.GenRectangle1(out imgReduced, row1, column1, row2, column2);
                HOperatorSet.Union2(hRegion.HoRect, imgReduced, out hRegion.HoRect);
            }

            imgReduced.Dispose();
        }

        /// <summary>
        /// 通过中心点和宽高修改橡皮筋参数
        /// </summary>
        /// <param name="centre">中心点</param>
        /// <param name="size">宽高</param>
        public static void ReCentre(this HObjContext hRegion, Point2d centre)
        {
            Point2d location = new Point2d(centre.X - hRegion.Width / 2, centre.Y - hRegion.Height / 2);
            hRegion.X = location.X;
            hRegion.Y = location.Y;
        }

        /// <summary>
        /// 通过中心点和宽高修改橡皮筋参数
        /// </summary>
        /// <param name="centre">中心点</param>
        /// <param name="size">宽高</param>
        public static void ReCentre(this HObjContext hRegion, Point2d centre, Size2d size)
        {
            Point2d TopLeft = new Point2d(centre.X - size.Width / 2, centre.Y - size.Height / 2);
            var rect = new Rect2d(TopLeft, size);
            hRegion.X = rect.X;
            hRegion.Y = rect.Y;
            hRegion.Width = rect.Width;
            hRegion.Height = rect.Height;
        }

        /// <summary>
        /// 通过左上点和宽高修改橡皮筋参数
        /// </summary>
        /// <param name="topLeft">左上点</param>
        /// <param name="size">大小</param>
        public static void ReTopLeft(this HObjContext hRegion, Point2d topLeft, Size2d size)
        {
            var rect = new Rect2d(topLeft, size);
            hRegion.X = rect.X;
            hRegion.Y = rect.Y;
            hRegion.Width = rect.Width;
            hRegion.Height = rect.Height;
        }

        /// <summary>
        /// 通过左上点和右下点修改橡皮筋参数
        /// </summary>
        /// <param name="topLeft">左上点</param>
        /// <param name="bottomRight">右下点</param>
        public static void Re2Point(this HObjContext hRegion, Point2d topLeft, Point2d bottomRight)
        {
            var rect = Rect2d.FromLTRB(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y);
            hRegion.X = rect.X;
            hRegion.Y = rect.Y;
            hRegion.Width = rect.Width;
            hRegion.Height = rect.Height;
        }

        /// <summary>
        /// 通过左上点和右下点修改橡皮筋参数
        /// </summary>
        /// <param name="x">左</param>
        /// <param name="y">上</param>
        /// <param name="width">宽</param>
        /// <param name="height">高</param>
        public static void Re2Point(this HObjContext hRegion, double x, double y, double width, double height)
        {
            var rect = new Rect2d(x, y, width, height);
            hRegion.X = rect.X;
            hRegion.Y = rect.Y;
            hRegion.Width = rect.Width;
            hRegion.Height = rect.Height;
        }

        /// <summary>
        /// 通过左上点和右下点修改橡皮筋参数
        /// </summary>
        public static void Re2Point(this HObjContext hRegion, HTuple row1, HTuple column1, HTuple row2, HTuple column2)
        {
            var rect = new Rect2d(row1, column1, row2, column2);
            hRegion.X = rect.X;
            hRegion.Y = rect.Y;
            hRegion.Width = rect.Width;
            hRegion.Height = rect.Height;
        }

        /// <summary>
        /// 通过左上点和右下点修改橡皮筋参数
        /// </summary>
        public static void UpdateDRegion(this HObjContext cvRegion, CvRegion InRegion)
        {
            cvRegion.X = InRegion.X;
            cvRegion.Y = InRegion.Y;
            cvRegion.Width = InRegion.Width;
            cvRegion.Height = InRegion.Height;
            cvRegion.HoRect = InRegion.HoRegion.Clone();
        }

    }
}
