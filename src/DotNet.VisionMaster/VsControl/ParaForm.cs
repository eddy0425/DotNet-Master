using System;
using System.Collections.Generic;
using System.Windows.Forms;


namespace DotNet.VisionMaster
{
    public partial class ParaForm : Form
    {
        int _index;
        string _name;
        List<IParaStrategy> _strategys;
        DisplayUI _display;
        Form_Value _form_Value;

        public ParaForm(DisplayUI displayUI)
        {
            InitializeComponent();

            _display = displayUI;
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
            _display.SetDrawMode(_name, DrawEnum.NewRect);
        }

        private void but_updataRegion_Click(object sender, EventArgs e)
        {
            _display.SetDrawMode(_name, DrawEnum.EditRect);
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

        private void btn_newModel_Click(object sender, EventArgs e)
        {
            //Edit_savePara();

            ////Matching inPara = jobPara.jobInfo.ListMatchings[ListIndex];
            //DisplayForm display = jobPara.disPlay;

            //if (btn_rectangle1_2.Checked) inPara.SetROI.Type = DrawForm.矩形;
            //else if (btn_rectangle2_2.Checked) inPara.SetROI.Type = DrawForm.仿射矩形;
            //else if (btn_circle_2.Checked) inPara.SetROI.Type = DrawForm.圆;
            //else if (btn_oval_2.Checked) inPara.SetROI.Type = DrawForm.椭圆;
            //else if (btn_polygon_2.Checked) inPara.SetROI.Type = DrawForm.多边型;

            //if (inPara.SetROI.IsDefault())
            //{
            //    Point2d centre = new Point2d(display.ho_Width / 2, display.ho_Height / 2);
            //    Size2d size = new Size2d(display.ho_Width / 4, display.ho_Height / 4);
            //    inPara.SetROI.UpdateCentre(centre, size);
            //}

            //display.DrawDispRegion(inPara.SetROI);
            //SetTemplate(display, inPara.SetROI, inPara.ModelInfo);

            switch (_strategys[_index].Name)
            {
                case "形状匹配":
                    {
                        ShapeMatchingStrategy shapeMatching = (ShapeMatchingStrategy)_strategys[_index];

                        var inPara = ((ShapeMatchingStrategy)_strategys[_index]).inPara;
                        var modeRect = inPara.ModeRect;

                        _display.SetDrawMode(_name, DrawEnum.NewRect);
                        //_displayCore.DrawDispRegion(modeRect);
                        shapeMatching.SetTemplate(_display, _strategys, modeRect);
                    }
                    break;
            }
         

      
        }

    }
}
