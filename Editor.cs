using System;
using System.Drawing;
using System.Windows.Forms;
using ScriptingForm.UI;

namespace ScriptingForm
{
    public partial class Editor : Form
    {
        private ScriptingControl scriptingControl;

        public Editor()
        {
            InitializeComponent();

            this.Text = "Script Editor";
            this.Size = new Size(880, 530);
            this.StartPosition = FormStartPosition.CenterScreen;

            scriptingControl = new ScriptingControl
            {
                Dock = DockStyle.Fill
            };

            this.Controls.Add(scriptingControl);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            scriptingControl.BringToFront();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            base.OnFormClosing(e);
        }
    }
}