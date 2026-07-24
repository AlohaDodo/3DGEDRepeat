namespace GDEngine.Core.Collections
{
    /// <summary>
    /// A lightweight name->instance registry for runtime objects that are NOT loaded via the Content pipeline.
    /// Use this for things like animation curves, data sets, runtime-generated assets, etc.
    /// </summary>
    /// <see cref="Dictionary{TKey, TValue}"/>
    public sealed class NamedDictionary<T>
    {
        #region Fields
        private readonly Dictionary<string, T> _items;
        private readonly string _name;
        #endregion

        #region Properties
        /// <summary>Optional label for debugging (e.g., "Curves3D").</summary>
        public string Name => _name;

        /// <summary>Total number of registered items.</summary>
        public int Count => _items.Count;

        /// <summary>All registered keys (case-insensitive).</summary>
        public IEnumerable<string> Keys => _items.Keys;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new <see cref="NamedDictionary{T}"/> with a default name of typeof(T).Name.
        /// </summary>
        public NamedDictionary()
            : this(typeof(T).Name)
        {
        }

        /// <summary>
        /// Creates a new <see cref="NamedDictionary{T}"/> with a custom debug name.
        /// </summary>
        public NamedDictionary(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Dictionary name must be non-empty.", nameof(name));

            // We keep keys case-insensitive to align with content dictionaries and reduce student surprises.
            _items = new Dictionary<string, T>(128, StringComparer.OrdinalIgnoreCase);
            _name = name;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Adds a new item under the given key. Returns false if the key already exists.
        /// </summary>
        public bool Add(string key, T item)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key must be non-empty.", nameof(key));

            // We do not overwrite here. This method is for “first-time registration”.
            if (_items.ContainsKey(key))
                return false;

            _items[key] = item;     // Store the instance for quick lookup by name
            return true;
        }

        /// <summary>
        /// Adds or updates an item under the given key. If the key exists and overwrite is false, no change occurs.
        /// Returns true if an add or update happened.
        /// </summary>
        public bool AddOrUpdate(string key, T item, bool overwrite = true)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key must be non-empty.", nameof(key));

            // If key not present, just add.
            if (!_items.ContainsKey(key))
            {
                _items[key] = item;
                return true;
            }

            // Key present but overwrite disabled → skip.
            if (!overwrite)
                return false;

            // Overwrite enabled → replace existing value.
            _items[key] = item;
            return true;
        }

        /// <summary>
        /// Tries to get an item by key. Returns true if found.
        /// </summary>
        public bool TryGet(string key, out T? value)
        {
            value = default;
            if (string.IsNullOrWhiteSpace(key))
                return false;

            // Standard dictionary lookup; avoids exceptions and allows “if found then use” patterns.
            if (_items.TryGetValue(key, out var v))
            {
                value = v;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets an item by key or returns default(T) if not present.
        /// </summary>
        public T? Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key must be non-empty.", nameof(key));

            // We do not auto-create or load here; this registry only stores what you explicitly add.
            T? value;
            if (_items.TryGetValue(key, out value))
                return value;
            else
                return default;
        }

        /// <summary>
        /// Returns true if the key has been registered.
        /// </summary>
        public bool Contains(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            // Presence check only; no side effects.
            return _items.ContainsKey(key);
        }

        /// <summary>
        /// Removes a key if present. Returns true if removed.
        /// </summary>
        public bool Remove(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            // Remove the instance reference; caller owns the instance lifetime.
            return _items.Remove(key);
        }

        /// <summary>
        /// Clears all registered items. Instances are NOT disposed; caller owns their lifetime.
        /// </summary>
        public void Clear()
        {
            // Reset to an empty state in O(n). Useful for hot-reload or scene resets.
            _items.Clear();
        }

        /// <summary>
        /// Bulk register entries from an arbitrary DTO source using selectors.
        /// This mirrors the ergonomics of ContentDictionary.LoadFromManifest but without ContentManager.
        /// </summary>
        /// <typeparam name="TE">Entry type (e.g., a JSON DTO)</typeparam>
        /// <param name="entries">Entries to register.</param>
        /// <param name="keySelector">Maps entry → key string.</param>
        /// <param name="itemSelector">Maps entry → instance of T.</param>
        /// <param name="overwrite">If true, existing keys are replaced.</param>
        /// <returns>Number of items added or updated.</returns>
        public int LoadFromEntries<TE>(
            IEnumerable<TE> entries,
            Func<TE, string> keySelector,
            Func<TE, T> itemSelector,
            bool overwrite = true)
        {
            if (entries == null)
                return 0;

            int changed = 0;

            // Convert each entry into (key, item) and add/update in the registry.
            foreach (var e in entries)
            {
                var key = keySelector(e);
                if (string.IsNullOrWhiteSpace(key))
                    continue;               // Skip incomplete rows quietly; keeps authoring forgiving.

                var item = itemSelector(e);
                if (AddOrUpdate(key, item, overwrite))
                    changed++;              // Track only actual modifications (added or updated)
            }

            return changed;
        }

        /// <summary>
        /// Bulk register from (key, item) pairs.
        /// </summary>
        /// <returns>Number of items added or updated.</returns>
        public int LoadFromPairs(IEnumerable<KeyValuePair<string, T>> pairs, bool overwrite = true)
        {
            if (pairs == null)
                return 0;

            int changed = 0;

            // Minimal utility for scenarios where the caller already mapped to pairs.
            foreach (var kv in pairs)
                if (AddOrUpdate(kv.Key, kv.Value, overwrite))
                    changed++;

            return changed;
        }
        #endregion

        #region Lifecycle Methods
        // None
        #endregion

        #region Housekeeping Methods
        public override string ToString()
        {
            return $"NamedDictionary<{typeof(T).Name}>(Name={_name}, Count={_items.Count})";
        }
        #endregion
    }
}
