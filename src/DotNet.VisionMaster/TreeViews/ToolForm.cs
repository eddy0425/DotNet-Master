using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DotNet.VisionMaster
{
    public partial class ToolForm : UIForm
    {
        IWin32Window _owner;

        public ToolForm(IWin32Window owner)
        {
            InitializeComponent();
            _owner = owner;
        }

        private void ToolForm_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible)
            {
                Point point = new Point(500, 300);
                Form ownerForm = this.Owner;
                if (ownerForm != null && ownerForm.WindowState != FormWindowState.Maximized)
                {
                    point = new Point(ownerForm.Location.X + ownerForm.Width, ownerForm.Location.Y);
                }

                this.Location = point;
            }
        }
    }
}
