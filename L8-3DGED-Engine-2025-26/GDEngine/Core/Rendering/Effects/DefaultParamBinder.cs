using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Rendering
{
    /// <summary>
    /// Fallback binder that sets parameters by name for custom compiled <see cref="Effect"/>s.
    /// Expects conventional parameter names: "World", "View", "Projection", "MainTexture", "Tint", etc.
    /// </summary>
    public sealed class DefaultParamBinder : IEffectBinder
    {
        public bool Supports(Effect effect) => true;

        public void ApplyCommonMatrices(Effect effect, Matrix world, Matrix view, Matrix projection)
        {
            var p = effect.Parameters;
            p?["World"]?.SetValue(world);
            p?["View"]?.SetValue(view);
            p?["Projection"]?.SetValue(projection);
        }

        public void Apply(Effect effect, EffectPropertyBlock block)
        {
            if (effect.Parameters == null) return;

            foreach (var kv in block.Entries)
            {
                var name = kv.Key;
                var val = kv.Value;
                var param = effect.Parameters[name];
                if (param == null) continue;

                switch (val)
                {
                    case float f: param.SetValue(f); break;
                    case int i: param.SetValue(i); break;
                    case bool b: param.SetValue(b); break;
                    case Vector2 v2: param.SetValue(v2); break;
                    case Vector3 v3: param.SetValue(v3); break;
                    case Vector4 v4: param.SetValue(v4); break;
                    case Matrix m: param.SetValue(m); break;
                    case Matrix[] ms: param.SetValue(ms); break;
                    case Texture2D t2d: param.SetValue(t2d); break;
                    case TextureCube tc: param.SetValue(tc); break;
                    // SamplerState not directly settable; students set it via Material/StateBlock if needed.
                }
            }
        }
    }
}
