using System;
using System.Windows.Forms;
using DotNet.Drawing;
using HalconDotNet;

namespace DotNet.HWindows
{
    public partial class Form_ModelDisp : Form
    {
        HWindowImage hWindowImage;
        HWindowMouse hWindowMouse;

        bool Adaptive = true;    //自适应
        HObject srcImage = new HObject();
        HObject findRegion = new HObject();
        HObject modelContour = new HObject();
        CvCoord transCoord;

        public HWindow hWindow { get { return hWindowControl1.HalconWindow; } }  //窗体句柄
        public Point2d ho_Centre { get { return new Point2d(hWindowImage.Width / 2, hWindowImage.Height / 2); } }
        public Size2d ho_Size { get { return new Size2d(hWindowImage.Width, hWindowImage.Height); } }
        
        public Form_ModelDisp(bool noneFormBorder = true)
        {
            InitializeComponent();

            hWindowImage = new HWindowImage(hWindow, hWindowControl1);
            hWindowMouse = new HWindowMouse(hWindow, hWindowControl1, hWindowImage);

            if (noneFormBorder)
            {
                this.FormBorderStyle = FormBorderStyle.None;     //无边框
                this.Dock = DockStyle.Fill;
                this.TopLevel = false;
            }

            hWindowMouse.HMouseDown += HWindowMouse_HMouseDown;
            hWindowMouse.HMouseUp += HWindowMouse_HMouseUp;
            hWindowMouse.HMouseWheel += HWindowMouse_HMouseWheel;
            hWindowMouse.HMouseMove += HWindowMouse_HMouseMove;

            InitHalcon();
        }

        /// <summary>
        /// 打开模板设置界面时显示
        /// </summary>
        public void ShowModel(string modelPath, ModelInfo info, HObject _findRegion, ModelType type)
        {
            try
            {
                if (!modelPath.FileExists()) return;

                this.Show();

                HOperatorSet.GenEmptyObj(out srcImage);
                HOperatorSet.ReadImage(out srcImage, modelPath);
                hWindowImage.Fun_DispImage(srcImage, Adaptive);

                HOperatorSet.GenEmptyObj(out findRegion);
                findRegion = _findRegion.Clone();

                HOperatorSet.AreaCenter(findRegion, out HTuple area, out HTuple hv_Row, out HTuple hv_Column);
                Point2d Follow = new Point2d(ho_Centre.X, ho_Centre.Y);
                Point2d matching = new Point2d(hv_Column.D, hv_Row.D);
                HalconHelper.TransRegion(matching, Follow, findRegion, out findRegion);

                type.FindModel2(srcImage, info, 1, out ModelResult result);

                if (result.score?.Length > 0)
                {
                    type.GetModelContours(info.modelID,result,out modelContour);
                    transCoord = new CvCoord(result.column.D, result.row.D, result.angle.D.ToDegrees());
                }

                ReDisplay();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        /// <summary>
        /// 修改模板后显示
        /// </summary>
        public void ShowModel(HObject _srcImage, HObject _findRegion, HObject _contour, CvCoord _coord)
        {
            try
            {
                this.Show();

                HOperatorSet.GenEmptyObj(out srcImage);
                srcImage = _srcImage.Clone();

                HOperatorSet.GenEmptyObj(out findRegion);
                findRegion = _findRegion.Clone();

                HOperatorSet.GenEmptyObj(out modelContour);
                modelContour = _contour.Clone();

                hWindowImage.Fun_DispImage(srcImage, Adaptive);

                HOperatorSet.AreaCenter(findRegion, out HTuple area, out HTuple hv_Row, out HTuple hv_Column);
                Point2d Follow = new Point2d(ho_Centre.X, ho_Centre.Y);
                Point2d matching = new Point2d(hv_Column.D, hv_Row.D);

                HalconHelper.TransRegion(matching, Follow, findRegion, out findRegion);
                transCoord = _coord;

                ReDisplay();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        /// <summary>
        /// 设置模板时显示
        /// </summary>
        public void ShowModel(HObject _srcImage, HObject _findRegion, HObject _contour, ModelResult result, ModelType type)
        {
            try
            {
                this.Show();

                HOperatorSet.GenEmptyObj(out srcImage);
                srcImage = _srcImage.Clone();
                HOperatorSet.CropDomain(srcImage, out srcImage);

                HOperatorSet.GenEmptyObj(out findRegion);
                findRegion = _findRegion.Clone();

                HOperatorSet.GenEmptyObj(out modelContour);
                modelContour = _contour.Clone();

                hWindowImage.Fun_DispImage(srcImage, Adaptive);

                HOperatorSet.AreaCenter(findRegion, out HTuple area, out HTuple hv_Row, out HTuple hv_Column);
                Point2d Follow = new Point2d(ho_Centre.X, ho_Centre.Y);
                Point2d matching = new Point2d(hv_Column.D, hv_Row.D);

                HalconHelper.TransRegion(matching, Follow, findRegion, out findRegion);

                if (type == ModelType.NccModel)
                    HalconHelper.TransRegion(matching, Follow, modelContour, out modelContour);
                else
                    HalconHelper.TransContourXld(matching, Follow, modelContour, out modelContour);

                HalconHelper.TransPixel(matching, Follow, result.row.D, result.column.D, out HTuple rowTrans, out HTuple colTrans);

                transCoord = new CvCoord(colTrans.D, rowTrans.D, result.angle.D.ToDegrees());

                ReDisplay();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void FormDispose()
        {
            hWindowMouse.HMouseDown -= HWindowMouse_HMouseDown;
            hWindowMouse.HMouseUp -= HWindowMouse_HMouseUp;
            hWindowMouse.HMouseWheel -= HWindowMouse_HMouseWheel;
            hWindowMouse.HMouseMove -= HWindowMouse_HMouseMove;

            srcImage?.Dispose();
            findRegion?.Dispose();
            modelContour?.Dispose();
        }

        private void HWindowMouse_HMouseDown(object sender, HMouseEventArgs e) { ReDisplay(); }
        private void HWindowMouse_HMouseUp(object sender, HMouseEventArgs e) { ReDisplay(); }
        private void HWindowMouse_HMouseWheel(object sender, HMouseEventArgs e) { ReDisplay(); }
        private void HWindowMouse_HMouseMove(object sender, HMouseEventArgs e) { ReDisplay(); }
        private void InitHalcon() 
        {
            try
            {
                //5120x3840  //512 × 512
                HImage hImage = new HImage("byte", 5120, 3840);
                hWindowImage.Fun_DispImage(hImage, true);
                hImage.Dispose();
            }
            catch
            {
                throw;
            }
        }
        private void ReDisplay()
        {
            if (findRegion.NotNull())
                hWindow.DispObj(findRegion, HColor.Red);
            if (modelContour.NotNull())
                hWindow.DispObj(modelContour, HColor.Green);
            if (transCoord.NotNull() && transCoord != null)
                hWindow.DispCross(transCoord, 50, HColor.Red);
        }
    }
}
