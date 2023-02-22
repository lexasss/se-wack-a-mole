using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace SEReader
{
    internal enum ControllerType
    {
        Gaze,
        Mouse,
    }

    internal enum IntersectionSource
    {
        Gaze,
        AI,
    }

    /// <summary>
    /// Stores all options
    /// </summary>
    internal class Options
    {
        public enum Option
        {
            General,
            Parser,
            LowPassFilter,
            Controller,
            Game,
        }

        public static Options Instance => _instance ??= new ();

        public event EventHandler<Option> Changed;

        // Constant

        public int CellX { get; } = 4;
        public int CellY { get; } = 2;
        public string ScreenName { get; } = "LeftScreen";
        public int ScreenWidth { get; } = (int)SystemParameters.PrimaryScreenWidth;
        public int ScreenHeight { get; } = (int)SystemParameters.PrimaryScreenHeight;
        public int PointsPerMole { get; } = 5;

        // Adjustable

        // Game

        public int MoleTimerInterval
        {
            get => _moleTimerInterval;
            set => Update(ref _moleTimerInterval, value, Option.Game);
        }
        public double MoleEventRate
        {
            get => _moleEventRate;
            set => Update(ref _moleEventRate, value, Option.Game);
        }
        public bool GoNoGo
        {
            get => _goNoGo;
            set => Update(ref _goNoGo, value, Option.Game);
        }
        public double NoGoProbability
        {
            get => _noGoProbability;
            set => Update(ref _noGoProbability, value, Option.Game);
        }

        // Low-pass filter

        public bool LowPassFilterEnabled
        {
            get => _lowPassFilterEnabled;
            set => Update(ref _lowPassFilterEnabled, value, Option.LowPassFilter);
        }
        public double LowPassFilterGain
        {
            get => _lowPassFilterGain;
            set => Update(ref _lowPassFilterGain, value, Option.LowPassFilter);
        }
        public int LowPassFilterResetDelay
        {
            get => _lowPassFilterResetDelay;
            set => Update(ref _lowPassFilterResetDelay, value, Option.LowPassFilter);
        }
        public double LowPassFilterWeightDamping
        {
            get => _lowPassFilterWeightDamping;
            set => Update(ref _lowPassFilterWeightDamping, value, Option.LowPassFilter);
        }

        // Controller

        public double FocusedCellExpansion
        {
            get => _currentCellExpansion;
            set => Update(ref _currentCellExpansion, value, Option.Controller);
        }
        public int FocusLatency
        {
            get => _focusLatency;
            set => Update(ref _focusLatency, value, Option.Controller);
        }
        public ControllerType Controller
        {
            get => _controller;
            set => Update(ref _controller, value, Option.Controller);
        }

        public int DwellTime
        {
            get => _dwellTime;
            set => Update(ref _dwellTime, value);
        }
        public int ShotDuration
        {
            get => _shotDuration;
            set => Update(ref _shotDuration, value);
        }

        // Parser

        public IntersectionSource IntersectionSource
        {
            get => _intersectionSource;
            set => Update(ref _intersectionSource, value, Option.Parser);
        }
        public bool IntersectionSourceFiltered
        {
            get => _intersectionSourceFiltered;
            set => Update(ref _intersectionSourceFiltered, value, Option.Parser);
        }
        public bool UseGazeQualityMeasurement
        {
            get => _useGazeQualityMeasurement;
            set => Update(ref _useGazeQualityMeasurement, value, Option.Parser);
        }
        public double GazeQualityThreshold
        {
            get => _gazeQualityThreshold;
            set => Update(ref _gazeQualityThreshold, value, Option.Parser);
        }

        // Load/Save

        /// <summary>
        /// Loads the options from the JSON file. Must be called at the very beginning of the application
        /// </summary>
        /// <param name="filename">The file that stores the options</param>
        /// <returns>Options object instance</returns>
        public static Options Load(string filename)
        {
            if (File.Exists(filename))
            {
                using var reader = new StreamReader(filename);
                string json = reader.ReadToEnd();
                _instance = (Options)JsonSerializer.Deserialize(json, typeof(Options));
            }

            return Instance;
        }

        /// <summary>
        /// Saves the options to a JSON file 
        /// </summary>
        /// <param name="filename">The file to store the options</param>
        /// <exception cref="Exception">Throws is <see cref="Options"/> instance is not created yet</exception>
        public static void Save(string filename)
        {
            if (_instance == null)
            {
                throw new Exception("Options do not exist");
            }

            string json = JsonSerializer.Serialize(_instance);
            using var writer = new StreamWriter(filename);
            writer.Write(json);
        }

        // Internal

        static Options _instance = null;

        int _dwellTime = 500;                   // ms
        bool _goNoGo = false;
        bool _lowPassFilterEnabled = true;
        double _lowPassFilterGain = 0.01;
        double _lowPassFilterWeightDamping = 0.8;   // unconditional next-gaze-point (on screen) weihgt damping
        int _moleTimerInterval = 1000;              // ms
        ControllerType _controller = ControllerType.Gaze;
        IntersectionSource _intersectionSource = IntersectionSource.Gaze;
        bool _intersectionSourceFiltered = false;
        int _focusLatency = 500;                // ms
        double _noGoProbability = 0.3;          // 0..1
        int _lowPassFilterResetDelay = 500;     // ms
        double _currentCellExpansion = 0.1;     // share of the cell size
        int _shotDuration = 200;                // ms
        double _moleEventRate = 0.5;            // 0..1
        bool _useGazeQualityMeasurement = false;
        double _gazeQualityThreshold = 0.5;

        private void Update<T>(ref T member, T value, Option option = Option.General)
        {
            member = value;
            Changed?.Invoke(this, option);
        }

        protected Options() { }
    }
}
