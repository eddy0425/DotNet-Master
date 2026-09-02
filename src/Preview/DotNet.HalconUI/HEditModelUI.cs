using DotNet.Drawing;
using HalconDotNet;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;


namespace DotNet.HalconUI
{
    public partial class HEditModelUI : Form
    {
        HObject _srcImage;
        HObject shrErase;
        HObject shrFindMode;
        HObject _shrContour;
        CvCoord _shrCoord;
        public HDisplayUI GetDisplay() => display;

        string shrColor => CB_ApplyColor.Text;
        int shrLineWidth => CB_ApplyLineWidth.Text.ExtractNumber();
        DrawEnum _drawType { get => display.DrawType; set => display.DrawType = value; }
        EraseRectMouse eraseRect = new EraseRectMouse();

        public HEditModelUI()
        {
            InitializeComponent();

            HOperatorSet.GenEmptyObj(out _srcImage);
            HOperatorSet.GenEmptyObj(out shrErase);
            HOperatorSet.GenEmptyObj(out shrFindMode);
            HOperatorSet.GenEmptyObj(out _shrContour);

            display.HMouseDown += OnMouseDown;
            display.HMouseUp += OnMouseUp;
            display.HMouseWheel += OnMouseWheel;
            display.HMouseMove += OnMouseMove;
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

        #region Events

        public void OnMouseDown(object sender, HMouseEventArgs e)
        {
            switch (_drawType)
            {
                case DrawEnum.Erase: eraseRect.OnMouseDown(e); break;
            }
        }

        public void OnMouseUp(object sender, HMouseEventArgs e)
        {
            switch (_drawType)
            {
                case DrawEnum.Erase: eraseRect.OnMouseUp(e); break;
            }
        }

        public void OnMouseWheel(object sender, HMouseEventArgs e)
        {
            switch (_drawType)
            {
                case DrawEnum.Erase: eraseRect.OnMouseWheel(e); break;
            }
        }

        public void OnMouseMove(object sender, HMouseEventArgs e)
        {
            switch (_drawType)
            {
                case DrawEnum.Erase:
                    {
                        eraseRect.SetPara(shrColor, shrLineWidth);
                        eraseRect.OnMouseMove(e);
                    }
                    break;
            }
        }

        #endregion

        private void but_addRegion_Click(object sender, System.EventArgs e)
        {
            _drawType = DrawEnum.None;
            DrawROI(shrFindMode, true);
        }

        private void btn_deleteRegion_Click(object sender, System.EventArgs e)
        {
            _drawType = DrawEnum.None;
            DrawROI(shrFindMode, false);
        }

        private RectEnum GetModifyShape()
        {
            RectEnum drawForm = RectEnum.Rectangle;
            if (CB_ModifyShape.Text == "矩形") drawForm = RectEnum.Rectangle;
            else if (CB_ModifyShape.Text == "仿射矩形") drawForm = RectEnum.AffRect;
            else if (CB_ModifyShape.Text == "圆") drawForm = RectEnum.Circle;
            else if (CB_ModifyShape.Text == "椭圆") drawForm = RectEnum.Ellipse;
            else if (CB_ModifyShape.Text == "多边型") drawForm = RectEnum.Polygon;
            return drawForm;
        }
        private void DrawROI(HObject findMode, bool IsAdd)
        {
            HObject drawRegion = null;
            HObject regionResult = null;
            try
            {
                display.Reset();
                display.ReDispImage();
                display.Display.Disp(findMode, DrawStyle.Of(HColor.Blue));
                var drawType = GetModifyShape();

                display.DrawRegion(drawType, out drawRegion);

                if (IsAdd)
                {
                    HOperatorSet.Union2(findMode, drawRegion, out regionResult);
                }
                else
                {
                    HOperatorSet.Difference(findMode, drawRegion, out regionResult);
                }

                shrFindMode.Dispose();
                shrFindMode = regionResult;
                regionResult = null;

                display.ReDispImage();
                display.Display.Disp(shrFindMode, DrawStyle.Of(HColor.Green));
            }
            finally
            {
                drawRegion?.Dispose();
                regionResult?.Dispose();
            }
        }

        private void but_ApplyRegion_Click(object sender, System.EventArgs e)
        {
            eraseRect.SetUp(display.Display, shrErase, shrFindMode, shrColor, shrLineWidth);
            _drawType = DrawEnum.Erase;
        }

        public void DisplayModel(string modelPath, HObject ho_ModeRect, HObject ho_Contour, ModelResult result)
        {
            display.Reset();

            _srcImage.Dispose();
            HOperatorSet.ReadImage(out _srcImage, modelPath);
            //display.DispImage(_srcImage);

            Point2d from = result.Coord.Center;
            Point2d to = display.Display.HoCentre;

            shrFindMode.Dispose();
            TransObject(from, to, ho_ModeRect, out shrFindMode);
            //display.Display.Disp(shrFindMode, DrawStyle.Of(HColor.Blue));

            _shrContour.Dispose();
            TransObject(from, to, ho_Contour, out _shrContour);
            //display.Display.Disp(_shrContour, DrawStyle.Of(HColor.Green));

            Point2d centerTrans = HalconController.TransPoint(from, to, new Point2d(result.Column, result.Row));
            _shrCoord = new CvCoord(centerTrans, Angle.FromRadians(result.Angle));
            //display.Display.Disp(_shrCoord, DrawStyle.Of(HColor.Red));

            display.SetModelPara(shrFindMode, _shrContour, _shrCoord);
            _drawType = DrawEnum.DispModel;

            display.DispImage(_srcImage);
            display.Display.Disp(shrFindMode, DrawStyle.Of(HColor.Blue));
            display.Display.Disp(_shrContour, DrawStyle.Of(HColor.Green));
            display.Display.Disp(_shrCoord, DrawStyle.Of(HColor.Red));
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

        private void HEditForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }

    }
}
