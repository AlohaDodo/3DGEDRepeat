# Lifecycle Reference

## Overview

GDEngine uses two parallel lifecycle systems:
1. **System Lifecycle** - Controls when systems execute within a frame
2. **Component Lifecycle** - Controls when component methods are called

Understanding these lifecycles is essential for writing correct, predictable game logic.

## FrameLifecycle Enum

```csharp
public enum FrameLifecycle : sbyte
{
    EarlyUpdate = 0,  // Input, events
    Update = 1,       // Game logic
    LateUpdate = 2,   // Physics, transforms
    Render = 3,       // 3D drawing
    PostRender = 4    // UI, post-processing
}
```

## System Execution Order

### Within a Single Frame

```
Scene.Update(deltaTime)
│
├── EarlyUpdate systems (sorted by Order)
│   └── System.Update(deltaTime)
│
├── Update systems (sorted by Order)
│   └── System.Update(deltaTime)
│
├── LateUpdate systems (sorted by Order)
│   └── System.Update(deltaTime)
│
├── Component Start() calls (first frame only)
├── Component Update() calls
└── Component LateUpdate() calls

Scene.Draw(deltaTime)
│
├── Render systems (sorted by Order)
│   └── System.Draw(deltaTime)
│
└── PostRender systems (sorted by Order)
    └── System.Draw(deltaTime)
```

### System Order Property

Systems within the same lifecycle are sorted by their `Order` property:

```csharp
// Runs before other EarlyUpdate systems
new EventSystem(bus, order: -1000);

// Runs after other Render systems
new UIRenderSystem(order: 10);
```

Lower order values execute first.

## Component Lifecycle Methods

### Execution Sequence

| Method | When Called | Use For |
|--------|-------------|---------|
| `Awake()` | Once, when GameObject added to Scene | Cache references, one-time setup |
| `Start()` | Once, before first Update | Initialization that depends on other components |
| `Update(float deltaTime)` | Every frame | Game logic, input handling |
| `LateUpdate(float deltaTime)` | Every frame, after all Updates | Camera follow, cleanup |
| `OnEnabled()` | When Enabled becomes true | Resume operations |
| `OnDisabled()` | When Enabled becomes false | Pause operations |
| `OnDestroy()` | When component/GameObject destroyed | Cleanup, unsubscribe events |

### Lifecycle Flow Diagram

```
GameObject added to Scene
         │
         ▼
    ┌─────────┐
    │ Awake() │ ◄── Called immediately
    └────┬────┘
         │
         ▼
    ┌──────────┐
    │OnEnabled()│ ◄── If component starts enabled
    └────┬─────┘
         │
    ═════╪═════ First frame ═════════════════
         │
         ▼
    ┌─────────┐
    │ Start() │ ◄── Called once, before first Update
    └────┬────┘
         │
    ═════╪═════ Every frame ═════════════════
         │
         ▼
    ┌──────────────────┐
    │ Update(deltaTime)│ ◄── Called if Enabled && GameObject.Enabled
    └────────┬─────────┘
             │
             ▼
    ┌──────────────────────┐
    │LateUpdate(deltaTime) │
    └──────────────────────┘
         │
    ═════╪═════ When Enabled changes ════════
         │
         ▼
    ┌────────────┐     ┌─────────────┐
    │OnDisabled()│ ◄─► │ OnEnabled() │
    └────────────┘     └─────────────┘
         │
    ═════╪═════ Destruction ═════════════════
         │
         ▼
    ┌────────────┐
    │OnDisabled()│ ◄── Called if was enabled
    └─────┬──────┘
          │
          ▼
    ┌────────────┐
    │ OnDestroy()│
    └────────────┘
```

## Typical System Lifecycle Assignments

| System | Lifecycle | Typical Order | Rationale |
|--------|-----------|---------------|-----------|
| EventSystem | EarlyUpdate | -1000 | Events ready before game logic |
| InputSystem | EarlyUpdate | 0 | Input ready for game logic |
| OrchestrationSystem | Update | 0 | Animations during logic phase |
| GameStateSystem | Update | 0 | Win/lose checks during logic |
| NavMeshSystem | Update | 0 | Pathfinding during logic |
| PhysicsSystem | LateUpdate | 1000 | Physics after movement intent |
| ImpulseSystem | LateUpdate | -1000 | Impulses before physics |
| CameraSystem | Render | -100 | Camera ready before rendering |
| RenderSystem | Render | 0 | Main 3D rendering |
| UIRenderSystem | PostRender | 10 | UI on top of 3D |

## Pause Behaviour

### PausableSystemBase

Systems extending `PausableSystemBase` can respect pause state:

```csharp
public class MySystem : PausableSystemBase
{
    public MySystem() : base(FrameLifecycle.Update)
    {
        // Configure what pauses
        PauseMode = PauseMode.Update;        // Only Update pauses
        PauseMode = PauseMode.Draw;          // Only Draw pauses
        PauseMode = PauseMode.Update | PauseMode.Draw; // Both pause
    }
    
    protected override void OnUpdate(float deltaTime)
    {
        // Only called when not paused (or PauseMode doesn't include Update)
    }
    
    protected override void OnDraw(float deltaTime)
    {
        // Only called when not paused (or PauseMode doesn't include Draw)
    }
}
```

### Time.IsPaused vs System Pause

- `Time.IsPaused` - Global time pause, affects `DeltaTimeSecs`
- `PausableSystemBase.SetPaused()` - Per-system pause control

## Common Patterns

### Caching References in Awake

```csharp
private Transform _transform;
private Camera _camera;

protected override void Awake()
{
    _transform = Transform; // Guaranteed available
    _camera = GameObject.GetComponent<Camera>();
}
```

### Deferring to Start

```csharp
protected override void Start()
{
    // Safe to access other GameObjects/components
    var target = GameObject.Scene.Find(go => go.Name == "Target");
}
```

### Cleanup in OnDestroy

```csharp
protected override void OnDestroy()
{
    // Unsubscribe from events
    _eventBus.Unsubscribe<DamageEvent>(_subscription);
    
    // Remove from systems
    _cameraSystem.Remove(_camera);
}
```

## Debugging Lifecycle Issues

Common problems and solutions:

| Problem | Likely Cause | Solution |
|---------|--------------|----------|
| NullReferenceException in Awake | Accessing scene-level resources | Move to Start() |
| Component not updating | Enabled is false | Check component and GameObject Enabled |
| System not running | Wrong lifecycle or not added | Verify Add() called and lifecycle matches |
| Events not received | EventSystem not in scene | Add EventSystem to scene |
| Physics not working | PhysicsSystem missing or order wrong | Check system presence and order |
