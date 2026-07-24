#nullable enable
using System;
using System.Collections.Generic;
using GDEngine.Core.Collections;
using GDEngine.Core.Enums;
using GDEngine.Core.Systems;
using GDEngine.Core.Timing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GDGame.Demos
{
    public sealed class PerfStatsSystem : SystemBase
    {
        #region Fields
        private Vector2 _anchorPosition = new Vector2(5, 5);
        private Color _colorDropShadow = Color.Black;
        private Color _colorText = Color.Yellow;
        private Func<IEnumerable<string>>? _linesProvider;
        private readonly SpriteFont? _font;
        private SpriteBatch? _spriteBatch;

        // Smoothing window (we'll describe the class in the aside)
        private readonly CircularBuffer<float> _recentDt = new CircularBuffer<float>(60);

        #region Properties
        public Vector2 AnchorPosition { get => _anchorPosition; set => _anchorPosition = value; }
        public Color ColorDropShadow { get => _colorDropShadow; set => _colorDropShadow = value; }
        public Color ColorText { get => _colorText; set => _colorText = value; } 
        #endregion
        #endregion

        #region Constructors
        public PerfStatsSystem(SpriteFont font, Func<IEnumerable<string>>? linesProvider = null)
           : base(FrameLifecycle.PostRender, order: 10)
        {
            _font = font ?? throw new ArgumentNullException(nameof(font));
            _linesProvider = linesProvider;
        }
        #endregion

        #region Lifecycle methods
        protected override void OnAdded()
        {
            var ctx = Context ?? throw new InvalidOperationException("EngineContext not set.");
            _spriteBatch = ctx.SpriteBatch;
        }

        public override void Draw(float deltaTime)
        {
            if (_spriteBatch == null)
                throw new NullReferenceException(nameof(_spriteBatch));

            if (_font == null)
                throw new NullReferenceException(nameof(_font));

            // Use unscaled delta so timescale changes don't affect FPS readout.
            float dt = MathF.Max(Time.UnscaledDeltaTimeSecs, 1e-6f); //16.67ms
            _recentDt.Push(dt);

            // Simple average over the small window
            var arr = _recentDt.ToArray();
            float sum = 0f;
            for (int i = 0; i < arr.Length; i++) sum += arr[i];
            float avgDt = arr.Length > 0 ? sum / arr.Length : dt;

            float fps = avgDt > 0f ? 1f / avgDt : 0f;
            float ms = avgDt * 1000f;
            string text = $"FPS: {fps:0.0}  |  {ms:0.00} ms  |  Frames: {Time.FrameCount}";

            _spriteBatch.Begin();

            _spriteBatch.DrawString(_font, text, _anchorPosition + new Vector2(2, 2), _colorDropShadow);
            _spriteBatch.DrawString(_font, text, _anchorPosition, _colorText);

            float y = _anchorPosition.Y + _font.LineSpacing + 5f;
            if (_linesProvider != null)
            {
                foreach (var line in _linesProvider())
                {
                    _spriteBatch.DrawString(_font, line, new Vector2(_anchorPosition.X, y) + new Vector2(2, 2), _colorDropShadow);
                    _spriteBatch.DrawString(_font, line, new Vector2(_anchorPosition.X, y), _colorText);
                    y += _font.LineSpacing;
                }
            }
            _spriteBatch.End();


        } 
        #endregion
    }
}
