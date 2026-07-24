# Events and Messaging

## Overview

GDEngine provides multiple messaging patterns for communication between systems and components:

| Pattern | Use Case | Coupling | Timing |
|---------|----------|----------|--------|
| **EventBus** | Decoupled system communication | Loose | Queued (once per frame) |
| **ImpulseBus** | Transient effects (shake, flash) | Loose | Immediate or continuous |
| **C# Events** | Direct component communication | Tight | Immediate |
| **Transform.Changed** | React to spatial changes | Medium | Immediate |

---

## EventBus

The `EventBus` enables decoupled, type-safe event communication. Events are queued when published and dispatched together by `EventSystem`.

### Accessing the EventBus

```csharp
// From EngineContext
var bus = context.Events;

// From a component
var bus = GameObject.Scene.Context.Events;

// From a system
var bus = Context.Events;
```

### Defining Events

Events are simple classes carrying data:

```csharp
// Simple event (no data)
public class GameStartedEvent { }

// Event with data
public class DamageEvent
{
    public GameObject Target { get; }
    public float Amount { get; }
    public Vector3 HitPoint { get; }
    
    public DamageEvent(GameObject target, float amount, Vector3 hitPoint)
    {
        Target = target;
        Amount = amount;
        HitPoint = hitPoint;
    }
}

// Event with computed properties
public class ItemCollectedEvent
{
    public string ItemId { get; }
    public int NewTotal { get; }
    public bool IsRareItem => ItemId.StartsWith("rare_");
    
    public ItemCollectedEvent(string itemId, int newTotal)
    {
        ItemId = itemId;
        NewTotal = newTotal;
    }
}
```

### Publishing Events

```csharp
// Publish an event (queued until next EventSystem.Update)
bus.Publish(new DamageEvent(target, 25f, hitPoint));

// Publish simple event
bus.Publish(new GameStartedEvent());
```

### Subscribing to Events

```csharp
// Basic subscription
IDisposable subscription = bus.On<DamageEvent>()
    .Do(evt => HandleDamage(evt));

// With priority (higher = earlier)
bus.On<DamageEvent>()
    .WithPriority(100)
    .Do(HandleDamage);

// Using priority presets
bus.On<DamageEvent>()
    .WithPriorityPreset(EventPriority.Systems)  // High priority
    .Do(HandleDamage);

bus.On<DamageEvent>()
    .WithPriorityPreset(EventPriority.Gameplay) // Normal priority
    .Do(HandleDamage);
```

### Unsubscribing

```csharp
public class HealthComponent : Component
{
    private IDisposable? _damageSubscription;
    
    protected override void Awake()
    {
        var bus = GameObject.Scene.Context.Events;
        
        _damageSubscription = bus.On<DamageEvent>()
            .Do(OnDamageReceived);
    }
    
    private void OnDamageReceived(DamageEvent evt)
    {
        if (evt.Target == GameObject)
            TakeDamage(evt.Amount);
    }
    
    protected override void OnDestroy()
    {
        // Always unsubscribe!
        _damageSubscription?.Dispose();
    }
}
```

### Filtering Events

```csharp
// Filter by condition
bus.On<DamageEvent>()
    .Where(evt => evt.Target == this.GameObject)
    .Do(HandleMyDamage);

// Filter by type
bus.On<CollisionEvent>()
    .Where(evt => evt.Other.Layer == LayerMask.Enemy)
    .Do(HandleEnemyCollision);
```

### Built-in Events

| Event | Description |
|-------|-------------|
| `GameStateChangedEvent` | Game state transition |
| `GameWonEvent` | Player won |
| `GameLostEvent` | Player lost |
| `GamePauseChangedEvent` | Pause state changed |
| `CollisionEvent` | Physics collision |
| `DamageEvent` | Damage dealt |
| `PlaySfxEvent` | Request sound effect |
| `PlayMusicEvent` | Request music playback |
| `StopMusicEvent` | Stop music |
| `FadeChannelEvent` | Fade audio channel |

---

## ImpulseBus

For short-lived effects like camera shake. Impulses are delivered immediately to listeners.

### Accessing ImpulseBus

```csharp
var impulses = context.Impulses;
```

### Sending Impulses

```csharp
// Camera shake
impulses.Send(new Eased3DImpulse(
    intensity: new Vector3(0.1f, 0.1f, 0f),
    duration: 0.3f,
    ease: Ease.SmoothStep
));
```

### Listening for Impulses

```csharp
public class CameraShakeListener : ImpulseListenerBase
{
    private Camera _camera;
    
    protected override void OnImpulse(Vector3 offset)
    {
        // Apply offset to camera
        _camera.Transform.TranslateBy(offset);
    }
}
```

### Continuous Sources

```csharp
// Register a continuous impulse source (e.g., engine vibration)
var source = new EngineVibrationSource();
impulses.AddContinuousSource(source);

// Remove when done
impulses.RemoveContinuousSource(source);
```

---

## C# Events (Direct Pattern)

For tightly-coupled, immediate communication within or between components.

### Declaring Events

```csharp
public class HealthComponent : Component
{
    public event Action<float>? HealthChanged;
    public event Action? Died;
    
    private float _health = 100f;
    
    public void TakeDamage(float amount)
    {
        _health -= amount;
        HealthChanged?.Invoke(_health);
        
        if (_health <= 0)
            Died?.Invoke();
    }
}
```

### Subscribing

```csharp
public class HealthUI : Component
{
    private HealthComponent? _health;
    private UIText? _text;
    
    protected override void Start()
    {
        _health = GameObject.GetComponent<HealthComponent>();
        _health.HealthChanged += OnHealthChanged;
        _health.Died += OnDied;
    }
    
    private void OnHealthChanged(float newHealth)
    {
        _text.Text = $"HP: {newHealth:F0}";
    }
    
    private void OnDied()
    {
        _text.Text = "DEAD";
        _text.Color = Color.Red;
    }
    
    protected override void OnDestroy()
    {
        if (_health != null)
        {
            _health.HealthChanged -= OnHealthChanged;
            _health.Died -= OnDied;
        }
    }
}
```

---

## Transform.Changed Event

React to spatial changes on transforms.

### Subscribing

```csharp
protected override void Awake()
{
    Transform.Changed += OnTransformChanged;
}

private void OnTransformChanged(Transform t, Transform.ChangeFlags flags)
{
    if ((flags & Transform.ChangeFlags.Position) != 0)
    {
        // Position changed
        UpdateBounds();
    }
    
    if ((flags & Transform.ChangeFlags.Rotation) != 0)
    {
        // Rotation changed
        UpdateDirection();
    }
}

protected override void OnDestroy()
{
    if (Transform != null)
        Transform.Changed -= OnTransformChanged;
}
```

### ChangeFlags

```csharp
[Flags]
public enum ChangeFlags : sbyte
{
    None = 0,
    Position = 1 << 0,   // Position changed
    Rotation = 1 << 1,   // Rotation changed
    Scale = 1 << 2,      // Scale changed
    Parent = 1 << 3,     // Parent changed
    Local = 1 << 4,      // Local values changed
    World = 1 << 5,      // World matrix affected
    FromParent = 1 << 6  // Changed due to parent
}
```

---

## Component.EnabledChanged Event

React to component enable/disable.

```csharp
public class UIManager : Component
{
    private List<UIRenderer> _trackedRenderers = new();
    
    public void Track(UIRenderer renderer)
    {
        _trackedRenderers.Add(renderer);
        renderer.EnabledChanged += OnRendererEnabledChanged;
    }
    
    private void OnRendererEnabledChanged(Component comp, bool enabled)
    {
        if (enabled)
            AddToActiveList(comp as UIRenderer);
        else
            RemoveFromActiveList(comp as UIRenderer);
    }
}
```

---

## Choosing the Right Pattern

| Scenario | Recommended Pattern |
|----------|---------------------|
| Player collected an item | EventBus (`ItemCollectedEvent`) |
| Health bar tracking health | C# Event (`HealthChanged`) |
| Explosion causes screen shake | ImpulseBus (`Eased3DImpulse`) |
| Camera follows player | Transform.Changed |
| Door opening triggers sound | EventBus (`PlaySfxEvent`) |
| Button clicked | C# Event (`OnClick`) |
| Game paused globally | EventBus (`GamePauseChangedEvent`) |
| UI element toggled | EnabledChanged |

---

## Best Practices

### Always Unsubscribe

```csharp
// Store subscription
private IDisposable? _subscription;

protected override void Awake()
{
    _subscription = bus.On<MyEvent>().Do(Handle);
}

protected override void OnDestroy()
{
    _subscription?.Dispose();  // Prevent memory leaks
}
```

### Keep Events Immutable

```csharp
// Good - immutable event
public class ScoreChangedEvent
{
    public int NewScore { get; }
    public ScoreChangedEvent(int score) => NewScore = score;
}

// Bad - mutable event
public class ScoreChangedEvent
{
    public int NewScore { get; set; }  // Could be modified by handlers
}
```

### Use Meaningful Event Names

```csharp
// Good - clear intent
public class PlayerEnteredTriggerEvent { }
public class ItemPickedUpEvent { }
public class DoorUnlockedEvent { }

// Bad - vague names
public class Event1 { }
public class DataChanged { }
```

### Consider Event Granularity

```csharp
// Too granular - causes event spam
public class PlayerMovedEvent { }  // Every frame?

// Too coarse - hard to filter
public class PlayerEvent { public string Type; }

// Just right - specific but not spammy
public class PlayerEnteredRoomEvent { public string RoomId; }
public class PlayerHealthChangedEvent { public float NewHealth; }
```
