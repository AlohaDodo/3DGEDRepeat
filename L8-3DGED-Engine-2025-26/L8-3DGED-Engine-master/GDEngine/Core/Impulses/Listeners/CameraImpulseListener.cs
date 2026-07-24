using Microsoft.Xna.Framework;
using GDEngine.Core.Components;

namespace GDEngine.Core.Impulses
{
    /// <summary>
    /// Applies eased displacement impulses from <see cref="Eased3DImpulse"/> to this
    /// GameObject's <see cref="Transform"/> to create camera shake.
    /// Attach to camera GameObject and call <see cref="Subscribe"/> once
    /// (e.g. after the <see cref="EngineContext"/> is initialised).
    /// </summary>
    /// <see cref="Eased3DImpulse"/>
    /// <see cref="ImpulseListenerBase{T}"/>
    /// <see cref="Transform"/>
    public sealed class CameraImpulseListener : ImpulseListenerBase<Eased3DImpulse>
    {
        #region Static Fields

        private const string _channel = "camera/impulse";

        #endregion

        #region Fields

        private Vector3 _accumulatedOffset;
        private Vector3 _lastAppliedOffset;

        #endregion

        #region Properties

        /// <summary>
        /// Channel name for camera impulses.
        /// </summary>
        protected override string? Channel
        {
            get { return _channel; }
        }

        /// <summary>
        /// The offset applied in the most recent frame (useful for debugging overlays).
        /// </summary>
        public Vector3 LastAppliedOffset
        {
            get { return _lastAppliedOffset; }
        }

        #endregion

        #region Constructors
        public CameraImpulseListener()
        {
            Subscribe();
        }
        #endregion

        #region Methods

        /// <summary>
        /// Subscribes this listener to the global <see cref="ImpulseBus"/> using the fluent API.
        /// </summary>
        public override void Subscribe()
        {
            Unsubscribe();

            ImpulseBus bus = Impulses;

            _subscription = bus
                .On<Eased3DImpulse>()
                .When(MatchesChannel)
                .WithPriority(ImpulsePriority.Systems)
                .Do(OnImpulse);
        }

        private bool MatchesChannel(Eased3DImpulse impulse)
        {
            return string.Compare(impulse.Channel, _channel, true) == 0;
        }

        /// <summary>
        /// Handles an incoming eased impulse by accumulating its offset for this frame.
        /// </summary>
        /// <param name="impulse">Impulse payload.</param>
        public override void OnImpulse(Eased3DImpulse impulse)
        {
            Vector3 offset = impulse.GetOffset();
            if (offset == Vector3.Zero)
                return;

            _accumulatedOffset += offset;
        }

        private void ApplyAccumulatedOffset()
        {
            if (Transform == null)
            {
                _lastAppliedOffset = Vector3.Zero;
                return;
            }

            // Remove last frame's offset so we never accumulate drift.
            if (_lastAppliedOffset != Vector3.Zero)
                Transform.TranslateBy(-_lastAppliedOffset, true);

            Vector3 newOffset = _accumulatedOffset;

            if (newOffset != Vector3.Zero)
            {
                Transform.TranslateBy(newOffset, true);
                _lastAppliedOffset = newOffset;
            }
            else
                _lastAppliedOffset = Vector3.Zero;
        }

        #endregion

        #region Lifecycle Methods

        protected override void LateUpdate(float deltaTime)
        {
            ApplyAccumulatedOffset();
            _accumulatedOffset = Vector3.Zero;
        }

        #endregion

        #region Housekeeping Methods
        #endregion
    }
}
