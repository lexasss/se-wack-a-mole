using System.Collections.Generic;
using System.Timers;

namespace SEReader
{
    internal interface ITickUpdatable
    {
        void Tick(int interval);
    }

    internal static class TickTimer
    {
        public static readonly int TICK_INTERVAL = 30;

        public static void Add(ITickUpdatable updatable)
        {
            _updatables.Add(updatable);
        }

        public static void Remove(ITickUpdatable updatable)
        {
            _updatables.Remove(updatable);
        }

        // Internal

        static readonly Timer _timer = new Timer(TICK_INTERVAL);
        static readonly List<ITickUpdatable> _updatables = new List<ITickUpdatable>();

        static TickTimer()
        {
            _timer.AutoReset = true;
            _timer.Elapsed += (s, e) =>
            {
                foreach (var updatable in _updatables)
                {
                    updatable.Tick(TICK_INTERVAL);
                }
            };
            _timer.Start();
        }
    }
}
