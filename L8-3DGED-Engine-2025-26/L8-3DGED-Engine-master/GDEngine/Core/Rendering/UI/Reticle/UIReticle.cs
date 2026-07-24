#nullable enable
using GDEngine.Core.Components;
using GDEngine.Core.Entities;
using GDEngine.Core.Timing;
using GDEngine.Core.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Rendering.UI
{
    /// <summary>
    /// Draws a rotating reticle sprite at the mouse cursor (with optional offset/scale).
    /// Uses centralized batching in <see cref="UIRenderer"/>.
    /// Optimized to cache mouse state in Update rather than polling in Draw.
    /// </summary>
    public class UIReticle : UIRenderer
    {
        #region Fields
        private Texture2D _texture = null!;
        private Vector2 _offset = Vector2.Zero;
        private float _rotationSpeedDegPerSec = 90f;

        // Cached viewport center
        private Vector2 _viewportCenter;
        #endregion

        #region Constructors
        public UIReticle(Texture2D texture)
        {
            _texture = texture;
        }
        #endregion

        #region Properties
        public Texture2D Texture
        {
            get => _texture;
            set
            {
                _texture = value;
                RecenterOriginFromSource();
            }
        }

        /// <summary>
        /// Optional 2D offset from the mouse position in pixels.
        /// </summary>
        public Vector2 Offset
        {
            get => _offset;
            set => _offset = value;
        }

        public float RotationSpeedDegPerSec
        {
            get => _rotationSpeedDegPerSec;
            set => _rotationSpeedDegPerSec = value;
        }

        /// <summary>
        /// Tint color for the reticle. Accesses base class Color property.
        /// </summary>
        public Color Tint
        {
            get => Color;
            set => Color = value;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Recalculates origin to center of texture/source rectangle.
        /// Now uses base class helper method.
        /// </summary>
        public void RecenterOriginFromSource()
        {
            CenterOriginFromTexture(_texture, _sourceRect);
        }
        #endregion

        #region Lifecycle Methods
        protected override void Start()
        {
            if (GameObject == null)
                throw new NullReferenceException(nameof(GameObject));

            // Cache viewport center
            var scene = GameObject.Scene;

            if (scene == null)
                throw new NullReferenceException(nameof(scene));

            var activeCamera = scene.ActiveCamera;
            var device = scene.Context.GraphicsDevice;

            if (activeCamera == null)
                throw new NullReferenceException(nameof(activeCamera));

            _viewportCenter = activeCamera.GetViewport(device).GetCenter();
        }
        protected override void Update(float deltaTime)
        {
            if(_rotationSpeedDegPerSec != 0)
                // Advance base RotationRadians so we use the centralized rotation field
                RotationRadians += MathHelper.ToRadians(_rotationSpeedDegPerSec) * Time.DeltaTimeSecs;
        }

        public override void Draw(GraphicsDevice device, Camera? camera)
        {
            if (_spriteBatch == null || _texture == null)
                return;

            // Use cached mouse position from Update
            var pos = _viewportCenter + _offset;  //Mouse.GetState().Position;

            _spriteBatch.Draw(
                _texture,
                pos,
                _sourceRect,
                Color,
                RotationRadians,
                _origin,
                _scale,
                Effects,
                LayerDepth);
        }
        #endregion
    }
}