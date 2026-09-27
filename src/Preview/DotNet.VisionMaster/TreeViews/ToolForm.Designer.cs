namespace DotNet.VisionMaster
{
    partial class ToolForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.treeView_Tool = new System.Windows.Forms.TreeView();
            this.label_Note = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // treeView_Tool
            // 
            this.treeView_Tool.AllowDrop = true;
            this.treeView_Tool.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(40)))));
            this.treeView_Tool.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.treeView_Tool.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treeView_Tool.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.treeView_Tool.ForeColor = System.Drawing.Color.White;
            this.treeView_Tool.FullRowSelect = true;
            this.treeView_Tool.ItemHeight = 33;
            this.treeView_Tool.LineColor = System.Drawing.Color.DodgerBlue;
            this.treeView_Tool.Location = new System.Drawing.Point(0, 31);
            this.treeView_Tool.Margin = new System.Windows.Forms.Padding(3, 25, 3, 3);
            this.treeView_Tool.Name = "treeView_Tool";
            this.treeView_Tool.ShowLines = false;
            this.treeView_Tool.Size = new System.Drawing.Size(251, 449);
            this.treeView_Tool.TabIndex = 4;
            // 
            // label_Note
            // 
            this.label_Note.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(40)))), ((int)(((byte)(30)))));
            this.label_Note.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.label_Note.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label_Note.Location = new System.Drawing.Point(0, 480);
            this.label_Note.Name = "label_Note";
            this.label_Note.Size = new System.Drawing.Size(251, 45);
            this.label_Note.TabIndex = 1;
            this.label_Note.Text = "提示：";
            // 
            // ToolForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(40)))));
            this.ClientSize = new System.Drawing.Size(251, 525);
            this.Controls.Add(this.treeView_Tool);
            this.Controls.Add(this.label_Note);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ForeColor = System.Drawing.SystemColors.Control;
            this.Name = "ToolForm";
            this.Padding = new System.Windows.Forms.Padding(0, 29, 0, 0);
            this.Style = Sunny.UI.UIStyle.Custom;
            this.Text = "工具箱";
            this.TitleHeight = 29;
            this.TopMost = true;
            this.VisibleChanged += new System.EventHandler(this.ToolForm_VisibleChanged);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TreeView treeView_Tool;
        private System.Windows.Forms.Label label_Note;
    }
}