using SEReader.Comm;
using SEReader.Logging;
using SEReader.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SEReader.Game
{
    [AllowScreenLog(ScreenLogger.Target.GazeController)]
    public class GazeController : Experiment.Observer
    {
        public GazeController(Game game, string screenName) : base(screenName)
        {
            _screenWidth = _options.ScreenWidth;
            _screenHeight = _options.ScreenHeight;
            _game = game;

            _screenLogger = ScreenLogger.Create();

            _lowPassFilter = new LowPassFilter(_options.ScreenWidth / _options.CellX * 0.7);

            _options.Changed += Options_Changed;
            Options_Changed(null, Options.Option.General);
        }

        // Internal

        readonly double _screenWidth;
        readonly double _screenHeight;
        readonly Game _game;
        readonly Options _options = Options.Instance;
        readonly LowPassFilter _lowPassFilter;
        readonly ScreenLogger _screenLogger;

        CancellationTokenSource _cancelWaiting = null;

        double _currentCellSizeFromItsCenter;

        int _lastCellX = -1;
        int _lastCellY = -1;

        protected override void HandleIntersection(Intersection intersection)
        {
            Point2D point = _lowPassFilter.Feed(intersection.Point);

            double gridX = (point.X / _screenWidth * _options.CellX);
            double gridY = (point.Y / _screenHeight * _options.CellY);

            int cellX = (int)gridX;
            int cellY = (int)gridY;

            double dx = Math.Abs(gridX - (_lastCellX + 0.5));
            double dy = Math.Abs(gridY - (_lastCellY + 0.5));
            if (dx < _currentCellSizeFromItsCenter && dy < _currentCellSizeFromItsCenter)
            {
                cellX = _lastCellX;
                cellY = _lastCellY;
            }

            _lastCellX = cellX;
            _lastCellY = cellY;

            _game.Focus(cellX, cellY);
        }

        protected override void HandleEvent(Event evt)
        {
            _cancelWaiting?.Cancel();
            _cancelWaiting = null;

            if (evt == Event.PlaneExit)
            {
                _cancelWaiting = new CancellationTokenSource();
                _screenLogger.Log($"{Timestamp.Ms} plane exit");
                Task.Delay(_options.FocusLatency, _cancelWaiting.Token).ContinueWith((arg) =>
                {
                    if (!arg.IsCanceled)
                    {
                        _screenLogger.Log($"{Timestamp.Ms} focus-off");
                        _cancelWaiting = null;
                        _game.ClearFocus();
                    }
                });
            }
            else
            {
                _screenLogger.Log($"{Timestamp.Ms} plane enter");
            }

            _lowPassFilter.Inform(evt);
        }

        private void Options_Changed(object _, Options.Option e)
        {
            if (e == Options.Option.Controller)
            {
                _currentCellSizeFromItsCenter = _options.FocusedCellExpansion + 0.5;
            }
        }
    }
}
