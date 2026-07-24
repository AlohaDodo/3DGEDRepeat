#nullable enable
using GDEngine.Core.Components;
using GDEngine.Core.Rendering.Base;
using GDEngine.Core.Rendering.UI;
using GDEngine.Core.Systems;
using GDEngine.Core.Utilities;

namespace GDEngine.Core.Rendering
{
    /// <summary>
    /// UI helper component that performs a physics raycast through the reticle
    /// (screen center) and pushes info about the hit collider into a
    /// <see cref="UIText"/> attached to the same GameObject.
    /// </summary>
    /// <see cref="UIReticle"/>
    /// <see cref="UIText"/>
    public class UIPickerInfo : Component
    {
        #region Fields
        private PhysicsSystem _physicsSystem = null!;
        private UIText _textRenderer = null!;
        private Camera _fallbackCamera = null!;

        private LayerMask _hitMask = LayerMask.All;
        private float _maxDistance = 1000f;
        private bool _hitTriggers = false;

        private string _currentText = string.Empty;

        // Optional custom formatter: if set, overrides default text format.
        private Func<RaycastHit, string>? _formatter;
        #endregion

        #region Properties

        /// <summary>
        /// Layers that are considered pickable by this UI picker.
        /// Defaults to <see cref="LayerMask.All"/>.
        /// </summary>
        public LayerMask HitMask
        {
            get => _hitMask;
            set => _hitMask = value;
        }

        /// <summary>
        /// Maximum raycast distance in world units.
        /// </summary>
        public float MaxDistance
        {
            get => _maxDistance;
            set => _maxDistance = MathF.Max(0f, value);
        }

        /// <summary>
        /// If true, trigger colliders are considered pickable.
        /// </summary>
        public bool HitTriggers
        {
            get => _hitTriggers;
            set => _hitTriggers = value;
        }

        /// <summary>
        /// Optional formatter that converts a <see cref="RaycastHit"/> to text.
        /// If null, a simple "Name (distance)" string is used.
        /// </summary>
        public Func<RaycastHit, string>? Formatter
        {
            get => _formatter;
            set => _formatter = value;
        }

        #endregion

        #region Lifecycle Methods

        protected override void Start()
        {
            if (GameObject == null)
                throw new NullReferenceException(nameof(GameObject));

            var scene = GameObject.Scene
                        ?? throw new NullReferenceException(nameof(GameObject.Scene));

            _physicsSystem = scene.GetSystem<PhysicsSystem>()
                            ?? throw new InvalidOperationException(
                                "UIPickerInfoRenderer requires a PhysicsSystem in the Scene.");

            _fallbackCamera = scene.ActiveCamera
                               ?? throw new InvalidOperationException(
                                   "UIPickerInfoRenderer requires an active Camera in the Scene.");

            _textRenderer = GameObject.GetComponent<UIText>()
                           ?? throw new InvalidOperationException(
                               "UIPickerInfoRenderer requires a UITextRenderer on the same GameObject.");

            // Always bind our provider – this GameObject's UITextRenderer is dedicated
            // to picker info.
            _textRenderer.TextProvider = () => _currentText;
        }

        protected override void Update(float deltaTime)
        {
            if (GameObject == null)
                return;

            var scene = GameObject.Scene;
            if (scene == null)
                return;

            // Re-acquire active camera in case the scene switched cameras.
            var camera = scene.ActiveCamera ?? _fallbackCamera;
            if (camera == null)
                return;

            var device = scene.Context.GraphicsDevice;
            var viewport = camera.GetViewport(device);

            // Reticle is at screen center (same as UIReticleRenderer).
            var center = viewport.GetCenter();

            RaycastHit hit;
            if (_physicsSystem.RaycastFromScreen(
                    camera,
                    center.X,
                    center.Y,
                    _maxDistance,
                    _hitMask,
                    out hit,
                    _hitTriggers))
            {
                _currentText = FormatHit(hit);
            }
            else
            {
                _currentText = string.Empty;
            }
        }

        #endregion

        #region Methods

        private string FormatHit(RaycastHit hit)
        {
            // Custom formatter wins
            if (_formatter != null)
                return _formatter(hit);

            var rb = hit.Body;
            var go = rb?.GameObject;
            if (go == null)
                return string.Empty;

            // Simple default: "Name (12.3u)"
            return $"{go.Name} ({hit.Distance:F1}u)";
        }

        #endregion
    }
}
