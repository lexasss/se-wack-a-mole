using SEReader.Utils;
using System;
using System.Threading.Tasks;

namespace SEReader.Tests
{
    internal class GameController
    {
        public static async Task Run(Experiment.Observer ctrl)
        {
            int count = 1000;
            bool isOnPlane = false;
            Random random = new Random();

            Comm.Intersection intersection = new Comm.Intersection()
            {
                ID = 1,
                PlaneName = "Windshield",
                Gaze = new Comm.Point3D() {
                    X = random.NextDouble(),
                    Y = random.NextDouble(),
                    Z = 0,
                },
                Point = new Comm.Point2D()
                {
                    X = random.NextDouble(),
                    Y = random.NextDouble(),
                }
            };
            Comm.Sample sample = new Comm.Sample()
            {
                ID = 1,
                TimeStamp = Timestamp.Ms,
                Intersections = new System.Collections.Generic.List<Comm.Intersection>() { intersection }
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
                            PlaneName = "Windshield",
                            Gaze = new Comm.Point3D()
                            {
                                X = random.NextDouble(),
                                Y = random.NextDouble(),
                                Z = 0,
                            },
                            Point = new Comm.Point2D()
                            {
                                X = random.NextDouble(),
                                Y = random.NextDouble(),
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
                //if (!isOnPlane)
                {
                    isOnPlane = !isOnPlane;
                    ctrl.Notify(isOnPlane ? Experiment.Observer.Event.PlaneEnter : Experiment.Observer.Event.PlaneExit);
                }

                await Task.Delay(10);
            }
        }
    }
}
