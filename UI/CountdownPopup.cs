
using Serilog;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScriptingForm.UI
{
    public class CountdownPopup : IDisposable
    {
        private readonly ILogger _logger;
        private bool _disposed;

        public CountdownPopup(ILogger logger = null)
        {
            _logger = logger;
        }

        public async Task ShowCountdownAsync(int milliseconds)
        {
            ValidateMilliseconds(milliseconds);

            try
            {
                var tcs = new TaskCompletionSource<bool>();
                using (var countdownForm = CreateCountdownForm(milliseconds, tcs))
                {
                    countdownForm.Show();
                    await tcs.Task;
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "Error during countdown display");
                throw;
            }
        }

        public async Task ShowCountdownAsync(int milliseconds, Action completionCallback)
        {
            await ShowCountdownAsync(milliseconds);
            completionCallback?.Invoke();
        }

        private void ValidateMilliseconds(int milliseconds)
        {
            const int maxMilliseconds = 20 * 60 * 1000; // 20 minutes
            if (milliseconds < 1 || milliseconds > maxMilliseconds)
            {
                throw new ArgumentOutOfRangeException(nameof(milliseconds),
                    "Value must be between 1 millisecond and 20 minutes.");
            }
        }

        private CountdownForm CreateCountdownForm(int milliseconds, TaskCompletionSource<bool> tcs)
        {
            return new CountdownForm(milliseconds, tcs);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Clean up managed resources
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    internal class CountdownForm : Form
    {
        private readonly Timer _timer;
        private readonly Label _countdownLabel;
        private readonly TaskCompletionSource<bool> _tcs;
        private int _remainingMilliseconds;

        public CountdownForm(int milliseconds, TaskCompletionSource<bool> tcs)
        {
            _remainingMilliseconds = milliseconds;
            _tcs = tcs;

            // Configure form
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(200, 100);
            BackColor = Color.White;

            // Create and configure label
            _countdownLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 16, FontStyle.Bold)
            };
            Controls.Add(_countdownLabel);

            // Configure timer
            _timer = new Timer
            {
                Interval = 100 // Update every 100ms for smoother countdown
            };
            _timer.Tick += Timer_Tick;

            UpdateDisplay();
            _timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            _remainingMilliseconds -= _timer.Interval;

            if (_remainingMilliseconds <= 0)
            {
                _timer.Stop();
                _tcs.SetResult(true);
                Close();
                return;
            }

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            var seconds = Math.Ceiling(_remainingMilliseconds / 1000.0);
            _countdownLabel.Text = $"{seconds:F1}s";
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _timer.Stop();
            _timer.Dispose();
            _tcs.TrySetResult(true);
            base.OnFormClosing(e);
        }
    }
}

