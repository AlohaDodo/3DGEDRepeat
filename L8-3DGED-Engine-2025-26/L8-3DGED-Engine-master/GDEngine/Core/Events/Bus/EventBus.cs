using System.Collections.Concurrent;

namespace GDEngine.Core.Events
{
    /// <summary>
    /// Lightweight publish/subscribe event hub with:
    /// 1) Main-thread flushing via <see cref="EventBusSystem"/>.
    /// 2) Thread-safe enqueue (Post) + immediate publish option.
    /// 3) Subscriptions with priority, filters, and one-shot.
    /// 4) Optional fluent builder & named priority presets (see EventBusFluent, EventPriority).
    /// </summary>
    /// <see cref="EventBusSystem"/>
    public sealed class EventBus : IDisposable
    {
        #region Static Fields
        #endregion

        #region Fields
        private readonly ConcurrentQueue<object> _queue = new ConcurrentQueue<object>();
        private readonly Dictionary<Type, List<EventSubscription>> _map = new Dictionary<Type, List<EventSubscription>>();
        private readonly object _mapLock = new object();
        private bool _disposed;
        #endregion

        #region Properties
        #endregion

        #region Constructors
        public EventBus()
        {
        }
        #endregion

        #region Methods
        /// <summary>
        /// Subscribe a handler for events of type T.
        /// Supports priority (lower runs first), an optional filter, and one-shot delivery.
        /// Returns an IDisposable you should dispose in OnDestroy/teardown.
        /// </summary>
        public IDisposable Subscribe<T>(Action<T> handler, int priority = 0, Predicate<T>? filter = null, bool once = false)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            // Convert the student's Predicate<T> into a generic object-based filter we can store.
            Func<object, bool>? filterObj = null;
            if (filter != null)
                filterObj = (o) => filter((T)o);

            // Create a small record describing this subscription.
            EventSubscription sub = new EventSubscription(handler, priority, filterObj, once);

            lock (_mapLock) // lock because multiple threads might add/remove subscribers
            {
                if (!_map.TryGetValue(typeof(T), out List<EventSubscription>? list))
                {
                    // First subscriber for this event type � make a list.
                    list = new List<EventSubscription>(4);
                    _map[typeof(T)] = list;
                }

                list.Add(sub);

                // Ensure deterministic call order: lower priority values run first.
                list.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            }

            // Return a tiny token that knows how to remove itself from the bus when disposed.
            return new Unsub(this, typeof(T), sub);
        }

        /// <summary>
        /// Enqueue an event to be delivered on the main thread during Flush (called by EventBusSystem).
        /// Safe from any thread.
        /// </summary>
        public void Post<T>(T evt)
        {
            if (evt != null)
                _queue.Enqueue(evt!);
        }

        /// <summary>
        /// Publish an event by enqueuing it for main-thread delivery.
        /// This is a convenience alias over <see cref="Post{T}(T)"/> so that game
        /// code and orchestration scripts can use "Publish" in a natural way.
        /// </summary>
        public void Publish<T>(T evt)
        {
            if (evt == null)
                return;

            Post(evt);
        }

        /// <summary>
        /// Publish immediately on the current thread (use on main thread only).
        /// </summary>
        public void PublishImmediate<T>(T evt)
        {
            if (evt != null)
                Dispatch(evt!);
        }

        /// <summary>
        /// Called once per frame by <see cref="EventBusSystem"/> on the main thread
        /// to deliver any queued events in a deterministic place in the frame.
        /// </summary>
        internal void DispatchAll()
        {
            while (_queue.TryDequeue(out object? evt))
                Dispatch(evt);
        }
        #endregion

        #region Lifecycle Methods
        #endregion

        #region Housekeeping Methods
        public void Dispose()
        {
            if (_disposed)
                return;

            // Clear all subscribers under a lock to avoid racing with new subscribes.
            lock (_mapLock)
                _map.Clear();

            // Drain the queue so the GC doesn't keep references alive.
            while (_queue.TryDequeue(out _))
            {
            }

            // Mark as disposed � future Dispose() calls do nothing (idempotent).
            _disposed = true;
        }
        #endregion

        #region Methods (private)
        private void Dispatch(object evt)
        {
            if (evt == null)
                return;

            List<EventSubscription>? snapshot = null;

            // Grab a stable snapshot of the current subscribers for this exact event type.
            // We don't iterate the live list to avoid issues if someone subscribes/unsubscribes during dispatch.
            lock (_mapLock)
            {
                if (!_map.TryGetValue(evt.GetType(), out List<EventSubscription>? list) || list.Count == 0)
                    return;

                snapshot = new List<EventSubscription>(list); // copy for safe iteration
            }

            // Track which one-shot subs we need to remove after invoking them.
            List<EventSubscription>? toRemove = null;

            // Walk the snapshot in priority order (already sorted at subscribe-time).
            for (int i = 0; i < snapshot.Count; i++)
            {
                EventSubscription s = snapshot[i];

                // If a filter exists and returns false, skip this handler.
                if (s.Filter != null && !s.Filter(evt))
                    continue;

                try
                {
                    // Handlers are stored as Delegate to keep the bus generic.
                    // DynamicInvoke keeps the student-facing API simple (Action<T>).
                    s.Handler.DynamicInvoke(evt);
                }
                catch
                {
                    // Intentional: swallow to keep the bus robust in class demos.
                    // A broken handler should not crash the entire game loop.
                    // (Later you can plug a logger/overlay here.)
                }

                if (s.Once)
                {
                    if (toRemove == null)
                        toRemove = new List<EventSubscription>(2);

                    toRemove.Add(s);
                }
            }

            // Remove any one-shot subscribers from the *live* list so they won't fire next time.
            if (toRemove != null)
            {
                lock (_mapLock)
                {
                    if (_map.TryGetValue(evt.GetType(), out List<EventSubscription>? list))
                    {
                        for (int i = 0; i < toRemove.Count; i++)
                            list.Remove(toRemove[i]);
                    }
                }
            }
        }
        #endregion

        #region Types (private)
        private sealed class Unsub : IDisposable
        {
            private readonly EventBus _owner;
            private readonly Type _type;
            private EventSubscription _sub;
            private bool _done;

            public Unsub(EventBus owner, Type type, EventSubscription sub)
            {
                _owner = owner;
                _type = type;
                _sub = sub;
            }

            public void Dispose()
            {
                if (_done)
                {
                    return;
                }

                // Remove this exact subscription from the bus under a lock.
                lock (_owner._mapLock)
                {
                    if (_owner._map.TryGetValue(_type, out List<EventSubscription>? list))
                        list.Remove(_sub);
                }

                // Mark so multiple Dispose() calls are safe (no double-remove).
                _done = true;
            }
        }

        private sealed class EventSubscription
        {
            public readonly Delegate Handler;
            public readonly int Priority;
            public readonly Func<object, bool>? Filter;
            public readonly bool Once;

            public EventSubscription(Delegate handler, int priority, Func<object, bool>? filter, bool once)
            {
                Handler = handler;
                Priority = priority;
                Filter = filter;
                Once = once;
            }
        }
        #endregion
    }

    /// <summary>
    /// Readable priority presets for EventBus subscriptions (lower numbers run earlier).
    /// These are guidelines, not a closed set�use WithPriority(...) for custom values.
    /// </summary>
    public static class EventPriority
    {
        /// <summary>
        /// Global coordination & frame ordering.
        /// Examples: scene loading/unloading, mode switches, pausing/resuming,
        /// cancelling or transforming events before systems react, cutscene orchestration.
        /// </summary>
        public const int Orchestrator = -100;

        /// <summary>
        /// Core systems that produce state other code depends on.
        /// Examples: physics resolution applying results, AI decision blackboards,
        /// animation state updates, input mapping, audio pre-mix cues that gameplay/UI may read.
        /// </summary>
        public const int Systems = -20;

        /// <summary>
        /// Default gameplay logic.
        /// Examples: health/damage, inventory updates, spawning/despawning,
        /// score tracking, mission state changes, camera controllers that react to gameplay.
        /// </summary>
        public const int Gameplay = 0;

        /// <summary>
        /// User interface & presentation that should reflect the final gameplay state this frame.
        /// Examples: HUD/overlay updates, menu reactions, notifications, crosshair/reticle changes.
        /// </summary>
        public const int UI = 50;

        /// <summary>
        /// Non-intrusive observers that must never affect gameplay ordering.
        /// Examples: analytics/telemetry, logging, debug capture, profiling hooks.
        /// </summary>
        public const int Telemetry = 100;
    }

    /// <summary>
    /// Fluent helpers for EventBus subscriptions:
    /// bus.On&lt;T&gt;().WithPriority(...).WithPriorityPreset(EventPriority.Gameplay).When(...).WhenNotNull(...).Until(...).Once().Do(handler)
    /// </summary>
    public static class EventBusFluent
    {
        #region API
        public static SubscriptionBuilder<T> On<T>(this EventBus bus)
        {
            return new SubscriptionBuilder<T>(bus);
        }
        #endregion

        #region Types
        /// <summary>
        /// Value-type builder; creates no heap objects until Do/Handle is called.
        /// </summary>
        public readonly struct SubscriptionBuilder<T>
        {
            private readonly EventBus _bus;
            private readonly int _priority;
            private readonly Predicate<T>? _filter;
            private readonly bool _once;
            private readonly Predicate<T>? _until;

            public SubscriptionBuilder(EventBus bus)
            {
                _bus = bus ?? throw new ArgumentNullException(nameof(bus));
                _priority = 0;
                _filter = null;
                _once = false;
                _until = null;
            }

            private SubscriptionBuilder(EventBus bus, int priority, Predicate<T>? filter, bool once, Predicate<T>? until)
            {
                _bus = bus;
                _priority = priority;
                _filter = filter;
                _once = once;
                _until = until;
            }

            /// <summary>Set an explicit integer priority (lower runs earlier).</summary>
            public SubscriptionBuilder<T> WithPriority(int priority)
            {
                return new SubscriptionBuilder<T>(_bus, priority, _filter, _once, _until);
            }

            /// <summary>Use a named preset from <see cref="EventPriority"/>.</summary>
            public SubscriptionBuilder<T> WithPriorityPreset(int preset)
            {
                return WithPriority(preset);
            }

            /// <summary>Filter events; only those for which predicate returns true are delivered.</summary>
            public SubscriptionBuilder<T> When(Predicate<T> predicate)
            {
                return new SubscriptionBuilder<T>(_bus, _priority, predicate, _once, _until);
            }

            /// <summary>
            /// Convenience filter: deliver only when selector(e) is not null.
            /// </summary>
            public SubscriptionBuilder<T> WhenNotNull<U>(Func<T, U> selector) where U : class
            {
                if (selector == null)
                    throw new ArgumentNullException(nameof(selector));

                return When(e => selector(e) != null);
            }

            /// <summary>Mark subscription as one-shot (auto-unsubscribe after first delivery).</summary>
            public SubscriptionBuilder<T> Once()
            {
                return new SubscriptionBuilder<T>(_bus, _priority, _filter, true, _until);
            }

            /// <summary>
            /// Keep listening until the predicate becomes true; then auto-unsubscribe.
            /// Evaluated after the handler runs on each delivered event.
            /// </summary>
            public SubscriptionBuilder<T> Until(Predicate<T> predicate)
            {
                return new SubscriptionBuilder<T>(_bus, _priority, _filter, _once, predicate);
            }

            /// <summary>Finalize: creates the subscription and returns the IDisposable token.</summary>
            public IDisposable Do(Action<T> handler)
            {
                if (handler == null)
                    throw new ArgumentNullException(nameof(handler));

                EventBus bus = _bus;
                int prio = _priority;
                Predicate<T>? filter = _filter;
                bool once = _once;
                Predicate<T>? until = _until;

                IDisposable? token = null;

                Action<T> wrapped = (e) =>
                {
                    handler(e);
                    if (until != null && until(e))
                        token?.Dispose();
                };

                token = bus.Subscribe(wrapped, prio, filter, once);
                return token;
            }
        }
        #endregion
    }
}
