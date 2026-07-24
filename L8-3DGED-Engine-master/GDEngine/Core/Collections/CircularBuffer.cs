#nullable enable
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace GDEngine.Core.Collections
{
    /// <summary>
    /// Fixed-size circular buffer (ring buffer) with automatic wraparound.
    /// Ideal for particle systems, audio buffers, frame history, and fixed-capacity queues.
    /// When full, oldest items are overwritten. Thread-safe by default.
    /// </summary>
    /// <typeparam name="T">Type of items to store.</typeparam>
    /// <see cref="Queue{T}"/>
    /// <see cref="List{T}"/>
    public sealed class CircularBuffer<T> : IEnumerable<T>, IDisposable
    {
        #region Static Fields
        private const int _minCapacity = 4;
        private const int _defaultCapacity = 16;
        #endregion

        #region Fields
        private readonly T[] _buffer;
        private int _head;       // Index of next write position
        private int _tail;       // Index of oldest item
        private int _count;      // Number of items currently in buffer
        private readonly int _capacity;
        private readonly object _lock = new object();
        private bool _disposed = false;
        #endregion

        #region Properties
        /// <summary>Fixed capacity of this buffer.</summary>
        public int Capacity => _capacity;

        /// <summary>Current number of items in the buffer.</summary>
        public int Count
        {
            get
            {
                lock (_lock)
                    return _count;
            }
        }

        /// <summary>True if buffer contains no items.</summary>
        public bool IsEmpty
        {
            get
            {
                lock (_lock)
                    return _count == 0;
            }
        }

        /// <summary>True if buffer is at full capacity.</summary>
        public bool IsFull
        {
            get
            {
                lock (_lock)
                    return _count == _capacity;
            }
        }

        /// <summary>
        /// Indexed access with wraparound. Index 0 is the oldest item, Count-1 is newest.
        /// </summary>
        /// <param name="index">Logical index [0..Count-1].</param>
        public T this[int index]
        {
            get
            {
                lock (_lock)
                {
                    if (index < 0 || index >= _count)
                        throw new IndexOutOfRangeException($"Index {index} is outside [0..{_count - 1}]");

                    var physicalIndex = (_tail + index) % _capacity;
                    return _buffer[physicalIndex];
                }
            }
            set
            {
                lock (_lock)
                {
                    if (index < 0 || index >= _count)
                        throw new IndexOutOfRangeException($"Index {index} is outside [0..{_count - 1}]");

                    var physicalIndex = (_tail + index) % _capacity;
                    _buffer[physicalIndex] = value;
                }
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new <see cref="CircularBuffer{T}"/> with the specified capacity.
        /// </summary>
        /// <param name="capacity">Fixed size of the buffer (minimum 4).</param>
        public CircularBuffer(int capacity = _defaultCapacity)
        {
            if (capacity < _minCapacity)
                capacity = _minCapacity;

            _capacity = capacity;
            _buffer = new T[_capacity];
            _head = 0;
            _tail = 0;
            _count = 0;
        }
        #endregion

        #region Core Methods
        /// <summary>
        /// Adds an item to the buffer. If full, overwrites the oldest item.
        /// </summary>
        /// <param name="item">Item to add.</param>
        /// <returns>The item that was overwritten, or default(T) if buffer wasn't full.</returns>
        [return: MaybeNull]
        public T Push(T item)
        {
            lock (_lock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(CircularBuffer<T>));

                T overwritten = default!;
                bool wasOverwritten = false;

                if (_count == _capacity)
                {
                    overwritten = _buffer[_tail];
                    wasOverwritten = true;
                    _tail = (_tail + 1) % _capacity;
                }
                else
                {
                    _count++;
                }

                _buffer[_head] = item;
                _head = (_head + 1) % _capacity;

                return wasOverwritten ? overwritten : default!;
            }
        }

        /// <summary>
        /// Removes and returns the oldest item from the buffer.
        /// </summary>
        /// <returns>The oldest item.</returns>
        /// <exception cref="InvalidOperationException">Thrown if buffer is empty.</exception>
        public T Pop()
        {
            lock (_lock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(CircularBuffer<T>));

                if (_count == 0)
                    throw new InvalidOperationException("Buffer is empty.");

                var item = _buffer[_tail];
                _buffer[_tail] = default!; // clear for GC on reference types
                _tail = (_tail + 1) % _capacity;
                _count--;

                return item;
            }
        }

        /// <summary>
        /// Attempts to remove and return the oldest item from the buffer.
        /// </summary>
        /// <param name="item">The oldest item, or default(T) if buffer is empty.</param>
        /// <returns>True if an item was removed, false if buffer was empty.</returns>
        public bool TryPop([MaybeNullWhen(false)] out T item)
        {
            lock (_lock)
            {
                if (_disposed || _count == 0)
                {
                    item = default!;
                    return false;
                }

                item = _buffer[_tail];
                _buffer[_tail] = default!;
                _tail = (_tail + 1) % _capacity;
                _count--;

                return true;
            }
        }

        /// <summary>
        /// Returns the oldest item without removing it.
        /// </summary>
        /// <returns>The oldest item.</returns>
        /// <exception cref="InvalidOperationException">Thrown if buffer is empty.</exception>
        public T Peek()
        {
            lock (_lock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(CircularBuffer<T>));

                if (_count == 0)
                    throw new InvalidOperationException("Buffer is empty.");

                return _buffer[_tail];
            }
        }

        /// <summary>
        /// Attempts to return the oldest item without removing it.
        /// </summary>
        /// <param name="item">The oldest item, or default(T) if buffer is empty.</param>
        /// <returns>True if an item exists, false if buffer was empty.</returns>
        public bool TryPeek([MaybeNullWhen(false)] out T item)
        {
            lock (_lock)
            {
                if (_disposed || _count == 0)
                {
                    item = default!;
                    return false;
                }

                item = _buffer[_tail];
                return true;
            }
        }

        /// <summary>
        /// Returns the newest item without removing it.
        /// </summary>
        /// <returns>The newest item.</returns>
        /// <exception cref="InvalidOperationException">Thrown if buffer is empty.</exception>
        public T PeekNewest()
        {
            lock (_lock)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(CircularBuffer<T>));

                if (_count == 0)
                    throw new InvalidOperationException("Buffer is empty.");

                var newestIndex = _head == 0 ? _capacity - 1 : _head - 1;
                return _buffer[newestIndex];
            }
        }

        /// <summary>
        /// Attempts to return the newest item without removing it.
        /// </summary>
        /// <param name="item">The newest item, or default(T) if buffer is empty.</param>
        /// <returns>True if an item exists, false if buffer was empty.</returns>
        public bool TryPeekNewest([MaybeNullWhen(false)] out T item)
        {
            lock (_lock)
            {
                if (_disposed || _count == 0)
                {
                    item = default!;
                    return false;
                }

                var newestIndex = _head == 0 ? _capacity - 1 : _head - 1;
                item = _buffer[newestIndex];
                return true;
            }
        }

        /// <summary>
        /// Removes all items from the buffer and clears references for GC.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                if (_disposed)
                    return;

                for (int i = 0; i < _capacity; i++)
                    _buffer[i] = default!;

                _head = 0;
                _tail = 0;
                _count = 0;
            }
        }

        /// <summary>
        /// Copies all items to an array in chronological order (oldest to newest).
        /// </summary>
        public T[] ToArray()
        {
            lock (_lock)
            {
                if (_count == 0)
                    return Array.Empty<T>();

                var result = new T[_count];

                if (_tail < _head)
                {
                    Array.Copy(_buffer, _tail, result, 0, _count);
                }
                else
                {
                    var tailLength = _capacity - _tail;
                    Array.Copy(_buffer, _tail, result, 0, tailLength);
                    Array.Copy(_buffer, 0, result, tailLength, _head);
                }

                return result;
            }
        }

        /// <summary>
        /// Copies all items to the destination array starting at arrayIndex.
        /// </summary>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            lock (_lock)
            {
                if (arrayIndex < 0 || arrayIndex + _count > array.Length)
                    throw new ArgumentOutOfRangeException(nameof(arrayIndex));

                if (_count == 0)
                    return;

                if (_tail < _head)
                {
                    Array.Copy(_buffer, _tail, array, arrayIndex, _count);
                }
                else
                {
                    var tailLength = _capacity - _tail;
                    Array.Copy(_buffer, _tail, array, arrayIndex, tailLength);
                    Array.Copy(_buffer, 0, array, arrayIndex + tailLength, _head);
                }
            }
        }

        /// <summary>
        /// Checks if the buffer contains the specified item.
        /// </summary>
        public bool Contains(T item)
        {
            lock (_lock)
            {
                if (_count == 0)
                    return false;

                var comparer = EqualityComparer<T>.Default;

                for (int i = 0; i < _count; i++)
                {
                    var physicalIndex = (_tail + i) % _capacity;
                    if (comparer.Equals(_buffer[physicalIndex], item))
                        return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Returns items matching the predicate.
        /// </summary>
        public List<T> FindAll(Predicate<T> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));

            lock (_lock)
            {
                var results = new List<T>();

                for (int i = 0; i < _count; i++)
                {
                    var physicalIndex = (_tail + i) % _capacity;
                    var item = _buffer[physicalIndex];

                    if (predicate(item))
                        results.Add(item);
                }

                return results;
            }
        }

        /// <summary>
        /// Executes an action on each item in chronological order.
        /// </summary>
        public void ForEach(Action<T> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            lock (_lock)
            {
                for (int i = 0; i < _count; i++)
                {
                    var physicalIndex = (_tail + i) % _capacity;
                    action(_buffer[physicalIndex]);
                }
            }
        }

        /// <summary>
        /// Enumerator that yields items in chronological order (oldest to newest).
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            var snapshot = ToArray();
            for (int i = 0; i < snapshot.Length; i++)
                yield return snapshot[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public override string ToString()
        {
            lock (_lock)
            {
                return $"CircularBuffer<{typeof(T).Name}>(Count={_count}, Capacity={_capacity}, Full={IsFull})";
            }
        }

        #endregion

        #region Lifecycle Methods
        // None
        #endregion

        #region Housekeeping Methods
        /// <summary>
        /// Disposes the buffer and clears all references.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                lock (_lock)
                {
                    // Dispose items if they implement IDisposable
                    for (int i = 0; i < _count; i++)
                    {
                        var physicalIndex = (_tail + i) % _capacity;
                        if (_buffer[physicalIndex] is IDisposable d)
                            d.Dispose();
                    }

                    Clear();
                }
            }

            _disposed = true;
        }

        ~CircularBuffer()
        {
            Dispose(false);
        }
        #endregion
     
    }
}
