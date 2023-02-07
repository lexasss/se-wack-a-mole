﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace SEReader.Comm
{
    public class DataSource
    {
        public event EventHandler<string> Data;
        public event EventHandler Closed;

        public bool IsRunning => _cmd != null;

        public void Start(string host, string port, bool isTesting = false)
        {
            if (IsRunning) return;

            string cmdParam = "/c " + (isTesting ? "ping 127.0.0.1 -n 6" : $"SocketClient.exe TCP {port} {host}");
            ProcessStartInfo cmdStartInfo = new ProcessStartInfo("cmd", cmdParam)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
            };

            _cmd = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = cmdStartInfo
            };
            _cmd.Exited += Cmd_Exited;
            _cmd.OutputDataReceived += Cmd_OutputDataReceived;

            _cmd.Start();
            _cmd.BeginOutputReadLine();
        }

        public async Task Stop()
        {
            if (!IsRunning) return;

            _cmd.CancelOutputRead();

            await Task.Delay(500);

            _cmd.SendCtrlC();
        }

        // Internal

        Process _cmd;

        private void Cmd_Exited(object sender, EventArgs e)
        {
            _cmd = null;

            Application.Current.Dispatcher.Invoke(() =>
            {
                Closed?.Invoke(this, new EventArgs());
            });
        }

        private void Cmd_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Data?.Invoke(this, e.Data);
            });
        }

    }
}
