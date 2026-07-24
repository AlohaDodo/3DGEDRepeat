# GDEngine Documentation

## Quick Navigation

### Tier 1: Architecture Reference
*Understand the big picture*

| Document | Description |
|----------|-------------|
| [Architecture Overview](00-architecture-overview.md) | Core concepts, class hierarchy, scene structure |
| [Lifecycle Reference](01-lifecycle-reference.md) | Frame lifecycle, component lifecycle, execution order |

### Tier 2: API Reference
*How to use each class*

| Document | Description |
|----------|-------------|
| [Core Classes](02-core-classes.md) | GameObject, Scene, Transform, Time, Component, SystemBase |
| [Systems Reference](03-systems-reference.md) | All built-in systems (Input, Audio, Physics, etc.) |
| [Components Reference](04-components-reference.md) | All built-in components (Camera, RigidBody, UI, etc.) |
| [Events and Messaging](05-events-and-messaging.md) | EventBus, ImpulseBus, C# events, patterns |
| [Orchestration Guide](06-orchestration-guide.md) | Timeline animations, tweens, sequences |
| [Audio Integration](07-audio-integration.md) | SFX, music, spatial audio, mixing |
| [Camera Behaviours](08-camera-behaviours.md) | Camera setup, transitions, effects |
| [UI System](09-ui-system.md) | UI components, layout, visibility |
| [Physics Basics](10-physics-basics.md) | RigidBody, colliders, raycasting, triggers |

### Tier 3: Task Guides
*How to accomplish specific goals*

| Guide | Description |
|-------|-------------|
| [Creating Custom Components](guides/creating-custom-components.md) | Component structure, dependencies, events |
| [Creating Custom Systems](guides/creating-custom-systems.md) | System structure, lifecycle, tracking |
| [Triggers and Detection](guides/triggers-and-detection.md) | Trigger volumes, proximity, raycasting |
| [Data-Driven Design](guides/data-driven-design.md) | Structuring game data, avoiding magic strings |

---

## By Task

### "I need to..."

| Task | See |
|------|-----|
| Understand the engine architecture | [Architecture Overview](00-architecture-overview.md) |
| Know when code runs | [Lifecycle Reference](01-lifecycle-reference.md) |
| Create a new component | [Creating Custom Components](guides/creating-custom-components.md) |
| Create a new system | [Creating Custom Systems](guides/creating-custom-systems.md) |
| Send/receive events | [Events and Messaging](05-events-and-messaging.md) |
| Animate objects smoothly | [Orchestration Guide](06-orchestration-guide.md) |
| Play sounds | [Audio Integration](07-audio-integration.md) |
| Move/transition cameras | [Camera Behaviours](08-camera-behaviours.md) |
| Display UI elements | [UI System](09-ui-system.md) |
| Detect player proximity | [Triggers and Detection](guides/triggers-and-detection.md) |
| Cast rays for interaction | [Physics Basics](10-physics-basics.md) |
| Structure my game data | [Data-Driven Design](guides/data-driven-design.md) |

---

## Key Concepts Quick Reference

### Component Lifecycle

```
Awake() → Start() → Update() → LateUpdate() → OnDestroy()
```

### Frame Lifecycle (Systems)

```
EarlyUpdate → Update → LateUpdate → [Components] → Render → PostRender
```

### Event Pattern

```csharp
// Subscribe
_subscription = events.On<MyEvent>().Do(HandleEvent);

// Publish
events.Publish(new MyEvent(data));

// Cleanup
_subscription?.Dispose();
```

### Orchestration Pattern

```csharp
orchestrator.Sequence()
    .MoveTo(transform, position, duration, Ease.SmoothStep)
    .Then()
    .Do(() => callback())
    .Play();
```

### Raycast Pattern

```csharp
if (physics.Raycast(origin, direction, maxDist, out var hit, layerMask))
{
    var hitObject = hit.GameObject;
    var hitPoint = hit.Position;
}
```

---

## Conventions

### Code Style

- Private fields: `_camelCase` with underscore prefix
- Properties: `PascalCase`
- Methods: `PascalCase`
- Local variables: `camelCase`

### Regions

```csharp
#region Fields
#region Properties  
#region Events
#region Constructor
#region Lifecycle
#region Public Methods
#region Private Methods
#endregion
```

### Documentation

```csharp
/// <summary>
/// Brief description of the member.
/// </summary>
/// <param name="name">Parameter description.</param>
/// <returns>Return value description.</returns>
```
