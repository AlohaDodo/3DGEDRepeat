using GDEngine.Core.Services;
using Microsoft.Xna.Framework.Content;

namespace GDEngine.Core.Collections
{
    /// <summary>
    /// Generic content dictionary with Add/Remove/Get/Clear and bulk registration helpers.
    /// Uses cached EngineContext to load assets on demand.
    /// </summary>
    /// <see cref="EngineContext"/>
    public sealed class ContentDictionary<T> : IDisposable where T : class
    {
        #region Static Fields
        #endregion

        #region Fields
        private readonly Dictionary<string, T> _items;
        private readonly Dictionary<string, string> _paths;
        private readonly ContentManager _content;
        private readonly string _name;
        private bool _disposed = false;
        #endregion

        #region Properties
        /// <summary>Optional label for debugging (e.g., "Texture2D").</summary>
        public string Name => _name;

        /// <summary>Number of cached items.</summary>
        public int Count => _items.Count;

        /// <summary>Returns all registered keys (case-insensitive).</summary>
        public IEnumerable<string> Keys => _items.Keys;
        #endregion

        #region Constructors
        /// <summary>
        /// Uses <see cref="EngineContext.Instance"/> and default name of typeof(T).Name.
        /// </summary>
        public ContentDictionary()
            : this(typeof(T).Name)
        {
        }

        /// <summary>
        /// Uses <see cref="EngineContext.Instance"/> with a custom name.
        /// </summary>
        public ContentDictionary(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Dictionary name must be non-empty.", nameof(name));

            if (EngineContext.Instance == null)
                throw new InvalidOperationException("EngineContext.Content is null. Ensure Content is initialized.");

            _content = EngineContext.Instance.Content;
            _items = new Dictionary<string, T>(128, StringComparer.OrdinalIgnoreCase);
            _paths = new Dictionary<string, string>(128, StringComparer.OrdinalIgnoreCase);
            _name = name;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Adds an item by key. If the key is new, loads from Content using path and stores it.
        /// </summary>
        /// <returns>True if loaded; false if the key already existed (no change).</returns>
        public bool Add(string key, string path)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key must be non-empty.", nameof(key));
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path must be non-empty.", nameof(path));

            // Only add if we haven't seen this key before (no overwrite here)
            if (!_paths.ContainsKey(key))
            {
                // Load the asset once via ContentManager using the given pipeline path
                var loaded = _content.Load<T>(path);

                // Cache both the loaded asset and the path we used (helps with future updates)
                _items[key] = loaded;
                _paths[key] = path;
                return true;
            }

            // Key already present: we do nothing (use AddOrUpdate if you want overwrite behaviour)
            return false;
        }

        /// <summary>
        /// Adds or updates an item. If the key exists and <paramref name="overwrite"/> is true and the path is different, reloads and replaces.
        /// </summary>
        /// <returns>True if a new item was added or an existing one was reloaded; false if skipped.</returns>
        public bool AddOrUpdate(string key, string path, bool overwrite)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key must be non-empty.", nameof(key));
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path must be non-empty.", nameof(path));

            // Case 1: key not present → load and store.
            if (!_paths.TryGetValue(key, out var existingPath))
            {
                var loaded = _content.Load<T>(path);
                _items[key] = loaded;
                _paths[key] = path;
                return true;
            }

            // Case 2: key present but overwrite disabled → do nothing.
            if (!overwrite) return false;

            // Case 3: key present and overwrite enabled.
            // Only reload if the content path changed (avoids unnecessary reloads).
            if (!string.Equals(existingPath, path, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var reloaded = _content.Load<T>(path);
                    _items[key] = reloaded;
                    _paths[key] = path;
                    return true;
                }
                catch(Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }

            // Path hasn't changed → no work needed.
            return false;
        }

        /// <summary>
        /// Tries to get a previously added item by key. Returns true if found.
        /// </summary>
        public bool TryGet(string key, out T? value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(key))
                return false;

            // Fast path: dictionary lookup; avoids exceptions compared to Get()
            if (_items.TryGetValue(key, out var v))
            {
                value = v;
                return true;
            }

            // Not found → caller can decide what to do (e.g., fallback or log)
            return false;
        }

        /// <summary>
        /// Gets a previously added item by key or null if not present.
        /// </summary>
        public T? Get(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Key must be non-empty.", nameof(key));

            // Return cached item if we have it; otherwise return null (no auto-load on demand)
            T? value;
            if (_items.TryGetValue(key, out value))
                return value;
            else
                return null;
        }

        /// <summary>
        /// Returns true if the key is present.
        /// </summary>
        public bool Contains(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            // Presence check only; no side effects
            return _items.ContainsKey(key);
        }

        /// <summary>
        /// Removes a key if present. Returns true if removed.
        /// </summary>
        public bool Remove(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            // Forget the path mapping first (kept in sync with items)
            _paths.Remove(key);

            // Remove the actual cached asset reference (ContentManager still owns the asset lifetime)
            return _items.Remove(key);
        }

        /// <summary>
        /// Clears all cached references. Does not dispose assets (ContentManager owns them).
        /// </summary>
        public void Clear()
        {
            // Remove all cached lookups; assets remain alive until ContentManager is disposed/unloaded
            _items.Clear();
            _paths.Clear();
        }

        /// <summary>
        /// Bulk load/register entries from arbitrary DTOs using selectors (e.g., manifest AssetEntry).
        /// </summary>
        /// <typeparam name="E">Entry type (e.g., AssetEntry)</typeparam>
        /// <param name="entries">Entries to register.</param>
        /// <param name="keySelector">Selects the dictionary key from the entry.</param>
        /// <param name="pathSelector">Selects the Content pipeline path from the entry.</param>
        /// <param name="overwrite">If true, existing keys will be reloaded when the path differs.</param>
        /// <returns>Number of items added or reloaded.</returns>
        public int LoadFromManifest<E>(
            IEnumerable<E> entries,
            Func<E, string> keySelector,
            Func<E, string> pathSelector,
            bool overwrite = false)
        {
            if (entries == null)
                return 0;

            int changed = 0;

            // Loop through each manifest entry and map it to (key, path)
            foreach (var e in entries)
            {
                var key = keySelector(e);
                var path = pathSelector(e);

                // Skip incomplete rows (helps students spot missing fields without crashing)
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(path))
                    continue;

                // Add new or update existing (optionally overwriting if path changed)
                if (AddOrUpdate(key, path, overwrite))
                    changed++;
            }

            // Return how many things we actually touched (added or reloaded)
            return changed;
        }

        /// <summary>
        /// Bulk load/register from (key,path) pairs.
        /// </summary>
        /// <returns>Number of items added or reloaded.</returns>
        public int LoadFromPairs(IEnumerable<KeyValuePair<string, string>> pairs, bool overwrite = false)
        {
            if (pairs == null)
                return 0;

            int changed = 0;

            // Minimal wrapper for scenarios where you already have (key, path) pairs
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
                // Dispose items if they implement IDisposable.
                // Note: Texture2D, Model, etc. loaded by ContentManager are typically
                // owned by the ContentManager, which will dispose them when it's disposed.
                // However, if we have custom content types, this will dispose them.
                foreach (var kvp in _items)
                {
                    if (kvp.Value is IDisposable disposable)
                        disposable.Dispose();
                }

                _items.Clear();
                _paths.Clear();
            }

            _disposed = true;
        }

        ~ContentDictionary()
        {
            Dispose(false);
        }

        public override string ToString()
        {
            return $"ContentDictionary<{typeof(T).Name}>(Name={_name}, Count={_items.Count})";
        }
        #endregion
    }
}
