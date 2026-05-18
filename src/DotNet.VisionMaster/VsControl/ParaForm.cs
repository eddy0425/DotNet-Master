using DotNet.Drawing;
using DotNet.HalconUI;
using DotNet.HalconAlgo;
using HalconDotNet;
using System;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;


namespace DotNet.VisionMaster
{
    public partial class ParaForm : Form
    {
        int _index;
        List<IParaStrategy> _strategys;
        HDisplayUI _disPlay;
        HModelUI _hModel;
        ValueForm _form_Value;
        HEditModelUI _editModel;

        Dictionary<RadioButton, RectEnum> _rectDrawMap;
        Dictionary<RadioButton, RectEnum> _modelDrawMap;

        public ParaForm(HDisplayUI displayUI)
        {
            InitializeComponent();

            _disPlay = displayUI;
            _hModel = new HModelUI();
            _editModel = new HEditModelUI();
            _form_Value = new ValueForm();

            panel1.Controls.Add(_hModel);
            _disPlay.DrawDoneEvent += DrawDoneEvent;

            _rectDrawMap = new Dictionary<RadioButton, RectEnum>
            {
                { btn_rectRectangle, RectEnum.Rectangle },
                { btn_rectAffRect,   RectEnum.AffRect },
                { btn_rectCircle,    RectEnum.Circle },
                { btn_rectEllipse,   RectEnum.Ellipse },
                { btn_rectPolygon,   RectEnum.Polygon },
            };

            _modelDrawMap = new Dictionary<RadioButton, RectEnum>
            {
                { btn_modelRectangle, RectEnum.Rectangle },
                { btn_modelAffRect,   RectEnum.AffRect },
                { btn_modelCircle,    RectEnum.Circle },
                { btn_modelEllipse,   RectEnum.Ellipse },
                { btn_modelPolygon,   RectEnum.Polygon },
            };

        }

        public void SelectPara(int index, List<IParaStrategy> strategys)
        {
            _index = index;
            _strategys = strategys;
        }
        private void btn_100_Click(object sender, EventArgs e)
        {
            try
            {
                var strategy = _strategys[_index];
                switch (strategy.Algorithm)
                {
                    case AlgoEnum.ShapeModel:
                    case AlgoEnum.NccModel:
                    case AlgoEnum.ScaledModel:
                    case AlgoEnum.GenericModel:
                    case AlgoEnum.FitLine:
                    case AlgoEnum.FitArcMidpoint:
                    case AlgoEnum.LineRotImage:
                    case AlgoEnum.RotateImage:
                        {
                            _form_Value.setValueForm(_index, _strategys, cmb_100.Text, OutEnum.Image);
                            if (_form_Value.DialogResult == DialogResult.OK)
                            {
                                cmb_100.Text = _form_Value.StrReturn;
                            }
                        }
                        break;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        private void btn_101_Click(object sender, EventArgs e)
        {
            try
            {
                var strategy = _strategys[_index];
                switch (strategy.Algorithm)
                {
                    case AlgoEnum.ShapeModel:
                    case AlgoEnum.NccModel:
                    case AlgoEnum.ScaledModel:
                    case AlgoEnum.GenericModel:
                    case AlgoEnum.FitLine:
                    case AlgoEnum.FitArcMidpoint:
                        {
                            _form_Value.setValueForm(_index, _strategys, cmb_101.Text, OutEnum.Region);
                            if (_form_Value.DialogResult == DialogResult.OK)
                            {
                                cmb_101.Text = _form_Value.StrReturn;
                            }
                        }
                        break;
                    case AlgoEnum.LineRotImage:
                        {
                            _form_Value.setValueForm(_index, _strategys, cmb_101.Text, OutEnum.Line);
                            if (_form_Value.DialogResult == DialogResult.OK)
                            {
                                cmb_101.Text = _form_Value.StrReturn;
                            }
                        }
                        break;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void btn_setPath_Click(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            if (button == btn_setPath)
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                try { fbd.SelectedPath = cmb_ImageFolder.Text; } catch { }
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    cmb_ImageFolder.Text = fbd.SelectedPath;
                }
            }
            else
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                try { fbd.SelectedPath = cmb_115.Text; } catch { }
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    cmb_115.Text = fbd.SelectedPath;
                }
            }
        }
        private void btn_openPath_Click(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            if (button == btn_openPath)
            {
                try
                {
                    System.Diagnostics.Process.Start(cmb_ImageFolder.Text);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
            else
            {
                try
                {
                    System.Diagnostics.Process.Start(cmb_115.Text);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
        }
        private void btn_setCoordIn_Click(object sender, EventArgs e)
        {
            try
            {
                var strategy = _strategys[_index];
                switch (strategy.Algorithm)
                {
                    case AlgoEnum.CreateROI:
                    case AlgoEnum.ShapeModel:
                    case AlgoEnum.NccModel:
                    case AlgoEnum.ScaledModel:
                    case AlgoEnum.GenericModel:
                    case AlgoEnum.FitLine:
                    case AlgoEnum.FitArcMidpoint:
                    case AlgoEnum.RotateImage:
                        {
                            _form_Value.setValueForm(_index, _strategys, cmb_CoordIn.Text, OutEnum.Coord);
                            if (_form_Value.DialogResult == DialogResult.OK)
                            {
                                cmb_CoordIn.Text = _form_Value.StrReturn;
                            }
                        }
                        break;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        private void btn_drawRegion_Click(object sender, EventArgs e)
        {
            try
            {
                var strategy = _strategys[_index];
                switch (strategy.Algorithm)
                {
                    case AlgoEnum.FitLine:
                    case AlgoEnum.FitArcMidpoint:
                        btn_rectAffRect.Checked = true;
                        break;
                    default:
                        btn_rectRectangle.Checked = true;
                        break;
                }
                var drawType = _rectDrawMap.FirstOrDefault(kv => kv.Key.Checked).Value;
                _disPlay.ReDispImage();
                strategy.DrawROI(_disPlay, drawType, true);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        private void but_editRegion_Click(object sender, EventArgs e)
        {
            try
            {
                var strategy = _strategys[_index];
                _disPlay.ReDispImage();
                var drawType = _rectDrawMap.FirstOrDefault(kv => kv.Key.Checked).Value;
                strategy.DrawROI(_disPlay, drawType, false);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        private void btn_newModel_Click(object sender, EventArgs e)
        {
            try
            {
                var strategy = _strategys[_index];
                var drawType = _modelDrawMap.FirstOrDefault(kv => kv.Key.Checked).Value;
                _disPlay.ReDispImage();
                strategy.SetTemplate(_disPlay, drawType, true);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }

        }

        private void but_modifyModel_Click(object sender, EventArgs e)
        {
            try
            {
                var strategy = _strategys[_index];
                var drawType = _modelDrawMap.FirstOrDefault(kv => kv.Key.Checked).Value;
                _disPlay.ReDispImage();
                strategy.SetTemplate(_disPlay, drawType, false);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        private void but_editModel_Click(object sender, EventArgs e)
        {
            try
            {
                var strategy = _strategys[_index];
                switch (strategy.Algorithm)
                {
                    case AlgoEnum.ShapeModel:
                        {
                            var inPara = ((ShapeModelStrategy)strategy).inPara;
                            var modelPath = inPara.ModelPath;
                            var modeRect = inPara.ModeRect.HoRegion;
                            var contour = inPara.HoContour;
                            var result = inPara.Results[0];
                            _editModel.Show();
                            _editModel.DisplayModel(modelPath, modeRect, contour, result);
                        }
                        break;
                    case AlgoEnum.NccModel:
                        {
                            var inPara = ((NccModelStrategy)strategy).inPara;
                            var modelPath = inPara.ModelPath;
                            var modeRect = inPara.ModeRect.HoRegion;
                            var contour = inPara.HoContour;
                            var result = inPara.Results[0];
                            _editModel.Show();
                            _editModel.DisplayModel(modelPath, modeRect, contour, result);
                        }
                        break;
                    case AlgoEnum.ScaledModel:
                        {
                            var inPara = ((ScaledModelStrategy)strategy).inPara;
                            var modelPath = inPara.ModelPath;
                            var modeRect = inPara.ModeRect.HoRegion;
                            var contour = inPara.HoContour;
                            var result = inPara.Results[0];
                            _editModel.Show();
                            _editModel.DisplayModel(modelPath, modeRect, contour, result);
                        }
                        break;
                    case AlgoEnum.GenericModel:
                        {
                            var inPara = ((GenericModelStrategy)strategy).inPara;
                            var modelPath = inPara.ModelPath;
                            var modeRect = inPara.ModeRect.HoRegion;
                            var contour = inPara.HoContour;
                            var result = inPara.Results[0];
                            _editModel.Show();
                            _editModel.DisplayModel(modelPath, modeRect, contour, result);
                        }
                        break;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
        private void DrawDoneEvent(object sender, DrawModelUIArgs e)
        {
            try
            {
                var strategy = _strategys[_index];
                switch (strategy.Algorithm)
                {
                    case AlgoEnum.ShapeModel:
                        {
                            var inPara = ((ShapeModelStrategy)strategy).inPara;
                            _hModel.DisplayModel(inPara.ModelPath, e.HoModeRect, e.HoContour, e.Result);
                        }
                        break;
                    case AlgoEnum.NccModel:
                        {
                            var inPara = ((NccModelStrategy)strategy).inPara;
                            _hModel.DisplayModel(inPara.ModelPath, e.HoModeRect, e.HoContour, e.Result);
                        }
                        break;
                    case AlgoEnum.ScaledModel:
                        {
                            var inPara = ((ScaledModelStrategy)strategy).inPara;
                            _hModel.DisplayModel(inPara.ModelPath, e.HoModeRect, e.HoContour, e.Result);
                        }
                        break;
                    case AlgoEnum.GenericModel:
                        {
                            var inPara = ((GenericModelStrategy)strategy).inPara;
                            _hModel.DisplayModel(inPara.ModelPath, e.HoModeRect, e.HoContour, e.Result);
                        }
                        break;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }

        }

    }
}
