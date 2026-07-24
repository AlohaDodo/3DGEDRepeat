using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Rendering
{
    /// <summary>
    /// Binder for <see cref="AlphaTestEffect"/>.
    /// </summary>
    public sealed class AlphaTestEffectBinder : IEffectBinder
    {
        public bool Supports(Effect effect) => effect is AlphaTestEffect;

        public void ApplyCommonMatrices(Effect effect, Matrix world, Matrix view, Matrix projection)
        {
            var ae = (AlphaTestEffect)effect;
            ae.World = world;
            ae.View = view;
            ae.Projection = projection;
        }

        public void Apply(Effect effect, EffectPropertyBlock block)
        {
            var ae = (AlphaTestEffect)effect;

            if (block.TryGet(PropertyKeys.MainTexture, out Texture2D? tex))
                ae.Texture = tex;

            if (block.TryGet(PropertyKeys.Alpha, out float alpha))
                ae.Alpha = alpha;

            if (block.TryGet(PropertyKeys.ReferenceAlpha, out int refA))
                ae.ReferenceAlpha = refA;

            if (block.TryGet(PropertyKeys.VertexColorEnabled, out bool vcol))
                ae.VertexColorEnabled = vcol;
        }
    }
}
