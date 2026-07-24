using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Rendering
{
    /// <summary>
    /// Binder for <see cref="DualTextureEffect"/>.
    /// </summary>
    public sealed class DualTextureEffectBinder : IEffectBinder
    {
        public bool Supports(Effect effect) => effect is DualTextureEffect;

        public void ApplyCommonMatrices(Effect effect, Matrix world, Matrix view, Matrix projection)
        {
            var de = (DualTextureEffect)effect;
            de.World = world;
            de.View = view;
            de.Projection = projection;
        }

        public void Apply(Effect effect, EffectPropertyBlock block)
        {
            var de = (DualTextureEffect)effect;

            if (block.TryGet(PropertyKeys.MainTexture, out Texture2D? t1))
                de.Texture = t1;

            if (block.TryGet(PropertyKeys.Texture2, out Texture2D? t2))
                de.Texture2 = t2;

            if (block.TryGet(PropertyKeys.Tint, out Color tint))
                de.DiffuseColor = tint.ToVector3();

            if (block.TryGet(PropertyKeys.Alpha, out float alpha))
                de.Alpha = alpha;
        }
    }
}
