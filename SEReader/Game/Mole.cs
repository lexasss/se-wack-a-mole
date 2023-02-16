using System;

namespace SEReader.Game
{
    public enum MoleType
    {
        Go,
        NoGo,
    }

    internal class Mole
    {
        public bool IsVisible { get; private set; } = false;
        public int X { get; private set; } = -1;
        public int Y { get; private set; } = -1;
        public MoleType Type { get; private set; } = MoleType.Go;

        public int CellIndex => IsVisible ? Y * _options.CellX + X : -1;
        public bool IsTimeToReverseVisibility => _random.NextDouble() < _options.MoleEventRate;

        public bool IsInCell(Cell cell) => X == cell.X && Y == cell.Y;

        public void Reset()
        {
            IsVisible = false;
            X = -1;
            Y = -1;
        }

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

        readonly Random _random = new();
        readonly Options _options = Options.Instance;
    }
}
