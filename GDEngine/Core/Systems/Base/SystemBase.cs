using GDEngine.Core.Entities;
using GDEngine.Core.Enums;
using GDEngine.Core.Services;

namespace GDEngine.Core.Systems
{
    /// <summary>
    /// Base class for frame-scheduled systems that operate over a <see cref="Scene"/>.
    /// Provides lifecycle placement via FrameLifecycle and an execution order within that lifecycle.
    /// </summary>
    /// <see cref="Scene"/>
    /// <see cref="FrameLifecycle"/>
    public abstract class SystemBase
    {

        #region Fields
        // Owning scene (assigned when added to a Scene)
        private Scene? _scene;

        // Lifecycle placement and relative order within that lifecycle
        private readonly FrameLifecycle _lifecycle;
        private readonly int _order;

        // Enable/disable switch
        private bool _enabled = true;
        #endregion

        #region Properties
        /// <summary>Scene that owns this system, or null if not yet added.</summary>
        public Scene? Scene => _scene;

        /// <summary>Engine lifecycle bucket for this system (e.g., Logic, Render).</summary>
        public FrameLifecycle Lifecycle => _lifecycle;

        /// <summary>Relative order within the lifecycle; lower runs first.</summary>
        public int Order => _order;

        /// <summary>When false, the scene will skip Update/Draw calls for this system.</summary>
        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        /// <summary>Convenience accessor to the engine services via the owning scene.</summary>
        public EngineContext? Context => _scene?.Context;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new <see cref="SystemBase"/> in a given <see cref="FrameLifecycle"/> with an optional order.
        /// </summary>
        /// <param name="lifecycle">Lifecycle where this system executes.</param>
        /// <param name="order">Relative order within the lifecycle (lower runs first).</param>
        protected SystemBase(FrameLifecycle lifecycle, int order = 0)
        {
            _lifecycle = lifecycle;
            _order = order;
        }
        #endregion

        #region Core Methods
        /// <summary>
        /// Called by <see cref="Scene"/> when this system is added to it.
        /// </summary>
        /// <param name="scene">The scene that now owns this system.</param>
        internal void OnAddedToScene(Scene scene)
        {
            if (scene == null)
                throw new ArgumentNullException(nameof(scene));

            if (_scene != null && _scene != scene)
                throw new InvalidOperationException("System is already attached to a different Scene.");

            _scene = scene;
            OnAdded();
        }

        /// <summary>
        /// Called by <see cref="Scene"/> when this system is about to be removed.
        /// </summary>
        internal void OnRemovedFromScene(Scene scene)
        {
            if (_scene == null)
                return;

            OnRemoved();
            _scene = null;
        }

        /// <summary>
        /// Per-frame update entry invoked by <see cref="Scene.Update(float)"/>.
        /// Override to implement non-render work.
        /// </summary>
        /// <param name="deltaTime">Seconds since last frame.</param>
        public virtual void Update(float deltaTime) { }

        /// <summary>
        /// Per-frame draw entry invoked by <see cref="Scene.Draw()"/>.
        /// Override to implement render work for Render/PostRender lifecycles.
        /// </summary>
        public virtual void Draw(float deltaTime) { }

        /// <summary>
        /// Hook called after the system is attached to a scene.
        /// </summary>
        protected virtual void OnAdded() { }

        /// <summary>
        /// Hook called before the system is detached from a scene.
        /// </summary>
        protected virtual void OnRemoved() { }
        #endregion

        #region Housekeeping Methods
        /// <summary>
        /// Returns a readable identifier for diagnostics.
        /// </summary>
        public override string ToString()
        {
            var sceneName = _scene?.Name ?? "<none>";
            return $"{GetType().Name}(Lifecycle={_lifecycle}, Order={_order}, Enabled={_enabled}, Scene={sceneName})";
        }
        #endregion
    }
}
