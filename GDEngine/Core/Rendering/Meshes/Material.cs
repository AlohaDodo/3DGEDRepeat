using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Rendering
{
    /// <summary>
    /// Minimal wrapper around an <see cref="Effect"/>.
    /// Holds render states and exposes a single Apply that works for all Effect types.
    /// </summary>
    public sealed class Material : IDisposable
    {
        #region Static Fields
        #endregion

        #region Fields
        private readonly Effect _effect;
        private RenderStates.RenderStateBlock _stateBlock;
        private SamplerState _samplerState = SamplerState.LinearWrap;
        private bool _disposed = false;
        private readonly bool _ownsEffect; // Track if we should dispose the effect
        #endregion

        #region Properties
        public Effect Effect => _effect;

        public RenderStates.RenderStateBlock StateBlock
        {
            get => _stateBlock;
            set => _stateBlock = value;
        }

        public SamplerState SamplerState
        {
            get => _samplerState;
            set => _samplerState = value ?? SamplerState.LinearWrap;
        }
        #endregion

        #region Constructors
        public Material(Effect effect, bool ownsEffect = false)
        {
            _effect = effect ?? throw new ArgumentNullException(nameof(effect));
            _stateBlock = RenderStates.Default3D();
            _ownsEffect = ownsEffect;
        }
        #endregion

        #region Methods
        public void SetTechnique(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return;
            if (_effect.Techniques[name] != null)
                _effect.CurrentTechnique = _effect.Techniques[name];
        }

        public void Apply(GraphicsDevice device, Matrix world, Matrix view, Matrix projection, EffectPropertyBlock block, Action drawCall)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));
            if (drawCall == null) throw new ArgumentNullException(nameof(drawCall));

            // Render states
            _stateBlock.Apply(device);
            if (_samplerState != null)
                device.SamplerStates[0] = _samplerState;

            // Effect binding via registry
            var binder = EffectBinderRegistry.Find(_effect);
            binder.ApplyCommonMatrices(_effect, world, view, projection);
            if (block != null) binder.Apply(_effect, block);

            var passes = _effect.CurrentTechnique.Passes;
            for (int i = 0; i < passes.Count; i++)
            {
                var pass = passes[i];
                pass.Apply();
                drawCall();
            }
        }
        #endregion

        #region Lifecycle Methods
        // None
        #endregion

        #region Housekeeping Methods
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
                // Only dispose the effect if we own it
                if (_ownsEffect)
                {
                    _effect?.Dispose();
                }
                // Note: SamplerState instances are typically static/shared, so we don't dispose them
            }

            _disposed = true;
        }

        ~Material()
        {
            Dispose(false);
        }
        #endregion
    }
}
