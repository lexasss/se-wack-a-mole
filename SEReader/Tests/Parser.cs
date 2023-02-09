using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace SEReader.Tests
{
    internal class Parser
    {
        public static async Task Run(Comm.Parser parser)
        {
            string filename = "test-data-socketclient.txt";
            if (!File.Exists(filename))
            {
                MessageBox.Show($"File '{filename}' does not exist in the app folder.", "SEReader : Test", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var stream = new StreamReader(filename))
            {
                while (!stream.EndOfStream)
                {
                    string line = stream.ReadLine();
                    parser.Feed(line);
                    await Task.Delay(1);
                }
            }
        }
    }
}
