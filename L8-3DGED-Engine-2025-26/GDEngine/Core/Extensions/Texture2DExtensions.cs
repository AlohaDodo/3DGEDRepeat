using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Utilities
{
    /// <summary>
    /// Extension helpers for <see cref="Texture2D"/>.
    /// </summary>
    /// <see cref="Texture2D"/>
    public static class Texture2DExtensions  //step 1 - static and <class>Extensions
    {

        /// <summary>
        /// Creates a 3x3 white texture where the centre pixel is either transparent or black.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device used to create the texture.</param>
        /// <param name="color"> Rect color</param>
        /// <param name="transparentCenter">
        /// If true, the centre pixel is Color.Transparent; 
        /// if false, the centre pixel is Color.Black.
        /// </param>
        public static Texture2D Create3x3WithHole(this GraphicsDevice graphicsDevice, 
            Color color, bool transparentCenter = true)
        {
            if (graphicsDevice == null)
                throw new ArgumentNullException(nameof(graphicsDevice));

            Texture2D texture = new Texture2D(graphicsDevice, 3, 3);

            Color[] data = new Color[3 * 3];

            // Fill everything with white
            for (int i = 0; i < data.Length; i++)
                data[i] = color;

            // Centre index in a 3x3: row 1, col 1 -> 1 * 3 + 1 = 4
            data[4] = transparentCenter ? Color.Transparent : Color.Black;

            texture.SetData(data);
            return texture;
        }


        /// <summary>
        /// Returns the centre point of the texture in pixel coordinates.
        /// Useful as an origin when drawing with <see cref="SpriteBatch"/>.
        /// </summary>
        /// <param name="texture">The texture instance.</param>
        /// <returns>Centre of the texture in pixels as a <see cref="Vector2"/>.</returns>
        public static Vector2 GetCenter(this Texture2D texture) //2 - static method, 3 - use this in 1st param
        {
            if (texture == null)
                throw new ArgumentNullException(nameof(texture));

            float x = texture.Width * 0.5f;
            float y = texture.Height * 0.5f;

            return new Vector2(x, y);
        }

    }
}
