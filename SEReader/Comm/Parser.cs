using SEReader.Game;
using SEReader.Logging;
using System;
using System.Collections.Generic;

namespace SEReader.Comm
{
    [AllowScreenLog(ScreenLogger.Target.ParserEvent)]
    public class Parser
    {
        public event EventHandler<Sample> Sample;
        public event EventHandler<Intersection> PlaneEnter;
        public event EventHandler<string> PlaneExit;

        public Parser()
        {
            _screenLogger = ScreenLogger.Create();
            _intersectionSource = NUMERICAL_CLOSEST_WORLD_INTERSECTION;
        }

        public void Reset()
        {
            _state = State.Initial;
            _intersectionDataIndex = -1;
            _activeIntersections.Clear();
            _foundIntersections.Clear();
            _frame.Intersections.Clear();

            _intersectionSource = GameOptions.Instance.IntersectionSource switch
            {
                IntersectionSource.Calibrated => NUMERICAL_CLOSEST_WORLD_INTERSECTION,
                IntersectionSource.Predicted => ESTIMATED_CLOSEST_WORLD_INTERSECTION,
                _ => throw new Exception($"This intersection source is not implemented")
            };
        }

        public void Feed(string line)
        {
            if (line == null || line.Length == 0)
            {
                FinilizeFrame();
                return;
            }

            switch (_state)
            {
                case State.Initial:
                    if (line.StartsWith(FRAME_NUMBER))
                    {
                        _frame.ID = int.Parse(line[FRAME_NUMBER.Length..]);
                    }
                    else if (line.StartsWith(TIME_STAMP))
                    {
                        _frame.TimeStamp = long.Parse(line[TIME_STAMP.Length..]);
                    }
                    else if (line.StartsWith(_intersectionSource))
                    {
                        _state = State.Intersections;
                    }
                    break;

                case State.Intersections:
                    if (line.StartsWith(INTERSECTION))
                    {
                        _state = State.Intersection;
                        _intersection.ID = int.Parse(line[INTERSECTION.Length..]);
                        _intersectionDataIndex = 0;
                    }
                    else
                    {
                        _state = State.Initial;
                        Feed(line);
                    }
                    break;

                case State.Intersection:
                    if (line.StartsWith(PAD))
                    {
                        string data = line[PAD.Length..];
                        switch (_intersectionDataIndex++)
                        {
                            case 0:
                                _intersection.Gaze = Point3D.Parse(data);
                                break;
                            case 1:
                                _intersection.Point = Point2D.Parse(data);
                                break;
                            case 2:
                                _intersection.PlaneName = data;
                                break;
                            default:
                                throw new Exception($"Unexpected data in the definition of intersection: '${data}'");
                        }

                        if (_intersectionDataIndex == 3)
                        {
                            CreateIntersection();
                            _state = State.Intersections;  // more intersections may come
                        }
                    }
                    else
                    {
                        _state = State.Initial;
                        Feed(line);
                    }
                    break;
            }
        }

        // Internal

        enum State
        {
            Initial,
            Intersections,
            Intersection
        }

        readonly string FRAME_NUMBER = "FrameNumber";
        readonly string TIME_STAMP = "TimeStamp";
        readonly string NUMERICAL_CLOSEST_WORLD_INTERSECTION = "NumericalClosestWorldIntersection";
        readonly string ESTIMATED_CLOSEST_WORLD_INTERSECTION = "EstimatedClosestWorldIntersection";
        readonly string INTERSECTION = "Intersection";
        readonly string PAD = "\t";

        readonly HashSet<string> _activeIntersections = new();
        readonly HashSet<string> _foundIntersections = new();
        readonly ScreenLogger _screenLogger;
        

        string _intersectionSource;

        State _state = State.Initial;

        int _intersectionDataIndex = -1;
        Intersection _intersection = new();

        Sample _frame = new() { Intersections = new List<Intersection>() }; // used as a buffer

        private void CreateIntersection()
        {
            _foundIntersections.Add(_intersection.PlaneName);
            _frame.Intersections.Add(_intersection);

            if (!_activeIntersections.Contains(_intersection.PlaneName))
            {
                _screenLogger?.Log($"Entered {_intersection.PlaneName}");
                PlaneEnter?.Invoke(this, _intersection);
            }
        }

        private void FinilizeFrame()
        {
            if (_frame.ID != 0)
            {
                _activeIntersections.ExceptWith(_foundIntersections);
                foreach (var name in _activeIntersections)
                {
                    _screenLogger?.Log($"Exited {name}");
                    PlaneExit?.Invoke(this, name);
                }

                _activeIntersections.Clear();
                _activeIntersections.UnionWith(_foundIntersections);
                _foundIntersections.Clear();

                Sample?.Invoke(this, _frame);
            }

            _frame.ID = 0;
            _frame.Intersections.Clear();
        }
    }
}
