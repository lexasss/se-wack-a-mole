﻿using System.IO;
using System.Threading.Tasks;
using System.Windows;
#if USE_TCP
using SEClient.Tcp;
using Intersection = WackAMole.Plane.Intersection;
#else
using SEClient.Cmd;
#endif

namespace WackAMole.Tests;

internal class LowPassFilter
{
    public static async Task Run(Game.GazeController gazeController)
    {
        string filename = "test-data-gazepoint.txt";
        if (!File.Exists(filename))
        {
            MessageBox.Show($"File '{filename}' does not exist in the app folder.", "Wack-a-Mole : Test", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        string screenName = GameOptions.Instance.ScreenName;

        using var stream = new StreamReader(filename);
        while (!stream.EndOfStream)
        {
            string? line = stream.ReadLine();
            if (line == null)
                continue;

            var p = line.Split('\t');

            var intersection = new Intersection()
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
            };

            gazeController.Feed(intersection);

            await Task.Delay(1);
        }
    }
}
