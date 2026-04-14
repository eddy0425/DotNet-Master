using DotNet.Drawing;
using DotNet.HalconUI;
using HalconDotNet;
using System.Windows.Forms;

namespace DotNet.VisionMaster
{
    /// <summary>
    /// 合成绘图处理器
    /// 用于绘制和编辑合成区域
    /// </summary>
    public class SynthethicDrawHandler : IDrawHandler
    {
        public bool NeedReDisp => true;

        public double LeftLong = 1.425;   //mm
        public double MediumLong = 1.15;  //mm
        public double RightLong = 1.425;  //mm
        public double TopLong = 2.85;     //mm
        public double Thickness = 0.4;   //mm 锡膏厚度
        public double Pixel = 13.8;       //um/pix

        private double scaling = 1;
        private double CalLeftLong => (LeftLong * 1000) / Pixel;          //pix
        private double CalMediumLong => (MediumLong * 1000) / Pixel;     //pix
        private double CalRightLong => (RightLong * 1000) / Pixel;       //pix
        private double CalTopLong => (TopLong * 1000) / Pixel;           //pix
        private double CalThickness => (Thickness * 1000) / Pixel;       //pix 锡膏厚度像素值

        Point2d RegTopLeft;
        Point2d RegBottomRight;
        Point2d RegCenter => new Point2d((RegTopLeft.X + RegBottomRight.X) / 2, (RegTopLeft.Y + RegBottomRight.Y) / 2);

        public void SetUp(DisplayUI display)
        {
            if (display.ShrSetUp == SetUpEnum.None)
            {
                display.ShrContour = DrawSynthethic(display, out RegTopLeft, out RegBottomRight);
                display.DispRegion(display.ShrContour, HColor.Green);

                display.DispPoint(RegTopLeft, HColor.OrangeRed, 100);
                display.DispPoint(RegBottomRight, HColor.OrangeRed, 100);
                display.DispPoint(RegCenter, HColor.OrangeRed, 100);

                display.ShrSetUp = SetUpEnum.Step1;
            }
        }

        public void OnMouseDown(DisplayUI display, HMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (display.ShrSetUp == SetUpEnum.Step1)
                {
                    switch (display.ShrCycleMove)
                    {
                        // 开始移动选中的顶点
                        case CycleMoveEnum.Start:
                            display.ShrCycleMove = CycleMoveEnum.StartMove;
                            break;
                        case CycleMoveEnum.End:
                            display.ShrCycleMove = CycleMoveEnum.EndMove;
                            break;
                        case CycleMoveEnum.Center:
                            display.ShrCycleMove = CycleMoveEnum.CenterMove;
                            break;
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
                    switch (display.ShrCycleMove)
                    {
                        // 结束顶点移动
                        case CycleMoveEnum.StartMove:
                        case CycleMoveEnum.EndMove:
                        case CycleMoveEnum.CenterMove:
                            display.ShrCycleMove = CycleMoveEnum.None;
                            break;
                    }
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (display.ShrSetUp == SetUpEnum.Step1)
                {
                    // 右键完成编辑
                    display.DrawSynthethicn(display.ShrContour, RegTopLeft, RegBottomRight);
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
                        switch (display.ShrCycleMove)
                        {
                            case CycleMoveEnum.StartMove:
                                RegTopLeft = new Point2d(e.X, e.Y);
                                display.ShrContour = newTopLeftSynthethic(display, new Point2d(e.X, e.Y), out scaling);
                                break;
                            case CycleMoveEnum.EndMove:
                                RegBottomRight = new Point2d(e.X, e.Y);
                                display.ShrContour = newBottomRightSynthethic(display, new Point2d(e.X, e.Y), out scaling);
                                break;
                            case CycleMoveEnum.CenterMove:
                                {
                                    var calX = RegCenter.X - e.X;
                                    var calY = RegCenter.Y - e.Y;
                                    RegTopLeft = new Point2d(RegTopLeft.X - calX, RegTopLeft.Y - calY);
                                    RegBottomRight = new Point2d(RegBottomRight.X - calX, RegBottomRight.Y - calY);
                                    display.ShrContour = newCenterSynthethic(display, RegCenter, out scaling);
                                }
                                break;
                            default:
                                {
                                    if (HalconHelper.IsNearPoint(RegTopLeft.X, RegTopLeft.Y, e.X, e.Y))
                                    {
                                        // 高亮显示靠近的顶点
                                        display.DispPoint(RegTopLeft, HColor.Yellow);
                                        display.ShrCycleMove = CycleMoveEnum.Start;
                                        break;
                                    }

                                    if (HalconHelper.IsNearPoint(RegBottomRight.X, RegBottomRight.Y, e.X, e.Y))
                                    {
                                        // 高亮显示靠近的顶点
                                        display.DispPoint(RegBottomRight, HColor.Yellow);
                                        display.ShrCycleMove = CycleMoveEnum.End;
                                        break;
                                    }

                                    if (HalconHelper.IsNearPoint(RegCenter.X, RegCenter.Y, e.X, e.Y))
                                    {
                                        // 高亮显示靠近的顶点
                                        display.DispPoint(RegCenter, HColor.Yellow);
                                        display.ShrCycleMove = CycleMoveEnum.Center;
                                        break;
                                    }
                                }
                                break;
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
                        display.DispPoint(RegTopLeft, HColor.OrangeRed, 100);
                        display.DispPoint(RegBottomRight, HColor.OrangeRed, 100);
                        display.DispPoint(RegCenter, HColor.OrangeRed, 100);
                        display.DispRegion(display.ShrContour, HColor.Green);
                    }
                    break;
                case SetUpEnum.Step2:
                    {
                        display.DispPoint(RegTopLeft, HColor.OrangeRed, 100);
                        display.DispPoint(RegBottomRight, HColor.OrangeRed, 100);
                        display.DispPoint(RegCenter, HColor.OrangeRed, 100);

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


        /// <summary>
        /// 绘制合成图形并输出左上角和右下角坐标
        /// </summary>
        /// <param name="context">绘图上下文</param>
        /// <param name="TopLeft">输出：左上角坐标（左上方矩形的最上边中心点，即点5）</param>
        /// <param name="BottomRight">输出：右下角坐标（右下方矩形的最下边中心点，即点3）</param>
        /// <returns>合成图形轮廓对象</returns>
        private HObject DrawSynthethic(DisplayUI display, out Point2d TopLeft, out Point2d BottomRight)
        {
            HObject ho_Contour; HOperatorSet.GenEmptyObj(out ho_Contour);
            HObject ho_Region1 = null, ho_Region2 = null, ho_Region3 = null, ho_Region4 = null, ho_Region5 = null;
            HObject ho_UnionRegion = null;

            // 初始化输出参数
            TopLeft = new Point2d(0, 0);
            BottomRight = new Point2d(0, 0);

            double centerRow = 0;
            double centerCol = 0;

            try
            {
                if (display.ShrCenter != null)
                {
                    centerRow = display.ShrCenter.Y;
                    centerCol = display.ShrCenter.X;
                }
                else
                {
                    // 获取源图像尺寸
                    centerRow = display.HoHeight / 2;
                    centerCol = display.HoWidth / 2;
                }

                // 计算像素值
                double halfMedium = CalMediumLong / 2;  // 中间水平线的一半
                double halfTop = CalTopLong / 2;        // 垂直高度的一半
                double leftLong = CalLeftLong;          // 左边斜线的水平投影
                double rightLong = CalRightLong;        // 右边斜线的水平投影
                double halfThickness = CalThickness / 2; // 锡膏厚度的一半

                // 计算各点坐标 (Row, Col)
                // 图片标注: 0/E是中心, 1是右中, 2是右上, 3是右下, 4是左中, 5是左上, 6是左下
                // 注意：HALCON中Row向下为正，所以"上"对应Row减小，"下"对应Row增大
                double row1 = centerRow, col1 = centerCol + halfMedium;                          // 点1 (中心右)
                double row4 = centerRow, col4 = centerCol - halfMedium;                          // 点4 (中心左)
                double row2 = centerRow - halfTop, col2 = centerCol + halfMedium + rightLong;    // 点2 (右上)
                double row3 = centerRow + halfTop, col3 = centerCol + halfMedium + rightLong;    // 点3 (右下)
                double row5 = centerRow - halfTop, col5 = centerCol - halfMedium - leftLong;     // 点5 (左上)
                double row6 = centerRow + halfTop, col6 = centerCol - halfMedium - leftLong;     // 点6 (左下)

                // 获取左上角和右下角坐标
                // 左上角：左上方矩形的最上边中心点（点5）
                // 右下角：右下方矩形的最下边中心点（点3）
                TopLeft = new Point2d(col5, row5);      // Point2d(X, Y) = Point2d(Col, Row)
                BottomRight = new Point2d(col3, row3);

                // 生成 5 个矩形区域，然后合并成一个整体
                // 线段1: 点5 -> 点4 (左上到中心左) - 斜线
                ho_Region1 = GenRectangle2Region(row5, col5, row4, col4, halfThickness);

                // 线段2: 点4 -> 点6 (中心左到左下) - 斜线
                ho_Region2 = GenRectangle2Region(row4, col4, row6, col6, halfThickness);

                // 线段3: 点4 -> 点1 (中心左到中心右，水平线)
                ho_Region3 = GenRectangle2Region(row4, col4, row1, col1, halfThickness);

                // 线段4: 点1 -> 点2 (中心右到右上) - 斜线
                ho_Region4 = GenRectangle2Region(row1, col1, row2, col2, halfThickness);

                // 线段5: 点1 -> 点3 (中心右到右下) - 斜线
                ho_Region5 = GenRectangle2Region(row1, col1, row3, col3, halfThickness);

                // 合并所有区域成一个整体
                HOperatorSet.Union2(ho_Region1, ho_Region2, out ho_UnionRegion);
                HObject temp;
                HOperatorSet.Union2(ho_UnionRegion, ho_Region3, out temp);
                ho_UnionRegion.Dispose(); ho_UnionRegion = temp;
                HOperatorSet.Union2(ho_UnionRegion, ho_Region4, out temp);
                ho_UnionRegion.Dispose(); ho_UnionRegion = temp;
                HOperatorSet.Union2(ho_UnionRegion, ho_Region5, out temp);
                ho_UnionRegion.Dispose(); ho_UnionRegion = temp;

                // 从合并后的区域提取轮廓
                ho_Contour.Dispose();
                HOperatorSet.GenContourRegionXld(ho_UnionRegion, out ho_Contour, "border");

                return ho_Contour;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DrawSynthethic Error: {ex.Message}");
                return ho_Contour;
            }
            finally
            {
                // 释放临时区域对象
                ho_Region1?.Dispose();
                ho_Region2?.Dispose();
                ho_Region3?.Dispose();
                ho_Region4?.Dispose();
                ho_Region5?.Dispose();
                ho_UnionRegion?.Dispose();
            }
        }

        /// <summary>
        /// 根据两个端点和半宽度生成矩形区域
        /// </summary>
        /// <param name="row1">起点行坐标</param>
        /// <param name="col1">起点列坐标</param>
        /// <param name="row2">终点行坐标</param>
        /// <param name="col2">终点列坐标</param>
        /// <param name="halfWidth">半宽度（厚度的一半）</param>
        /// <returns>矩形区域对象</returns>
        private HObject GenRectangle2Region(double row1, double col1, double row2, double col2, double halfWidth)
        {
            HObject region;
            HOperatorSet.GenEmptyRegion(out region);

            // 计算线段中点
            double centerRow = (row1 + row2) / 2;
            double centerCol = (col1 + col2) / 2;

            // 计算线段长度
            double length = System.Math.Sqrt(System.Math.Pow(row2 - row1, 2) + System.Math.Pow(col2 - col1, 2));
            double halfLength = length / 2;

            // 计算线段角度 (相对于水平方向)
            // 注意：HALCON中Row向下为正，但角度系统基于标准数学坐标系(Y向上为正)
            // 所以需要翻转Row方向：phi = atan2(-(row2-row1), col2-col1) = atan2(row1-row2, col2-col1)
            double phi = System.Math.Atan2(row1 - row2, col2 - col1);

            // 使用 GenRectangle2 生成矩形区域
            region.Dispose();
            HOperatorSet.GenRectangle2(out region, centerRow, centerCol, phi, halfLength, halfWidth);

            return region;
        }

        /// <summary>
        /// 只修改左上角位置，右下角（点3）固定不变，以右下角为锚点缩放图形
        /// </summary>
        /// <param name="context">绘图上下文</param>
        /// <param name="newTopLeft">新的左上角坐标</param>
        /// <param name="scaling">输出缩放比例</param>
        /// <returns>缩放后的轮廓对象</returns>
        private HObject newTopLeftSynthethic(DisplayUI display, Point2d newTopLeft, out double scaling)
        {
            HObject ho_Contour; HOperatorSet.GenEmptyObj(out ho_Contour);
            HObject ho_Region1 = null, ho_Region2 = null, ho_Region3 = null, ho_Region4 = null, ho_Region5 = null;
            HObject ho_UnionRegion = null;

            try
            {
                // 固定右下角（点3）作为锚点
                double anchorCol = RegBottomRight.X;  // 点3的Col坐标
                double anchorRow = RegBottomRight.Y;  // 点3的Row坐标

                // 计算原始尺寸（点5到点3的距离）
                double originalWidth = CalMediumLong + CalLeftLong + CalRightLong;  // 整体宽度
                double originalHeight = CalTopLong;                                  // 整体高度
                double originalDiagonal = System.Math.Sqrt(originalWidth * originalWidth + originalHeight * originalHeight);

                // 计算新尺寸（新左上角到固定右下角的距离）
                double newWidth = anchorCol - newTopLeft.X;
                double newHeight = anchorRow - newTopLeft.Y;
                double newDiagonal = System.Math.Sqrt(newWidth * newWidth + newHeight * newHeight);

                // 计算缩放比例
                scaling = newDiagonal / originalDiagonal;

                // 根据缩放比例计算新的像素值
                double halfMedium = CalMediumLong / 2 * scaling;
                double halfTop = CalTopLong / 2 * scaling;
                double leftLong = CalLeftLong * scaling;
                double rightLong = CalRightLong * scaling;
                double halfThickness = CalThickness / 2 * scaling;

                // 从固定的右下角（点3）反推中心点位置
                // 点3: row3 = centerRow + halfTop, col3 = centerCol + halfMedium + rightLong
                double newCenterRow = anchorRow - halfTop;
                double newCenterCol = anchorCol - halfMedium - rightLong;

                // 计算各点坐标 (Row, Col)
                double row1 = newCenterRow, col1 = newCenterCol + halfMedium;
                double row4 = newCenterRow, col4 = newCenterCol - halfMedium;
                double row2 = newCenterRow - halfTop, col2 = newCenterCol + halfMedium + rightLong;
                double row3 = newCenterRow + halfTop, col3 = newCenterCol + halfMedium + rightLong;  // = anchorRow, anchorCol
                double row5 = newCenterRow - halfTop, col5 = newCenterCol - halfMedium - leftLong;
                double row6 = newCenterRow + halfTop, col6 = newCenterCol - halfMedium - leftLong;

                // 生成 5 个矩形区域，然后合并成一个整体
                ho_Region1 = GenRectangle2Region(row5, col5, row4, col4, halfThickness);
                ho_Region2 = GenRectangle2Region(row4, col4, row6, col6, halfThickness);
                ho_Region3 = GenRectangle2Region(row4, col4, row1, col1, halfThickness);
                ho_Region4 = GenRectangle2Region(row1, col1, row2, col2, halfThickness);
                ho_Region5 = GenRectangle2Region(row1, col1, row3, col3, halfThickness);

                // 合并所有区域成一个整体
                HOperatorSet.Union2(ho_Region1, ho_Region2, out ho_UnionRegion);
                HObject temp;
                HOperatorSet.Union2(ho_UnionRegion, ho_Region3, out temp);
                ho_UnionRegion.Dispose(); ho_UnionRegion = temp;
                HOperatorSet.Union2(ho_UnionRegion, ho_Region4, out temp);
                ho_UnionRegion.Dispose(); ho_UnionRegion = temp;
                HOperatorSet.Union2(ho_UnionRegion, ho_Region5, out temp);
                ho_UnionRegion.Dispose(); ho_UnionRegion = temp;

                // 从合并后的区域提取轮廓
                ho_Contour.Dispose();
                HOperatorSet.GenContourRegionXld(ho_UnionRegion, out ho_Contour, "border");

                // 只更新左上角，右下角保持不变
                RegTopLeft = new Point2d(col5, row5);
                // RegBottomRight 保持不变（锚点位置）

                return ho_Contour;
            }
            catch (System.Exception ex)
            {
                scaling = 1.0;
                System.Diagnostics.Debug.WriteLine($"newTopLeftSynthethic Error: {ex.Message}");
                return ho_Contour;
            }
            finally
            {
                ho_Region1?.Dispose();
                ho_Region2?.Dispose();
                ho_Region3?.Dispose();
                ho_Region4?.Dispose();
                ho_Region5?.Dispose();
                ho_UnionRegion?.Dispose();
            }
        }

        /// <summary>
        /// 只修改右下角位置，左上角（点5）固定不变，以左上角为锚点缩放图形
        /// </summary>
        /// <param name="context">绘图上下文</param>
        /// <param name="newBottomRight">新的右下角坐标</param>
        /// <param name="scaling">输出缩放比例</param>
        /// <returns>缩放后的轮廓对象</returns>
        private HObject newBottomRightSynthethic(DisplayUI display, Point2d newBottomRight, out double scaling)
        {
            HObject ho_Contour; HOperatorSet.GenEmptyObj(out ho_Contour);
            HObject ho_Region1 = null, ho_Region2 = null, ho_Region3 = null, ho_Region4 = null, ho_Region5 = null;
            HObject ho_UnionRegion = null;

            try
            {
                // 固定左上角（点5）作为锚点
                double anchorCol = RegTopLeft.X;  // 点5的Col坐标
                double anchorRow = RegTopLeft.Y;  // 点5的Row坐标

                // 计算原始尺寸（点5到点3的距离）
                double originalWidth = CalMediumLong + CalLeftLong + CalRightLong;  // 整体宽度
                double originalHeight = CalTopLong;                                  // 整体高度
                double originalDiagonal = System.Math.Sqrt(originalWidth * originalWidth + originalHeight * originalHeight);

                // 计算新尺寸（固定左上角到新右下角的距离）
                double newWidth = newBottomRight.X - anchorCol;
                double newHeight = newBottomRight.Y - anchorRow;
                double newDiagonal = System.Math.Sqrt(newWidth * newWidth + newHeight * newHeight);

                // 计算缩放比例
                scaling = newDiagonal / originalDiagonal;

                // 根据缩放比例计算新的像素值
                double halfMedium = CalMediumLong / 2 * scaling;
                double halfTop = CalTopLong / 2 * scaling;
                double leftLong = CalLeftLong * scaling;
                double rightLong = CalRightLong * scaling;
                double halfThickness = CalThickness / 2 * scaling;

                // 从固定的左上角（点5）反推中心点位置
                // 点5: row5 = centerRow - halfTop, col5 = centerCol - halfMedium - leftLong
                double newCenterRow = anchorRow + halfTop;
                double newCenterCol = anchorCol + halfMedium + leftLong;

                // 计算各点坐标 (Row, Col)
                double row1 = newCenterRow, col1 = newCenterCol + halfMedium;
                double row4 = newCenterRow, col4 = newCenterCol - halfMedium;
                double row2 = newCenterRow - halfTop, col2 = newCenterCol + halfMedium + rightLong;
                double row3 = newCenterRow + halfTop, col3 = newCenterCol + halfMedium + rightLong;
                double row5 = newCenterRow - halfTop, col5 = newCenterCol - halfMedium - leftLong;  // = anchorRow, anchorCol
                double row6 = newCenterRow + halfTop, col6 = newCenterCol - halfMedium - leftLong;

                // 生成 5 个矩形区域，然后合并成一个整体
                ho_Region1 = GenRectangle2Region(row5, col5, row4, col4, halfThickness);
                ho_Region2 = GenRectangle2Region(row4, col4, row6, col6, halfThickness);
                ho_Region3 = GenRectangle2Region(row4, col4, row1, col1, halfThickness);
                ho_Region4 = GenRectangle2Region(row1, col1, row2, col2, halfThickness);
                ho_Region5 = GenRectangle2Region(row1, col1, row3, col3, halfThickness);

                // 合并所有区域成一个整体
                HOperatorSet.Union2(ho_Region1, ho_Region2, out ho_UnionRegion);
                HObject temp;
                HOperatorSet.Union2(ho_UnionRegion, ho_Region3, out temp);
                ho_UnionRegion.Dispose(); ho_UnionRegion = temp;
                HOperatorSet.Union2(ho_UnionRegion, ho_Region4, out temp);
                ho_UnionRegion.Dispose(); ho_UnionRegion = temp;
                HOperatorSet.Union2(ho_UnionRegion, ho_Region5, out temp);
                ho_UnionRegion.Dispose(); ho_UnionRegion = temp;

                // 从合并后的区域提取轮廓
                ho_Contour.Dispose();
                HOperatorSet.GenContourRegionXld(ho_UnionRegion, out ho_Contour, "border");

                // 只更新右下角，左上角保持不变
                // RegTopLeft 保持不变（锚点位置）
                RegBottomRight = new Point2d(col3, row3);

                return ho_Contour;
            }
            catch (System.Exception ex)
            {
                scaling = 1.0;
                System.Diagnostics.Debug.WriteLine($"newBottomRightSynthethic Error: {ex.Message}");
                return ho_Contour;
            }
            finally
            {
                ho_Region1?.Dispose();
                ho_Region2?.Dispose();
                ho_Region3?.Dispose();
                ho_Region4?.Dispose();
                ho_Region5?.Dispose();
                ho_UnionRegion?.Dispose();
            }
        }

        /// <summary>
        /// 只修改中心位置，保持当前图形大小不变，平移整个图形
        /// 保留之前通过 newTopLeftSynthethic 或 newBottomRightSynthethic 修改的缩放比例
        /// 左上角和右下角都会随之更新
        /// </summary>
        /// <param name="context">绘图上下文</param>
        /// <param name="newCenter">新的中心坐标</param>
        /// <param name="scaling">输出当前的缩放比例</param>
        /// <returns>平移后的轮廓对象</returns>
        private HObject newCenterSynthethic(DisplayUI display, Point2d newCenter, out double scaling)
        {
            HObject ho_Contour; HOperatorSet.GenEmptyObj(out ho_Contour);
            HObject ho_Region1 = null, ho_Region2 = null, ho_Region3 = null, ho_Region4 = null, ho_Region5 = null;
            HObject ho_UnionRegion = null;

            try
            {
                // 根据当前的 RegTopLeft 和 RegBottomRight 计算当前的缩放比例
                // 这样可以保留之前通过 newTopLeftSynthethic 或 newBottomRightSynthethic 修改的比例
                double currentWidth = RegBottomRight.X - RegTopLeft.X;
                double currentHeight = RegBottomRight.Y - RegTopLeft.Y;
                double currentDiagonal = System.Math.Sqrt(currentWidth * currentWidth + currentHeight * currentHeight);

                double originalWidth = CalMediumLong + CalLeftLong + CalRightLong;
                double originalHeight = CalTopLong;
                double originalDiagonal = System.Math.Sqrt(originalWidth * originalWidth + originalHeight * originalHeight);

                // 计算当前的缩放比例
                scaling = currentDiagonal / originalDiagonal;

                // 新的中心点（从传入参数获取）
                double newCenterCol = newCenter.X;
                double newCenterRow = newCenter.Y;

                // 使用当前的缩放比例计算像素值（保持当前大小）
                double halfMedium = CalMediumLong / 2 * scaling;
                double halfTop = CalTopLong / 2 * scaling;
                double leftLong = CalLeftLong * scaling;
                double rightLong = CalRightLong * scaling;
                double halfThickness = CalThickness / 2 * scaling;

                // 计算各点坐标 (Row, Col)
                double row1 = newCenterRow, col1 = newCenterCol + halfMedium;
                double row4 = newCenterRow, col4 = newCenterCol - halfMedium;
                double row2 = newCenterRow - halfTop, col2 = newCenterCol + halfMedium + rightLong;
                double row3 = newCenterRow + halfTop, col3 = newCenterCol + halfMedium + rightLong;
                double row5 = newCenterRow - halfTop, col5 = newCenterCol - halfMedium - leftLong;
                double row6 = newCenterRow + halfTop, col6 = newCenterCol - halfMedium - leftLong;

                // 生成 5 个矩形区域，然后合并成一个整体
                ho_Region1 = GenRectangle2Region(row5, col5, row4, col4, halfThickness);
                ho_Region2 = GenRectangle2Region(row4, col4, row6, col6, halfThickness);
                ho_Region3 = GenRectangle2Region(row4, col4, row1, col1, halfThickness);
                ho_Region4 = GenRectangle2Region(row1, col1, row2, col2, halfThickness);
                ho_Region5 = GenRectangle2Region(row1, col1, row3, col3, halfThickness);

                // 合并所有区域成一个整体
                HOperatorSet.Union2(ho_Region1, ho_Region2, out ho_UnionRegion);
                HObject temp;
                HOperatorSet.Union2(ho_UnionRegion, ho_Region3, out temp);
                ho_UnionRegion.Dispose(); ho_UnionRegion = temp;
                HOperatorSet.Union2(ho_UnionRegion, ho_Region4, out temp);
                ho_UnionRegion.Dispose(); ho_UnionRegion = temp;
                HOperatorSet.Union2(ho_UnionRegion, ho_Region5, out temp);
                ho_UnionRegion.Dispose(); ho_UnionRegion = temp;

                // 从合并后的区域提取轮廓
                ho_Contour.Dispose();
                HOperatorSet.GenContourRegionXld(ho_UnionRegion, out ho_Contour, "border");

                // 同步更新左上角和右下角（图形平移后位置都会改变）
                RegTopLeft = new Point2d(col5, row5);
                RegBottomRight = new Point2d(col3, row3);

                return ho_Contour;
            }
            catch (System.Exception ex)
            {
                scaling = 1.0;
                System.Diagnostics.Debug.WriteLine($"newCenterSynthethic Error: {ex.Message}");
                return ho_Contour;
            }
            finally
            {
                ho_Region1?.Dispose();
                ho_Region2?.Dispose();
                ho_Region3?.Dispose();
                ho_Region4?.Dispose();
                ho_Region5?.Dispose();
                ho_UnionRegion?.Dispose();
            }
        }

    }
}
