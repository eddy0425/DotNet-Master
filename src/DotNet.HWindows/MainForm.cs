using System;
using System.Windows.Forms;
using System.Collections.Generic;
using HalconDotNet;
using OpenCvSharp;
using DotNet.Library.Extension;

namespace DotNet.HWindows
{
    public partial class MainForm : Form
    {
        string systemFolder = @"C:\DotNet\ModifyModel";

        ModelType m_modelType = ModelType.ScaledShapeModel;

        HObject imgTemp;
        Form_HWDisPlay m_display;
        List<FindModelInfo> m_findModels;
        Form_ModelModify modifyTemplate;
        public int m_index { get { return comboBox1.SelectedIndex; } }

        /// <summary> 设置区域中心  </summary>
        public Point2d SetModelCenter { set; get; } = new Point2d();

        public MainForm()
        {
            InitializeComponent();

            m_display = new Form_HWDisPlay();
            m_display.Show();
            panel1.Controls.Add(m_display);
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            imgTemp?.Dispose();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            m_findModels = new List<FindModelInfo>();
            for (int i = 0; i < 10; i++)
            {
                FindModelInfo m_info = new FindModelInfo();
                m_info.LoadParameter(systemFolder, $"Test{i}");
                m_findModels.Add(m_info);
            }

            modifyTemplate = new Form_ModelModify();
            modifyTemplate.FormClosing += ModifyTemplate_FormClosing;

            comboBox1.SelectedIndex = 0;

            if (imgTemp == null)
            {
                imgTemp = new HObject();
                HOperatorSet.GenEmptyObj(out imgTemp);
                HOperatorSet.ReadImage(out imgTemp, "image\\TextImage.png");
                m_display.DispImage(imgTemp);
            }

        }
        private void ModifyTemplate_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (modifyTemplate.IsOk)
            {
                m_findModels[m_index].SaveParameter();
                m_display.ReDispImage();
                m_display.DispObj(m_findModels[m_index].SetROI.HoRegion, HColor.Red);
            }
        }
        private void button1_Click(object sender, EventArgs e)  //画查找区域
        {
            m_findModels[m_index].FindROI.Color = HColor.Blue;
            DrawRegion(m_findModels[m_index].FindROI,false);
            m_findModels[m_index].SaveParameter();
        }
        private void button2_Click(object sender, EventArgs e)  //新建模版
        {
            m_findModels[m_index].SetROI.Color = HColor.Red;
            DrawRegion(m_findModels[m_index].SetROI, true);
            SetTemplate(m_findModels[m_index],m_modelType);
            m_findModels[m_index].SaveParameter();

            SetModelCenter = m_findModels[m_index].SetROI.Centre;
        }
        private void button3_Click(object sender, EventArgs e)  //修改模版
        {
            m_findModels[m_index].SetROI.Color = HColor.Red;
            DrawRegion(m_findModels[m_index].SetROI,false);
            SetTemplate(m_findModels[m_index], m_modelType);
            m_findModels[m_index].SaveParameter();

            SetModelCenter = m_findModels[m_index].SetROI.Centre;
        }
        private void button4_Click(object sender, EventArgs e)  //编辑模板
        {
            modifyTemplate.ShowModifyTemplate(m_findModels[m_index].ModelPath,
               m_findModels[m_index].SetROI, SetModelCenter, m_findModels[m_index].ModelInfo, m_modelType);
        }

        private void button5_Click(object sender, EventArgs e)  //显示查找区域
        {
            m_display.ReDispImage();
            m_display.DispObj(m_findModels[m_index].FindROI.HoRegion, HColor.Blue);
        }
        private void button6_Click(object sender, EventArgs e)  //显示模版设置区域
        {
            m_display.ReDispImage();
            m_display.DispObj(m_findModels[m_index].SetROI.HoRegion, HColor.Red);
        }
      
        private void button7_Click(object sender, EventArgs e)  //查找模版
        {
            findScaledShapeModel(imgTemp, m_findModels[m_index],m_modelType);
        }
        private void button8_Click(object sender, EventArgs e)  //打开参数文件夹
        {
            System.Diagnostics.Process.Start(systemFolder);
        }

        /// <summary>
        /// 画区域
        /// </summary>
        /// <param name="region">区域</param>
        private void DrawRegion(CvRegion region,bool newModel)
        {
            try
            {
                m_display.ReDispImage();
                m_display.SetColor(HColor.Red);
                if (newModel)
                {
                    m_display.DrawRegion(region);
                }
                else
                    m_display.DrawRegionMod(region);

                m_display.DispRegion(region);
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
        }

        /// <summary>
        /// 设置模板
        /// </summary>
        private void SetTemplate(FindModelInfo info,ModelType type)
        {
            HObject imgReduced = new HObject();
            HObject contourModel = new HObject(); HOperatorSet.GenEmptyObj(out contourModel);

            try
            {
                HObject hImage = m_display.HoImage;
                HOperatorSet.GenEmptyObj(out imgReduced);
                HOperatorSet.ReduceDomain(hImage, info.SetROI.HoRegion, out imgReduced);

                //制作模板
                type.CreateModel(imgReduced,info.ModelInfo);
                type.FindModel2(hImage, info.ModelInfo,1, out ModelResult result);

                if (result.score.Length > 0)
                {
                    type.GetModelContours(info.ModelInfo.modelID, result, out contourModel);

                    CvOperatorSet.SaveSmallestRectImage(hImage, imgReduced, info.ModelPath);

                    m_display.ReDispImage();
                    m_display.DispObj(info.SetROI.HoRegion, HColor.Red);
                    m_display.DispObj(contourModel, HColor.Green);
                }
                else
                {
                    MessageBox.Show("新建模板失败！", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }

            imgReduced.Dispose();
            contourModel.Dispose();
        }

        private void findScaledShapeModel(HObject hImage, FindModelInfo info,ModelType type)
        {
            HObject imgReduced = new HObject(); HOperatorSet.GenEmptyObj(out imgReduced);
            HObject contourModel = new HObject(); HOperatorSet.GenEmptyObj(out contourModel);

            try
            {
                HOperatorSet.ReduceDomain(hImage, info.FindROI.HoRegion, out imgReduced);

                type.FindModel(imgReduced, info.ModelInfo, out List<ModelResult> results);

                m_display.ReDispImage();
                m_display.DispObj(info.FindROI.HoRegion, HColor.Blue);
                m_display.DispObj(info.SetROI.HoRegion, HColor.Red);

                for (int i = 0; i < results.Count; i++)
                {
                    type.GetModelContours(info.ModelInfo.modelID, results[i], out contourModel);

                    m_display.DispObj(contourModel, HColor.Green);

                    m_display.DispCross(results[i].column.D, results[i].row.D, results[i].angle.D.ToDegrees(), 50, HColor.Red);
                    m_display.DispText((i + 1).ToString(), results[i].column.D, results[i].row.D, HColor.Green);
                }
            }
            catch (Exception ex) { MessageBox.Show("findScaledShapeModel:" + ex.Message); }

            imgReduced.Dispose();
            contourModel.Dispose();
        }

        
    }
}
