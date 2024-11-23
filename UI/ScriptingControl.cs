using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using Newtonsoft.Json;
using ScriptingForm.Dialogs;
using ScriptingForm.UI;
using ScriptingForm.Scripts;
namespace ScriptingForm.UI
{
    public class ScriptingControl : UserControl
    {
        private TreeView scriptTreeView;
        private ListBox commandListBox;
        private ListBox targetListBox;
        private ListBox parameterListBox;
        private Button addCommandButton;
        private Button removeCommandButton;
        private Button moveUpButton;
        private Button moveDownButton;
        private TableLayoutPanel mainLayout;
        private TableLayoutPanel rightLayout;
        private Panel buttonPanel;

        private Button saveButton;
        private Button loadButton;
        private SaveFileDialog saveFileDialog;
        private OpenFileDialog openFileDialog;

        // Add these fields at the class level
        private readonly CountdownPopup _countdownPopup;
        private readonly Dictionary<string, Action<string[], TaskCompletionSource<bool>>> _commandExecutors;

        private readonly ILaserTECController _laserController;

        // Command definitions
        private readonly Dictionary<string, HashSet<string>> commandTargets = new Dictionary<string, HashSet<string>>
        {
            { "SET_OUTPUT", new HashSet<string> { "VACUUM_BASE", "UV_PLC1", "UV_PLC2", "UV_PLC3" } },
            { "CLEAR_OUTPUT", new HashSet<string> { "VACUUM_BASE", "UV_PLC1", "UV_PLC2", "UV_PLC3" } },
            { "SLIDE", new HashSet<string> { "UV_HEAD", "DISPENSOR_HEAD", "PICK_UP_TOOL", "L_Gripper", "R_Gripper" } },
            { "MOVE", new HashSet<string> { "GANTRY", "HexapodLeft", "HexapodRight" } },
            { "SHOW_DIALOG", new HashSet<string> { "POPUPBOX" } },
            { "SHOW_COUNTDOWN", new HashSet<string> { "N/A" } },
            { "WAIT", new HashSet<string> { "TIMER" } }, // Add new WAIT command
            { "LASER_CURRENT", new HashSet<string> { "LASER" } },
            { "LASER_POWER", new HashSet<string> { "LASER" } },
            { "TEC_POWER", new HashSet<string> { "TEC" } },
            { "READ", new HashSet<string> {
                "ActualSagnac", "TargetSagnac", "KeithleyCurrent",
                "PM400_1", "PM400_2", "PICH5", "PICH6"
            }}
        };

        // Target parameters
        private readonly Dictionary<string, HashSet<string>> targetParameters = new Dictionary<string, HashSet<string>>
        {
            { "UV_HEAD", new HashSet<string> { "ACTIVATE", "DEACTIVATE" } },
            { "DISPENSOR_HEAD", new HashSet<string> { "ACTIVATE", "DEACTIVATE" } },
            { "PICK_UP_TOOL", new HashSet<string> { "ACTIVATE", "DEACTIVATE" } },
            { "L_Gripper", new HashSet<string> { "ACTIVATE", "DEACTIVATE" } },
            { "R_Gripper", new HashSet<string> { "ACTIVATE", "DEACTIVATE" } },
            { "GANTRY", new HashSet<string> { "MidBack", "UV", "GripLeftLens", "GripRightLens", "SeePIC", "SeeSLED" } },
            { "HexapodLeft", new HashSet<string> { "Home", "LensGrip", "LensPlace", "RejectLens" } },
            { "HexapodRight", new HashSet<string> { "Home", "LensGrip", "LensPlace", "RejectLens" } },
            // ... existing parameters ...
            { "POPUPBOX", new HashSet<string> { "OK", "YES_NO" } },
            { "N/A", new HashSet<string>() }, // Will be validated separately for numeric input
            { "TIMER", new HashSet<string>() }, // Empty set since we'll handle validation separately
            { "LASER", new HashSet<string> { "HIGH", "LOW", "ON", "OFF" } },
            { "TEC", new HashSet<string> { "ON", "OFF" } }

        };

        // Update the command definitions in the constructor
        public ScriptingControl()
        {
            _countdownPopup = new CountdownPopup();

            // Initialize command executors in constructor
            _commandExecutors = new Dictionary<string, Action<string[], TaskCompletionSource<bool>>>
            {
                { "SHOW_DIALOG", ExecuteShowDialog },
                { "SHOW_COUNTDOWN", ExecuteShowCountdown }
            };
            InitializeComponents();
            InitializeFileDialogs();
            SetupLayout();
            PopulateCommandList();
            WireUpEvents();
        }


        // Add execution methods
        private async void ExecuteShowDialog(string[] commandParts, TaskCompletionSource<bool> tcs)
        {
            try
            {
                // Format: SHOW_DIALOG ^ POPUPBOX ^ OK/YES_NO ^ Title ^ Message
                if (commandParts.Length < 5)
                {
                    MessageBox.Show("Invalid dialog command format", "Error");
                    tcs.SetResult(false);
                    return;
                }

                string dialogType = commandParts[2];
                string title = commandParts[3];
                string message = commandParts[4];

                if (dialogType == "OK")
                {
                    CustomMessageBox.Show(message, title, MessageBoxButtons.OK);
                    tcs.SetResult(true);
                }
                else if (dialogType == "YES_NO")
                {
                    DialogResult result = CustomMessageBox.Show(message, title, MessageBoxButtons.YesNo);
                    tcs.SetResult(result == DialogResult.Yes);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing dialog: {ex.Message}", "Error");
                tcs.SetResult(false);
            }
        }

        private async void ExecuteShowCountdown(string[] commandParts, TaskCompletionSource<bool> tcs)
        {
            try
            {
                // Format: SHOW_COUNTDOWN ^ N/A ^ milliseconds
                if (commandParts.Length < 3 || !int.TryParse(commandParts[2], out int milliseconds))
                {
                    MessageBox.Show("Invalid countdown command format or invalid number", "Error");
                    tcs.SetResult(false);
                    return;
                }

                await _countdownPopup.ShowCountdownAsync(milliseconds);
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error executing countdown: {ex.Message}", "Error");
                tcs.SetResult(false);
            }
        }

        // Add a method to execute the script
        public async Task ExecuteScript()
        {
            foreach (TreeNode node in scriptTreeView.Nodes)
            {
                string[] commandParts = node.Text.Split(new[] { " ^ " }, StringSplitOptions.None);
                string command = commandParts[0];

                if (_commandExecutors.ContainsKey(command))
                {
                    var tcs = new TaskCompletionSource<bool>();
                    _commandExecutors[command](commandParts, tcs);
                    await tcs.Task;
                }
            }
        }
        private void InitializeFileDialogs()
        {
            saveFileDialog = new SaveFileDialog
            {
                Filter = "Script files (*.script)|*.script|All files (*.*)|*.*",
                DefaultExt = "script",
                AddExtension = true
            };

            openFileDialog = new OpenFileDialog
            {
                Filter = "Script files (*.script)|*.script|All files (*.*)|*.*",
                DefaultExt = "script"
            };
        }
        private void InitializeComponents()
        {

            // Initialize new save/load buttons
            saveButton = new Button
            {
                Text = "Save Script",
                Dock = DockStyle.Top,
                Height = 30
            };

            loadButton = new Button
            {
                Text = "Load Script",
                Dock = DockStyle.Top,
                Height = 30
            };

            // Initialize main controls
            scriptTreeView = new TreeView
            {
                Dock = DockStyle.Fill,
                HideSelection = false
            };

            commandListBox = new ListBox
            {
                Dock = DockStyle.Fill,
                SelectionMode = SelectionMode.One
            };

            targetListBox = new ListBox
            {
                Dock = DockStyle.Fill,
                SelectionMode = SelectionMode.One
            };

            parameterListBox = new ListBox
            {
                Dock = DockStyle.Fill,
                SelectionMode = SelectionMode.One
            };

            // Initialize buttons
            addCommandButton = new Button
            {
                Text = "Add Command",
                Dock = DockStyle.Top,
                Height = 30
            };

            removeCommandButton = new Button
            {
                Text = "Remove Command",
                Dock = DockStyle.Top,
                Height = 30
            };

            moveUpButton = new Button
            {
                Text = "Move Up",
                Dock = DockStyle.Top,
                Height = 30
            };

            moveDownButton = new Button
            {
                Text = "Move Down",
                Dock = DockStyle.Top,
                Height = 30
            };

            // Initialize layouts
            mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };

            rightLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4
            };

            buttonPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true
            };
        }

        private void SetupLayout()
        {
            // Configure main layout
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            // Configure right layout
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));

            // Add buttons to button panel
            buttonPanel.Controls.Add(moveDownButton);
            buttonPanel.Controls.Add(moveUpButton);
            buttonPanel.Controls.Add(removeCommandButton);
            buttonPanel.Controls.Add(addCommandButton);

            // Add controls to right layout
            rightLayout.Controls.Add(commandListBox, 0, 0);
            rightLayout.Controls.Add(targetListBox, 0, 1);
            rightLayout.Controls.Add(parameterListBox, 0, 2);
            rightLayout.Controls.Add(buttonPanel, 0, 3);

            // Add controls to main layout
            mainLayout.Controls.Add(scriptTreeView, 0, 0);
            mainLayout.Controls.Add(rightLayout, 1, 0);


            // Add save/load buttons to button panel
            buttonPanel.Controls.Add(saveButton);
            buttonPanel.Controls.Add(loadButton);
            buttonPanel.Controls.Add(moveDownButton);
            buttonPanel.Controls.Add(moveUpButton);
            buttonPanel.Controls.Add(removeCommandButton);
            buttonPanel.Controls.Add(addCommandButton);

            // Add main layout to control
            Controls.Add(mainLayout);
        }

        private void PopulateCommandList()
        {
            commandListBox.Items.Clear();
            foreach (string command in commandTargets.Keys)
            {
                commandListBox.Items.Add(command);
            }
        }

        private void WireUpEvents()
        {
            commandListBox.SelectedIndexChanged += CommandListBox_SelectedIndexChanged;
            targetListBox.SelectedIndexChanged += TargetListBox_SelectedIndexChanged;
            addCommandButton.Click += AddCommandButton_Click;
            removeCommandButton.Click += RemoveCommandButton_Click;
            moveUpButton.Click += MoveUpButton_Click;
            moveDownButton.Click += MoveDownButton_Click;


            saveButton.Click += SaveButton_Click;
            loadButton.Click += LoadButton_Click;
        }

        private void CommandListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            targetListBox.Items.Clear();
            parameterListBox.Items.Clear();

            if (commandListBox.SelectedItem is string selectedCommand)
            {
                foreach (string target in commandTargets[selectedCommand])
                {
                    targetListBox.Items.Add(target);
                }
            }
        }

        private void TargetListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            parameterListBox.Items.Clear();

            if (targetListBox.SelectedItem is string selectedTarget &&
                targetParameters.ContainsKey(selectedTarget))
            {
                foreach (string parameter in targetParameters[selectedTarget])
                {
                    parameterListBox.Items.Add(parameter);
                }
            }
        }

        // Override the existing AddCommandButton_Click to handle the new dialog commands
        private void AddCommandButton_Click(object sender, EventArgs e)
        {
            if (commandListBox.SelectedItem == null) return;

            string command = commandListBox.SelectedItem.ToString();
            string target = targetListBox.SelectedItem?.ToString() ?? "";
            string parameter = parameterListBox.SelectedItem?.ToString() ?? "";

            string nodeText;
            if (command == "WAIT")
            {
                using (var millisForm = new InputDialog("Enter wait time in milliseconds (1-3600000)"))
                {
                    if (millisForm.ShowDialog() != DialogResult.OK) return;
                    if (!int.TryParse(millisForm.InputText, out int millis) || millis < 1 || millis > 3600000)
                    {
                        MessageBox.Show("Please enter a valid number between 1 and 3600000", "Invalid Input");
                        return;
                    }
                    nodeText = $"{command} ^ {target} ^ {millis}";
                    var node2 = new TreeNode(nodeText);
                    scriptTreeView.Nodes.Add(node2);
                    return;  // Add this return to prevent the code below from executing
                }
            }
            if (command == "SHOW_DIALOG")
            {
                using (var titleForm = new InputDialog("Enter Dialog Title"))
                {
                    if (titleForm.ShowDialog() != DialogResult.OK) return;

                    using (var messageForm = new InputDialog("Enter Dialog Message"))
                    {
                        if (messageForm.ShowDialog() != DialogResult.OK) return;
                        nodeText = $"{command} ^ {target} ^ {parameter} ^ {titleForm.InputText} ^ {messageForm.InputText}";
                    }
                }
            }
            else if (command == "SHOW_COUNTDOWN")
            {
                using (var millisForm = new InputDialog("Enter milliseconds (1-1200000)"))
                {
                    if (millisForm.ShowDialog() != DialogResult.OK) return;
                    if (!int.TryParse(millisForm.InputText, out int millis) || millis < 1 || millis > 1200000)
                    {
                        MessageBox.Show("Please enter a valid number between 1 and 1200000", "Invalid Input");
                        return;
                    }
                    nodeText = $"{command} ^ {target} ^ {millis}";
                }
            }
            else
            {
                nodeText = $"{command} ^ {target}" + (string.IsNullOrEmpty(parameter) ? "" : $" ^ {parameter}");
            }

            nodeText = $"{command} ^ {target}" + (string.IsNullOrEmpty(parameter) ? "" : $" ^ {parameter}");
            var regularNode = new TreeNode(nodeText);
            scriptTreeView.Nodes.Add(regularNode);
        }
        private void RemoveCommandButton_Click(object sender, EventArgs e)
        {
            if (scriptTreeView.SelectedNode != null)
            {
                scriptTreeView.Nodes.Remove(scriptTreeView.SelectedNode);
            }
        }

        private void MoveUpButton_Click(object sender, EventArgs e)
        {
            MoveNode(-1);
        }

        private void MoveDownButton_Click(object sender, EventArgs e)
        {
            MoveNode(1);
        }

        private void MoveNode(int direction)
        {
            TreeNode selectedNode = scriptTreeView.SelectedNode;
            if (selectedNode == null) return;

            TreeNodeCollection nodes = scriptTreeView.Nodes;
            int index = nodes.IndexOf(selectedNode);
            int newIndex = index + direction;

            if (newIndex >= 0 && newIndex < nodes.Count)
            {
                nodes.RemoveAt(index);
                nodes.Insert(newIndex, selectedNode);
                scriptTreeView.SelectedNode = selectedNode;
            }
        }

        public string[] GetScript()
        {
            return scriptTreeView.Nodes.Cast<TreeNode>()
                                     .Select(node => node.Text)
                                     .ToArray();
        }

        public void LoadScript(string[] commands)
        {
            scriptTreeView.Nodes.Clear();
            foreach (string command in commands)
            {
                scriptTreeView.Nodes.Add(new TreeNode(command));
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (scriptTreeView.Nodes.Count == 0)
            {
                MessageBox.Show("No commands to save.", "Save Script",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    SaveScriptToFile(saveFileDialog.FileName);
                    MessageBox.Show("Script saved successfully.", "Save Script",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving script: {ex.Message}", "Save Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void LoadButton_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    LoadScriptFromFile(openFileDialog.FileName);
                    MessageBox.Show("Script loaded successfully.", "Load Script",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading script: {ex.Message}", "Load Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public void SaveScriptToFile(string filename)
        {
            var scriptData = new ScriptData
            {
                Commands = GetScript(),
                Version = "1.0"
            };

            string jsonData = JsonConvert.SerializeObject(scriptData, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(filename, jsonData);
        }

        public void LoadScriptFromFile(string filename)
        {
            string jsonData = File.ReadAllText(filename);
            var scriptData = JsonConvert.DeserializeObject<ScriptData>(jsonData);

            if (scriptData != null)
            {
                scriptTreeView.Nodes.Clear();
                LoadScript(scriptData.Commands);
            }
        }

        // Validate command before adding to tree
        public bool ValidateCommand(string command)
        {
            string[] parts = command.Split(new[] { " ^ " }, StringSplitOptions.None);

            if (parts.Length < 2) return false;

            string cmd = parts[0];
            string target = parts[1];

            if (!commandTargets.ContainsKey(cmd)) return false;
            if (!commandTargets[cmd].Contains(target)) return false;

            if (parts.Length > 2)
            {
                string parameter = parts[2];
                if (targetParameters.ContainsKey(target) &&
                    !targetParameters[target].Contains(parameter))
                    return false;
            }

            return true;
        }

        // Helper class for script serialization
        private class ScriptData
        {
            public string Version { get; set; }
            public string[] Commands { get; set; }
        }
    }
}
