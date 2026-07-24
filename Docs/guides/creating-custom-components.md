# Task Guide: Creating Custom Components

## Overview

This guide covers creating your own Component classes to add custom behaviour to GameObjects.

---

## Basic Component Structure

```csharp
using GDEngine.Core.Components;
using GDEngine.Core.Entities;
using Microsoft.Xna.Framework;

namespace MyGame.Components
{
    public class MyComponent : Component
    {
        #region Fields
        private float _someValue;
        #endregion
        
        #region Properties
        public float SomeValue 
        { 
            get => _someValue; 
            set => _someValue = value; 
        }
        #endregion
        
        #region Lifecycle Methods
        protected override void Awake()
        {
            // Called once when added to scene
            // Cache references here
        }
        
        protected override void Start()
        {
            // Called once before first Update
            // Safe to access other GameObjects/components
        }
        
        protected override void Update(float deltaTime)
        {
            // Called every frame
            // Main logic goes here
        }
        
        protected override void LateUpdate(float deltaTime)
        {
            // Called after all Updates
            // Good for follow cameras, cleanup
        }
        
        protected override void OnDestroy()
        {
            // Cleanup when destroyed
            // Unsubscribe events, release resources
        }
        #endregion
    }
}
```

---

## Component with Dependencies

When your component needs other components or systems:

```csharp
public class DependentComponent : Component
{
    // Dependencies
    private AudioSystem? _audioSystem;
    private RigidBody? _rigidBody;
    private Transform? _targetTransform;
    
    // Configuration
    public string SoundId { get; set; } = "default";
    public GameObject? Target { get; set; }
    
    protected override void Awake()
    {
        // Get sibling component (same GameObject)
        _rigidBody = GameObject.GetComponent<RigidBody>();
        
        // Get system from scene
        _audioSystem = GameObject.Scene.GetSystem<AudioSystem>();
    }
    
    protected override void Start()
    {
        // Safe to access other GameObjects
        if (Target != null)
        {
            _targetTransform = Target.Transform;
        }
        
        // Validate required dependencies
        if (_rigidBody == null)
        {
            throw new InvalidOperationException(
                "DependentComponent requires RigidBody on same GameObject");
        }
    }
}
```

---

## Component with Events

### Publishing Events

```csharp
public class ItemCollector : Component
{
    private EventBus? _events;
    
    protected override void Awake()
    {
        _events = GameObject.Scene.Context.Events;
    }
    
    public void CollectItem(string itemId)
    {
        // Publish event for other systems to react
        _events?.Publish(new ItemCollectedEvent(itemId));
    }
}

// Define your event class
public class ItemCollectedEvent
{
    public string ItemId { get; }
    
    public ItemCollectedEvent(string itemId)
    {
        ItemId = itemId;
    }
}
```

### Subscribing to Events

```csharp
public class ScoreDisplay : Component
{
    private IDisposable? _subscription;
    private int _score;
    
    protected override void Awake()
    {
        var events = GameObject.Scene.Context.Events;
        
        _subscription = events.On<ItemCollectedEvent>()
            .Do(OnItemCollected);
    }
    
    private void OnItemCollected(ItemCollectedEvent evt)
    {
        _score += 10;
        UpdateDisplay();
    }
    
    protected override void OnDestroy()
    {
        // Always unsubscribe!
        _subscription?.Dispose();
    }
}
```

---

## Component with C# Events

For direct, immediate callbacks:

```csharp
public class HealthComponent : Component
{
    // Events
    public event Action<float>? HealthChanged;
    public event Action? Died;
    
    // State
    private float _health = 100f;
    private float _maxHealth = 100f;
    
    // Properties
    public float Health => _health;
    public float MaxHealth => _maxHealth;
    public bool IsDead => _health <= 0;
    
    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        
        _health = Math.Max(0, _health - amount);
        HealthChanged?.Invoke(_health);
        
        if (_health <= 0)
        {
            Died?.Invoke();
        }
    }
    
    public void Heal(float amount)
    {
        if (IsDead) return;
        
        _health = Math.Min(_maxHealth, _health + amount);
        HealthChanged?.Invoke(_health);
    }
    
    protected override void OnDestroy()
    {
        // Clear all subscribers
        HealthChanged = null;
        Died = null;
    }
}
```

---

## Component with Input

### Using IInputReceiver

```csharp
public class PlayerController : Component, IInputReceiver
{
    private InputSystem? _inputSystem;
    private float _moveX, _moveY;
    
    protected override void Start()
    {
        _inputSystem = GameObject.Scene.GetSystem<InputSystem>();
        _inputSystem?.Add(this);  // Register as receiver
    }
    
    // IInputReceiver implementation
    public void OnAxis(string axisName, float value)
    {
        switch (axisName)
        {
            case "Horizontal":
                _moveX = value;
                break;
            case "Vertical":
                _moveY = value;
                break;
        }
    }
    
    public void OnButtonDown(string buttonName)
    {
        if (buttonName == "Jump")
            Jump();
        else if (buttonName == "Interact")
            TryInteract();
    }
    
    public void OnButtonUp(string buttonName)
    {
        // Handle button release
    }
    
    protected override void Update(float deltaTime)
    {
        // Use input values
        var movement = new Vector3(_moveX, 0, _moveY);
        Transform.TranslateBy(movement * 5f * deltaTime);
    }
    
    protected override void OnDestroy()
    {
        _inputSystem?.Remove(this);  // Unregister
    }
}
```

---

## Component with Orchestration

```csharp
public class AnimatedDoor : Component
{
    private Orchestrator? _orchestrator;
    private Quaternion _closedRotation;
    private Quaternion _openRotation;
    private bool _isOpen;
    
    public float OpenAngle { get; set; } = 90f;
    public float AnimationDuration { get; set; } = 1f;
    
    protected override void Awake()
    {
        var orchSystem = GameObject.Scene.GetSystem<OrchestrationSystem>();
        _orchestrator = orchSystem?.Orchestrator;
        
        _closedRotation = Transform.Rotation;
        _openRotation = _closedRotation * 
            Quaternion.CreateFromYawPitchRoll(
                MathHelper.ToRadians(OpenAngle), 0, 0);
    }
    
    public void Open()
    {
        if (_isOpen || _orchestrator == null) return;
        _isOpen = true;
        
        _orchestrator.Sequence()
            .RotateTo(Transform, _openRotation, AnimationDuration, Ease.QuadOut)
            .Play();
    }
    
    public void Close()
    {
        if (!_isOpen || _orchestrator == null) return;
        _isOpen = false;
        
        _orchestrator.Sequence()
            .RotateTo(Transform, _closedRotation, AnimationDuration, Ease.QuadIn)
            .Play();
    }
    
    public void Toggle()
    {
        if (_isOpen) Close();
        else Open();
    }
}
```

---

## Component that Finds Objects

```csharp
public class TargetFinder : Component
{
    private List<GameObject> _targets = new();
    
    public LayerMask TargetLayer { get; set; } = LayerMask.Enemy;
    public float SearchRadius { get; set; } = 10f;
    
    protected override void Start()
    {
        RefreshTargets();
    }
    
    public void RefreshTargets()
    {
        _targets.Clear();
        
        var scene = GameObject.Scene;
        var myPosition = Transform.Position;
        
        _targets = scene.FindAll(go => 
        {
            // Check layer
            if (!TargetLayer.Contains(go.Layer))
                return false;
            
            // Check distance
            var distance = Vector3.Distance(myPosition, go.Transform.Position);
            return distance <= SearchRadius;
        });
    }
    
    public GameObject? GetClosestTarget()
    {
        if (_targets.Count == 0) return null;
        
        var myPosition = Transform.Position;
        GameObject? closest = null;
        float closestDist = float.MaxValue;
        
        foreach (var target in _targets)
        {
            var dist = Vector3.Distance(myPosition, target.Transform.Position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = target;
            }
        }
        
        return closest;
    }
}
```

---

## Enable/Disable Handling

```csharp
public class ToggleableComponent : Component
{
    private AudioSystem? _audio;
    
    protected override void OnEnabled()
    {
        // Called when Enabled changes to true
        // Resume operations
        _audio?.PlayOneShot("activate");
    }
    
    protected override void OnDisabled()
    {
        // Called when Enabled changes to false
        // Pause operations, release resources
        _audio?.PlayOneShot("deactivate");
    }
}

// Usage
component.Enabled = false;  // Triggers OnDisabled
component.Enabled = true;   // Triggers OnEnabled
```

---

## Best Practices Checklist

- [ ] Cache references in `Awake()` or `Start()`, not in `Update()`
- [ ] Validate required dependencies with meaningful error messages
- [ ] Unsubscribe from all events in `OnDestroy()`
- [ ] Use `deltaTime` for frame-rate independent logic
- [ ] Keep `Update()` lightweight; avoid allocations
- [ ] Use properties for configuration (settable from outside)
- [ ] Document public members with XML comments
- [ ] Use regions to organize code sections
- [ ] Follow naming conventions (underscore prefix for private fields)
