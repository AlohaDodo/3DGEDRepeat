# Orchestration Guide

## Overview

The Orchestration system provides timeline-based animations and sequences. It allows you to chain movements, rotations, fades, delays, and callbacks into fluid sequences without manual timer management.

---

## Core Concepts

| Concept | Description |
|---------|-------------|
| **Orchestrator** | Manages all active sequences |
| **Sequence** | A chain of timed steps |
| **Step** | A single operation (tween, wait, callback) |
| **Easing** | Controls animation curve shape |

---

## Accessing the Orchestrator

```csharp
// From OrchestrationSystem
var orchSystem = scene.GetSystem<OrchestrationSystem>();
var orchestrator = orchSystem.Orchestrator;
```

---

## Creating Sequences

### Basic Structure

```csharp
orchestrator.Sequence()
    .Step1()
    .Then()
    .Step2()
    .Then()
    .Step3()
    .Play();
```

### Movement

```csharp
// Move to absolute position
orchestrator.Sequence()
    .MoveTo(transform, new Vector3(10, 0, 5), duration: 2f)
    .Play();

// Move to position with easing
orchestrator.Sequence()
    .MoveTo(transform, targetPos, 1.5f, Ease.SmoothStep)
    .Play();

// Move by offset
orchestrator.Sequence()
    .MoveBy(transform, new Vector3(0, 5, 0), 1f, Ease.QuadOut)
    .Play();
```

### Rotation

```csharp
// Rotate to absolute rotation
orchestrator.Sequence()
    .RotateTo(transform, targetQuaternion, duration: 1f)
    .Play();

// Rotate by euler angles (radians)
orchestrator.Sequence()
    .RotateBy(transform, new Vector3(0, MathHelper.Pi, 0), 1f)
    .Play();
```

### Scaling

```csharp
// Scale to size
orchestrator.Sequence()
    .ScaleTo(transform, new Vector3(2, 2, 2), 0.5f, Ease.BounceOut)
    .Play();

// Scale by factor
orchestrator.Sequence()
    .ScaleBy(transform, 1.5f, 0.3f)
    .Play();
```

### Waiting

```csharp
// Wait for duration
orchestrator.Sequence()
    .Wait(2f)
    .Then()
    .DoSomething()
    .Play();

// Wait until condition
orchestrator.Sequence()
    .WaitUntil(() => door.IsOpen)
    .Then()
    .MoveTo(player, doorPosition, 1f)
    .Play();
```

### Callbacks

```csharp
// Execute action
orchestrator.Sequence()
    .Do(() => PlaySound("click"))
    .Play();

// Execute with delay
orchestrator.Sequence()
    .Wait(0.5f)
    .Then()
    .Do(() => ShowMessage("Hello"))
    .Play();
```

---

## Chaining Steps

### Sequential (Then)

Steps execute one after another:

```csharp
orchestrator.Sequence()
    .MoveTo(t, pos1, 1f)    // 0s - 1s
    .Then()
    .MoveTo(t, pos2, 1f)    // 1s - 2s
    .Then()
    .MoveTo(t, pos3, 1f)    // 2s - 3s
    .Play();
```

### Parallel (With)

Steps execute simultaneously:

```csharp
orchestrator.Sequence()
    .MoveTo(t, targetPos, 1f)
    .With()
    .RotateTo(t, targetRot, 1f)   // Runs at same time as move
    .With()
    .ScaleTo(t, Vector3.One * 2, 1f)
    .Play();
```

### Mixed

```csharp
orchestrator.Sequence()
    // Phase 1: Move and rotate together
    .MoveTo(t, pos1, 1f)
    .With()
    .RotateTo(t, rot1, 1f)
    .Then()
    // Phase 2: Wait
    .Wait(0.5f)
    .Then()
    // Phase 3: Scale and callback together
    .ScaleTo(t, Vector3.One * 0.5f, 0.3f)
    .With()
    .Do(() => PlaySound("shrink"))
    .Play();
```

---

## Easing Functions

Easing controls the rate of change during animation:

| Ease | Description | Use Case |
|------|-------------|----------|
| `Ease.Linear` | Constant speed | Mechanical movement |
| `Ease.SmoothStep` | Smooth start and end | General purpose |
| `Ease.QuadIn` | Accelerate | Starting movement |
| `Ease.QuadOut` | Decelerate | Stopping movement |
| `Ease.QuadInOut` | Accel then decel | Smooth travel |
| `Ease.CubicIn/Out/InOut` | More pronounced | Dramatic movement |
| `Ease.BounceOut` | Bounce at end | Landing, impacts |
| `Ease.ElasticOut` | Overshoot and settle | UI pop-in |
| `Ease.BackIn` | Pull back first | Wind-up |
| `Ease.BackOut` | Overshoot slightly | Energetic arrival |

### Usage

```csharp
.MoveTo(transform, target, 1f, Ease.SmoothStep)
.ScaleTo(transform, scale, 0.5f, Ease.BounceOut)
.RotateTo(transform, rotation, 2f, Ease.QuadInOut)
```

---

## Sequence Control

### Playing

```csharp
var sequence = orchestrator.Sequence()
    .MoveTo(t, pos, 1f)
    .Play();  // Starts immediately
```

### Pausing/Resuming

```csharp
sequence.Pause();
sequence.Resume();
```

### Stopping

```csharp
sequence.Stop();           // Stop where it is
sequence.Stop(complete: true);  // Jump to end
```

### Looping

```csharp
orchestrator.Sequence()
    .MoveTo(t, pos1, 1f)
    .Then()
    .MoveTo(t, pos2, 1f)
    .Loop(count: 3)  // Repeat 3 times
    .Play();

orchestrator.Sequence()
    .RotateBy(t, new Vector3(0, MathHelper.TwoPi, 0), 2f)
    .LoopForever()   // Infinite loop
    .Play();
```

### Completion Callbacks

```csharp
orchestrator.Sequence()
    .MoveTo(t, target, 1f)
    .OnComplete(() => {
        Debug.Log("Movement finished!");
    })
    .Play();
```

---

## Practical Examples

### Door Opening

```csharp
public void OpenDoor(Transform doorTransform)
{
    var openRotation = Quaternion.CreateFromYawPitchRoll(
        MathHelper.ToRadians(90), 0, 0);
    
    orchestrator.Sequence()
        .Do(() => audioSystem.PlayOneShot("door_creak"))
        .RotateTo(doorTransform, openRotation, 1.2f, Ease.QuadOut)
        .OnComplete(() => _isOpen = true)
        .Play();
}
```

### Camera Transition

```csharp
public void TransitionToCamera(Transform target, float duration)
{
    var cameraTransform = activeCamera.Transform;
    
    orchestrator.Sequence()
        .MoveTo(cameraTransform, target.Position, duration, Ease.SmoothStep)
        .With()
        .RotateTo(cameraTransform, target.Rotation, duration, Ease.SmoothStep)
        .Play();
}
```

### Item Collection Effect

```csharp
public void CollectItem(Transform item, Transform player)
{
    orchestrator.Sequence()
        // Float up
        .MoveBy(item, Vector3.Up * 0.5f, 0.2f, Ease.QuadOut)
        .With()
        .ScaleTo(item, Vector3.One * 1.3f, 0.2f, Ease.QuadOut)
        .Then()
        // Fly to player
        .MoveTo(item, player.Position, 0.3f, Ease.QuadIn)
        .With()
        .ScaleTo(item, Vector3.Zero, 0.3f, Ease.QuadIn)
        .Then()
        // Cleanup
        .Do(() => {
            audioSystem.PlayOneShot("collect");
            item.GameObject.Destroy();
            inventory.Add(itemData);
        })
        .Play();
}
```

### UI Panel Slide

```csharp
public void ShowPanel(UIRenderer panel)
{
    var startPos = new Vector2(-300, panel.Position.Y);
    var endPos = new Vector2(50, panel.Position.Y);
    
    panel.Position = startPos;
    panel.Enabled = true;
    
    orchestrator.Sequence()
        .TweenVector2(
            () => panel.Position,
            v => panel.Position = v,
            endPos,
            0.4f,
            Ease.BackOut)
        .Play();
}
```

### Examination Camera

```csharp
public void ExamineObject(Transform target)
{
    var examinePos = target.Position + new Vector3(0, 0.5f, 1.5f);
    var lookRotation = CalculateLookAt(examinePos, target.Position);
    
    // Store original position for return
    var originalPos = cameraTransform.Position;
    var originalRot = cameraTransform.Rotation;
    
    orchestrator.Sequence()
        // Move to examine position
        .MoveTo(cameraTransform, examinePos, 0.8f, Ease.SmoothStep)
        .With()
        .RotateTo(cameraTransform, lookRotation, 0.8f, Ease.SmoothStep)
        .Then()
        // Wait for input
        .WaitUntil(() => Input.GetButtonDown("Cancel"))
        .Then()
        // Return to original
        .MoveTo(cameraTransform, originalPos, 0.6f, Ease.SmoothStep)
        .With()
        .RotateTo(cameraTransform, originalRot, 0.6f, Ease.SmoothStep)
        .Play();
}
```

### Staged Reveal

```csharp
public void RevealClue()
{
    orchestrator.Sequence()
        // Dim lights
        .Do(() => SetAmbientLight(0.3f))
        .Wait(0.5f)
        .Then()
        // Spotlight on clue
        .Do(() => spotlight.Enabled = true)
        .TweenFloat(
            () => spotlight.Intensity,
            v => spotlight.Intensity = v,
            1f,
            1f,
            Ease.QuadIn)
        .Then()
        // Play reveal sound
        .Do(() => audioSystem.PlayOneShot("reveal"))
        .Wait(2f)
        .Then()
        // Restore
        .Do(() => spotlight.Enabled = false)
        .Do(() => SetAmbientLight(1f))
        .Play();
}
```

---

## Best Practices

### Store References for Control

```csharp
private Sequence? _currentSequence;

public void StartAnimation()
{
    _currentSequence = orchestrator.Sequence()
        .MoveTo(t, pos, 1f)
        .Play();
}

public void CancelAnimation()
{
    _currentSequence?.Stop();
}
```

### Avoid Conflicts

```csharp
// Bad - multiple sequences fighting over same transform
orchestrator.Sequence().MoveTo(t, pos1, 1f).Play();
orchestrator.Sequence().MoveTo(t, pos2, 1f).Play();  // Conflict!

// Good - stop previous before starting new
_currentSequence?.Stop();
_currentSequence = orchestrator.Sequence()
    .MoveTo(t, pos2, 1f)
    .Play();
```

### Use Appropriate Durations

```csharp
// UI feedback: 0.1 - 0.3 seconds
.ScaleTo(button, pressedScale, 0.1f)

// Camera transitions: 0.5 - 1.5 seconds
.MoveTo(camera, newPos, 0.8f)

// Dramatic reveals: 1 - 3 seconds
.FadeIn(overlay, 2f)
```

### Combine with Events

```csharp
orchestrator.Sequence()
    .MoveTo(door, openPosition, 1f)
    .Then()
    .Do(() => eventBus.Publish(new DoorOpenedEvent(doorId)))
    .Play();
```
