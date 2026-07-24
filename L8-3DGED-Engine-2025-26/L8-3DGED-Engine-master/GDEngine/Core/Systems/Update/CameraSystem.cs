using GDEngine.Core.Components;
using GDEngine.Core.Enums;
using GDEngine.Core.Rendering;
using GDEngine.Core.Rendering.Base;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Systems
{
    /// <summary>
    /// Manages runtime cameras: registration, aspect/resize sync, clear flags, sorting, and helpers.
    /// Runs in Render lifecycle before the <see cref="RenderSystem"/>.
    /// </summary>
    public sealed class CameraSystem : SystemBase
    {
        #region Fields
        private readonly List<Camera> _cameras = new List<Camera>();
        private readonly Dictionary<Camera, BoundingFrustum> _frusta = new Dictionary<Camera, BoundingFrustum>();

        private Camera? _activeCamera;
        private GraphicsDevice _graphicsDevice = null!;
        private int _backbufferWidth;
        private int _backbufferHeight;

        private readonly List<Camera> _sorted = new List<Camera>();
        #endregion

        #region Properties
        public Camera? ActiveCamera
        {
            get => _activeCamera;
            set
            {
                if (_activeCamera == value)
                    return;

                _activeCamera = value;
                EnsureFrustum(_activeCamera);
            }
        }

        public IReadOnlyList<Camera> Cameras => _cameras;
        #endregion

        #region Constructors
        public CameraSystem(GraphicsDevice graphicsDevice, int order = -100)
            : base(FrameLifecycle.Render, order)
        {
            _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
            _backbufferWidth = graphicsDevice.PresentationParameters.BackBufferWidth;
            _backbufferHeight = graphicsDevice.PresentationParameters.BackBufferHeight;
        }
        #endregion

        #region Methods
 
        public void Add(Camera camera)
        {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));

            if (_cameras.Contains(camera))
                return;

            _cameras.Add(camera);
            EnsureFrustum(camera);
            SyncAspect(camera);

            if (_activeCamera == null)
                _activeCamera = camera;
        }

        public void Remove(Camera camera)
        {
            if (camera == null)
                return;

            _cameras.Remove(camera);
            _frusta.Remove(camera);

            if (_activeCamera == camera)
            {
                if (_cameras.Count > 0)
                    _activeCamera = _cameras[0];
                else
                    _activeCamera = null;
            }
        }

        /// <summary>
        /// Fills <paramref name="destinationList"/> with cameras sorted by stack role, then by Depth.
        /// Base cameras first (ascending Depth), then Overlay cameras (ascending Depth).
        /// </summary>
        public void GetSortedStack(List<Camera> destinationList)
        {
            if (destinationList == null)
                throw new ArgumentNullException(nameof(destinationList));

            destinationList.Clear();
            destinationList.AddRange(_cameras);

            destinationList.Sort((a, b) =>
            {
                int roleCompare = a.StackRole.CompareTo(b.StackRole);
                if (roleCompare != 0)
                    return roleCompare;

                return a.Depth.CompareTo(b.Depth);
            });
        }

        /// <summary>
        /// Applies the clear operation for a given camera. For PiP overlays, expect ClearFlags == None.
        /// </summary>
        public void ApplyClears(Camera camera)
        {
            if (camera == null)
                return;

            switch (camera.ClearFlags)
            {
                case Camera.ClearFlagsType.Color:
                case Camera.ClearFlagsType.Skybox:
                    _graphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, camera.ClearColor, 1f, 0);
                    break;

                case Camera.ClearFlagsType.DepthOnly:
                    _graphicsDevice.Clear(ClearOptions.DepthBuffer, Color.Transparent, 1f, 0);
                    break;

                case Camera.ClearFlagsType.None:
                    break;
            }
        }

        /// <summary>
        /// Builds a visible set using layer-mask filtering and enabled flags.
        /// Bounding tests can be added when bounds are exposed.
        /// </summary>
        public void BuildVisibleSet(Camera camera, IEnumerable<MeshRenderer> allRenderers, List<MeshRenderer> destinationList)
        {
            if (destinationList == null)
                throw new ArgumentNullException(nameof(destinationList));

            destinationList.Clear();

            if (camera == null)
                return;

            LayerMask cameraMask = camera.CullingMask;
            BoundingFrustum frustum = GetFrustum(camera);

            foreach (var meshRenderer in allRenderers)
            {
                if (meshRenderer == null)
                    continue;

                // Respect component/GameObject enable flags
                if (!meshRenderer.Enabled)
                    continue;

                var owner = meshRenderer.GameObject;
                if (owner == null || !owner.Enabled)
                    continue;

                LayerMask objectLayer = owner.Layer;

                // Only include renderers whose layer overlaps this camera's mask
                if (!cameraMask.Overlaps(objectLayer))
                    continue;

                // TODO: add frustum test once you expose bounds
                // if (!Intersects(frustum, meshRenderer.Bounds)) continue;

                destinationList.Add(meshRenderer);
            }
        }

        /// <summary>
        /// Converts screen space (pixels) to world using this camera's effective viewport (PiP-aware).
        /// </summary>
        public Vector3 ScreenToWorld(Camera camera, Vector3 screenPoint)
        {
            var viewport = camera.GetViewport(_graphicsDevice);
            return viewport.Unproject(screenPoint, camera.Projection, camera.View, Matrix.Identity);
        }

        /// <summary>
        /// Converts world to screen space (pixels) using this camera's effective viewport (PiP-aware).
        /// </summary>
        public Vector3 WorldToScreen(Camera camera, Vector3 worldPoint)
        {
            var viewport = camera.GetViewport(_graphicsDevice);
            return viewport.Project(worldPoint, camera.Projection, camera.View, Matrix.Identity);
        }

        /// <summary>
        /// Returns a picking ray from this camera using its effective viewport (PiP-aware).
        /// </summary>
        public Ray ScreenPointToRay(Camera camera, Vector2 screenPixel)
        {
            var viewport = camera.GetViewport(_graphicsDevice);

            var nearPoint = new Vector3(screenPixel, 0f);
            var farPoint = new Vector3(screenPixel, 1f);

            var nearWorld = viewport.Unproject(nearPoint, camera.Projection, camera.View, Matrix.Identity);
            var farWorld = viewport.Unproject(farPoint, camera.Projection, camera.View, Matrix.Identity);

            var direction = Vector3.Normalize(farWorld - nearWorld);
            return new Ray(nearWorld, direction);
        }

        public override void Draw(float deltaTime)
        {
            // Handle resize which requires an update to aspect sync
            var presentation = _graphicsDevice.PresentationParameters;
            if (presentation.BackBufferWidth != _backbufferWidth || presentation.BackBufferHeight != _backbufferHeight)
            {
                _backbufferWidth = presentation.BackBufferWidth;
                _backbufferHeight = presentation.BackBufferHeight;

                // Re-sync aspect ratio for all cameras that follow backbuffer
                for (int i = 0; i < _cameras.Count; i++)
                    SyncAspect(_cameras[i]);
            }

            // Refresh frusta from latest camera matrices (after components’ LateUpdate)
            for (int i = 0; i < _cameras.Count; i++)
                UpdateFrustumFromCamera(_cameras[i]);
        }
        #endregion

        #region Housekeeping Methods
        private void SyncAspect(Camera camera)
        {
            if (camera == null)
                return;

            camera.AspectRatio = camera.GetAspectRatio();
        }

        private void EnsureFrustum(Camera? camera)
        {
            if (camera == null)
                return;

            if (!_frusta.ContainsKey(camera))
                _frusta[camera] = new BoundingFrustum(camera.ViewProjection);
            else
                _frusta[camera].Matrix = camera.ViewProjection;
        }

        private void UpdateFrustumFromCamera(Camera camera)
        {
            BoundingFrustum frustum = GetFrustum(camera);
            frustum.Matrix = camera.ViewProjection;
        }

        private BoundingFrustum GetFrustum(Camera camera)
        {
            EnsureFrustum(camera);
            return _frusta[camera];
        }

        private bool Intersects(BoundingFrustum frustum, BoundingBox bounds)
        {
            return frustum.Contains(bounds) != ContainmentType.Disjoint;
        }
        #endregion
    }
}
