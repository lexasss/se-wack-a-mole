using SEReader.Comm;

namespace SEReader.Tracker
{
    internal class Mirror : Plane
    {
        public Mirror(string side) : base(side + "Mirror") { }

        // Internal

        protected override void HandleIntersection(Intersection intersection)
        {
            // TODO: we probably do not need to do anything here
        }

        protected override void HandleEvent(Event evt)
        {
            // TODO: we probably do not need to do anything here
        }
    }
}
