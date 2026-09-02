using DotNet.Drawing;
using HalconDotNet;
using System;
using System.Windows.Forms;


namespace DotNet.HalconUI
{
    public partial class HModelUI : UserControl
    {
        HObject _srcImage;
        HObject _modeRect;
        HObject _contour;
        CvCoord _coord;
        readonly HDisplay display;
        readonly HWindowMouse mouse;

        public HModelUI()
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;
            display = new HDisplay(hWindowControl);
            mouse = new HWindowMouse(hWindowControl, display);

            HOperatorSet.GenEmptyObj(out _srcImage);
            HOperatorSet.GenEmptyObj(out _modeRect);
            HOperatorSet.GenEmptyObj(out _contour);

            hWindowControl.HMouseMove += OnMouseMove;
        }

        public void DisplayModel(string modelPath, HObject ho_ModeRect, HObject ho_Contour, ModelResult result)
        {
            hWindowControl.Focus();

            _srcImage.Dispose();
            HOperatorSet.ReadImage(out _srcImage, modelPath);
            display.DispImage(_srcImage);

            Point2d from = result.Coord.Center;
            Point2d to = display.HoCentre;

            _modeRect.Dispose();
            TransObject(from, to, ho_ModeRect, out _modeRect);
            display.Disp(_modeRect, DrawStyle.Of(HColor.Blue));

            _contour.Dispose();
            TransObject(from, to, ho_Contour, out _contour);
            display.Disp(_contour, DrawStyle.Of(HColor.Green));

            Point2d centerTrans = HalconController.TransPoint(from, to, new Point2d(result.Column, result.Row));
            _coord = new CvCoord(centerTrans, Angle.FromRadians(result.Angle));
            display.Disp(_coord, DrawStyle.Of(HColor.Red));
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
                HalconController.TransContourXld(from, to, obj, out objTrans);
            }
            else
            {
                HalconController.TransRegion(from, to, obj, out objTrans);
            }
        }

        /// <summary>
        /// 释放本控件持有的 HALCON 资源，由 <see cref="Dispose(bool)"/> 的 disposing 分支调用。
        /// </summary>
        /// <remarks>
        /// 三个 HObject 字段与显示/鼠标对象原先从未释放：控件反复创建销毁时，
        /// HALCON 侧的非托管句柄会持续累积。
        /// </remarks>
        private void ReleaseDisplayResources()
        {
            hWindowControl.HMouseMove -= OnMouseMove;

            try { mouse?.Dispose(); }
            catch (Exception ex) { Log.Error(nameof(HModelUI), "释放鼠标交互资源失败.", ex); }

            try { display?.Dispose(); }
            catch (Exception ex) { Log.Error(nameof(HModelUI), "释放显示资源失败.", ex); }

            try
            {
                _srcImage?.Dispose();
                _modeRect?.Dispose();
                _contour?.Dispose();
            }
            catch (Exception ex) { Log.Error(nameof(HModelUI), "释放图像资源失败.", ex); }
        }

        public void OnMouseMove(object sender, HMouseEventArgs e)
        {
            display.Disp(_modeRect, DrawStyle.Of(HColor.Blue));
            display.Disp(_contour, DrawStyle.Of(HColor.Green));
            display.Disp(_coord, DrawStyle.Of(HColor.OrangeRed));
        }

    }
}
