using System;
using HalconDotNet;

namespace DotNet.Drawing
{
    public class HWindowImage : IDisposable
    {
        HWindow hWindow;
        HWindowControl hWindowControl;

        ZoomImage getInfo;
        ZoomImage zoomInfo;

        public HObject HoImage;
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
            hWindowControl = (HWindowControl)sender;

            if (hWindowControl.Visible)
            {
                if (getInfo.parent.Width != hWindowControl.Parent.Width || getInfo.parent.Height != hWindowControl.Parent.Height)
                {
                    getInfo.parent.Width = hWindowControl.Parent.Width;
                    getInfo.parent.Height = hWindowControl.Parent.Height;

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

                //1、判断图片是否为空
                if (!_image.NotNull())
                {
                    HOperatorSet.ClearWindow(hWindow);
                    return;
                }
                HoImage = _image;

                //2、获取信息
                HOperatorSet.GetImageSize(HoImage, out getInfo.width, out getInfo.height);

                //3、缩放判断
                if (getInfo.width.D != zoomInfo.width.D || getInfo.height.D != zoomInfo.height.D)
                {
                    Fun_ZoomImage(getInfo);
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
        /// 初始化
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
                //zoomInfo = info.Clone();
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
            }
        }
    }
}
