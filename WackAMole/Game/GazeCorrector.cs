#if USE_TCP
using SEClient.Tcp;
#else
using SEClient.Cmd;
#endif
using System.Collections.Generic;
using System.Linq;
using WackAMole.Logging;
using WackAMole.Utils;

namespace WackAMole.Game;

[AllowScreenLog]
internal class Reference
{
    public Reference(int i, int j, double x, double y)
    {
        _i = i;
        _j = j;
        _x = x;
        _y = y;
    }

    public bool IsAt(Mole mole) => mole.X == _i && mole.Y == _j;

    public void SetStartGazingTime()
    {
        _startGazingTimestamp = Timestamp.Ms + GazePointCollectionDelay;
    }

    public Point2D Feed(Point2D point)
    {
        if (Timestamp.Ms > _startGazingTimestamp)
        {
            if (_gazePoints.Count == GlanceBufferSize)
                _gazePoints.Dequeue();

            _gazePoints.Enqueue(point);
            //_screenLogger?.Log($"REF_{_i}_{_j}.GP+ {point.X:F3} {point.Y:F3}");
        }

        var correctionX = _corrections.Mean(corr => corr.X);
        var correctionY = _corrections.Mean(corr => corr.Y);
        //_screenLogger?.Log($"REF_{_i}_{_j}.CORR {correction.X:F3} {correction.Y:F3}");
        return new Point2D() { X = point.X - correctionX, Y = point.Y - correctionY };
    }

    public void CalculateCorrection()
    {
        if (_gazePoints.Count == GlanceBufferSize)
        {
            var medianX = _gazePoints.Median(pt => pt.X);
            var medianY = _gazePoints.Median(pt => pt.Y);

            if (_corrections.Count == CorrectionBufferSize)
                _corrections.Dequeue();

            var correction = new Point2D() { X = medianX - _x, Y = medianY - _y };
            _corrections.Enqueue(correction);

            var correctionX = _corrections.Mean(corr => corr.X);
            var correctionY = _corrections.Mean(corr => corr.Y);
            _screenLogger?.Log($"REF_{_i}_{_j} {correction.X:F3} {correction.Y:F3}");
        }

        _gazePoints.Clear();
    }

    // Internal

    static readonly int GlanceBufferSize = 12;
    static readonly int CorrectionBufferSize = 5;
    static readonly int GazePointCollectionDelay = 800;  // ms

    static readonly ScreenLogger? _screenLogger = ScreenLogger.Create(ScreenLogger.Target.Corrector);

    readonly int _i;
    readonly int _j;
    readonly double _x;
    readonly double _y;

    readonly Queue<Point2D> _gazePoints = new(GlanceBufferSize);
    readonly Queue<Point2D> _corrections = new(CorrectionBufferSize);

    long _startGazingTimestamp = 0;
}

internal class GazeCorrector
{
    public GazeCorrector()
    {
        List<Reference> refs = new();
        for (int j = 0; j < _options.CellY; ++j)
        {
            for (int i = 0; i < _options.CellX; ++i)
            {
                double x = (0.45 + i) / _options.CellX;     // No idea why the aiming center is in 0.45 and not in 0.5...
                double y = (0.45 + j) / _options.CellY;

                var reference = new Reference(i, j, x, y);
                refs.Add(reference);
            }
        }

        _references = refs.ToArray();
    }

    public Point2D Feed(Point2D point, double screenWidth, double screenHeight)
    {
        if (_options.UseSmartGazeCorrection && _activeReference != null)
        {
            point.X /= screenWidth;
            point.Y /= screenHeight;
            point = _activeReference.Feed(point);
            point.X *= screenWidth;
            point.Y *= screenHeight;
        }

        return point;
    }

    /// <summary>
    /// Sets/removes the active cell, i.e. the cell with a visual attractor
    /// </summary>
    public void MoleVisibilityChangeHandler(object? _, Mole mole)
    {
        if (mole.IsVisible)
        {
            _activeReference = _references.FirstOrDefault(r => r.IsAt(mole));
            _activeReference?.SetStartGazingTime();
        }
        else
        {
            _activeReference?.CalculateCorrection();
            _activeReference = null;
        }
    }

    // Internal

    readonly GameOptions _options = GameOptions.Instance;

    /// <summary>
    /// List of cell centers to be used as gaze references/attractors.
    /// Attractors allow to correct the gaze point when the initial tracking accuracy is not high
    /// </summary>
    readonly Reference[] _references;

    Reference? _activeReference = null;
}
