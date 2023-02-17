using SEReader.Game;
using System;
using System.Threading.Tasks;

namespace SEReader.Tests
{
    internal class GazeController
    {
        public static async Task Run(Game.GazeController ctrl)
        {
            int count = 1000;
            bool isOnPlane = false;
            var options = Options.Instance;

            string screenName = options.ScreenName;
            var width = options.ScreenWidth;
            var height = options.ScreenHeight;

            Random random = new();

            Comm.Intersection intersection = new()
            {
                ID = 1,
                PlaneName = screenName,
                Gaze = new Comm.Point3D()
                {
                    X = random.NextDouble(),
                    Y = random.NextDouble(),
                    Z = 0,
                },
                Point = new Comm.Point2D()
                {
                    X = random.NextDouble() * width,
                    Y = random.NextDouble() * height,
                }
            };

            while (--count >= 0)
            {
                intersection.ID += 1;

                if (random.NextDouble() < 0.02)
                {
                    intersection = new()
                    {
                        ID = intersection.ID,
                        PlaneName = screenName,
                        Gaze = new Comm.Point3D()
                        {
                            X = random.NextDouble(),
                            Y = random.NextDouble(),
                            Z = 0,
                        },
                        Point = new Comm.Point2D()
                        {
                            X = random.NextDouble() * width,
                            Y = random.NextDouble() * height,
                        }
                    };
                }

                if (isOnPlane)
                {
                    ctrl.Feed(intersection);
                }

                var prob = isOnPlane ? 0.01 : 0.3;
                if (random.NextDouble() < prob)
                {
                    isOnPlane = !isOnPlane;
                    ctrl.Notify(isOnPlane ? Plane.Plane.Event.Enter : Plane.Plane.Event.Exit);
                }

                await Task.Delay(10);
            }
        }
    }
}
