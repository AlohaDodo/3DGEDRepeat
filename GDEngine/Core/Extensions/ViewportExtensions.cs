using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Utilities
{
    /// <summary>
    /// Extension helpers for <see cref="Viewport"/>.
    /// </summary>
    /// <see cref="Core.Camera"/>
    public static class ViewportExtensions
    {

        /// <summary>
        /// Returns the centre point of the viewport in pixel coordinates.
        /// </summary>
        /// <param name="viewport">The viewport instance.</param>
        /// <returns>Centre of the viewport in pixels as a <see cref="Vector2"/>.</returns>
        public static Vector2 GetCenter(this Viewport viewport)
        {
            return new Vector2(viewport.Width, viewport.Height) * 0.5f;
        }

    }
}
