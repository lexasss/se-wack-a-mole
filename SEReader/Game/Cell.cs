using SEReader.Logging;
using SEReader.Utils;
using System;
using System.Diagnostics;

namespace SEReader.Game
{
    public class Cell : ITickUpdatable
    {
        public enum Activity
        {
            Active,
            Inactive
        }

        public int X { get; }
        public int Y { get; }
        public bool IsFocused => _isFocused;

        /// <summary>
        /// Fires when the cell is activated after its couner reaches the dwell-time, or the counter is reset
        /// </summary>
        public event EventHandler<Activity> ActivationChanged;

        public Cell(int x, int y)
        {
            X = x;
            Y = y;

            TickTimer.Add(this);
        }

        public bool Matches(int x, int y) => X == x && Y == y;

        public void SetFocus()
        {
            _logger.Add(LogSource.Experiment, "cell", "focused", $"{Y},{X}");
            Debug.WriteLine($"{Y}/{X} focused");

            _isFocused = true;
        }

        public void Shoot()
        {
            //_isActivated = false;
            //_attentionCounter = _options.DwellTime - 1;
            _attentionCounter = _options.DwellTime + _options.ShotDuration;
            _isActivated = true;

            _logger.Add(LogSource.Experiment, "cell", "shot-on", $"{Y},{X}");
            ActivationChanged.Invoke(this, Activity.Active);
        }

        public void RemoveFocus()
        {
            Debug.WriteLine($"{Y}/{X} left");
            _isFocused = false;

            if (_isActivated)
            {
                _isActivated = false;
                _attentionCounter = 0;

                _logger.Add(LogSource.Experiment, "cell", "target-off", $"{Y},{X}");
                ActivationChanged.Invoke(this, Activity.Inactive);
            }
        }

        public void Reset()
        {
            _isFocused = false;
            _isActivated = false;
            _attentionCounter = 0;
        }

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
                    if (_options.Controller == Controller.Gaze)
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

        int _attentionCounter = 0;
        bool _isFocused = false;
        bool _isActivated = false;


        private void DecreaseAttentionCounter(int interval)
        {
            _attentionCounter -= interval;
            if (_attentionCounter < _options.DwellTime)
            {
                _attentionCounter = 0;

                _logger.Add(LogSource.Experiment, "cell", "shot-off", $"{Y},{X}");
                ActivationChanged.Invoke(this, Activity.Inactive);
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