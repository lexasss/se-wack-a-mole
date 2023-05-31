namespace WackAMole.Plane
{
    /// <summary>
    /// Base for objects representing SmartEye plane/screen
    /// </summary>
    public abstract class Plane
    {
        public enum Event
        {
            Enter,
            Exit
        }

        public bool IsEnabled { get; set; } = true;

        public string PlaneName => _planeName;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="planeName">SmartEye plane or screen name</param>
        public Plane(string planeName)
        {
            _planeName = planeName;
        }

        /// <summary>
        /// Notifies about gaze-on and gaze-off events
        /// </summary>
        /// <param name="evt">Gaze event</param>
        public void Notify(Event evt)
        {
            if (!IsEnabled) return;

            _logger.Add(Logging.LogSource.Tracker, evt.ToString(), PlaneName);

            HandleEvent(evt);
        }

        /// <summary>
        /// Consumes the gaze point that fell on the plane
        /// </summary>
        /// <param name="intersection"></param>
        public void Feed(SEClient.Intersection intersection)
        {
            if (!IsEnabled) return;

            HandleIntersection(intersection);
        }

        // Internal

        readonly string _planeName;
        readonly Logging.FlowLogger _logger = Logging.FlowLogger.Instance;

        protected virtual void HandleIntersection(SEClient.Intersection intersection) { }
        protected virtual void HandleEvent(Event evt) { }
    }
}
