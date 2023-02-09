﻿using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SEReader.Comm
{
    public static class Extensions
    {
        public static void SendCtrlC(this Process p)
        {
            if (AttachConsole((uint)p.Id))
            {
                SetConsoleCtrlHandler(null, true);
                try
                {
                    if (!GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0))
                    {
                        Debug.WriteLine("Opps...");
                    }
                    p.WaitForExit();
                }
                finally
                {
                    SetConsoleCtrlHandler(null, false);
                    FreeConsole();
                }
            }
        }

        // Internal

        const int CTRL_C_EVENT = 0;
        [DllImport("kernel32.dll")]

        static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AttachConsole(uint dwProcessId);
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        static extern bool FreeConsole();
        [DllImport("kernel32.dll")]
        static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);

        // Delegate type to be used as the Handler Routine for SCCH
        delegate bool ConsoleCtrlDelegate(uint CtrlType);
    }
}
