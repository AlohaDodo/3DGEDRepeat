using GDEngine.Core.Enums;
using GDEngine.Core.Events;

namespace GDEngine.Core.Systems
{
    /// <summary>
    /// Main-thread pump for EventBus. Add this system to the Scene
    /// so queued events are flushed once per frame in EarlyUpdate.
    /// </summary>
    /// <see cref="EventBus"/>
    public sealed class EventSystem : SystemBase
    {
        #region Static Fields
        #endregion

        #region Fields
        private readonly EventBus _bus;
        #endregion

        #region Properties
        #endregion

        #region Constructors
        /// <summary>
        /// Create a system for the event bus that runs in EarlyUpdate and dispatches events
        /// </summary>
        public EventSystem(EventBus bus, int order = -1000)
            : base(FrameLifecycle.EarlyUpdate, order)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }
        #endregion

        #region Methods
        #endregion

        #region Lifecycle Methods
        public override void Update(float deltaTime)
        {
            // Dispatch all that were posted since last frame.
            _bus.DispatchAll();
        }
        #endregion

        #region Housekeeping Methods
        #endregion
    }
}
