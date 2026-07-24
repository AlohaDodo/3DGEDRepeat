using GDEngine.Core.Systems;
using System.Collections.Concurrent;

namespace GDEngine.Core.Impulses
{
    /// <summary>
    /// Lightweight publish/subscribe impulse hub with:
    /// 1) Main-thread flushing via ImpulseSystem.
    /// 2) Thread-safe enqueue (Post) + immediate publish option.
    /// 3) Subscriptions with priority, filters, and one-shot.
    /// 4) Optional fluent builder & named priority presets ImpulseBusFluent, ImpulsePriority.
    /// 5) Continuous impulse sources driven by ImpulseSystem each frame.
    /// </summary>
    /// <see cref="ImpulseSystem"/>
    public sealed class ImpulseBus : IDisposable
    {
        #region Static Fields
        #endregion

        #region Fields
        private readonly ConcurrentQueue<object> _queue = new ConcurrentQueue<object>();
        private readonly Dictionary<Type, List<ImpulseSubscription>> _map = new Dictionary<Type, List<ImpulseSubscription>>();
        private readonly List<ContinuousSource> _continuousSources = new List<ContinuousSource>();
        private readonly object _mapLock = new object();
        private readonly object _sourceLock = new object();
        private bool _disposed;
        #endregion

        #region Properties
        #endregion

        #region Constructors
        public ImpulseBus()
        {
        }
        #endregion

        #region Methods
        /// <summary>
        /// Subscribe a handler for impulses of type T.
        /// Supports priority (lower runs first), an optional filter, and one-shot delivery.
        /// Returns an IDisposable you should dispose in OnDestroy/teardown.
        /// </summary>
        public IDisposable Subscribe<T>(Action<T> handler, int priority = 0, Predicate<T>? filter = null, bool once = false)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            Func<object, bool>? filterObj = null;
            if (filter != null)
                filterObj = o => filter((T)o);

            ImpulseSubscription sub = new ImpulseSubscription(handler, priority, filterObj, once);

            lock (_mapLock)
            {
                if (!_map.TryGetValue(typeof(T), out List<ImpulseSubscription>? list))
                {
                    list = new List<ImpulseSubscription>(4);
                    _map[typeof(T)] = list;
                }

                list.Add(sub);
                list.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            }

            return new Unsub(this, typeof(T), sub);
        }

        /// <summary>
        /// Enqueue an impulse to be delivered on the main thread during Flush (called by <see cref="ImpulseSystem"/>).
        /// Safe from any thread.
        /// </summary>
        public void Post<T>(T impulse)
        {
            if (impulse != null)
                _queue.Enqueue(impulse!);
        }

        /// <summary>
        /// Publish an impulse by enqueuing it for main-thread delivery.
        /// Convenience alias over <see cref="Post{T}(T)"/> so game code can use "Publish" naturally.
        /// </summary>
        public void Publish<T>(T impulse)
        {
            if (impulse == null)
                return;

            Post(impulse);
        }

        /// <summary>
        /// Publish immediately on the current thread (use on main thread only).
        /// </summary>
        public void PublishImmediate<T>(T impulse)
        {
            if (impulse != null)
                Dispatch(impulse!);
        }

        /// <summary>
        /// Called once per frame by <see cref="ImpulseSystem"/> on the main thread
        /// to deliver any queued impulses in a deterministic place in the frame.
        /// </summary>
        internal void DispatchAll()
        {
            while (_queue.TryDequeue(out object? impulse))
                Dispatch(impulse);
        }

        /// <summary>
        /// Create a continuous impulse source that will generate impulses over time.
        /// The generator is given (elapsed, duration) in seconds and returns an impulse instance.
        /// </summary>
        public IContinuousImpulseSource CreateContinuousSource<T>(
            Func<float, float, T> generator,
            float duration,
            bool autoRemove = true)
        {
            if (generator == null)
                throw new ArgumentNullException(nameof(generator));

            if (duration <= 0f)
                duration = 0.0001f;

            ContinuousSource src = new ContinuousSource(this, elapsed => generator(elapsed, duration), duration, autoRemove);

            lock (_sourceLock)
                _continuousSources.Add(src);

            return src;
        }

        /// <summary>
        /// Stop and remove a previously created continuous impulse source.
        /// </summary>
        public void RemoveSource(IContinuousImpulseSource source)
        {
            if (source == null)
                return;

            lock (_sourceLock)
                _continuousSources.Remove((ContinuousSource)source);
        }

        /// <summary>
        /// Called once per frame by <see cref="ImpulseSystem"/> after <see cref="DispatchAll"/>,
        /// to advance continuous impulse sources and publish any impulses they generate.
        /// </summary>
        internal void UpdateContinuousSources(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            lock (_sourceLock)
            {
                if (_continuousSources.Count == 0)
                    return;

                for (int i = _continuousSources.Count - 1; i >= 0; i--)
                {
                    ContinuousSource src = _continuousSources[i];
                    bool done = src.Tick(deltaTime);

                    if (done)
                        _continuousSources.RemoveAt(i);
                }
            }
        }
        #endregion

        #region Lifecycle Methods
        #endregion

        #region Housekeeping Methods
        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_mapLock)
                _map.Clear();

            lock (_sourceLock)
                _continuousSources.Clear();

            while (_queue.TryDequeue(out _))
            {
            }

            _disposed = true;
        }
        #endregion

        #region Methods (private)
        private void Dispatch(object impulse)
        {
            if (impulse == null)
                return;

            List<ImpulseSubscription>? snapshot = null;

            lock (_mapLock)
            {
                if (!_map.TryGetValue(impulse.GetType(), out List<ImpulseSubscription>? list) || list.Count == 0)
                    return;

                snapshot = new List<ImpulseSubscription>(list);
            }

            List<ImpulseSubscription>? toRemove = null;

            for (int i = 0; i < snapshot.Count; i++)
            {
                ImpulseSubscription s = snapshot[i];

                if (s.Filter != null && !s.Filter(impulse))
                    continue;

                try
                {
                    s.Handler.DynamicInvoke(impulse);
                }
                catch
                {
                    // Swallow to keep demos robust.
                }

                if (s.Once)
                {
                    if (toRemove == null)
                        toRemove = new List<ImpulseSubscription>(2);

                    toRemove.Add(s);
                }
            }

            if (toRemove != null)
            {
                lock (_mapLock)
                {
                    if (_map.TryGetValue(impulse.GetType(), out List<ImpulseSubscription>? list))
                    {
                        for (int i = 0; i < toRemove.Count; i++)
                            list.Remove(toRemove[i]);
                    }
                }
            }
        }
        #endregion

        #region Types (public)
        /// <summary>
        /// Handle for a continuous impulse source managed by <see cref="ImpulseBus"/>.
        /// </summary>
        public interface IContinuousImpulseSource
        {
            /// <summary>
            /// Total duration of this source, in seconds.
            /// </summary>
            float Duration
            {
                get;
            }

            /// <summary>
            /// Elapsed time since the source was created, in seconds.
            /// </summary>
            float Elapsed
            {
                get;
            }

            /// <summary>
            /// Whether this source has completed its duration.
            /// </summary>
            bool IsComplete
            {
                get;
            }

            /// <summary>
            /// Stop this source and prevent further impulses.
            /// </summary>
            void Stop();
        }
        #endregion

        #region Types (private)
        private sealed class Unsub : IDisposable
        {
            private readonly ImpulseBus _owner;
            private readonly Type _type;
            private ImpulseSubscription _sub;
            private bool _done;

            public Unsub(ImpulseBus owner, Type type, ImpulseSubscription sub)
            {
                _owner = owner;
                _type = type;
                _sub = sub;
            }

            public void Dispose()
            {
                if (_done)
                    return;

                lock (_owner._mapLock)
                {
                    if (_owner._map.TryGetValue(_type, out List<ImpulseSubscription>? list))
                        list.Remove(_sub);
                }

                _done = true;
            }
        }

        private sealed class ImpulseSubscription
        {
            public readonly Delegate Handler;
            public readonly int Priority;
            public readonly Func<object, bool>? Filter;
            public readonly bool Once;

            public ImpulseSubscription(Delegate handler, int priority, Func<object, bool>? filter, bool once)
            {
                Handler = handler;
                Priority = priority;
                Filter = filter;
                Once = once;
            }
        }

        private sealed class ContinuousSource : IContinuousImpulseSource
        {
            private readonly ImpulseBus _owner;
            private readonly Func<float, object?> _generator;
            private readonly float _duration;
            private readonly bool _autoRemove;
            private float _elapsed;
            private bool _stopped;

            public ContinuousSource(ImpulseBus owner, Func<float, object?> generator, float duration, bool autoRemove)
            {
                _owner = owner;
                _generator = generator;
                _duration = duration;
                _autoRemove = autoRemove;
            }

            public float Duration
            {
                get { return _duration; }
            }

            public float Elapsed
            {
                get { return _elapsed; }
            }

            public bool IsComplete
            {
                get { return _elapsed >= _duration || _stopped; }
            }

            public void Stop()
            {
                _stopped = true;
            }

            public bool Tick(float deltaTime)
            {
                if (_stopped)
                    return true;

                _elapsed += deltaTime;

                object? impulse = _generator(_elapsed);
                if (impulse != null)
                    _owner.Dispatch(impulse);

                if (_elapsed >= _duration)
                    return _autoRemove || _stopped;

                return false;
            }
        }
        #endregion
    }

    /// <summary>
    /// Readable priority presets for ImpulseBus subscriptions (lower numbers run earlier).
    /// These are guidelines, not a closed set—use WithPriority(...) for custom values.
    /// </summary>
    public static class ImpulsePriority
    {
        /// <summary>
        /// Impulses that coordinate global effects ordering (e.g. cutscene camera, fade in/out).
        /// </summary>
        public const int Orchestrator = -100;

        /// <summary>
        /// Core effect systems that should react before gameplay/UI reads them.
        /// Examples: camera controllers, audio pre-mix impulses.
        /// </summary>
        public const int Systems = -20;

        /// <summary>
        /// Default gameplay-related impulses (e.g. recoil, hit reactions).
        /// </summary>
        public const int Gameplay = 0;

        /// <summary>
        /// UI and presentation impulses (e.g. HUD pulse, menu bounce).
        /// </summary>
        public const int UI = 50;

        /// <summary>
        /// Non-intrusive observers (e.g. telemetry overlays, debug visualisation).
        /// </summary>
        public const int Telemetry = 100;
    }

    /// <summary>
    /// Fluent helpers for ImpulseBus subscriptions:
    /// bus.On&lt;T&gt;().WithPriority(...).WithPriorityPreset(ImpulsePriority.Gameplay).When(...).WhenNotNull(...).Until(...).Once().Do(handler)
    /// </summary>
    public static class ImpulseBusFluent
    {
        #region API
        public static SubscriptionBuilder<T> On<T>(this ImpulseBus bus)
        {
            return new SubscriptionBuilder<T>(bus);
        }
        #endregion

        #region Types
        /// <summary>
        /// Value-type builder; creates no heap objects until Do/Handle is called.
        /// Mirrors the EventBus fluent API for familiarity.
        /// </summary>
        public readonly struct SubscriptionBuilder<T>
        {
            private readonly ImpulseBus _bus;
            private readonly int _priority;
            private readonly Predicate<T>? _filter;
            private readonly bool _once;
            private readonly Predicate<T>? _until;

            public SubscriptionBuilder(ImpulseBus bus)
            {
                _bus = bus ?? throw new ArgumentNullException(nameof(bus));
                _priority = 0;
                _filter = null;
                _once = false;
                _until = null;
            }

            private SubscriptionBuilder(ImpulseBus bus, int priority, Predicate<T>? filter, bool once, Predicate<T>? until)
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

            /// <summary>Filter impulses; only those for which predicate returns true are delivered.</summary>
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
            /// Evaluated after the handler runs on each delivered impulse.
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

                ImpulseBus bus = _bus;
                int prio = _priority;
                Predicate<T>? filter = _filter;
                bool once = _once;
                Predicate<T>? until = _until;

                IDisposable? token = null;

                Action<T> wrapped = e =>
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
