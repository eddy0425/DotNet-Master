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
        DrawContext context;
        DisplayForm display => context.display;

        public bool NeedReDispImage => true;

        public void SetUp(DrawContext _context)
        {
            context = _context;
            if (context.SetUp == SetUpEnum.None)
            {
                if (context.Polygons.Count == 0)
                {
                    GetShapeModelPoints(out double[] rowPoints, out double[] columnPoints);
                    context.Polygons = HalconHelper.GetPolygons(rowPoints, columnPoints);
                    display.DispPoint(rowPoints, columnPoints, HColor.Green);
                }
                else
                {
                    display.DispPoint(context.Polygons, HColor.Green);
                }

                context.SetUp = SetUpEnum.Step1;
            }
        }

        public void OnMouseDown(DrawContext context, HMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (context.SetUp == SetUpEnum.Step1)
                {
                    if (context.CycleMove == CycleMoveEnum.Start)
                    {
                        // 开始移动选中的顶点
                        context.CycleMove = CycleMoveEnum.StartMove;
                    }
                }
            }
        }

        public void OnMouseUp(DrawContext context, HMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (context.SetUp == SetUpEnum.Step1)
                {
                    if (context.CycleMove == CycleMoveEnum.StartMove)
                    {
                        // 结束顶点移动
                        context.CycleMove = CycleMoveEnum.None;
                    }
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (context.SetUp == SetUpEnum.Step1)
                {
                    // 右键完成编辑
                    context.HoContour = HalconHelper.GenContours(context.Polygons);
                    context.DrawPolygon(context.HoContour);
                    context.SetUp = SetUpEnum.Step2;
                }
            }
        }

        public void OnMouseWheel(DrawContext context, HMouseEventArgs e)
        {
            // 滚轮事件仅触发重绘
        }

        public void OnMouseMove(DrawContext context, HMouseEventArgs e)
        {
            OnReDisplay(context);

            switch (context.SetUp)
            {
                case SetUpEnum.Step1:
                    {
                        if (context.CycleMove == CycleMoveEnum.StartMove)
                        {
                            // 移动选中的顶点
                            if (SelectIndex >= 0 && SelectIndex < context.Polygons.Count)
                            {
                                context.Polygons[SelectIndex] = new Point2d(e.X, e.Y);
                            }
                        }
                        else
                        {
                            // 检测是否靠近某个顶点
                            for (int i = 0; i < context.Polygons.Count; i++)
                            {
                                if (HalconHelper.IsNearPoint(context.Polygons[i].X, context.Polygons[i].Y, e.X, e.Y))
                                {
                                    // 高亮显示靠近的顶点
                                    display.DispPoint(context.Polygons[i], HColor.Red);
                                    SelectIndex = i;
                                    context.CycleMove = CycleMoveEnum.Start;
                                    break;
                                }
                            }
                        }
                    }
                    break;
            }
        }

        public void OnReDisplay(DrawContext context)
        {
            switch (context.SetUp)
            {
                case SetUpEnum.Step1:
                    {
                        display.DispPoint(context.Polygons, HColor.Green);

                        // 绘制多边形边
                        for (int i = 0; i < context.Polygons.Count; i++)
                        {
                            if (i < context.Polygons.Count - 1)
                            {
                                display.DispLine(new CvLine(context.Polygons[i].X, context.Polygons[i].Y,
                                               context.Polygons[i + 1].X, context.Polygons[i + 1].Y), HColor.Red);
                            }
                        }

                        // 绘制闭合线
                        if (context.Polygons.Count > 2)
                        {
                            display.DispLine(new CvLine(context.Polygons[0].X, context.Polygons[0].Y,
                                           context.Polygons[context.Polygons.Count - 1].X,
                                           context.Polygons[context.Polygons.Count - 1].Y), HColor.Red);
                        }
                    }
                    break;
                case SetUpEnum.Step2:
                    {
                        // 显示模型相关区域
                        display.DispRegion(context.HoRegion, HColor.Blue);
                        display.DispRegion(context.HoContour, HColor.Green);

                        if (context.Center != null)
                        {
                            display.DispPoint(context.Center, HColor.Yellow);
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
