#nullable enable
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Rendering
{
    /// <summary>
    /// Binder for custom PBR shaders with support for:
    /// - Albedo texture (RGBA: RGB for color, A for opacity)
    /// - Normal map
    /// - SRM texture (R=Specular, G=Roughness, B=Metallic)
    /// - Emissive texture (RGBA: RGB for color, A for strength)
    /// - Global emissive strength multiplier
    /// </summary>
    public sealed class PBREffectBinder : IEffectBinder
    {
        #region Constants
        private const int MAX_LIGHTS = 8;
        #endregion

        #region Fields
        // Cached parameter lookups for performance
        private EffectParameter? _worldParam;
        private EffectParameter? _viewParam;
        private EffectParameter? _projectionParam;
        private EffectParameter? _worldInverseTransposeParam;
        
        // Texture parameters
        private EffectParameter? _albedoTextureParam;
        private EffectParameter? _normalTextureParam;
        private EffectParameter? _srmTextureParam;
        private EffectParameter? _emissiveTextureParam;
        
        // Material property parameters
        private EffectParameter? _albedoColorParam;
        private EffectParameter? _emissiveColorParam;
        private EffectParameter? _emissiveStrengthParam;
        private EffectParameter? _globalEmissiveMultiplierParam;
        private EffectParameter? _defaultSpecularParam;
        private EffectParameter? _defaultRoughnessParam;
        private EffectParameter? _defaultMetallicParam;
        
        // Texture flag parameters
        private EffectParameter? _useAlbedoTextureParam;
        private EffectParameter? _useNormalTextureParam;
        private EffectParameter? _useSRMTextureParam;
        private EffectParameter? _useEmissiveTextureParam;
        
        // Lighting parameters
        private EffectParameter? _cameraPositionParam;
        private EffectParameter? _ambientColorParam;
        private EffectParameter? _activeLightCountParam;
        private EffectParameter? _lightPositionsParam;
        private EffectParameter? _lightColorsParam;
        private EffectParameter? _lightRangesParam;
        private EffectParameter? _lightIntensitiesParam;

        private bool _parametersCached = false;
        #endregion

        #region Methods
        /// <summary>
        /// Returns true if the effect has a technique named "PBR"
        /// </summary>
        public bool Supports(Effect effect)
        {
            if (effect == null)
                return false;

            // Check if this effect has a "PBR" technique
            return effect.Techniques["PBR"] != null;
        }

        /// <summary>
        /// Applies the world, view, and projection matrices to the PBR shader
        /// </summary>
        public void ApplyCommonMatrices(Effect effect, Matrix world, Matrix view, Matrix projection)
        {
            if (effect == null)
                return;

            // Cache parameters on first use
            if (!_parametersCached)
            {
                CacheParameters(effect);
            }

            // Set matrices
            _worldParam?.SetValue(world);
            _viewParam?.SetValue(view);
            _projectionParam?.SetValue(projection);
            
            // Calculate and set WorldInverseTranspose for normal transformation
            Matrix worldInverseTranspose = Matrix.Transpose(Matrix.Invert(world));
            _worldInverseTransposeParam?.SetValue(worldInverseTranspose);
        }

        /// <summary>
        /// Applies material properties from the EffectPropertyBlock to the PBR shader
        /// </summary>
        public void Apply(Effect effect, EffectPropertyBlock block)
        {
            if (effect == null || block == null)
                return;

            // Cache parameters on first use
            if (!_parametersCached)
            {
                CacheParameters(effect);
            }

            // Apply textures
            ApplyTextures(block);
            
            // Apply material properties
            ApplyMaterialProperties(block);
            
            // Apply lighting (this would typically come from a lighting system)
            ApplyLighting(block);
        }
        #endregion

        #region Helper Methods
        /// <summary>
        /// Caches all effect parameters for efficient lookup
        /// </summary>
        private void CacheParameters(Effect effect)
        {
            // Matrix parameters
            _worldParam = effect.Parameters["World"];
            _viewParam = effect.Parameters["View"];
            _projectionParam = effect.Parameters["Projection"];
            _worldInverseTransposeParam = effect.Parameters["WorldInverseTranspose"];
            
            // Texture parameters
            _albedoTextureParam = effect.Parameters["AlbedoTexture"];
            _normalTextureParam = effect.Parameters["NormalTexture"];
            _srmTextureParam = effect.Parameters["SRMTexture"];
            _emissiveTextureParam = effect.Parameters["EmissiveTexture"];
            
            // Material property parameters
            _albedoColorParam = effect.Parameters["AlbedoColor"];
            _emissiveColorParam = effect.Parameters["EmissiveColor"];
            _emissiveStrengthParam = effect.Parameters["EmissiveStrength"];
            _globalEmissiveMultiplierParam = effect.Parameters["GlobalEmissiveMultiplier"];
            _defaultSpecularParam = effect.Parameters["DefaultSpecular"];
            _defaultRoughnessParam = effect.Parameters["DefaultRoughness"];
            _defaultMetallicParam = effect.Parameters["DefaultMetallic"];
            
            // Texture flag parameters
            _useAlbedoTextureParam = effect.Parameters["UseAlbedoTexture"];
            _useNormalTextureParam = effect.Parameters["UseNormalTexture"];
            _useSRMTextureParam = effect.Parameters["UseSRMTexture"];
            _useEmissiveTextureParam = effect.Parameters["UseEmissiveTexture"];
            
            // Lighting parameters
            _cameraPositionParam = effect.Parameters["CameraPosition"];
            _ambientColorParam = effect.Parameters["AmbientColor"];
            _activeLightCountParam = effect.Parameters["ActiveLightCount"];
            _lightPositionsParam = effect.Parameters["LightPositions"];
            _lightColorsParam = effect.Parameters["LightColors"];
            _lightRangesParam = effect.Parameters["LightRanges"];
            _lightIntensitiesParam = effect.Parameters["LightIntensities"];
            
            _parametersCached = true;
        }

        /// <summary>
        /// Applies texture parameters from the property block
        /// </summary>
        private void ApplyTextures(EffectPropertyBlock block)
        {
            // Albedo texture
            if (block.TryGet(PropertyKeys.AlbedoTexture, out Texture2D? albedoTex))
            {
                _albedoTextureParam?.SetValue(albedoTex);
                _useAlbedoTextureParam?.SetValue(albedoTex != null);
            }

            // Normal map
            if (block.TryGet(PropertyKeys.NormalTexture, out Texture2D? normalTex))
            {
                _normalTextureParam?.SetValue(normalTex);
                _useNormalTextureParam?.SetValue(normalTex != null);
            }

            // SRM texture (Specular/Roughness/Metallic)
            if (block.TryGet(PropertyKeys.SRMTexture, out Texture2D? srmTex))
            {
                _srmTextureParam?.SetValue(srmTex);
                _useSRMTextureParam?.SetValue(srmTex != null);
            }

            // Emissive texture
            if (block.TryGet(PropertyKeys.EmissiveTexture, out Texture2D? emissiveTex))
            {
                _emissiveTextureParam?.SetValue(emissiveTex);
                _useEmissiveTextureParam?.SetValue(emissiveTex != null);
            }
        }

        /// <summary>
        /// Applies material property parameters from the property block
        /// </summary>
        private void ApplyMaterialProperties(EffectPropertyBlock block)
        {
            // Albedo color (RGB + Alpha for opacity)
            if (block.TryGet(PropertyKeys.AlbedoColor, out Color albedoColor))
            {
                _albedoColorParam?.SetValue(albedoColor.ToVector4());
            }

            // Emissive color
            if (block.TryGet(PropertyKeys.EmissiveColor, out Vector3 emissiveColor))
            {
                _emissiveColorParam?.SetValue(emissiveColor);
            }

            // Emissive strength
            if (block.TryGet(PropertyKeys.EmissiveStrength, out float emissiveStrength))
            {
                _emissiveStrengthParam?.SetValue(emissiveStrength);
            }

            // Global emissive multiplier
            if (block.TryGet(PropertyKeys.GlobalEmissiveMultiplier, out float globalEmissive))
            {
                _globalEmissiveMultiplierParam?.SetValue(globalEmissive);
            }

            // Default material values (used when no SRM texture is present)
            if (block.TryGet(PropertyKeys.DefaultSpecular, out float specular))
            {
                _defaultSpecularParam?.SetValue(specular);
            }

            if (block.TryGet(PropertyKeys.DefaultRoughness, out float roughness))
            {
                _defaultRoughnessParam?.SetValue(roughness);
            }

            if (block.TryGet(PropertyKeys.DefaultMetallic, out float metallic))
            {
                _defaultMetallicParam?.SetValue(metallic);
            }
        }

        /// <summary>
        /// Applies lighting parameters from the property block
        /// Note: In a real implementation, this would typically pull from a LightingSystem
        /// </summary>
        private void ApplyLighting(EffectPropertyBlock block)
        {
            // Camera position (for specular calculations)
            if (block.TryGet("CameraPosition", out Vector3 camPos))
            {
                _cameraPositionParam?.SetValue(camPos);
            }

            // Ambient color
            if (block.TryGet("AmbientColor", out Vector3 ambient))
            {
                _ambientColorParam?.SetValue(ambient);
            }

            // Point lights
            if (block.TryGet("ActiveLightCount", out int lightCount))
            {
                _activeLightCountParam?.SetValue(Math.Min(lightCount, MAX_LIGHTS));
            }

            if (block.TryGet("LightPositions", out Vector3[] lightPositions))
            {
                _lightPositionsParam?.SetValue(lightPositions);
            }

            if (block.TryGet("LightColors", out Vector3[] lightColors))
            {
                _lightColorsParam?.SetValue(lightColors);
            }

            if (block.TryGet("LightRanges", out float[] lightRanges))
            {
                _lightRangesParam?.SetValue(lightRanges);
            }

            if (block.TryGet("LightIntensities", out float[] lightIntensities))
            {
                _lightIntensitiesParam?.SetValue(lightIntensities);
            }
        }
        #endregion
    }
}
