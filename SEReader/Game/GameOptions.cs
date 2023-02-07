namespace SEReader.Game
{
    internal class GameOptions
    {
        public static GameOptions Instance => _instance ??= new();

        public int DwellTime { get; } = 500;
        public int ShotDuration { get; } = 200;
        public int PointsPerMole { get; } = 5;
        public bool GoNoGo { get; } = true;

        // Internal methods

        static GameOptions _instance = null;

        protected GameOptions() { }
    }
}
