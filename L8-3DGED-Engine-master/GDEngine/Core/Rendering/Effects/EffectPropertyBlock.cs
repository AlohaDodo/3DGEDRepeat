using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Rendering
{
    /// <summary>
    /// Per-object surface overrides that are independent of the concrete <see cref="Effect"/> type.
    /// Keys like "MainTexture", "Tint", "Alpha", "Bones" are intentionally shared across all effect types.
    /// </summary>
    /// <see cref="IEffectBinder"/>
    public sealed class EffectPropertyBlock
    {
        #region Static Fields
        #endregion

        #region Fields
        private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);
        #endregion

        #region Properties
        public IEnumerable<KeyValuePair<string, object>> Entries => _values;

        // Student-friendly aliases
        public Texture2D MainTexture { set => SetTexture(PropertyKeys.MainTexture, value); }
        public Texture2D SecondaryTexture { set => SetTexture(PropertyKeys.Texture2, value); }
        public Color Tint { set => SetColor(PropertyKeys.Tint, value); }
        public float Alpha { set => SetFloat(PropertyKeys.Alpha, value); }
        public bool UseLighting { set => SetBool(PropertyKeys.UseLighting, value); }
        public float SpecularPower { set => SetFloat(PropertyKeys.SpecularPower, value); }
        public Matrix[] Bones { set => SetMatrices(PropertyKeys.Bones, value); }
        #endregion

        #region Constructors
        #endregion

        #region Methods
        // Typed setters students will actually use
        public void SetTexture(string key, Texture2D tex) => _values[key] = tex;
        public void SetFloat(string key, float v) => _values[key] = v;
        public void SetInt(string key, int v) => _values[key] = v;
        public void SetBool(string key, bool v) => _values[key] = v;
        public void SetColor(string key, Color c) => _values[key] = c;
        public void SetVector2(string key, Vector2 v) => _values[key] = v;
        public void SetVector3(string key, Vector3 v) => _values[key] = v;
        public void SetVector4(string key, Vector4 v) => _values[key] = v;
        public void SetMatrix(string key, Matrix m) => _values[key] = m;
        public void SetMatrices(string key, Matrix[] ms) => _values[key] = ms;
        public void SetSampler(string key, SamplerState s) => _values[key] = s;

        public bool TryGet<T>(string key, out T? value)
        {
            if (_values.TryGetValue(key, out var obj) && obj is T t)
            {
                value = t;
                return true;
            }
            value = default;
            return false;
        }

        public void Clear() => _values.Clear();
        #endregion

        #region Lifecycle Methods
        // None
        #endregion

        #region Housekeeping Methods
        #endregion
    }

    /// <summary>
    /// Canonical property keys used by <see cref="EffectPropertyBlock"/> and binders.
    /// </summary>
    public static class PropertyKeys
    {
        // ORIGINAL PROPERTY KEYS
        /// <summary>
        /// Primary texture for the material (used by BasicEffect, AlphaTestEffect, etc.)
        /// </summary>
        public const string MainTexture = "MainTexture";

        /// <summary>
        /// Secondary texture for dual-texture effects (used by DualTextureEffect)
        /// </summary>
        public const string Texture2 = "Texture2";

        /// <summary>
        /// Color tint applied to the material (multiplied with texture color)
        /// </summary>
        public const string Tint = "Tint";

        /// <summary>
        /// Alpha/opacity value for transparency (0.0 = fully transparent, 1.0 = fully opaque)
        /// </summary>
        public const string Alpha = "Alpha";

        /// <summary>
        /// Flag indicating whether to use lighting calculations (for BasicEffect)
        /// </summary>
        public const string UseLighting = "UseLighting";

        /// <summary>
        /// Specular power/shininess value for Phong/Blinn-Phong lighting (higher = sharper highlights)
        /// </summary>
        public const string SpecularPower = "SpecularPower";

        /// <summary>
        /// Bone transformation matrices for skinned mesh animation
        /// </summary>
        public const string Bones = "Bones";

        /// <summary>
        /// Environment/reflection map texture (used by EnvironmentMapEffect)
        /// </summary>
        public const string EnvironmentMap = "EnvironmentMap";

        /// <summary>
        /// Amount of environment map reflection to blend (0.0 = none, 1.0 = full reflection)
        /// </summary>
        public const string EnvAmount = "EnvAmount";

        /// <summary>
        /// Fresnel effect amount for environment mapping (edge-based reflection intensity)
        /// </summary>
        public const string Fresnel = "Fresnel";

        /// <summary>
        /// Reference alpha value for alpha testing (pixels with alpha below this are discarded)
        /// </summary>
        public const string ReferenceAlpha = "ReferenceAlpha";

        /// <summary>
        /// Flag indicating whether to use per-vertex color data
        /// </summary>
        public const string VertexColorEnabled = "VertexColorEnabled";

        // PBR MATERIAL PROPERTIES - New keys added for PBR support

        /// <summary>
        /// Albedo texture (RGBA: RGB for base color, A for opacity)
        /// </summary>
        public const string AlbedoTexture = "AlbedoTexture";

        /// <summary>
        /// Albedo color multiplier (default: white)
        /// </summary>
        public const string AlbedoColor = "AlbedoColor";

        /// <summary>
        /// Normal map texture (tangent-space)
        /// </summary>
        public const string NormalTexture = "NormalTexture";

        /// <summary>
        /// SRM texture (R=Specular, G=Roughness, B=Metallic)
        /// </summary>
        public const string SRMTexture = "SRMTexture";

        /// <summary>
        /// Emissive texture (RGBA: RGB for color, A for per-pixel strength)
        /// </summary>
        public const string EmissiveTexture = "EmissiveTexture";

        /// <summary>
        /// Emissive color (default: black - no emission)
        /// </summary>
        public const string EmissiveColor = "EmissiveColor";

        /// <summary>
        /// Emissive strength multiplier (default: 1.0)
        /// </summary>
        public const string EmissiveStrength = "EmissiveStrength";

        /// <summary>
        /// Global emissive multiplier for scene-wide emission control (default: 1.0)
        /// </summary>
        public const string GlobalEmissiveMultiplier = "GlobalEmissiveMultiplier";

        /// <summary>
        /// Default specular value when no SRM texture is present (0.0 - 1.0, default: 0.5)
        /// </summary>
        public const string DefaultSpecular = "DefaultSpecular";

        /// <summary>
        /// Default roughness value when no SRM texture is present (0.0 - 1.0, default: 0.5)
        /// </summary>
        public const string DefaultRoughness = "DefaultRoughness";

        /// <summary>
        /// Default metallic value when no SRM texture is present (0.0 - 1.0, default: 0.0)
        /// </summary>
        public const string DefaultMetallic = "DefaultMetallic";

        // PBR TEXTURE FLAGS
        // Flags to enable/disable texture usage in shaders

        /// <summary>
        /// Flag indicating whether to use the albedo texture
        /// </summary>
        public const string UseAlbedoTexture = "UseAlbedoTexture";

        /// <summary>
        /// Flag indicating whether to use the normal texture
        /// </summary>
        public const string UseNormalTexture = "UseNormalTexture";

        /// <summary>
        /// Flag indicating whether to use the SRM texture
        /// </summary>
        public const string UseSRMTexture = "UseSRMTexture";

        /// <summary>
        /// Flag indicating whether to use the emissive texture
        /// </summary>
        public const string UseEmissiveTexture = "UseEmissiveTexture";
    }
}
