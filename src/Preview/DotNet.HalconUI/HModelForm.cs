using DotNet.Drawing;
using HalconDotNet;
using System.Windows.Forms;


namespace DotNet.HalconUI
{
    public partial class HModelForm : UserControl
    {
        HObject _srcImage;
        HObject _modeRect;
        HObject _contour;
        HDisplayCore display;

        public HModelForm()
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;
            display = new HDisplayCore(hWindowControl);

            HOperatorSet.GenEmptyObj(out _srcImage);
            HOperatorSet.GenEmptyObj(out _modeRect);
            HOperatorSet.GenEmptyObj(out _contour);
        }

        public void DisplayModel(string modelPath, HObject ho_ModeRect, HObject ho_Contour, ModelResult result)
        {
            hWindowControl.Focus();

            _srcImage.Dispose();
            HOperatorSet.ReadImage(out _srcImage, modelPath);
            display.DispImage(_srcImage);

            Point2d from = result.Coord.Center;
            Point2d to = display.Centre;

            _modeRect.Dispose();
            TransObject(from, to, ho_ModeRect, out _modeRect);
            display.DispRegion(_modeRect, HColor.Blue);

            _contour.Dispose();
            TransObject(from, to, ho_Contour, out _contour);
            display.DispRegion(_contour, HColor.Green);

            HalconHelper.TransPixel(from, to, result.Row, result.Column, out HTuple rowTrans, out HTuple colTrans);
            display.DispCross(colTrans, rowTrans, result.Angle, HColor.Red);
        }

        private static void TransObject(Point2d from, Point2d to, HObject obj, out HObject objTrans)
        {
            if (obj == null || !obj.IsInitialized() || obj.CountObj() <= 0)
            {
                HOperatorSet.GenEmptyObj(out objTrans);
                return;
            }

            HOperatorSet.GetObjClass(obj, out HTuple objClass);
            if (objClass.S.StartsWith("xld"))
            {
                HalconHelper.TransContourXld(from, to, obj, out objTrans);
            }
            else
            {
                HalconHelper.TransRegion(from, to, obj, out objTrans);
            }
        }
    }
}
