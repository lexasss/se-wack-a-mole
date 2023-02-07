using SEReader.Comm;
using System.Diagnostics;

namespace SEReader.Experiment
{
    internal class Mirror : Observer
    {
        public Mirror(string planeName) : base(planeName) { }

        // Internal

        protected override void HandleIntersection(Intersection intersection)
        {
            // TODO: handle it properly here,
            // for example:
            if (intersection.Point.X < 1000)
            {
                Debug.WriteLine("gazing left");
            }
            else
            {
                Debug.WriteLine("gazing right");
            }
        }

        protected override void HandleEvent(Event evt)
        {
            // TODO: handle it properly here,
        }
    }
}
