using System;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SEReader.Logging
{
    internal class ScreenLogger
    {
        [Flags]
        public enum Target
        {
            None = 0,
            ParserEvent = 1,
            GazePoint = 2,
            LowPassFilter = 4,
            Game = 8,
        }

        public static ScreenLogger Instance => _instance;

        public Target Targets { get; private set; } = Target.None;

        public ScreenLogger(TextBox output)
        {
            _instance = this;

            _output = output;
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        public ScreenLogger WithTarget(Target target)
        {
            Targets |= target;
            return this;
        }

        public void Log(Target target, string message)
        {
            if (Targets.HasFlag(target))
            {
                _dispatcher.Invoke(AddToScreen, target, message);
            }
        }

        // Internal

        static ScreenLogger _instance = null;

        readonly TextBox _output;
        readonly Dispatcher _dispatcher;

        private void AddToScreen(Target target, string message)
        {
            _output.Text += $"\n[{target}] {message}";
            _output.ScrollToEnd();
        }
    }
}
