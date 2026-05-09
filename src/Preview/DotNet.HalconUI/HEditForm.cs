using DotNet.Drawing;
using HalconDotNet;
using System.Windows.Forms;


namespace DotNet.HalconUI
{
    public partial class HEditForm : Form
    {
        public string ShrColor => CB_ApplyColor.Text;
        public int ShrLineWidth => CB_ApplyLineWidth.Text.ExtractNumber();

        HObject _srcImage;
        HObject _modeRect;
        HObject _contour;
        public DisplayUI GetDisplay() => display;

        public HEditForm()
        {
            InitializeComponent();

            HOperatorSet.GenEmptyObj(out _srcImage);
            HOperatorSet.GenEmptyObj(out _modeRect);
            HOperatorSet.GenEmptyObj(out _contour);
        }

        private void HEditForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }

        private void HEditForm_Load(object sender, System.EventArgs e)
        {
            CB_ModifyShape.Items.Clear();
            CB_ModifyShape.Items.Add("矩形"); CB_ModifyShape.Items.Add("仿射矩形"); CB_ModifyShape.Items.Add("圆"); CB_ModifyShape.Items.Add("椭圆"); CB_ModifyShape.Items.Add("多边型");
            CB_ModifyShape.SelectedIndex = 0;

            for (int i = 1; i < 10; i++) CB_ApplyLineWidth.Items.Add($"线宽{i}");
            for (int i = 10; i < 100; i = i + 10) CB_ApplyLineWidth.Items.Add($"线宽{i}");

            CB_ApplyLineWidth.SelectedIndex = 0;
            CB_ApplyColor.SelectedIndex = 0;
        }

        private void but_addRegion_Click(object sender, System.EventArgs e)
        {
            //display.SetDrawMode("", DrawEnum.NewRect);
        }

        private void btn_deleteRegion_Click(object sender, System.EventArgs e)
        {

        }

        private void but_ApplyRegion_Click(object sender, System.EventArgs e)
        {
            display.SetDrawMode("", DrawEnum.EraseRect);
        }

        public void DisplayModel(string modelPath, HObject ho_ModeRect, HObject ho_Contour, ModelResult result)
        {
            display.Reset();

            _srcImage.Dispose();
            HOperatorSet.ReadImage(out _srcImage, modelPath);
            display.DispImage(_srcImage);

            Point2d from = result.Coord.Center;
            Point2d to = display.HoCentre;

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
