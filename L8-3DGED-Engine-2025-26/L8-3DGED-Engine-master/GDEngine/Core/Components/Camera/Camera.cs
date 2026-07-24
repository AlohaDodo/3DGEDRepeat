using GDEngine.Core.Rendering.Base;
using GDEngine.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Components
{
    /// <summary>
    /// Camera component with switchable Perspective/Orthographic projection, LayerMask culling,
    /// and per-camera clear/stack settings. Uses lazy view/projection recomputation via dirty flags.
    /// </summary>
    /// <see cref="Transform"/>
    /// <see cref="Component"/>
    public sealed class Camera : Component
    {
        #region Enums
        /// <summary>
        /// Camera projection modes.
        /// </summary>
        public enum ProjectionType : sbyte
        {
            Perspective = 0,
            Orthographic = 1
        }

        /// <summary>
        /// Camera clear policies.
        /// </summary>
        public enum ClearFlagsType : sbyte
        {
            Skybox = 0,   // reserved; currently same as Color unless a skybox pass is added
            Color = 1,
            DepthOnly = 2,
            None = 3
        }

        /// <summary>
        /// Camera stack role used for sorting and composition.
        /// </summary>
        public enum StackType : sbyte
        {
            Base = 0,
            Overlay = 1
        } 
        #endregion

        #region Static Fields
        #endregion

        #region Fields
        private float _fieldOfView = MathHelper.PiOver4;
        private float _aspectRatio = 16f / 9f;
        private float _nearPlane = 0.1f;
        private float _farPlane = 1000f;

        // Orthographic controls
        private float _orthographicSize = 10f; // half-height in world units

        // Projection mode
        private ProjectionType _projectionType = ProjectionType.Perspective;

        // LayerMask culling
        private LayerMask _cullingMask = LayerMask.All;

        // Camera stack / clearing
        private ClearFlagsType _clearFlags = ClearFlagsType.Color;
        private Color _clearColor = Color.CornflowerBlue;
        private StackType _stackRole = StackType.Base;
        private int _depth;

        // PiP / per-camera viewport in pixels (null => full backbuffer)
        private Viewport? _viewport;

        private Matrix _view;
        private Matrix _projection;
        private Matrix _viewProjection;

        private bool _viewDirty = true;
        private bool _projectionDirty = true;
        private CameraSystem? _cameraSystem;
        #endregion

        #region Properties
        /// <summary>
        /// View matrix (recalculated on demand).
        /// </summary>
        public Matrix View
        {
            get
            {
                if (_viewDirty)
                {
                    RecalculateView();
                }
                return _view;
            }
        }

        /// <summary>
        /// Projection matrix (recalculated on demand).
        /// </summary>
        public Matrix Projection
        {
            get
            {
                if (_projectionDirty)
                {
                    RecalculateProjection();
                }
                return _projection;
            }
        }

        /// <summary>
        /// ViewProjection matrix (recalculated on demand).
        /// </summary>
        public Matrix ViewProjection
        {
            get
            {
                if (_viewDirty || _projectionDirty)
                {
                    RecalculateViewProjection();
                }
                return _viewProjection;
            }
        }

        /// <summary>
        /// Perspective field of view in radians.
        /// </summary>
        public float FieldOfView
        {
            get => _fieldOfView;
            set
            {
                if (_fieldOfView != value)
                {
                    _fieldOfView = value;
                    _projectionDirty = true;
                }
            }
        }

        /// <summary>
        /// Aspect ratio (width / height) used when no PixelViewport is set.
        /// </summary>
        public float AspectRatio
        {
            set
            {
                if (_aspectRatio != value)
                {
                    _aspectRatio = value;
                    _projectionDirty = true;
                }
            }
        }

        /// <summary>
        /// Near clip plane distance.
        /// </summary>
        public float NearPlane
        {
            get => _nearPlane;
            set
            {
                if (_nearPlane != value)
                {
                    _nearPlane = value;
                    _projectionDirty = true;
                }
            }
        }

        /// <summary>
        /// Far clip plane distance.
        /// </summary>
        public float FarPlane
        {
            get => _farPlane;
            set
            {
                if (_farPlane != value)
                {
                    _farPlane = value;
                    _projectionDirty = true;
                }
            }
        }

        /// <summary>
        /// Orthographic half-height (world units). Width is derived from aspect ratio.
        /// </summary>
        public float OrthographicSize
        {
            get => _orthographicSize;
            set
            {
                if (_orthographicSize != value)
                {
                    _orthographicSize = value;
                    _projectionDirty = true;
                }
            }
        }

        /// <summary>
        /// Current projection mode.
        /// </summary>
        public ProjectionType ProjectionMode
        {
            get => _projectionType;
            set
            {
                if (_projectionType != value)
                {
                    _projectionType = value;
                    _projectionDirty = true;
                }
            }
        }

        /// <summary>
        /// Per-camera layer mask for culling.
        /// </summary>
        public LayerMask CullingMask
        {
            get => _cullingMask;
            set => _cullingMask = value;
        }

        /// <summary>
        /// Camera clear policy.
        /// </summary>
        public ClearFlagsType ClearFlags
        {
            get => _clearFlags;
            set => _clearFlags = value;
        }

        /// <summary>
        /// Color used when clearing with <see cref="ClearFlagsType.Color"/>.
        /// </summary>
        public Color ClearColor
        {
            get => _clearColor;
            set => _clearColor = value;
        }

        /// <summary>
        /// Stack role (Base or Overlay).
        /// </summary>
        public StackType StackRole
        {
            get => _stackRole;
            set => _stackRole = value;
        }

        /// <summary>
        /// Depth sort key within the same stack role (lower draws first). Use a higher value for PiP overlays.
        /// </summary>
        public int Depth
        {
            get => _depth;
            set => _depth = value;
        }

        /// <summary>
        /// Optional pixel-space viewport for this camera (null = full backbuffer).
        /// When set, projection aspect is derived from this rectangle.
        /// </summary>
        public Viewport? Viewport
        {
            set
            {
                _viewport = value;
                _projectionDirty = true;
            }
        }
        #endregion

        #region Constructors
        #endregion

        #region Methods
        /// <summary>
        /// Toggle between Perspective and Orthographic projection.
        /// </summary>
        public void ToggleProjection()
        {
            if (_projectionType == ProjectionType.Perspective)
            {
                ProjectionMode = ProjectionType.Orthographic;
            }
            else
            {
                ProjectionMode = ProjectionType.Perspective;
            }
        }

        /// <summary>
        /// Returns the effective graphics Viewport for this camera.
        /// If <see cref="Viewport"/> is set, uses that rectangle; otherwise
        /// returns a full-backbuffer viewport based on the given graphics device.
        /// </summary>

        /// <summary>
        /// Returns the effective graphics <see cref="Viewport"/> for this camera.
        /// If <see cref="Viewport"/> is set, uses that rectangle; otherwise returns
        /// a full-backbuffer viewport based on the given graphics device.
        /// </summary>
        public Viewport GetViewport(GraphicsDevice graphicsDevice)
        {
            if (_viewport.HasValue)
            {
                var r = _viewport.Value;
                return new Viewport(r.X, r.Y, r.Width, r.Height);
            }

            var pp = graphicsDevice.PresentationParameters;
            return new Viewport(0, 0, pp.BackBufferWidth, pp.BackBufferHeight);
        }

        /// <summary>
        /// Returns the effective aspect ratio (width / height) for this camera.
        /// Uses the per-camera <see cref="Viewport"/> when set; otherwise falls
        /// back to the configured <see cref="AspectRatio"/>.
        /// </summary>
        public float GetAspectRatio()
        {
            return CalculateAspectRatio();
        }

        /// <summary>
        /// Computes the effective aspect ratio (width / height) for this camera,
        /// taking into account a per-camera <see cref="Viewport"/> if one is set.
        /// </summary>
        private float CalculateAspectRatio()
        {
            float aspect = _aspectRatio;

            if (_viewport.HasValue)
            {
                var r = _viewport.Value;
                int h = r.Height <= 0 ? 1 : r.Height;
                aspect = (float)r.Width / h;
            }

            return aspect;
        }

        private void OnTransformChanged(Transform transform, Transform.ChangeFlags flags)
        {
            _viewDirty = true;
        }

        private void RecalculateView()
        {
            if (Transform == null)
                throw new NullReferenceException(nameof(Transform));

            Vector3 position = Transform.Position;
            Vector3 forward = Transform.Forward;
            Vector3 up = Transform.Up;

            _view = Matrix.CreateLookAt(position, position + forward, up);
            _viewDirty = false;
        }

        private void RecalculateProjection()
        {
            float aspect = CalculateAspectRatio();

            if (_projectionType == ProjectionType.Perspective)
            {
                _projection = Matrix.CreatePerspectiveFieldOfView(
                    _fieldOfView,
                    aspect,
                    _nearPlane,
                    _farPlane
                );
            }
            else
            {
                float height = 2f * _orthographicSize;
                float width = height * aspect;

                _projection = Matrix.CreateOrthographic(
                    width,
                    height,
                    _nearPlane,
                    _farPlane
                );
            }

            _projectionDirty = false;
        }


        private void RecalculateViewProjection()
        {
            if (_viewDirty)
            {
                RecalculateView();
            }
            if (_projectionDirty)
            {
                RecalculateProjection();
            }

            _viewProjection = _view * _projection;
        }
        #endregion

        #region Lifecycle Methods
        protected override void Awake()
        {
            var scene = GameObject?.Scene;
            if (scene == null)
                throw new NullReferenceException("Camera requires a GameObject in a Scene.");

            _cameraSystem = scene.GetSystem<CameraSystem>();
            if (_cameraSystem == null)
                throw new InvalidOperationException("OverlayRenderSystem not found. Add it to the Scene before using OverlayRenderer.");

            _cameraSystem.Add(this);

            _viewDirty = true;
            _projectionDirty = true;

            if (Transform != null)
                Transform.Changed += OnTransformChanged;

        }
        #endregion

        #region Housekeeping Methods
        protected override void OnDestroy()
        {
            _cameraSystem?.Remove(this);
            _cameraSystem = null;
        }
        #endregion
    }
}
