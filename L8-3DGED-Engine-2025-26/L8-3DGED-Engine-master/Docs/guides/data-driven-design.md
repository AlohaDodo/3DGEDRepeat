# Task Guide: Data-Driven Design

## Overview

Data-driven design separates **what things are** (data) from **how they behave** (code). Instead of hardcoding values in your components, you define data externally and let your code interpret it.

This guide explores approaches to structuring game data—you decide which fits your project best.

---

## Why Data-Driven?

### The Problem: Magic Strings and Numbers

```csharp
// Bad: Values scattered in code
if (itemName == "rusty_key")  // Magic string
{
    ShowMessage("An old rusty key. Might open something.");
    PlaySound("key_pickup");
    AddToInventory("rusty_key");
    score += 10;  // Magic number
}
else if (itemName == "flashlight")
{
    ShowMessage("A working flashlight.");
    // ... more hardcoded logic
}
```

Problems:
- Hard to find all places an item is referenced
- Easy to introduce typos
- Difficult to add new items
- Logic and data intertwined

### The Solution: Centralized Data

```csharp
// Good: Data defines behaviour
var itemData = GetItemData(itemId);
ShowMessage(itemData.Description);
PlaySound(itemData.PickupSound);
AddToInventory(itemId);
score += itemData.PointValue;
```

Benefits:
- Single source of truth
- Easy to add/modify items
- Code handles any item the same way
- Clear separation of concerns

---

## Approach 1: Enum + Static Data

Simple, compile-time safe, good for small fixed sets.

```csharp
// Define IDs as enum
public enum ItemId
{
    None,
    RustyKey,
    Flashlight,
    Note,
    Battery
}

// Define data structure
public record ItemData(
    string DisplayName,
    string Description,
    string PickupSound,
    int PointValue,
    bool IsKeyItem
);

// Static registry
public static class ItemDatabase
{
    private static readonly Dictionary<ItemId, ItemData> _items = new()
    {
        [ItemId.RustyKey] = new ItemData(
            DisplayName: "Rusty Key",
            Description: "An old rusty key. Might open something nearby.",
            PickupSound: "sfx_key_pickup",
            PointValue: 10,
            IsKeyItem: true
        ),
        [ItemId.Flashlight] = new ItemData(
            DisplayName: "Flashlight",
            Description: "A working flashlight. The battery is low.",
            PickupSound: "sfx_item_pickup",
            PointValue: 5,
            IsKeyItem: true
        ),
        // ... more items
    };
    
    public static ItemData Get(ItemId id) => _items[id];
    public static bool TryGet(ItemId id, out ItemData? data) => 
        _items.TryGetValue(id, out data);
}
```

**Usage:**

```csharp
public class CollectableItem : Component
{
    public ItemId ItemId { get; set; }
    
    public void OnCollect()
    {
        var data = ItemDatabase.Get(ItemId);
        
        _audio.PlayOneShot(data.PickupSound);
        _ui.ShowMessage(data.Description);
        _inventory.Add(ItemId);
    }
}
```

**Pros:** Type-safe, IntelliSense support, compile-time validation  
**Cons:** Requires recompilation to add items

---

## Approach 2: String Keys + Dictionary

Flexible, easily extensible, works well with external data.

```csharp
public class ItemData
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public string PickupSound { get; set; } = "default_pickup";
    public int PointValue { get; set; }
    public bool IsKeyItem { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
}

public class ItemRegistry
{
    private readonly Dictionary<string, ItemData> _items = new();
    
    public void Register(ItemData item)
    {
        _items[item.Id] = item;
    }
    
    public ItemData? Get(string id) =>
        _items.TryGetValue(id, out var data) ? data : null;
    
    public IEnumerable<ItemData> GetByTag(string tag) =>
        _items.Values.Where(i => i.Tags.Contains(tag));
}
```

**Populating the registry:**

```csharp
// Manual registration
var registry = new ItemRegistry();

registry.Register(new ItemData
{
    Id = "rusty_key",
    DisplayName = "Rusty Key",
    Description = "An old rusty key.",
    PickupSound = "sfx_key",
    IsKeyItem = true,
    Tags = new[] { "key", "metal" }
});

registry.Register(new ItemData
{
    Id = "flashlight",
    DisplayName = "Flashlight",
    // ...
});
```

**Pros:** Easy to extend, can load from files  
**Cons:** No compile-time validation of IDs

### Using Constants for Safety

```csharp
public static class ItemIds
{
    public const string RustyKey = "rusty_key";
    public const string Flashlight = "flashlight";
    public const string Note = "note";
    public const string Battery = "battery";
}

// Usage - compile-time constant, but still a string
var data = registry.Get(ItemIds.RustyKey);
```

---

## Approach 3: Class Hierarchy

When items have significantly different behaviours.

```csharp
public abstract class ItemData
{
    public abstract string Id { get; }
    public abstract string DisplayName { get; }
    public abstract string Description { get; }
    public virtual string PickupSound => "default_pickup";
    
    public virtual void OnCollect(InventorySystem inventory) 
    {
        inventory.Add(Id);
    }
    
    public virtual void OnUse(PlayerState player) { }
}

public class KeyItem : ItemData
{
    public override string Id => "rusty_key";
    public override string DisplayName => "Rusty Key";
    public override string Description => "Opens the basement door.";
    
    public string UnlocksId { get; init; } = "";
}

public class ConsumableItem : ItemData
{
    public override string Id { get; }
    public override string DisplayName { get; }
    public override string Description { get; }
    
    public float HealthRestore { get; init; }
    
    public override void OnUse(PlayerState player)
    {
        player.Health += HealthRestore;
    }
}
```

**Pros:** Type-specific behaviour, polymorphism  
**Cons:** More complex, harder to add items without coding

---

## Structuring Complex Data

### Nested Data

```csharp
public class InteractableData
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    
    // Nested data for different interaction types
    public ExamineData? Examine { get; set; }
    public CollectData? Collect { get; set; }
    public ActivateData? Activate { get; set; }
}

public class ExamineData
{
    public string Description { get; set; } = "";
    public string Sound { get; set; } = "examine_default";
    public string? JournalEntry { get; set; }
}

public class CollectData
{
    public string ItemId { get; set; } = "";
    public string Sound { get; set; } = "pickup_default";
    public string? VoiceLine { get; set; }
}

public class ActivateData
{
    public string TargetId { get; set; } = "";
    public string Action { get; set; } = "";  // "unlock", "toggle", "reveal"
    public string? RequiredItem { get; set; }
    public string Sound { get; set; } = "activate_default";
}
```

### Conditional/Gated Content

```csharp
public class InteractableData
{
    // ... basic fields
    
    // Requirements for interaction
    public string? RequiredItemId { get; set; }
    public string? RequiredStateKey { get; set; }
    public int? RequiredStateValue { get; set; }
    
    // Different states
    public string LockedDescription { get; set; } = "It won't budge.";
    public string UnlockedDescription { get; set; } = "";
}

// Checking requirements
public bool CanInteract(InteractableData data, PlayerState state)
{
    if (data.RequiredItemId != null && !state.HasItem(data.RequiredItemId))
        return false;
    
    if (data.RequiredStateKey != null)
    {
        var value = state.GetValue(data.RequiredStateKey);
        if (value < (data.RequiredStateValue ?? 0))
            return false;
    }
    
    return true;
}
```

---

## Connecting Data to GameObjects

### Option A: Component Stores ID

```csharp
public class Interactable : Component
{
    public string DataId { get; set; } = "";
    
    private InteractableData? _data;
    
    protected override void Start()
    {
        var registry = GameObject.Scene.GetSystem<DataRegistry>();
        _data = registry.GetInteractable(DataId);
    }
}
```

### Option B: System Maintains Mapping

```csharp
public class InteractableSystem : SystemBase
{
    private Dictionary<GameObject, InteractableData> _mapping = new();
    
    public void Register(GameObject go, string dataId)
    {
        var data = _dataRegistry.Get(dataId);
        _mapping[go] = data;
    }
    
    public InteractableData? GetData(GameObject go) =>
        _mapping.TryGetValue(go, out var data) ? data : null;
}
```

### Option C: Name Convention

```csharp
// GameObject name matches data ID
protected override void Start()
{
    var dataId = GameObject.Name;  // "rusty_key"
    _data = registry.Get(dataId);
}
```

---

## Design Considerations

### Questions to Ask

1. **How many items/objects will you have?**
   - Few (<20): Enum approach works well
   - Many (>50): String/dictionary more maintainable

2. **How different are the items?**
   - Similar structure: Single data class
   - Very different: Class hierarchy or tagged data

3. **Will you add items often?**
   - Rarely: Code-based fine
   - Frequently: Consider external files

4. **Do items have state?**
   - Stateless: Data can be shared
   - Stateful: Separate instance data from definition

### Avoiding Common Pitfalls

```csharp
// Pitfall 1: Mixing data with state
public class ItemData
{
    public string Id { get; set; }
    public int Count { get; set; }  // Bad: This is instance state!
}

// Better: Separate definition from instance
public class ItemDefinition { /* shared, immutable */ }
public class InventorySlot { public string ItemId; public int Count; }


// Pitfall 2: Stringly-typed everything
item.Type = "weapon";
if (item.Type == "wapon")  // Typo! No compile error

// Better: Use enums for categories
public enum ItemCategory { Weapon, Key, Consumable, Document }


// Pitfall 3: Deep nesting
data.Interactions.Examine.Conditions.Required.Items[0].Id  // Fragile!

// Better: Flatten or provide helper methods
data.GetExamineRequirements()
```

---

## Summary

| Approach | Best For | Trade-offs |
|----------|----------|------------|
| Enum + Static | Small, fixed sets | Type-safe but rigid |
| String + Dictionary | Flexible, extensible | No compile-time checks |
| Class Hierarchy | Varying behaviours | More complex |
| External Files | Large datasets | Runtime loading required |

Choose based on your project's needs. Start simple—you can always refactor to a more complex approach if needed.
