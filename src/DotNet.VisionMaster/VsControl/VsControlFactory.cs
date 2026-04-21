using System.Reflection;
using System.Windows.Forms;
using DotNet.HalconAlgo;


namespace DotNet.VisionMaster
{
    public static class VsControlFactory
    {
        public static object GetControl(this Form form, string name)
        {
            return form.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(form);
        }

        public static void ShowTabs(this Form form, params TabPageEnum[] tabsToShow)
        {
            // 清空当前显示的标签页 TabControl
            var tabControl1 = (TabControl)form.GetControl("tabControl1");
            tabControl1.TabPages.Clear();

            for (int i = 0; i < tabsToShow.Length; i++)
            {
                switch (tabsToShow[i])
                {
                    case TabPageEnum.FileImage:
                        tabControl1.TabPages.Add((TabPage)form.GetControl("tabPage0"));
                        break;
                    case TabPageEnum.Parameter:
                        var tabPage = (TabPage)form.GetControl("tabPage1");
                        tabControl1.TabPages.Add(tabPage);
                        foreach (Control itemt in tabPage.Controls) itemt.Visible = false;  //关闭对应的显示
                        break;
                    case TabPageEnum.Region:
                        tabControl1.TabPages.Add((TabPage)form.GetControl("tabPage2"));
                        break;
                    case TabPageEnum.Matching:
                        tabControl1.TabPages.Add((TabPage)form.GetControl("tabPage3"));
                        break;
                    case TabPageEnum.Display:
                        tabControl1.TabPages.Add((TabPage)form.GetControl("tabPage4"));
                        break;
                }
            }

            // 默认显示第一个 TabPage
            tabControl1.SelectedIndex = 0;
        }

      
    }
}
