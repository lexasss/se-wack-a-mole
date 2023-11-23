using System;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace WackAMole
{
    internal enum ControllerType
    {
        Gaze,
        Mouse,
    }

    /// <summary>
    /// Stores all options
    /// </summary>
    internal class GameOptions
    {
        public enum Option
        {
            General,
            Parser,
            LowPassFilter,
            Controller,
            Game,
        }

        public static GameOptions Instance => _instance ??= new ();

        public event EventHandler<Option>? Changed;

        // Constant

        public int CellX { get; init;  } = 3;
        public int CellY { get; init; } = 1;
        public string ScreenName { get; init; } = "CentralConsole";
        public int ScreenWidth { get; init; } = (int)SystemParameters.PrimaryScreenWidth;
        public int ScreenHeight { get; init; } = (int)SystemParameters.PrimaryScreenHeight;
        public int PointsPerMole { get; init; } = 5;

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
        public bool UseSmartGazeCorrection
        {
            get => _useSmartGazeCorrection;
            set => Update(ref _useSmartGazeCorrection, value, Option.Game);     // well, not really a "game" option
        }
        public bool ShowGazeCursor
        {
            get => _showGazeCursor;
            set => Update(ref _showGazeCursor, value, Option.Game);
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

        // General

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

        // Load/Save

        /// <summary>
        /// Loads the options from the JSON file. Must be called at the very beginning of the application
        /// </summary>
        /// <param name="filename">The file that stores the options</param>
        /// <returns>Options object instance</returns>
        public static GameOptions Load(string filename)
        {
            if (File.Exists(filename))
            {
                using var reader = new StreamReader(filename);
                string json = reader.ReadToEnd();
                _instance = JsonSerializer.Deserialize<GameOptions>(json);
            }

            return Instance;
        }

        /// <summary>
        /// Saves the options to a JSON file 
        /// </summary>
        /// <param name="filename">The file to store the options</param>
        /// <exception cref="Exception">Throws is <see cref="GameOptions"/> instance is not created yet</exception>
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

        static GameOptions? _instance = null;

        int _dwellTime = 500;                   // ms
        bool _goNoGo = false;
        bool _lowPassFilterEnabled = true;
        double _lowPassFilterGain = 0.01;
        double _lowPassFilterWeightDamping = 0.8;   // unconditional next-gaze-point (on screen) weihgt damping
        int _moleTimerInterval = 1000;              // ms
        ControllerType _controller = ControllerType.Gaze;
        int _focusLatency = 500;                // ms
        double _noGoProbability = 0.3;          // 0..1
        int _lowPassFilterResetDelay = 500;     // ms
        double _currentCellExpansion = 0.1;     // share of the cell size
        int _shotDuration = 200;                // ms
        double _moleEventRate = 0.5;            // 0..1
        bool _useSmartGazeCorrection = false;
        bool _showGazeCursor = false;

        private void Update<T>(ref T member, T value, Option option = Option.General)
        {
            member = value;
            Changed?.Invoke(this, option);
        }
    }
}
