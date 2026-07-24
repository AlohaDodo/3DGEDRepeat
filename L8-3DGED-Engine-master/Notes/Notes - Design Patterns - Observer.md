# Design Patterns - Observer & Event Systems in C# 

## Table of Contents
1. [C# Delegates and Function Pointers](#1-c-delegates-and-function-pointers)
2. [Actions and Funcs](#2-actions-and-funcs)
3. [C# Events](#3-c-events)
4. [Observer Design Pattern](#4-observer-design-pattern)
5. [Simple String-Based Event System](#5-simple-string-based-event-system)
6. [Adding Event Handlers](#6-adding-event-handlers)
7. [Priority-Based Execution](#7-priority-based-execution)
8. [One-Shot Subscriptions](#8-one-shot-subscriptions)
9. [Subscription Objects and Disposal](#9-subscription-objects-and-disposal)
10. [Complete Type-Safe Event System](#10-complete-type-safe-event-system)
11. [Appendix A: Common Pitfalls](#11-appendix-a-common-pitfalls)
12. [Appendix B: Event Bus Fluent Builder Examples](#12-appendix-b-event-bus-fluent-builder-examples)

---

## Learning Objectives

By the end of this lesson, you will be able to:

1. **Explain** the purpose and differences between delegates, Actions, Funcs, and C# events
2. **Implement** the Observer design pattern and understand its application in game systems
3. **Build** a type-safe generic event system with subscribe, unsubscribe, and publish functionality
4. **Apply** priority-based execution ordering to control event handler sequencing
5. **Create** disposable subscription tokens following the IDisposable pattern to prevent memory leaks
6. **Analyze** when event-driven communication is appropriate versus direct method calls
7. **Integrate** one-shot subscriptions and filtering into an event system
8. **Design** a complete event-driven architecture for a game system (e.g., combat, UI, inventory)

## Prerequisites

- Understanding of C# classes, methods, and basic object-oriented programming
- Familiarity with generic types in C# (`List<T>`, `Dictionary<TKey, TValue>`)
- Knowledge of C# interfaces and implementing interface contracts
- Basic understanding of collections (List, Dictionary)
- Familiarity with lambda expressions and anonymous functions
- Understanding of method parameters and return types
- Knowledge of null-safety and null-conditional operators (`?.`)

---

## 1. C# Delegates and Function Pointers

### What is a Delegate?

A **delegate** is a type that represents references to methods with a particular parameter list and return type. Think of it as a "method container" or "function pointer" that you can pass around.

```csharp
// Define a delegate type
public delegate void MessageHandler(string message);

public class DelegateExample
{
    // A method that matches the delegate signature
    public static void PrintToConsole(string message)
    {
        Console.WriteLine($"Console: {message}");
    }
    
    public static void PrintToDebug(string message)
    {
        Debug.WriteLine($"Debug: {message}");
    }
    
    public static void Main()
    {
        // Create delegate instances
        MessageHandler handler1 = PrintToConsole;
        MessageHandler handler2 = PrintToDebug;
        
        // Invoke delegates
        handler1("Hello World");  // Output: Console: Hello World
        handler2("Hello World");  // Output: Debug: Hello World
        
        // Multicast delegates - chain multiple methods
        MessageHandler combined = handler1 + handler2;
        combined("Broadcast");
        // Output:
        // Console: Broadcast
        // Debug: Broadcast
    }
}
```

### Key Concepts

**Delegate Declaration:**
```csharp
public delegate ReturnType DelegateName(ParameterType1 param1, ParameterType2 param2);
```

**Delegate Features:**
- Type-safe function pointers
- Can reference static or instance methods
- Support multicast (chaining multiple methods)
- Can be null (check before invoking!)

**Common Pitfall:**
```csharp
MessageHandler handler = null;
handler("Test"); // NullReferenceException!

// Safe invocation
handler?.Invoke("Test"); // C# 6.0+
```

---

## 2. Actions and Funcs

### Built-in Delegate Types

C# provides pre-defined generic delegates to avoid declaring custom delegate types for common scenarios.

### Action<T>

An **Action** is a delegate that takes parameters but returns void.

```csharp
// No parameters
Action simpleAction = () => Console.WriteLine("No parameters");

// One parameter
Action<string> printAction = (message) => Console.WriteLine(message);

// Multiple parameters (up to 16)
Action<string, int, bool> complexAction = (name, age, isActive) => 
{
    Console.WriteLine($"{name} is {age} years old. Active: {isActive}");
};

// Usage
simpleAction();                           // No parameters
printAction("Hello");                     // Hello
complexAction("Alice", 30, true);         // Alice is 30 years old. Active: True
```

### Func<T, TResult>

A **Func** is a delegate that takes parameters AND returns a value. The last type parameter is always the return type.

```csharp
// No parameters, returns int
Func<int> getNumber = () => 42;

// One parameter, returns bool
Func<int, bool> isEven = (number) => number % 2 == 0;

// Multiple parameters, returns string
Func<string, int, string> repeat = (text, count) => 
{
    string result = "";
    for (int i = 0; i < count; i++)
        result += text;
    return result;
};

// Usage
int num = getNumber();                    // 42
bool even = isEven(10);                   // true
string repeated = repeat("Ha", 3);        // HaHaHa
```

### Comparison Table

| Delegate Type | Returns Value? | Example Signature |
|--------------|----------------|-------------------|
| `Action` | No (void) | `Action` |
| `Action<T>` | No (void) | `Action<string>` |
| `Action<T1, T2>` | No (void) | `Action<int, string>` |
| `Func<TResult>` | Yes | `Func<int>` returns int |
| `Func<T, TResult>` | Yes | `Func<string, int>` takes string, returns int |
| `Func<T1, T2, TResult>` | Yes | `Func<int, int, bool>` takes 2 ints, returns bool |

### Practical Example: Game Event Callbacks

```csharp
public class GameEvents
{
    // Simple notification (no parameters, no return)
    public Action OnGameStart;
    
    // Notification with data (parameters, no return)
    public Action<int> OnScoreChanged;
    public Action<string, Vector3> OnPlayerSpawned;
    
    public void StartGame()
    {
        OnGameStart?.Invoke();
    }
    
    public void AddScore(int points)
    {
        OnScoreChanged?.Invoke(points);
    }
}
```

---

## 3. C# Events

### What Are Events?

An **event** is a special type of multicast delegate with restricted access. Events follow the publisher-subscriber pattern and prevent external code from invoking or replacing the delegate directly. Multicast means that multiple entities (e.g. audio system, animation system, inventory, etc.) can subscribe to an event. 

### Event vs Delegate

```csharp
public class WithoutEvents
{
    // Using public delegate (BAD - no encapsulation)
    public Action<string> OnMessage;
    
    public void Trigger()
    {
        OnMessage?.Invoke("Hello");
    }
}

public class WithEvents
{
    // Using event (GOOD - encapsulated)
    public event Action<string> OnMessage;
    
    public void Trigger()
    {
        OnMessage?.Invoke("Hello");
    }
}

// Usage comparison
var bad = new WithoutEvents();
bad.OnMessage = SomeHandler;           // Can REPLACE all handlers!
bad.OnMessage?.Invoke("Hijacked");     // External code can invoke!

var good = new WithEvents();
good.OnMessage += SomeHandler;         // Can only ADD handlers
good.OnMessage?.Invoke("Test");        // Compile error! Cannot invoke from outside
```

### Event Declaration Patterns

**Simple Event:**
```csharp
public class Button
{
    public event Action OnClick;
    
    public void Click()
    {
        OnClick?.Invoke();
    }
}

// Usage
var button = new Button();
button.OnClick += () => Console.WriteLine("Clicked!");
button.Click(); // Output: Clicked!
```

**Event with EventArgs:**
```csharp
public class ScoreEventArgs : EventArgs
{
    public int NewScore { get; set; }
    public int Delta { get; set; }
}

public class ScoreManager
{
    // Standard .NET event pattern
    public event EventHandler<ScoreEventArgs> ScoreChanged;
    
    private int _score;
    
    public void AddScore(int points)
    {
        int oldScore = _score;
        _score += points;
        
        OnScoreChanged(new ScoreEventArgs 
        { 
            NewScore = _score, 
            Delta = points 
        });
    }
    
    protected virtual void OnScoreChanged(ScoreEventArgs e)
    {
        ScoreChanged?.Invoke(this, e);
    }
}

// Usage
var manager = new ScoreManager();
manager.ScoreChanged += (sender, e) => 
{
    Console.WriteLine($"Score changed by {e.Delta} to {e.NewScore}");
};
manager.AddScore(10); // Output: Score changed by 10 to 10
```

### Event Best Practices

1. **Name events with "On" prefix or past tense:** `OnClick`, `Clicked`, `ScoreChanged`
2. **Use EventHandler<T> for standard .NET pattern**
3. **Always check for null before invoking:** `MyEvent?.Invoke()`
4. **Provide protected virtual OnEventName method** for derived classes to override
5. **Make events public, invocation private**

---

## 4. Observer Design Pattern

### Pattern Overview

The Observer pattern defines a one-to-many dependency between objects. When one object (the Subject) changes state, all its dependents (Observers) are notified automatically.

### Classic Implementation

```csharp
// Observer interface
public interface IObserver
{
    void Update(string message);
}

// Subject interface
public interface ISubject
{
    void Subscribe(IObserver observer);
    void Unsubscribe(IObserver observer);
    void Notify(string message);
}

// Concrete Subject
public class NewsPublisher : ISubject
{
    private List<IObserver> _observers = new List<IObserver>();
    
    public void Subscribe(IObserver observer)
    {
        if (!_observers.Contains(observer))
            _observers.Add(observer);
    }
    
    public void Unsubscribe(IObserver observer)
    {
        _observers.Remove(observer);
    }
    
    public void NotifyAll(string message)
    {
        foreach (var observer in _observers)
        {
            observer.Update(message);
        }
    }
    
    public void PublishNews(string news)
    {
        Console.WriteLine($"Breaking news: {news}");
        NotifyAll(news);
    }
}

// Concrete Observers
public class EmailSubscriber : IObserver
{
    private string _email;
    
    public EmailSubscriber(string email)
    {
        _email = email;
    }
    
    public void Update(string message)
    {
        Console.WriteLine($"Email to {_email}: {message}");
    }
}

public class SMSSubscriber : IObserver
{
    private string _phoneNumber;
    
    public SMSSubscriber(string phoneNumber)
    {
        _phoneNumber = phoneNumber;
    }
    
    public void Update(string message)
    {
        Console.WriteLine($"SMS to {_phoneNumber}: {message}");
    }
}

```

```csharp
// Usage
var publisher = new NewsPublisher();
var emailSub = new EmailSubscriber("user@example.com");
var smsSub = new SMSSubscriber("+1234567890");

publisher.Subscribe(emailSub);
publisher.Subscribe(smsSub);

publisher.PublishNews("Major event occurred!");
// Output:
// Breaking news: Major event occurred!
// Email to user@example.com: Major event occurred!
// SMS to +1234567890: Major event occurred!

publisher.Unsubscribe(emailSub);
publisher.PublishNews("Another update");
// Output:
// Breaking news: Another update
// SMS to +1234567890: Another update
```

### Observer Pattern in Games

```csharp
// Game-specific implementation
public class HealthSystem : ISubject
{
    private List<IObserver> _observers = new List<IObserver>();
    private int _health = 100;
    
    public void Subscribe(IObserver observer) 
    { 
        _observers.Add(observer); 
    }
    
    public void Unsubscribe(IObserver observer) 
    { 
        _observers.Remove(observer); 
    }
    
    public void NotifyAll(string message) 
    { 
        foreach (var obs in _observers) 
            obs.Update(message); 
    }
    
    public void TakeDamage(int damage)
    {
        _health -= damage;
        NotifyAll($"Health: {_health}");
    }
}

// UI Observer
public class HealthUI : IObserver
{
    public void Update(string message)
    {
        Console.WriteLine($"[UI] Updating health display: {message}");
    }
}

// Sound Observer
public class SoundManager : IObserver
{
    public void Update(string message)
    {
        if (message.Contains("Died"))
            Console.WriteLine("[Sound] Playing death sound");
        else
            Console.WriteLine("[Sound] Playing damage sound");
    }
}
```

### Problems with Classic Observer Pattern

1. **Tight coupling:** Observers must implement interface
2. **Type limitation:** All observers receive same message type
3. **No priority:** Cannot control execution order
4. **Memory leaks:** Easy to forget detaching observers
5. **Boilerplate:** Lots of interface implementation code

**Solution:** Modern event systems address these issues!

---

## 5. Simple String-Based Event System

Let's build a simple event system that improves on the Observer pattern.

### Version 1: Basic String Events

```csharp
public class SimpleEventSystem
{
    // Dictionary mapping event names to handler lists
    private Dictionary<string, List<Action<string>>> _events;
    
    public SimpleEventSystem()
    {
        _events = new Dictionary<string, List<Action<string>>>();
    }
    
    // Subscribe to an event
    public void Subscribe(string eventName, Action<string> handler)
    {
        if (!_events.ContainsKey(eventName))
        {
            _events[eventName] = new List<Action<string>>();
        }
        
        _events[eventName].Add(handler);
    }
    
    // Publish an event
    public void Publish(string eventName, string data)
    {
        if (_events.ContainsKey(eventName))
        {
            foreach (var handler in _events[eventName])
            {
                handler(data);
            }
        }
    }
}

```

```csharp
// Usage
var eventSystem = new SimpleEventSystem();

// Subscribe multiple handlers to the same event
eventSystem.Subscribe("PlayerDied", (data) => 
{
    Console.WriteLine($"UI: Show death screen - {data}");
});

eventSystem.Subscribe("PlayerDied", (data) => 
{
    Console.WriteLine($"Sound: Play death sound - {data}");
});

eventSystem.Subscribe("PlayerDied", (data) => 
{
    Console.WriteLine($"Stats: Record death - {data}");
});

// Publish event
eventSystem.Publish("PlayerDied", "Fell off cliff");
// Output:
// UI: Show death screen - Fell off cliff
// Sound: Play death sound - Fell off cliff
// Stats: Record death - Fell off cliff
```

### Version 2: Adding Unsubscribe

```csharp
public class SimpleEventSystem
{
    private Dictionary<string, List<Action<string>>> _events;
    
    public SimpleEventSystem()
    {
        _events = new Dictionary<string, List<Action<string>>>();
    }
    
    public void Subscribe(string eventName, Action<string> handler)
    {
        if (!_events.ContainsKey(eventName))
        {
            _events[eventName] = new List<Action<string>>();
        }
        
        _events[eventName].Add(handler);
    }
    
    // NEW: Unsubscribe method
    public void Unsubscribe(string eventName, Action<string> handler)
    {
        if (_events.ContainsKey(eventName))
        {
            _events[eventName].Remove(handler);
            
            // Clean up empty lists
            if (_events[eventName].Count == 0)
            {
                _events.Remove(eventName);
            }
        }
    }
    
    public void Publish(string eventName, string data)
    {
        if (_events.ContainsKey(eventName))
        {
            // Create a copy to avoid modification during iteration
            var handlers = new List<Action<string>>(_events[eventName]);
            
            foreach (var handler in handlers)
            {
                handler(data);
            }
        }
    }
}

```

```csharp
// Usage with unsubscribe
var eventSystem = new SimpleEventSystem();

Action<string> tempHandler = (data) => 
{
    Console.WriteLine($"Temporary handler: {data}");
};

eventSystem.Subscribe("Test", tempHandler);
eventSystem.Publish("Test", "Message 1"); // Temporary handler receives this

eventSystem.Unsubscribe("Test", tempHandler);
eventSystem.Publish("Test", "Message 2"); // Temporary handler does NOT receive this
```

### Advantages of String-Based System

- Simple to understand
- No interface requirements
- Dynamic event names
- Easy to debug

### Disadvantages

- No type safety (everything is string)
- Typos in event names cause silent failures
- No compile-time checking
- Performance overhead from string lookups
- Cannot pass complex data easily

---

## 6. Adding Event Handlers

Let's evolve our system to support generic types for type safety.

### Version 3: Type-Safe Generic Events

```csharp
public class TypedEventSystem
{
    // Dictionary mapping event Types to handler lists
    private Dictionary<Type, List<Delegate>> _events;
    
    public TypedEventSystem()
    {
        _events = new Dictionary<Type, List<Delegate>>();
    }
    
    // Subscribe with generic type
    public void Subscribe<T>(Action<T> handler)
    {
        Type eventType = typeof(T);
        
        if (!_events.ContainsKey(eventType))
        {
            _events[eventType] = new List<Delegate>();
        }
        
        _events[eventType].Add(handler);
    }
    
    // Unsubscribe with generic type
    public void Unsubscribe<T>(Action<T> handler)
    {
        Type eventType = typeof(T);
        
        if (_events.ContainsKey(eventType))
        {
            _events[eventType].Remove(handler);
        }
    }
    
    // Publish with generic type
    public void Publish<T>(T eventData)
    {
        Type eventType = typeof(T);
        
        if (_events.ContainsKey(eventType))
        {
            // Make a copy for safe iteration
            var handlers = new List<Delegate>(_events[eventType]);
            
            foreach (var handler in handlers)
            {
                // Cast and invoke
                var typedHandler = handler as Action<T>;
                typedHandler?.Invoke(eventData);
            }
        }
    }
}

// Define event types as classes or structs
public class PlayerDiedEvent
{
    public string Reason { get; set; }
    public Vector3 Location { get; set; }
    public int Score { get; set; }
}

public class ScoreChangedEvent
{
    public int OldScore { get; set; }
    public int NewScore { get; set; }
}
```

```csharp

// Usage
var eventSystem = new TypedEventSystem();

// Type-safe subscription
eventSystem.Subscribe<PlayerDiedEvent>((evt) => 
{
    Console.WriteLine($"Player died: {evt.Reason} at {evt.Location}");
});

eventSystem.Subscribe<ScoreChangedEvent>((evt) => 
{
    Console.WriteLine($"Score: {evt.OldScore} -> {evt.NewScore}");
});

// Type-safe publishing
eventSystem.Publish(new PlayerDiedEvent 
{ 
    Reason = "Fell off cliff", 
    Location = new Vector3(10, 0, 5),
    Score = 1500
});

eventSystem.Publish(new ScoreChangedEvent 
{ 
    OldScore = 100, 
    NewScore = 150 
});

// Compile error - type mismatch caught at compile time!
// eventSystem.Subscribe<PlayerDiedEvent>((ScoreChangedEvent evt) => { }); // Won't compile!
```

### Benefits of Type-Safe System

- Compile-time type checking
- IntelliSense support
- Refactoring-friendly
- Complex data structures supported
- Self-documenting code

---

## 7. Priority-Based Execution

Sometimes you need control over the order in which handlers execute.

### Version 4: Adding Priority

```csharp
// Internal subscription record
internal class Subscription
{
    public Delegate Handler { get; set; }
    public int Priority { get; set; }
}

public class PriorityEventSystem
{
    private Dictionary<Type, List<Subscription>> _events;
    
    public PriorityEventSystem()
    {
        _events = new Dictionary<Type, List<Subscription>>();
    }
    
    // Subscribe with priority (lower numbers = higher priority = runs first)
    public void Subscribe<T>(Action<T> handler, int priority = 0)
    {
        Type eventType = typeof(T);
        
        if (!_events.ContainsKey(eventType))
        {
            _events[eventType] = new List<Subscription>();
        }
        
        var subscription = new Subscription
        {
            Handler = handler,
            Priority = priority
        };
        
        _events[eventType].Add(subscription);
        
        // Sort by priority (lower number = runs first)
        _events[eventType].Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }
    
    public void Publish<T>(T eventData)
    {
        Type eventType = typeof(T);
        
        if (_events.ContainsKey(eventType))
        {
            var subscriptions = new List<Subscription>(_events[eventType]);
            
            foreach (var sub in subscriptions)
            {
                var handler = sub.Handler as Action<T>;
                handler?.Invoke(eventData);
            }
        }
    }
}

// Define priority constants for clarity
public static class EventPriority
{
    public const int Critical = -100;
    public const int High = -50;
    public const int Normal = 0;
    public const int Low = 50;
    public const int VeryLow = 100;
}

```

```csharp
// Usage
var eventSystem = new PriorityEventSystem();

eventSystem.Subscribe<PlayerDiedEvent>((evt) => 
{
    Console.WriteLine("3. Low priority: Update UI");
}, EventPriority.Low);

eventSystem.Subscribe<PlayerDiedEvent>((evt) => 
{
    Console.WriteLine("1. Critical: Save game state");
}, EventPriority.Critical);

eventSystem.Subscribe<PlayerDiedEvent>((evt) => 
{
    Console.WriteLine("2. Normal: Play sound");
}, EventPriority.Normal);

eventSystem.Publish(new PlayerDiedEvent { Reason = "Defeated by enemy" });
// Output (in priority order):
// 1. Critical: Save game state
// 2. Normal: Play sound
// 3. Low priority: Update UI
```

### Priority Use Cases

**Priority -100 (Critical/Systems):**
- Core game state changes
- Physics updates
- Save game operations

**Priority 0 (Normal/Gameplay):**
- Standard gameplay logic
- AI reactions
- Animation triggers

**Priority 50 (Low/UI):**
- UI updates
- Visual effects
- Sound effects

**Priority 100 (VeryLow/Telemetry):**
- Analytics
- Logging
- Debug info

---

## 8. One-Shot Subscriptions

Some handlers should only run once and then automatically unsubscribe.

### Version 5: Adding Once Flag

```csharp
internal class Subscription
{
    public Delegate Handler { get; set; }
    public int Priority { get; set; }
    public bool Once { get; set; } // NEW: One-shot flag
}

public class EventSystemWithOnce
{
    private Dictionary<Type, List<Subscription>> _events;
    
    public EventSystemWithOnce()
    {
        _events = new Dictionary<Type, List<Subscription>>();
    }
    
    public void Subscribe<T>(Action<T> handler, int priority = 0, bool once = false)
    {
        Type eventType = typeof(T);
        
        if (!_events.ContainsKey(eventType))
        {
            _events[eventType] = new List<Subscription>();
        }
        
        var subscription = new Subscription
        {
            Handler = handler,
            Priority = priority,
            Once = once // Store the once flag
        };
        
        _events[eventType].Add(subscription);
        _events[eventType].Sort((a, b) => a.Priority.CompareTo(b.Priority));
    }
    
    public void Publish<T>(T eventData)
    {
        Type eventType = typeof(T);
        
        if (!_events.ContainsKey(eventType))
            return;
            
        var subscriptions = new List<Subscription>(_events[eventType]);
        var toRemove = new List<Subscription>(); // Track one-shot handlers
        
        foreach (var sub in subscriptions)
        {
            var handler = sub.Handler as Action<T>;
            handler?.Invoke(eventData);
            
            // Mark for removal if it's a one-shot
            if (sub.Once)
            {
                toRemove.Add(sub);
            }
        }
        
        // Remove one-shot subscriptions
        foreach (var sub in toRemove)
        {
            _events[eventType].Remove(sub);
        }
    }
}

```

```csharp
// Usage Examples
var eventSystem = new EventSystemWithOnce();

// Regular subscription (fires every time)
eventSystem.Subscribe<GameStartEvent>((evt) => 
{
    Console.WriteLine("This runs every time");
}, once: false);

// One-shot subscription (fires only once)
eventSystem.Subscribe<GameStartEvent>((evt) => 
{
    Console.WriteLine("This runs only the first time");
}, once: true);

// First publish
eventSystem.Publish(new GameStartEvent());
// Output:
// This runs every time
// This runs only the first time

// Second publish
eventSystem.Publish(new GameStartEvent());
// Output:
// This runs every time
// (one-shot handler is gone)
```

### Practical One-Shot Examples

```csharp
// Tutorial system - show help only once
eventSystem.Subscribe<FirstEnemyEncounteredEvent>((evt) => 
{
    ShowTutorial("How to fight enemies");
}, once: true);

// Achievement unlock - fire only on first completion
eventSystem.Subscribe<Level1CompletedEvent>((evt) => 
{
    UnlockAchievement("Novice Adventurer");
}, once: true);

// Cutscene trigger - play once per game session
eventSystem.Subscribe<PlayerEnteredBossRoomEvent>((evt) => 
{
    PlayCutscene("BossIntroduction");
}, once: true);

// Initialization callback - setup that runs once
eventSystem.Subscribe<GameInitializedEvent>((evt) => 
{
    LoadPlayerPreferences();
    InitializeAudioSystem();
    ConnectToServer();
}, priority: EventPriority.Critical, once: true);
```

---

## 9. Subscription Objects and Disposal

The current system has a problem: how do you unsubscribe when you used a lambda or anonymous function?

### The Unsubscribe Problem

```csharp
// Problem: Can't unsubscribe lambdas easily
eventSystem.Subscribe<PlayerDiedEvent>((evt) => 
{
    Console.WriteLine("Handler 1");
});

// How do we unsubscribe this? We don't have a reference to the lambda!
```

### Solution: Return a Subscription Token

```csharp
// Subscription token that can be disposed
public class SubscriptionToken : IDisposable
{
    private Action _unsubscribe;
    private bool _disposed;
    
    public SubscriptionToken(Action unsubscribe)
    {
        _unsubscribe = unsubscribe;
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;
            
        _unsubscribe?.Invoke();
        _unsubscribe = null;
        _disposed = true;
    }
}

public class EventSystemWithDisposal
{
    private Dictionary<Type, List<Subscription>> _events;
    
    public EventSystemWithDisposal()
    {
        _events = new Dictionary<Type, List<Subscription>>();
    }
    
    // Now returns a disposable token
    public IDisposable Subscribe<T>(Action<T> handler, int priority = 0, bool once = false)
    {
        Type eventType = typeof(T);
        
        if (!_events.ContainsKey(eventType))
        {
            _events[eventType] = new List<Subscription>();
        }
        
        var subscription = new Subscription
        {
            Handler = handler,
            Priority = priority,
            Once = once
        };
        
        _events[eventType].Add(subscription);
        _events[eventType].Sort((a, b) => a.Priority.CompareTo(b.Priority));
        
        // Return a token that knows how to unsubscribe
        return new SubscriptionToken(() => Unsubscribe(eventType, subscription));
    }
    
    private void Unsubscribe(Type eventType, Subscription subscription)
    {
        if (_events.ContainsKey(eventType))
        {
            _events[eventType].Remove(subscription);
        }
    }
    
    public void Publish<T>(T eventData)
    {
        Type eventType = typeof(T);
        
        if (!_events.ContainsKey(eventType))
            return;
            
        var subscriptions = new List<Subscription>(_events[eventType]);
        var toRemove = new List<Subscription>();
        
        foreach (var sub in subscriptions)
        {
            var handler = sub.Handler as Action<T>;
            handler?.Invoke(eventData);
            
            if (sub.Once)
                toRemove.Add(sub);
        }
        
        foreach (var sub in toRemove)
        {
            _events[eventType].Remove(sub);
        }
    }
}

```

```csharp

// Usage Pattern 1: Manual disposal
var subscription = eventSystem.Subscribe<PlayerDiedEvent>((evt) => 
{
    Console.WriteLine("Temporary handler");
});

// Later, when no longer needed
subscription.Dispose(); // Unsubscribes automatically

// Usage Pattern 2: Using statement (automatic disposal)
using (var sub = eventSystem.Subscribe<PlayerDiedEvent>((evt) => 
{
    Console.WriteLine("Scoped handler");
}))
{
    // Handler is active here
    eventSystem.Publish(new PlayerDiedEvent());
} // Automatically unsubscribes when leaving scope

// Usage Pattern 3: Component lifecycle
public class EnemyAI : Component
{
    private IDisposable _subscription;
    
    public void OnEnable()
    {
        _subscription = eventSystem.Subscribe<PlayerSpottedEvent>((evt) => 
        {
            ChasePlayer(evt.PlayerPosition);
        });
    }
    
    public void OnDisable()
    {
        _subscription?.Dispose(); // Clean unsubscribe
    }
}
```

### Benefits of Disposable Subscriptions

- No memory leaks from forgotten subscriptions
- Works with lambdas and anonymous functions
- Integrates with `using` statements
- Clear lifetime management
- Follows .NET disposal patterns

---

## 10. Complete Type-Safe Event System

Let's put it all together into a production-ready event system.

### Final Implementation

```csharp
using System;
using System.Collections.Generic;

namespace EventSystem
{
    /// <summary>
    /// Internal subscription record with priority, filter, and one-shot support
    /// </summary>
    internal class Subscription
    {
        public Delegate Handler { get; set; }
        public int Priority { get; set; }
        public bool Once { get; set; }
        public Func<object, bool> Filter { get; set; }
    }

    /// <summary>
    /// Disposable subscription token for clean unsubscription
    /// </summary>
    public class SubscriptionToken : IDisposable
    {
        private Action _unsubscribe;
        private bool _disposed;
        
        public SubscriptionToken(Action unsubscribe)
        {
            _unsubscribe = unsubscribe;
        }
        
        public void Dispose()
        {
            if (_disposed)
                return;
                
            _unsubscribe?.Invoke();
            _unsubscribe = null;
            _disposed = true;
        }
    }

    /// <summary>
    /// Priority constants for event handler ordering
    /// </summary>
    public static class EventPriority
    {
        public const int Critical = -100;  // Core systems
        public const int High = -50;       // Important gameplay
        public const int Normal = 0;       // Standard gameplay
        public const int Low = 50;         // UI updates
        public const int VeryLow = 100;    // Telemetry/logging
    }

    /// <summary>
    /// Complete type-safe event system with priority, filtering, and one-shot support
    /// </summary>
    public class EventBus
    {
        private Dictionary<Type, List<Subscription>> _events;
        private object _lock = new object();
        
        public EventBus()
        {
            _events = new Dictionary<Type, List<Subscription>>();
        }
        
        /// <summary>
        /// Subscribe to events of type T
        /// </summary>
        /// <param name="handler">The handler to invoke when event fires</param>
        /// <param name="priority">Execution priority (lower = earlier, default 0)</param>
        /// <param name="filter">Optional filter predicate</param>
        /// <param name="once">If true, auto-unsubscribe after first invocation</param>
        /// <returns>Disposable token for unsubscribing</returns>
        public IDisposable Subscribe<T>(
            Action<T> handler, 
            int priority = 0, 
            Predicate<T> filter = null,
            bool once = false)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
                
            Type eventType = typeof(T);
            
            // Convert typed filter to object-based filter for storage
            Func<object, bool> objectFilter = null;
            if (filter != null)
                objectFilter = (obj) => filter((T)obj);
            
            var subscription = new Subscription
            {
                Handler = handler,
                Priority = priority,
                Once = once,
                Filter = objectFilter
            };
            
            lock (_lock)
            {
                if (!_events.ContainsKey(eventType))
                {
                    _events[eventType] = new List<Subscription>();
                }
                
                _events[eventType].Add(subscription);
                
                // Sort by priority (lower numbers run first)
                _events[eventType].Sort((a, b) => a.Priority.CompareTo(b.Priority));
            }
            
            // Return disposable token
            return new SubscriptionToken(() => Unsubscribe(eventType, subscription));
        }
        
        /// <summary>
        /// Publish an event to all subscribers
        /// </summary>
        public void Publish<T>(T eventData)
        {
            if (eventData == null)
                return;
                
            Type eventType = typeof(T);
            List<Subscription> subscriptions;
            
            // Get snapshot of current subscriptions
            lock (_lock)
            {
                if (!_events.ContainsKey(eventType))
                    return;
                    
                subscriptions = new List<Subscription>(_events[eventType]);
            }
            
            var toRemove = new List<Subscription>();
            
            // Invoke handlers in priority order
            foreach (var sub in subscriptions)
            {
                // Apply filter if present
                if (sub.Filter != null && !sub.Filter(eventData))
                    continue;
                
                try
                {
                    var handler = sub.Handler as Action<T>;
                    handler?.Invoke(eventData);
                }
                catch (Exception ex)
                {
                    // Log error but continue processing other handlers
                    Console.WriteLine($"Event handler error: {ex.Message}");
                }
                
                // Mark one-shot handlers for removal
                if (sub.Once)
                    toRemove.Add(sub);
            }
            
            // Remove one-shot subscriptions
            if (toRemove.Count > 0)
            {
                lock (_lock)
                {
                    foreach (var sub in toRemove)
                    {
                        _events[eventType].Remove(sub);
                    }
                }
            }
        }
        
        /// <summary>
        /// Internal unsubscribe called by subscription tokens
        /// </summary>
        private void Unsubscribe(Type eventType, Subscription subscription)
        {
            lock (_lock)
            {
                if (_events.ContainsKey(eventType))
                {
                    _events[eventType].Remove(subscription);
                    
                    // Clean up empty event lists
                    if (_events[eventType].Count == 0)
                        _events.Remove(eventType);
                }
            }
        }
        
        /// <summary>
        /// Clear all subscriptions
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _events.Clear();
            }
        }
        
        /// <summary>
        /// Get count of subscribers for a specific event type
        /// </summary>
        public int GetSubscriberCount<T>()
        {
            Type eventType = typeof(T);
            lock (_lock)
            {
                return _events.ContainsKey(eventType) ? _events[eventType].Count : 0;
            }
        }
    }
}
```


### Complete Usage Example

```csharp
// Define event types
public class PlayerDiedEvent
{
    public string Reason { get; set; }
    public int FinalScore { get; set; }
}

public class ScoreChangedEvent
{
    public int OldScore { get; set; }
    public int NewScore { get; set; }
    public int Delta => NewScore - OldScore;
}

public class ItemCollectedEvent
{
    public string ItemName { get; set; }
    public int Quantity { get; set; }
}

// Usage in game
public class Game
{
    private EventBus _eventBus;
    private List<IDisposable> _subscriptions;
    
    public void Initialize()
    {
        _eventBus = new EventBus();
        _subscriptions = new List<IDisposable>();
        
        // Critical priority: Save game on death
        _subscriptions.Add(_eventBus.Subscribe<PlayerDiedEvent>((evt) => 
        {
            SaveGameState();
            Console.WriteLine($"[Critical] Game saved. Score: {evt.FinalScore}");
        }, EventPriority.Critical));
        
        // Normal priority: Gameplay logic
        _subscriptions.Add(_eventBus.Subscribe<PlayerDiedEvent>((evt) => 
        {
            DisablePlayerControls();
            Console.WriteLine($"[Gameplay] Player died: {evt.Reason}");
        }, EventPriority.Normal));
        
        // Low priority: UI updates
        _subscriptions.Add(_eventBus.Subscribe<PlayerDiedEvent>((evt) => 
        {
            ShowDeathScreen();
            Console.WriteLine("[UI] Death screen displayed");
        }, EventPriority.Low));
        
        // Filtered subscription: only for big score changes
        _subscriptions.Add(_eventBus.Subscribe<ScoreChangedEvent>((evt) => 
        {
            Console.WriteLine($"[Achievement] Big score increase: +{evt.Delta}!");
        }, 
        priority: EventPriority.Normal,
        filter: (evt) => evt.Delta >= 100));
        
        // One-shot: First item collection tutorial
        _subscriptions.Add(_eventBus.Subscribe<ItemCollectedEvent>((evt) => 
        {
            ShowTutorial("You collected an item! Press I to view inventory.");
        }, once: true));
    }
    
    public void OnPlayerDeath(string reason, int score)
    {
        _eventBus.Publish(new PlayerDiedEvent 
        { 
            Reason = reason, 
            FinalScore = score 
        });
    }
    
    public void OnScoreChanged(int oldScore, int newScore)
    {
        _eventBus.Publish(new ScoreChangedEvent 
        { 
            OldScore = oldScore, 
            NewScore = newScore 
        });
    }
    
    public void OnItemCollected(string itemName, int quantity)
    {
        _eventBus.Publish(new ItemCollectedEvent 
        { 
            ItemName = itemName, 
            Quantity = quantity 
        });
    }
    
    public void Cleanup()
    {
        // Dispose all subscriptions
        foreach (var sub in _subscriptions)
        {
            sub.Dispose();
        }
        _subscriptions.Clear();
        _eventBus.Clear();
    }
    
    // Stub methods
    private void SaveGameState() { }
    private void DisablePlayerControls() { }
    private void ShowDeathScreen() { }
    private void ShowTutorial(string message) { }
}

// Example execution
var game = new Game();
game.Initialize();

// Trigger events
game.OnPlayerDeath("Fell off cliff", 1500);
// Output (in priority order):
// [Critical] Game saved. Score: 1500
// [Gameplay] Player died: Fell off cliff
// [UI] Death screen displayed

game.OnScoreChanged(100, 150);
// (No achievement output - delta is only 50)

game.OnScoreChanged(100, 250);
// [Achievement] Big score increase: +150!

game.OnItemCollected("Health Potion", 1);
// Tutorial shown (only once)

game.OnItemCollected("Mana Potion", 1);
// No tutorial (one-shot already fired)

game.Cleanup();
```

---

## Summary and Best Practices

### What We've Built

1.  **Type-safe** event system using generics
2.  **Priority-based** handler execution
3.  **Filtering** support for conditional handlers
4.  **One-shot** subscriptions that auto-unsubscribe
5.  **Disposable** subscription tokens for clean lifecycle management
6.  **Thread-safe** with proper locking
7.  **Error handling** that prevents one bad handler from breaking others

### When to Use Event Systems

**Good Use Cases:**
- Decoupling game systems (UI, audio, gameplay)
- Achievement and analytics tracking
- Tutorial and notification systems
- Game state changes (pause, level complete, game over)
- Player actions (movement, combat, inventory)

**Bad Use Cases:**
- Tight game loops requiring high performance (use direct calls)
- Communication between tightly coupled systems
- Simple one-to-one communication (use direct references)
- Real-time physics or rendering (too much overhead)

### Performance Considerations

1. **Event allocation:** Create event objects sparingly, consider object pooling
2. **Lock contention:** Minimize time spent in locks
3. **Handler count:** Keep subscriber lists reasonably sized
4. **Filter complexity:** Keep filter predicates simple and fast
5. **One-shot cleanup:** Happens during dispatch, minimal overhead

### Memory Management Tips

```csharp
// BAD: Memory leak - never disposed
public class LeakyComponent
{
    public void Start()
    {
        eventBus.Subscribe<GameEvent>((evt) => HandleEvent(evt));
        // Subscription never cleaned up!
    }
}

// GOOD: Proper disposal
public class ProperComponent
{
    private IDisposable _subscription;
    
    public void Start()
    {
        _subscription = eventBus.Subscribe<GameEvent>((evt) => HandleEvent(evt));
    }
    
    public void OnDestroy()
    {
        _subscription?.Dispose();
    }
}

// BETTER: Using statement for automatic cleanup
public class BetterComponent
{
    public void TemporaryHandler()
    {
        using (var sub = eventBus.Subscribe<GameEvent>((evt) => HandleEvent(evt)))
        {
            // Handler active only in this scope
            DoSomething();
        } // Automatically disposed
    }
}
```

### Testing Event Systems

```csharp
[Test]
public void TestEventDelivery()
{
    var eventBus = new EventBus();
    bool handlerCalled = false;
    
    eventBus.Subscribe<TestEvent>((evt) => handlerCalled = true);
    eventBus.Publish(new TestEvent());
    
    Assert.IsTrue(handlerCalled);
}

[Test]
public void TestPriorityOrder()
{
    var eventBus = new EventBus();
    var order = new List<int>();
    
    eventBus.Subscribe<TestEvent>((evt) => order.Add(3), priority: 50);
    eventBus.Subscribe<TestEvent>((evt) => order.Add(1), priority: -50);
    eventBus.Subscribe<TestEvent>((evt) => order.Add(2), priority: 0);
    
    eventBus.Publish(new TestEvent());
    
    Assert.AreEqual(new[] { 1, 2, 3 }, order);
}

[Test]
public void TestOnceFlag()
{
    var eventBus = new EventBus();
    int callCount = 0;
    
    eventBus.Subscribe<TestEvent>((evt) => callCount++, once: true);
    
    eventBus.Publish(new TestEvent());
    eventBus.Publish(new TestEvent());
    
    Assert.AreEqual(1, callCount);
}
```

---

## 11. Appendix A: Common Pitfalls

### Pitfall 1: Circular Event Dependencies
```csharp
// BAD: Can cause infinite loops
eventBus.Subscribe<EventA>((evt) => eventBus.Publish(new EventB()));
eventBus.Subscribe<EventB>((evt) => eventBus.Publish(new EventA()));
```

### Pitfall 2: Modifying Collections During Iteration
```csharp
// BAD: Modifying list while iterating
foreach (var handler in handlers)
{
    handlers.Remove(handler); // InvalidOperationException!
}

// GOOD: Copy first, then modify
var copy = new List<Handler>(handlers);
foreach (var handler in copy)
{
    handlers.Remove(handler);
}
```

### Pitfall 3: Forgetting Null Checks
```csharp
// BAD: Can throw NullReferenceException
Action<string> handler = null;
handler("Test"); // Boom!

// GOOD: Null-conditional operator
handler?.Invoke("Test");
```

### Pitfall 4: Memory Leaks from Forgotten Subscriptions
```csharp
// BAD: Subscription keeps object alive
public class Component
{
    public Component()
    {
        eventBus.Subscribe<GameEvent>((evt) => this.OnEvent(evt));
        // 'this' captured in lambda - object won't be GC'd!
    }
}

// GOOD: Explicit disposal
public class Component : IDisposable
{
    private IDisposable _sub;
    
    public Component()
    {
        _sub = eventBus.Subscribe<GameEvent>((evt) => this.OnEvent(evt));
    }
    
    public void Dispose()
    {
        _sub?.Dispose();
    }
}
```

---

## 12. Appendix B: Event Bus Fluent Builder Examples

## Example 1 — Basic subscription
**Description:** Subscribe to all `PlayerDamagedEvent` events.

```csharp
bus.On<PlayerDamagedEvent>()
   .Do(e => Console.WriteLine($"Damage: {e.Amount}"));
```

**Explanation:**  
This registers a handler that prints the damage amount every time a `PlayerDamagedEvent` is dispatched.

## Example 2 — Subscription with filter (`When`)
**Description:** Only react to “big hits”.

```csharp
bus.On<PlayerDamagedEvent>()
   .When(e => e.Amount >= 20)
   .Do(HandleBigHit);
```

**Explanation:**  
The handler only runs if the damage is **20 or higher**.

## Example 3 — Priority preset
**Description:** Ensure gameplay logic runs before UI systems.

```csharp
bus.On<PlayerDamagedEvent>()
   .WithPriorityPreset(EventPriority.Gameplay)
   .Do(UpdateHealthUI);
```

**Explanation:**  
Gameplay-level subscribers run before UI or telemetry listeners.

## Example 4 — One-shot subscription (`Once`)
**Description:** React only to the *first* level completion.

```csharp
bus.On<LevelCompletedEvent>()
   .Once()
   .Do(e => ShowEndScreen(e.LevelId));
```

**Explanation:**  
After firing once, the handler is automatically removed.

## Example 5 — Combined: filter + priority + once
**Description:** Trigger an achievement when the first boss dies.

```csharp
bus.On<EnemyDiedEvent>()
   .WithPriorityPreset(EventPriority.Gameplay)
   .When(e => e.IsBoss)
   .Once()
   .Do(e => UnlockAchievement("FirstBossKill"));
```

**Explanation:**  
Only fires for boss deaths, runs in gameplay phase, and triggers once.

## Example 6 — Guarding against null (“When not null”)
**Description:** Only update inventory if a valid item is included.

```csharp
bus.On<ItemPickedUpEvent>()
   .When(e => e.Item != null)
   .Do(e => AddToInventory(e.Item));
```

**Explanation:**  
Filters out malformed events.

## Example 7 — Auto-stop condition (`Until`)
**Description:** Stop reacting once the player’s health reaches zero.

```csharp
bus.On<PlayerDamagedEvent>()
   .When(e => e.Amount > 0)
   .Until(e => player.Health <= 0)
   .Do(e => UpdateHealthUI());
```

**Explanation:**  
Subscription automatically removes itself when the stop-condition becomes true.

## Example 8 — Manual unsubscribe (lifetime-managed)
**Description:** A powerup subscribes when active and unsubscribes when it expires.

```csharp
private IDisposable _sub;

public void Activate()
{
    _sub = bus.On<PlayerDamagedEvent>()
             .Do(e => ReduceDamageTaken(e));
}

public void Deactivate()
{
    _sub?.Dispose();
}
```

**Explanation:**  
Shows how to explicitly manage subscription lifetimes.

## Example 9 — Using builder inside a component
**Description:** A camera component reacts to player movement.

```csharp
public sealed class CameraFollow : Component
{
    private IDisposable _sub;

    public override void OnEnable()
    {
        _sub = bus.On<PlayerMovedEvent>()
                 .Do(e => Follow(e.NewPosition));
    }

    public override void OnDisable()
    {
        _sub?.Dispose();
    }
}
```

**Explanation:**  
Demonstrates real-world integration inside component lifecycle methods.

