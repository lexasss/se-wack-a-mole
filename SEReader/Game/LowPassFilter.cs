using SEReader.Comm;
using SEReader.Logging;
using SEReader.Utils;
using System;

namespace SEReader.Game
{
    internal class LowPassFilter
    {
        public LowPassFilter(double threshold)
        {
            _threshold = threshold;
            _gain = _options.LowPassFilterGain;

            //_logger = ScreenLogger.Instance?.WithTarget(ScreenLogger.Target.LowPassFilter);
        }

        public Point2D Feed(Point2D point)
        {
            if (!pointExists || !_options.LowPassFilterEnabled)
            {
                pointExists = true;

                _point.X = point.X;
                _point.Y = point.Y;

                _logger?.Log(ScreenLogger.Target.LowPassFilter, $"{point.X:F0} {point.Y:F0}");
            }
            else
            {
                double dist = _point.Distance(point);
                double rtDist = _realTimePoint.Distance(point);

                double nextWeight1 = 1.0 / (1.0 + Math.Exp(_gain * (_threshold - dist)));
                double nextWeight2 = 1.0 - 1.0 / (1.0 + Math.Exp(_gain * (_threshold - rtDist)));
                double nextWeight = nextWeight1 * nextWeight2;
                double prevWeight = 1.0 - nextWeight;

                double x = _point.X;
                double y = _point.Y;

                _point.X = _point.X * prevWeight + point.X * nextWeight;
                _point.Y = _point.Y * prevWeight + point.Y * nextWeight;

                _logger?.Log(ScreenLogger.Target.LowPassFilter, $"{x:F0} {y:F0} | {point.X:F0} {point.Y:F0} {dist:F0} | {nextWeight1:F3} * {nextWeight2:F3} = {nextWeight:F3} | {_point.X:F0} {_point.Y:F0}");
            }

            _realTimePoint.X = point.X;
            _realTimePoint.Y = point.Y;

            return _point;
        }

        public void Inform(Experiment.Observer.Event evt)
        {
            if (evt == Experiment.Observer.Event.PlaneExit)
            {
                _exitTimestamp = Timestamp.Ms;
            }
            else if (evt == Experiment.Observer.Event.PlaneEnter)
            {
                if ((Timestamp.Ms - _exitTimestamp) > _options.LowPassFilterResetDelay)
                {
                    pointExists = false;
                    _logger?.Log(ScreenLogger.Target.LowPassFilter, "LP Reset");
                }
            }
        }

        // Internal

        readonly double _threshold;
        readonly double _gain;
        readonly Point2D _point = new Point2D(); // buffer
        readonly Point2D _realTimePoint = new Point2D(); // buffer
        readonly GameOptions _options = GameOptions.Instance;
        readonly ScreenLogger _logger;

        bool pointExists = false;
        long _exitTimestamp = 0;
    }
}
