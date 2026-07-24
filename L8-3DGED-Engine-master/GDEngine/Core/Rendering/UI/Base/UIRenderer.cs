using GDEngine.Core.Components;
using GDEngine.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Rendering
{
    /// <summary>
    /// Named UI layers for SpriteBatch BackToFront sorting (0 = front, 1 = back).
    /// Use directly as a float thanks to implicit conversion: ui.LayerDepth = UILayer.Menu;
    /// </summary>
    public readonly struct UILayer
    {
        #region Fields
        public readonly float Depth;
        #endregion

        #region Static Fields
        public static readonly UILayer Cursor = new UILayer(0f);          // on-top pointers/reticles
        public static readonly UILayer MenuFront = new UILayer(0.05f);    // highlights/selection states
        public static readonly UILayer Menu = new UILayer(0.1f);          // menu text/buttons
        public static readonly UILayer MenuBack = new UILayer(0.2f);          // menu text/buttons
        public static readonly UILayer HUD = new UILayer(0.4f);           // in-game HUD overlays
        public static readonly UILayer Background = new UILayer(1f);      // background images / backdrops
        #endregion

        #region Constructors
        public UILayer(float depth)
        {
            Depth = depth;
        }
        #endregion

        #region Operator Overloading
        public static implicit operator float(UILayer layer)
        {
            return layer.Depth;
        }
        #endregion

        #region Housekeeping Methods
        public override string ToString()
        {
            return Depth.ToString("0.00");
        }
        #endregion
    }

    /// <summary>
    /// Base component for on-screen overlays that draw in PostRender via <see cref="UIRenderSystem"/>.
    /// Centralizes common UI draw fields so subclasses only supply per-type data.
    /// Refactored to include common fields: scale, origin, sourceRect, colors, and graphicsDevice.
    /// </summary>
    /// <see cref="Component"/>
    /// <see cref="UIRenderSystem"/>
    public abstract class UIRenderer : Component
    {
        #region Static Fields
        protected static readonly Vector2 _shadowNudge = new Vector2(1f, 1f);
        public const float LAYER_DEPTH_EPSILON = 0.1f;
        #endregion

        #region Fields
        protected SpriteBatch? _spriteBatch;
        protected GraphicsDevice? _graphicsDevice;

        // Drawing properties
        private float _layerDepth = 0.9f;
        private float _rotationRadians = 0f;
        private SpriteEffects _effects = SpriteEffects.None;

        // Layout - common across multiple child classes
        protected Vector2 _scale = Vector2.One;
        protected Vector2 _origin = Vector2.Zero;
        protected Rectangle? _sourceRect;
        private TextAnchor _anchor = TextAnchor.TopLeft;

        // Colors - common tint and shadow
        protected Color _color = Color.White;
        protected Color _shadowColor = new Color(0, 0, 0, 180);
        #endregion

        #region Properties
        public float LayerDepth
        {
            get { return _layerDepth; }
            set { _layerDepth = MathHelper.Clamp(value, 0f, 1f); }
        }

        public float RotationRadians
        {
            get { return _rotationRadians; }
            set { _rotationRadians = value; }
        }

        public SpriteEffects Effects
        {
            get { return _effects; }
            set { _effects = value; }
        }

        /// <summary>
        /// Logical anchor to use when positioning this UI element relative to its
        /// base position or rectangle (TopLeft, Center, BottomRight, etc).
        /// </summary>
        public TextAnchor Anchor
        {
            get { return _anchor; }
            set { _anchor = value; }
        }

        /// <summary>
        /// Scale of the UI element as a 2D vector (allows non-uniform scaling).
        /// </summary>
        public Vector2 Scale
        {
            get { return _scale; }
            set { _scale = value; }
        }

        /// <summary>
        /// Origin point for rotation and positioning (in local texture coordinates).
        /// </summary>
        public Vector2 Origin
        {
            get { return _origin; }
            set { _origin = value; }
        }

        /// <summary>
        /// Optional source rectangle for texture-based rendering (null = full texture).
        /// </summary>
        public Rectangle? SourceRectangle
        {
            get { return _sourceRect; }
            set { _sourceRect = value; }
        }

        /// <summary>
        /// Primary color/tint for the UI element.
        /// </summary>
        public Color Color
        {
            get { return _color; }
            set { _color = value; }
        }

        /// <summary>
        /// Shadow color used when drawing drop shadows.
        /// </summary>
        public Color ShadowColor
        {
            get { return _shadowColor; }
            set { _shadowColor = value; }
        }
        #endregion

        #region Helper Methods

        public static float Behind(float layerDepth, float e = LAYER_DEPTH_EPSILON)
        {
            return Math.Clamp(layerDepth + e, 0, 1);
        }

        public static float Before(float layerDepth, float e = LAYER_DEPTH_EPSILON)
        {
            return Math.Clamp(layerDepth - e, 0, 1);
        }

        /// <summary>
        /// Returns an offset from the top-left of a region of the given size so that
        /// drawing with this offset as the origin will respect the chosen anchor.
        /// </summary>
        protected static Vector2 ComputeAnchorOffset(Vector2 size, TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.TopLeft: return Vector2.Zero;
                case TextAnchor.Top: return new Vector2(size.X * 0.5f, 0f);
                case TextAnchor.TopRight: return new Vector2(size.X, 0f);
                case TextAnchor.Left: return new Vector2(0f, size.Y * 0.5f);
                case TextAnchor.Center: return size * 0.5f;
                case TextAnchor.Right: return new Vector2(size.X, size.Y * 0.5f);
                case TextAnchor.BottomLeft: return new Vector2(0f, size.Y);
                case TextAnchor.Bottom: return new Vector2(size.X * 0.5f, size.Y);
                default: return new Vector2(size.X, size.Y); // BottomRight
            }
        }

        /// <summary>
        /// Convenience helper: given a base position and content size, returns the
        /// actual draw position and origin to use for DrawString/Draw.
        /// </summary>
        protected void ApplyAnchor(
            Vector2 basePosition,
            Vector2 contentSize,
            out Vector2 drawPosition,
            out Vector2 originFromAnchor)
        {
            originFromAnchor = ComputeAnchorOffset(contentSize, _anchor);
            drawPosition = basePosition;
        }

        /// <summary>
        /// Calculates and sets the origin to the center of a texture or source rectangle.
        /// Shared logic extracted from UITextureRenderer and UIReticleRenderer.
        /// </summary>
        protected void CenterOriginFromTexture(Texture2D? texture, Rectangle? sourceRect)
        {
            if (texture == null) return;

            if (sourceRect.HasValue)
            {
                var r = sourceRect.Value;
                _origin = new Vector2(r.Width * 0.5f, r.Height * 0.5f);
            }
            else
            {
                _origin = new Vector2(texture.Width * 0.5f, texture.Height * 0.5f);
            }
        }

        /// <summary>
        /// Helper to draw text with an optional drop shadow in a single call.
        /// Reduces code duplication across text-rendering UI elements.
        /// </summary>
        protected void DrawStringWithShadow(
            SpriteFont font,
            string text,
            Vector2 position,
            Color color,
            float rotation,
            Vector2 origin,
            float scale,
            SpriteEffects effects,
            float layerDepth,
            bool enableShadow = true)
        {
            if (_spriteBatch == null) return;

            if (enableShadow)
            {
                _spriteBatch.DrawString(font, text, position + _shadowNudge, _shadowColor,
                    rotation, origin, scale, effects, Behind(layerDepth));
            }

            _spriteBatch.DrawString(font, text, position, color,
                rotation, origin, scale, effects, layerDepth);
        }

        /// <summary>
        /// Helper to draw text with an optional drop shadow using Vector2 scale.
        /// </summary>
        protected void DrawStringWithShadow(
            SpriteFont font,
            string text,
            Vector2 position,
            Color color,
            float rotation,
            Vector2 origin,
            Vector2 scale,
            SpriteEffects effects,
            float layerDepth,
            bool enableShadow = true)
        {
            if (_spriteBatch == null) return;

            if (enableShadow)
            {
                _spriteBatch.DrawString(font, text, position + _shadowNudge, _shadowColor,
                    rotation, origin, scale, effects, Behind(layerDepth));
            }

            _spriteBatch.DrawString(font, text, position, color,
                rotation, origin, scale, effects, layerDepth);
        }
        #endregion

        #region Constructors
        protected UIRenderer()
        {
        }
        #endregion

        #region Lifecycle Methods
        protected override void Awake()
        {
            base.Awake();

            // Cache SpriteBatch and GraphicsDevice from the scene context
            _spriteBatch = GameObject?.Scene?.Context.SpriteBatch;
            _graphicsDevice = GameObject?.Scene?.Context.GraphicsDevice;

            // Auto-register with the scene's UIRenderSystem so it can be drawn
            var scene = GameObject?.Scene;
            if (scene == null)
                return;

            var uiSystem = scene.GetSystem<UIRenderSystem>();
            if (uiSystem != null)
                uiSystem.Add(this);
        }

        public abstract void Draw(GraphicsDevice device, Camera? camera);
        #endregion
    }

    public enum TextAnchor
    {
        TopLeft, Top, TopRight,
        Left, Center, Right,
        BottomLeft, Bottom, BottomRight
    }
}