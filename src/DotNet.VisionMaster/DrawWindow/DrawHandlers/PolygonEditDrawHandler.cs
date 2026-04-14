using DotNet.Drawing;
using DotNet.HalconUI;
using HalconDotNet;
using System.Windows.Forms;

namespace DotNet.VisionMaster
{
    /// <summary>
    /// 多边形绘图处理器2
    /// 用于编辑已有的多边形轮廓（拖动顶点）
    /// </summary>
    public class PolygonEditDrawHandler : IDrawHandler
    {
        int SelectIndex = 0; //选中的索引

        public bool NeedReDisp => true;

        public void SetUp(DisplayUI display)
        {
            if (display.ShrSetUp == SetUpEnum.None)
            {
                if (display.ShrPolygons.Count == 0)
                {
                    GetShapeModelPoints(out double[] rowPoints, out double[] columnPoints);
                    display.ShrPolygons = HalconHelper.GetPolygons(rowPoints, columnPoints);
                    display.DispPoint(rowPoints, columnPoints, HColor.Green);
                }
                else
                {
                    display.DispPoint(display.ShrPolygons, HColor.Green);
                }

                display.ShrSetUp = SetUpEnum.Step1;
            }
        }

        public void OnMouseDown(DisplayUI display, HMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (display.ShrSetUp == SetUpEnum.Step1)
                {
                    if (display.ShrCycleMove == CycleMoveEnum.Start)
                    {
                        // 开始移动选中的顶点
                        display.ShrCycleMove = CycleMoveEnum.StartMove;
                    }
                }
            }
        }

        public void OnMouseUp(DisplayUI display, HMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (display.ShrSetUp == SetUpEnum.Step1)
                {
                    if (display.ShrCycleMove == CycleMoveEnum.StartMove)
                    {
                        // 结束顶点移动
                        display.ShrCycleMove = CycleMoveEnum.None;
                    }
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (display.ShrSetUp == SetUpEnum.Step1)
                {
                    // 右键完成编辑
                    display.ShrContour = HalconHelper.GenContours(display.ShrPolygons);
                    display.DrawPolygon(display.ShrContour);
                    display.ShrSetUp = SetUpEnum.Step2;
                }
            }
        }

        public void OnMouseWheel(DisplayUI display, HMouseEventArgs e)
        {
            // 滚轮事件仅触发重绘
        }

        public void OnMouseMove(DisplayUI display, HMouseEventArgs e)
        {
            OnReDisplay(display);

            switch (display.ShrSetUp)
            {
                case SetUpEnum.Step1:
                    {
                        if (display.ShrCycleMove == CycleMoveEnum.StartMove)
                        {
                            // 移动选中的顶点
                            if (SelectIndex >= 0 && SelectIndex < display.ShrPolygons.Count)
                            {
                                display.ShrPolygons[SelectIndex] = new Point2d(e.X, e.Y);
                            }
                        }
                        else
                        {
                            // 检测是否靠近某个顶点
                            for (int i = 0; i < display.ShrPolygons.Count; i++)
                            {
                                if (HalconHelper.IsNearPoint(display.ShrPolygons[i].X, display.ShrPolygons[i].Y, e.X, e.Y))
                                {
                                    // 高亮显示靠近的顶点
                                    display.DispPoint(display.ShrPolygons[i], HColor.Red);
                                    SelectIndex = i;
                                    display.ShrCycleMove = CycleMoveEnum.Start;
                                    break;
                                }
                            }
                        }
                    }
                    break;
            }
        }

        public void OnReDisplay(DisplayUI display)
        {
            switch (display.ShrSetUp)
            {
                case SetUpEnum.Step1:
                    {
                        display.DispPoint(display.ShrPolygons, HColor.Green);

                        // 绘制多边形边
                        for (int i = 0; i < display.ShrPolygons.Count; i++)
                        {
                            if (i < display.ShrPolygons.Count - 1)
                            {
                                display.DispLine(new CvLine(display.ShrPolygons[i].X, display.ShrPolygons[i].Y,
                                               display.ShrPolygons[i + 1].X, display.ShrPolygons[i + 1].Y), HColor.Red);
                            }
                        }

                        // 绘制闭合线
                        if (display.ShrPolygons.Count > 2)
                        {
                            display.DispLine(new CvLine(display.ShrPolygons[0].X, display.ShrPolygons[0].Y,
                                           display.ShrPolygons[display.ShrPolygons.Count - 1].X,
                                           display.ShrPolygons[display.ShrPolygons.Count - 1].Y), HColor.Red);
                        }
                    }
                    break;
                case SetUpEnum.Step2:
                    {
                        // 显示模型相关区域
                        display.DispRegion(display.ShrRegion, HColor.Blue);
                        display.DispRegion(display.ShrContour, HColor.Green);

                        if (display.ShrCenter != null)
                        {
                            display.DispPoint(display.ShrCenter, HColor.Yellow);
                        }
                    }
                    break;
            }
        }

        private void GetShapeModelPoints(out double[] rowPoints, out double[] columnPoints)
        {
            rowPoints = new double[0];
            columnPoints = new double[0];
            HObject ho_ShapeModel; HOperatorSet.GenEmptyObj(out ho_ShapeModel);
            HObject ho_Contour; HOperatorSet.GenEmptyObj(out ho_Contour);

            //try
            //{
            //    // 获取 Row 坐标
            //    HOperatorSet.GetGenericShapeModelResult(HalconHelper.hv_MatchResultID, "all", "row", out HTuple hv_Row);
            //    // 获取 Column 坐标  
            //    HOperatorSet.GetGenericShapeModelResult(HalconHelper.hv_MatchResultID, "all", "column", out HTuple hv_Column);

            //    // 获取模板轮廓信息
            //    HOperatorSet.GetShapeModelContours(out ho_ShapeModel, HalconHelper.hv_ModelID, 1);

            //    // 仿射变换轮廓到匹配位置
            //    ho_Contour.Dispose();
            //    HalconHelper.AffineTransContourXld(0, 0, 0, hv_Row, hv_Column, 0, ho_ShapeModel, out ho_Contour);

            //    ExtractContourPoints(ho_Contour, out rowPoints, out columnPoints);
            //}
            //finally
            //{
            //    ho_Contour.Dispose();
            //    ho_ShapeModel.Dispose();
            //}
        }

        /// <summary>
        /// 提取 XLD 轮廓中点数最多的轮廓坐标（使用多边形近似减少点数）
        /// </summary>
        /// <param name="ho_Contour">输入轮廓</param>
        /// <param name="rowPoints">输出点数最多的轮廓行坐标</param>
        /// <param name="columnPoints">输出点数最多的轮廓列坐标</param>
        /// <param name="alpha">多边形近似的阈值（像素），值越大点越少，默认2.0 (1.0 - 5.0)</param>
        private void ExtractContourPoints(HObject contour, out double[] rowPoints, out double[] columnPoints, double alpha = 1.0)
        {
            rowPoints = new double[0];
            columnPoints = new double[0];

            HObject ho_Polygons = new HObject();
            HOperatorSet.GenEmptyObj(out ho_Polygons);

            try
            {
                // 1. 使用 Ramer 算法将轮廓近似为多边形，减少点数
                ho_Polygons.Dispose();
                HOperatorSet.GenPolygonsXld(contour, out ho_Polygons, "ramer", alpha);

                // 2. 获取多边形对象的数量
                HOperatorSet.CountObj(ho_Polygons, out HTuple hv_PolygonCount);

                int maxPointCount = 0;

                // 3. 遍历每个多边形，找到点数最多的那个
                for (int i = 1; i <= hv_PolygonCount.I; i++)
                {
                    HObject ho_SinglePolygon;
                    HOperatorSet.SelectObj(ho_Polygons, out ho_SinglePolygon, i);

                    // 获取多边形的顶点坐标
                    HOperatorSet.GetPolygonXld(ho_SinglePolygon, out HTuple hv_Rows, out HTuple hv_Columns,
                                               out HTuple hv_Length, out HTuple hv_Phi);

                    double[] rows = hv_Rows.ToDArr();
                    double[] columns = hv_Columns.ToDArr();

                    // 筛选点数最多的轮廓
                    if (rows.Length > maxPointCount)
                    {
                        maxPointCount = rows.Length;
                        rowPoints = rows;
                        columnPoints = columns;
                    }

                    // 调试输出
                    System.Diagnostics.Debug.WriteLine($"轮廓 {i}: 多边形近似后包含 {rows.Length} 个顶点");

                    ho_SinglePolygon.Dispose();
                }

                System.Diagnostics.Debug.WriteLine($"选中点数最多的轮廓，包含 {maxPointCount} 个顶点");
            }
            finally
            {
                ho_Polygons.Dispose();
            }
        }



    }
}
