using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Serilog;

namespace ScriptingForm.Scripts
{
    // Interfaces for missing classes


    // Main command class
    public class EnhancedScriptCommand
    {
        public string Command { get; set; }
        public string Target { get; set; }
        public string[] Parameters { get; set; }
        public string RawCommand { get; set; }
    }

    // Main interpreter class
    public class EnhancedScriptInterpreter
    {
        private ILogger _logger;
        private readonly Dictionary<string, Func<EnhancedScriptCommand, Task<bool>>> _commandHandlers = new Dictionary<string, Func<EnhancedScriptCommand, Task<bool>>>();
        private readonly IEziioController _eziioControlTop;
        private readonly ISlidesController _slidesController;
        private readonly IGraphManager[] _graphManagers;
        private readonly ICountdownPopup _countdownPopup;


        public EnhancedScriptInterpreter(
        IEziioController eziioTop,
        ISlidesController slides,
        IGraphManager[] graphManagers,
        ILogger logger)
        {
            _logger = logger;
            _eziioControlTop = eziioTop;
            _slidesController = slides;
            _graphManagers = graphManagers;
            InitializeCommandHandlers();
        }



        private void InitializeCommandHandlers()
        {
            _commandHandlers.Clear(); // Clear any existing entries
            _commandHandlers.Add("SET_OUTPUT", HandleSetOutput);
            _commandHandlers.Add("CLEAR_OUTPUT", HandleClearOutput);
            _commandHandlers.Add("SLIDE", HandleSlideCommand);
            _commandHandlers.Add("MOVE", HandleMoveCommand);
            _commandHandlers.Add("SHOW_DIALOG", HandleShowDialog);
            _commandHandlers.Add("SHOW_COUNTDOWN", HandleShowCountdown);
        }

        public async Task<bool> ExecuteCommand(string rawCommand)
        {
            try
            {
                var command = ParseCommand(rawCommand);
                if (command == null) return true; // Skip empty or comment lines

                if (_commandHandlers.TryGetValue(command.Command, out var handler))
                {
                    return await handler(command);
                }
                else
                {
                    _logger.Error($"Unknown command: {command.Command}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error executing command: {rawCommand}");
                return false;
            }
        }

        private EnhancedScriptCommand ParseCommand(string line)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(line)) return null;

                // Remove comments
                var commentIndex = line.IndexOf("//");
                if (commentIndex >= 0)
                {
                    line = line.Substring(0, commentIndex);
                }

                line = line.Trim();
                if (string.IsNullOrEmpty(line)) return null;

                // Split by '^' and trim each part
                var parts = line.Split(new[] { '^' }, StringSplitOptions.None)
                               .Select(p => p.Trim())
                               .Where(p => !string.IsNullOrEmpty(p))
                               .ToArray();

                if (parts.Length < 2)
                {
                    _logger.Warning($"Invalid command format (needs at least 2 parts): {line}");
                    return null;
                }

                return new EnhancedScriptCommand
                {
                    Command = parts[0].Trim(),
                    Target = parts[1].Trim(),
                    Parameters = parts.Length > 2 ? parts.Skip(2).ToArray() : new string[0],
                    RawCommand = line
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error parsing command: {line}");
                return null;
            }
        }

        private async Task<bool> HandleSetOutput(EnhancedScriptCommand command)
        {
            try
            {
                _eziioControlTop.SetOutputByName(command.Target);
                await Task.Delay(100); // Small delay for stability
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error in HandleSetOutput: {command.RawCommand}");
                return false;
            }
        }

        private async Task<bool> HandleClearOutput(EnhancedScriptCommand command)
        {
            try
            {
                _eziioControlTop.ClearOutputByName(command.Target);
                await Task.Delay(100);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error in HandleClearOutput: {command.RawCommand}");
                return false;
            }
        }

        private async Task<bool> HandleSlideCommand(EnhancedScriptCommand command)
        {
            try
            {
                var action = command.Parameters[0]?.ToUpper();
                if (action == "DEACTIVATE")
                {
                    await _slidesController.DeactivateSlideAsync(command.Target);
                }
                else if (action == "ACTIVATE")
                {
                    await _slidesController.ActivateSlideAsync(command.Target);
                }
                else
                {
                    _logger.Error($"Invalid slide action: {action}");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error in HandleSlideCommand: {command.RawCommand}");
                return false;
            }
        }

        private async Task<bool> HandleMoveCommand(EnhancedScriptCommand command)
        {
            try
            {
                _logger.Information($"Processing move command: {command.RawCommand}");
                _logger.Information($"Target: {command.Target}, Parameters: {string.Join(", ", command.Parameters)}");

                var component = ParseComponent(command.Target);
                _logger.Information($"Parsed component index: {component}");

                if (component < 0 || component >= _graphManagers.Length)
                {
                    _logger.Error($"Invalid component index {component} for target: {command.Target}");
                    return false;
                }

                if (_graphManagers[component] == null)
                {
                    _logger.Error($"No graph manager available for component {command.Target} at index {component}");
                    return false;
                }

                if (command.Parameters == null || command.Parameters.Length == 0)
                {
                    _logger.Error("No target position specified in move command");
                    return false;
                }

                string targetPosition = command.Parameters[0];
                _logger.Information($"Moving {command.Target} to position: {targetPosition}");

                await _graphManagers[component].MoveToPoint(targetPosition, false);
                _logger.Information($"Successfully completed move to {targetPosition}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error executing move command: {command.RawCommand}");
                return false;
            }
        }
        
        
        // And update the ParseComponent method to return int instead of nullable enum
        private int ParseComponent(string component)
        {
            if (string.IsNullOrEmpty(component))
            {
                _logger.Warning("Empty component name");
                return -1;
            }

            component = component.Trim().ToUpper();
            _logger.Information($"Parsing component: {component}");

            switch (component)
            {
                case "GANTRY":
                    return (int)uaaComponent.Gantry;
                case "HEXAPOD_LEFT":
                case "HEXAPODLEFT":
                    return (int)uaaComponent.HexapodLeft;
                case "HEXAPOD_RIGHT":
                case "HEXAPODRIGHT":
                    return (int)uaaComponent.HexapodRight;
                default:
                    _logger.Warning($"Unknown component: {component}");
                    return -1;
            }
        }
        private async Task<bool> HandleShowDialog(EnhancedScriptCommand command)
        {
            try
            {
                string dialogType = command.Parameters[0];
                string title = command.Parameters[1];
                string message = command.Parameters[2];

                var buttons = dialogType == "YES_NO" ? MessageBoxButtons.YesNo : MessageBoxButtons.OK;
                var result = MessageBox.Show(message, title, buttons);

                return dialogType != "YES_NO" || result == DialogResult.Yes;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error in HandleShowDialog: {command.RawCommand}");
                return false;
            }
        }

        private async Task<bool> HandleShowCountdown(EnhancedScriptCommand command)
        {
            try
            {
                if (_countdownPopup != null && int.TryParse(command.Parameters[0], out int milliseconds))
                {
                    await _countdownPopup.ShowCountdownAsync(milliseconds);
                    return true;
                }
                _logger.Error($"Invalid countdown duration: {command.Parameters[0]}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error in HandleShowCountdown: {command.RawCommand}");
                return false;
            }
        }


    }

    // Enum for components
    public enum uaaComponent
    {
        HexapodLeft = 0,
        HexapodBottom = 1,
        HexapodRight = 2,
        Gantry = 3
    }
}
