# Systems Reference

## Overview

Systems are scene-level managers that process components or coordinate engine features. Each system runs in a specific `FrameLifecycle` phase.

---

## EventSystem

**Namespace:** `GDEngine.Core.Systems`  
**Lifecycle:** `EarlyUpdate`  
**Order:** -1000 (runs first)

Dispatches queued events from the `EventBus` once per frame.

### Constructor

```csharp
EventSystem(EventBus bus, int order = -1000)
```

### Purpose

Ensures all events posted since the last frame are delivered to subscribers before game logic runs.

### Setup

```csharp
var eventBus = context.Events;
scene.Add(new EventSystem(eventBus));
```

### Notes

- Must be added before systems that depend on events
- Events are queued when published, dispatched in batch

---

## InputSystem

**Namespace:** `GDEngine.Core.Systems`  
**Lifecycle:** `EarlyUpdate`  
**Order:** 0

Polls input devices and delivers input to registered receivers.

### Constructor

```csharp
InputSystem()
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `DeviceCount` | `int` | Number of registered devices |
| `ReceiverCount` | `int` | Number of registered receivers |

### Methods

```csharp
// Device management
void Add(IInputDevice device)
bool Remove(IInputDevice device)

// Receiver management  
void Add(IInputReceiver receiver)
bool Remove(IInputReceiver receiver)

// Factory
static InputSystem CreateDefault()  // Keyboard + Gamepad
```

### Setup

```csharp
var inputSystem = InputSystem.CreateDefault();
scene.Add(inputSystem);

// Or manual setup
var inputSystem = new InputSystem();
inputSystem.Add(new GDKeyboardInput());
inputSystem.Add(new GDMouseInput());
inputSystem.Add(new GDGamepadInput());
```

### Implementing IInputReceiver

```csharp
public class PlayerController : Component, IInputReceiver
{
    protected override void Start()
    {
        var inputSystem = GameObject.Scene.GetSystem<InputSystem>();
        inputSystem?.Add(this);
    }
    
    public void OnAxis(string axisName, float value)
    {
        // Handle analog input
    }
    
    public void OnButtonDown(string buttonName)
    {
        // Handle button press
    }
    
    protected override void OnDestroy()
    {
        var inputSystem = GameObject?.Scene?.GetSystem<InputSystem>();
        inputSystem?.Remove(this);
    }
}
```

---

## OrchestrationSystem

**Namespace:** `GDEngine.Core.Systems`  
**Lifecycle:** `Update`  
**Order:** 0

Drives timeline-based animations and sequences via the `Orchestrator`.

### Constructor

```csharp
OrchestrationSystem(int order = 0)
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Orchestrator` | `Orchestrator` | The underlying orchestrator instance |
| `ShowPerSequenceDebug` | `bool` | Include per-sequence info in debug output |

### Methods

```csharp
void Configure(Action<Orchestrator.OrchestratorOptions> configure)
void SetEventPublisher(Action<object> publish)
```

### Setup

```csharp
var orchestrationSystem = new OrchestrationSystem();
scene.Add(orchestrationSystem);

// Access orchestrator for sequences
var orchestrator = orchestrationSystem.Orchestrator;
```

### Creating Sequences

```csharp
var orchestrator = orchestrationSystem.Orchestrator;

// Simple tween
orchestrator.Sequence()
    .MoveTo(transform, targetPosition, duration: 1.5f, Ease.SmoothStep)
    .Then()
    .RotateTo(transform, targetRotation, duration: 1.0f)
    .Play();

// With callbacks
orchestrator.Sequence()
    .Wait(0.5f)
    .Do(() => PlaySound("click"))
    .FadeOut(uiElement, duration: 0.3f)
    .Play();
```

---

## GameStateSystem

**Namespace:** `GDEngine.Core.Systems`  
**Lifecycle:** `Update`  
**Order:** 0  
**Base:** `PausableSystemBase`

Manages high-level game state (InProgress, Won, Lost, Paused) and evaluates win/lose conditions.

### Constructor

```csharp
GameStateSystem(int order = 0)
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `State` | `GameOutcomeState` | Current game state |
| `WinCondition` | `IGameCondition?` | Condition for winning |
| `LoseCondition` | `IGameCondition?` | Condition for losing |
| `ShowConditionTrees` | `bool` | Show conditions in debug |
| `ShowOnlyFailingConditions` | `bool` | Filter debug output |

### Events

| Event | Signature | Description |
|-------|-----------|-------------|
| `StateChanged` | `Action<GameOutcomeState, GameOutcomeState>` | Old and new state |

### Methods

```csharp
void ConfigureConditions(IGameCondition? win, IGameCondition? lose)
void Reset()
void Pause()
void Resume()
```

### GameOutcomeState Enum

```csharp
public enum GameOutcomeState
{
    InProgress,
    Won,
    Lost,
    Paused
}
```

### Setup

```csharp
var gameState = new GameStateSystem();
scene.Add(gameState);

// Configure conditions
gameState.ConfigureConditions(
    winCondition: new AllItemsCollectedCondition(inventory),
    loseCondition: new TimerExpiredCondition(timer)
);

// React to state changes
gameState.StateChanged += (oldState, newState) => {
    if (newState == GameOutcomeState.Won)
        ShowVictoryScreen();
};
```

---

## PhysicsSystem

**Namespace:** `GDEngine.Core.Systems`  
**Lifecycle:** `LateUpdate`  
**Order:** 1000 (runs late)  
**Base:** `PausableSystemBase`

BepuPhysics v2 simulation manager. Handles collision detection, rigid body dynamics, and raycasting.

### Constructor

```csharp
PhysicsSystem(int order = 1000)
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Gravity` | `Vector3` | Global gravity (default: 0, -9.81, 0) |
| `Simulation` | `Simulation` | BepuPhysics simulation instance |
| `VelocityIterations` | `int` | Solver iterations |
| `SubstepCount` | `int` | Physics substeps per frame |
| `FixedTimestep` | `float` | Fixed step size (-1 = variable) |
| `LastStepDt` | `float` | Actual timestep of last update |

### Methods

```csharp
// Raycasting
bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, 
             out RayHit hit, LayerMask mask = LayerMask.All)

// Shape queries
bool SphereCast(Vector3 origin, float radius, Vector3 direction, 
                float maxDistance, out RayHit hit)

// Body management (internal, used by RigidBody component)
```

### Setup

```csharp
var physicsSystem = new PhysicsSystem();
physicsSystem.Gravity = new Vector3(0, -15f, 0); // Stronger gravity
scene.Add(physicsSystem);
```

### Raycasting Example

```csharp
var physics = scene.GetSystem<PhysicsSystem>();

if (physics.Raycast(origin, direction, 100f, out var hit))
{
    var hitObject = hit.GameObject;
    var hitPoint = hit.Position;
    var hitNormal = hit.Normal;
}
```

---

## AudioSystem

**Namespace:** `GDEngine.Core.Systems`  
**Lifecycle:** `Update`  
**Order:** 0

Central audio controller for SFX, music, and volume mixing.

### Constructor

```csharp
AudioSystem(ContentDictionary<SoundEffect> sounds, int order = 0)
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Mixer` | `AudioMixer` | Volume mixer for channels |

### Audio Channels

```csharp
public enum AudioChannel : sbyte
{
    Master = 0,
    Music = 1,
    Sfx = 2,
    Ui = 3
}
```

### Methods

```csharp
// SFX
void PlayOneShot(string clipId, float volume = 1f)
void PlayOneShot3D(string clipId, Transform emitter, float volume = 1f)
void StopAllSfx()

// Music
void PlayMusic(string clipId, float volume = 1f, float fadeIn = 0f, bool loop = true)
void StopMusic(float fadeOut = 0f)

// Mixer
void SetVolume(AudioChannel channel, float volume)
void FadeTo(AudioChannel channel, float target, float duration)
```

### Event-Based Playback

```csharp
// Via EventBus
context.Events.Publish(new PlaySfxEvent("explosion", volume: 0.8f));
context.Events.Publish(new PlaySfxEvent("footstep", volume: 0.5f, 
                                        spatial: true, emitter: transform));
context.Events.Publish(new PlayMusicEvent("ambient", fadeInSeconds: 2f));
context.Events.Publish(new StopMusicEvent(fadeOutSeconds: 1f));
```

### Setup

```csharp
var sounds = new ContentDictionary<SoundEffect>(content);
sounds.Add("explosion", "Audio/explosion");
sounds.Add("footstep", "Audio/footstep");
sounds.Add("ambient", "Audio/ambient_music");

var audioSystem = new AudioSystem(sounds);
scene.Add(audioSystem);
```

### Spatial Audio

```csharp
// 3D positioned sound
audioSystem.PlayOneShot3D("explosion", enemyTransform, volume: 1f);

// The AudioSystem updates its listener position from ActiveCamera
```

---

## CameraSystem

**Namespace:** `GDEngine.Core.Systems`  
**Lifecycle:** `Render`  
**Order:** -100 (before RenderSystem)

Manages cameras, handles aspect ratio sync, and provides utility methods.

### Constructor

```csharp
CameraSystem(GraphicsDevice graphicsDevice, int order = -100)
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `ActiveCamera` | `Camera?` | Currently active camera |
| `Cameras` | `IReadOnlyList<Camera>` | All registered cameras |

### Methods

```csharp
// Camera management
void Add(Camera camera)
void Remove(Camera camera)
void GetSortedStack(List<Camera> destinationList)

// Coordinate conversion
Vector3 ScreenToWorld(Camera camera, Vector3 screenPoint)
Vector3 WorldToScreen(Camera camera, Vector3 worldPoint)
Ray ScreenPointToRay(Camera camera, Vector2 screenPixel)

// Rendering helpers
void ApplyClears(Camera camera)
void BuildVisibleSet(Camera camera, IEnumerable<MeshRenderer> all, List<MeshRenderer> visible)
```

### Setup

```csharp
var cameraSystem = new CameraSystem(graphicsDevice);
scene.Add(cameraSystem);

// Cameras auto-register via their Awake() method
```

### Picking Example

```csharp
var cameraSystem = scene.GetSystem<CameraSystem>();
var camera = scene.ActiveCamera;

// Get ray from mouse position
var mousePos = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
Ray ray = cameraSystem.ScreenPointToRay(camera, mousePos);

// Use for raycasting
if (physicsSystem.Raycast(ray.Position, ray.Direction, 1000f, out var hit))
{
    // Hit something
}
```

---

## RenderSystem

**Namespace:** `GDEngine.Core.Systems`  
**Lifecycle:** `Render`  
**Order:** 0

Renders the scene for all cameras (or just active camera).

### Constructor

```csharp
RenderSystem(int order = -100)
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Layout` | `RenderLayout` | SingleActive or FullStack |

### RenderLayout Enum

```csharp
public enum RenderLayout : sbyte
{
    SingleActive = 0,  // Render only ActiveCamera
    FullStack = 1      // Render all cameras in stack order
}
```

### Setup

```csharp
var renderSystem = new RenderSystem();
renderSystem.Layout = RenderSystem.RenderLayout.SingleActive;
scene.Add(renderSystem);
```

---

## UIRenderSystem

**Namespace:** `GDEngine.Core.Systems`  
**Lifecycle:** `PostRender`  
**Order:** 10 (after 3D rendering)

Renders UI overlays via SpriteBatch.

### Constructor

```csharp
UIRenderSystem(int order = 10)
```

### Methods

```csharp
void Add(UIRenderer renderer)
void Remove(UIRenderer renderer)
```

### Notes

- UIRenderers auto-register via their lifecycle
- Maintains active/inactive lists for efficiency
- Assumes full backbuffer viewport restored by RenderSystem

### Setup

```csharp
scene.Add(new UIRenderSystem());

// UIRenderer components auto-register
```

---

## ImpulseSystem

**Namespace:** `GDEngine.Core.Systems`  
**Lifecycle:** `LateUpdate`  
**Order:** -1000 (before physics)

Dispatches impulses (camera shake, screen effects) from ImpulseBus.

### Constructor

```csharp
ImpulseSystem(ImpulseBus bus, int order = -1000)
```

### Setup

```csharp
var impulseBus = context.Impulses;
scene.Add(new ImpulseSystem(impulseBus));
```

### Usage

```csharp
// Send camera shake
context.Impulses.Send(new Eased3DImpulse(
    intensity: new Vector3(0.1f, 0.1f, 0f),
    duration: 0.3f,
    ease: Ease.SmoothStep
));
```

---

## NavMeshSystem

**Namespace:** `GDEngine.Core.Systems`  
**Lifecycle:** `Update`  
**Order:** 0  
**Base:** `PausableSystemBase`

Provides pathfinding via A* on a navigation mesh.

### Constructor

```csharp
NavMeshSystem(int order = 0)
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `NavMesh` | `NavMesh` | The navigation graph |

### Methods

```csharp
// Building
void BuildWith(INavMeshBuilder builder)
void BuildFlatGrid(int width, int height, float cellSize, Vector3 origin, 
                   Func<Vector3, bool>? isWalkable = null)

// Pathfinding
bool TryFindPath(Vector3 start, Vector3 end, List<Vector3> outPath)
IReadOnlyList<Vector3>? TryFindPath(Vector3 start, Vector3 end)
```

### Setup

```csharp
var navSystem = new NavMeshSystem();
scene.Add(navSystem);

// Build a flat grid
navSystem.BuildFlatGrid(
    width: 20, 
    height: 20, 
    cellSize: 1f, 
    origin: Vector3.Zero,
    isWalkable: pos => !IsObstacle(pos)
);
```

### Pathfinding Example

```csharp
var navSystem = scene.GetSystem<NavMeshSystem>();
var path = new List<Vector3>();

if (navSystem.TryFindPath(startPos, endPos, path))
{
    // path contains waypoints
    foreach (var waypoint in path)
    {
        // Move towards waypoint
    }
}
```

---

## System Setup Summary

Typical scene setup order:

```csharp
// EarlyUpdate systems
scene.Add(new EventSystem(context.Events));
scene.Add(new InputSystem());

// Update systems
scene.Add(new OrchestrationSystem());
scene.Add(new GameStateSystem());
scene.Add(new AudioSystem(sounds));

// LateUpdate systems
scene.Add(new ImpulseSystem(context.Impulses));
scene.Add(new PhysicsSystem());

// Render systems
scene.Add(new CameraSystem(graphicsDevice));
scene.Add(new RenderSystem());

// PostRender systems
scene.Add(new UIRenderSystem());
```
