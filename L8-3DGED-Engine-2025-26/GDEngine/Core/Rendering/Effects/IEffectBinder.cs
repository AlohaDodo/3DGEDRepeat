using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Rendering
{
    /// <summary>
    /// Maps generic property keys from <see cref="EffectPropertyBlock"/> to a concrete MonoGame <see cref="Effect"/>.
    /// </summary>
    public interface IEffectBinder
    {
        bool Supports(Effect effect);
        void ApplyCommonMatrices(Effect effect, Matrix world, Matrix view, Matrix projection);
        void Apply(Effect effect, EffectPropertyBlock block);
    }
}
