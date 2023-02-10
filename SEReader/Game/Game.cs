using SEReader.Logging;
using System;
using System.Collections.Generic;
using System.Timers;

namespace SEReader.Game
{
    [AllowScreenLog(ScreenLogger.Target.Game)]
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

        public Cell[] Cells => _cells.ToArray();

        public bool IsRunning => _timer.Enabled;

        public Game(GameRenderer renderer)
        {
            _renderer = renderer;
            _renderer.CellClicked += Renderer_CellClicked;

            for (int y = 0; y < _options.CellY; ++y)
            {
                for (int x = 0; x < _options.CellX; ++x)
                {
                    Cell cell = new Cell(x, y);
                    cell.ActivationChanged += Cell_ActivationChanged;
                    _cells.Add(cell);
                }
            }

            _timer.Interval = _options.MoleTimerInterval;
            _timer.Elapsed += Timer_Elapsed;

            _screenLogger = ScreenLogger.Create();
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

            _renderer.SetMoleVisibility(TargetVisibility.Hidden);
            _renderer.SetFocusVisibility(TargetVisibility.Hidden);
            _renderer.SetShotVisibility(TargetVisibility.Hidden);

            _moleX = -1;
            _moleY = -1;
            _score = 0;

            _focusedCell = null;
            _shotCell = null;

            _renderer.SetScore(_score);
        }

        /// <summary>
        /// Removes any focus/shot
        /// </summary>
        public void ClearFocus()
        {
            Focus(-1, -1);

            if (_shotCell != null)
            {
                _renderer.SetShotVisibility(TargetVisibility.Hidden);
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
                    _screenLogger?.Log("focus removed");
                }
            }
            else if (!_focusedCell?.Matches(x, y) ?? true)
            {
                hasChanged = true;
                _focusedCell?.RemoveFocus();

                _focusedCell = _cells.Find(cell => cell.X == x && cell.Y == y);
                _focusedCell?.SetFocus();
                _screenLogger?.Log(_focusedCell == null ?
                    "focus removed" :
                    $"{_focusedCell.X} {_focusedCell.Y}");
            }

            if (hasChanged)
            {
                _renderer.SetFocusVisibility(
                    _focusedCell == null ? TargetVisibility.Hidden : TargetVisibility.Visible,
                    _focusedCell?.X ?? -1, _focusedCell?.Y ?? -1);
            }
        }

        // Internal

        readonly List<Cell> _cells = new();
        readonly Timer _timer = new();
        readonly Random _random = new();
        readonly FlowLogger _logger = FlowLogger.Instance;
        readonly GameOptions _options = GameOptions.Instance;
        readonly ScreenLogger _screenLogger;
        readonly GameRenderer _renderer;

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

            _moleX = _isMoleVisible ? (int)(_random.NextDouble() * _options.CellX) : -1;
            _moleY = _isMoleVisible ? (int)(_random.NextDouble() * _options.CellY) /*2*/ : -1;
            var moleVisibility = _isMoleVisible ? TargetVisibility.Visible : TargetVisibility.Hidden;

            if (_isMoleVisible)
            {
                _mole = _random.NextDouble() < _options.NoGoProbability ? Mole.NoGo : Mole.Go;
                _renderer.SetMole(_mole);
            }

            _renderer.SetMoleVisibility(moleVisibility, _moleX, _moleY);

            _logger.Add(LogSource.Experiment, "mole", moleVisibility.ToString(), $"{_moleX},{_moleY}", _mole.ToString().ToLower());
        }

        private void Renderer_CellClicked(object sender, GameRenderer.CellClickedEventArgs e)
        {
            Focus(e.X, e.Y);

            Cell cell = _cells[e.Y * _options.CellX + e.X];
            cell.Shoot();
        }

        private void Cell_ActivationChanged(object sender, Cell.Activity e)
        {
            Cell cell = sender as Cell;

            if (e == Cell.Activity.Active)
            {
                _renderer.SetShotVisibility(TargetVisibility.Visible, cell.X, cell.Y);
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
                    _renderer.SetScore(_score);
                }
            }
            else if (_shotCell != null)
            {
                if (_shotCell.X == cell.X && _shotCell.Y == cell.Y)
                {
                    _renderer.SetShotVisibility(TargetVisibility.Hidden);
                    _shotCell = null;
                }
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_random.NextDouble() < _options.MoleEventRate)
            {
                ReverseMoleVisibility();
            }
        }
    }
}
