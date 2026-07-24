# Physics Basics

## Overview

GDEngine uses BepuPhysics v2 for physics simulation. The `PhysicsSystem` manages rigid bodies, collision detection, and raycasting.

---

## Setup

```csharp
// Add physics system to scene
var physicsSystem = new PhysicsSystem();
physicsSystem.Gravity = new Vector3(0, -9.81f, 0);
scene.Add(physicsSystem);
```

---

## Body Types

| Type | Description | Use Case |
|------|-------------|----------|
| `Static` | Immovable, infinite mass | Walls, floors, level geometry |
| `Kinematic` | Code-controlled, affects dynamics | Moving platforms, doors |
| `Dynamic` | Physics-driven | Projectiles, debris, characters |

### Creating Static Bodies

```csharp
var wall = new GameObject("Wall");

// Collider defines shape
var collider = wall.AddComponent<BoxCollider>();
collider.Size = new Vector3(10f, 3f, 0.5f);

// RigidBody connects to physics
var rb = wall.AddComponent<RigidBody>();
rb.BodyType = BodyType.Static;

scene.Add(wall);
```

### Creating Dynamic Bodies

```csharp
var ball = new GameObject("Ball");

var collider = ball.AddComponent<SphereCollider>();
collider.Radius = 0.5f;

var rb = ball.AddComponent<RigidBody>();
rb.BodyType = BodyType.Dynamic;
rb.Mass = 1f;
rb.UseGravity = true;
rb.LinearDamping = 0.1f;

scene.Add(ball);
```

### Creating Kinematic Bodies

```csharp
var platform = new GameObject("MovingPlatform");

var collider = platform.AddComponent<BoxCollider>();
collider.Size = new Vector3(4f, 0.5f, 4f);

var rb = platform.AddComponent<RigidBody>();
rb.BodyType = BodyType.Kinematic;

scene.Add(platform);

// Move via transform (physics follows)
platform.Transform.TranslateBy(movement * deltaTime);
```

---

## Colliders

### BoxCollider

```csharp
var box = gameObject.AddComponent<BoxCollider>();
box.Size = new Vector3(width, height, depth);
box.Center = Vector3.Zero;  // Offset from transform
```

### SphereCollider

```csharp
var sphere = gameObject.AddComponent<SphereCollider>();
sphere.Radius = 0.5f;
sphere.Center = Vector3.Zero;
```

### CapsuleCollider

```csharp
var capsule = gameObject.AddComponent<CapsuleCollider>();
capsule.Radius = 0.3f;
capsule.Height = 1.8f;  // Total height including caps
capsule.Center = new Vector3(0, 0.9f, 0);  // Offset to stand on ground
```

### Collider Offset

```csharp
// Center property offsets collider from transform origin
collider.Center = new Vector3(0, 1f, 0);  // 1 unit above transform origin

// Useful when:
// - Pivot is at feet but collider should be at center
// - Multiple colliders on one object
```

---

## Forces and Impulses

### Continuous Forces

```csharp
// Apply force over time (call in Update)
rb.AddForce(Vector3.Up * 100f);  // 100 Newtons upward

// Force at position (causes torque)
rb.AddForce(Vector3.Forward * 50f, hitPoint);
```

### Instant Impulses

```csharp
// Instant velocity change
rb.AddImpulse(Vector3.Up * 10f);  // Jump

// Torque (rotational force)
rb.AddTorque(Vector3.Up * 5f);
```

### Velocity Control

```csharp
// Direct velocity set
rb.LinearVelocity = new Vector3(5f, 0, 0);
rb.AngularVelocity = Vector3.Zero;  // Stop spinning

// Read velocities
var speed = rb.LinearVelocity.Length();
```

---

## Raycasting

### Basic Raycast

```csharp
var physics = scene.GetSystem<PhysicsSystem>();

Vector3 origin = camera.Transform.Position;
Vector3 direction = camera.Transform.Forward;

if (physics.Raycast(origin, direction, 100f, out RayHit hit))
{
    GameObject hitObject = hit.GameObject;
    Vector3 hitPoint = hit.Position;
    Vector3 hitNormal = hit.Normal;
    float distance = hit.Distance;
}
```

### Layer Filtering

```csharp
// Only hit enemies
if (physics.Raycast(origin, direction, 100f, out hit, LayerMask.Enemy))
{
    // Hit an enemy
}

// Hit world and interactables, not player
var mask = LayerMask.World | LayerMask.Interactable;
if (physics.Raycast(origin, direction, 100f, out hit, mask))
{
    // ...
}
```

### From Camera (Picking)

```csharp
var cameraSystem = scene.GetSystem<CameraSystem>();
var physics = scene.GetSystem<PhysicsSystem>();

// Get ray from mouse
var mousePos = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
Ray ray = cameraSystem.ScreenPointToRay(scene.ActiveCamera, mousePos);

// Cast ray
if (physics.Raycast(ray.Position, ray.Direction, 100f, out hit))
{
    var clickedObject = hit.GameObject;
}
```

---

## Trigger Volumes

Triggers detect overlaps without physical collision response.

### Creating a Trigger

```csharp
var trigger = new GameObject("InteractionZone");

var collider = trigger.AddComponent<BoxCollider>();
collider.Size = new Vector3(2f, 2f, 2f);
collider.IsTrigger = true;  // Important!

var rb = trigger.AddComponent<RigidBody>();
rb.BodyType = BodyType.Static;  // Triggers are usually static

scene.Add(trigger);
```

### Handling Trigger Events

Trigger events come through the EventBus:

```csharp
public class TriggerHandler : Component
{
    private IDisposable? _subscription;
    
    protected override void Awake()
    {
        var events = GameObject.Scene.Context.Events;
        
        _subscription = events.On<TriggerEvent>()
            .Where(e => e.Trigger == this.GameObject)
            .Do(OnTrigger);
    }
    
    private void OnTrigger(TriggerEvent evt)
    {
        if (evt.Type == TriggerEventType.Enter)
        {
            // Object entered trigger
            var other = evt.Other;
        }
        else if (evt.Type == TriggerEventType.Exit)
        {
            // Object exited trigger
        }
    }
    
    protected override void OnDestroy()
    {
        _subscription?.Dispose();
    }
}
```

---

## Collision Events

Non-trigger collisions also generate events:

```csharp
events.On<CollisionEvent>()
    .Where(e => e.BodyA == this.GameObject || e.BodyB == this.GameObject)
    .Do(OnCollision);

private void OnCollision(CollisionEvent evt)
{
    var other = evt.BodyA == this.GameObject ? evt.BodyB : evt.BodyA;
    var contactPoint = evt.ContactPoint;
    var normal = evt.Normal;
    var impulse = evt.Impulse;
}
```

---

## Common Patterns

### Ground Check

```csharp
public bool IsGrounded(Transform transform, float checkDistance = 0.1f)
{
    var physics = scene.GetSystem<PhysicsSystem>();
    var origin = transform.Position + Vector3.Up * 0.05f;
    
    return physics.Raycast(
        origin, 
        Vector3.Down, 
        checkDistance + 0.05f, 
        out _, 
        LayerMask.World
    );
}
```

### Interactive Object Detection

```csharp
public class InteractionDetector : Component
{
    public float Range { get; set; } = 3f;
    public LayerMask InteractLayer { get; set; } = LayerMask.Interactable;
    
    private PhysicsSystem? _physics;
    private CameraSystem? _cameraSystem;
    
    public GameObject? GetLookedAtObject()
    {
        var camera = GameObject.Scene.ActiveCamera;
        if (camera == null) return null;
        
        var origin = camera.Transform.Position;
        var direction = camera.Transform.Forward;
        
        if (_physics.Raycast(origin, direction, Range, out var hit, InteractLayer))
        {
            return hit.GameObject;
        }
        
        return null;
    }
}
```

### Explosion Force

```csharp
public void ApplyExplosion(Vector3 center, float radius, float force)
{
    var physics = scene.GetSystem<PhysicsSystem>();
    
    // Find all dynamic bodies in radius
    foreach (var go in scene.GameObjects)
    {
        var rb = go.GetComponent<RigidBody>();
        if (rb == null || rb.BodyType != BodyType.Dynamic) continue;
        
        var direction = go.Transform.Position - center;
        var distance = direction.Length();
        
        if (distance > radius || distance < 0.01f) continue;
        
        // Falloff with distance
        var falloff = 1f - (distance / radius);
        var impulse = Vector3.Normalize(direction) * force * falloff;
        
        rb.AddImpulse(impulse);
    }
}
```

### Teleport (Safe Position Set)

```csharp
public void Teleport(RigidBody rb, Vector3 newPosition)
{
    rb.SetPosition(newPosition);
    rb.LinearVelocity = Vector3.Zero;  // Clear momentum
    rb.AngularVelocity = Vector3.Zero;
}
```

---

## Physics Properties

### Damping

```csharp
// Linear damping (air resistance)
rb.LinearDamping = 0.1f;  // 0 = none, 1 = instant stop

// Angular damping (rotational drag)
rb.AngularDamping = 0.1f;
```

### Gravity Toggle

```csharp
// Per-body gravity control
rb.UseGravity = false;  // Floats
rb.UseGravity = true;   // Falls

// Global gravity
physicsSystem.Gravity = new Vector3(0, -20f, 0);  // Moon gravity
physicsSystem.Gravity = Vector3.Zero;  // No gravity
```

---

## Debug Visualization

The PhysicsSystem includes debug rendering:

```csharp
// In PhysicsSystem
physicsSystem.DebugDrawEnabled = true;

// Colors:
// - Green: Dynamic bodies
// - Blue: Kinematic bodies
// - Gray: Static bodies
// - Yellow: Triggers
```

---

## Performance Tips

### Use Simple Shapes

```csharp
// Prefer (fastest to slowest):
// 1. Sphere
// 2. Capsule
// 3. Box
// 4. Convex mesh (avoid if possible)
```

### Static When Possible

```csharp
// If it doesn't move, make it static
rb.BodyType = BodyType.Static;  // Most efficient
```

### Disable Unnecessary Bodies

```csharp
// Disable distant or inactive physics
rb.Enabled = false;  // Removes from simulation
```

### Layer Filtering

```csharp
// Use layers to reduce collision checks
player.Layer = LayerMask.Player;
enemy.Layer = LayerMask.Enemy;
trigger.Layer = LayerMask.Trigger;

// Configure collision matrix in physics system
```
