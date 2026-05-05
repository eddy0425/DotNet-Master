using DotNet.Drawing;
using DotNet.HalconAlgo;
using DotNet.HalconUI;
using HalconDotNet;
using System;
using System.Collections.Generic;
using System.Windows.Forms;


namespace DotNet.VisionMaster
{
    public partial class ParaForm : Form
    {
        int _index;
        List<IParaStrategy> _strategys;
        DisplayUI _disPlay;
        HModelForm _hModel;
        ValueForm _form_Value;
        HEditForm _editModel;

        public ParaForm(DisplayUI displayUI)
        {
            InitializeComponent();

            _disPlay = displayUI;
            _hModel = new HModelForm();
            _editModel = new HEditForm();
            _form_Value = new ValueForm();

            panel1.Controls.Add(_hModel);
            _disPlay.ModelEvent += _disPlay_ModelEvent;
            _disPlay.DrawDoneEvent += _disPlay_DrawDoneEvent;
        }

        private void _disPlay_ModelEvent(object sender, DrawModelArgs e)
        {
            throw new NotImplementedException();
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

        public void SelectPara(int index, List<IParaStrategy> strategys)
        {
            _index = index;
            _strategys = strategys;
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
                        {
                            _disPlay.SetDrawMode(strategy.Name, DrawEnum.NewAffRect);
                        }
                        break;
                    default:
                        _disPlay.SetDrawMode(strategy.Name, DrawEnum.NewRect);
                        break;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        private void but_updataRegion_Click(object sender, EventArgs e)
        {
            try
            {
                var strategy = _strategys[_index];
                _disPlay.SetDrawMode(strategy.Name, DrawEnum.EditRect);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
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

        private void btn_newModel_Click(object sender, EventArgs e)
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
                        {
                            _disPlay.SetDrawMode(strategy.Name, DrawEnum.SetModel);
                        }
                        break;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }

        }

        private void _disPlay_DrawDoneEvent(object sender, DrawModelUIArgs e)
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


        private void but_editModel_Click(object sender, EventArgs e)
        {
            try
            {
                var filePath = AlgoPaths.JobDir + _index + "\\matching.bmp";
                var strategy = _strategys[_index];
                switch (strategy.Algorithm)
                {
                    case AlgoEnum.ShapeModel:
                        {
                            _editModel.Show();
                            _editModel.ShowModifyTemplate(filePath);
                            //_disPlay.SetDrawMode(strategy.Name, DrawEnum.NewAffRect);
                        }
                        break;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }
    }
}
