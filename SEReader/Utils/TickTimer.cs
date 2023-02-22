using System.Collections.Generic;
using System.Timers;

namespace SEReader.Utils
{
    internal interface ITickUpdatable
    {
        void Tick(int interval);
    }

    /// <summary>
    /// The timer that updates UI widgets on a regular basis
    /// </summary>
    internal static class TickTimer
    {
        public static readonly int TICK_INTERVAL = 30;

        /// <summary>
        /// Adds a widget to be regularly updated
        /// </summary>
        /// <param name="updatable">a widget</param>
        public static void Add(ITickUpdatable updatable)
        {
            _updatables.Add(updatable);
        }

        /// <summary>
        /// Removes a widget from the list of those that should be regularly updated
        /// </summary>
        /// <param name="updatable">a widget</param>
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
