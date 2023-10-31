using System;
using System.Threading;
using System.Threading.Tasks;
#if USE_TCP
using SEClient.Tcp;
using Intersection = WackAMole.Plane.Intersection;
#else
using SEClient.Cmd;
#endif

namespace WackAMole.Game;

/// <summary>
/// Uses <see cref="Sample"/> and/or <see cref="Intersection"/> to play the game 
/// </summary>
internal class GazeController : Plane.Plane
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="game">Game instance</param>
    /// <param name="screenName">SmartEye screen name where the game is shown</param>
    public GazeController(Game game, string screenName, GazeCorrector gazeCorrector) : base(screenName)
    {
        _screenWidth = _options.ScreenWidth;
        _screenHeight = _options.ScreenHeight;
        _game = game;
        _gazeCorrector = gazeCorrector;

        _lowPassFilter = new LowPassFilter(_options.ScreenWidth / _options.CellX * 0.7);

        _options.Changed += Options_Changed;
        Options_Changed(null, GameOptions.Option.Controller);
    }

    // Internal

    readonly double _screenWidth;
    readonly double _screenHeight;
    readonly Game _game;
    readonly GameOptions _options = GameOptions.Instance;
    readonly LowPassFilter _lowPassFilter;
    readonly GazeCorrector _gazeCorrector;

    CancellationTokenSource? _cancelWaiting = null;

    double _currentCellSizeFromItsCenter;

    int _lastCellX = -1;
    int _lastCellY = -1;

    protected override void HandleIntersection(Intersection intersection)
    {
        Point2D point = _lowPassFilter.Feed(intersection.Point);

        point = _gazeCorrector.Feed(point, _screenWidth, _screenHeight);
        _game.SetGaze(point.X, point.Y);

        double gridX = point.X / _screenWidth * _options.CellX;
        double gridY = point.Y / _screenHeight * _options.CellY;

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

        if (evt == Event.Exit)
        {
            _cancelWaiting = new CancellationTokenSource();
            Task.Delay(_options.FocusLatency, _cancelWaiting.Token).ContinueWith((arg) =>
            {
                if (!arg.IsCanceled)
                {
                    _cancelWaiting = null;
                    _game.ClearFocus();
                }
            });
        }

        _lowPassFilter.Inform(evt);
    }

    private void Options_Changed(object? _, GameOptions.Option e)
    {
        if (e == GameOptions.Option.Controller)
        {
            _currentCellSizeFromItsCenter = _options.FocusedCellExpansion + 0.5;
        }
    }
}
