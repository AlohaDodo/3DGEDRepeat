using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Rendering
{
    /// <summary>
    /// Chooses the appropriate <see cref="IEffectBinder"/> at runtime.
    /// </summary>
    public static class EffectBinderRegistry
    {
        #region Static Fields
        private static readonly List<IEffectBinder> _binders = new()
        {
            new BasicEffectBinder(),
            new AlphaTestEffectBinder(),
            new DualTextureEffectBinder(),
            new EnvironmentMapEffectBinder(),
            new PBREffectBinder()
            //TODO - skinned, custom effect support
        };
        #endregion

        #region Methods
        public static IEffectBinder Find(Effect fx)
        {
            for (int i = 0; i < _binders.Count; i++)
                if (_binders[i].Supports(fx)) return _binders[i];
            return new DefaultParamBinder();
        }
        #endregion
    }
}
