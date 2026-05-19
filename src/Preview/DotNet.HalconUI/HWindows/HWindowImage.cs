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
        public double HoWidth { get { return getInfo.width; } }
        public double HoHeight { get { return getInfo.height; } }

        public HWindowImage(HWindow _hWindow, HWindowControl _hWindowControl)
        {
            hWindow = _hWindow ?? throw new ArgumentNullException(nameof(_hWindow));
            hWindowControl = _hWindowControl ?? throw new ArgumentNullException(nameof(_hWindowControl));

            getInfo = new ZoomImage();
            zoomInfo = new ZoomImage();

            hWindowControl.Resize += HWindowControl_Resize;
        }

        bool CanDraw()
        {
            if (_disposed) return false;
            if (hWindowControl == null || hWindowControl.IsDisposed) return false;
            if (hWindowControl.Parent == null) return false;
            if (!hWindowControl.Visible) return false;
            try { return hWindow != null && hWindow.IsInitialized(); }
            catch { return false; }
        }

        private void HWindowControl_Resize(object sender, EventArgs e)
        {
            if (_disposed) return;

            HWindowControl control = sender as HWindowControl;
            if (control == null || control.Parent == null) return;
            if (!control.Visible) return;

            if (getInfo.parent.Width != control.Parent.Width || getInfo.parent.Height != control.Parent.Height)
            {
                getInfo.parent.Width = control.Parent.Width;
                getInfo.parent.Height = control.Parent.Height;

                Fun_ZoomImage(getInfo);
                Fun_ReDisplay();
            }
        }

        /// <summary> 图像显示 </summary>
        public void Fun_ReDisplay()
        {
            if (!CanDraw()) return;

            try
            {
                if (!HoImage.NotNull())
                {
                    HOperatorSet.ClearWindow(hWindow);
                    return;
                }

                HOperatorSet.DispObj(HoImage, hWindow);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HWindowImage.Fun_ReDisplay] {ex.Message}");
            }
        }

        /// <summary> 图像显示 </summary>
        public void Fun_DispImage(HObject _image, bool isSetPart)
        {
            if (!CanDraw()) return;

            try
            {
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
                Console.WriteLine($"[HWindowImage.Fun_DispImage] {ex.Message}");
            }
        }

        /// <summary> 按父容器尺寸缩放/居中 HWindowControl </summary>
        private void Fun_ZoomImage(ZoomImage info)
        {
            if (_disposed || hWindowControl == null || hWindowControl.IsDisposed) return;
            if (hWindowControl.Parent == null) return;
            if (info == null) return;

            // 防御除零：父容器尚未布局时宽高可能为 0
            int parentW = hWindowControl.Parent.Width;
            int parentH = hWindowControl.Parent.Height;
            double imgW = info.width.D;
            double imgH = info.height.D;
            if (parentW <= 0 || parentH <= 0 || imgW <= 0 || imgH <= 0) return;

            try
            {
                if ((imgW / parentW) < (imgH / parentH))
                {
                    hWindowControl.Width = (int)(imgW * parentH / imgH);
                    hWindowControl.Height = parentH;
                    hWindowControl.Location = new System.Drawing.Point((parentW - hWindowControl.Width) / 2, 0);
                }
                else
                {
                    hWindowControl.Height = (int)(imgH * parentW / imgW);
                    hWindowControl.Width = parentW;
                    hWindowControl.Location = new System.Drawing.Point(0, (parentH - hWindowControl.Height) / 2);
                }
                zoomInfo.width = info.width;
                zoomInfo.height = info.height;
                HOperatorSet.ClearWindow(hWindow);
                HOperatorSet.SetDraw(hWindow, "margin");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HWindowImage.Fun_ZoomImage] {ex}");
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
