using HalconDotNet;
using OpenCvSharp;
using System.Collections.Generic;


namespace DotNet.HWindows
{
    public static class RegionExtension
    {
        /// <summary>
        /// 获取区域
        /// </summary>
        public static void GenRegion(this CvRegion hRegion)
        {
            if (hRegion == null) return;
            HOperatorSet.GenEmptyObj(out hRegion.InRegion);
            switch (hRegion.Type)
            {
                case DrawForm.矩形:
                    {
                        HOperatorSet.GenRectangle1(out hRegion.InRegion, hRegion.TopLeft.Y, hRegion.TopLeft.X,
                                               hRegion.BottomRight.Y, hRegion.BottomRight.X);
                    }
                    break;
                case DrawForm.仿射矩形:
                    {
                        HOperatorSet.GenRectangle2(out hRegion.InRegion, hRegion.CentreY, hRegion.CentreX, hRegion.Phi,
                                               hRegion.Width / 2, hRegion.Height / 2);
                    }
                    break;
                case DrawForm.圆:
                    {
                        HOperatorSet.GenCircle(out hRegion.InRegion, hRegion.CentreY, hRegion.CentreX, hRegion.Width / 2);
                    }
                    break;
                case DrawForm.椭圆:
                    {
                        HOperatorSet.GenEllipse(out hRegion.InRegion, hRegion.CentreY, hRegion.CentreX, hRegion.Phi,
                                               hRegion.Width / 2, hRegion.Height / 2);
                    }
                    break;
                case DrawForm.多边型:
                    {
                        HOperatorSet.GenRegionPolygon(out hRegion.InRegion, hRegion.PolygonX, hRegion.PolygonY);
                    }
                    break;
                case DrawForm.圆环:
                    {
                        HObject circle1 = new HObject(); HOperatorSet.GenEmptyObj(out circle1);
                        HObject circle2 = new HObject(); HOperatorSet.GenEmptyObj(out circle2);
                        HOperatorSet.GenCircle(out circle1, hRegion.CentreY, hRegion.CentreX, hRegion.MaxRadius);
                        HOperatorSet.GenCircle(out circle2, hRegion.CentreY, hRegion.CentreX, hRegion.MinRadius);
                        HOperatorSet.Difference(circle1, circle2, out hRegion.InRegion);
                        circle1.Dispose();
                        circle2.Dispose();
                    }
                    break;
            }
        }

        /// <summary>
        /// 获取坐标区域
        /// </summary>
        public static void GenCoordsRegion(this CvRegion hRegion, List<CvCoord> coords)
        {
            if (coords == null) return;

            HObject imgReduced = new HObject(); 
            HOperatorSet.GenEmptyObj(out hRegion.InRegion);

            for (int i = 0; i < coords.Count; i++)
            {
                HTuple row1 = coords[i].Y - hRegion.Height / 2;
                HTuple column1 = coords[i].X - hRegion.Width / 2;
                HTuple row2 = coords[i].Y + hRegion.Height / 2;
                HTuple column2 = coords[i].X + hRegion.Width / 2;

                HOperatorSet.GenEmptyObj(out imgReduced);
                HOperatorSet.GenRectangle1(out imgReduced, row1, column1, row2, column2);
                HOperatorSet.Union2(hRegion.HoRegion, imgReduced, out hRegion.InRegion);
            }

            imgReduced.Dispose();
        }

        /// <summary>
        /// 通过中心点和宽高修改橡皮筋参数
        /// </summary>
        /// <param name="centre">中心点</param>
        /// <param name="size">宽高</param>
        public static void UpdateCentre(this CvRegion hRegion, Point2d centre)
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
        public static void UpdateCentre(this CvRegion hRegion, Point2d centre, Size2d size)
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
        public static void UpdateTopLeft(this CvRegion hRegion, Point2d topLeft, Size2d size)
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
        public static void Update2Point(this CvRegion hRegion, Point2d topLeft, Point2d bottomRight)
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
        public static void Update2Point(this CvRegion hRegion, double x, double y, double width, double height)
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
        public static void Update2Point(this CvRegion hRegion, HTuple row1, HTuple column1, HTuple row2, HTuple column2)
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
        public static void UpdateDRegion(this CvRegion cvRegion, CvRegion InRegion)
        {
            cvRegion.X = InRegion.X;
            cvRegion.Y = InRegion.Y;
            cvRegion.Width = InRegion.Width;
            cvRegion.Height = InRegion.Height;
            cvRegion.HoRegion = InRegion.HoRegion.Clone();
        }

    }
}
