#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;

namespace GDEngine.Core.Collections
{
    /// <summary>
    /// Array-backed collection with dictionary-based indexing for O(1) add, remove (swap-remove), and lookup.
    /// Iteration is cache-friendly across the live segment [0..Count-1]. Not thread-safe.
    /// </summary>
    /// <see cref="List{T}"/>
    /// <see cref="Dictionary{TKey, TValue}"/>
    /// <see cref="IEnumerable{T}"/>
    public sealed class IndexedCollection<T> : IEnumerable<T>, IDisposable where T : class
    {
        #region Static Fields
        private const int _minCapacity = 16;
        #endregion

        #region Fields
        private readonly Dictionary<T, int> _indices;
        private T[] _items;
        private int _count;
        private bool _disposed;
        #endregion

        #region Properties
        /// <summary>Current number of items.</summary>
        public int Count => _count;

        /// <summary>Current array capacity.</summary>
        public int Capacity => _items.Length;

        /// <summary>
        /// Direct access to the backing array for zero-allocation loops (use only indices [0..Count-1]).
        /// </summary>
        public T[] Items => _items;

        /// <summary>Indexed access with bounds checking.</summary>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= _count)
                    throw new IndexOutOfRangeException($"Index {index} is outside [0..{_count - 1}]");

                return _items[index];
            }
        }
        #endregion

        #region Constructors
        /// <summary>Create with an initial capacity (minimum 16).</summary>
        public IndexedCollection(int initialCapacity = _minCapacity)
        {
            if (initialCapacity < _minCapacity)
                initialCapacity = _minCapacity;

            _items = new T[initialCapacity];
            _indices = new Dictionary<T, int>(initialCapacity);
            _count = 0;
            _disposed = false;
        }
        #endregion

        #region Methods
        /// <summary>Add to end (O(1) amortized). Ignores duplicates. Throws if item is null.</summary>
        public void Add(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (_indices.ContainsKey(item))
                return;

            if (_count == _items.Length)
                Resize((int)MathF.Max(_items.Length * 2, _minCapacity));

            _items[_count] = item;
            _indices[item] = _count;
            _count++;
        }

        /// <summary>Remove via swap-remove (O(1)). Returns false if null or not present.</summary>
        public bool Remove(T item)
        {
            if (item == null)
                return false;

            if (!_indices.TryGetValue(item, out int index))
                return false;

            int last = _count - 1;

            if (index != last)
            {
                var moved = _items[last];
                _items[index] = moved;
                _indices[moved] = index;
            }

            _items[last] = default!; // clear for GC on reference types
            _indices.Remove(item);
            _count--;

            return true;
        }

        /// <summary>O(1) existence check.</summary>
        public bool Contains(T item)
        {
            if (item == null)
                return false;

            return _indices.ContainsKey(item);
        }

        /// <summary>O(1) index lookup; returns -1 if not found.</summary>
        public int IndexOf(T item)
        {
            if (item == null)
                return -1;

            return _indices.TryGetValue(item, out int idx) ? idx : -1;
        }

        /// <summary>Remove all items and clear array slots to enable GC.</summary>
        public void Clear()
        {
            for (int i = 0; i < _count; i++)
                _items[i] = default!;

            _indices.Clear();
            _count = 0;
        }

        /// <summary>Ensure backing capacity is at least the requested value (powers-of-two growth).</summary>
        public void EnsureCapacity(int capacity)
        {
            if (capacity <= _items.Length)
                return;

            int newCap = _items.Length < _minCapacity ? _minCapacity : _items.Length;
            while (newCap < capacity)
                newCap *= 2;

            Resize(newCap);
        }

        /// <summary>Copy live items [0..Count-1] into the destination array starting at arrayIndex.</summary>
        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (arrayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Must be non-negative.");

            if (arrayIndex + _count > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Destination array too small.");

            Array.Copy(_items, 0, array, arrayIndex, _count);
        }

        /// <summary>Debug-only integrity validation. Throws if an invariant is broken.</summary>
        public void ValidateIntegrity()
        {
            if (_count != _indices.Count)
                throw new InvalidOperationException($"Count mismatch: Array={_count}, Map={_indices.Count}");

            for (int i = 0; i < _count; i++)
            {
                var it = _items[i];
                if (it == null)
                    throw new InvalidOperationException($"Null item at index {i}");

                if (!_indices.TryGetValue(it, out int mapped))
                    throw new InvalidOperationException($"Item at index {i} missing from dictionary");

                if (mapped != i)
                    throw new InvalidOperationException($"Index mismatch for item at {i}: map={mapped}");
            }
        }

        /// <summary>Enumerator over the live region [0..Count-1].</summary>
        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < _count; i++)
                yield return _items[i];
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        // Resize backing array; indices remain valid since we keep order.
        private void Resize(int newCapacity)
        {
            var dst = new T[newCapacity];
            if (_count > 0)
                Array.Copy(_items, 0, dst, 0, _count);
            _items = dst;
        }
        #endregion

        #region Lifecycle Methods
        // None
        #endregion

        #region Housekeeping Methods
        /// <summary>Disposes the collection, disposing items that implement IDisposable and clearing references.</summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            // Dispose any items that implement IDisposable
            for (int i = 0; i < _count; i++)
            {
                if (_items[i] is IDisposable d)
                    d.Dispose();

                _items[i] = default!;
            }

            _indices.Clear();
            _count = 0;
            _disposed = true;

            GC.SuppressFinalize(this);
        }

        public override string ToString()
        {
            return $"IndexedCollection<{typeof(T).Name}>(Count={_count}, Capacity={_items.Length})";
        }
        #endregion
    }
}
