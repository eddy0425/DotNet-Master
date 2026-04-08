using System;
using System.Windows.Forms;
using System.Collections.Generic;


namespace DotNet.VisionMaster
{
    public partial class ParaForm : Form
    {
        int _index;
        string _name;
        List<IParaStrategy> _strategys;
        DisplayForm _display;
        Form_Value _form_Value;

        public ParaForm(DisplayForm displayForm)
        {
            InitializeComponent();

            _display = displayForm;
            _form_Value = new Form_Value();
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
            _name = strategys[_index].Name;
        }

        private void btn_drawRegion_Click(object sender, EventArgs e)
        {
            _display.SetDrawMode(_name, WinDrawType.NewRect);
        }

        private void btn_setCoordIn_Click(object sender, EventArgs e)
        {
            switch (_strategys[_index].Name)
            {
                case "形状匹配":
                case "直线查找":
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

        private void btn_100_Click(object sender, EventArgs e)
        {
            switch (_strategys[_index].Name)
            {
                case "形状匹配":
                case "直线查找":
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

        private void btn_101_Click(object sender, EventArgs e)
        {
            switch (_strategys[_index].Name)
            {
                case "形状匹配":
                case "直线查找":
                    {
                        _form_Value.setValueForm(_index, _strategys, cmb_101.Text, OutEnum.Region);
                        if (_form_Value.DialogResult == DialogResult.OK)
                        {
                            cmb_101.Text = _form_Value.StrReturn;
                        }
                    }
                    break;
            }
        }

       
    }
}
