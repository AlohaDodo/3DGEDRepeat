using GDEngine.Core.Components;
using GDEngine.Core.Entities;
using GDEngine.Core.Rendering;
using GDEngine.Core.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public class UIRotatedSprite : UIRenderer
{
    public Texture2D? Texture { get; set; }
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; } = Vector2.One;
    public Color Color { get; set; } = Color.White;
    public float Rotation { get; set; } = 0f;

    public override void Draw(GraphicsDevice device, Camera? camera)
    {
        if (Texture == null) return;

        var spriteBatch = EngineContext.Instance.SpriteBatch;

        spriteBatch.Draw(
            Texture,
            new Rectangle(
                (int)Position.X,
                (int)Position.Y,
                (int)Size.X,
                (int)Size.Y),
            null,
            Color,
            Rotation,
            Vector2.Zero,        // top-left pivot (keeps layout consistent)
            SpriteEffects.None,
            LayerDepth
        );
    }
}
