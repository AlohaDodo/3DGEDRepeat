namespace GDEngine.Core.Collections
{
    /// <summary>
    /// Generic object pool for reusable instances to reduce GC pressure and allocation overhead.
    /// Supports custom factory and reset policies. Thread-safe by default.
    /// </summary>
    /// <typeparam name="T">Type of object to pool. Must be a reference type.</typeparam>
    /// <see cref="Queue{T}"/>
    /// <see cref="IPoolable"/>
    public class ObjectPool<T> where T : class
    {
        #region Static Fields
        private const int _defaultCapacity = 16;
        private const int _maxCapacity = 1024;
        #endregion

        #region Fields
        private readonly Queue<T> _available;
        private readonly HashSet<T> _active;
        private readonly Func<T> _factory;
        private readonly Action<T>? _onGet;
        private readonly Action<T>? _onReturn;
        private readonly int _maxSize;
        private readonly object _lock = new object();
        private bool _disposed = false;
        #endregion

        #region Properties
        /// <summary>Number of instances currently available in the pool.</summary>
        public int AvailableCount
        {
            get
            {
                lock (_lock)
                    return _available.Count;
            }
        }

        /// <summary>Number of instances currently in use (checked out).</summary>
        public int ActiveCount
        {
            get
            {
                lock (_lock)
                    return _active.Count;
            }
        }

        /// <summary>Total number of instances managed by this pool (Available + Active).</summary>
        public int TotalCount
        {
            get
            {
                lock (_lock)
                    return _available.Count + _active.Count;
            }
        }

        /// <summary>Maximum capacity of this pool.</summary>
        public int MaxSize => _maxSize;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new <see cref="ObjectPool{T}"/> with a custom factory.
        /// </summary>
        /// <param name="factory">Function to create new instances when pool is empty.</param>
        /// <param name="onGet">Optional callback when an instance is retrieved (e.g., reset state).</param>
        /// <param name="onReturn">Optional callback when an instance is returned (e.g., cleanup).</param>
        /// <param name="initialSize">Number of instances to pre-allocate.</param>
        /// <param name="maxSize">Maximum pool capacity. Returns beyond this are discarded.</param>
        public ObjectPool(
            Func<T> factory,
            Action<T>? onGet = null,
            Action<T>? onReturn = null,
            int initialSize = 0,
            int maxSize = _maxCapacity)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (initialSize < 0)
                throw new ArgumentOutOfRangeException(nameof(initialSize), "Initial size must be non-negative.");

            if (maxSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxSize), "Max size must be positive.");

            _factory = factory;
            _onGet = onGet;
            _onReturn = onReturn;
            _maxSize = maxSize;

            int capacity = (int)MathF.Min(initialSize > 0 ? initialSize : _defaultCapacity, maxSize);
            _available = new Queue<T>(capacity);
            _active = new HashSet<T>();

            // Pre-allocate initial instances
            for (int i = 0; i < initialSize; i++)
            {
                var instance = _factory();
                if (instance != null)
                    _available.Enqueue(instance);
            }
        }
        #endregion

        #region Core Methods
        /// <summary>
        /// Gets an instance from the pool. Creates a new one if pool is empty.
        /// </summary>
        /// <returns>A ready-to-use instance of <typeparamref name="T"/>.</returns>
        public T Get()
        {
            lock (_lock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ObjectPool<T>));

                T instance;

                // Try to reuse from pool
                if (_available.Count > 0)
                {
                    instance = _available.Dequeue();
                }
                else
                {
                    // Create new instance
                    instance = _factory();
                    if (instance == null)
                        throw new InvalidOperationException("Factory returned null instance.");
                }

                // Track as active
                _active.Add(instance);

                // Reset/initialize via callback
                _onGet?.Invoke(instance);

                return instance;
            }
        }

        /// <summary>
        /// Returns an instance to the pool for reuse. Discarded if pool is at max capacity.
        /// </summary>
        /// <param name="instance">Instance to return. Ignored if null or not from this pool.</param>
        /// <returns>True if returned to pool, false if discarded or invalid.</returns>
        public bool Return(T instance)
        {
            if (instance == null)
                return false;

            lock (_lock)
            {
                if (_disposed)
                    return false;

                // Only return instances that were checked out from this pool
                if (!_active.Remove(instance))
                    return false;

                // Cleanup via callback
                _onReturn?.Invoke(instance);

                // Return to pool if not at capacity
                if (_available.Count < _maxSize)
                {
                    _available.Enqueue(instance);
                    return true;
                }
                else
                {
                    // Pool full; discard and dispose if possible
                    if (instance is IDisposable disposable)
                        disposable.Dispose();

                    return false;
                }
            }
        }

        /// <summary>
        /// Pre-allocates additional instances up to the target count.
        /// </summary>
        /// <param name="count">Target number of available instances.</param>
        public void Prewarm(int count)
        {
            if (count <= 0)
                return;

            lock (_lock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ObjectPool<T>));

                var toCreate = MathF.Min(count - _available.Count, _maxSize - TotalCount);

                for (int i = 0; i < toCreate; i++)
                {
                    var instance = _factory();
                    if (instance != null)
                        _available.Enqueue(instance);
                }
            }
        }

        /// <summary>
        /// Clears all available instances from the pool and disposes them if possible.
        /// Active instances are not affected.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                while (_available.Count > 0)
                {
                    var instance = _available.Dequeue();
                    if (instance is IDisposable disposable)
                        disposable.Dispose();
                }
            }
        }

        /// <summary>
        /// Shrinks the pool to the target size by disposing excess available instances.
        /// </summary>
        /// <param name="targetSize">Desired number of available instances.</param>
        public void Shrink(int targetSize)
        {
            if (targetSize < 0)
                targetSize = 0;

            lock (_lock)
            {
                if (_disposed)
                    return;

                while (_available.Count > targetSize)
                {
                    var instance = _available.Dequeue();
                    if (instance is IDisposable disposable)
                        disposable.Dispose();
                }
            }
        }
        #endregion

        #region Lifecycle Methods
        // None
        #endregion

        #region Housekeeping Methods
        /// <summary>
        /// Disposes the pool and all managed instances (both available and active).
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                lock (_lock)
                {
                    // Dispose available instances
                    while (_available.Count > 0)
                    {
                        var instance = _available.Dequeue();
                        if (instance is IDisposable disposable)
                            disposable.Dispose();
                    }

                    // Dispose active instances
                    foreach (var instance in _active)
                    {
                        if (instance is IDisposable disposable)
                            disposable.Dispose();
                    }

                    _active.Clear();
                }
            }

            _disposed = true;
        }

        ~ObjectPool()
        {
            Dispose(false);
        }

        public override string ToString()
        {
            lock (_lock)
            {
                return $"ObjectPool<{typeof(T).Name}>(Available={_available.Count}, Active={_active.Count}, Max={_maxSize})";
            }
        }
        #endregion
    }

    /// <summary>
    /// Optional interface for pooled objects that need reset/cleanup callbacks.
    /// Implement this to get automatic OnGet/OnReturn behavior.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>Called when retrieved from the pool. Reset state here.</summary>
        void OnGet();

        /// <summary>Called when returned to the pool. Cleanup here.</summary>
        void OnReturn();
    }

    /// <summary>
    /// Extension methods for convenient pool usage with <see cref="IPoolable"/> objects.
    /// </summary>
    public static class ObjectPoolExtensions
    {
        /// <summary>
        /// Creates an <see cref="ObjectPool{T}"/> with automatic <see cref="IPoolable"/> callbacks.
        /// </summary>
        public static ObjectPool<T> CreatePoolablePool<T>(
            Func<T> factory,
            int initialSize = 0,
            int maxSize = 1024)
            where T : class, IPoolable
        {
            return new ObjectPool<T>(
                factory,
                onGet: obj => obj.OnGet(),
                onReturn: obj => obj.OnReturn(),
                initialSize,
                maxSize
            );
        }
    }
}