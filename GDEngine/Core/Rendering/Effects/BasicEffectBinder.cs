using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Rendering
{
    /// <summary>
    /// Binder for <see cref="BasicEffect"/>.
    /// </summary>
    public sealed class BasicEffectBinder : IEffectBinder
    {
        public bool Supports(Effect effect) => effect is BasicEffect;

        public void ApplyCommonMatrices(Effect effect, Matrix world, Matrix view, Matrix projection)
        {
            var be = (BasicEffect)effect;
            be.World = world;
            be.View = view;
            be.Projection = projection;
        }

        public void Apply(Effect effect, EffectPropertyBlock block)
        {
            var be = (BasicEffect)effect;

            if (block.TryGet(PropertyKeys.MainTexture, out Texture2D? tex))
            {
                be.TextureEnabled = tex != null;
                be.Texture = tex;
            }

            if (block.TryGet(PropertyKeys.UseLighting, out bool useLighting))
            {
                be.LightingEnabled = useLighting;
                if (useLighting) be.PreferPerPixelLighting = true;
            }

            if (block.TryGet(PropertyKeys.Tint, out Color tint))
            {
                be.DiffuseColor = tint.ToVector3();
                be.Alpha = tint.A / 255f;
            }

            if (block.TryGet(PropertyKeys.Alpha, out float alpha))
                be.Alpha = alpha;

            if (block.TryGet(PropertyKeys.SpecularPower, out float spec))
                be.SpecularPower = spec;
        }
    }
}
