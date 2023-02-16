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

        public Game(GameRenderer renderer)
        {
            _renderer = renderer;

            for (int y = 0; y < _options.CellY; ++y)
            {
                for (int x = 0; x < _options.CellX; ++x)
                {
                    Cell cell = new Cell(x, y);
                    cell.ActivationChanged += Cell_ActivationChanged;
                    _cells.Add(cell);
                }
            }

            _timer.Elapsed += Timer_Elapsed;

            _screenLogger = ScreenLogger.Create();

            _options.Changed += Options_Changed;
            Options_Changed(null, Options.Option.Game);
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

            _mole.Reset();
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

        /// <summary>
        /// Displayes the shooting target
        /// </summary>
        /// <param name="x">X of a cell to be shot</param>
        /// <param name="y">Y of a cell to be shot</param>
        public void Shoot(int x, int y)
        {
            Cell cell = _cells[y * _options.CellX + x];
            cell.Shoot();
        }

        // Internal

        readonly List<Cell> _cells = new();
        readonly Timer _timer = new();
        readonly FlowLogger _logger = FlowLogger.Instance;
        readonly Options _options = Options.Instance;
        readonly ScreenLogger _screenLogger;
        readonly GameRenderer _renderer;

        Cell _focusedCell = null;
        Cell _shotCell = null;

        Mole _mole = new Mole();
        int _score = 0;

        private void ReverseMoleVisibility()
        {
            if (_mole.IsVisible)
            {
                _cells[_mole.Y * _options.CellX + _mole.X].CanBeActivated = false;
            }

            _mole.ReverseVisibility();

            if (_mole.IsVisible)
            {
                _cells[_mole.CellIndex].CanBeActivated = true;
                _renderer.SetMole(_mole.Type);
            }

            var moleVisibility = _mole.IsVisible ? TargetVisibility.Visible : TargetVisibility.Hidden;
            _renderer.SetMoleVisibility(moleVisibility, _mole.X, _mole.Y);

            _logger.Add(LogSource.Experiment, "mole", moleVisibility.ToString(), $"{_mole.X},{_mole.Y}", _mole.Type.ToString().ToLower());
        }

        private void Cell_ActivationChanged(object sender, Cell.Activity e)
        {
            Cell cell = sender as Cell;

            if (e == Cell.Activity.Active)
            {
                _renderer.SetShotVisibility(TargetVisibility.Visible, cell.X, cell.Y);
                _shotCell = cell;

                if (_mole.X == cell.X && _mole.Y == cell.Y)   // we shot the mole!
                {
                    ReverseMoleVisibility();

                    if (_mole.Type == MoleType.Go)
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
            if (_mole.IsTimeToReverseVisibility)
            {
                ReverseMoleVisibility();
            }
        }

        private void Options_Changed(object sender, Options.Option e)
        {
            if (e == Options.Option.Game)
            {
                _timer.Interval = _options.MoleTimerInterval;
            }
        }
    }
}
