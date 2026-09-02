using System;
using DotNet.Drawing;
using HalconDotNet;

namespace DotNet.HalconUI
{
    public class HWindowImage : IDisposable
    {
        readonly HWindow _hWindow;
        readonly HWindowControl _hWindowControl;

        ZoomImage getInfo;
        ZoomImage zoomInfo;

        bool _disposed;

        /// <summary>
        /// 当前显示的图像。所有权由调用方（<see cref="HDisplay"/>）持有，本类只引用，不负责释放。
        /// </summary>
        public HObject HoImage { get; private set; }
        public double HoWidth { get { return getInfo.width; } }
        public double HoHeight { get { return getInfo.height; } }

        public HWindowImage(HWindowControl hWindowControl)
        {
            if (hWindowControl == null) throw new ArgumentNullException(nameof(hWindowControl));

            _hWindow = hWindowControl.HalconWindow;
            _hWindowControl = hWindowControl;

            getInfo = new ZoomImage();
            zoomInfo = new ZoomImage();

            hWindowControl.Resize += HWindowControl_Resize;
        }

        bool CanDraw()
        {
            if (_disposed) return false;
            if (_hWindowControl == null || _hWindowControl.IsDisposed) return false;
            if (_hWindowControl.Parent == null) return false;
            if (!_hWindowControl.Visible) return false;
            try { return _hWindow != null && _hWindow.IsInitialized(); }
            catch { return false; }
        }

        private void HWindowControl_Resize(object sender, EventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                // 尺寸变化回调运行在控件布局路径上，抛出会打断整个 Layout；
                // 但完全静默会让"窗口不跟随缩放"这类问题无从查起，所以记日志后忽略。
                Log.Warn(nameof(HWindowImage), "自适应缩放失败.", ex);
            }
        }

        /// <summary> 设置图像 </summary>
        internal void Fun_SetImage(HObject _image)
        {
            HoImage = _image;
            HOperatorSet.GetImageSize(HoImage, out getInfo.width, out getInfo.height);
        }

        /// <summary> 图像显示 </summary>
        public void Fun_ReDisplay()
        {
            if (!CanDraw()) return;

            try
            {
                if (!HoImage.NotNull())
                {
                    HOperatorSet.ClearWindow(_hWindow);
                    return;
                }

                HOperatorSet.DispObj(HoImage, _hWindow);
            }
            catch (Exception ex)
            {
                Log.Error(nameof(HWindowImage), "重绘图像失败.", ex);
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
                    HOperatorSet.ClearWindow(_hWindow);
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
                    HOperatorSet.SetPart(_hWindow, 0, 0, getInfo.height - 1, getInfo.width - 1);
                }
                HOperatorSet.DispObj(HoImage, _hWindow);
            }
            catch (Exception ex)
            {
                Log.Error(nameof(HWindowImage), "显示图像失败.", ex);
            }
        }

        /// <summary> 按父容器尺寸缩放/居中 HWindowControl </summary>
        private void Fun_ZoomImage(ZoomImage info)
        {
            if (_disposed || _hWindowControl == null || _hWindowControl.IsDisposed) return;
            if (_hWindowControl.Parent == null) return;
            if (info == null) return;

            // 防御除零：父容器尚未布局时宽高可能为 0
            int parentW = _hWindowControl.Parent.Width;
            int parentH = _hWindowControl.Parent.Height;
            double imgW = info.width.D;
            double imgH = info.height.D;
            if (parentW <= 0 || parentH <= 0 || imgW <= 0 || imgH <= 0) return;

            try
            {
                if ((imgW / parentW) < (imgH / parentH))
                {
                    _hWindowControl.Width = (int)(imgW * parentH / imgH);
                    _hWindowControl.Height = parentH;
                    _hWindowControl.Location = new System.Drawing.Point((parentW - _hWindowControl.Width) / 2, 0);
                }
                else
                {
                    _hWindowControl.Height = (int)(imgH * parentW / imgW);
                    _hWindowControl.Width = parentW;
                    _hWindowControl.Location = new System.Drawing.Point(0, (parentH - _hWindowControl.Height) / 2);
                }
                zoomInfo.width = info.width;
                zoomInfo.height = info.height;
                HOperatorSet.ClearWindow(_hWindow);
                HOperatorSet.SetDraw(_hWindow, "margin");
            }
            catch (Exception ex)
            {
                Log.Error(nameof(HWindowImage), "缩放图像失败.", ex);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            // 仅取消事件订阅；HoImage 的所有权不在本类，不在此释放，避免双重释放。
            if (_hWindowControl != null && !_hWindowControl.IsDisposed)
            {
                _hWindowControl.Resize -= HWindowControl_Resize;
            }

            HoImage = null;
            GC.SuppressFinalize(this);
        }
    }
}
