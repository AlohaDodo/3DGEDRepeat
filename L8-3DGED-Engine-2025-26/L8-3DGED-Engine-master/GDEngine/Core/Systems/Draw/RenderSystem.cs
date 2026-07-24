#nullable enable
using GDEngine.Core.Components;
using GDEngine.Core.Entities;
using GDEngine.Core.Enums;
using GDEngine.Core.Rendering;
using GDEngine.Core.Services;
using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Systems
{
    /// <summary>
    /// Renders the scene for all cameras each frame.
    /// - In FullStack layout: iterates cameras in stack order (Base -> Overlay, then by Depth).
    /// - In SingleActive layout: renders only <see cref="CameraSystem.ActiveCamera"/>.
    /// - For each camera: sets the device viewport from PixelViewport, applies camera clear, and renders visible renderers.
    /// - Restores the full backbuffer viewport at the end so UI/post systems can assume full-screen.
    /// </summary>
    public class RenderSystem : SystemBase
    {
        #region Enums
        /// <summary>
        /// Controls whether this system renders all cameras in the stack or just the active camera.
        /// </summary>
        public enum RenderLayout : sbyte
        {
            /// <summary>
            /// Render only the <see cref="CameraSystem.ActiveCamera"/> if present.
            /// </summary>
            SingleActive = 0,

            /// <summary>
            /// Render the full sorted camera stack (Base + Overlay).
            /// </summary>
            FullStack = 1
        }
        #endregion

        #region Fields
        private Scene _scene = null!;
        private EngineContext _context = null!;
        private GraphicsDevice _device = null!;
        private CameraSystem? _cameraSystem = null!;
        private readonly List<Camera> _cameraStack = new List<Camera>(8);
        private readonly List<MeshRenderer> _visible = new List<MeshRenderer>(512);

        private RenderLayout _layout = RenderLayout.SingleActive;
        #endregion

        #region Properties
        /// <summary>
        /// Controls how this system chooses which cameras to render.
        /// </summary>
        public RenderLayout Layout
        {
            get => _layout;
            set => _layout = value;
        }
        #endregion

        #region Constructors
        public RenderSystem(int order = -100)
            : base(FrameLifecycle.Render, order)
        {
        }
        #endregion

        #region Lifecycle Methods
        protected override void OnAdded()
        {
            if (Scene == null)
                throw new NullReferenceException(nameof(Scene));

            _scene = Scene;
            _context = _scene.Context;
            _device = _context.GraphicsDevice;
            _cameraSystem = _scene.GetSystem<CameraSystem>();
        }

        public override void Draw(float deltaTime)
        {
            // No renderables? early out
            var renderers = _scene.Renderers;
            if (renderers == null || renderers.Count == 0)
                return;

            if (_cameraSystem == null)
                throw new ArgumentNullException(nameof(_cameraSystem));

            _cameraStack.Clear();

            if (_layout == RenderLayout.FullStack)
            {
                // Existing behaviour: render all cameras in stack order
                _cameraSystem.GetSortedStack(_cameraStack);
            }
            else
            {
                // SingleActive layout: render only the active camera if present
                var active = _cameraSystem.ActiveCamera;
                if (active != null)
                    _cameraStack.Add(active);
            }

            if (_cameraStack.Count == 0)
                return;

            // Keep a copy of the full backbuffer viewport so we can restore it later
            var fullViewport = _device.Viewport;

            // For each camera: set viewport, clear, filter by mask, then draw
            for (int i = 0; i < _cameraStack.Count; i++)
            {
                var camera = _cameraStack[i];

                // Apply this camera's viewport
                _device.Viewport = camera.GetViewport(_device);

                // Clear per camera (for overlays, ClearFlags is typically None)
                _cameraSystem.ApplyClears(camera);

                // Build visible set (mask + later bounds test)
                _visible.Clear();
                _cameraSystem.BuildVisibleSet(camera, renderers, _visible);

                // Render each visible renderer with this camera
                for (int j = 0; j < _visible.Count; j++)
                {
                    _visible[j].Draw(_device, camera);
                }
            }

            // Restore full-screen viewport so UI/Post systems behave as expected
            _device.Viewport = fullViewport;
        }
        #endregion
    }
}
