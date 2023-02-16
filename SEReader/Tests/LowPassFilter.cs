using SEReader.Comm;
using SEReader.Game;
using SEReader.Utils;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace SEReader.Tests
{
    internal class LowPassFilter
    {
        public static async Task Run(GazeController gazeController)
        {
            string filename = "test-data-gazepoint.txt";
            if (!File.Exists(filename))
            {
                MessageBox.Show($"File '{filename}' does not exist in the app folder.", "SEReader : Test", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string screenName = Options.Instance.ScreenName;

            Sample sample = new Sample()
            {
                ID = 1,
                TimeStamp = Timestamp.Ms,
                Intersections = new System.Collections.Generic.List<Intersection>(),
            };

            using (var stream = new StreamReader(filename))
            {
                while (!stream.EndOfStream)
                {
                    string line = stream.ReadLine();
                    if (line == null)
                        continue;

                    var p = line.Split('\t');

                    sample.Intersections = new System.Collections.Generic.List<Intersection>()
                    {
                        new Intersection()
                        {
                            ID = 1,
                            PlaneName = screenName,
                            Gaze = new Point3D()
                            {
                                X = 0,
                                Y = 0,
                                Z = 0,
                            },
                            Point = new Point2D()
                            {
                                X = int.Parse(p[0]),
                                Y = int.Parse(p[1])
                            }
                        }
                    };

                    gazeController.Feed(ref sample);

                    await Task.Delay(1);
                }
            }
        }
    }
}
