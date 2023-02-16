using SEReader.Logging;
using SEReader.Utils;
using System;

namespace SEReader.Game
{
    [AllowScreenLog(ScreenLogger.Target.Cell)]
    public class Cell : ITickUpdatable
    {
        public enum State
        {
            Active,
            Inactive
        }

        public int X { get; }
        public int Y { get; }
        public bool IsFocused => _isFocused;

        public bool CanBeActivated { get; set; } = false;

        /// <summary>
        /// Fires when the cell is activated after its couner reaches the dwell-time, or the counter is reset
        /// </summary>
        public event EventHandler<State> ActivationChanged;

        public Cell(int x, int y)
        {
            X = x;
            Y = y;

            _screenLogger = ScreenLogger.Create();

            TickTimer.Add(this);
        }

        public bool IsAt(int x, int y) => X == x && Y == y;

        public void SetFocus()
        {
            _logger.Add(LogSource.Game, "cell", "focused", $"{Y},{X}");
            _screenLogger.Log($"{Y}/{X} focused");

            _isFocused = true;
        }

        public void RemoveFocus()
        {
            _screenLogger.Log($"{Y}/{X} left");
            _isFocused = false;

            if (_isActivated)
            {
                _isActivated = false;
                _attentionCounter = 0;

                _logger.Add(LogSource.Game, "cell", "off", $"{Y},{X}");
                ActivationChanged.Invoke(this, State.Inactive);
            }
        }

        public void Shoot()
        {
            _attentionCounter = _options.DwellTime + _options.ShotDuration;
            _isActivated = true;

            _logger.Add(LogSource.Game, "cell", "shot-on", $"{Y},{X}");
            ActivationChanged.Invoke(this, State.Active);
        }

        public void Reset()
        {
            _isFocused = false;
            _isActivated = false;
            _attentionCounter = 0;
        }

        /// <summary>
        /// Implementation of ITickUpdatable.
        /// DO NOT CALL IT EXPLICITELY!
        /// </summary>
        /// <param name="interval">time passed form the previous tick</param>
        public void Tick(int interval)
        {
            if (_isFocused)
            {
                if (_isActivated)
                {
                    DecreaseAttentionCounter(interval);
                }
                else if (_attentionCounter < _options.DwellTime)
                {
                    if (_options.Controller == ControllerType.Gaze && CanBeActivated)
                    {
                        IncreaseAttentionCounter(interval);
                    }
                }
            }
            else if (_isActivated)
            {
                DecreaseAttentionCounter(interval);
            }
            else
            {
                _attentionCounter = Math.Max(0, _attentionCounter - interval);
            }
        }

        // Internal

        readonly FlowLogger _logger = FlowLogger.Instance;
        readonly Options _options = Options.Instance;
        readonly ScreenLogger _screenLogger;

        int _attentionCounter = 0;
        bool _isFocused = false;
        bool _isActivated = false;


        private void DecreaseAttentionCounter(int interval)
        {
            _attentionCounter -= interval;
            if (_attentionCounter < _options.DwellTime)
            {
                _attentionCounter = 0;

                _logger.Add(LogSource.Game, "cell", "shot-off", $"{Y},{X}");
                ActivationChanged.Invoke(this, State.Inactive);
            }
        }

        private void IncreaseAttentionCounter(int interval)
        {
            _attentionCounter += interval;
            if (_attentionCounter >= _options.DwellTime)
            {
                Shoot();
            }
        }
    }
}