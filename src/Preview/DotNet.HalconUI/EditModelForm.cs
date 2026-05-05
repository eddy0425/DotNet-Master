using DotNet.Drawing;
using HalconDotNet;
using System.Windows.Forms;


namespace DotNet.HalconUI
{
    public partial class EditModelForm : Form
    {
        public string ShrColor => CB_ApplyColor.Text;
        public int ShrLineWidth => CB_ApplyLineWidth.Text.ExtractNumber();


        public DisplayUI GetDisplay() => display;

        public EditModelForm()
        {
            InitializeComponent();
        }

        private void EditModelForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }

        private void EditModelForm_Load(object sender, System.EventArgs e)
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
            display.SetDrawMode("", DrawEnum.NewRect);
        }

        private void btn_deleteRegion_Click(object sender, System.EventArgs e)
        {

        }

        private void but_ApplyRegion_Click(object sender, System.EventArgs e)
        {
            display.SetDrawMode("", DrawEnum.EraseRect);
        }

        public void ShowModifyTemplate(string modelPath)
        {
            HObject srcImage; HOperatorSet.GenEmptyObj(out srcImage);
            try
            {
                //IsOk = false;
                //if (this.Visible)
                //{
                //    this.Close();
                //}

                //this.Show();
                //info = _info;
                //CB_LockCenter.Checked = _info.LockCenter;
                //m_type = _type;

                //if (srcImage.NotNull()) { srcImage.Dispose(); HOperatorSet.GenEmptyObj(out srcImage); }

                srcImage.Dispose();
                HOperatorSet.ReadImage(out srcImage, modelPath);
                display.DispImage(srcImage);

                //findModeRegion = _findRegion;
                //setModeCenter = _setModeCenter;

                //CvOperatorSet.TransRegion(setModeCenter, disPlay.HoCentre, findModeRegion.HoRegion, out findModeRegion.InRegion);

                ////参数备份
                //oldModelID = info.modelID.Clone();

                //m_type.FindModel2(srcImage, info, 1, out ModelResult oldResult); if (oldResult.score.Length <= 0) throw new Exception($"查找模板失败！!");
                //LockCenter = new CvCoord(oldResult.X, oldResult.Y, oldResult.angle);

                //SetTemplate(srcImage, findModeRegion, info, m_type);
            }
            catch
            {
                this.Hide();
                throw;
            }
            finally
            {
                srcImage.Dispose();
            }
        }
    }
}
