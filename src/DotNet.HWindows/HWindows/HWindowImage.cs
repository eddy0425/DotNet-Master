using HalconDotNet;
using System;
using System.Windows.Forms;

namespace DotNet.HWindows
{
    public class HWindowImage : IDisposable
    {
        readonly HWindow _hWindow;
        readonly HWindowControl _hWindowControl;
        readonly ZoomImage _getInfo;
        readonly ZoomImage _zoomInfo;

        public volatile HObject HoImage;
        public double Width { get { return _getInfo.width; } }
        public double Height { get { return _getInfo.height; } }

        public HWindowImage(HWindow hWindow, HWindowControl hWindowControl)
        {
            _hWindow = hWindow;
            _hWindowControl = hWindowControl;

            _getInfo = new ZoomImage();
            _zoomInfo = new ZoomImage();

            _hWindowControl.Resize += HWindowControl_Resize;
        }

        private void HWindowControl_Resize(object sender, EventArgs e)
        {
            try
            {
                if (_hWindowControl.Visible)
                {
                    if (_getInfo.parent.Width != _hWindowControl.Parent.Width || _getInfo.parent.Height != _hWindowControl.Parent.Height)
                    {
                        _getInfo.parent.Width = _hWindowControl.Parent.Width;
                        _getInfo.parent.Height = _hWindowControl.Parent.Height;

                        Fun_ZoomImage(_getInfo);
                        Fun_Redisplay();
                    }
                }
            }
            catch(Exception ex)
            { 
                Console.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// 重新显示
        /// </summary>
        public void Fun_Redisplay()
        {
            try
            {
                Fun_DispImage(HoImage, false);
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// 图像显示
        /// </summary>
        public void Fun_DispImage(HObject _image, bool isSetPart)
        {
            try
            {
                if (_hWindowControl.Parent == null || _hWindowControl.IsDisposed || !_hWindowControl.Visible) return;

                //1、判断图片是否为空
                if (!_image.NotNull())
                {
                    HOperatorSet.ClearWindow(_hWindow);
                    return;
                }
                HoImage = _image;

                //2、获取信息
                HOperatorSet.GetImageSize(HoImage, out _getInfo.width, out _getInfo.height);

                //3、缩放判断
                if (_getInfo.width.D != _zoomInfo.width.D || _getInfo.height.D != _zoomInfo.height.D)
                {
                    Fun_ZoomImage(_getInfo);
                }
                //if (getInfo.parent.Width != hWindowControl.Parent.Width || getInfo.parent.Height != hWindowControl.Parent.Height)
                //{
                //    getInfo.parent.Width = hWindowControl.Parent.Width;
                //    getInfo.parent.Height = hWindowControl.Parent.Height;

                //    Fun_ZoomImage(getInfo);
                //}

                //4、显示图像
                if (isSetPart)
                {
                    HOperatorSet.SetPart(_hWindow, 0, 0, _getInfo.height - 1, _getInfo.width - 1);
                }
                HOperatorSet.DispObj(HoImage, _hWindow);

            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// 初始化
        /// </summary>
        private void Fun_ZoomImage(ZoomImage info)
        {
            try
            {
                // 更新UI（非计算密集型操作放到主线程中）
                _hWindowControl.Parent.Invoke((MethodInvoker)delegate
                {
                    if (_hWindowControl.Parent == null) return;
                    if ((info.width.D / _hWindowControl.Parent.Width) < (info.height.D / _hWindowControl.Parent.Height))
                    {
                        _hWindowControl.Width = (int)(info.width.D * _hWindowControl.Parent.Height / info.height.D);
                        _hWindowControl.Height = _hWindowControl.Parent.Height;
                        _hWindowControl.Location = new System.Drawing.Point((_hWindowControl.Parent.Width - _hWindowControl.Width) / 2, 0);
                    }
                    else
                    {
                        _hWindowControl.Height = (int)(info.height.D * _hWindowControl.Parent.Width / info.width.D);
                        _hWindowControl.Width = _hWindowControl.Parent.Width;
                        _hWindowControl.Location = new System.Drawing.Point(0, (_hWindowControl.Parent.Height - _hWindowControl.Height) / 2);
                    }
                });

                //zoomInfo = info.Clone();
                _zoomInfo.width = info.width;
                _zoomInfo.height = info.height;
                HOperatorSet.ClearWindow(_hWindow);
                HOperatorSet.SetDraw(_hWindow, "margin");
            }
            catch
            {
                throw;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~HWindowImage()
        {
            Dispose(false);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                HoImage?.Dispose();
                _hWindowControl.Resize -= HWindowControl_Resize;
            }
        }
    }
}
