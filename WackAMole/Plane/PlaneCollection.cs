using WackAMole.Comm;
using System.Collections.Generic;

namespace WackAMole.Plane
{
    /// <summary>
    /// Maintaince a collection of planes/screens
    /// </summary>
    internal class PlaneCollection
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="planes">a list of planes/screens</param>
        public PlaneCollection(params Plane[] planes)
        {
            _planes.AddRange(planes);
        }

        /// <summary>
        /// Passes the gaze-on and gaze-off events to the correct plane/screen
        /// </summary>
        /// <param name="evt">Event name</param>
        /// <param name="name">Plane/screen that received it</param>
        public void Notify(Plane.Event evt, string name)
        {
            foreach (var plane in _planes)
            {
                if (plane.PlaneName == name)
                {
                    plane.Notify(evt);
                    break;
                }
            }
        }

        /// <summary>
        /// Consumes a gaze sample
        /// </summary>
        /// <param name="sample">gae sample</param>
        public void Feed(ref Sample sample)
        {
            foreach (var ints in sample.Intersections)
            {
                var plane = _planes.Find(plane => plane.PlaneName == ints.PlaneName);
                plane?.Feed(ints);
            }
        }

        // Internal

        List<Plane> _planes = new ();
    }
}
