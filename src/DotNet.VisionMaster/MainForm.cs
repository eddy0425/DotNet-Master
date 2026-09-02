using System;
using DotNet.HalconUI;
using DotNet.Vision.Abstractions;
using DotNet.HalconAlgo;
using System.Windows.Forms;
using System.Collections.Generic;


namespace DotNet.VisionMaster
{
    public partial class MainForm : Form
    {
        HDisplayUI _display;
        ParaForm _formPara;
        private int _index;
        private IParaStrategy _currentStrategy => _strategys[_index];
        private List<IParaStrategy> _strategys = new List<IParaStrategy>();
        private readonly Dictionary<string, VsControlModel> _vsControls = new Dictionary<string, VsControlModel>();

        public MainForm()
        {
            InitializeComponent();
            AlgoPaths.UIBlock = false;

            _display = new HDisplayUI();
            panel1.Controls.Add(_display);

            _formPara = new ParaForm(_display);
            panel2.Controls.Add(_formPara);

            _strategys.Add(new FileImageStrategy());
            _strategys.Add(new CreateROIStrategy());
            _strategys.Add(new ShapeModelStrategy());
            _strategys.Add(new FitLineStrategy());
            _strategys.Add(new FitArcMidpointStrategy());
            _strategys.Add(new NccModelStrategy());
            _strategys.Add(new ScaledModelStrategy());
            _strategys.Add(new GenericModelStrategy());

            for (int i = 0; i < _strategys.Count; i++)
            {
                _strategys[i].Init(_display);
            }

            LogFile logFile = new LogFile();

            var fileImage = ((FileImageStrategy)_strategys[0]).inPara;
            fileImage.ImageFolder = "D:\\testImage\\FitArcMidpoint";
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
        private void button5_Click(object sender, EventArgs e)
        {
            SwitchStrategy(4);
        }
        private void button6_Click(object sender, EventArgs e)
        {
            SwitchStrategy(5);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            SwitchStrategy(6);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            SwitchStrategy(7);
        }

        /// <summary>
        /// 切换算法策略：解绑旧控件 → 清空 → 设置新策略 → 显示新参数
        /// </summary>
        private void SwitchStrategy(int index)
        {
            _index = index;
            _vsControls.ClearAll();
            _formPara.SelectPara(_index, _strategys);
            if (_currentStrategy is IParaBinding binding)
                binding.DispPara(new WinFormsParaUiHost(_formPara, _vsControls));
            if (_currentStrategy is IRoiEditable roi)
                roi.DispROI(_display);
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

                if (_currentStrategy is IParaBinding binding)
                    binding.SavePara(new WinFormsParaUiHost(_formPara, _vsControls));
                _currentStrategy.Fun_action(_display.Display, _strategys);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        
    }
}
