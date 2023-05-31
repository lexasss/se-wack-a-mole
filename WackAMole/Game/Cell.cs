using WackAMole.Logging;
using WackAMole.Utils;
using System;

namespace WackAMole.Game
{
    public class Cell : ITickUpdatable
    {
        public enum State
        {
            Active,
            Inactive
        }

        public int X { get; }
        public int Y { get; }

        public bool CanBeActivated { get; set; } = false;

        /// <summary>
        /// Fires when the cell is activated after its couner reaches the dwell-time, or the counter is reset
        /// </summary>
        public event EventHandler<State> ActivationChanged;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="x">Location X</param>
        /// <param name="y">Location Y</param>
        public Cell(int x, int y)
        {
            X = x;
            Y = y;

            TickTimer.Add(this);
        }

        /// <summary>
        /// Checks whether the cell is located in the given location
        /// </summary>
        /// <param name="x">Location X</param>
        /// <param name="y">Location Y</param>
        /// <returns>Check result</returns>
        public bool IsAt(int x, int y) => X == x && Y == y;

        /// <summary>
        /// Marks the cell bearing the focus
        /// </summary>
        public void SetFocus()
        {
            _logger.Add(LogSource.Game, "cell", "focused", $"{Y},{X}");

            _isFocused = true;
        }

        /// <summary>
        /// Removes the focus mark, and also activation mark if any
        /// </summary>
        public void RemoveFocus()
        {
            _isFocused = false;

            if (_isActivated)
            {
                _isActivated = false;
                _attentionCounter = 0;

                _logger.Add(LogSource.Game, "cell", "off", $"{Y},{X}");
                ActivationChanged.Invoke(this, State.Inactive);
            }
        }

        /// <summary>
        /// Sets the activation mark
        /// </summary>
        public void Shoot()
        {
            _attentionCounter = _options.DwellTime + _options.ShotDuration;
            _isActivated = true;

            _logger.Add(LogSource.Game, "cell", "shot-on", $"{Y},{X}");
            ActivationChanged.Invoke(this, State.Active);
        }

        /// <summary>
        /// Resets the state without firing <see cref="ActivationChanged" event/>
        /// </summary>
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
        readonly GameOptions _options = GameOptions.Instance;

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