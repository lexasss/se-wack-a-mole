using SEReader.Comm;
using System.Collections.Generic;

namespace SEReader.Plane
{
    internal class PlaneCollection
    {
        public PlaneCollection(params Plane[] plane)
        {
            _planes.AddRange(plane);
        }

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
