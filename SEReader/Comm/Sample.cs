using System.Collections.Generic;

namespace SEReader.Comm
{
    public struct Intersection
    {
        public int ID;
        public string PlaneName;
        public Point3D Gaze;
        public Point2D Point;
    }

    public struct Sample
    {
        public int ID;
        public long TimeStamp;
        public List<Intersection> Intersections;
    }
}
