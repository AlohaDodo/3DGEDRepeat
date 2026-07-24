# Camera Behaviours

## Overview

GDEngine's camera system supports multiple cameras, viewport configurations, and smooth transitions. This guide covers camera setup, common behaviours, and transition techniques.

---

## Camera Setup

### Creating a Camera

```csharp
var cameraGO = new GameObject("MainCamera");
var camera = cameraGO.AddComponent<Camera>();

// Configure projection
camera.FieldOfView = MathHelper.ToRadians(60f);
camera.NearPlane = 0.1f;
camera.FarPlane = 500f;

// Configure clear
camera.ClearFlags = Camera.ClearFlagsType.Color;
camera.ClearColor = Color.CornflowerBlue;

// Add to scene
scene.Add(cameraGO);
scene.SetActiveCamera(camera);
```

### First-Person Camera

```csharp
var fpsCameraGO = new GameObject("FPSCamera");
var camera = fpsCameraGO.AddComponent<Camera>();

camera.FieldOfView = MathHelper.ToRadians(75f);  // Wider FOV
camera.NearPlane = 0.05f;  // Close near plane

// Attach to player
fpsCameraGO.Transform.SetParent(playerTransform);
fpsCameraGO.Transform.TranslateTo(new Vector3(0, 1.7f, 0));  // Eye height
```

### Third-Person Camera

```csharp
var tpsCameraGO = new GameObject("TPSCamera");
var camera = tpsCameraGO.AddComponent<Camera>();

camera.FieldOfView = MathHelper.ToRadians(55f);

// Position behind and above player
tpsCameraGO.Transform.TranslateTo(new Vector3(0, 3f, -5f));

// Look at player
var lookDir = playerTransform.Position - tpsCameraGO.Transform.Position;
tpsCameraGO.Transform.RotateToWorld(
    Quaternion.CreateFromRotationMatrix(
        Matrix.CreateLookAt(Vector3.Zero, lookDir, Vector3.Up)
    )
);
```

---

## CameraSystem Utilities

### Screen-to-World Conversion

```csharp
var cameraSystem = scene.GetSystem<CameraSystem>();
var camera = scene.ActiveCamera;

// Mouse position to world point
var mousePos = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
var worldPoint = cameraSystem.ScreenToWorld(camera, 
    new Vector3(mousePos, 0.5f));  // Z = depth (0-1)
```

### World-to-Screen Conversion

```csharp
// Project world point to screen
Vector3 screenPos = cameraSystem.WorldToScreen(camera, targetWorldPosition);

// Check if in front of camera
if (screenPos.Z > 0 && screenPos.Z < 1)
{
    // Point is visible, use screenPos.X and screenPos.Y for UI
}
```

### Picking Ray

```csharp
// Get ray from mouse position
var mousePos = new Vector2(Mouse.GetState().X, Mouse.GetState().Y);
Ray ray = cameraSystem.ScreenPointToRay(camera, mousePos);

// Use for raycasting
var physics = scene.GetSystem<PhysicsSystem>();
if (physics.Raycast(ray.Position, ray.Direction, 100f, out var hit))
{
    // Hit something at hit.Position
}
```

---

## Camera Transitions

### Using Orchestration

```csharp
public void TransitionCamera(Vector3 targetPos, Quaternion targetRot, float duration)
{
    var cameraTransform = scene.ActiveCamera.Transform;
    
    orchestrator.Sequence()
        .MoveTo(cameraTransform, targetPos, duration, Ease.SmoothStep)
        .With()
        .RotateTo(cameraTransform, targetRot, duration, Ease.SmoothStep)
        .Play();
}
```

### Smooth Follow

```csharp
public class SmoothFollow : Component
{
    public Transform? Target { get; set; }
    public Vector3 Offset { get; set; } = new Vector3(0, 2, -5);
    public float SmoothTime { get; set; } = 0.3f;
    
    private Vector3 _velocity;
    
    protected override void LateUpdate(float deltaTime)
    {
        if (Target == null) return;
        
        var targetPosition = Target.Position + Offset;
        var newPosition = SmoothDamp(
            Transform.Position, 
            targetPosition, 
            ref _velocity, 
            SmoothTime, 
            deltaTime
        );
        
        Transform.TranslateTo(newPosition);
    }
    
    private Vector3 SmoothDamp(Vector3 current, Vector3 target, 
                               ref Vector3 velocity, float smoothTime, float dt)
    {
        float omega = 2f / smoothTime;
        float x = omega * dt;
        float exp = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
        
        Vector3 change = current - target;
        Vector3 temp = (velocity + omega * change) * dt;
        velocity = (velocity - omega * temp) * exp;
        
        return target + (change + temp) * exp;
    }
}
```

### Look-At Target

```csharp
public class LookAtTarget : Component
{
    public Transform? Target { get; set; }
    public float SmoothSpeed { get; set; } = 5f;
    
    protected override void LateUpdate(float deltaTime)
    {
        if (Target == null) return;
        
        var direction = Target.Position - Transform.Position;
        if (direction.LengthSquared() < 0.001f) return;
        
        var targetRotation = Quaternion.CreateFromRotationMatrix(
            Matrix.CreateLookAt(Vector3.Zero, direction, Vector3.Up)
        );
        
        var currentRotation = Transform.Rotation;
        var smoothedRotation = Quaternion.Slerp(
            currentRotation, 
            targetRotation, 
            SmoothSpeed * deltaTime
        );
        
        Transform.RotateToWorld(smoothedRotation);
    }
}
```

---

## Camera Modes

### Mode Switching Pattern

```csharp
public enum CameraMode
{
    FirstPerson,
    ThirdPerson,
    Examination,
    Cinematic
}

public class CameraController : Component
{
    private CameraMode _currentMode = CameraMode.FirstPerson;
    private Sequence? _transitionSequence;
    
    public void SetMode(CameraMode mode)
    {
        if (_currentMode == mode) return;
        
        // Stop any ongoing transition
        _transitionSequence?.Stop();
        
        var oldMode = _currentMode;
        _currentMode = mode;
        
        TransitionToMode(oldMode, mode);
    }
    
    private void TransitionToMode(CameraMode from, CameraMode to)
    {
        switch (to)
        {
            case CameraMode.FirstPerson:
                TransitionToFirstPerson();
                break;
            case CameraMode.ThirdPerson:
                TransitionToThirdPerson();
                break;
            case CameraMode.Examination:
                TransitionToExamination();
                break;
            case CameraMode.Cinematic:
                TransitionToCinematic();
                break;
        }
    }
    
    private void TransitionToFirstPerson()
    {
        _transitionSequence = orchestrator.Sequence()
            .MoveTo(Transform, firstPersonPosition, 0.5f, Ease.SmoothStep)
            .With()
            .Do(() => SetFOV(75f))
            .Play();
    }
    
    private void TransitionToExamination()
    {
        // Store return position
        _returnPosition = Transform.Position;
        _returnRotation = Transform.Rotation;
        
        _transitionSequence = orchestrator.Sequence()
            .MoveTo(Transform, examinePosition, 0.8f, Ease.SmoothStep)
            .With()
            .RotateTo(Transform, examineRotation, 0.8f, Ease.SmoothStep)
            .Play();
    }
}
```

### Examination Camera

```csharp
public class ExaminationCamera : Component
{
    private Camera? _camera;
    private Orchestrator? _orchestrator;
    
    private Vector3 _originalPosition;
    private Quaternion _originalRotation;
    private bool _isExamining;
    
    public void Examine(Transform target)
    {
        if (_isExamining) return;
        _isExamining = true;
        
        // Store original
        _originalPosition = Transform.Position;
        _originalRotation = Transform.Rotation;
        
        // Calculate examine position (in front of object)
        var examinePos = target.Position + target.Forward * 1.5f + Vector3.Up * 0.5f;
        var lookRotation = CalculateLookAt(examinePos, target.Position);
        
        // Transition
        _orchestrator.Sequence()
            .MoveTo(Transform, examinePos, 0.6f, Ease.SmoothStep)
            .With()
            .RotateTo(Transform, lookRotation, 0.6f, Ease.SmoothStep)
            .OnComplete(() => OnExamineReady())
            .Play();
    }
    
    public void ExitExamine()
    {
        if (!_isExamining) return;
        
        _orchestrator.Sequence()
            .MoveTo(Transform, _originalPosition, 0.5f, Ease.SmoothStep)
            .With()
            .RotateTo(Transform, _originalRotation, 0.5f, Ease.SmoothStep)
            .OnComplete(() => {
                _isExamining = false;
                OnExamineExit();
            })
            .Play();
    }
    
    private Quaternion CalculateLookAt(Vector3 from, Vector3 to)
    {
        var direction = Vector3.Normalize(to - from);
        var up = Vector3.Up;
        
        // Handle looking straight down
        if (Math.Abs(Vector3.Dot(direction, up)) > 0.99f)
            up = Vector3.Forward;
        
        var matrix = Matrix.CreateLookAt(Vector3.Zero, direction, up);
        return Quaternion.CreateFromRotationMatrix(Matrix.Invert(matrix));
    }
}
```

---

## Camera Effects

### Field of View Zoom

```csharp
public void ZoomIn(float targetFOV, float duration)
{
    var camera = scene.ActiveCamera;
    float startFOV = camera.FieldOfView;
    
    orchestrator.Sequence()
        .TweenFloat(
            () => camera.FieldOfView,
            fov => camera.FieldOfView = fov,
            targetFOV,
            duration,
            Ease.SmoothStep
        )
        .Play();
}
```

### Camera Shake (via ImpulseBus)

```csharp
public void ShakeCamera(float intensity, float duration)
{
    context.Impulses.Send(new Eased3DImpulse(
        intensity: new Vector3(intensity, intensity, 0),
        duration: duration,
        ease: Ease.SmoothStep
    ));
}

// Different shake types
public void ImpactShake() => ShakeCamera(0.15f, 0.2f);
public void ExplosionShake() => ShakeCamera(0.3f, 0.5f);
public void EarthquakeShake() => ShakeCamera(0.1f, 2f);
```

### Dolly Zoom (Vertigo Effect)

```csharp
public void DollyZoom(Transform target, float duration)
{
    var camera = scene.ActiveCamera;
    var cameraTransform = camera.Transform;
    
    float initialDistance = Vector3.Distance(cameraTransform.Position, target.Position);
    float initialFOV = camera.FieldOfView;
    
    // Calculate FOV that keeps target same size at half distance
    float targetDistance = initialDistance * 0.5f;
    float targetFOV = 2f * MathF.Atan(
        MathF.Tan(initialFOV / 2f) * (initialDistance / targetDistance)
    );
    
    orchestrator.Sequence()
        .TweenFloat(
            () => Vector3.Distance(cameraTransform.Position, target.Position),
            d => {
                var dir = Vector3.Normalize(cameraTransform.Position - target.Position);
                cameraTransform.TranslateTo(target.Position + dir * d);
            },
            targetDistance,
            duration,
            Ease.SmoothStep
        )
        .With()
        .TweenFloat(
            () => camera.FieldOfView,
            fov => camera.FieldOfView = fov,
            targetFOV,
            duration,
            Ease.SmoothStep
        )
        .Play();
}
```

---

## Multiple Cameras

### Overlay Camera (Minimap/PiP)

```csharp
// Main camera
mainCamera.StackRole = Camera.StackType.Base;
mainCamera.Depth = 0;
mainCamera.ClearFlags = Camera.ClearFlagsType.Color;
mainCamera.CullingMask = LayerMask.World | LayerMask.Player;

// Minimap camera
minimapCamera.StackRole = Camera.StackType.Overlay;
minimapCamera.Depth = 10;  // Renders after main
minimapCamera.ClearFlags = Camera.ClearFlagsType.Color;
minimapCamera.ClearColor = new Color(0, 0, 0, 200);  // Semi-transparent
minimapCamera.ProjectionMode = Camera.ProjectionType.Orthographic;
minimapCamera.OrthographicSize = 50f;

// Position in corner
minimapCamera.Viewport = new Viewport(
    x: screenWidth - 210,
    y: 10,
    width: 200,
    height: 200
);

// Different culling mask (only show map elements)
minimapCamera.CullingMask = LayerMask.Minimap;
```

### Switching Active Camera

```csharp
// By camera reference
scene.SetActiveCamera(newCamera);

// By GameObject name
scene.SetActiveCamera("SecurityCamera_01");

// With transition
public void SwitchCamera(Camera target, float duration)
{
    var current = scene.ActiveCamera;
    if (current == target) return;
    
    // Transition then switch
    orchestrator.Sequence()
        .MoveTo(current.Transform, target.Transform.Position, duration)
        .With()
        .RotateTo(current.Transform, target.Transform.Rotation, duration)
        .OnComplete(() => scene.SetActiveCamera(target))
        .Play();
}
```

---

## Best Practices

### Avoid Gimbal Lock

```csharp
// Use quaternions for smooth rotation
Transform.RotateToWorld(targetQuaternion);

// Avoid accumulating euler angles
// Bad:
pitch += deltaPitch;
yaw += deltaYaw;
Transform.RotateEulerBy(new Vector3(pitch, yaw, 0));  // Can accumulate errors

// Good:
var pitchQuat = Quaternion.CreateFromAxisAngle(Vector3.Right, deltaPitch);
var yawQuat = Quaternion.CreateFromAxisAngle(Vector3.Up, deltaYaw);
Transform.RotateBy(pitchQuat);
Transform.RotateBy(yawQuat, worldSpace: true);
```

### Camera in LateUpdate

```csharp
// Follow logic should run in LateUpdate
// after all other transforms have updated
protected override void LateUpdate(float deltaTime)
{
    FollowTarget(deltaTime);
}
```

### Clamp Camera Angles

```csharp
private float _pitch;  // Vertical angle
private const float MaxPitch = 80f;
private const float MinPitch = -80f;

private void UpdateLook(float deltaPitch)
{
    _pitch = MathHelper.Clamp(
        _pitch + deltaPitch,
        MathHelper.ToRadians(MinPitch),
        MathHelper.ToRadians(MaxPitch)
    );
}
```
