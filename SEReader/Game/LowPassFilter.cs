using SEReader.Comm;
using SEReader.Logging;
using SEReader.Utils;
using System;

namespace SEReader.Game
{
    //[AllowScreenLog(ScreenLogger.Target.LowPassFilter)]
    internal class LowPassFilter
    {
        public LowPassFilter(double threshold)
        {
            _threshold = threshold;

            _screenLogger = ScreenLogger.Create();
        }

        public Point2D Feed(Point2D newPoint)
        {
            if (!pointExists || !_options.LowPassFilterEnabled)
            {
                pointExists = true;

                _filteredPoint.X = newPoint.X;
                _filteredPoint.Y = newPoint.Y;

                _screenLogger?.Log($"{newPoint.X:F0} {newPoint.Y:F0}");
            }
            else
            {
                double gain = _options.LowPassFilterGain;

                // We favor new points close to the current gaze locaiton.
                // The more distant the new point is, the smaller weight it gets
                double dist = _filteredPoint.Distance(newPoint);
                double nextWeight = 1.0 / (1.0 + Math.Exp(gain * (_threshold - dist)));

                // We favor new points far from the previous raw point to handle ???
                //double rtDist = _realTimePoint.Distance(newPoint);
                //double nextWeight2 = 1.0 - 1.0 / (1.0 + Math.Exp(gain * (_threshold - rtDist)));
                //double nextWeight = nextWeight1 * nextWeight2;

                double prevWeight = 1.0 - nextWeight;

                double x = _filteredPoint.X;
                double y = _filteredPoint.Y;

                _filteredPoint.X = _filteredPoint.X * prevWeight + newPoint.X * nextWeight;
                _filteredPoint.Y = _filteredPoint.Y * prevWeight + newPoint.Y * nextWeight;

                //_screenLogger?.Log($"{x:F0} {y:F0} | {newPoint.X:F0} {newPoint.Y:F0} {dist:F0} | {nextWeight1:F3} * {nextWeight2:F3} = {nextWeight:F3} | {_filteredPoint.X:F0} {_filteredPoint.Y:F0}");
                _screenLogger?.Log($"{x:F0} {y:F0} | {newPoint.X:F0} {newPoint.Y:F0} {dist:F0} {nextWeight:F3} | {_filteredPoint.X:F0} {_filteredPoint.Y:F0}");
            }

            _realTimePoint.X = newPoint.X;
            _realTimePoint.Y = newPoint.Y;

            return _filteredPoint;
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
                    _screenLogger?.Log("Reset");
                }
            }
        }

        // Internal

        readonly double _threshold;
        readonly Point2D _filteredPoint = new Point2D(); // buffer
        readonly Point2D _realTimePoint = new Point2D(); // buffer
        readonly GameOptions _options = GameOptions.Instance;
        readonly ScreenLogger _screenLogger;

        bool pointExists = false;
        long _exitTimestamp = 0;
    }
}
