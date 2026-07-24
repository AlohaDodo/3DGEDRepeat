using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Rendering
{
    /// <summary>
    /// Binder for <see cref="EnvironmentMapEffect"/>.
    /// </summary>
    public sealed class EnvironmentMapEffectBinder : IEffectBinder
    {
        public bool Supports(Effect effect) => effect is EnvironmentMapEffect;

        public void ApplyCommonMatrices(Effect effect, Matrix world, Matrix view, Matrix projection)
        {
            var ee = (EnvironmentMapEffect)effect;
            ee.World = world;
            ee.View = view;
            ee.Projection = projection;
        }

        public void Apply(Effect effect, EffectPropertyBlock block)
        {
            var ee = (EnvironmentMapEffect)effect;

            if (block.TryGet(PropertyKeys.MainTexture, out Texture2D? baseTex))
                ee.Texture = baseTex;

            if (block.TryGet(PropertyKeys.EnvironmentMap, out TextureCube? cube))
                ee.EnvironmentMap = cube;

            if (block.TryGet(PropertyKeys.EnvAmount, out float amt))
                ee.EnvironmentMapAmount = amt;

            if (block.TryGet(PropertyKeys.Fresnel, out float fres))
                ee.FresnelFactor = fres;

            if (block.TryGet(PropertyKeys.Tint, out Color tint))
                ee.DiffuseColor = tint.ToVector3();

            if (block.TryGet(PropertyKeys.Alpha, out float alpha))
                ee.Alpha = alpha;
        }
    }
}
