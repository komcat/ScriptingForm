using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            scriptingControl = new ScriptingControl
            {
                Dock = DockStyle.Fill
            };

            // Add to your form or a panel
            this.Controls.Add(scriptingControl);
        }
    }
}
