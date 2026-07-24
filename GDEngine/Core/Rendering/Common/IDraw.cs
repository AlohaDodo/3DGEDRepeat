using GDEngine.Core.Components;
using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Rendering.Base
{
    /// <summary>
    /// Base interface for any component that has a Draw method and runs in Render or PostRender frame lifecycle
    /// </summary>
    /// <see cref="Systems.Draw.RenderSystem"/>
    /// <see cref="Systems.Draw.UIRenderSystem"/>
    public interface IDraw
    {
        public void Draw(GraphicsDevice device, Camera? camera);
    }
}