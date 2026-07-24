# Task Guide: Triggers and Collision Detection

## Overview

Triggers are invisible volumes that detect when objects enter or exit them. Use triggers for interaction zones, area effects, and gameplay events.

---

## Creating a Trigger Volume

```csharp
var triggerGO = new GameObject("InteractionZone");

// 1. Add collider with IsTrigger = true
var collider = triggerGO.AddComponent<BoxCollider>();
collider.Size = new Vector3(2f, 2f, 2f);
collider.IsTrigger = true;  // Essential!

// 2. Add static RigidBody
var rb = triggerGO.AddComponent<RigidBody>();
rb.BodyType = BodyType.Static;

// 3. Position in world
triggerGO.Transform.TranslateTo(new Vector3(5, 1, 10));

scene.Add(triggerGO);
```

### Trigger Shapes

```csharp
// Box trigger
var box = go.AddComponent<BoxCollider>();
box.Size = new Vector3(width, height, depth);
box.IsTrigger = true;

// Sphere trigger
var sphere = go.AddComponent<SphereCollider>();
sphere.Radius = 3f;
sphere.IsTrigger = true;

// Capsule trigger (doorways, corridors)
var capsule = go.AddComponent<CapsuleCollider>();
capsule.Radius = 1f;
capsule.Height = 3f;
capsule.IsTrigger = true;
```

---

## Detecting Trigger Events

### Via EventBus

```csharp
public class TriggerZone : Component
{
    private IDisposable? _subscription;
    
    public event Action<GameObject>? OnEnter;
    public event Action<GameObject>? OnExit;
    
    protected override void Awake()
    {
        var events = GameObject.Scene.Context.Events;
        
        _subscription = events.On<TriggerEvent>()
            .Where(e => e.Trigger == this.GameObject)
            .Do(HandleTrigger);
    }
    
    private void HandleTrigger(TriggerEvent evt)
    {
        switch (evt.Type)
        {
            case TriggerEventType.Enter:
                OnEnter?.Invoke(evt.Other);
                break;
            case TriggerEventType.Exit:
                OnExit?.Invoke(evt.Other);
                break;
        }
    }
    
    protected override void OnDestroy()
    {
        _subscription?.Dispose();
    }
}
```

### Usage

```csharp
var zone = triggerGO.AddComponent<TriggerZone>();

zone.OnEnter += other => {
    if (other.Layer == LayerMask.Player)
    {
        ShowInteractionPrompt();
    }
};

zone.OnExit += other => {
    if (other.Layer == LayerMask.Player)
    {
        HideInteractionPrompt();
    }
};
```

---

## Proximity Detection (Alternative)

For simple "is player nearby?" checks without physics triggers:

```csharp
public class ProximityDetector : Component
{
    public float DetectionRadius { get; set; } = 2f;
    public LayerMask TargetLayer { get; set; } = LayerMask.Player;
    
    private GameObject? _detectedTarget;
    private bool _wasInRange;
    
    public event Action<GameObject>? OnTargetEnter;
    public event Action<GameObject>? OnTargetExit;
    public bool IsTargetInRange => _detectedTarget != null;
    public GameObject? DetectedTarget => _detectedTarget;
    
    protected override void Update(float deltaTime)
    {
        var target = FindTarget();
        bool isInRange = target != null;
        
        // Detect state changes
        if (isInRange && !_wasInRange)
        {
            _detectedTarget = target;
            OnTargetEnter?.Invoke(target!);
        }
        else if (!isInRange && _wasInRange)
        {
            var previous = _detectedTarget;
            _detectedTarget = null;
            OnTargetExit?.Invoke(previous!);
        }
        
        _wasInRange = isInRange;
    }
    
    private GameObject? FindTarget()
    {
        var myPos = Transform.Position;
        
        foreach (var go in GameObject.Scene.GameObjects)
        {
            if (!TargetLayer.Contains(go.Layer)) continue;
            
            var dist = Vector3.Distance(myPos, go.Transform.Position);
            if (dist <= DetectionRadius)
                return go;
        }
        
        return null;
    }
}
```

---

## Raycast-Based Detection

For line-of-sight or directional detection:

```csharp
public class LookDetector : Component
{
    public float MaxDistance { get; set; } = 5f;
    public LayerMask DetectionMask { get; set; } = LayerMask.Interactable;
    
    private PhysicsSystem? _physics;
    private GameObject? _lookedAt;
    
    public GameObject? LookedAtObject => _lookedAt;
    public event Action<GameObject?>? OnLookChanged;
    
    protected override void Start()
    {
        _physics = GameObject.Scene.GetSystem<PhysicsSystem>();
    }
    
    protected override void Update(float deltaTime)
    {
        var origin = Transform.Position;
        var direction = Transform.Forward;
        
        GameObject? newTarget = null;
        
        if (_physics != null && 
            _physics.Raycast(origin, direction, MaxDistance, out var hit, DetectionMask))
        {
            newTarget = hit.GameObject;
        }
        
        if (newTarget != _lookedAt)
        {
            _lookedAt = newTarget;
            OnLookChanged?.Invoke(_lookedAt);
        }
    }
}
```

---

## Combining Trigger + Raycast

Common pattern: Trigger for range, raycast for precision:

```csharp
public class InteractionDetector : Component
{
    private TriggerZone? _triggerZone;
    private PhysicsSystem? _physics;
    private HashSet<GameObject> _inRange = new();
    
    public float InteractDistance { get; set; } = 2f;
    
    public GameObject? GetInteractable(Vector3 lookOrigin, Vector3 lookDirection)
    {
        // First: Must be in trigger zone
        if (_inRange.Count == 0) return null;
        
        // Second: Must be looked at
        if (_physics.Raycast(lookOrigin, lookDirection, InteractDistance, out var hit))
        {
            if (_inRange.Contains(hit.GameObject))
                return hit.GameObject;
        }
        
        return null;
    }
    
    protected override void Start()
    {
        _physics = GameObject.Scene.GetSystem<PhysicsSystem>();
        _triggerZone = GameObject.GetComponent<TriggerZone>();
        
        _triggerZone.OnEnter += obj => _inRange.Add(obj);
        _triggerZone.OnExit += obj => _inRange.Remove(obj);
    }
}
```

---

## Collision Events (Non-Trigger)

For actual physics collisions:

```csharp
public class CollisionHandler : Component
{
    private IDisposable? _subscription;
    
    public event Action<GameObject, Vector3, Vector3>? OnCollision;
    
    protected override void Awake()
    {
        var events = GameObject.Scene.Context.Events;
        
        _subscription = events.On<CollisionEvent>()
            .Where(e => e.BodyA == this.GameObject || e.BodyB == this.GameObject)
            .Do(HandleCollision);
    }
    
    private void HandleCollision(CollisionEvent evt)
    {
        var other = evt.BodyA == this.GameObject ? evt.BodyB : evt.BodyA;
        OnCollision?.Invoke(other, evt.ContactPoint, evt.Normal);
    }
    
    protected override void OnDestroy()
    {
        _subscription?.Dispose();
    }
}
```

---

## Common Patterns

### Room Entry Detection

```csharp
public class RoomTrigger : Component
{
    public string RoomId { get; set; } = "";
    
    private TriggerZone? _zone;
    private EventBus? _events;
    
    protected override void Start()
    {
        _zone = GameObject.GetComponent<TriggerZone>();
        _events = GameObject.Scene.Context.Events;
        
        _zone.OnEnter += OnEnter;
        _zone.OnExit += OnExit;
    }
    
    private void OnEnter(GameObject other)
    {
        if (other.Layer == LayerMask.Player)
        {
            _events?.Publish(new RoomEnteredEvent(RoomId));
        }
    }
    
    private void OnExit(GameObject other)
    {
        if (other.Layer == LayerMask.Player)
        {
            _events?.Publish(new RoomExitedEvent(RoomId));
        }
    }
}
```

### Pickup Zone

```csharp
public class PickupTrigger : Component
{
    public string ItemId { get; set; } = "";
    public bool DestroyOnPickup { get; set; } = true;
    
    private bool _pickedUp;
    
    protected override void Start()
    {
        var zone = GameObject.GetComponent<TriggerZone>();
        zone.OnEnter += OnEnter;
    }
    
    private void OnEnter(GameObject other)
    {
        if (_pickedUp) return;
        if (other.Layer != LayerMask.Player) return;
        
        _pickedUp = true;
        
        var events = GameObject.Scene.Context.Events;
        events.Publish(new ItemCollectedEvent(ItemId));
        
        if (DestroyOnPickup)
            GameObject.Destroy();
    }
}
```

### Interaction Zone with Prompt

```csharp
public class InteractionZone : Component
{
    public string PromptText { get; set; } = "Press E to interact";
    
    private ProximityDetector? _detector;
    private UIText? _promptUI;
    private bool _isPlayerInRange;
    
    protected override void Start()
    {
        _detector = GameObject.GetComponent<ProximityDetector>();
        
        _detector.OnTargetEnter += _ => ShowPrompt();
        _detector.OnTargetExit += _ => HidePrompt();
    }
    
    private void ShowPrompt()
    {
        _isPlayerInRange = true;
        _promptUI.Text = PromptText;
        _promptUI.Enabled = true;
    }
    
    private void HidePrompt()
    {
        _isPlayerInRange = false;
        _promptUI.Enabled = false;
    }
    
    public bool CanInteract() => _isPlayerInRange;
}
```

---

## Debugging Tips

### Visualize Trigger Bounds

```csharp
// In a debug system or component
public void DrawTriggerBounds(Collider collider, Color color)
{
    if (collider is BoxCollider box)
    {
        var center = collider.Transform.Position + box.Center;
        var size = box.Size;
        // Draw wireframe box...
    }
    else if (collider is SphereCollider sphere)
    {
        var center = collider.Transform.Position + sphere.Center;
        // Draw wireframe sphere...
    }
}
```

### Log Trigger Events

```csharp
protected override void Awake()
{
    var zone = GameObject.GetComponent<TriggerZone>();
    
    zone.OnEnter += obj => 
        Debug.WriteLine($"[{GameObject.Name}] Enter: {obj.Name}");
    
    zone.OnExit += obj => 
        Debug.WriteLine($"[{GameObject.Name}] Exit: {obj.Name}");
}
```
