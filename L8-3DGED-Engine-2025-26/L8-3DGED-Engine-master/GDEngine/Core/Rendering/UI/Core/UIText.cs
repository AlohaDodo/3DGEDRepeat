using GDEngine.Core.Components;
using GDEngine.Core.Rendering.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GDEngine.Core.Rendering
{
    /// <summary>
    /// Generic UI text renderer that draws a string at a position supplied by a delegate.
    /// Uses centralized batching via <see cref="UIRenderer"/>.
    /// Optimized to cache non-null delegates and avoid hot-path null checks.
    /// </summary>
    public class UIText : UIRenderer
    {
        #region Fields
        private SpriteFont _font = null!;

        // Delegate storage (nullable for API)
        private Func<string>? _textProvider;
        private Func<Vector2>? _positionProvider;
        private Func<Color>? _colorProvider;

        // Non-null cached delegates (for performance in hot path)
        private Func<string> _textProviderNonNull = () => string.Empty;
        private Func<Vector2> _positionProviderNonNull = () => Vector2.Zero;

        private Vector2 _offset = Vector2.Zero;
        private float _uniformScale = 1f;
        private bool _dropShadow = true;
        private Color _fallbackColor = Color.White;

        // Cached computation results
        private string _text = string.Empty;
        private Vector2 _size;
        private Vector2 _originFromAnchor;
        private Vector2 _drawPos;
        private Color _resolvedColor;
        #endregion

        #region Properties
        public SpriteFont Font { get => _font; set => _font = value; }

        public Func<string>? TextProvider
        {
            get => _textProvider;
            set
            {
                _textProvider = value;
                _textProviderNonNull = value ?? (() => string.Empty);
            }
        }

        public Func<Vector2>? PositionProvider
        {
            get => _positionProvider;
            set
            {
                _positionProvider = value;
                _positionProviderNonNull = value ?? (() => Vector2.Zero);
            }
        }

        public Func<Color>? ColorProvider { get => _colorProvider; set => _colorProvider = value; }

        public Vector2 Offset { get => _offset; set => _offset = value; }

        /// <summary>
        /// Uniform scale factor for the text. Uses base class _scale field internally.
        /// </summary>
        public float UniformScale
        {
            get => _uniformScale;
            set
            {
                _uniformScale = Math.Max(0.01f, value);
                _scale = new Vector2(_uniformScale, _uniformScale);
            }
        }

        public bool DropShadow { get => _dropShadow; set => _dropShadow = value; }

        public Color FallbackColor { get => _fallbackColor; set => _fallbackColor = value; }
        public string Text { get; set; }
        public Vector2 Position { get; set; }
        #endregion

        #region Constructors
        public UIText()
        {

        }
        public UIText(SpriteFont font)
        {
            _font = font;
        }

        public UIText(SpriteFont font, string text, Vector2 position)
        {
            _font = font;
            _textProvider = () => text ?? string.Empty;
            _textProviderNonNull = _textProvider;
            _positionProvider = () => position;
            _positionProviderNonNull = _positionProvider;
        }

        public static UIText FromMouse(SpriteFont font, string text)
        {
            var renderer = new UIText(font);
            renderer._textProvider = () => text ?? string.Empty;
            renderer._textProviderNonNull = renderer._textProvider;
            renderer._positionProvider = () => Mouse.GetState().Position.ToVector2();
            renderer._positionProviderNonNull = renderer._positionProvider;
            return renderer;
        }
        #endregion

        #region Lifecycle Methods
        protected override void LateUpdate(float deltaTime)
        {
            // Use non-null cached delegates (no null-coalescing in hot path)
            _text = _textProviderNonNull();
            if (_text.Length == 0)
                return;

            var basePos = _positionProviderNonNull();
            _size = _font.MeasureString(_text) * _uniformScale;

            // Let the base class handle anchor logic
            ApplyAnchor(basePos + _offset, _size, out _drawPos, out _originFromAnchor);

            _resolvedColor = _colorProvider != null
                ? _colorProvider()
                : _fallbackColor;
        }

        public override void Draw(GraphicsDevice device, Camera? camera)
        {
            if (_spriteBatch == null || _font == null || _text.Length == 0) return;

            // Use base class helper method for consistent shadow rendering
            DrawStringWithShadow(
                _font,
                _text,
                _drawPos,
                _resolvedColor,
                RotationRadians,
                _originFromAnchor,
                _uniformScale,
                Effects,
                LayerDepth,
                _dropShadow);
        }
        #endregion
    }
}