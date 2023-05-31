namespace WackAMole.Plane
{
    /// <summary>
    /// Represent a SmartEye plane as a mirror
    /// </summary>
    internal class Mirror : Plane
    {
        public Mirror(string side) : base(side + "Mirror") { }
    }
}
