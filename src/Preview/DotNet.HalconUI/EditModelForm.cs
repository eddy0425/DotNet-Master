using DotNet.Drawing;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DotNet.HalconUI
{
    public partial class EditModelForm : Form
    {
        bool EraseRegion = false;
        bool EnableEdit = false;
        DisplayForm _display => displayForm1;          //图像显示窗体
        public EditModelForm()
        {
            InitializeComponent();

            _display.HMouseDown += DisPlay_HMouseDown;
            _display.HMouseUp += DisPlay_HMouseUp;
            _display.HMouseWheel += DisPlay_HMouseWheel;
            _display.HMouseMove += DisPlay_HMouseMove;
        }
        private void FormDispose()
        {
            _display.HMouseDown -= DisPlay_HMouseDown;
            _display.HMouseUp -= DisPlay_HMouseUp;
            _display.HMouseWheel -= DisPlay_HMouseWheel;
            _display.HMouseMove -= DisPlay_HMouseMove;
        }
        private void DisPlay_HMouseDown(object sender, HMouseEventArgs e)
        {
            try
            {
                //ReDisplay();

                //if (e.Button == MouseButtons.Left) // 检查用户是否按下了鼠标右键
                //{
                //    if (EraseRegion)
                //    {
                //        EnableEdit = true;
                //        DrawCircle(e.Y, e.X);
                //        DispEraseRegion();
                //    }
                //}
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        private void DisPlay_HMouseUp(object sender, HMouseEventArgs e)
        {
            try
            {
                //ReDisplay();

                //if (e.Button == MouseButtons.Left) // 检查用户是否按下了鼠标右键
                //{
                //    if (EraseRegion)
                //    {
                //        EnableEdit = false;
                //        //DispEraseRegion();
                //    }
                //}
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        private void DisPlay_HMouseWheel(object sender, HMouseEventArgs e)
        {
            try
            {
                //ReDisplay();

                //if (EraseRegion)
                //{
                //    DispEraseRegion();
                //}
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        private void DisPlay_HMouseMove(object sender, HMouseEventArgs e)
        {
            try
            {
                //if (EraseRegion)
                //{
                //    if (EnableEdit)
                //    {
                //        ReDisplay();
                //        DrawCircle(e.Y, e.X);
                //        DispEraseRegion();
                //    }
                //    else
                //    {
                //        disPlay.ReDispImage();

                //        ReDisplay();
                //        DispEraseRegion();
                //        DispCircle(e.Y, e.X);

                //    }
                //}
                //else
                //{
                //    ReDisplay();
                //}
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }
}
