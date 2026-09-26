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
            // 绘制未结束就切换会出两件事: 下面的 DispROI → SetRectPara 会改写 DrawType,
            // 进行中的绘制会话从此收不到鼠标事件、一直卡到 5 分钟超时; 且本窗体已经切到新策略,
            // ParaForm 的索引却停在旧的, 之后 ParaForm 上的操作会静默作用到错误的策略上。
            // 所以必须在改动任何状态之前拦下来。
            if (_formPara.IsDrawBusy)
            {
                MessageBox.Show("当前正在绘制 ROI / 模板，请先在图像上右键确认或取消后再切换工具。");
                return;
            }

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
