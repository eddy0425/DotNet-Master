using DotNet.HalconAlgo;
using DotNet.HalconUI;
using DotNet.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace DotNet.VisionMaster
{
    public partial class CreateROIForm : Form
    {
        HDisplayUI _display;
        ParaForm _formPara;
        private int _index;
        private IParaStrategy _currentStrategy => _strategys[_index];
        private List<IParaStrategy> _strategys = new List<IParaStrategy>();
        private readonly Dictionary<string, VsControlModel> _vsControls = new Dictionary<string, VsControlModel>();


        public CreateROIForm()
        {
            InitializeComponent();
            AlgoPaths.UIBlock = false;

            _display = new HDisplayUI();
            panel1.Controls.Add(_display);

            _formPara = new ParaForm(_display);
            panel2.Controls.Add(_formPara);

            string path1 = Path.Combine("D:\\", "Recipes", "Data", "Pick", "mapVision1.json");
            var shapeModel = new ShapeModelStrategy();
            DotNet.Data.JsonHelper.Load(path1, out shapeModel);

            _strategys.Add(new FileImageStrategy());
            _strategys.Add(shapeModel);
            _strategys.Add(new CreateROIStrategy());
            _strategys.Add(new FitLineStrategy());

            for (int i = 0; i < _strategys.Count; i++)
            {
                _strategys[i].Init(_display);
                _strategys[i].RunIndex = i;
            }

            LogFile logFile = new LogFile();

            var fileImage = ((FileImageStrategy)_strategys[0]).inPara;
            fileImage.ImageFolder = "D:\\testImage\\123";

            //ShapeModelStrategy strategy1 = (ShapeModelStrategy)_strategys[1];
            //strategy1.inPara = shapeModel;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SwitchStrategy(0);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            SwitchStrategy(1);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            SwitchStrategy(2);
        }
        private void button4_Click(object sender, EventArgs e)
        {
            SwitchStrategy(3);
        }

        /// <summary>
        /// 切换算法策略：解绑旧控件 → 清空 → 设置新策略 → 显示新参数
        /// </summary>
        private void SwitchStrategy(int index)
        {
            _index = index;
            _vsControls.ClearAll();
            _formPara.SelectPara(_index, _strategys);
            _currentStrategy.DispPara(_formPara, _vsControls);
            _currentStrategy.DispROI(_display);
        }

        private void but_Run_Click(object sender, EventArgs e)
        {
            try
            {
                _display.ReDispImage();

                switch (_strategys[_index].Name)
                {
                    case "ShapeMode":
                        {
                            //_display.SetDrawMode("ShapeMode", DrawEnum.DispModel);
                        }
                        break;
                }

                _currentStrategy.SavePara(_formPara, _vsControls);
                _currentStrategy.Fun_action(_display, _strategys);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void but_Cycle_Click(object sender, EventArgs e)
        {
            try
            {
                for (int i = 0; i < _strategys.Count; i++)
                {
                    _strategys[i].Fun_action(_display, _strategys);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button4_old(object sender, EventArgs e)
        {
            string path1 = Path.Combine("D:\\", "Recipes", "Data", "Pick", "mapVision1.json");

            var shapeModelStrategy = _strategys[1] as ShapeModelStrategy;

            DotNet.Data.JsonHelper.Save(path1, shapeModelStrategy);
        }
    }
}
