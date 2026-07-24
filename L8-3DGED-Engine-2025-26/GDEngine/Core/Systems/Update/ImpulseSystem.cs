using GDEngine.Core.Enums;
using GDEngine.Core.Impulses;

namespace GDEngine.Core.Systems
{
    /// <summary>
    /// Main-thread pump for <see cref="ImpulseBus"/>.
    /// Add this system to the Scene so queued impulses and continuous sources
    /// are processed once per frame (typically in LateUpdate).
    /// </summary>
    /// <see cref="ImpulseBus"/>
    public sealed class ImpulseSystem : SystemBase
    {
        #region Static Fields
        #endregion

        #region Fields
        private readonly ImpulseBus _bus;
        #endregion

        #region Properties
        #endregion

        #region Constructors
        /// <summary>
        /// Create a system for the impulse bus that runs in LateUpdate and dispatches impulses,
        /// then advances any continuous impulse sources.
        /// </summary>
        public ImpulseSystem(ImpulseBus bus, int order = -1000)
            : base(FrameLifecycle.LateUpdate, order)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }
        #endregion

        #region Methods
        #endregion

        #region Lifecycle Methods
        public override void Update(float deltaTime)
        {
            // Flush queued impulses generated since last frame.
            _bus.DispatchAll();

            // Advance continuous sources and publish any impulses they generate.
            _bus.UpdateContinuousSources(deltaTime);
        }
        #endregion

        #region Housekeeping Methods
        #endregion
    }
}
