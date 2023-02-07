using SEReader.Logging;
using System;
using System.Collections.Generic;
using System.Timers;
using System.Windows.Media.Media3D;

// go/no-go

namespace SEReader.Game
{
    public class Game
    {
        public enum TargetVisibility
        {
            Hidden,
            Visible
        }

        public enum Mole
        {
            Go,
            NoGo,
        }

        public class TargetVisibilityChangedArgs : EventArgs
        {
            public TargetVisibility Visibility { get; }
            public int CellX { get; }
            public int CellY { get; }
            public TargetVisibilityChangedArgs(TargetVisibility visibility, int x = -1, int y = -1)
            {
                Visibility = visibility;
                CellX = x;
                CellY = y;
            }
        }

        public int CellCountX { get; private set; }
        public int CellCountY { get; private set; }

        public Cell[] Cells => _cells.ToArray();

        public bool IsRunning => _timer.Enabled;

        public event EventHandler<Mole> MoleChanged;
        public event EventHandler<TargetVisibilityChangedArgs> MoleVisibilityChanged;
        public event EventHandler<TargetVisibilityChangedArgs> ShotVisibilityChanged;
        public event EventHandler<TargetVisibilityChangedArgs> FocusVisibilityChanged;
        public event EventHandler<int> ScoreChanged;

        public Game(int cellsX, int cellsY)
        {
            CellCountX = cellsX;
            CellCountY = cellsY;

            for (int y = 0; y < CellCountY; ++y)
            {
                for (int x = 0; x < CellCountX; ++x)
                {
                    Cell cell = new Cell(x, y);
                    cell.ActivationChanged += Cell_ActivationChanged;
                    _cells.Add(cell);
                }
            }

            _timer.Interval = 1000;
            _timer.Elapsed += Timer_Elapsed;
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();

            foreach (var cell in _cells)
                cell.Reset();

            MoleVisibilityChanged?.Invoke(this, new TargetVisibilityChangedArgs(TargetVisibility.Hidden));
            FocusVisibilityChanged?.Invoke(this, new TargetVisibilityChangedArgs(TargetVisibility.Hidden));
            ShotVisibilityChanged?.Invoke(this, new TargetVisibilityChangedArgs(TargetVisibility.Hidden));

            _moleX = -1;
            _moleY = -1;
            _score = 0;

            _focusedCell = null;
            _shotCell = null;

            ScoreChanged(this, _score);
        }

        /// <summary>
        /// Removes any focus/shot
        /// </summary>
        public void Clear()
        {
            Focus(-1, -1);

            if (_shotCell != null)
            {
                ShotVisibilityChanged(this, new TargetVisibilityChangedArgs(TargetVisibility.Hidden));
                _shotCell = null;
            }
        }

        /// <summary>
        /// Moves focus to the cell specified via its coordinates
        /// </summary>
        /// <param name="x">X of a cell to be focused; negative value removes any current focus</param>
        /// <param name="y">Y of a cell to be focused; negative value removes any current focus</param>
        public void Focus(int x, int y)
        {
            bool hasChanged = false;

            if (x < 0 || y < 0)
            {
                if (_focusedCell != null)
                {
                    hasChanged = true;
                    _focusedCell.RemoveFocus();
                    _focusedCell = null;
                }
            }
            else if (!_focusedCell?.Matches(x, y) ?? true)
            {
                hasChanged = true;
                _focusedCell?.RemoveFocus();

                _focusedCell = _cells.Find(cell => cell.X == x && cell.Y == y);
                _focusedCell?.SetFocus();
            }

            if (hasChanged)
            {
                FocusVisibilityChanged?.Invoke(this, new TargetVisibilityChangedArgs(
                    _focusedCell == null ? TargetVisibility.Hidden : TargetVisibility.Visible,
                    _focusedCell?.X ?? -1, _focusedCell?.Y ?? -1));
            }
        }

        /// <summary>
        /// Moves focus to the cell specified via its coordinates, and shoots immediately
        /// </summary>
        /// <param name="x">X of a cell to be focused; negative value removes any current focus</param>
        /// <param name="y">Y of a cell to be focused; negative value removes any current focus</param>
        public void FocusAndShoot(int x, int y)
        {
            Focus(x, y);

            Cell cell = _cells[y * CellCountX + x];
            cell.Shoot();
        }

        // Internal

        readonly List<Cell> _cells = new List<Cell>();
        readonly Timer _timer = new Timer();
        readonly Random _random = new Random();
        readonly FlowLogger _logger = FlowLogger.Instance;
        readonly GameOptions _options = GameOptions.Instance;

        Cell _focusedCell = null;
        Cell _shotCell = null;

        bool _isMoleVisible = false;
        int _moleX = -1;
        int _moleY = -1;
        int _score = 0;
        Mole _mole = Mole.Go;

        private void ReverseMoleVisibility()
        {
            _isMoleVisible = !_isMoleVisible;

            _moleX = _isMoleVisible ? (int)(_random.NextDouble() * CellCountX) : -1;
            _moleY = _isMoleVisible ? (int)(_random.NextDouble() * CellCountY) /*2*/ : -1;
            var moleVisibility = _isMoleVisible ? TargetVisibility.Visible : TargetVisibility.Hidden;

            if (_isMoleVisible)
            {
                _mole = _random.NextDouble() < 0.5 ? Mole.Go : Mole.NoGo;
                MoleChanged(this, _mole);
            }

            MoleVisibilityChanged?.Invoke(this, new TargetVisibilityChangedArgs(moleVisibility, _moleX, _moleY));

            _logger.Add(LogSource.Experiment, "mole", moleVisibility.ToString(), $"{_moleX},{_moleY}", _mole.ToString().ToLower());
        }

        private void Cell_ActivationChanged(object sender, Cell.Activity e)
        {
            Cell cell = sender as Cell;

            if (e == Cell.Activity.Active)
            {
                ShotVisibilityChanged(this, new TargetVisibilityChangedArgs(TargetVisibility.Visible, cell.X, cell.Y));
                _shotCell = cell;

                if (_moleX == cell.X && _moleY == cell.Y)
                {
                    ReverseMoleVisibility();

                    if (_mole == Mole.Go)
                    {
                        _score += _options.PointsPerMole;
                    }
                    else
                    {
                        _score = Math.Max(0, _score - _options.PointsPerMole);
                    }

                    _logger.Add(LogSource.Experiment, "score", _score.ToString());
                    ScoreChanged?.Invoke(this, _score);
                }
            }
            else if (_shotCell != null)
            {
                if (_shotCell.X == cell.X && _shotCell.Y == cell.Y)
                {
                    ShotVisibilityChanged(this, new TargetVisibilityChangedArgs(TargetVisibility.Hidden));
                    _shotCell = null;
                }
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_random.NextDouble() < 0.5)
            {
                ReverseMoleVisibility();
            }
        }
    }
}
