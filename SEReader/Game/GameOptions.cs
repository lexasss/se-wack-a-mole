namespace SEReader.Game
{
    internal class GameOptions
    {
        public static GameOptions Instance => _instance ??= new();

        public int CellX { get; } = 6;
        public int CellY { get; } = 4;
        public string ScreenName { get; } = "LeftScreen";
        public int ScreenWidth { get; } = 2560;
        public int ScreenHeight { get; } = 1080;
        public int DwellTime { get; } = 500;    // ms
        public int ShotDuration { get; } = 200; // ms
        public int MoleTimerInterval { get; } = 1000; // ms
        public double MoleEventRate { get; } = 0.5; // 0..1
        public int PointsPerMole { get; } = 5;
        public bool GoNoGo { get; } = true;
        public double NoGoProbability { get; } = 0.3;   // 0..1
        public bool LowPassFilterEnabled { get; } = true;
        public double LowPassFilterGain { get; } = 0.01;
        public double LowPassFilterResetDelay { get; } = 500;
        public double CurrentCellExpansion { get; } = 0.1;  // share of the cell size

        // Internal methods

        static GameOptions _instance = null;

        protected GameOptions() { }
    }
}
