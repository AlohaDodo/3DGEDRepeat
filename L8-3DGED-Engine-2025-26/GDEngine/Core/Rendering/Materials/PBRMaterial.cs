#nullable enable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Rendering
{
    /// <summary>
    /// Helper class for creating and configuring PBR materials.
    /// Provides a convenient API for setting up:
    /// - Albedo (RGB + Alpha for opacity)
    /// - Normal maps
    /// - SRM maps (Specular/Roughness/Metallic)
    /// - Emissive (RGB + Strength)
    /// </summary>
    public sealed class PBRMaterial : IDisposable
    {
        #region Fields
        private readonly Material _material;
        private readonly EffectPropertyBlock _properties;
        private bool _disposed = false;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the underlying Material instance
        /// </summary>
        public Material Material => _material;

        /// <summary>
        /// Gets the property block for advanced customization
        /// </summary>
        public EffectPropertyBlock Properties => _properties;

        /// <summary>
        /// Albedo texture (RGBA: RGB for color, A for opacity)
        /// </summary>
        public Texture2D? AlbedoTexture
        {
            set
            {
                _properties.SetTexture(PropertyKeys.AlbedoTexture, value!);
                _properties.SetBool(PropertyKeys.UseAlbedoTexture, value != null);
            }
        }

        /// <summary>
        /// Albedo color multiplier (default: white)
        /// </summary>
        public Color AlbedoColor
        {
            set => _properties.SetColor(PropertyKeys.AlbedoColor, value);
        }

        /// <summary>
        /// Normal map texture
        /// </summary>
        public Texture2D? NormalTexture
        {
            set
            {
                _properties.SetTexture(PropertyKeys.NormalTexture, value!);
                _properties.SetBool(PropertyKeys.UseNormalTexture, value != null);
            }
        }

        /// <summary>
        /// SRM texture (R=Specular, G=Roughness, B=Metallic)
        /// </summary>
        public Texture2D? SRMTexture
        {
            set
            {
                _properties.SetTexture(PropertyKeys.SRMTexture, value!);
                _properties.SetBool(PropertyKeys.UseSRMTexture, value != null);
            }
        }

        /// <summary>
        /// Emissive texture (RGBA: RGB for color, A for strength)
        /// </summary>
        public Texture2D? EmissiveTexture
        {
            set
            {
                _properties.SetTexture(PropertyKeys.EmissiveTexture, value!);
                _properties.SetBool(PropertyKeys.UseEmissiveTexture, value != null);
            }
        }

        /// <summary>
        /// Emissive color (default: black - no emission)
        /// </summary>
        public Vector3 EmissiveColor
        {
            set => _properties.SetVector3(PropertyKeys.EmissiveColor, value);
        }

        /// <summary>
        /// Emissive strength multiplier (default: 1.0)
        /// </summary>
        public float EmissiveStrength
        {
            set => _properties.SetFloat(PropertyKeys.EmissiveStrength, value);
        }

        /// <summary>
        /// Global emissive multiplier (default: 1.0)
        /// </summary>
        public float GlobalEmissiveMultiplier
        {
            set => _properties.SetFloat(PropertyKeys.GlobalEmissiveMultiplier, value);
        }

        /// <summary>
        /// Default specular value when no SRM texture is used (0.0 - 1.0, default: 0.5)
        /// </summary>
        public float DefaultSpecular
        {
            set => _properties.SetFloat(PropertyKeys.DefaultSpecular, value);
        }

        /// <summary>
        /// Default roughness value when no SRM texture is used (0.0 - 1.0, default: 0.5)
        /// </summary>
        public float DefaultRoughness
        {
            set => _properties.SetFloat(PropertyKeys.DefaultRoughness, value);
        }

        /// <summary>
        /// Default metallic value when no SRM texture is used (0.0 - 1.0, default: 0.0)
        /// </summary>
        public float DefaultMetallic
        {
            set => _properties.SetFloat(PropertyKeys.DefaultMetallic, value);
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new PBR material with the specified effect
        /// </summary>
        /// <param name="effect">The PBR effect (must have a "PBR" technique)</param>
        /// <param name="ownsEffect">If true, the effect will be disposed when this material is disposed</param>
        public PBRMaterial(Effect effect, bool ownsEffect = false)
        {
            if (effect == null)
                throw new ArgumentNullException(nameof(effect));

            _material = new Material(effect, ownsEffect);
            _properties = new EffectPropertyBlock();

            // Set default values
            InitializeDefaults();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Sets default PBR material values
        /// </summary>
        private void InitializeDefaults()
        {
            AlbedoColor = Color.White;
            EmissiveColor = Vector3.Zero;
            EmissiveStrength = 1.0f;
            GlobalEmissiveMultiplier = 1.0f;
            DefaultSpecular = 0.5f;
            DefaultRoughness = 0.5f;
            DefaultMetallic = 0.0f;
        }

        /// <summary>
        /// Applies this material to the graphics device and executes the draw call
        /// </summary>
        public void Apply(GraphicsDevice device, Matrix world, Matrix view, Matrix projection, Action drawCall)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            if (drawCall == null)
                throw new ArgumentNullException(nameof(drawCall));

            _material.Apply(device, world, view, projection, _properties, drawCall);
        }

        /// <summary>
        /// Sets the render state block (blend state, depth state, etc.)
        /// </summary>
        public void SetRenderStates(RenderStates.RenderStateBlock stateBlock)
        {
            _material.StateBlock = stateBlock;
        }

        /// <summary>
        /// Sets the sampler state for textures
        /// </summary>
        public void SetSamplerState(SamplerState samplerState)
        {
            _material.SamplerState = samplerState;
        }

        /// <summary>
        /// Configures lighting parameters (typically called by a lighting system)
        /// </summary>
        public void SetLighting(
            Vector3 cameraPosition,
            Vector3 ambientColor,
            int activeLightCount,
            Vector3[] lightPositions,
            Vector3[] lightColors,
            float[] lightRanges,
            float[] lightIntensities)
        {
            _properties.SetVector3("CameraPosition", cameraPosition);
            _properties.SetVector3("AmbientColor", ambientColor);
            _properties.SetInt("ActiveLightCount", activeLightCount);
            _properties.SetValue("LightPositions", lightPositions);
            _properties.SetValue("LightColors", lightColors);
            _properties.SetValue("LightRanges", lightRanges);
            _properties.SetValue("LightIntensities", lightIntensities);
        }
        #endregion

        #region Housekeeping
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _material?.Dispose();
                _properties?.Clear();
            }

            _disposed = true;
        }

        ~PBRMaterial()
        {
            Dispose(false);
        }
        #endregion
    }

    /// <summary>
    /// Extension methods for EffectPropertyBlock to support PBR-specific array types
    /// </summary>
    public static class PBRPropertyBlockExtensions
    {
        public static void SetValue<T>(this EffectPropertyBlock block, string key, T[] value)
        {
            if (block == null)
                throw new ArgumentNullException(nameof(block));

            // Store the array directly - the binder will handle it
            var dict = typeof(EffectPropertyBlock)
                .GetField("_values", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(block) as Dictionary<string, object>;

            if (dict != null && value != null)
            {
                dict[key] = value;
            }
        }
    }
}
