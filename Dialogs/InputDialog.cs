using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScriptingForm.Dialogs
{
    partial class InputDialog : Form
    {
        private TextBox textBox;
        private Button okButton;
        private Button cancelButton;

        public string InputText => textBox.Text;

        public InputDialog(string prompt)
        {
            this.Text = prompt;
            this.Size = new Size(300, 150);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            textBox = new TextBox
            {
                Location = new Point(10, 20),
                Size = new Size(264, 20)
            };

            okButton = new Button
            {
                DialogResult = DialogResult.OK,
                Location = new Point(50, 70),
                Size = new Size(75, 23),
                Text = "OK"
            };

            cancelButton = new Button
            {
                DialogResult = DialogResult.Cancel,
                Location = new Point(160, 70),
                Size = new Size(75, 23),
                Text = "Cancel"
            };

            this.Controls.AddRange(new Control[] { textBox, okButton, cancelButton });
            this.AcceptButton = okButton;
            this.CancelButton = cancelButton;
        }
    }
}
