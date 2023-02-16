using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SEReader.Logging
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AllowScreenLogAttribute : Attribute
    {
        public ScreenLogger.Target Target { get; }
        public AllowScreenLogAttribute(ScreenLogger.Target target)
        {
            Target = target;
        }
    }

    public class ScreenLogger
    {
        public enum Target
        {
            General,
            DataSource,
            Parser,
            GazePoint,
            LowPassFilter,
            Game,
            Cell,
            Renderer,
            GazeController,
        }

        public static HashSet<Target> Enabled = new()
        {
        };

        public static void Initialize(TextBox output)
        {
            if (_printer != null)
            {
                throw new Exception($"{nameof(ScreenPrinter)} has been initialized already");
            }

            _printer = new ScreenPrinter(output);
        }

        public static ScreenLogger Create(Target? target = null)
        {
            if (target != null)
            {
                return new ScreenLogger(target ?? Target.General);
            }

            StackTrace stackTrace = new StackTrace();
            var cls = stackTrace.GetFrame(1).GetMethod().DeclaringType;
            var attr = (AllowScreenLogAttribute)Attribute.GetCustomAttribute(cls, typeof(AllowScreenLogAttribute));
            return attr != null ? new ScreenLogger(attr.Target) : null;
        }

        private ScreenLogger(Target target)
        {
            _target = target;
        }

        public void Log(string message)
        {
            if (Enabled.Contains(_target))
            {
                _printer?.Print(_target, message);
            }
        }

        // Internal


        private class ScreenPrinter
        {
            public ScreenPrinter(TextBox output)
            {
                _output = output;
                _dispatcher = Dispatcher.CurrentDispatcher;
            }

            public void Print(Target target, string message)
            {
                _dispatcher.Invoke(PrintSafe, target, message);
            }

            // Internal

            readonly TextBox _output;
            readonly Dispatcher _dispatcher;

            private void PrintSafe(Target target, string message)
            {
                _output.Text += $"\n[{target}] {message}";
                _output.ScrollToEnd();
            }
        }

        static ScreenPrinter _printer = null;

        readonly Target _target;
    }
}
