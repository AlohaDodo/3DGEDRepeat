# Components Reference

## Overview

Components are behaviours attached to GameObjects. This reference covers the built-in components provided by the engine.

---

## Camera

**Namespace:** `GDEngine.Core.Components`

Defines a viewpoint for rendering with projection settings and layer culling.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `View` | `Matrix` | View matrix (auto-calculated) |
| `Projection` | `Matrix` | Projection matrix (auto-calculated) |
| `ViewProjection` | `Matrix` | Combined matrix |
| `FieldOfView` | `float` | Perspective FOV in radians |
| `AspectRatio` | `float` | Width / height ratio |
| `NearPlane` | `float` | Near clip distance |
| `FarPlane` | `float` | Far clip distance |
| `OrthographicSize` | `float` | Ortho half-height (world units) |
| `ProjectionMode` | `ProjectionType` | Perspective or Orthographic |
| `CullingMask` | `LayerMask` | Which layers to render |
| `ClearFlags` | `ClearFlagsType` | How to clear before rendering |
| `ClearColor` | `Color` | Background color |
| `StackRole` | `StackType` | Base or Overlay |
| `Depth` | `int` | Sort order within stack role |
| `Viewport` | `Viewport?` | Optional pixel-space viewport |

### Enums

```csharp
public enum ProjectionType : sbyte
{
    Perspective = 0,
    Orthographic = 1
}

public enum ClearFlagsType : sbyte
{
    Skybox = 0,    // Reserved
    Color = 1,     // Clear with ClearColor
    DepthOnly = 2, // Clear depth only
    None = 3       // No clear (for overlays)
}

public enum StackType : sbyte
{
    Base = 0,      // Main camera
    Overlay = 1    // Rendered on top
}
```

### Methods

```csharp
void ToggleProjection()
Viewport GetViewport(GraphicsDevice graphicsDevice)
float GetAspectRatio()
```

### Usage

```csharp
// Create camera
var cameraGO = new GameObject("MainCamera");
var camera = cameraGO.AddComponent<Camera>();

// Configure
camera.FieldOfView = MathHelper.ToRadians(60f);
camera.NearPlane = 0.1f;
camera.FarPlane = 500f;
camera.ClearColor = Color.Black;
camera.CullingMask = LayerMask.World | LayerMask.Player;

// Position
cameraGO.Transform.TranslateTo(new Vector3(0, 5, -10));

// Add to scene and set active
scene.Add(cameraGO);
scene.SetActiveCamera(camera);
```

### Multiple Cameras

```csharp
// Main camera (Base)
mainCamera.StackRole = Camera.StackType.Base;
mainCamera.Depth = 0;
mainCamera.ClearFlags = Camera.ClearFlagsType.Color;

// Overlay camera (e.g., minimap)
overlayCamera.StackRole = Camera.StackType.Overlay;
overlayCamera.Depth = 10;
overlayCamera.ClearFlags = Camera.ClearFlagsType.None;
overlayCamera.Viewport = new Viewport(10, 10, 200, 200);
```

---

## MeshRenderer

**Namespace:** `GDEngine.Core.Rendering`

Renders a 3D mesh using a material.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Material` | `Material?` | Material for rendering |
| `Overrides` | `EffectPropertyBlock` | Per-instance property overrides |

### Methods

```csharp
void Render(GraphicsDevice device, Camera camera)
void Draw(GraphicsDevice device, Camera camera)  // Alias for Render
```

### Requirements

- Requires a `MeshFilter` component on the same GameObject
- Auto-registers with Scene during `Start()`

### Usage

```csharp
var cube = new GameObject("Cube");

// Add mesh data
var meshFilter = cube.AddComponent<MeshFilter>();
meshFilter.SetMesh(cubeMesh);

// Add renderer with material
var renderer = cube.AddComponent<MeshRenderer>();
renderer.Material = new Material(basicEffect);

// Optional: per-instance overrides
renderer.Overrides.SetColor("DiffuseColor", Color.Red);

scene.Add(cube);
```

---

## MeshFilter

**Namespace:** `GDEngine.Core.Rendering`

Holds mesh geometry (vertices, indices) for rendering.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `PrimitiveType` | `PrimitiveType` | Triangle list, line list, etc. |
| `PrimitiveCount` | `int` | Number of primitives |

### Methods

```csharp
void SetMesh(VertexBuffer vertices, IndexBuffer indices, 
             PrimitiveType type, int primitiveCount)
void BindBuffers(GraphicsDevice device)
```

### Usage

```csharp
var meshFilter = gameObject.AddComponent<MeshFilter>();

// From pre-built buffers
meshFilter.SetMesh(vertexBuffer, indexBuffer, 
                   PrimitiveType.TriangleList, triangleCount);

// Using factory
var filter = MeshFilterFactory.CreateBox(graphicsDevice, 1f, 1f, 1f);
```

---

## RigidBody

**Namespace:** `GDEngine.Core.Components`

Connects a GameObject to physics simulation.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `BodyType` | `BodyType` | Static, Kinematic, or Dynamic |
| `Mass` | `float` | Mass in kg (Dynamic only) |
| `UseGravity` | `bool` | Affected by gravity (Dynamic only) |
| `LinearDamping` | `float` | Linear velocity damping (0-1) |
| `AngularDamping` | `float` | Angular velocity damping (0-1) |
| `LinearVelocity` | `Vector3` | Current linear velocity |
| `AngularVelocity` | `Vector3` | Current angular velocity |

### BodyType Enum

```csharp
public enum BodyType : byte
{
    Static = 0,     // Immovable (walls, floors)
    Kinematic = 1,  // Code-controlled (platforms, doors)
    Dynamic = 2     // Physics-driven (projectiles, debris)
}
```

### Methods

```csharp
void AddForce(Vector3 force)
void AddForce(Vector3 force, Vector3 position)
void AddImpulse(Vector3 impulse)
void AddTorque(Vector3 torque)
void SetPosition(Vector3 position)
void SetRotation(Quaternion rotation)
```

### Requirements

- Requires a `Collider` component on the same GameObject
- Requires `PhysicsSystem` in the scene

### Usage

```csharp
var ball = new GameObject("Ball");

// Add collider first
var collider = ball.AddComponent<SphereCollider>();
collider.Radius = 0.5f;

// Add rigid body
var rb = ball.AddComponent<RigidBody>();
rb.BodyType = BodyType.Dynamic;
rb.Mass = 1f;
rb.UseGravity = true;
rb.LinearDamping = 0.1f;

scene.Add(ball);

// Apply forces
rb.AddForce(Vector3.Up * 500f);        // Continuous force
rb.AddImpulse(Vector3.Forward * 10f);  // Instant impulse
```

### Kinematic Bodies

```csharp
// For moving platforms, animated doors
rb.BodyType = BodyType.Kinematic;

// Move via transform (physics will follow)
transform.TranslateBy(movement * Time.DeltaTimeSecs);
```

---

## Collider (Abstract)

**Namespace:** `GDEngine.Core.Components`

Base class for collision shapes.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Center` | `Vector3` | Offset from transform origin |
| `IsTrigger` | `bool` | If true, detects overlaps only |

### Concrete Types

#### BoxCollider

```csharp
var box = gameObject.AddComponent<BoxCollider>();
box.Size = new Vector3(2f, 1f, 3f);  // Width, height, depth
box.Center = Vector3.Zero;
```

#### SphereCollider

```csharp
var sphere = gameObject.AddComponent<SphereCollider>();
sphere.Radius = 0.5f;
sphere.Center = Vector3.Zero;
```

#### CapsuleCollider

```csharp
var capsule = gameObject.AddComponent<CapsuleCollider>();
capsule.Radius = 0.3f;
capsule.Height = 1.8f;  // Total height including caps
capsule.Center = new Vector3(0, 0.9f, 0);  // Offset to stand on ground
```

### Trigger Volumes

```csharp
var trigger = new GameObject("TriggerZone");

var collider = trigger.AddComponent<BoxCollider>();
collider.Size = new Vector3(5f, 3f, 5f);
collider.IsTrigger = true;

var rb = trigger.AddComponent<RigidBody>();
rb.BodyType = BodyType.Static;

// Handle trigger events via EventBus
```

---

## NavMeshAgent

**Namespace:** `GDEngine.Core.Components.Navigation`

Pathfinding movement on a navigation mesh.

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Speed` | `float` | Movement speed |
| `StoppingDistance` | `float` | Distance to stop from target |
| `HasPath` | `bool` | Whether currently following a path |
| `DebugPath` | `IReadOnlyList<Vector3>` | Current path (for visualization) |

### Methods

```csharp
void SetDestination(Vector3 target)
void Stop()
```

### Requirements

- Requires `NavMeshSystem` in the scene
- Navigation mesh must be built

### Usage

```csharp
var enemy = new GameObject("Enemy");
var agent = enemy.AddComponent<NavMeshAgent>();
agent.Speed = 3f;
agent.StoppingDistance = 1f;

scene.Add(enemy);

// Move to target
agent.SetDestination(playerPosition);

// Check arrival
if (!agent.HasPath)
{
    // Arrived or no path found
}
```

---

## UI Components

### UIRenderer (Abstract Base)

Base class for all UI components. Auto-registers with `UIRenderSystem`.

### UIText

Renders text using SpriteFont.

```csharp
var textGO = new GameObject("ScoreText");
var text = textGO.AddComponent<UIText>();
text.Font = spriteFont;
text.Text = "Score: 0";
text.Position = new Vector2(10, 10);
text.Color = Color.White;
text.LayerDepth = 0.5f;
```

### UITexture

Renders a 2D texture/sprite.

```csharp
var iconGO = new GameObject("HealthIcon");
var icon = iconGO.AddComponent<UITexture>();
icon.Texture = healthTexture;
icon.Position = new Vector2(50, 50);
icon.Size = new Vector2(32, 32);
icon.Tint = Color.White;
```

### UIButton

Interactive button with click handling.

```csharp
var buttonGO = new GameObject("StartButton");
var button = buttonGO.AddComponent<UIButton>();
button.Texture = buttonTexture;
button.Font = spriteFont;
button.Text = "Start";
button.Position = new Vector2(400, 300);
button.Size = new Vector2(200, 50);
button.OnClick += () => StartGame();
```

### UISlider

Draggable slider control.

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
slider.OnValueChanged += volume => SetVolume(volume);
```

### UIMenuPanel

Container for menu items with automatic layout.

```csharp
var menuGO = new GameObject("MainMenu");
var panel = menuGO.AddComponent<UIMenuPanel>();
panel.PanelPosition = new Vector2(100, 100);
panel.ItemSize = new Vector2(200, 40);
panel.VerticalSpacing = 10f;

panel.AddButton("Play", buttonTex, font, OnPlayClick);
panel.AddButton("Options", buttonTex, font, OnOptionsClick);
panel.AddSlider("Volume", trackTex, handleTex, font, 0, 1, 0.8f, OnVolumeChange);

panel.IsVisible = true;
```

### UIReticle

Crosshair/targeting reticle.

```csharp
var hudGO = new GameObject("HUD");
var reticle = hudGO.AddComponent<UIReticle>();
reticle.Texture = reticleTexture;
reticle.Size = new Vector2(32, 32);
reticle.Tint = Color.White;
// Automatically centers on screen
```

### UIDebugInfo

Debug text overlay.

```csharp
var debugGO = new GameObject("Debug");
var debug = debugGO.AddComponent<UIDebugInfo>();
debug.Font = debugFont;
debug.Position = new Vector2(10, 10);

// Add info providers
debug.AddProvider(sceneManager);       // IShowDebugInfo
debug.AddProvider(physicsSystem);
debug.AddProvider(gameStateSystem);
```

---

## Material

**Namespace:** `GDEngine.Core.Rendering`

Wraps an Effect with property management.

### Constructor

```csharp
Material(Effect effect)
```

### Methods

```csharp
void Apply(GraphicsDevice device, Matrix world, Matrix view, Matrix projection,
           EffectPropertyBlock overrides, Action drawCall)

void SetTexture(string name, Texture2D texture)
void SetFloat(string name, float value)
void SetVector3(string name, Vector3 value)
void SetMatrix(string name, Matrix value)
```

### Usage

```csharp
// Create from BasicEffect
var effect = new BasicEffect(graphicsDevice);
effect.TextureEnabled = true;
effect.Texture = brickTexture;

var material = new Material(effect);

// Assign to renderer
meshRenderer.Material = material;
```

---

## EffectPropertyBlock

**Namespace:** `GDEngine.Core.Rendering`

Per-instance material property overrides.

### Methods

```csharp
void SetFloat(string name, float value)
void SetVector3(string name, Vector3 value)
void SetColor(string name, Color color)
void SetTexture(string name, Texture2D texture)
void SetMatrix(string name, Matrix value)
void Clear()
```

### Usage

```csharp
// Different tints using same material
renderer1.Material = sharedMaterial;
renderer1.Overrides.SetColor("DiffuseColor", Color.Red);

renderer2.Material = sharedMaterial;
renderer2.Overrides.SetColor("DiffuseColor", Color.Blue);
```

---

## LayerMask

**Namespace:** `GDEngine.Core.Rendering.Base`

Bitfield for categorizing and filtering objects.

### Predefined Values

```csharp
public static LayerMask None     // 0 - No layers
public static LayerMask World    // 1 - Default world objects
public static LayerMask Player   // 2 - Player objects
public static LayerMask Enemy    // 4 - Enemy objects
public static LayerMask UI       // 8 - UI elements
public static LayerMask Ignore   // 16 - Ignored by default
public static LayerMask All      // All bits set
```

### Methods

```csharp
bool Overlaps(LayerMask other)
bool Contains(LayerMask layer)
```

### Usage

```csharp
// Assign layer
enemy.Layer = LayerMask.Enemy;

// Camera culling
camera.CullingMask = LayerMask.World | LayerMask.Player;

// Raycast filtering
if (physics.Raycast(origin, dir, 100f, out hit, LayerMask.Enemy))
{
    // Only hits enemies
}
```
