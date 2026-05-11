using System;
using DotNet.Drawing;
using HalconDotNet;

namespace DotNet.HalconUI
{
    public class HWindowImage : IDisposable
    {
        readonly HWindow hWindow;
        readonly HWindowControl hWindowControl;

        ZoomImage getInfo;
        ZoomImage zoomInfo;

        bool _disposed;

        /// <summary>
        /// 当前显示的图像。所有权由调用方（<see cref="HDisplayCore"/>）持有，本类只引用，不负责释放。
        /// </summary>
        public HObject HoImage { get; private set; }
        public double Width { get { return getInfo.width; } }
        public double Height { get { return getInfo.height; } }

        public HWindowImage(HWindow _hWindow, HWindowControl _hWindowControl)
        {
            hWindow = _hWindow;
            hWindowControl = _hWindowControl;

            getInfo = new ZoomImage();
            zoomInfo = new ZoomImage();

            hWindowControl.Resize += HWindowControl_Resize;
        }

        private void HWindowControl_Resize(object sender, EventArgs e)
        {
            HWindowControl control = (HWindowControl)sender;

            if (control.Visible)
            {
                if (getInfo.parent.Width != control.Parent.Width || getInfo.parent.Height != control.Parent.Height)
                {
                    getInfo.parent.Width = control.Parent.Width;
                    getInfo.parent.Height = control.Parent.Height;

                    Fun_ZoomImage(getInfo);
                    Fun_ReDisplay();
                }
            }
        }

        /// <summary>
        /// 重新显示
        /// </summary>
        public void Fun_ReDisplay()
        {
            Fun_DispImage(HoImage, false);
        }

        /// <summary>
        /// 图像显示
        /// </summary>
        public void Fun_DispImage(HObject _image, bool isSetPart)
        {
            try
            {
                if (hWindowControl.Parent == null || hWindowControl.IsDisposed || !hWindowControl.Visible) return;

                if (!_image.NotNull())
                {
                    HOperatorSet.ClearWindow(hWindow);
                    return;
                }
                HoImage = _image;

                HOperatorSet.GetImageSize(HoImage, out getInfo.width, out getInfo.height);

                if (getInfo.width.D != zoomInfo.width.D || getInfo.height.D != zoomInfo.height.D)
                {
                    Fun_ZoomImage(getInfo);
                }

                if (isSetPart)
                {
                    HOperatorSet.SetPart(hWindow, 0, 0, getInfo.height - 1, getInfo.width - 1);
                }
                HOperatorSet.DispObj(HoImage, hWindow);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 按父容器尺寸缩放/居中 HWindowControl
        /// </summary>
        private void Fun_ZoomImage(ZoomImage info)
        {
            try
            {
                if (hWindowControl.Parent == null) return;
                if ((info.width.D / hWindowControl.Parent.Width) < (info.height.D / hWindowControl.Parent.Height))
                {
                    hWindowControl.Width = (int)(info.width.D * hWindowControl.Parent.Height / info.height.D);
                    hWindowControl.Height = hWindowControl.Parent.Height;
                    hWindowControl.Location = new System.Drawing.Point((hWindowControl.Parent.Width - hWindowControl.Width) / 2, 0);
                }
                else
                {
                    hWindowControl.Height = (int)(info.height.D * hWindowControl.Parent.Width / info.width.D);
                    hWindowControl.Width = hWindowControl.Parent.Width;
                    hWindowControl.Location = new System.Drawing.Point(0, (hWindowControl.Parent.Height - hWindowControl.Height) / 2);
                }
                zoomInfo.width = info.width;
                zoomInfo.height = info.height;
                HOperatorSet.ClearWindow(hWindow);
                HOperatorSet.SetDraw(hWindow, "margin");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // 仅取消事件订阅；HoImage 的所有权不在本类，不在此释放，避免双重释放。
            if (hWindowControl != null && !hWindowControl.IsDisposed)
            {
                hWindowControl.Resize -= HWindowControl_Resize;
            }

            HoImage = null;
            GC.SuppressFinalize(this);
        }
    }
}
