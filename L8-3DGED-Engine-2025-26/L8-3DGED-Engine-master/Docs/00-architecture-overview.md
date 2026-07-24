# GDEngine Architecture Overview

## Introduction

GDEngine is a 3D game engine built on MonoGame/XNA Framework following a **Unity-inspired Entity-Component-System (ECS)** architecture. This document provides a high-level overview of the engine's structure and how its parts connect.

## Core Concepts

### The Three Pillars

| Concept | Description |
|---------|-------------|
| **GameObject** | A container that holds components. Every object in your scene is a GameObject. |
| **Component** | A behaviour or data attached to a GameObject. Components define what an object *is* and *does*. |
| **System** | A manager that operates across the scene, processing components or coordinating engine features. |

### How They Relate

```
Scene
├── Systems (process the whole scene)
│   ├── InputSystem
│   ├── EventSystem
│   ├── PhysicsSystem
│   ├── AudioSystem
│   ├── CameraSystem
│   ├── RenderSystem
│   └── ...
│
└── GameObjects (individual entities)
    ├── GameObject: "Player"
    │   ├── Transform (always present)
    │   ├── Camera
    │   └── PlayerController
    │
    ├── GameObject: "Door"
    │   ├── Transform
    │   ├── MeshRenderer
    │   ├── BoxCollider
    │   └── RigidBody
    │
    └── GameObject: "Light"
        ├── Transform
        └── Light
```

## Class Hierarchy

### Components

```
Component (abstract base)
├── Transform          - Position, rotation, scale with hierarchy
├── Camera             - View/projection, layer culling
├── MeshRenderer       - 3D mesh drawing
├── RigidBody          - Physics body (static/kinematic/dynamic)
├── Collider (abstract)
│   ├── BoxCollider
│   ├── SphereCollider
│   └── CapsuleCollider
├── UIRenderer (abstract)
│   ├── UIText
│   ├── UITexture
│   ├── UIButton
│   ├── UISlider
│   └── ...
└── NavMeshAgent       - Pathfinding movement
```

### Systems

```
SystemBase (abstract base)
├── InputSystem        - Polls input devices, feeds receivers
├── EventSystem        - Dispatches EventBus queue
├── OrchestrationSystem - Timeline/tween animations
├── GameStateSystem    - Win/lose condition tracking
├── NavMeshSystem      - Pathfinding graph
├── AudioSystem        - SFX, music, mixer
├── ImpulseSystem      - Camera shake dispatch
├── CameraSystem       - Camera management
├── RenderSystem       - 3D rendering
└── UIRenderSystem     - 2D overlay rendering

PausableSystemBase : SystemBase
├── PhysicsSystem      - BepuPhysics simulation
├── GameStateSystem    - Respects pause
└── NavMeshSystem      - Respects pause
```

### Managers

```
GameComponent (MonoGame)
├── SceneManager       - Manages multiple scenes
└── MenuManager        - UI panel state machine
```

## Frame Lifecycle

Every frame, systems execute in a **deterministic order** defined by `FrameLifecycle`:

```
┌─────────────────────────────────────────────────────────────┐
│  FRAME START                                                │
├─────────────────────────────────────────────────────────────┤
│  1. EarlyUpdate    │ Input polling, event dispatch          │
│                    │ (InputSystem, EventSystem)             │
├─────────────────────────────────────────────────────────────┤
│  2. Update         │ Game logic, AI, orchestration          │
│                    │ (OrchestrationSystem, GameStateSystem) │
├─────────────────────────────────────────────────────────────┤
│  3. LateUpdate     │ Physics, transform finalization        │
│                    │ (PhysicsSystem, ImpulseSystem)         │
├─────────────────────────────────────────────────────────────┤
│  Component Lifecycle: Start → Update → LateUpdate           │
├─────────────────────────────────────────────────────────────┤
│  4. Render         │ 3D drawing                             │
│                    │ (CameraSystem, RenderSystem)           │
├─────────────────────────────────────────────────────────────┤
│  5. PostRender     │ UI overlay, debug visualization        │
│                    │ (UIRenderSystem)                       │
├─────────────────────────────────────────────────────────────┤
│  FRAME END                                                  │
└─────────────────────────────────────────────────────────────┘
```

Within each lifecycle phase, systems are sorted by their `Order` property (lower runs first).

## Component Lifecycle

Components follow a Unity-like lifecycle:

```
┌──────────────────┐
│     Awake()      │  Called once when added to scene
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│     Start()      │  Called once before first Update
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│    Update()      │◄─────┐  Called every frame
└────────┬─────────┘      │
         │                │
         ▼                │
┌──────────────────┐      │
│  LateUpdate()    │──────┘  Called after all Updates
└────────┬─────────┘
         │
         ▼ (when destroyed)
┌──────────────────┐
│   OnDestroy()    │  Cleanup, unsubscribe events
└──────────────────┘
```

Additional lifecycle hooks:
- `OnEnabled()` - Called when `Enabled` changes to `true`
- `OnDisabled()` - Called when `Enabled` changes to `false`

## Messaging Architecture

### EventBus (Decoupled Events)

For communication between unrelated systems:

```
Publisher                          Subscriber
    │                                  │
    │  Publish(new DoorOpenedEvent())  │
    │ ──────────────────────────────►  │
    │                                  │
    │         EventBus                 │
    │    (queued, dispatched           │
    │     once per frame)              │
    │                                  │
```

### ImpulseBus (Transient Effects)

For short-lived effects like camera shake:

```
Source                             Listener
    │                                  │
    │   Send(new ShakeImpulse())       │
    │ ──────────────────────────────►  │
    │                                  │
    │       ImpulseBus                 │
    │   (immediate or continuous)      │
    │                                  │
```

### Direct Events (C# events)

For tightly-coupled communication:

```csharp
// Publisher
public event Action<float> HealthChanged;

// Subscriber
healthComponent.HealthChanged += OnHealthChanged;
```

## Service Locator: EngineContext

`EngineContext` provides access to shared engine services:

```csharp
// Access via Scene
var context = scene.Context;

// Available services
context.GraphicsDevice  // XNA graphics device
context.Content         // Content manager for loading assets
context.SpriteBatch     // Shared SpriteBatch for 2D rendering
context.Events          // EventBus instance
context.Impulses        // ImpulseBus instance
```

## Time Management

The static `Time` class provides frame timing:

```csharp
Time.DeltaTimeSecs         // Scaled frame time
Time.UnscaledDeltaTimeSecs // Raw frame time (ignores TimeScale)
Time.TimeScale             // Global speed multiplier (0 = paused)
Time.IsPaused              // Pause state
```

## Scene Structure

A typical scene setup:

```csharp
// Create scene with engine context
var scene = new Scene(context, "GameScene");

// Add systems (order matters within lifecycle)
scene.Add(new EventSystem(context.Events));
scene.Add(new InputSystem());
scene.Add(new PhysicsSystem());
scene.Add(new CameraSystem(graphicsDevice));
scene.Add(new RenderSystem());
scene.Add(new UIRenderSystem());

// Add game objects
var player = new GameObject("Player");
player.AddComponent<Camera>();
player.AddComponent<PlayerController>();
scene.Add(player);
```

## Layer System

GameObjects can be assigned to layers for filtering:

```csharp
gameObject.Layer = LayerMask.World;    // Default layer
gameObject.Layer = LayerMask.UI;       // UI layer
gameObject.Layer = LayerMask.Ignore;   // Excluded from rendering
```

Cameras use `CullingMask` to filter which layers they render.

## Next Steps

- See **Component Reference** for individual component documentation
- See **System Reference** for system-specific details
- See **Task Guides** for common implementation patterns
