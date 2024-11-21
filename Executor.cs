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
    public partial class Executor : Form
    {
        private ExecutorControl executorControl;

        public Executor()
        {
            InitializeComponent();

            Text = "Script Executor";

            executorControl = new ExecutorControl
            {
                Dock = DockStyle.Fill
            };

            Controls.Add(executorControl);

            // Add menu strip for loading scripts
            var menuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("File");
            var loadScript = new ToolStripMenuItem("Load Script", null, LoadScript_Click);

            fileMenu.DropDownItems.Add(loadScript);
            menuStrip.Items.Add(fileMenu);
            Controls.Add(menuStrip);
        }

        private void LoadScript_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog
            {
                Filter = "Script files (*.script)|*.script|All files (*.*)|*.*",
                DefaultExt = "script"
            })
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var script = System.IO.File.ReadAllText(openFileDialog.FileName);
                        var scriptData = Newtonsoft.Json.JsonConvert.DeserializeObject<ScriptData>(script);

                        if (scriptData?.Commands != null)
                        {
                            executorControl.LoadScript(scriptData.Commands);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading script: {ex.Message}", "Load Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private class ScriptData
        {
            public string Version { get; set; }
            public string[] Commands { get; set; }
        }
    }
}