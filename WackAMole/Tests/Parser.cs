using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace WackAMole.Tests
{
    internal class Parser
    {
        public static async Task Run(Comm.Parser parser, bool isFast)
        {
            string filename = "test-data-socketclient2.txt";
            if (!File.Exists(filename))
            {
                MessageBox.Show($"File '{filename}' does not exist in the app folder.", "Wack-a-Mole : Test", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var stream = new StreamReader(filename))
            {
                string line = "";

                Func<Task> parseLine = () => {
                    parser.Feed(line);
                    return Task.CompletedTask;
                };

                while (!stream.EndOfStream)
                {
                    line = stream.ReadLine();
                    if (isFast)
                    {
                        await Task.Run(parseLine);
                    }
                    else
                    {
                        parser.Feed(line);
                        await Task.Delay(1);
                    }
                }
            }
        }
    }
}
