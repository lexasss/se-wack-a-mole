namespace SEReader.Comm
{
    public class Point3D
    {
        public double X;
        public double Y;
        public double Z;

        public static Point3D Parse(string text)
        {
            string[] v = text.Split(' ');
            return new Point3D()
            {
                X = double.Parse(v[0]),
                Y = double.Parse(v[1]),
                Z = double.Parse(v[2]),
            };
        }
    }

    public class Point2D
    {
        public double X;
        public double Y;

        public static Point2D Parse(string text)
        {
            string[] v = text.Split(' ');
            return new Point2D()
            {
                X = double.Parse(v[0]),
                Y = double.Parse(v[1]),
            };
        }
    }

}
