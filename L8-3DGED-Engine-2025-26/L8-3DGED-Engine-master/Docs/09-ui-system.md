# UI System

## Overview

GDEngine's UI system uses component-based renderers drawn via SpriteBatch in the PostRender phase. UI components auto-register with `UIRenderSystem`.

---

## Setup

```csharp
// Add UI render system to scene
scene.Add(new UIRenderSystem());

// UI components on GameObjects will auto-register
```

---

## UI Components

### UIText

Renders text using a SpriteFont.

```csharp
var textGO = new GameObject("Title");
var text = textGO.AddComponent<UIText>();

text.Font = content.Load<SpriteFont>("Fonts/Arial");
text.Text = "Welcome";
text.Position = new Vector2(100, 50);
text.Color = Color.White;
text.Scale = 1.5f;
text.LayerDepth = 0.5f;  // 0 = front, 1 = back

scene.Add(textGO);
```

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `Font` | `SpriteFont` | Font to use |
| `Text` | `string` | Text content |
| `Position` | `Vector2` | Screen position |
| `Color` | `Color` | Text color |
| `Scale` | `float` | Size multiplier |
| `LayerDepth` | `float` | Draw order (0-1) |
| `Origin` | `Vector2` | Pivot point |

### UITexture

Renders a 2D texture/sprite.

```csharp
var iconGO = new GameObject("HealthIcon");
var icon = iconGO.AddComponent<UITexture>();

icon.Texture = content.Load<Texture2D>("UI/heart");
icon.Position = new Vector2(20, 20);
icon.Size = new Vector2(32, 32);
icon.Tint = Color.White;
icon.SourceRectangle = null;  // Use full texture

scene.Add(iconGO);
```

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `Texture` | `Texture2D` | Image to display |
| `Position` | `Vector2` | Screen position |
| `Size` | `Vector2` | Display size |
| `Tint` | `Color` | Color tint |
| `SourceRectangle` | `Rectangle?` | Portion of texture |
| `LayerDepth` | `float` | Draw order |
| `Origin` | `Vector2` | Pivot point |
| `Rotation` | `float` | Rotation in radians |

### UIButton

Interactive button with hover/click states.

```csharp
var buttonGO = new GameObject("StartButton");
var button = buttonGO.AddComponent<UIButton>();

button.Texture = buttonTexture;
button.Font = font;
button.Text = "Start Game";
button.Position = new Vector2(400, 300);
button.Size = new Vector2(200, 50);

button.NormalColor = Color.White;
button.HoverColor = Color.LightGray;
button.PressedColor = Color.Gray;
button.TextColor = Color.Black;

button.OnClick += () => StartGame();
button.OnHover += () => PlayHoverSound();

scene.Add(buttonGO);
```

**Events:**

| Event | Description |
|-------|-------------|
| `OnClick` | Button clicked |
| `OnHover` | Mouse entered |
| `OnExit` | Mouse exited |

### UISlider

Draggable slider for value selection.

```csharp
var sliderGO = new GameObject("VolumeSlider");
var slider = sliderGO.AddComponent<UISlider>();

slider.TrackTexture = trackTexture;
slider.HandleTexture = handleTexture;
slider.Position = new Vector2(100, 200);
slider.Size = new Vector2(200, 20);

slider.MinValue = 0f;
slider.MaxValue = 1f;
slider.Value = 0.8f;

slider.OnValueChanged += newValue => SetVolume(newValue);

scene.Add(sliderGO);
```

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `TrackTexture` | `Texture2D` | Background track |
| `HandleTexture` | `Texture2D` | Draggable handle |
| `MinValue` | `float` | Minimum value |
| `MaxValue` | `float` | Maximum value |
| `Value` | `float` | Current value |

### UIMenuPanel

Container that auto-layouts child elements.

```csharp
var menuGO = new GameObject("MainMenu");
var panel = menuGO.AddComponent<UIMenuPanel>();

panel.PanelPosition = new Vector2(100, 150);
panel.ItemSize = new Vector2(200, 40);
panel.VerticalSpacing = 10f;

// Add items (creates child GameObjects)
panel.AddButton("Play", buttonTex, font, OnPlayClick);
panel.AddButton("Options", buttonTex, font, OnOptionsClick);
panel.AddButton("Quit", buttonTex, font, OnQuitClick);
panel.AddSlider("Volume", trackTex, handleTex, font, 0, 1, 0.8f, OnVolumeChange);

panel.IsVisible = true;

scene.Add(menuGO);
```

### UIReticle

Centered crosshair.

```csharp
var hudGO = new GameObject("HUD");
var reticle = hudGO.AddComponent<UIReticle>();

reticle.Texture = crosshairTexture;
reticle.Size = new Vector2(32, 32);
reticle.Tint = Color.White;
// Automatically centers on screen

scene.Add(hudGO);
```

### UIDebugInfo

Debug text overlay showing system info.

```csharp
var debugGO = new GameObject("Debug");
var debug = debugGO.AddComponent<UIDebugInfo>();

debug.Font = debugFont;
debug.Position = new Vector2(10, 10);
debug.Color = Color.Yellow;

// Add providers (implement IShowDebugInfo)
debug.AddProvider(sceneManager);
debug.AddProvider(physicsSystem);
debug.AddProvider(orchestrationSystem);

scene.Add(debugGO);
```

---

## UILayer Constants

Use these for consistent layer ordering:

```csharp
public static class UILayer
{
    public const float Background = 1.0f;    // Furthest back
    public const float MenuBack = 0.9f;
    public const float MenuContent = 0.5f;
    public const float Overlay = 0.2f;
    public const float Tooltip = 0.1f;
    public const float Front = 0.0f;         // Closest
}

// Usage
text.LayerDepth = UILayer.MenuContent;
button.LayerDepth = UILayer.MenuContent;
tooltip.LayerDepth = UILayer.Tooltip;
```

---

## Screen Positioning

### Anchoring

```csharp
public static class ScreenAnchor
{
    public static Vector2 TopLeft => Vector2.Zero;
    public static Vector2 TopCenter => new Vector2(ScreenWidth / 2, 0);
    public static Vector2 TopRight => new Vector2(ScreenWidth, 0);
    public static Vector2 CenterLeft => new Vector2(0, ScreenHeight / 2);
    public static Vector2 Center => new Vector2(ScreenWidth / 2, ScreenHeight / 2);
    public static Vector2 CenterRight => new Vector2(ScreenWidth, ScreenHeight / 2);
    public static Vector2 BottomLeft => new Vector2(0, ScreenHeight);
    public static Vector2 BottomCenter => new Vector2(ScreenWidth / 2, ScreenHeight);
    public static Vector2 BottomRight => new Vector2(ScreenWidth, ScreenHeight);
}

// Center a button
button.Position = ScreenAnchor.Center - button.Size / 2;

// Top-right corner with margin
icon.Position = ScreenAnchor.TopRight - new Vector2(icon.Size.X + 10, -10);
```

### Responsive Layout

```csharp
public class ResponsiveUI : Component
{
    private UIText? _title;
    private int _lastWidth, _lastHeight;
    
    protected override void Update(float deltaTime)
    {
        var viewport = GraphicsDevice.Viewport;
        
        if (viewport.Width != _lastWidth || viewport.Height != _lastHeight)
        {
            _lastWidth = viewport.Width;
            _lastHeight = viewport.Height;
            RepositionElements();
        }
    }
    
    private void RepositionElements()
    {
        // Center title
        var titleSize = _title.Font.MeasureString(_title.Text);
        _title.Position = new Vector2(
            (_lastWidth - titleSize.X) / 2,
            50
        );
    }
}
```

---

## Visibility Control

### Show/Hide

```csharp
// Toggle visibility via Enabled
panel.Enabled = false;  // Hide
panel.Enabled = true;   // Show

// Or via custom property on panels
menuPanel.IsVisible = false;
```

### Fade In/Out

```csharp
public void FadeIn(UITexture element, float duration)
{
    element.Tint = new Color(255, 255, 255, 0);  // Transparent
    element.Enabled = true;
    
    orchestrator.Sequence()
        .TweenFloat(
            () => element.Tint.A / 255f,
            a => element.Tint = new Color(255, 255, 255, (byte)(a * 255)),
            1f,
            duration,
            Ease.SmoothStep
        )
        .Play();
}

public void FadeOut(UITexture element, float duration)
{
    orchestrator.Sequence()
        .TweenFloat(
            () => element.Tint.A / 255f,
            a => element.Tint = new Color(255, 255, 255, (byte)(a * 255)),
            0f,
            duration,
            Ease.SmoothStep
        )
        .OnComplete(() => element.Enabled = false)
        .Play();
}
```

---

## Common Patterns

### HUD Display

```csharp
public class HUD : Component
{
    private UIText? _healthText;
    private UIText? _scoreText;
    private UITexture? _healthBar;
    
    protected override void Start()
    {
        CreateHealthDisplay();
        CreateScoreDisplay();
        
        // Subscribe to updates
        var events = GameObject.Scene.Context.Events;
        events.On<HealthChangedEvent>().Do(OnHealthChanged);
        events.On<ScoreChangedEvent>().Do(OnScoreChanged);
    }
    
    private void OnHealthChanged(HealthChangedEvent evt)
    {
        _healthText.Text = $"HP: {evt.NewHealth:F0}";
        
        // Resize health bar
        float percent = evt.NewHealth / evt.MaxHealth;
        _healthBar.Size = new Vector2(200 * percent, 20);
    }
}
```

### Dialog Box

```csharp
public class DialogBox : Component
{
    private UITexture? _background;
    private UIText? _text;
    private UIText? _speakerName;
    
    private Queue<string> _lines = new();
    private bool _isShowing;
    
    public void Show(string speaker, params string[] lines)
    {
        _speakerName.Text = speaker;
        _lines = new Queue<string>(lines);
        _isShowing = true;
        
        ShowNextLine();
        
        _background.Enabled = true;
        _text.Enabled = true;
        _speakerName.Enabled = true;
    }
    
    public void Advance()
    {
        if (_lines.Count > 0)
            ShowNextLine();
        else
            Hide();
    }
    
    private void ShowNextLine()
    {
        _text.Text = _lines.Dequeue();
    }
    
    public void Hide()
    {
        _isShowing = false;
        _background.Enabled = false;
        _text.Enabled = false;
        _speakerName.Enabled = false;
    }
}
```

### Tooltip

```csharp
public class Tooltip : Component
{
    private UITexture? _background;
    private UIText? _text;
    
    public void Show(string message, Vector2 position)
    {
        _text.Text = message;
        
        // Measure and position
        var textSize = _text.Font.MeasureString(message);
        var padding = new Vector2(10, 5);
        
        _background.Position = position;
        _background.Size = textSize + padding * 2;
        _text.Position = position + padding;
        
        // Keep on screen
        ClampToScreen();
        
        _background.Enabled = true;
        _text.Enabled = true;
    }
    
    public void Hide()
    {
        _background.Enabled = false;
        _text.Enabled = false;
    }
}
```

### Inventory Grid

```csharp
public class InventoryUI : Component
{
    private const int Columns = 4;
    private const int SlotSize = 64;
    private const int Padding = 4;
    
    private List<UITexture> _slots = new();
    private List<UITexture> _items = new();
    
    public void Refresh(List<ItemData> inventory)
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            if (i < inventory.Count)
            {
                _items[i].Texture = inventory[i].Icon;
                _items[i].Enabled = true;
            }
            else
            {
                _items[i].Enabled = false;
            }
        }
    }
    
    private Vector2 GetSlotPosition(int index)
    {
        int row = index / Columns;
        int col = index % Columns;
        
        return new Vector2(
            col * (SlotSize + Padding),
            row * (SlotSize + Padding)
        );
    }
}
```

---

## IShowDebugInfo Interface

Implement to provide debug info to `UIDebugInfo`:

```csharp
public interface IShowDebugInfo
{
    IEnumerable<string> GetDebugLines();
}

public class MySystem : SystemBase, IShowDebugInfo
{
    public IEnumerable<string> GetDebugLines()
    {
        yield return $"MySystem: Active={_isActive}";
        yield return $"  Count={_items.Count}";
        yield return $"  State={_state}";
    }
}
```

---

## Performance Tips

### Minimize Text Changes

```csharp
// Bad - updates every frame
protected override void Update(float deltaTime)
{
    _scoreText.Text = $"Score: {_score}";  // String allocation every frame
}

// Good - update only when changed
private int _lastDisplayedScore;

protected override void Update(float deltaTime)
{
    if (_score != _lastDisplayedScore)
    {
        _lastDisplayedScore = _score;
        _scoreText.Text = $"Score: {_score}";
    }
}
```

### Disable Hidden Elements

```csharp
// Don't just move off-screen
element.Position = new Vector2(-1000, -1000);  // Still draws!

// Instead, disable
element.Enabled = false;  // Skips draw entirely
```

### Use Layer Depth Wisely

```csharp
// Elements at same depth may z-fight
// Use distinct depths for overlapping elements
background.LayerDepth = 0.9f;
content.LayerDepth = 0.5f;
overlay.LayerDepth = 0.1f;
```
