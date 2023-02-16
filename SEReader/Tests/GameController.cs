using SEReader.Game;
using SEReader.Utils;
using System;
using System.Threading.Tasks;

namespace SEReader.Tests
{
    internal class GameController
    {
        public static async Task Run(GazeController ctrl)
        {
            int count = 1000;
            bool isOnPlane = false;
            var options = Options.Instance;

            string screenName = options.ScreenName;
            var width = options.ScreenWidth;
            var height = options.ScreenHeight;

            Random random = new();

            Comm.Sample sample = new Comm.Sample()
            {
                ID = 1,
                TimeStamp = Timestamp.Ms,
                Intersections = new System.Collections.Generic.List<Comm.Intersection>()
            };

            while (--count >= 0)
            {
                if (random.NextDouble() < 0.02)
                {
                    sample.Intersections = new System.Collections.Generic.List<Comm.Intersection>()
                    {
                        new Comm.Intersection()
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
                        }
                    };
                }

                if (isOnPlane)
                {
                    ctrl.Feed(ref sample);
                }

                var prob = isOnPlane ? 0.01 : 0.3;
                if (random.NextDouble() < prob)
                {
                    isOnPlane = !isOnPlane;
                    ctrl.Notify(isOnPlane ? Tracker.Plane.Event.PlaneEnter : Tracker.Plane.Event.PlaneExit);
                }

                await Task.Delay(10);
            }
        }
    }
}
