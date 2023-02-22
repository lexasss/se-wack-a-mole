using System;

namespace SEReader.Game
{
    public enum MoleType
    {
        Go,
        NoGo,
    }

    /// <summary>
    /// Mole data
    /// </summary>
    internal class Mole
    {
        public bool IsVisible { get; private set; } = false;
        public int X { get; private set; } = -1;
        public int Y { get; private set; } = -1;
        public MoleType Type { get; private set; } = MoleType.Go;

        /// <summary>
        /// Generates a random event for the mole to be shown/hidden
        /// </summary>
        public bool IsTimeToReverseVisibility => _random.NextDouble() < _options.MoleEventRate;

        /// <summary>
        /// Checks if the mole is in the given cell
        /// </summary>
        /// <param name="cell">Coordinates to check</param>
        /// <returns>True if the mole in the given cell</returns>
        public bool IsInCell(Cell cell) => X == cell.X && Y == cell.Y;

        /// <summary>
        /// Resets mole data
        /// </summary>
        public void Reset()
        {
            IsVisible = false;
            X = -1;
            Y = -1;
        }

        /// <summary>
        /// To be called when the mode vilibility status was changed
        /// </summary>
        public void ReverseVisibility()
        {
            IsVisible = !IsVisible;
            X = IsVisible ? (int)(_random.NextDouble() * _options.CellX) : -1;
            Y = IsVisible ? (int)(_random.NextDouble() * _options.CellY) : -1;
            if (IsVisible)
            {
                Type = _options.GoNoGo && _random.NextDouble() < _options.NoGoProbability ? MoleType.NoGo : MoleType.Go;
            }
        }

        // Internal

        readonly Random _random = new ();
        readonly Options _options = Options.Instance;
    }
}
