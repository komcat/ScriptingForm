using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScriptingForm.UI
{
    // Create a custom message box form
    public class CustomMessageBox : Form
    {
        public static DialogResult Show(string message, string title = "Message", MessageBoxButtons buttons = MessageBoxButtons.OK)
        {
            using (CustomMessageBox msgBox = new CustomMessageBox(message, title, buttons))
            {
                return msgBox.ShowDialog();
            }
        }

        private CustomMessageBox(string message, string title, MessageBoxButtons buttons)
        {
            // Form setup
            this.Text = title;
            this.Width = 400;
            this.Height = 200;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MinimizeBox = false;
            this.MaximizeBox = false;

            // Create label for message
            Label label = new Label();
            label.Text = message;
            label.Font = new Font("Arial", 14);
            label.AutoSize = true;
            label.Location = new Point(20, 20);
            this.Controls.Add(label);

            // Create buttons based on type
            if (buttons == MessageBoxButtons.YesNo)
            {
                CreateYesNoButtons();
            }
            else // OK button
            {
                CreateOkButton();
            }
        }

        private void CreateOkButton()
        {
            Button okButton = new Button();
            okButton.Text = "OK";
            okButton.DialogResult = DialogResult.OK;
            okButton.Location = new Point(150, 100);
            okButton.Font = new Font("Arial", 12);
            okButton.Width = 100;
            okButton.Height = 30;
            this.Controls.Add(okButton);
        }

        private void CreateYesNoButtons()
        {
            // Yes button
            Button yesButton = new Button();
            yesButton.Text = "Yes";
            yesButton.DialogResult = DialogResult.Yes;
            yesButton.Location = new Point(100, 100);
            yesButton.Font = new Font("Arial", 12);
            yesButton.Width = 100;
            yesButton.Height = 30;
            this.Controls.Add(yesButton);

            // No button
            Button noButton = new Button();
            noButton.Text = "No";
            noButton.DialogResult = DialogResult.No;
            noButton.Location = new Point(220, 100);
            noButton.Font = new Font("Arial", 12);
            noButton.Width = 100;
            noButton.Height = 30;
            this.Controls.Add(noButton);
        }
    }
}
