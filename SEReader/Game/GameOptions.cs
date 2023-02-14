using System;
using System.IO;
using System.Text.Json;
using System.Windows;

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

        public event EventHandler Changed;

        // Constant

        public int CellX { get; } = 4;
        public int CellY { get; } = 2;
        public string ScreenName { get; } = "LeftScreen";
        public int ScreenWidth { get; } = (int)SystemParameters.PrimaryScreenWidth;
        public int ScreenHeight { get; } = (int)SystemParameters.PrimaryScreenHeight;

        // Adjustable / potentionally adjustable

        public int DwellTime
        {
            get => _dwellTime;
            set => Update(ref _dwellTime, value);
        }
        public int ShotDuration { get; } = 200; // ms
        public int MoleTimerInterval
        {
            get => _moleTimerInterval;
            set => Update(ref _moleTimerInterval, value);
        }
        public double MoleEventRate { get; } = 0.5; // 0..1
        public int PointsPerMole { get; } = 5;
        public bool GoNoGo
        {
            get => _goNoGo;
            set => Update(ref _goNoGo, value);
        }
        public double NoGoProbability { get; } = 0.3;   // 0..1
        public bool LowPassFilterEnabled
        {
            get => _lowPassFilterEnabled;
            set => Update(ref _lowPassFilterEnabled, value);
        }
        public double LowPassFilterGain { get; } = 0.01;
        public double LowPassFilterResetDelay { get; } = 500;
        public double CurrentCellExpansion { get; } = 0.1;  // share of the cell size
        public IntersectionSource IntersectionSource
        {
            get => _intersectionSource;
            set => Update(ref _intersectionSource, value);
        }

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

        int _dwellTime = 500;                   // ms
        bool _goNoGo = false;
        bool _lowPassFilterEnabled = true;
        int _moleTimerInterval = 1000;          // ms
        IntersectionSource _intersectionSource = IntersectionSource.Calibrated;

        void Update<T>(ref T member, T value)
        {
            member = value;
            Changed?.Invoke(this, new EventArgs());
        }

        protected GameOptions() { }
    }
}
