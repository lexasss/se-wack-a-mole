using SEReader.Utils;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;

namespace SEReader.Plane
{
    internal class PlaneRenderer
    {
        public PlaneRenderer(params Panel[] panels)
        {
            foreach (var panel in panels)
            {
                if (panel.Children.Count == 2)
                {
                    Label counter = panel.Children[1] as Label;
                    if (counter != null)
                    {
                        _planes.Add(panel.Tag, new Plane(panel, counter));
                    }
                }
            }
        }

        public void Enter(string name)
        {
            if (!_planes.ContainsKey(name))
            {
                return;
            }

            _planes[name].Enter();
        }

        public void Exit(string name)
        {
            if (!_planes.ContainsKey(name))
            {
                return;
            }

            _planes[name].Exit();
        }

        public void Reset()
        {
            foreach (var plane in _planes.Values)
            {
                plane.Reset();
            }
        }

        // Internal

        class Plane
        {
            public Panel Panel { get; }
            public Label Counter { get; }
            public long Time { get; private set; } = 0;

            public Plane(Panel panel, Label counter)
            {
                Panel = panel;
                Counter = counter;
            }

            public void Enter()
            {
                Panel.Background = ACTIVE_BACKGROUND;
                _startMs = Timestamp.Ms;
            }

            public void Exit()
            {
                Panel.Background = NORMAL_BACKGROUND;
                if (_startMs > 0)
                {
                    Time += Timestamp.Ms - _startMs;
                    Counter.Content = Time;
                }
            }

            public void Reset()
            {
                Panel.Background = NORMAL_BACKGROUND;
                Counter.Content = 0;
                Time = 0;

                _startMs = 0;
            }

            // Internal

            readonly Brush NORMAL_BACKGROUND = Brushes.Bisque;
            readonly Brush ACTIVE_BACKGROUND = Brushes.Tomato;

            long _startMs = 0;
        }

        Dictionary<object, Plane> _planes = new();
    }
}
