using GDEngine.Core.Enums;

namespace GDEngine.Core.Systems
{
    /// <summary>
    /// Base class for systems that respect a global pause state using <see cref="PauseMode"/>.
    /// Wraps the normal <see cref="SystemBase.Update(float)"/> and <see cref="SystemBase.Draw(float)"/>
    /// calls and delegates work to OnUpdate/OnDraw when not paused.
    /// </summary>
    /// <see cref="SystemBase"/>
    /// <see cref="PauseMode"/>
    public abstract class PausableSystemBase : SystemBase
    {
        #region Fields

        private bool _isPaused;
        private PauseMode _pauseMode;

        #endregion

        #region Properties

        /// <summary>
        /// Controls how this system behaves when the game is paused.
        /// Defaults to pausing Update but not Draw.
        /// </summary>
        public PauseMode PauseMode
        {
            get { return _pauseMode; }
            set { _pauseMode = value; }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new pausable system in the given lifecycle and order.
        /// </summary>
        /// <param name="lifecycle">Lifecycle where this system executes.</param>
        /// <param name="order">Relative order within that lifecycle (lower runs first).</param>
        protected PausableSystemBase(FrameLifecycle lifecycle, int order = 0)
            : base(lifecycle, order)
        {
            _pauseMode = PauseMode.Update;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sets the current paused state for this system.
        /// Typically called from a GamePauseChangedEvent listener.
        /// </summary>
        /// <param name="paused">True to mark this system as paused.</param>
        public void SetPaused(bool paused)
        {
            _isPaused = paused;
        }

        /// <summary>
        /// Wraps the base Update and respects <see cref="PauseMode"/>.
        /// Delegates actual work to <see cref="OnUpdate(float)"/>.
        /// </summary>
        public override void Update(float deltaTime)
        {
            // Scene already checks Enabled before calling Update,
            // but if that ever changes this remains safe.
            if (_isPaused && (PauseMode & PauseMode.Update) != 0)
                return;

            OnUpdate(deltaTime);
        }

        /// <summary>
        /// Wraps the base Draw and respects <see cref="PauseMode"/>.
        /// Delegates actual work to <see cref="OnDraw(float)"/>.
        /// </summary>
        public override void Draw(float deltaTime)
        {
            if (_isPaused && (PauseMode & PauseMode.Draw) != 0)
                return;

            OnDraw(deltaTime);
        }

        /// <summary>
        /// Override to implement per-frame non-render work for this system.
        /// Called from <see cref="Update(float)"/> when not paused for Update.
        /// </summary>
        protected virtual void OnUpdate(float deltaTime) { }

        /// <summary>
        /// Override to implement per-frame render work for this system.
        /// Called from <see cref="Draw(float)"/> when not paused for Draw.
        /// </summary>
        protected virtual void OnDraw(float deltaTime) { }

        #endregion
    }
}
