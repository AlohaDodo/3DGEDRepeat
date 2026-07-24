using GDEngine.Core.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Rendering.UI
{
    /// <summary>
    /// Draws a <see cref="Texture2D"/> in screen space via centralized batching in <see cref="UIRenderer"/>.
    /// Supports an explicit <see cref="Size"/> destination rectangle or sprite-style position + scale drawing.
    /// </summary>
    public class UITexture : UIRenderer
    {
        #region Fields
        private Texture2D? _texture;
        private Vector2 _position;
        private Vector2 _size;
        #endregion

        #region Properties
        /// <summary>
        /// Texture to draw.
        /// </summary>
        public Texture2D? Texture
        {
            get
            {
                return _texture;
            }
            set
            {
                _texture = value;
            }
        }

        /// <summary>
        /// Top-left position in UI space.
        /// </summary>
        public Vector2 Position
        {
            get
            {
                return _position;
            }
            set
            {
                _position = value;
            }
        }

        /// <summary>
        /// Destination size in UI space. When non-zero, the texture is drawn into a rectangle of this size.
        /// When zero, the texture is drawn at its natural size using <see cref="UIRenderer.Scale"/>.
        /// </summary>
        public Vector2 Size
        {
            get
            {
                return _size;
            }
            set
            {
                _size = value;
            }
        }

        /// <summary>
        /// Convenience property for setting the tint color on the underlying <see cref="UIRenderer.Color"/>.
        /// </summary>
        public Color Tint
        {
            get
            {
                return Color;
            }
            set
            {
                Color = value;
            }
        }

        /// <summary>
        /// Destination rectangle computed from <see cref="Position"/> and <see cref="Size"/>.
        /// Only valid when <see cref="Size"/> has positive dimensions.
        /// </summary>
        public Rectangle DestinationRectangle
        {
            get
            {
                return new Rectangle(
                    (int)_position.X,
                    (int)_position.Y,
                    (int)_size.X,
                    (int)_size.Y);
            }
        }
        #endregion

        #region Constructors
        public UITexture()
        {
            _position = Vector2.Zero;
            _size = Vector2.Zero;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Centers the origin based on the current texture and source rectangle.
        /// Useful when drawing without an explicit <see cref="Size"/>.
        /// </summary>
        public void CenterOrigin()
        {
            CenterOriginFromTexture(_texture, _sourceRect);
        }
        #endregion

        #region Lifecycle Methods
        public override void Draw(GraphicsDevice device, Camera? camera)
        {
            if (!Enabled)
                return;

            if (_spriteBatch == null)
                return;

            if (_texture == null)
                return;

            // If we have an explicit size, draw into a destination rectangle
            // so the visual matches any logical bounds defined by the caller.
            if (_size.X > 0f && _size.Y > 0f)
            {
                var destRect = DestinationRectangle;

                _spriteBatch.Draw(
                    _texture,
                    destRect,
                    _sourceRect,
                    Color,
                    RotationRadians,
                    Vector2.Zero,      // rotation around top-left of the dest rect
                    Effects,
                    LayerDepth);
            }
            else
            {
                // Fallback: original sprite-style draw using position + scale.
                _spriteBatch.Draw(
                    _texture,
                    _position,
                    _sourceRect,
                    Color,
                    RotationRadians,
                    _origin,
                    _scale,
                    Effects,
                    LayerDepth);
            }
        }
        #endregion
    }
}
