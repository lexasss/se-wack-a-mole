using System.IO;
using System.Text.Json;

namespace SEReader.Game
{
    internal enum IntersectionSource
    {
        Calibrated,
        Predicted
    }

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
        public IntersectionSource IntersectionSource { get; set; } = IntersectionSource.Calibrated;

        public static GameOptions Load(string filename)
        {
            if (File.Exists(filename))
            {
                using var reader = new StreamReader(filename);
                string json = reader.ReadToEnd();
                _instance = (GameOptions)JsonSerializer.Deserialize(json, typeof(GameOptions));
            }

            return Instance;
        }

        public static void Save(string filename)
        {
            if (_instance == null)
            {
                throw new System.Exception("Options do not exist");
            }

            string json = JsonSerializer.Serialize(_instance);
            using var writer = new StreamWriter(filename);
            writer.Write(json);
        }

        // Internal methods

        static GameOptions _instance = null;

        protected GameOptions() { }
    }
}
