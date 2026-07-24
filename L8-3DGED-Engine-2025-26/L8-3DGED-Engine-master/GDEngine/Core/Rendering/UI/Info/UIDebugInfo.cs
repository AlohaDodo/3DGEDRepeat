#nullable enable
using GDEngine.Core.Components;
using GDEngine.Core.Screen;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Rendering.UI
{
    /// <summary>
    /// Simple contract for systems/components that can expose debug text lines
    /// for on-screen visualization via <see cref="UIDebugInfo"/>.
    /// </summary>
    public interface IShowDebugInfo
    {
        /// <summary>
        /// Return one or more lines of debug information to be shown on screen.
        /// </summary>
        IEnumerable<string> GetDebugLines();
    }

    /// <summary>
    /// Generic debug text overlay that draws lines from one or more
    /// <see cref="IShowDebugInfo"/> providers using the UI rendering pipeline.
    /// Attach this to a <see cref="GameObject"/> in the Scene.
    /// </summary>
    /// <see cref="UIRenderer"/>
    /// <see cref="IShowDebugInfo"/>
    public sealed class UIDebugInfo : UIRenderer
    {
        #region Fields
        private SpriteFont _font = null!;
        private Texture2D? _backgroundTexture;

        // Layout
        private Vector2 _anchor = new Vector2(5f, 5f);
        private Vector2 _texturePadding = new Vector2(5f, 5f);
        private Rectangle _backRect;

        // Style
        private Color _shadow = Color.Black;
        private Color _text = Color.Yellow;
        private Color _bgColor = new Color(40, 40, 40, 125);

        // Providers and lines
        private readonly List<IShowDebugInfo> _providers = new List<IShowDebugInfo>(8);
        private readonly List<string> _lines = new List<string>(32);

        // Corner anchoring (re-uses ScreenCorner enum from UIStatsRenderer)
        private ScreenCorner _screenCorner = ScreenCorner.BottomLeft;
        private Vector2 _margin = new Vector2(10f, 10f);
        #endregion

        #region Properties
        /// <summary>
        /// Font used to render debug lines.
        /// </summary>
        public SpriteFont Font
        {
            get { return _font; }
            set { _font = value; }
        }

        /// <summary>
        /// Text color for debug lines.
        /// </summary>
        public Color TextColor
        {
            get { return _text; }
            set { _text = value; }
        }

        /// <summary>
        /// Shadow color for debug lines.
        /// </summary>
        public Color Shadow
        {
            get { return _shadow; }
            set { _shadow = value; }
        }

        /// <summary>
        /// Background panel color.
        /// </summary>
        public Color BackgroundColor
        {
            get { return _bgColor; }
            set { _bgColor = value; }
        }

        /// <summary>
        /// Padding, in pixels, between the text and the background edges.
        /// </summary>
        public Vector2 TexturePadding
        {
            get { return _texturePadding; }
            set { _texturePadding = value; }
        }

        /// <summary>
        /// Corner of the game window that this overlay should attach to.
        /// </summary>
        public ScreenCorner ScreenCorner
        {
            get { return _screenCorner; }
            set { _screenCorner = value; }
        }

        /// <summary>
        /// Margin in pixels from the chosen screen corner to the outer edge of the panel.
        /// </summary>
        public Vector2 Margin
        {
            get { return _margin; }
            set { _margin = value; }
        }

        /// <summary>
        /// Collection of debug info providers that this overlay will query each frame.
        /// Add systems/components that implement <see cref="IShowDebugInfo"/>.
        /// </summary>
        public IList<IShowDebugInfo> Providers
        {
            get { return _providers; }
        }
        #endregion

        #region Lifecycle Methods
        protected override void Awake()
        {
            base.Awake();

            if (_graphicsDevice != null && _backgroundTexture == null)
            {
                _backgroundTexture = new Texture2D(_graphicsDevice, 1, 1, false, SurfaceFormat.Color);
                _backgroundTexture.SetData(new[] { Color.White });
            }
        }

        protected override void LateUpdate(float deltaTime)
        {
            _lines.Clear();

            if (_providers.Count == 0 || _font == null)
            {
                _backRect = Rectangle.Empty;
                return;
            }

            // Pull lines from all providers
            for (int i = 0; i < _providers.Count; i++)
            {
                IShowDebugInfo provider = _providers[i];
                IEnumerable<string> src = provider.GetDebugLines();
                if (src == null)
                    continue;

                foreach (string line in src)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        _lines.Add(line);
                }
            }

            if (_lines.Count == 0)
            {
                _backRect = Rectangle.Empty;
                return;
            }

            // Measure panel size
            float maxWidth = 0f;
            for (int i = 0; i < _lines.Count; i++)
            {
                float width = _font.MeasureString(_lines[i]).X;
                if (width > maxWidth)
                    maxWidth = width;
            }

            int linesCount = _lines.Count;
            float totalWidth = _texturePadding.X * 2f + maxWidth;
            float totalHeight = _texturePadding.Y * 2f + _font.LineSpacing * linesCount;

            Vector2 panelTopLeft = ComputePanelTopLeft(totalWidth, totalHeight);

            _anchor = panelTopLeft + _texturePadding;

            _backRect = new Rectangle(
                (int)MathF.Floor(panelTopLeft.X),
                (int)MathF.Floor(panelTopLeft.Y),
                (int)MathF.Ceiling(totalWidth),
                (int)MathF.Ceiling(totalHeight));
        }

        public override void Draw(GraphicsDevice device, Camera? camera)
        {
            if (_spriteBatch == null || _font == null)
                return;

            if (_backRect.Width <= 0 || _backRect.Height <= 0)
                return;

            float backgroundDepth = Behind(LayerDepth);

            // Background panel
            if (_backgroundTexture != null)
            {
                _spriteBatch.Draw(
                    _backgroundTexture,
                    _backRect,
                    null,
                    _bgColor,
                    0f,
                    Vector2.Zero,
                    SpriteEffects.None,
                    backgroundDepth);
            }

            // Lines
            float y = _anchor.Y;
            for (int i = 0; i < _lines.Count; i++)
            {
                Vector2 pos = new Vector2(_anchor.X, y);

                DrawStringWithShadow(
                    _font,
                    _lines[i],
                    pos,
                    _text,
                    RotationRadians,
                    Vector2.Zero,
                    1f,
                    Effects,
                    LayerDepth,
                    true);

                y += _font.LineSpacing;
            }
        }

        protected override void OnDestroy()
        {
            if (_backgroundTexture != null)
            {
                _backgroundTexture.Dispose();
                _backgroundTexture = null;
            }
        }
        #endregion

        #region Methods
        private Vector2 ComputePanelTopLeft(float panelWidth, float panelHeight)
        {
            if (_graphicsDevice == null)
                return _margin;

            Viewport vp = _graphicsDevice.Viewport;

            switch (_screenCorner)
            {
                case ScreenCorner.TopLeft:
                    return new Vector2(
                        _margin.X,
                        _margin.Y);

                case ScreenCorner.TopRight:
                    return new Vector2(
                        vp.Width - panelWidth - _margin.X,
                        _margin.Y);

                case ScreenCorner.BottomLeft:
                    return new Vector2(
                        _margin.X,
                        vp.Height - panelHeight - _margin.Y);

                default: // BottomRight
                    return new Vector2(
                        vp.Width - panelWidth - _margin.X,
                        vp.Height - panelHeight - _margin.Y);
            }
        }
        #endregion
    }
}
