# Lab: UIRenderer Components - Mouse Reticle + Text 

## Overview
In this lab you will **implement two UI components from starter stubs** and wire them in `Main`:
- **`UIReticleRenderer`** — a rotating mouse reticle sprite (supports atlas frames).
- **`UITextRenderer`** — a generic text renderer driven by **delegates** for text, position, and color.

You will start from the attached **stub files** and progressively add code until both components render correctly, then finish by adding the **mouse‑locked HUD** in `Main`.

## Rationale
- Debug/HUD elements should render **after** the 3D scene so they are not occluded: we render with `SpriteBatch` in screen space using **alpha blending** and **no depth**.
- Splitting **reticle** and **text** keeps things **modular**: you can reuse `UITextRenderer` for FPS, ammo, tooltips, etc.
- Using **delegates** (`Func<string>`, `Func<Vector2>`, `Func<Color>`) makes the text renderer **data‑driven** and easy to bind to live values without tight coupling.
- The reticle uses `Time.DeltaTimeSecs` for **framerate‑independent rotation**, so behavior is stable at 60/144/240Hz.

## Prerequisites
- The project already provides a `SpriteBatch` via the engine context and a `UIRenderSystem` that runs post‑render.
- Content keys exist:
  - Font: `"mouse_reticle_font"`
  - Texture: `"Crosshair_21"`
- A waypoint GameObject named **`"test crate textured cube"`** and a camera named **`"First person camera"`** exist in your scene.
- Starter stubs are provided:
  - `GDEngine/Core/Rendering/UI/UIReticleRenderer.cs` (empty class)
  - `GDEngine/Core/Rendering/UI/UITextRenderer.cs` (empty class)

---

# Part A — Implement `UIReticleRenderer` from the stub

> Open **`UIReticleRenderer.cs`**. You’ll turn the empty class into a working component that draws a rotating sprite at the mouse position, with optional **atlas frame** support.

## A1) Class signature + static render states
Add the inheritance and reusable render states at the top of the class.

```csharp
using GDEngine.Core.Components;
using GDEngine.Core.Timing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GDEngine.Core.Rendering.UI
{
    /// <summary>
    /// Draws a rotating reticle sprite at the mouse position (with optional offset/scale),
    /// supporting texture atlases via <see cref="SourceRectangle"/>.
    /// </summary>
    /// <see cref="UIRenderer"/>
    public class UIReticleRenderer : UIRenderer
    {
        #region Static Fields
        private static readonly RasterizerState _raster = RasterizerState.CullNone;
        private static readonly DepthStencilState _depth = DepthStencilState.None;
        private static readonly BlendState _blend = BlendState.AlphaBlend;       // premultiplied alpha
        private static readonly SamplerState _sampler = SamplerState.PointClamp; // crisp HUD pixels
        #endregion
```

## A2) Fields + properties
Add fields for sprite data and public properties to tweak behavior.

```csharp
        #region Fields
        private SpriteBatch _spriteBatch;
        private Texture2D _texture;
        private Rectangle? _sourceRect;      // atlas frame (null = full texture)
        private Vector2 _origin;
        private Vector2 _scale = Vector2.One;
        private Vector2 _offset = Vector2.Zero;
        private float _rotationRad;
        private float _rotationSpeedDegPerSec = 90f;
        private float _layerDepth = 0f;
        private Color _tint = Color.White;
        #endregion

        #region Properties
        public Texture2D Texture { get => _texture; set { _texture = value; RecenterOriginFromSource(); } }
        public Rectangle? SourceRectangle { get => _sourceRect; set { _sourceRect = value; RecenterOriginFromSource(); } }
        public Vector2 Scale { get => _scale; set => _scale = value; }
        public Vector2 Offset { get => _offset; set => _offset = value; }
        public float RotationSpeedDegPerSec { get => _rotationSpeedDegPerSec; set => _rotationSpeedDegPerSec = value; }
        public float LayerDepth { get => _layerDepth; set => _layerDepth = MathHelper.Clamp(value, 0f, 1f); }
        public Color Tint { get => _tint; set => _tint = value; }
        #endregion
```

## A3) Constructors + helper
```csharp
        #region Constructors
        public UIReticleRenderer(Texture2D texture, Rectangle? source = null)
        {
            _texture = texture;
            _sourceRect = source;
            RecenterOriginFromSource();
        }
        #endregion

        #region Methods
        public void RecenterOriginFromSource()
        {
            if (_texture == null) return;
            if (_sourceRect.HasValue)
            {
                var r = _sourceRect.Value;
                _origin = new Vector2(r.Width * 0.5f, r.Height * 0.5f);
            }
            else
            {
                _origin = new Vector2(_texture.Width * 0.5f, _texture.Height * 0.5f);
            }
        }
        #endregion
```

## A4) Lifecycle: `Awake` + `Render`
```csharp
        #region Lifecycle Methods
        protected override void Awake()
        {
            base.Awake();
            _spriteBatch = GameObject.Scene.Context.SpriteBatch;
        }

        public override void Render(GraphicsDevice device, Camera camera)
        {
            if (_spriteBatch == null || _texture == null) return;

            // rotate at a constant angular velocity (deg/sec * dt)
            _rotationRad += MathHelper.ToRadians(_rotationSpeedDegPerSec) * Time.DeltaTimeSecs;

            var mouse = Mouse.GetState().Position.ToVector2();
            var pos = mouse + _offset;

            _spriteBatch.Begin(SpriteSortMode.Deferred, _blend, _sampler, _depth, _raster);
            _spriteBatch.Draw(_texture, pos, _sourceRect, _tint, _rotationRad, _origin, _scale, SpriteEffects.None, _layerDepth);
            _spriteBatch.End();
        }
        #endregion
    }
}
```

## **Checkpoint A**  
- Reticle appears at the mouse and rotates smoothly.  
- Changing `Scale`, `Tint`, or `SourceRectangle` works as expected.

---

# Part B — Implement `UITextRenderer` from the stub

> Open **`UITextRenderer.cs`**. You’ll implement a **generic** text component that gets its **text**, **position**, and optionally **color** from delegates each frame.

## B1) Class signature + static render states
```csharp
using GDEngine.Core.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GDEngine.Core.Rendering.UI
{
    /// <summary>
    /// Generic UI text renderer that draws a string at a position supplied by a delegate.
    /// Use it for mouse-locked labels, fixed HUD text, or dynamically positioned UI.
    /// </summary>
    /// <see cref="UIReticleRenderer"/>
    public class UITextRenderer : UIRenderer
    {
        #region Static Fields
        private static readonly RasterizerState _raster = RasterizerState.CullNone;
        private static readonly DepthStencilState _depth = DepthStencilState.None;
        private static readonly BlendState _blend = BlendState.AlphaBlend;
        private static readonly SamplerState _sampler = SamplerState.PointClamp;
        private static readonly Vector2 _shadowNudge = new Vector2(1f, 1f);
        #endregion
```

## B2) Fields + properties
```csharp
        #region Fields
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;

        private Func<string> _textProvider = () => string.Empty;
        private Func<Vector2> _positionProvider = () => Vector2.Zero;
        private Func<Color> _colorProvider = null;

        private Vector2 _offset = Vector2.Zero;
        private float _scale = 1f;
        private float _layerDepth = 0f;
        private bool _dropShadow = true;
        private Color _fallbackColor = Color.White;
        private Color _shadowColor = new Color(0, 0, 0, 180);
        private TextAnchor _anchor = TextAnchor.TopLeft;
        #endregion

        #region Properties
        public SpriteFont Font { get => _font; set => _font = value; }
        public Func<string> TextProvider { get => _textProvider; set => _textProvider = value ?? (() => string.Empty); }
        public Func<Vector2> PositionProvider { get => _positionProvider; set => _positionProvider = value ?? (() => Vector2.Zero); }
        public Func<Color> ColorProvider { get => _colorProvider; set => _colorProvider = value; }
        public Vector2 Offset { get => _offset; set => _offset = value; }
        public float Scale { get => _scale; set => _scale = Math.Max(0.01f, value); }
        public float LayerDepth { get => _layerDepth; set => _layerDepth = MathHelper.Clamp(value, 0f, 1f); }
        public bool DropShadow { get => _dropShadow; set => _dropShadow = value; }
        public Color FallbackColor { get => _fallbackColor; set => _fallbackColor = value; }
        public Color ShadowColor { get => _shadowColor; set => _shadowColor = value; }
        public TextAnchor Anchor { get => _anchor; set => _anchor = value; }
        #endregion
```

## B3) Constructors + anchor helper
```csharp
        #region Constructors
        public UITextRenderer(SpriteFont font) { _font = font; }

        public UITextRenderer(SpriteFont font, string text, Vector2 position)
        {
            _font = font;
            _textProvider = () => text ?? string.Empty;
            _positionProvider = () => position;
        }

        public static UITextRenderer FromMouse(SpriteFont font, string text)
        {
            return new UITextRenderer(font)
            {
                _textProvider = () => text ?? string.Empty,
                _positionProvider = () => Mouse.GetState().Position.ToVector2()
            };
        }
        #endregion

        #region Methods
        public static Vector2 ComputeAnchorOffset(Vector2 size, TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.TopLeft:    return Vector2.Zero;
                case TextAnchor.Top:        return new Vector2(size.X * 0.5f, 0);
                case TextAnchor.TopRight:   return new Vector2(size.X, 0);
                case TextAnchor.Left:       return new Vector2(0, size.Y * 0.5f);
                case TextAnchor.Center:     return size * 0.5f;
                case TextAnchor.Right:      return new Vector2(size.X, size.Y * 0.5f);
                case TextAnchor.BottomLeft: return new Vector2(0, size.Y);
                case TextAnchor.Bottom:     return new Vector2(size.X * 0.5f, size.Y);
                default:                    return new Vector2(size.X, size.Y); // BottomRight
            }
        }
        #endregion
```

## B4) Lifecycle: `Awake` + `Render`
```csharp
        #region Lifecycle Methods
        protected override void Awake()
        {
            base.Awake();
            _spriteBatch = GameObject.Scene.Context.SpriteBatch;
        }

        public override void Render(GraphicsDevice device, Camera camera)
        {
            if (_spriteBatch == null || _font == null) return;

            var text = _textProvider?.Invoke() ?? string.Empty;
            if (text.Length == 0) return;

            var basePos = _positionProvider?.Invoke() ?? Vector2.Zero;
            var size = _font.MeasureString(text) * _scale;
            var anchorOff = ComputeAnchorOffset(size, _anchor);
            var drawPos = basePos + _offset - anchorOff;

            var color = _colorProvider != null ? _colorProvider() : _fallbackColor;

            _spriteBatch.Begin(SpriteSortMode.Deferred, _blend, _sampler, _depth, _raster);
            if (_dropShadow)
                _spriteBatch.DrawString(_font, text, drawPos + _shadowNudge, _shadowColor, 0f, Vector2.Zero, _scale, SpriteEffects.None, _layerDepth);
            _spriteBatch.DrawString(_font, text, drawPos, color, 0f, Vector2.Zero, _scale, SpriteEffects.None, _layerDepth);
            _spriteBatch.End();
        }
        #endregion
    }

    /// <summary>Anchor positions for <see cref="UITextRenderer"/>.</summary>
    public enum TextAnchor
    {
        TopLeft, Top, TopRight,
        Left, Center, Right,
        BottomLeft, Bottom, BottomRight
    }
}
```

## **Checkpoint B**  
- Single‑line and multi‑line strings render correctly.  
- Changing `Anchor`, `Offset`, and `Scale` positions the text as expected.

---

# Part C — Wire both components in `Main`

> Implement **`InitializeMouseReticleRenderer()`** so the reticle follows the mouse and a two‑line HUD shows distance to a waypoint and a changing health value.

## C1) Setup and assets
```csharp
var uiGO = new GameObject("HUD");
var reticleAtlas = _textureDictionary.Get("Crosshair_21");
var uiFont       = _fontDictionary.Get("mouse_reticle_font");
```

## C2) Reticle
```csharp
var reticle = new UIReticleRenderer(reticleAtlas);
reticle.SourceRectangle = null;           // or pick an atlas frame Rectangle(x,y,w,h)
reticle.Scale = new Vector2(0.1f, 0.1f);
reticle.RotationSpeedDegPerSec = 45;
uiGO.AddComponent(reticle);
```

## C3) Text with a lines provider
```csharp
var waypointObject = _scene.Find(go => go.Name.Equals("test crate textured cube"));
var cameraObject   = _scene.Find(go => go.Name.Equals("First person camera"));

Func<IEnumerable<string>> linesProvider = () =>
{
    var dist = Vector3.Distance(cameraObject.Transform.Position, waypointObject.Transform.Position);
    var hp   = _dummyHealth; // demo value updated in Update()
    return new[] { $"Dist: {dist:F1} m", $"Health:   {hp}" };
};

var text = new UITextRenderer(uiFont);
text.PositionProvider = () => Mouse.GetState().Position.ToVector2();
text.Anchor           = TextAnchor.Center;
text.Offset           = new Vector2(0, 50);
text.FallbackColor    = Color.White;
text.DropShadow       = true;
text.ShadowColor      = Color.Black;
text.TextProvider     = () => string.Join("\n", linesProvider());

uiGO.AddComponent(text);

_scene.Add(uiGO);
IsMouseVisible = false;
```

## **Checkpoint C**  
- Reticle rotates and follows the mouse.  
- Two text lines follow the mouse and update every frame.

---

## Troubleshooting
- **Nothing draws:** ensure your `UIRenderSystem` executes *after* the main renderer and that `SpriteBatch.Begin/End` are balanced.
- **Dark edges on PNG:** verify premultiplied textures + `BlendState.AlphaBlend`.
- **Null reference on names:** confirm the GameObject names match exactly.

---

# Appendix: Lab Solutions 

## `UIReticleRenderer.cs`
```csharp
using GDEngine.Core.Components;
using GDEngine.Core.Timing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GDEngine.Core.Rendering.UI
{
    /// <summary>
    /// Draws a rotating reticle sprite at the mouse position (with optional offset/scale),
    /// supporting texture atlases via <see cref="SourceRectangle"/>.
    /// </summary>
    /// <see cref="UIRenderer"/>
    public class UIReticleRenderer : UIRenderer
    {
        #region Static Fields
        private static readonly RasterizerState _raster = RasterizerState.CullNone;
        private static readonly DepthStencilState _depth = DepthStencilState.None;
        private static readonly BlendState _blend = BlendState.AlphaBlend;
        private static readonly SamplerState _sampler = SamplerState.PointClamp;
        #endregion

        #region Fields
        private SpriteBatch _spriteBatch;
        private Texture2D _texture;
        private Rectangle? _sourceRect;
        private Vector2 _origin;
        private Vector2 _scale = Vector2.One;
        private Vector2 _offset = Vector2.Zero;
        private float _rotationRad;
        private float _rotationSpeedDegPerSec = 90f;
        private float _layerDepth = 0f;
        private Color _tint = Color.White;
        #endregion

        #region Properties
        public Texture2D Texture { get => _texture; set { _texture = value; RecenterOriginFromSource(); } }
        public Rectangle? SourceRectangle { get => _sourceRect; set { _sourceRect = value; RecenterOriginFromSource(); } }
        public Vector2 Scale { get => _scale; set => _scale = value; }
        public Vector2 Offset { get => _offset; set => _offset = value; }
        public float RotationSpeedDegPerSec { get => _rotationSpeedDegPerSec; set => _rotationSpeedDegPerSec = value; }
        public float LayerDepth { get => _layerDepth; set => _layerDepth = MathHelper.Clamp(value, 0f, 1f); }
        public Color Tint { get => _tint; set => _tint = value; }
        #endregion

        #region Constructors
        public UIReticleRenderer(Texture2D texture, Rectangle? source = null)
        {
            _texture = texture;
            _sourceRect = source;
            RecenterOriginFromSource();
        }
        #endregion

        #region Methods
        public void RecenterOriginFromSource()
        {
            if (_texture == null) return;
            if (_sourceRect.HasValue)
            {
                var r = _sourceRect.Value;
                _origin = new Vector2(r.Width * 0.5f, r.Height * 0.5f);
            }
            else
            {
                _origin = new Vector2(_texture.Width * 0.5f, _texture.Height * 0.5f);
            }
        }
        #endregion

        #region Lifecycle Methods
        protected override void Awake()
        {
            base.Awake();
            _spriteBatch = GameObject.Scene.Context.SpriteBatch;
        }

        public override void Render(GraphicsDevice device, Camera camera)
        {
            if (_spriteBatch == null || _texture == null) return;
            _rotationRad += MathHelper.ToRadians(_rotationSpeedDegPerSec) * Time.DeltaTimeSecs;

            var mouse = Mouse.GetState().Position.ToVector2();
            var pos = mouse + _offset;

            _spriteBatch.Begin(SpriteSortMode.Deferred, _blend, _sampler, _depth, _raster);
            _spriteBatch.Draw(_texture, pos, _sourceRect, _tint, _rotationRad, _origin, _scale, SpriteEffects.None, _layerDepth);
            _spriteBatch.End();
        }
        #endregion
    }
}
```

## `UITextRenderer.cs`
```csharp
using GDEngine.Core.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GDEngine.Core.Rendering.UI
{
    /// <summary>
    /// Generic UI text renderer that draws a string at a position supplied by a delegate.
    /// Use it for mouse-locked labels, fixed HUD text, or dynamically positioned UI.
    /// </summary>
    /// <see cref="UIReticleRenderer"/>
    public class UITextRenderer : UIRenderer
    {
        #region Static Fields
        private static readonly RasterizerState _raster = RasterizerState.CullNone;
        private static readonly DepthStencilState _depth = DepthStencilState.None;
        private static readonly BlendState _blend = BlendState.AlphaBlend;
        private static readonly SamplerState _sampler = SamplerState.PointClamp;
        private static readonly Vector2 _shadowNudge = new Vector2(1f, 1f);
        #endregion

        #region Fields
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;

        private Func<string> _textProvider = () => string.Empty;
        private Func<Vector2> _positionProvider = () => Vector2.Zero;
        private Func<Color> _colorProvider = null;

        private Vector2 _offset = Vector2.Zero;
        private float _scale = 1f;
        private float _layerDepth = 0f;
        private bool _dropShadow = true;
        private Color _fallbackColor = Color.White;
        private Color _shadowColor = new Color(0, 0, 0, 180);
        private TextAnchor _anchor = TextAnchor.TopLeft;
        #endregion

        #region Properties
        public SpriteFont Font { get => _font; set => _font = value; }
        public Func<string> TextProvider { get => _textProvider; set => _textProvider = value ?? (() => string.Empty); }
        public Func<Vector2> PositionProvider { get => _positionProvider; set => _positionProvider = value ?? (() => Vector2.Zero); }
        public Func<Color> ColorProvider { get => _colorProvider; set => _colorProvider = value; }
        public Vector2 Offset { get => _offset; set => _offset = value; }
        public float Scale { get => _scale; set => _scale = Math.Max(0.01f, value); }
        public float LayerDepth { get => _layerDepth; set => _layerDepth = MathHelper.Clamp(value, 0f, 1f); }
        public bool DropShadow { get => _dropShadow; set => _dropShadow = value; }
        public Color FallbackColor { get => _fallbackColor; set => _fallbackColor = value; }
        public Color ShadowColor { get => _shadowColor; set => _shadowColor = value; }
        public TextAnchor Anchor { get => _anchor; set => _anchor = value; }
        #endregion

        #region Constructors
        public UITextRenderer(SpriteFont font) { _font = font; }

        public UITextRenderer(SpriteFont font, string text, Vector2 position)
        {
            _font = font;
            _textProvider = () => text ?? string.Empty;
            _positionProvider = () => position;
        }

        public static UITextRenderer FromMouse(SpriteFont font, string text)
        {
            return new UITextRenderer(font)
            {
                _textProvider = () => text ?? string.Empty,
                _positionProvider = () => Mouse.GetState().Position.ToVector2()
            };
        }
        #endregion

        #region Methods
        public static Vector2 ComputeAnchorOffset(Vector2 size, TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.TopLeft:    return Vector2.Zero;
                case TextAnchor.Top:        return new Vector2(size.X * 0.5f, 0);
                case TextAnchor.TopRight:   return new Vector2(size.X, 0);
                case TextAnchor.Left:       return new Vector2(0, size.Y * 0.5f);
                case TextAnchor.Center:     return size * 0.5f;
                case TextAnchor.Right:      return new Vector2(size.X, size.Y * 0.5f);
                case TextAnchor.BottomLeft: return new Vector2(0, size.Y);
                case TextAnchor.Bottom:     return new Vector2(size.X * 0.5f, size.Y);
                default:                    return new Vector2(size.X, size.Y); // BottomRight
            }
        }
        #endregion

        #region Lifecycle Methods
        protected override void Awake()
        {
            base.Awake();
            _spriteBatch = GameObject.Scene.Context.SpriteBatch;
        }

        public override void Render(GraphicsDevice device, Camera camera)
        {
            if (_spriteBatch == null || _font == null) return;

            var text = _textProvider?.Invoke() ?? string.Empty;
            if (text.Length == 0) return;

            var basePos = _positionProvider?.Invoke() ?? Vector2.Zero;
            var size = _font.MeasureString(text) * _scale;
            var anchorOff = ComputeAnchorOffset(size, _anchor);
            var drawPos = basePos + _offset - anchorOff;

            var color = _colorProvider != null ? _colorProvider() : _fallbackColor;

            _spriteBatch.Begin(SpriteSortMode.Deferred, _blend, _sampler, _depth, _raster);
            if (_dropShadow)
                _spriteBatch.DrawString(_font, text, drawPos + _shadowNudge, _shadowColor, 0f, Vector2.Zero, _scale, SpriteEffects.None, _layerDepth);
            _spriteBatch.DrawString(_font, text, drawPos, color, 0f, Vector2.Zero, _scale, SpriteEffects.None, _layerDepth);
            _spriteBatch.End();
        }
        #endregion
    }

    public enum TextAnchor
    {
        TopLeft, Top, TopRight,
        Left, Center, Right,
        BottomLeft, Bottom, BottomRight
    }
}
```

## `Main::InitializeMouseReticleRenderer()` 
```csharp
private void InitializeMouseReticleRenderer()
{
    var uiGO = new GameObject("HUD");

    var reticleAtlas = _textureDictionary.Get("Crosshair_21");
    var uiFont       = _fontDictionary.Get("mouse_reticle_font");

    var reticle = new UIReticleRenderer(reticleAtlas);
    reticle.SourceRectangle = null;
    reticle.Scale = new Vector2(0.1f, 0.1f);
    reticle.RotationSpeedDegPerSec = 45;
    uiGO.AddComponent(reticle);

    var waypointObject = _scene.Find((go) => go.Name.Equals("test crate textured cube"));
    var cameraObject   = _scene.Find(go => go.Name.Equals("First person camera"));

    Func<IEnumerable<string>> linesProvider = () =>
    {
        var distToWaypoint = Vector3.Distance(
            cameraObject.Transform.Position,
            waypointObject.Transform.Position);
        var hp = _dummyHealth;
        return new[]
        {
            $"Dist: {distToWaypoint:F1} m",
            $"Health:   {hp}"
        };
    };

    var text = new UITextRenderer(uiFont);
    text.PositionProvider = () => Mouse.GetState().Position.ToVector2();
    text.Anchor           = TextAnchor.Center;
    text.Offset           = new Vector2(0, 50);
    text.FallbackColor    = Color.White;
    text.DropShadow       = true;
    text.ShadowColor      = Color.Black;
    text.TextProvider     = () => string.Join("\n", linesProvider());

    uiGO.AddComponent(text);
    _scene.Add(uiGO);
    IsMouseVisible = false;
}
```

---

**You’re done.** Students start from the stubs, complete Parts A and B step‑by‑step, then finish Part C to run the feature end‑to‑end.
