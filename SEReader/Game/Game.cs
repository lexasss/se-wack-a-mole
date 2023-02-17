using SEReader.Logging;
using System;
using System.Collections.Generic;
using System.Timers;

namespace SEReader.Game
{
    [AllowScreenLog(ScreenLogger.Target.Game)]
    public class Game
    {
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

            _renderer.Hide(GameRenderer.Target.Mole);
            _renderer.Hide(GameRenderer.Target.Focus);
            _renderer.Hide(GameRenderer.Target.Shot);

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
                _renderer.Hide(GameRenderer.Target.Shot);
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
            else if (!_focusedCell?.IsAt(x, y) ?? true)
            {
                hasChanged = true;
                _focusedCell?.RemoveFocus();

                _focusedCell = _cells.Find(cell => cell.X == x && cell.Y == y);
                _focusedCell?.SetFocus();
            }

            if (hasChanged)
            {
                _screenLogger?.Log(_focusedCell == null ?
                    "unfocused" :
                    $"focused {_focusedCell.X} {_focusedCell.Y}");

                if (_focusedCell == null)
                    _renderer.Hide(GameRenderer.Target.Focus);
                else
                    _renderer.Show(GameRenderer.Target.Focus, _focusedCell.X, _focusedCell.Y);
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

        readonly List<Cell> _cells = new ();
        readonly Timer _timer = new ();
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
                _renderer.SetMoleType(_mole.Type);
                _renderer.Show(GameRenderer.Target.Mole, _mole.X, _mole.Y);
            }
            else
            {
                _renderer.Hide(GameRenderer.Target.Mole);
            }

            _logger.Add(LogSource.Game, "mole", _mole.IsVisible ? "shown" : "hidden", $"{_mole.X},{_mole.Y}", _mole.Type.ToString().ToLower());
        }

        private void Cell_ActivationChanged(object sender, Cell.State e)
        {
            Cell cell = sender as Cell;

            if (e == Cell.State.Active)
            {
                _renderer.Show(GameRenderer.Target.Shot, cell.X, cell.Y);
                _shotCell = cell;

                if (_mole.IsInCell(cell))   // we shot the mole!
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

                    _logger.Add(LogSource.Game, "score", _score.ToString());
                    _renderer.SetScore(_score);
                }
            }
            else if (_shotCell != null)
            {
                if (_shotCell.X == cell.X && _shotCell.Y == cell.Y)
                {
                    _renderer.Hide(GameRenderer.Target.Shot);
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
