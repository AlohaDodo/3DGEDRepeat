# Lab: PerfStatsSystem — FPS Overlay (PostRender)

## Overview
In this lab you’ll add a tiny on-screen overlay that shows **FPS**, **average frame time (ms)**, and **total frame count** in the top-left corner. You’ll implement it as a **system** that runs in the **PostRender** phase of the frame lifecycle so it always draws **after** your normal scene rendering and stays visible on top.

Your engine dispatches systems by **FrameLifecycle** buckets. The **PostRender** bucket runs *after* the main render pass has finished but *before* the backbuffer is presented to the screen. This is the ideal place for debug overlays because:
- They won’t get occluded by scene geometry (they’re drawn last).
- They don’t interfere with depth/culling of normal world rendering.
- They can freely use `SpriteBatch` in screen space for crisp UI text.
- They’re easy to toggle on/off without touching gameplay or renderer code.

## Rationale
- You already compute per-frame timing via `Time.Update(gameTime)`. We’ll reuse `Time.UnscaledDeltaTimeSecs` and `Time.FrameCount` so the overlay doesn’t reinvent timing.
- Debug HUDs should render **after** the scene → use `FrameLifecycle.PostRender`.
- `Main` uses a `ContentDictionary<SpriteFont>` and an `EngineContext` that exposes a shared `SpriteBatch`. The overlay will **accept a `SpriteFont` in its constructor** (from your dictionary) and **pull `SpriteBatch` from the context** in `OnAdded()`.
- To avoid jumpy FPS numbers, we’ll smooth frame time using a small history buffer (see **Aside: CircularBuffer<T>**).

## Prerequisites
- A `SpriteFont` in your Content project, e.g. `assets/fonts/perfStats.spritefont`.
- `Main` already calls `Time.Update(gameTime)`, then `_scene.Update(Time.DeltaTimeSecs)` and `_scene.Draw(Time.DeltaTimeSecs)`.
- You have a `ContentDictionary<SpriteFont>` (e.g., `_fontDictionary`) and an `EngineContext` exposing `SpriteBatch`.

---

## Steps (edit the attached barebones `PerfStatsSystem`)

> You’ll start from the **barebones class** your repo provides and add **only** the minimal code to print FPS/ms/frame count. No extras yet.

### 1) Inherit from SystemBase
This class is a system and will need to be added to the `Scene` as a system so it needs to inherit from `SystemBase`. 

```csharp
public class PerfStatsSystem : SystemBase
{
    //...
}
```

### 2) Add private fields
Add these fields inside the class (follow your underscore naming style):

```csharp
// Fields
private readonly Vector2 _anchorPosition = new Vector2(10, 10);
private readonly SpriteFont _font;
private SpriteBatch _spriteBatch;

// Optional: tiny smoothing window (we'll describe the class in the aside)
private readonly GDEngine.Core.Collections.CircularBuffer<float> _recentDt =
    new GDEngine.Core.Collections.CircularBuffer<float>(60);
```

### 3) Ensure the constructor accepts a SpriteFont
Keep the constructor simple and inject the font (from your `ContentDictionary`):

```csharp
public PerfStatsSystem(SpriteFont font)
    : base(FrameLifecycle.PostRender, 10 /*some user-defined sort order*/)
{
    _font = font ?? throw new ArgumentNullException(nameof(font));
}
```

### 4) Fetch `SpriteBatch` in `OnAdded()`
Use the shared `SpriteBatch` from your `EngineContext` when the system is added:

```csharp
protected override void OnAdded()
{
    var ctx = Context ?? throw new InvalidOperationException("EngineContext not set.");
    _spriteBatch = ctx.SpriteBatch;
}
```

### 5) Implement `Draw(...)` to compute + render stats
Add a minimal `Draw` that computes smoothed FPS and renders one line:

```csharp
public override void Draw(float deltaTime)
{
    // Use unscaled delta so timescale changes don't affect FPS readout.
    float dt = MathF.Max(GDEngine.Core.Timing.Time.UnscaledDeltaTimeSecs, 1e-6f);
    _recentDt.Push(dt);

    // Simple average over the small window
    var arr = _recentDt.ToArray();
    float sum = 0f;
    for (int i = 0; i < arr.Length; i++) sum += arr[i];
    float avgDt = arr.Length > 0 ? sum / arr.Length : dt;

    float fps = avgDt > 0f ? 1f / avgDt : 0f;
    float ms = avgDt * 1000f;
    string text = $"FPS: {fps:0.0}  |  {ms:0.00} ms  |  Frames: {GDEngine.Core.Timing.Time.FrameCount}";

    _spriteBatch.Begin();
    _spriteBatch.DrawString(_font, text, _anchorPosition, Microsoft.Xna.Framework.Color.Yellow);
    _spriteBatch.End();
}
```

### 6) Wire it in `Main.InitializeSystems()`
After your main systems are set up, add:

```csharp
var debugFont = _fontDictionary.Get("perfStats");
_scene.Add(new PerfStatsSystem(debugFont));
```

That’s the **entire minimal version**.

---

## Run & Verify
- Build & run.
- You should see a yellow line in the top-left, like:  
  `FPS: 60.0  |  16.67 ms  |  Frames: 12345`

---

## Aside: About `CircularBuffer<T>` (and simple alternatives)
We use a `CircularBuffer<float>(60)` to keep the **last ~60 frame times**. Each frame we push the newest `dt` and pop the oldest automatically. Averaging over this small window smooths out spikes so the FPS number is **stable** but still **responsive**.

**Key ideas:**
- It’s a fixed-size ring: when it fills, new pushes overwrite the oldest value.
- Reads are `O(n)` only for our tiny window when we compute an average.
- This avoids dynamic allocations common with List insert/removes per frame.

**If you dont want to use the CircularBuffer, here is easy substitutes:**

**Small fixed array + rolling index** — conceptually similar to a ring:
   ```csharp
   private readonly float[] _samples = new float[60];
   private int _idx = 0, _count = 0;

   // in Draw():
   _samples[_idx] = dt;
   _idx = (_idx + 1) % _samples.Length;
   if (_count < _samples.Length) _count++;

   float sum = 0f;
   for (int i = 0; i < _count; i++) sum += _samples[i];
   float avgDt = sum / _count;
   ```

You can swap this code into `Draw` without changing the rest of the system.

---

## Aside (Add-on Feature, implement **after** the minimal version works): Extra lines via `Func<IEnumerable<string>>`

This optional feature lets you pass a function that supplies extra text lines each frame. Follow these **small, ordered edits** to your current `PerfStatsSystem`:

**Step 1 — Add a field** (near your other fields):
```csharp
private readonly Func<IEnumerable<string>>? _linesProvider;
```

**Step 2 — Change the constructor signature** to accept it (default `null`) and assign it:
```csharp
public PerfStatsSystem(SpriteFont font, Func<IEnumerable<string>>? linesProvider = null)
    : base(FrameLifecycle.PostRender, order: 10)
{
    _font = font ?? throw new ArgumentNullException(nameof(font));
    _linesProvider = linesProvider;
}
```

**Step 3 — Append the extra lines in `Draw(...)`** after drawing the header line:
```csharp
float y = _anchorPosition.Y + _font.LineSpacing + 2f;
if (_linesProvider != null)
{
    foreach (var line in _linesProvider())
    {
        _spriteBatch.DrawString(_font, line, new Vector2(_anchorPosition.X, y), Microsoft.Xna.Framework.Color.Yellow);
        y += _font.LineSpacing;
    }
}
```

**Step 4 — Usage example (fixed strings):**
```csharp
var debugFont = _fontDictionary.Get("perfStats");
string[] _debugLines = new[]
{
    "Renderer: Forward",
    "Camera: Main",
    "Build: Dev"
};
_scene.Add(new PerfStatsSystem(debugFont, () => _debugLines));
```

**Step 5 — Usage example (dynamic strings per frame):**
```csharp
var debugFont = _fontDictionary.Get("perfStats");
_scene.Add(new PerfStatsSystem(debugFont, () =>
{
    return new[]
    {
        $"Camera Info:",
        $" - Game Object Count: {_scene.GameObjects.Count}",
        $" - Camera[Position]: {_cameraGO.Transform.Position}",
        $" - Camera[Forward]: {_cameraGO.Transform.Forward}"
    };
}));
```

---

## Suggested Improvements 
- **Toggle key:** Add a public `Enabled` flag on `PerfStats` and flip it via your `InputSystem` (e.g., `F1` to show/hide).
- **Text shadow for contrast:** Draw a black shadow at `(x+1, y+1)` before the yellow text.
- **Color by performance:** Green (>55 FPS), Orange (30–55), Red (<30).
- **Panel background:** Use a 1×1 white texture tinted with low alpha to draw a translucent rectangle behind the text.
- **More metrics later:** Min/Max/Avg over N seconds; draw counts (renderers, triangles), simple memory via `GC.GetTotalMemory(false)`.
