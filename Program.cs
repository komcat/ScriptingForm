using System;
using System.Windows.Forms;
using ScriptingForm.Scripts;
using Serilog;

namespace ScriptingForm
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Create null logger if you don't want to use logging
            ILogger logger = new LoggerConfiguration()
                .CreateLogger();

            // Create a dummy provider if you want to test without real data
            var dummyProvider = new DummyRealtimeDataProvider();

            Application.Run(new Executor(dummyProvider, logger));
        }
    }

    // Create a dummy provider for testing
    public class DummyRealtimeDataProvider : IRealtimeDataProvider
    {
        public double GetValueByName(string inputName) => 0;
        public double GetTargetByName(string inputName) => 0;
        public string GetUnit(string inputName) => "V";
        public bool HasChannel(string inputName) => true;
    }
}