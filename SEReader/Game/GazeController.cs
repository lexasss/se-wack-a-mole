using SEReader.Comm;
using System;

namespace SEReader.Game
{
    public class GazeController : Experiment.Observer
    {
        public GazeController(Game game, string screenName) : base(screenName)
        {
            _screenWidth = _options.ScreenWidth;
            _screenHeight = _options.ScreenHeight;
            _game = game;

            _lowPassFilter = new LowPassFilter(_options.ScreenWidth / _options.CellX * 0.7);

            _options.Changed += Options_Changed;
            Options_Changed(null, GameOptions.Option.General);
        }

        // Internal

        readonly double _screenWidth;
        readonly double _screenHeight;
        readonly Game _game;
        readonly GameOptions _options = GameOptions.Instance;
        readonly LowPassFilter _lowPassFilter;
        
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
            if (evt == Event.PlaneExit)
            {
                _game.ClearFocus();
            }

            _lowPassFilter.Inform(evt);
        }

        private void Options_Changed(object sender, GameOptions.Option e)
        {
            _currentCellSizeFromItsCenter = _options.CurrentCellExpansion + 0.5;
        }
    }
}
