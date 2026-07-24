#nullable enable
using GDEngine.Core.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace GDEngine.Core.Rendering.UI
{
    public class UIReticuleRenderer : UIRenderer
    {
        private Texture2D? _texture;
        private SpriteFont? _font;
        private Vector2 _offset;
        private float _rotation;

        public Texture2D? Texture { get => _texture; set => _texture = value; }
        public SpriteFont? Font { get => _font; set => _font = value; }
        public Vector2 Offset { get => _offset; set => _offset = value; }

        protected override void Awake()
        {
            base.Awake();
            // Get ref to draw textures and strings
            _spriteBatch = GameObject?.Scene?.Context.SpriteBatch;

            

        }
        public override void Draw(GraphicsDevice device, Camera? camera)
        {
            if (_spriteBatch == null)
                throw new NullReferenceException(nameof(_spriteBatch));

            if(_texture == null)
                throw new NullReferenceException(nameof(_texture));

            _spriteBatch.Begin(
                SpriteSortMode.FrontToBack,
                BlendState.AlphaBlend,         
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone);

            _rotation +=1;

            var mousePosition = Mouse.GetState().Position.ToVector2();
            _spriteBatch.DrawString(_font, "Dist[3]", mousePosition + _offset, Color.Black);
            _spriteBatch.Draw(_texture, mousePosition, null,
                Color.White, MathHelper.ToRadians(_rotation),
                new Vector2(_texture.Width/2, _texture.Height/2), 
                6, SpriteEffects.None, 0);
            _spriteBatch.End();
        }
    }
}
