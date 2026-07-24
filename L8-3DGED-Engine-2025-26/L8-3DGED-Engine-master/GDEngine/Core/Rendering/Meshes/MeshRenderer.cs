using GDEngine.Core.Components;
using GDEngine.Core.Entities;
using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Rendering
{
    /// <summary>
    /// Draws a <see cref="MeshFilter"/> using a <see cref="Material"/>; supports per-object overrides via <see cref="EffectPropertyBlock"/>.
    /// </summary>
    /// <see cref="MeshFilter"/>
    /// <see cref="Material"/>
    /// <see cref="Camera"/>
    public sealed class MeshRenderer : Component
    {
        #region Static Fields
        #endregion

        #region Fields
        private MeshFilter? _meshFilter;
        private Material? _material;
        private readonly EffectPropertyBlock _overrides = new();
        #endregion

        #region Properties
        public Material? Material
        {
            get => _material;
            set => _material = value;
        }

        public EffectPropertyBlock Overrides
        {
            get => _overrides;
        }
        #endregion

        #region Constructors
        #endregion

        #region Methods
        /// <summary>
        /// Renders this renderer using the supplied graphics device and camera.
        /// </summary>
        public void Render(GraphicsDevice device, Camera camera)
        {
            if (Transform == null)
                return;
            if (_meshFilter == null)
                return;
            if (_material == null)
                return;

            _meshFilter.BindBuffers(device);

            _material.Apply(
                device,
                Transform.WorldMatrix,
                camera.View,
                camera.Projection,
                _overrides,
                () => device.DrawIndexedPrimitives(
                    _meshFilter.PrimitiveType,
                    0,
                    0,
                    _meshFilter.PrimitiveCount));
        }

        /// <summary>
        /// Convenience wrapper so systems that expect Draw() still work.
        /// </summary>
        public void Draw(GraphicsDevice device, Camera camera)
        {
            Render(device, camera);
        }
        #endregion

        #region Lifecycle Methods
        protected override void Start()
        {
            if (GameObject == null)
                return;

            _meshFilter = GameObject.GetComponent<MeshFilter>();

            // Register with the owning Scene so RenderSystem/CameraSystem can see us
            Scene? scene = GameObject.Scene;
            if (scene != null)
                scene.RegisterRenderer(this);
        }
        #endregion

        #region Housekeeping Methods
        protected override void OnDestroy()
        {
            // Unregister from the scene so we are no longer considered for rendering
            if (GameObject != null)
            {
                Scene? scene = GameObject.Scene;
                if (scene != null)
                    scene.UnregisterRenderer(this);
            }

            base.OnDestroy();
        }
        #endregion
    }
}
