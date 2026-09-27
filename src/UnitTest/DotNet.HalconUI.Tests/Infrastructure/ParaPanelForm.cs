using System.Reflection;
using System.Windows.Forms;

namespace DotNet.HalconUI.Tests
{
    /// <summary>
    /// 参数面板替身：按 Designer 生成代码的方式把控件声明为<b>私有字段</b>，
    /// 供 <see cref="VsControlFactory.GetControl"/> 反射查找。
    /// </summary>
    /// <remarks>
    /// 必须在 STA 线程上创建（见 <see cref="Sta"/>）。构造后会强制创建全部子控件，
    /// 使 DataBindings 立即生效。
    /// </remarks>
    internal sealed class ParaPanelForm : Form
    {
#pragma warning disable IDE0044, CS0414 // 字段由反射读取，模拟 Designer 生成的私有字段
        private TabControl tabControl1 = new TabControl();
        private TabPage tabPage0 = new TabPage("文件图像");
        private TabPage tabPage1 = new TabPage("参数");
        private TabPage tabPage2 = new TabPage("区域");
        private TabPage tabPage3 = new TabPage("匹配");
        private TabPage tabPage4 = new TabPage("显示");

        private CheckBox ckb_disp0 = new CheckBox();
        private CheckBox ckb_disp1 = new CheckBox();
        private CheckBox ckb_disp2 = new CheckBox();
        private CheckBox ckb_disp3 = new CheckBox();
        private CheckBox ckb_disp4 = new CheckBox();

        private Label lbl_Name = new Label();
        private Button but_Apply = new Button();
        private TextBox txt_Value = new TextBox();
        private ComboBox cmb_Mode = new ComboBox();
        private CheckBox ckb_Enable = new CheckBox();
        private RadioButton rdb_Dark = new RadioButton();
        private TrackBar trackBar1 = new TrackBar { Minimum = 0, Maximum = 255 };
        private GroupBox groupBox1 = new GroupBox();
        private DataGridView dataGridView1 = new DataGridView();

        /// <summary>类型不匹配的字段：名字对得上，但不是策略期望的控件类型。</summary>
        private string notAControl = "x";
#pragma warning restore IDE0044, CS0414

        public ParaPanelForm()
        {
            ShowInTaskbar = false;

            tabControl1.TabPages.AddRange(new[] { tabPage0, tabPage1, tabPage2, tabPage3, tabPage4 });
            tabPage1.Controls.AddRange(new Control[] { lbl_Name, but_Apply, txt_Value, cmb_Mode, ckb_Enable, rdb_Dark, trackBar1, groupBox1, dataGridView1 });
            tabPage4.Controls.AddRange(new Control[] { ckb_disp0, ckb_disp1, ckb_disp2, ckb_disp3, ckb_disp4 });
            Controls.Add(tabControl1);

            ForceCreate(this);
        }

        // 未选中的 TabPage 及其子控件不可见，公开的 CreateControl() 会跳过它们；
        // 而 WinForms 只有在控件 Created 之后才激活 DataBindings。借内部重载忽略可见性强制创建。
        private static readonly MethodInfo CreateControlIgnoreVisible =
            typeof(Control).GetMethod("CreateControl", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(bool) }, null);

        private static void ForceCreate(Control control)
        {
            CreateControlIgnoreVisible.Invoke(control, new object[] { true });
            foreach (Control child in control.Controls) ForceCreate(child);
        }

        public TabControl Tabs => tabControl1;
        public TabPage Page(int index) => new[] { tabPage0, tabPage1, tabPage2, tabPage3, tabPage4 }[index];
        public CheckBox DispCheckBox(int index) => new[] { ckb_disp0, ckb_disp1, ckb_disp2, ckb_disp3, ckb_disp4 }[index];

        public Label Label => lbl_Name;
        public Button Button => but_Apply;
        public TextBox TextBox => txt_Value;
        public ComboBox ComboBox => cmb_Mode;
        public CheckBox CheckBox => ckb_Enable;
        public RadioButton RadioButton => rdb_Dark;
        public TrackBar TrackBar => trackBar1;
        public GroupBox GroupBox => groupBox1;
        public DataGridView Grid => dataGridView1;
    }
}
