using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScriptingForm.Scripts;
using ScriptingForm.UI;
using Serilog;

namespace ScriptingForm
{
    public partial class Executor : Form
    {
        private ExecutorControl executorControl;
        private readonly IRealtimeDataProvider _realtimeData;
        private readonly ILogger _logger;
        private readonly EnhancedScriptInterpreter _scriptInterpreter;

        public Executor(IRealtimeDataProvider realtimeData, ILogger logger)
        {
            _realtimeData = realtimeData;
            _logger = logger;

            InitializeComponent();

            Text = "Script Executor";

            // Initialize the script interpreter
            _scriptInterpreter = new EnhancedScriptInterpreter(
                eziioTop: null, // Initialize with your actual dependencies
                slides: null,
                graphManagers: null,
                laserController: null,
                realtimeData: _realtimeData,
                logger: _logger
            );

            executorControl = new ExecutorControl(_scriptInterpreter)
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

        // In Executor.cs
        private async void LoadScript_Click(object sender, EventArgs e)
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
                            // Pass any command outputs along with the commands
                            var commands = scriptData.Commands;
                            executorControl.LoadScript(commands);
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