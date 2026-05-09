using DotNet.Drawing;
using HalconDotNet;
using System.Drawing;
using System.Windows.Forms;


namespace DotNet.HalconUI
{
    public partial class HEditForm : Form
    {
        string shrColor => CB_ApplyColor.Text;
        int shrLineWidth => CB_ApplyLineWidth.Text.ExtractNumber();
        private enum DrawHandle { None, Erase, DisPlay }

        HObject _srcImage;
        HObject shrErase;
        HObject shrFindMode;
        HObject _shrContour;
        DrawHandle _hover;

        EraseRectHandler eraseRect;
        public DisplayUI GetDisplay() => display;


        public HEditForm()
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

            display.HMouseDown += (s, e) => DrawHelper.Active?.OnMouseDown(e);
            display.HMouseUp += (s, e) => DrawHelper.Active?.OnMouseUp(e);
            display.HMouseMove += (s, e) => DrawHelper.Active?.OnMouseMove(e);

            eraseRect = new EraseRectHandler();
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
                case DrawHandle.Erase: eraseRect.OnMouseDown(e); break;
            }
        }

        public void OnMouseUp(object sender, HMouseEventArgs e)
        {
            switch (_hover)
            {
                case DrawHandle.Erase: eraseRect.OnMouseUp(e); break;
            }
        }

        public void OnMouseWheel(object sender, HMouseEventArgs e)
        {
            switch (_hover)
            {
                case DrawHandle.Erase: eraseRect.OnMouseWheel(e); break;
            }
        }

        public void OnMouseMove(object sender, HMouseEventArgs e)
        {
            switch (_hover)
            {
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
            DrawROI(shrFindMode, true, out HObject region);
            shrFindMode.Dispose();
            region = shrFindMode;
            display.DispRegion(shrFindMode, HColor.Blue);
        }

        private void btn_deleteRegion_Click(object sender, System.EventArgs e)
        {
            _hover = DrawHandle.None;
            DrawROI(shrFindMode, false, out HObject region);
            shrFindMode.Dispose();
            region = shrFindMode;
            display.DispRegion(shrFindMode, HColor.Blue);
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
        private void DrawROI(HObject findMode, bool IsAdd, out HObject region)
        {
            HOperatorSet.GenEmptyObj(out region);
            HObject drawRegion; HOperatorSet.GenEmptyObj(out drawRegion);
            try
            {
                display.Reset();
                display.ReDispImage();
                display.DispRegion(findMode, HColor.Blue);

                var drawType = GetModifyShape();

                if (IsAdd)
                {
                    display.DrawRegion(drawType, out drawRegion);
                    HOperatorSet.Union2(findMode, drawRegion, out region);

                    display.ReDispImage();
                    display.DispRegion(drawRegion, HColor.Green);
                }
                else
                {
                    display.DrawRegion(drawType, out drawRegion);
                    HOperatorSet.Difference(findMode, drawRegion, out region);

                    display.ReDispImage();
                    display.DispRegion(drawRegion, HColor.Red);
                }
            }
            finally
            {
                drawRegion.Dispose();
            }
        }

        private void but_ApplyRegion_Click(object sender, System.EventArgs e)
        {
            _hover = DrawHandle.Erase;
            eraseRect.SetUp(display, shrErase, shrFindMode, shrColor, shrLineWidth);
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

        private void HEditForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }

    }
}
