using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScriptingForm.UI;
using Serilog;

namespace ScriptingForm.Scripts
{
    //Script termination
    public class ScriptExecutionTerminatedException : Exception
    {
        public ScriptExecutionTerminatedException(string message) : base(message) { }
    }

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
        private readonly Dictionary<string, Func<EnhancedScriptCommand, CancellationToken, CancellationTokenSource, Task<bool>>> _commandHandlers;

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
            _commandHandlers = new Dictionary<string, Func<EnhancedScriptCommand, CancellationToken, CancellationTokenSource, Task<bool>>>();

            _logger = logger;
            _eziioControlTop = eziioTop;
            _slidesController = slides;
            _graphManagers = graphManagers;
            InitializeCommandHandlers();
        }

        private void InitializeCommandHandlers()
        {
            _commandHandlers.Clear();
            _commandHandlers.Add("SET_OUTPUT", HandleSetOutput);
            _commandHandlers.Add("CLEAR_OUTPUT", HandleClearOutput);
            _commandHandlers.Add("SLIDE", HandleSlideCommand);
            _commandHandlers.Add("MOVE", HandleMoveCommand);
            _commandHandlers.Add("SHOW_DIALOG", HandleShowDialog);
            _commandHandlers.Add("SHOW_COUNTDOWN", HandleShowCountdown);
        }

        public async Task<bool> ExecuteCommand(string rawCommand, CancellationToken cancellationToken, CancellationTokenSource cts)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.Information("Command execution cancelled before parsing: {Command}", rawCommand);
                    return false;
                }

                var command = ParseCommand(rawCommand);
                if (command == null)
                {
                    _logger.Information("Skipping empty or comment line");
                    return true; // Skip empty or comment lines
                }

                if (_commandHandlers.TryGetValue(command.Command, out var handler))
                {
                    _logger.Information("Executing command: {Command}", command.Command);
                    return await handler(command, cancellationToken, cts);
                }
                else
                {
                    _logger.Error("Unknown command: {Command}", command.Command);
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Information("Command execution cancelled: {Command}", rawCommand);
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error executing command: {Command}", rawCommand);
                return false;
            }
        }
        private EnhancedScriptCommand ParseCommand(string line)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(line))
                    return null;

                // Remove comments
                var commentIndex = line.IndexOf("//");
                if (commentIndex >= 0)
                {
                    line = line.Substring(0, commentIndex);
                }

                line = line.Trim();
                if (string.IsNullOrEmpty(line))
                    return null;

                // Split by '^' and trim each part
                var parts = line.Split(new[] { '^' }, StringSplitOptions.None)
                               .Select(p => p.Trim())
                               .Where(p => !string.IsNullOrEmpty(p))
                               .ToArray();

                if (parts.Length < 2)
                {
                    _logger.Warning("Invalid command format (needs at least 2 parts): {Line}", line);
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
                _logger.Error(ex, "Error parsing command: {Line}", line);
                return null;
            }
        }
        // Modify other command handlers to accept CancellationToken
        private async Task<bool> HandleSetOutput(
            EnhancedScriptCommand command,
            CancellationToken cancellationToken,
            CancellationTokenSource cancellationTokenSource)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.Information("SET_OUTPUT command cancelled before execution");
                    return false;
                }

                _eziioControlTop.SetOutputByName(command.Target);
                _logger.Information($"Set output {command.Target}");

                try
                {
                    await Task.Delay(100, cancellationToken); // Small delay for stability
                }
                catch (OperationCanceledException)
                {
                    _logger.Information("SET_OUTPUT command cancelled during delay");
                    return false;
                }

                return !cancellationToken.IsCancellationRequested;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error in HandleSetOutput: {command.RawCommand}");
                return false;
            }
        }

        private async Task<bool> HandleClearOutput(
            EnhancedScriptCommand command,
            CancellationToken cancellationToken,
            CancellationTokenSource cancellationTokenSource)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.Information("CLEAR_OUTPUT command cancelled before execution");
                    return false;
                }

                _eziioControlTop.ClearOutputByName(command.Target);
                _logger.Information($"Cleared output {command.Target}");

                try
                {
                    await Task.Delay(100, cancellationToken); // Small delay for stability
                }
                catch (OperationCanceledException)
                {
                    _logger.Information("CLEAR_OUTPUT command cancelled during delay");
                    return false;
                }

                return !cancellationToken.IsCancellationRequested;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error in HandleClearOutput: {command.RawCommand}");
                return false;
            }
        }
        private async Task<bool> HandleSlideCommand(
            EnhancedScriptCommand command,
            CancellationToken cancellationToken,
            CancellationTokenSource cancellationTokenSource)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.Information("Slide command cancelled before execution");
                    return false;
                }

                var action = command.Parameters[0]?.ToUpper();
                if (action == "DEACTIVATE")
                {
                    await _slidesController.DeactivateSlideAsync(command.Target);
                    _logger.Information($"Deactivated slide {command.Target}");
                }
                else if (action == "ACTIVATE")
                {
                    await _slidesController.ActivateSlideAsync(command.Target);
                    _logger.Information($"Activated slide {command.Target}");
                }
                else
                {
                    _logger.Error($"Invalid slide action: {action}");
                    return false;
                }

                try
                {
                    await Task.Delay(100, cancellationToken); // Small delay for stability
                }
                catch (OperationCanceledException)
                {
                    _logger.Information("Slide command cancelled during delay");
                    return false;
                }

                return !cancellationToken.IsCancellationRequested;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error in HandleSlideCommand: {command.RawCommand}");
                return false;
            }
        }


        private async Task<bool> HandleMoveCommand(
            EnhancedScriptCommand command,
            CancellationToken cancellationToken,
            CancellationTokenSource cancellationTokenSource)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.Information("Move command cancelled before execution");
                    return false;
                }

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

                try
                {
                    await _graphManagers[component].MoveToPoint(targetPosition, false);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.Information("Move command cancelled after movement");
                        return false;
                    }

                    _logger.Information($"Successfully completed move to {targetPosition}");
                    return true;
                }
                catch (OperationCanceledException)
                {
                    _logger.Information("Move command cancelled during movement");
                    return false;
                }
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
        private async Task<bool> HandleShowDialog(
                EnhancedScriptCommand command,
                CancellationToken cancellationToken,
                CancellationTokenSource cancellationTokenSource)
        {
            try
            {
                string dialogType = command.Parameters[0];
                string title = command.Parameters[1];
                string message = command.Parameters[2];

                var tcs = new TaskCompletionSource<DialogResult>();

                if (Application.OpenForms.Count > 0)
                {
                    var mainForm = Application.OpenForms[0];
                    mainForm.Invoke((MethodInvoker)delegate
                    {
                        try
                        {
                            var buttons = dialogType == "YES_NO" ? MessageBoxButtons.YesNo : MessageBoxButtons.OK;
                            var result = MessageBox.Show(mainForm, message, title, buttons);
                            tcs.SetResult(result);
                        }
                        catch (Exception ex)
                        {
                            tcs.SetException(ex);
                        }
                    });
                }
                else
                {
                    var buttons = dialogType == "YES_NO" ? MessageBoxButtons.YesNo : MessageBoxButtons.OK;
                    var result = MessageBox.Show(message, title, buttons);
                    tcs.SetResult(result);
                }

                var dialogResult = await tcs.Task;
                _logger.Information($"Dialog result: {dialogResult} for command: {command.RawCommand}");

                if (dialogType == "YES_NO" && dialogResult == DialogResult.No)
                {
                    _logger.Information($"User selected No - requesting script termination at dialog: {message}");
                    cancellationTokenSource.Cancel();
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error in HandleShowDialog: {command.RawCommand}");
                return false;
            }
        }
        private async Task<bool> HandleShowCountdown(
            EnhancedScriptCommand command,
            CancellationToken cancellationToken,
            CancellationTokenSource cancellationTokenSource)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.Information("Countdown cancelled before starting");
                    return false;
                }

                if (!int.TryParse(command.Parameters[0], out int milliseconds))
                {
                    _logger.Error($"Invalid countdown duration: {command.Parameters[0]}");
                    return false;
                }

                var tcs = new TaskCompletionSource<bool>();

                if (Application.OpenForms.Count > 0)
                {
                    var mainForm = Application.OpenForms[0];
                    mainForm.Invoke((MethodInvoker)async delegate
                    {
                        try
                        {
                            using (var countdownForm = new CountdownPopup(_logger))
                            {
                                try
                                {
                                    await countdownForm.ShowCountdownAsync(milliseconds);
                                    if (!cancellationToken.IsCancellationRequested)
                                    {
                                        tcs.SetResult(true);
                                    }
                                    else
                                    {
                                        tcs.SetResult(false);
                                    }
                                }
                                catch (OperationCanceledException)
                                {
                                    _logger.Information("Countdown cancelled during execution");
                                    tcs.SetResult(false);
                                }
                                catch (Exception ex)
                                {
                                    _logger.Error(ex, "Error during countdown");
                                    tcs.SetException(ex);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "Error creating or showing countdown form");
                            tcs.SetException(ex);
                        }
                    });
                }
                else
                {
                    _logger.Warning("No forms available for countdown display, using simple delay");
                    try
                    {
                        await Task.Delay(milliseconds, cancellationToken);
                        tcs.SetResult(true);
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.Information("Simple delay countdown cancelled");
                        tcs.SetResult(false);
                    }
                }

                try
                {
                    bool result = await tcs.Task;
                    if (result)
                    {
                        _logger.Information($"Countdown completed for duration: {milliseconds}ms");
                        return true;
                    }
                    else
                    {
                        _logger.Information($"Countdown cancelled for duration: {milliseconds}ms");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error waiting for countdown completion");
                    return false;
                }
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
