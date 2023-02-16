using System.Collections.Generic;

namespace SEReader.Tracker
{
    internal class PlaneCollection
    {
        public void Add(params Plane[] plane)
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

        // Internal

        List<Plane> _planes = new();
    }
}
