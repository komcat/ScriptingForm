using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScriptingForm.UI
{
    public class ExecutorControl : UserControl
    {
        private DataGridView commandGridView;
        private Button executeButton;
        private TableLayoutPanel mainLayout;
        private Panel buttonPanel;
        private ContextMenuStrip gridContextMenu;

        private List<CommandExecutionData> executionData = new List<CommandExecutionData>();

        public class CommandExecutionData
        {
            public int CommandNumber { get; set; }
            public DateTime? ExecutionTime { get; set; }
            public string CommandText { get; set; }
            public string Output { get; set; }
            public bool Selected { get; set; }
        }

        public ExecutorControl()
        {
            InitializeComponents();
            SetupLayout();
            ConfigureGrid();
            SetupContextMenu();
        }

        private void InitializeComponents()
        {
            // Initialize DataGridView
            commandGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                ReadOnly = false,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            };

            // Initialize Execute Button
            executeButton = new Button
            {
                Text = "Execute Selected Commands",
                Dock = DockStyle.Right,
                Height = 30,
                Width = 200
            };

            // Initialize Layouts
            mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2
            };

            buttonPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 40
            };

            // Initialize Context Menu
            gridContextMenu = new ContextMenuStrip();
        }

        private void SetupLayout()
        {
            // Configure main layout
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 90F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

            // Add button to panel
            buttonPanel.Controls.Add(executeButton);

            // Add controls to main layout
            mainLayout.Controls.Add(commandGridView, 0, 0);
            mainLayout.Controls.Add(buttonPanel, 0, 1);

            // Add main layout to control
            Controls.Add(mainLayout);
        }

        private void SetupContextMenu()
        {
            // Add menu items
            var selectAllItem = new ToolStripMenuItem("Select All Selected Rows");
            var deselectAllItem = new ToolStripMenuItem("Deselect All Selected Rows");

            // Add click handlers
            selectAllItem.Click += (sender, e) => UpdateSelectionForSelectedRows(true);
            deselectAllItem.Click += (sender, e) => UpdateSelectionForSelectedRows(false);

            // Add items to context menu
            gridContextMenu.Items.AddRange(new ToolStripItem[] {
                selectAllItem,
                deselectAllItem
            });

            // Attach context menu to grid
            commandGridView.ContextMenuStrip = gridContextMenu;

            // Add context menu opening handler to check if multiple rows are selected
            gridContextMenu.Opening += (sender, e) =>
            {
                bool hasMultipleSelection = commandGridView.SelectedRows.Count > 1;
                foreach (ToolStripItem item in gridContextMenu.Items)
                {
                    item.Visible = hasMultipleSelection;
                }

                // Cancel showing menu if no multiple selection
                if (!hasMultipleSelection)
                {
                    e.Cancel = true;
                }
            };
        }

        private void UpdateSelectionForSelectedRows(bool select)
        {
            foreach (DataGridViewRow row in commandGridView.SelectedRows)
            {
                row.Cells["Selected"].Value = select;
                executionData[row.Index].Selected = select;
            }
            commandGridView.Refresh();
        }
        private void ConfigureGrid()
        {
            // Configure DataGridView columns
            commandGridView.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Selected",
                HeaderText = "Select",
                Width = 60
            });

            commandGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "CommandNumber",
                HeaderText = "#",
                ReadOnly = true
            });



            commandGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "CommandText",
                HeaderText = "Command",
                ReadOnly = true
            });

            commandGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Output",
                HeaderText = "Output",
                ReadOnly = true
            });

            commandGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ExecutionTime",
                HeaderText = "Time",
                ReadOnly = true
            });

            // Wire up events
            executeButton.Click += ExecuteButton_Click;
        }

        public void LoadScript(string[] commands)
        {
            executionData.Clear();
            commandGridView.Rows.Clear();

            for (int i = 0; i < commands.Length; i++)
            {
                var data = new CommandExecutionData
                {
                    CommandNumber = i + 1,
                    CommandText = commands[i],
                    ExecutionTime = null,
                    Output = "",
                    Selected = false
                };
                executionData.Add(data);

                var row = new string[] {
                    "False",
                    (i + 1).ToString(),
                    "",
                    commands[i],
                    ""
                };
                commandGridView.Rows.Add(row);
            }
        }

        private async void ExecuteButton_Click(object sender, EventArgs e)
        {
            var selectedRows = new List<int>();

            // Get selected rows
            for (int i = 0; i < commandGridView.Rows.Count; i++)
            {
                if (Convert.ToBoolean(commandGridView.Rows[i].Cells["Selected"].Value))
                {
                    selectedRows.Add(i);
                }
            }

            // If no rows selected, execute all
            if (selectedRows.Count == 0)
            {
                selectedRows = Enumerable.Range(0, commandGridView.Rows.Count).ToList();
            }

            await ExecuteCommands(selectedRows);
        }

        private async Task ExecuteCommands(List<int> rowIndices)
        {
            executeButton.Enabled = false;

            try
            {
                foreach (int rowIndex in rowIndices)
                {
                    // Update execution time
                    DateTime executionTime = DateTime.Now;
                    commandGridView.Rows[rowIndex].Cells["ExecutionTime"].Value = executionTime.ToString("yyyy-MM-dd HH:mm:ss");

                    // Get command
                    string command = commandGridView.Rows[rowIndex].Cells["CommandText"].Value.ToString();

                    // TODO: Implement actual command execution here
                    // For now, just simulate execution with a delay
                    await Task.Delay(1000);
                    string output = $"Executed command {rowIndex + 1}";

                    // Update output
                    commandGridView.Rows[rowIndex].Cells["Output"].Value = output;

                    // Update data model
                    executionData[rowIndex].ExecutionTime = executionTime;
                    executionData[rowIndex].Output = output;

                    // Refresh display
                    commandGridView.Refresh();
                }
            }
            finally
            {
                executeButton.Enabled = true;
            }
        }

        // Method to get execution results
        public List<CommandExecutionData> GetExecutionResults()
        {
            return executionData.ToList();
        }

        // Method to clear execution results
        public void ClearExecutionResults()
        {
            foreach (DataGridViewRow row in commandGridView.Rows)
            {
                row.Cells["ExecutionTime"].Value = "";
                row.Cells["Output"].Value = "";
                row.Cells["Selected"].Value = false;
            }

            foreach (var data in executionData)
            {
                data.ExecutionTime = null;
                data.Output = "";
                data.Selected = false;
            }
        }
    }
}