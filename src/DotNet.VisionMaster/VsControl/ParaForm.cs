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
        DisplayUI _disPlay;
        Form_Value _form_Value;

        public ParaForm(DisplayUI displayUI)
        {
            InitializeComponent();

            _disPlay = displayUI;
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
            //FitArcMidpointStrategy fitArcMidpoint = (FitArcMidpointStrategy)_strategys[_index];
            //var inPara = ((FitArcMidpointStrategy)_strategys[_index]).inPara;

            switch (_strategys[_index].Name)
            {
                case "圆弧中点":
                    {
                        //var inPara = ((FitArcMidpointStrategy)_strategys[_index]).inPara;
                        //var hRegion = inPara.HoRect;

                        //_disPlay.SetDrawMode(_name, DrawEnum.None);

                        //_disPlay.Reset();
                        //_disPlay.ReDispImage();
                        //_disPlay.SetColor(HColor.Red);
                        //HOperatorSet.DrawRectangle2(_disPlay.HoWindow, out HTuple row, out HTuple column, out HTuple phi, out HTuple length1, out HTuple length2);
                        //HOperatorSet.GenRectangle2(out hRegion.InRegion, row, column, phi, length1, length2);

                        //hRegion.UpdateCenter(new Point2d(column.D, row.D), new Size2d(length1.D * 2, length2.D * 2));
                        //hRegion.Phi = phi;

                        //_disPlay.DispRegion(hRegion);

                        _disPlay.SetDrawMode(_name, DrawEnum.NewAffRect);
                    }
                    break;
                default:
                    _disPlay.SetDrawMode(_name, DrawEnum.NewRect);
                    break;
            }
        }

        private void but_updataRegion_Click(object sender, EventArgs e)
        {
            _disPlay.SetDrawMode(_name, DrawEnum.EditRect);
        }

        private void btn_setCoordIn_Click(object sender, EventArgs e)
        {
            switch (_strategys[_index].Name)
            {
                case "��״ƥ��":
                case "ֱ�߲���":
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
                case "��״ƥ��":
                case "ֱ�߲���":
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
                case "��״ƥ��":
                case "ֱ�߲���":
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

            //if (btn_rectangle1_2.Checked) inPara.SetROI.Type = DrawForm.����;
            //else if (btn_rectangle2_2.Checked) inPara.SetROI.Type = DrawForm.�������;
            //else if (btn_circle_2.Checked) inPara.SetROI.Type = DrawForm.Բ;
            //else if (btn_oval_2.Checked) inPara.SetROI.Type = DrawForm.��Բ;
            //else if (btn_polygon_2.Checked) inPara.SetROI.Type = DrawForm.�����;

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
                case "ShapeMode":
                    {
                        _disPlay.SetDrawMode("ShapeMode", DrawEnum.SetModel);
                    }
                    break;
            }
         

      
        }

    }
}
