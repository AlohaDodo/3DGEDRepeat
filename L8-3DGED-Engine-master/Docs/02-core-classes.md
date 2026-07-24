# Core Classes Reference

## GameObject

**Namespace:** `GDEngine.Core.Entities`

A container for components representing an entity in the scene.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Display name for debugging |
| `Enabled` | `bool` | If false, components skip Update/LateUpdate |
| `Transform` | `Transform` | Always-present transform component |
| `Components` | `IReadOnlyList<Component>` | All attached components |
| `Scene` | `Scene?` | The scene containing this object |
| `Layer` | `LayerMask` | Layer for rendering/query filtering |
| `IsStatic` | `bool` | Hint for optimization (immovable objects) |

### Methods

```csharp
// Add a new component by type
T AddComponent<T>() where T : Component, new()

// Add an existing component instance
Component AddComponent(Component component)

// Get first component of type (or null)
T? GetComponent<T>() where T : Component

// Try to get component (returns false if not found)
bool TryGetComponent<T>(out T? component) where T : Component

// Get all components of type
List<T> GetComponents<T>() where T : Component

// Remove first component of type
bool RemoveComponent<T>() where T : Component

// Remove specific component instance
bool RemoveComponent(Component component)

// Destroy this object and all components
void Destroy()
```

### Usage Example

```csharp
// Create a new game object
var enemy = new GameObject("Enemy_01");

// Add components
var collider = enemy.AddComponent<BoxCollider>();
var rigidBody = enemy.AddComponent<RigidBody>();
var renderer = enemy.AddComponent<MeshRenderer>();

// Configure
enemy.Layer = LayerMask.World;
enemy.Transform.TranslateTo(new Vector3(5, 0, 10));

// Add to scene
scene.Add(enemy);

// Later: find and modify
var found = scene.Find(go => go.Name == "Enemy_01");
found?.GetComponent<RigidBody>()?.AddForce(Vector3.Up * 100);
```

---

## Scene

**Namespace:** `GDEngine.Core.Entities`

Container for GameObjects and Systems. Coordinates lifecycles and rendering.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Scene identifier |
| `Context` | `EngineContext` | Engine services access |
| `ActiveCamera` | `Camera?` | Currently active camera |
| `GameObjects` | `IReadOnlyList<GameObject>` | All objects in scene |
| `Systems` | `IReadOnlyList<SystemBase>` | All registered systems |
| `Renderers` | `IReadOnlyList<MeshRenderer>` | Registered mesh renderers |

### Events

| Event | Signature | Description |
|-------|-----------|-------------|
| `GameObjectAdded` | `Action<GameObject>` | Fired when object added |
| `GameObjectRemoved` | `Action<GameObject>` | Fired when object removed |

### Methods

```csharp
// Add a system
void Add(SystemBase system)
T AddSystem<T>(T system) where T : SystemBase

// Add a game object
GameObject Add(GameObject gameObject)

// Remove a game object
bool Remove(GameObject gameObject)

// Find objects
GameObject? Find(Predicate<GameObject> filter)
List<GameObject> FindAll(Predicate<GameObject> filter)

// Get a system by type
T? GetSystem<T>() where T : SystemBase

// Camera control
void SetActiveCamera(Camera? camera)
void SetActiveCamera(string? targetName)

// Frame methods (called by SceneManager)
void Update(float deltaTime)
void Draw(float deltaTime)

// Cleanup
void Clear()
void ClearGameObject()
void ClearSystems()
```

### Usage Example

```csharp
// Create scene
var scene = new Scene(context, "MainLevel");

// Add required systems
scene.Add(new EventSystem(context.Events));
scene.Add(new InputSystem());
scene.Add(new CameraSystem(graphicsDevice));
scene.Add(new RenderSystem());

// Add objects
var player = CreatePlayer();
scene.Add(player);

// Set camera
scene.SetActiveCamera("PlayerCamera");

// Find objects by criteria
var enemies = scene.FindAll(go => go.Name.StartsWith("Enemy"));
var door = scene.Find(go => go.GetComponent<DoorComponent>() != null);
```

---

## Transform

**Namespace:** `GDEngine.Core.Components`

Hierarchical transform with position, rotation, and scale. Automatically attached to every GameObject.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `LocalPosition` | `Vector3` | Position relative to parent |
| `LocalRotation` | `Quaternion` | Rotation relative to parent |
| `LocalScale` | `Vector3` | Scale relative to parent |
| `Position` | `Vector3` | World-space position (read-only*) |
| `Rotation` | `Quaternion` | World-space rotation (read-only*) |
| `LocalMatrix` | `Matrix` | Local TRS matrix (cached) |
| `WorldMatrix` | `Matrix` | World matrix (cached) |
| `Parent` | `Transform?` | Parent transform |
| `Children` | `IReadOnlyList<Transform>` | Child transforms |
| `Right` | `Vector3` | Local X axis in world space |
| `Up` | `Vector3` | Local Y axis in world space |
| `Forward` | `Vector3` | Local Z axis in world space |

### Events

| Event | Signature | Description |
|-------|-----------|-------------|
| `Changed` | `Action<Transform, ChangeFlags>` | Fired when transform changes |

### Methods

```csharp
// Hierarchy
void SetParent(Transform? newParent)
void SetParent(GameObject gameObject)

// Position
void TranslateTo(in Vector3 target)
void TranslateBy(in Vector3 delta, bool worldSpace = false)

// Rotation
void RotateBy(in Quaternion delta, bool worldSpace = false)
void RotateEulerBy(in Vector3 eulerRadians, bool worldSpace = false)
void RotateToWorld(in Quaternion worldRotation)

// Scale
void ScaleTo(in Vector3 scaleTo)
void ScaleBy(in Vector3 scaleBy)
void ScaleBy(float scaleBy)
```

### ChangeFlags

```csharp
[Flags]
public enum ChangeFlags : sbyte
{
    None = 0,
    Position = 1 << 0,
    Rotation = 1 << 1,
    Scale = 1 << 2,
    Parent = 1 << 3,
    Local = 1 << 4,
    World = 1 << 5,
    FromParent = 1 << 6
}
```

### Usage Example

```csharp
// Position an object
transform.TranslateTo(new Vector3(10, 0, 5));

// Move relative to current position
transform.TranslateBy(Vector3.Forward * speed * deltaTime);

// Move in world space (ignores rotation)
transform.TranslateBy(Vector3.Up * 2f, worldSpace: true);

// Rotate by euler angles
transform.RotateEulerBy(new Vector3(0, MathHelper.PiOver4, 0));

// Set up hierarchy
childTransform.SetParent(parentTransform);

// React to changes
transform.Changed += (t, flags) => {
    if ((flags & Transform.ChangeFlags.Position) != 0)
        OnPositionChanged();
};
```

---

## Time

**Namespace:** `GDEngine.Core.Timing`

Static class providing frame timing, time scaling, and fixed-interval events.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `DeltaTimeSecs` | `float` | Scaled time since last frame |
| `UnscaledDeltaTimeSecs` | `float` | Raw time since last frame |
| `SmoothDeltaTimeSecs` | `float` | Averaged delta (reduces spikes) |
| `TimeScale` | `float` | Global speed multiplier (default: 1.0) |
| `IsPaused` | `bool` | Whether game time is paused |
| `FrameCount` | `int` | Total frames since start |
| `TimeSinceStartupSecs` | `float` | Scaled time since start |
| `RealtimeSinceStartupSecs` | `double` | Unscaled time since start |
| `CurrentFPS` | `float` | Instantaneous frame rate |
| `AverageFPS` | `float` | Average FPS over last second |
| `FixedDeltaTime` | `float` | Physics timestep (default: 1/60) |
| `MaxDeltaTime` | `float` | Frame time cap (default: 0.1s) |

### Events

| Event | Frequency | Use Case |
|-------|-----------|----------|
| `OnFixedUpdate` | 60 Hz | Physics, deterministic logic |
| `OnFixedUpdate100ms` | 10 Hz | Frequent AI updates |
| `OnFixedUpdate250ms` | 4 Hz | UI refresh |
| `OnFixedUpdate500ms` | 2 Hz | Distant object updates |
| `OnFixedUpdate1000ms` | 1 Hz | Statistics, cleanup |

### Methods

```csharp
// Pause control
static void Pause()
static void Resume()
static void TogglePause()

// Called by game loop (internal)
static void Update(GameTime gameTime)
```

### Usage Example

```csharp
// Frame-rate independent movement
transform.TranslateBy(direction * speed * Time.DeltaTimeSecs);

// Slow motion effect
Time.TimeScale = 0.5f;

// Pause game
Time.Pause();

// Subscribe to fixed interval
Time.OnFixedUpdate += () => {
    // Physics-rate logic
};

Time.OnFixedUpdate1000ms += () => {
    // Once per second
    UpdateStatistics();
};
```

---

## EngineContext

**Namespace:** `GDEngine.Core.Services`

Service hub providing access to shared engine resources.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `GraphicsDevice` | `GraphicsDevice` | XNA graphics device |
| `Content` | `ContentManager` | Asset loading |
| `SpriteBatch` | `SpriteBatch` | Shared 2D renderer |
| `Events` | `EventBus` | Global event bus |
| `Impulses` | `ImpulseBus` | Impulse/shake bus |
| `Instance` | `EngineContext` | Static singleton access |

### Methods

```csharp
// Initialize (call once at startup)
static void Initialize(GraphicsDevice graphicsDevice, ContentManager content)
```

### Usage Example

```csharp
// In Game.Initialize()
EngineContext.Initialize(GraphicsDevice, Content);

// Access from a component
var context = GameObject.Scene.Context;
var texture = context.Content.Load<Texture2D>("Textures/brick");

// Publish an event
context.Events.Publish(new PlayerDiedEvent());
```

---

## Component (Base Class)

**Namespace:** `GDEngine.Core.Components`

Abstract base class for all components.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Enabled` | `bool` | If false, Update/LateUpdate skipped |
| `GameObject` | `GameObject?` | Owning object |
| `Transform` | `Transform?` | Shortcut to owner's transform |

### Events

| Event | Signature | Description |
|-------|-----------|-------------|
| `EnabledChanged` | `Action<Component, bool>` | Fired when Enabled changes |

### Virtual Methods to Override

```csharp
protected virtual void Awake() { }
protected virtual void Start() { }
protected virtual void Update(float deltaTime) { }
protected virtual void LateUpdate(float deltaTime) { }
protected virtual void OnEnabled() { }
protected virtual void OnDisabled() { }
protected virtual void OnDestroy() { }
```

### Usage Example

```csharp
public class HealthComponent : Component
{
    private float _health = 100f;
    
    public float Health => _health;
    public event Action<float>? HealthChanged;
    
    protected override void Awake()
    {
        // Cache references
    }
    
    public void TakeDamage(float amount)
    {
        _health = Math.Max(0, _health - amount);
        HealthChanged?.Invoke(_health);
    }
    
    protected override void OnDestroy()
    {
        HealthChanged = null; // Clear subscribers
    }
}
```

---

## SystemBase (Base Class)

**Namespace:** `GDEngine.Core.Systems`

Abstract base class for scene-level systems.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Scene` | `Scene?` | Owning scene |
| `Lifecycle` | `FrameLifecycle` | When this system runs |
| `Order` | `int` | Priority within lifecycle |
| `Enabled` | `bool` | If false, Update/Draw skipped |
| `Context` | `EngineContext?` | Engine services access |

### Virtual Methods

```csharp
public virtual void Update(float deltaTime) { }
public virtual void Draw(float deltaTime) { }
protected virtual void OnAdded() { }
protected virtual void OnRemoved() { }
```

### Usage Example

```csharp
public class ScoreSystem : SystemBase
{
    private int _score;
    
    public ScoreSystem() : base(FrameLifecycle.Update, order: 100)
    {
    }
    
    protected override void OnAdded()
    {
        // Subscribe to events
        Context?.Events.On<EnemyKilledEvent>()
            .Do(e => _score += e.Points);
    }
    
    public override void Update(float deltaTime)
    {
        // Per-frame logic
    }
}
```

---

## PausableSystemBase

**Namespace:** `GDEngine.Core.Systems`

SystemBase extension that respects pause state.

### Additional Properties

| Property | Type | Description |
|----------|------|-------------|
| `PauseMode` | `PauseMode` | What pauses (Update, Draw, or both) |

### Additional Methods

```csharp
void SetPaused(bool paused)
```

### Virtual Methods

```csharp
protected virtual void OnUpdate(float deltaTime) { }
protected virtual void OnDraw(float deltaTime) { }
```

### PauseMode Flags

```csharp
[Flags]
public enum PauseMode
{
    Update = 1,  // Pause OnUpdate
    Draw = 2     // Pause OnDraw
}
```

### Usage Example

```csharp
public class EnemyAISystem : PausableSystemBase
{
    public EnemyAISystem() : base(FrameLifecycle.Update)
    {
        PauseMode = PauseMode.Update; // AI stops when paused
    }
    
    protected override void OnUpdate(float deltaTime)
    {
        // Only runs when not paused
        UpdateAllEnemies(deltaTime);
    }
}
```
