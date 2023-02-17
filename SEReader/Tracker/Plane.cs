using SEReader.Comm;

namespace SEReader.Tracker
{
    public abstract class Plane
    {
        public enum Event
        {
            Enter,
            Exit
        }

        public bool IsEnabled { get; set; } = true;

        public string PlaneName => _planeName;


        public Plane(string planeName)
        {
            _planeName = planeName;
        }

        public void Notify(Event evt)
        {
            if (!IsEnabled) return;

            _logger.Add(Logging.LogSource.Tracker, evt.ToString(), PlaneName);

            HandleEvent(evt);
        }

        public void Feed(Intersection intersection)
        {
            if (!IsEnabled) return;

            HandleIntersection(intersection);
        }

        // Internal

        readonly string _planeName;
        readonly Logging.FlowLogger _logger = Logging.FlowLogger.Instance;

        protected abstract void HandleIntersection(Intersection intersection);
        protected abstract void HandleEvent(Event evt);
    }
}
