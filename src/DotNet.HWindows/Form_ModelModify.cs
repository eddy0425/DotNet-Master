using System;
using System.Drawing;
using System.Windows.Forms;
using DotNet.Drawing;
using HalconDotNet;

namespace DotNet.HWindows
{
    public partial class Form_ModelModify : Form
    {
        HTuple oldModelID;                  //模版ID备份
        CvCoord modelCentre;                //模版中心
        Point2d setModeCenter;              //模板查找区域中心点备份
        CvRegion findModeRegion;            //模板区域
        ModelType m_type;
        CvCoord LockCenter = new CvCoord();


        HObject srcImage = new HObject();
        readonly Form_HWDisPlay disPlay;          //图像显示窗体

        bool EraseRegion = false;
        bool EnableEdit = false;
        HObject m_eraseRegion = new HObject();
        HObject modelContour = new HObject();

        ModelInfo info { set; get; }


        #region
        public bool IsOk { set; get; }
        public HObject showSrcImage { get { return srcImage; } }
        public HObject showFindRegion { get { return findModeRegion.HoRegion; } }
        public HObject showContour { get { return modelContour; } }
        public CvCoord showCoord { get { return modelCentre; } }
        
        #endregion
        public Form_ModelModify()
        {
            
            InitializeComponent();

            disPlay = new Form_HWDisPlay();
            disPlay.Show();
            panel1.Controls.Add(disPlay);

            disPlay.HoMouseEvent.HMouseDown += DisPlay_HMouseDown;
            disPlay.HoMouseEvent.HMouseUp += DisPlay_HMouseUp;
            disPlay.HoMouseEvent.HMouseWheel += DisPlay_HMouseWheel;
            disPlay.HoMouseEvent.HMouseMove += DisPlay_HMouseMove;

            HOperatorSet.GenEmptyObj(out m_eraseRegion);
            HOperatorSet.GenEmptyObj(out modelContour);
        }
        private void FormDispose()
        {
            disPlay.HoMouseEvent.HMouseDown -= DisPlay_HMouseDown;
            disPlay.HoMouseEvent.HMouseUp -= DisPlay_HMouseUp;
            disPlay.HoMouseEvent.HMouseWheel -= DisPlay_HMouseWheel;
            disPlay.HoMouseEvent.HMouseMove -= DisPlay_HMouseMove;

            srcImage?.Dispose();
            m_eraseRegion?.Dispose();
            modelContour?.Dispose();
        }
        private void Form_ModifyModel_Load(object sender, EventArgs e)
        {
            CB_ModifyShape.Items.Clear();
            CB_ModifyShape.Items.Add("矩形"); CB_ModifyShape.Items.Add("仿射矩形"); CB_ModifyShape.Items.Add("圆"); CB_ModifyShape.Items.Add("椭圆"); CB_ModifyShape.Items.Add("多边型");
            CB_ModifyShape.SelectedIndex = 0;

            for (int i = 1; i < 10; i++) CB_ApplyLineWidth.Items.Add($"线宽{i}");
            for (int i = 10; i < 100; i = i + 10) CB_ApplyLineWidth.Items.Add($"线宽{i}");
            CB_ApplyLineWidth.SelectedIndex = 0;

            CB_ApplyColor.SelectedIndex = 0;
        }

        private void Form_ModifyPR_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                HalconHelper.TransRegion(disPlay.HoCentre, setModeCenter, findModeRegion.HoRegion, out findModeRegion.InRegion);

                this.Hide();
                e.Cancel = true;
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        //public void ShowModifyTemplate(string modelPath, CvRegion _findRegion, ModelInfo _info, ModelType _type)
        //{
        //    try
        //    {
        //        if (this.Visible)
        //        {
        //            this.Hide();
        //        }

        //        info = _info;
        //        CB_LockCenter.Checked = _info.LockCenter;
        //        m_type = _type;
        //        IsOk = false;
        //        this.Show();

        //        HOperatorSet.GenEmptyObj(out srcImage);
        //        HOperatorSet.ReadImage(out srcImage, modelPath);
        //        disPlay.DispImage(srcImage);

        //        findModeRegion = _findRegion;
        //        setModeCenter = new Point2d(findModeRegion.CentreX, findModeRegion.CentreY);

        //        CvOperatorSet.TransRegion(setModeCenter, disPlay.HoCentre, findModeRegion.HoRegion, out findModeRegion.InRegion);

        //        //参数备份
        //        oldModelID = info.modelID.Clone();
        //        modelCentre = m_type.GetModelCentre(srcImage, info);

        //        SetTemplate(srcImage, findModeRegion, info, m_type);
        //    }
        //    catch
        //    {
        //        this.Hide();
        //        throw;
        //    }
        //}
        
        public void ShowModifyTemplate(string modelPath, CvRegion _findRegion, Point2d _setModeCenter, ModelInfo _info, ModelType _type)
        {
            try
            {
                IsOk = false;
                if (this.Visible)
                {
                    this.Close();
                }
               
                this.Show();
                info = _info;
                CB_LockCenter.Checked = _info.LockCenter;
                m_type = _type;
             
                if (srcImage.NotNull()) { srcImage.Dispose(); HOperatorSet.GenEmptyObj(out srcImage); }
                HOperatorSet.ReadImage(out srcImage, modelPath);
                disPlay.DispImage(srcImage);

                findModeRegion = _findRegion;
                setModeCenter = _setModeCenter;

                HalconHelper.TransRegion(setModeCenter, disPlay.HoCentre, findModeRegion.HoRegion, out findModeRegion.InRegion);

                //参数备份
                oldModelID = info.modelID.Clone();

                m_type.FindModel2(srcImage, info, 1, out ModelResult oldResult); if (oldResult.score.Length <= 0) throw new Exception($"查找模板失败！!");
                LockCenter = new CvCoord(oldResult.X, oldResult.Y, oldResult.angle);

                SetTemplate(srcImage, findModeRegion, info, m_type);
            }
            catch
            {
           
                this.Hide();
                throw;
            }
        }
        private void SetTemplate(HObject hImage, CvRegion fRegion, ModelInfo info, ModelType type)
        {
            HObject imgReduced = new HObject(); HOperatorSet.GenEmptyObj(out imgReduced);

            try
            {
                var newResult = new ModelResult();
                var rowTrans = new HTuple();
                var colTrans = new HTuple();

                HOperatorSet.ReduceDomain(hImage, fRegion.HoRegion, out imgReduced);

                //制作模板
                HOperatorSet.ClearShapeModel(info.modelID);
                type.CreateModel(imgReduced, info);
               
                if (info.LockCenter)
                {
                    type.FindModel2(hImage, info, 1, out newResult); if (newResult.score.Length <= 0) throw new Exception($"新建模板失败！!");

                    HalconHelper.TransPixel(newResult.coord, LockCenter, 0, 0, out rowTrans, out colTrans);
                    type.SetModelOrigin(info.modelID, rowTrans, colTrans);

                    ////修改模版中心
                    //HTuple hv_HomMat2D = new HTuple();
                    //HOperatorSet.VectorAngleToRigid(newResult.row.D, newResult.column.D, newResult.angle.D, LockCenter.Y, LockCenter.X, LockCenter.angle, out hv_HomMat2D);
                    ////HOperatorSet.VectorAngleToRigid(newCoord.Y, newCoord.X, 0, oldCoord.Y, oldCoord.X, 0, out hv_HomMat2D);
                    //HOperatorSet.AffineTransPixel(hv_HomMat2D, 0, 0, out rowTrans, out colTrans);

                    //type.SetModelOrigin(info.modelID, rowTrans, colTrans);
                }

                // 显示
                type.FindModel2(hImage, info, 1, out newResult); if (newResult.score.Length <= 0) throw new Exception($"新建模板失败！!");
                type.GetModelContours(info.modelID, newResult, out modelContour);
                modelCentre = new CvCoord(newResult.X, newResult.Y, newResult.angle);

                disPlay.ReDispImage();
                disPlay.DispObj(findModeRegion.HoRegion, HColor.Blue);
                disPlay.DispObj(modelContour, HColor.Green);
                disPlay.DispCross(modelCentre, 50, HColor.OrangeRed);


                if (info.LockCenter)
                {
                    disPlay.DispText($"坐标偏移值 X:{colTrans.D.ToString("F2")} Y:{rowTrans.D.ToString("F2")}", 10, 5, HColor.Red);
                }
                disPlay.DispText($"新坐标 X:{modelCentre.X.ToString("F2")} Y:{modelCentre.Y.ToString("F2")} A:{modelCentre.Angle.ToString("F2")}", 10, 10, HColor.Red);
                disPlay.DispText($"旧坐标 X:{LockCenter.X.ToString("F2")} Y:{LockCenter.Y.ToString("F2")} A:{LockCenter.Angle.ToString("F2")}", 10, 15, HColor.Red);

                IsOk = true;
            }
            catch
            {
                info.modelID = oldModelID.Clone();
                IsOk = false;
                throw;
            }
            finally
            {
                imgReduced.Dispose();
            }
        }

        private void UpdataTemplateCenter()
        {
            disPlay.hWindowControl1.Focus();
            ReDisplay();

            disPlay.SetColor(HColor.Red);
            HOperatorSet.DrawPointMod(disPlay.HoWindow, modelCentre.Y, modelCentre.X, out HTuple row, out HTuple column);

            //修改模版中心
            HTuple hv_HomMat2D = new HTuple();
            HOperatorSet.VectorAngleToRigid(modelCentre.Y, modelCentre.X, 0, row, column, 0, out hv_HomMat2D);
            m_type.GetModelOrigin(info.modelID, out HTuple modeRow, out HTuple modeColumn);
            HOperatorSet.AffineTransPixel(hv_HomMat2D, modeRow, modeColumn, out HTuple rowTrans, out HTuple colTrans);
            m_type.SetModelOrigin(info.modelID, rowTrans, colTrans);


            // 显示
            var newResult = new ModelResult();
            m_type.FindModel2(srcImage, info, 1, out newResult); if (newResult.score.Length <= 0) throw new Exception($"新建模板失败！!");
            m_type.GetModelContours(info.modelID, newResult, out modelContour);
            modelCentre = new CvCoord(newResult.X, newResult.Y, newResult.angle);

            disPlay.ReDispImage();
            disPlay.DispObj(findModeRegion.HoRegion, HColor.Red);
            disPlay.DispObj(modelContour, HColor.Green);
            disPlay.DispCross(modelCentre, 50, HColor.Green);

            disPlay.DispText($"新坐标 X:{modelCentre.X.ToString("F2")} Y:{modelCentre.Y.ToString("F2")} A:{modelCentre.Angle.ToString("F2")}", 10, 10, HColor.Red);
            disPlay.DispText($"旧坐标 X:{LockCenter.X.ToString("F2")} Y:{LockCenter.Y.ToString("F2")} A:{LockCenter.Angle.ToString("F2")}", 10, 15, HColor.Red);

        }

        private void but_addRegion_Click(object sender, EventArgs e)
        {
            try
            {
                ColorButton(but_addRegion, false);
                findModeRegion.AddOrDecrease = true;
                findModeRegion.Type = GetModifyShape();
                DrawTemplateRegion();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally 
            { 
                ColorButton(but_addRegion, true); 
            }
        }
        private void btn_deleteRegion_Click(object sender, EventArgs e)
        {
            try
            {
                ColorButton(btn_deleteRegion, false);
                findModeRegion.AddOrDecrease = false;
                findModeRegion.Type = GetModifyShape();
                DrawTemplateRegion();
            }
            catch (Exception ex) 
            { 
                MessageBox.Show(ex.Message);
            }
            finally 
            { 
                ColorButton(btn_deleteRegion, true); 
            }
        }
        private void but_ModyfyCenter_Click(object sender, EventArgs e)
        {
            try
            {
                ColorButton(but_ModyfyCenter, false);
                UpdataTemplateCenter();
                LockCenter = new CvCoord(modelCentre.X, modelCentre.Y, modelCentre.Angle);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                ColorButton(but_ModyfyCenter, true);
            }
        }
        private void DrawTemplateRegion()
        {
            HObject region = new HObject(); HOperatorSet.GenEmptyObj(out region);

            //开始
            disPlay.hWindowControl1.Focus();
            ReDisplay();

            if (findModeRegion.AddOrDecrease) disPlay.SetColor(HColor.Green);
            else disPlay.SetColor(HColor.Red);

            switch (findModeRegion.Type)
            {
                case RectEnum.Rectangle:
                    HTuple row, col, row3, col3;
                    HOperatorSet.DrawRectangle1(disPlay.HoWindow, out row, out col, out row3, out col3);
                    if (row.D == 0 && col.D == 0)
                        return;
                    HOperatorSet.GenRectangle1(out region, row, col, row3, col3);
                    break;

                case RectEnum.Rectangle2:
                    HTuple angle, length1, length2;
                    HOperatorSet.DrawRectangle2(disPlay.HoWindow, out row, out col, out angle, out length1, out length2);
                    if (row.D == 0 && col.D == 0)
                        return;
                    HOperatorSet.GenRectangle2(out region, row, col, angle, length1, length2);
                    break;

                case RectEnum.Circle:
                    HTuple radius;
                    HOperatorSet.DrawCircle(disPlay.HoWindow, out row, out col, out radius);
                    if (row.D == 0 && col.D == 0)
                        return;
                    HOperatorSet.GenCircle(out region, row, col, radius);
                    break;

                case RectEnum.Ellipse:
                    HOperatorSet.DrawEllipse(disPlay.HoWindow, out row, out col, out angle, out length1, out length2);
                    if (row.D == 0 && col.D == 0)
                        return;
                    HOperatorSet.GenEllipse(out region, row, col, angle, length1, length2);
                    break;

                case RectEnum.Polygon:
                    HOperatorSet.DrawRegion(out region, disPlay.HoWindow);
                    HTuple R, C, A;
                    HOperatorSet.AreaCenter(region, out R, out C, out A);
                    if (A.D == 0)
                        return;
                    break;
            }

            if (findModeRegion.AddOrDecrease) HOperatorSet.Union2(findModeRegion.HoRegion, region, out findModeRegion.InRegion);
            else HOperatorSet.Difference(findModeRegion.HoRegion, region, out findModeRegion.InRegion);

            SetTemplate(srcImage, findModeRegion, info, m_type);
        }
        

        #region 其他事件
        private void ModifyTemplate_Resize(object sender, EventArgs e)
        {
            if (this.Visible) ReDisplay();
        }
       
        private void DisPlay_HMouseDown(object sender, HMouseEventArgs e)
        {
            try
            {
                ReDisplay();

                if (e.Button == MouseButtons.Left) // 检查用户是否按下了鼠标右键
                {
                    if (EraseRegion)
                    {
                        EnableEdit = true;
                        DrawCircle(e.Y, e.X);
                        DispEraseRegion();
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        private void DisPlay_HMouseUp(object sender, HMouseEventArgs e) 
        {
            try
            {
                ReDisplay();

                if (e.Button == MouseButtons.Left) // 检查用户是否按下了鼠标右键
                {
                    if (EraseRegion)
                    {
                        EnableEdit = false;
                        //DispEraseRegion();
                    }
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        private void DisPlay_HMouseWheel(object sender, HMouseEventArgs e) 
        {
            try
            {
                ReDisplay();

                if (EraseRegion)
                {
                    DispEraseRegion();
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        private void DisPlay_HMouseMove(object sender, HMouseEventArgs e)
        {
            try
            {
                if (EraseRegion)
                {
                    if (EnableEdit)
                    {
                        ReDisplay();
                        DrawCircle(e.Y, e.X);
                        DispEraseRegion();
                    }
                    else
                    {
                        disPlay.ReDispImage();

                        ReDisplay();
                        DispEraseRegion();
                        DispCircle(e.Y, e.X);

                    }
                }
                else
                {
                    ReDisplay();
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        private void CB_ApplyColor_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (EraseRegion)
                {
                    DispEraseRegion();
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        #endregion

        private void but_ApplyRegion_Click(object sender, EventArgs e)
        {
            try
            {
                EraseRegion = !EraseRegion;
                if (EraseRegion)
                {
                    if (m_eraseRegion.NotNull()) { m_eraseRegion.Dispose(); HOperatorSet.GenEmptyObj(out m_eraseRegion); }
                }
                else
                {
                    if (m_eraseRegion.NotNull()) { m_eraseRegion.Dispose(); HOperatorSet.GenEmptyObj(out m_eraseRegion); }
                    //HOperatorSet.Difference(findModeRegion.HoRegion, m_eraseRegion, out findModeRegion.InRegion);
                    SetTemplate(srcImage, findModeRegion, info, m_type);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                EnableEdit = false;
                ColorButton(but_ApplyRegion, !EraseRegion);
            }
        }

        private void DrawCircle(HTuple row, HTuple column)
        {
            HObject SubRegion = new HObject(); HOperatorSet.GenEmptyObj(out SubRegion);
            try
            {
                disPlay.SetDraw("fill");
                HOperatorSet.GenCircle(out SubRegion, row, column, CB_ApplyLineWidth.Text.ExtractNumber());
                HOperatorSet.Union2(m_eraseRegion, SubRegion, out m_eraseRegion);
                HOperatorSet.Difference(findModeRegion.HoRegion, m_eraseRegion, out findModeRegion.InRegion);
                disPlay.SetDraw("margin");
            }
            catch
            {
                throw;
            }
            finally
            {
                SubRegion.Dispose();
            }
        }

        private void DispCircle(HTuple row, HTuple column)
        {
            HObject SubRegion = new HObject(); HOperatorSet.GenEmptyObj(out SubRegion);
            try
            {
                disPlay.SetDraw("fill");
                HOperatorSet.GenCircle(out SubRegion, row, column, CB_ApplyLineWidth.Text.ExtractNumber());
                disPlay.DispObj(SubRegion, CB_ApplyColor.Text);
                disPlay.SetDraw("margin");
            }
            catch
            {
                throw;
            }
            finally
            {
                SubRegion.Dispose();
            }
        }

        private void DispEraseRegion()
        {
            if (m_eraseRegion.NotNull())
            {
                disPlay.SetDraw("fill");
                disPlay.DispObj(m_eraseRegion, CB_ApplyColor.Text);
                disPlay.SetDraw("margin");
            }
        }

        private void ReDisplay()
        {
            disPlay.SetDraw("margin");
            disPlay.DispObj(findModeRegion.HoRegion, HColor.Blue);
            disPlay.DispObj(modelContour, HColor.Green);
            if (modelCentre != null) disPlay.DispCross(modelCentre, 30, HColor.OrangeRed);
        }

        private RectEnum GetModifyShape()
        {
            RectEnum drawForm = RectEnum.Rectangle;
            if (CB_ModifyShape.Text == "矩形") drawForm = RectEnum.Rectangle;
            else if (CB_ModifyShape.Text == "仿射矩形") drawForm = RectEnum.Rectangle2;
            else if (CB_ModifyShape.Text == "圆") drawForm = RectEnum.Circle;
            else if (CB_ModifyShape.Text == "椭圆") drawForm = RectEnum.Ellipse;
            else if (CB_ModifyShape.Text == "多边型") drawForm = RectEnum.Polygon;
            return drawForm;
        }

        private void CB_LockCenter_CheckedChanged(object sender, EventArgs e)
        {
            info.LockCenter = CB_LockCenter.Checked;
        }

        private void ColorButton(Button button, bool state)
        {
            EnableButton(state);
            button.BackColor = !state ? Color.Red : Color.FromArgb(80, 160, 255);
            button.Enabled = true;
        }
        private void EnableButton(bool State)
        {
            but_addRegion.Enabled = State;
            btn_deleteRegion.Enabled = State;
            but_ApplyRegion.Enabled = State;
            but_ModyfyCenter.Enabled = State;
        }
    }

}
