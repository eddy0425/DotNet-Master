using DotNet.Drawing;
using HalconDotNet;
using System.Drawing;
using System.Windows.Forms;


namespace DotNet.HalconUI
{
    public partial class HEditModelForm : Form
    {
        string shrColor => CB_ApplyColor.Text;
        int shrLineWidth => CB_ApplyLineWidth.Text.ExtractNumber();
        private enum DrawHandle { None, Erase, DispModel }

        HObject _srcImage;
        HObject shrErase;
        HObject shrFindMode;
        HObject _shrContour;
        CvCoord _shrCoord;
        DrawHandle _hover;

        NoneMouse noneMouse;
        EraseRectMouse eraseRect;
        DispModelMouse dispModel;
        public DisplayUI GetDisplay() => display;


        public HEditModelForm()
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

            noneMouse = new NoneMouse();
            eraseRect = new EraseRectMouse();
            dispModel = new DispModelMouse();
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
            switch (_hover)
            {
                case DrawHandle.None: noneMouse.OnMouseDown(e); break;
                case DrawHandle.Erase: eraseRect.OnMouseDown(e); break;
                case DrawHandle.DispModel: dispModel.OnMouseDown(e); break;
            }
        }

        public void OnMouseUp(object sender, HMouseEventArgs e)
        {
            switch (_hover)
            {
                case DrawHandle.None: noneMouse.OnMouseUp(e); break;
                case DrawHandle.Erase: eraseRect.OnMouseUp(e); break;
                case DrawHandle.DispModel: dispModel.OnMouseUp(e); break;
            }
        }

        public void OnMouseWheel(object sender, HMouseEventArgs e)
        {
            switch (_hover)
            {
                case DrawHandle.None: noneMouse.OnMouseWheel(e); break;
                case DrawHandle.Erase: eraseRect.OnMouseWheel(e); break;
                case DrawHandle.DispModel: dispModel.OnMouseWheel(e); break;
            }
        }

        public void OnMouseMove(object sender, HMouseEventArgs e)
        {
            switch (_hover)
            {
                case DrawHandle.None: noneMouse.OnMouseMove(e); break;
                case DrawHandle.DispModel: dispModel.OnMouseMove(e); break;
                case DrawHandle.Erase:
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
            _hover = DrawHandle.None;
            DrawROI(shrFindMode, true);
        }

        private void btn_deleteRegion_Click(object sender, System.EventArgs e)
        {
            _hover = DrawHandle.None;
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
                display.DispRegion(findMode, HColor.Blue);
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
                display.DispRegion(shrFindMode, HColor.Green);
            }
            finally
            {
                drawRegion?.Dispose();
                regionResult?.Dispose();
            }
        }

        private void but_ApplyRegion_Click(object sender, System.EventArgs e)
        {
            eraseRect.SetUp(display, shrErase, shrFindMode, shrColor, shrLineWidth);
            _hover = DrawHandle.Erase;
        }

        public void DisplayModel(string modelPath, HObject ho_ModeRect, HObject ho_Contour, ModelResult result)
        {
            display.Reset();

            _srcImage.Dispose();
            HOperatorSet.ReadImage(out _srcImage, modelPath);
            display.DispImage(_srcImage);

            Point2d from = result.Coord.Center;
            Point2d to = display.HoCentre;

            shrFindMode.Dispose();
            TransObject(from, to, ho_ModeRect, out shrFindMode);
            display.DispRegion(shrFindMode, HColor.Blue);

            _shrContour.Dispose();
            TransObject(from, to, ho_Contour, out _shrContour);
            display.DispRegion(_shrContour, HColor.Green);

            HalconHelper.TransPixel(from, to, result.Row, result.Column, out HTuple rowTrans, out HTuple colTrans);
            _shrCoord = new CvCoord(colTrans, rowTrans, result.Angle);
            display.DispCross(colTrans, rowTrans, result.Angle, HColor.Red);

            dispModel.SetUp(display, shrFindMode, _shrContour, _shrCoord);
            _hover = DrawHandle.DispModel;
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

        private void HEditForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }

    }
}
