# Task Guide: Creating Custom Systems

## Overview

Systems are scene-level managers that operate across multiple GameObjects or coordinate engine features. Create a custom system when you need centralized logic that doesn't belong to any single GameObject.

---

## When to Use a System vs Component

| Use a **System** when... | Use a **Component** when... |
|--------------------------|----------------------------|
| Managing multiple objects of a type | Behaviour belongs to one object |
| Coordinating between unrelated objects | Data is specific to one entity |
| Processing requires scene-wide view | Logic is self-contained |
| Centralizing a service (audio, scoring) | Per-instance configuration needed |

---

## Basic System Structure

```csharp
using GDEngine.Core.Systems;
using GDEngine.Core.Entities;

namespace MyGame.Systems
{
    public class MySystem : SystemBase
    {
        #region Fields
        private List<MyComponent> _tracked = new();
        #endregion
        
        #region Constructor
        public MySystem(int order = 0) 
            : base(FrameLifecycle.Update, order)
        {
        }
        #endregion
        
        #region Lifecycle
        protected override void OnAdded()
        {
            // Called when system added to scene
            // Subscribe to events, initialize state
        }
        
        public override void Update(float deltaTime)
        {
            // Called every frame during Update lifecycle
        }
        
        public override void Draw(float deltaTime)
        {
            // Called every frame during Render/PostRender lifecycle
            // Only override if your system draws
        }
        
        protected override void OnRemoved()
        {
            // Cleanup when removed from scene
        }
        #endregion
    }
}
```

---

## Choosing the Right Lifecycle

```csharp
// Input/Events - runs first
: base(FrameLifecycle.EarlyUpdate, order)

// Game logic - main update phase
: base(FrameLifecycle.Update, order)

// Post-movement cleanup - after game logic
: base(FrameLifecycle.LateUpdate, order)

// 3D rendering phase
: base(FrameLifecycle.Render, order)

// UI/overlay rendering - runs last
: base(FrameLifecycle.PostRender, order)
```

---

## System with Component Tracking

Track specific component types across the scene:

```csharp
public class InteractableSystem : SystemBase
{
    private readonly List<InteractableComponent> _interactables = new();
    
    public InteractableSystem() : base(FrameLifecycle.Update, order: 50)
    {
    }
    
    // Registration methods for components to call
    public void Register(InteractableComponent interactable)
    {
        if (!_interactables.Contains(interactable))
            _interactables.Add(interactable);
    }
    
    public void Unregister(InteractableComponent interactable)
    {
        _interactables.Remove(interactable);
    }
    
    // Query methods
    public InteractableComponent? GetClosest(Vector3 position, float maxRange)
    {
        InteractableComponent? closest = null;
        float closestDist = maxRange;
        
        foreach (var interactable in _interactables)
        {
            if (!interactable.Enabled) continue;
            
            var dist = Vector3.Distance(position, interactable.Transform.Position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = interactable;
            }
        }
        
        return closest;
    }
    
    public IReadOnlyList<InteractableComponent> GetAll() => _interactables;
}
```

Components self-register:

```csharp
public class InteractableComponent : Component
{
    private InteractableSystem? _system;
    
    protected override void Start()
    {
        _system = GameObject.Scene.GetSystem<InteractableSystem>();
        _system?.Register(this);
    }
    
    protected override void OnDestroy()
    {
        _system?.Unregister(this);
    }
}
```

---

## System with Event Integration

```csharp
public class ScoreSystem : SystemBase
{
    private int _score;
    private IDisposable? _collectSubscription;
    private IDisposable? _enemySubscription;
    
    public int Score => _score;
    public event Action<int>? ScoreChanged;
    
    public ScoreSystem() : base(FrameLifecycle.Update, order: 100)
    {
    }
    
    protected override void OnAdded()
    {
        var events = Context.Events;
        
        _collectSubscription = events.On<ItemCollectedEvent>()
            .Do(e => AddScore(e.Points));
        
        _enemySubscription = events.On<EnemyDefeatedEvent>()
            .Do(e => AddScore(e.Points));
    }
    
    public void AddScore(int points)
    {
        _score += points;
        ScoreChanged?.Invoke(_score);
    }
    
    public void Reset()
    {
        _score = 0;
        ScoreChanged?.Invoke(_score);
    }
    
    protected override void OnRemoved()
    {
        _collectSubscription?.Dispose();
        _enemySubscription?.Dispose();
    }
}
```

---

## Pausable System

For systems that should respect game pause:

```csharp
public class EnemyAISystem : PausableSystemBase
{
    private readonly List<EnemyAI> _enemies = new();
    
    public EnemyAISystem() : base(FrameLifecycle.Update, order: 200)
    {
        // Configure what pauses
        PauseMode = PauseMode.Update;  // AI stops when paused
    }
    
    protected override void OnAdded()
    {
        // Subscribe to pause events
        Context.Events.On<GamePauseChangedEvent>()
            .Do(e => SetPaused(e.IsPaused));
    }
    
    // Use OnUpdate instead of Update for pausable systems
    protected override void OnUpdate(float deltaTime)
    {
        // This only runs when NOT paused
        foreach (var enemy in _enemies)
        {
            enemy.Think(deltaTime);
        }
    }
}
```

---

## System with Debug Visualization

Implement `IShowDebugInfo` for debug overlay:

```csharp
public class InventorySystem : SystemBase, IShowDebugInfo
{
    private readonly Dictionary<string, int> _items = new();
    
    public InventorySystem() : base(FrameLifecycle.Update)
    {
    }
    
    public void AddItem(string itemId, int count = 1)
    {
        if (_items.ContainsKey(itemId))
            _items[itemId] += count;
        else
            _items[itemId] = count;
    }
    
    public bool HasItem(string itemId) => 
        _items.ContainsKey(itemId) && _items[itemId] > 0;
    
    public int GetCount(string itemId) =>
        _items.TryGetValue(itemId, out var count) ? count : 0;
    
    // IShowDebugInfo implementation
    public IEnumerable<string> GetDebugLines()
    {
        yield return $"Inventory: {_items.Count} types";
        foreach (var kvp in _items)
        {
            yield return $"  {kvp.Key}: {kvp.Value}";
        }
    }
}
```

---

## System as Service Provider

Systems can provide services to components:

```csharp
public interface IInteractionService
{
    bool TryInteract(Vector3 position, float range);
    InteractableComponent? GetCurrentTarget();
}

public class InteractionSystem : SystemBase, IInteractionService
{
    private InteractableComponent? _currentTarget;
    private PhysicsSystem? _physics;
    
    public InteractionSystem() : base(FrameLifecycle.Update, order: 50)
    {
    }
    
    protected override void OnAdded()
    {
        _physics = Scene.GetSystem<PhysicsSystem>();
    }
    
    public bool TryInteract(Vector3 position, float range)
    {
        // Implementation
        return false;
    }
    
    public InteractableComponent? GetCurrentTarget() => _currentTarget;
}

// Components access via interface
public class PlayerInteraction : Component
{
    private IInteractionService? _interactionService;
    
    protected override void Start()
    {
        _interactionService = GameObject.Scene.GetSystem<InteractionSystem>();
    }
}
```

---

## System Setup Checklist

When adding your system to a scene:

```csharp
// Consider order relative to other systems

// EarlyUpdate: -1000 to -1
scene.Add(new EventSystem(context.Events, order: -1000));
scene.Add(new InputSystem());

// Update: 0 to 999
scene.Add(new MyGameLogicSystem(order: 50));
scene.Add(new ScoreSystem(order: 100));
scene.Add(new InteractionSystem(order: 150));

// LateUpdate: 1000+
scene.Add(new PhysicsSystem(order: 1000));

// Render: Any
scene.Add(new CameraSystem(graphicsDevice));
scene.Add(new RenderSystem());

// PostRender: Any
scene.Add(new UIRenderSystem());
scene.Add(new MyDebugOverlaySystem(order: 100));
```

---

## Best Practices

- [ ] Choose lifecycle phase based on when your system needs to run
- [ ] Use order to control execution within a phase
- [ ] Subscribe to events in `OnAdded()`, unsubscribe in `OnRemoved()`
- [ ] Implement `IShowDebugInfo` for debugging complex systems
- [ ] Extend `PausableSystemBase` if system should respect pause
- [ ] Provide clean registration/query APIs for components
- [ ] Keep per-frame work minimal; cache where possible
- [ ] Document system purpose and dependencies
