using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Rendering
{
    /// <summary>
    /// Handy render-state presets and a simple immutable state block you can apply per draw.
    /// </summary>
    public static class RenderStates
    {
        #region Static Fields
        private static readonly RasterizerState _wireframeRasterizer = new RasterizerState
        {
            FillMode = FillMode.WireFrame,
            CullMode = CullMode.None,
            MultiSampleAntiAlias = false
        };
        #endregion

        #region Methods
        public static RenderStateBlock Opaque3D()
        {
            return new RenderStateBlock(
                BlendState.Opaque,
                DepthStencilState.Default,
                RasterizerState.CullCounterClockwise,
                SamplerState.LinearWrap
            );
        }

        public static RenderStateBlock AlphaBlend3D()
        {
            return new RenderStateBlock(
                BlendState.AlphaBlend,
                DepthStencilState.DepthRead,
                RasterizerState.CullCounterClockwise,
                SamplerState.LinearWrap
            );
        }

        public static RenderStateBlock Cutout3D()
        {
            return new RenderStateBlock(
                BlendState.Opaque,
                DepthStencilState.Default,
                RasterizerState.CullCounterClockwise,
                SamplerState.LinearWrap
            );
        }

        public static RenderStateBlock Additive3D()
        {
            return new RenderStateBlock(
                BlendState.Additive,
                DepthStencilState.DepthRead,
                RasterizerState.CullCounterClockwise,
                SamplerState.LinearWrap
            );
        }

        // Use custom rasterizer with FillMode.WireFrame
        public static RenderStateBlock Wireframe3D()
        {
            return new RenderStateBlock(
                BlendState.Opaque,
                DepthStencilState.Default,
                _wireframeRasterizer,
                SamplerState.LinearClamp
            );
        }

        // Optional alias if other code expects Default3D()
        public static RenderStateBlock Default3D() => Opaque3D();
        #endregion

        /// <summary>
        /// Immutable set of GPU render states. Apply to a <see cref="GraphicsDevice"/> before drawing.
        /// </summary>
        public readonly struct RenderStateBlock
        {
            public readonly BlendState? Blend;
            public readonly DepthStencilState? DepthStencil;
            public readonly RasterizerState? Rasterizer;
            public readonly SamplerState? Sampler;

            public RenderStateBlock(
                BlendState? blend,
                DepthStencilState? depthStencil,
                RasterizerState? rasterizer,
                SamplerState? sampler)
            {
                Blend = blend;
                DepthStencil = depthStencil;
                Rasterizer = rasterizer;
                Sampler = sampler;
            }

            public void Apply(GraphicsDevice device)
            {
                if (Blend != null) device.BlendState = Blend;
                if (DepthStencil != null) device.DepthStencilState = DepthStencil;
                if (Rasterizer != null) device.RasterizerState = Rasterizer;
                if (Sampler != null) device.SamplerStates[0] = Sampler;
            }

            public RenderStateBlock WithBlend(BlendState? blend)
                => new RenderStateBlock(blend, DepthStencil, Rasterizer, Sampler);

            public RenderStateBlock WithDepth(DepthStencilState? depth)
                => new RenderStateBlock(Blend, depth, Rasterizer, Sampler);

            public RenderStateBlock WithRaster(RasterizerState? rast)
                => new RenderStateBlock(Blend, DepthStencil, rast, Sampler);

            public RenderStateBlock WithSampler(SamplerState? samp)
                => new RenderStateBlock(Blend, DepthStencil, Rasterizer, samp);
        }
    }
}
