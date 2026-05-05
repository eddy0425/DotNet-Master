using DotNet.Drawing;
using HalconDotNet;
using System;
using System.Windows.Forms;


namespace DotNet.HalconUI
{
    public partial class HModelForm : UserControl
    {
        HObject srcImage;
        HDisplayCore display;

        #region 属性

        public HObject HoImage => display.HoImage;  //图像
        public HWindow HoWindow => hWindowControl.HalconWindow;  //窗体句柄

        #endregion

        public HModelForm()
        {
            InitializeComponent();
            display = new HDisplayCore(hWindowControl);

            this.Dock = DockStyle.Fill;

            HOperatorSet.GenEmptyObj(out srcImage);
        }

        public void Reset()
        {
            hWindowControl.Focus();
        }

        public void SetDraw(HTuple mode)
        {
            HOperatorSet.SetDraw(HoWindow, mode);
        }

        public void DisplayModel(string modelPath, Point2d point, HObject ho_Contour, HTuple modelID, ModelType type)
        {
            HObject region; HOperatorSet.GenEmptyObj(out region);
            HObject regionTrans1; HOperatorSet.GenEmptyObj(out regionTrans1);
            HObject regionTrans2; HOperatorSet.GenEmptyObj(out regionTrans2);

            try
            {
                srcImage.Dispose();
                HOperatorSet.ReadImage(out srcImage, modelPath);
                display.DispImage(srcImage);

                regionTrans1.Dispose();
                HalconHelper.TransRegion(point, new Point2d(), ho_Contour, out regionTrans1);

                regionTrans2.Dispose();
                HalconHelper.TransRegion(new Point2d(), display.Centre, ho_Contour, out regionTrans2);

                //ho_Contours.Dispose();
                //type.GetModelContours(modelID,new ModelResult(),out ho_Contours);
                display.DispRegion(regionTrans2);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                regionTrans1.Dispose();
                regionTrans2.Dispose();
            }
        }

        public void DisplayModel2(string modelPath, HObject ho_ModeRect, HObject ho_Contour, ModelResult result)
        {
            HObject region; HOperatorSet.GenEmptyObj(out region);
            HObject regionTrans1; HOperatorSet.GenEmptyObj(out regionTrans1);
            HObject regionTrans2; HOperatorSet.GenEmptyObj(out regionTrans2);

            try
            {
                srcImage.Dispose();
                HOperatorSet.ReadImage(out srcImage, modelPath);
                display.DispImage(srcImage);
                display.DispRegion(ho_ModeRect);

                //regionTrans1.Dispose();
                //HalconHelper.TransRegion(point, new Point2d(), ho_Contour, out regionTrans1);

                regionTrans2.Dispose();
                HalconHelper.TransRegion(result.Coord.Center, display.Centre, ho_Contour, out regionTrans2);

                //ho_Contours.Dispose();
                //type.GetModelContours(modelID,new ModelResult(),out ho_Contours);
                display.DispRegion(regionTrans2);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                regionTrans1.Dispose();
                regionTrans2.Dispose();
            }
        }
    }
}
