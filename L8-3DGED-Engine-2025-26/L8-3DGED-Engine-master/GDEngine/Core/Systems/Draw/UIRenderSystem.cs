using GDEngine.Core.Components;
using GDEngine.Core.Entities;
using GDEngine.Core.Enums;
using GDEngine.Core.Rendering;
using GDEngine.Core.Services;
using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Systems
{
    /// <summary>
    /// PostRender dispatcher that asks overlay components to draw after the 3D pass.
    /// Assumes RenderingSystem restored the full backbuffer viewport.
    /// 
    /// Maintains separate active/inactive lists to eliminate enabled checks in draw loop.
    /// Inspired by the GameObject partitioning pattern - O(k) draw instead of O(n) with checks.
    /// </summary>
    /// <see cref="Scene"/>
    /// <see cref="UIRenderer"/>
    /// <see cref="RenderSystem"/>
    public sealed class UIRenderSystem : SystemBase
    {
        #region Static Fields
        private static readonly SpriteSortMode _sort = SpriteSortMode.BackToFront;
        private static readonly RasterizerState _raster = RasterizerState.CullCounterClockwise;
        private static readonly DepthStencilState _depth = DepthStencilState.None;
        private static readonly BlendState _blend = BlendState.AlphaBlend;
        private static readonly SamplerState _sampler = SamplerState.PointClamp;
        #endregion

        #region Fields
        private Scene _scene = null!;
        private EngineContext _context = null!;
        private GraphicsDevice _device = null!;
        private SpriteBatch _spriteBatch;

        // Maintain separate lists for active and all renderers
        private readonly List<UIRenderer> _allRenderers = new List<UIRenderer>(16);
        private readonly List<UIRenderer> _activeRenderers = new List<UIRenderer>(16);
        private bool _needsRebuildActiveList = false;

        #endregion

        #region Constructors
        public UIRenderSystem(int order = 10)
            : base(FrameLifecycle.PostRender, order)
        {

        }
        #endregion

        #region Methods
        /// <summary>
        /// Registers a UIRenderer with this system. Subscribes to enabled changes for list management.
        /// </summary>
        public void Add(UIRenderer renderer)
        {
            if (renderer == null)
                throw new ArgumentNullException(nameof(renderer));
            if (_allRenderers.Contains(renderer))
                return;

            _allRenderers.Add(renderer);

            // Add to active list if currently enabled
            if (renderer.Enabled)
                _activeRenderers.Add(renderer);

            // Subscribe to enabled changes to keep active list in sync
            renderer.EnabledChanged += OnRendererEnabledChanged;
        }

        /// <summary>
        /// Unregisters a UIRenderer from this system.
        /// </summary>
        public void Remove(UIRenderer renderer)
        {
            if (renderer == null)
                return;

            _allRenderers.Remove(renderer);
            _activeRenderers.Remove(renderer);
            renderer.EnabledChanged -= OnRendererEnabledChanged;
        }

        /// <summary>
        /// Handles enabled state changes on renderers. Marks active list for rebuild.
        /// OPTIMIZATION: Deferred rebuild instead of immediate list manipulation.
        /// </summary>
        private void OnRendererEnabledChanged(Component component, bool enabled)
        {
            _needsRebuildActiveList = true;
        }

        /// <summary>
        /// Rebuilds the active renderers list based on current enabled states.
        /// Called lazily before draw if needed.
        /// </summary>
        private void RebuildActiveList()
        {
            _activeRenderers.Clear();

            for (int i = 0; i < _allRenderers.Count; i++)
            {
                if (_allRenderers[i].Enabled)
                    _activeRenderers.Add(_allRenderers[i]);
            }

            _needsRebuildActiveList = false;
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
            _spriteBatch = _context.SpriteBatch;
        }

        /// <summary>
        /// Draws all active UI renderers without per-element enabled checks.
        /// OPTIMIZATION: Only iterates active renderers (O(k) instead of O(n) with checks).
        /// </summary>
        public override void Draw(float deltaTime)
        {
            // Rebuild active list if any renderer changed enabled state
            if (_needsRebuildActiveList)
                RebuildActiveList();

            _spriteBatch.Begin(_sort, _blend, _sampler, _depth, _raster);

            // Draw only active renderers - no enabled check needed!
            for (int i = 0; i < _activeRenderers.Count; i++)
                _activeRenderers[i].Draw(_device, Scene?.ActiveCamera);

            _spriteBatch.End();
        }
        #endregion
    }
}