using GDEngine.Core.Components;
using GDEngine.Core.Enums;
using GDEngine.Core.Rendering;
using GDEngine.Core.Rendering.Base;
using GDEngine.Core.Services;
using GDEngine.Core.Systems;

namespace GDEngine.Core.Entities
{
    /// <summary>
    /// Logical collection of scene GameObject instances and lifecycle-ordered systems.
    /// Coordinates component lifecycle (Awake/Start/Update/LateUpdate) and dispatches system lifecycles.
    /// </summary>
    /// <see cref="GameObject"/>
    /// <see cref="Component"/>
    /// <see cref="SystemBase"/>
    /// <see cref="FrameLifecycle"/>
    /// <see cref="EngineContext"/>
    public sealed class Scene : IDisposable
    {
        #region Fields

        // Owned objects
        private readonly List<GameObject> _gameObjects = new();

        // Lifecycle tracking used to wake up new components
        private readonly HashSet<Component> _started = new();

        // Flat snapshot for inspection/UI
        private readonly List<SystemBase> _systemsAll = new();

        // Systems bucketed by FrameLifecycle index (we know there are exactly 5 lifecycles)
        private readonly List<SystemBase>[] _systemsByLifecycle;

        // Scene-owned registry of renderers used by Camera/Render systems
        private readonly List<MeshRenderer> _renderers = new List<MeshRenderer>(512);

        private readonly EngineContext _context;

        private bool _disposed = false;

        private Camera? _activeCamera;

        #endregion

        #region Events

        public event Action<GameObject>? GameObjectAdded;
        public event Action<GameObject>? GameObjectRemoved;

        #endregion

        #region Properties

        public string Name { get; set; }

        public EngineContext Context => _context;

        /// <summary>
        /// The currently active camera for this scene.
        /// Backed by a local field and synchronised with <see cref="CameraSystem"/> when present.
        /// </summary>
        public Camera? ActiveCamera
        {
            get => GetActiveCamera();
            set => SetActiveCamera(value);
        }

        public IReadOnlyList<GameObject> GameObjects => _gameObjects;
        public IReadOnlyList<SystemBase> Systems => _systemsAll;

        /// <summary>
        /// Read-only view of the scene's registered mesh renderers.
        /// Renderers should be added/removed via <see cref="RegisterRenderer"/> /
        /// <see cref="UnregisterRenderer"/> from components like <see cref="MeshRenderer"/>.
        /// </summary>
        public IReadOnlyList<MeshRenderer> Renderers => _renderers;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new <see cref="Scene"/>.
        /// </summary>
        /// <param name="context">Engine services container used by the scene.</param>
        /// <param name="name">Debug/display name.</param>
        public Scene(EngineContext context, string name = "Untitled Scene")
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            Name = name;

            _systemsByLifecycle = new List<SystemBase>[5];
            for (int i = 0; i < _systemsByLifecycle.Length; i++)
                _systemsByLifecycle[i] = new List<SystemBase>(4);
        }

        #endregion

        #region Core Methods

        /// <summary>
        /// Adds a system to the scene; routes to the lifecycle bucket and sorts by Order within that bucket.
        /// </summary>
        public void Add(SystemBase system)
        {
            if (system == null)
                throw new ArgumentNullException(nameof(system));

            if (system.Scene != this && system.Scene != null)
                throw new InvalidOperationException("System already attached to a different Scene.");

            system.OnAddedToScene(this);
            _systemsAll.Add(system);

            var systemBucket = _systemsByLifecycle[(int)system.Lifecycle];
            systemBucket.Add(system);

            // Stable ascending order by Order
            systemBucket.Sort((a, b) =>
            {
                if (a.Order == b.Order)
                    return 0;
                return a.Order < b.Order ? -1 : 1;
            });
        }

        /// <summary>
        /// Adds a strongly-typed system and returns it for convenience.
        /// </summary>
        public T AddSystem<T>(T system) where T : SystemBase
        {
            Add(system);
            return system;
        }

        /// <summary>
        /// Adds an existing <see cref="GameObject"/> to the scene and runs Awake() on its components.
        /// Renderers will be registered when their components call <see cref="RegisterRenderer"/>.
        /// </summary>
        public GameObject Add(GameObject gameObject)
        {
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));

            if (_gameObjects.Contains(gameObject))
                return gameObject;

            gameObject.Scene = this;
            _gameObjects.Add(gameObject);

            // Run Awake on all pre-existing components
            var comps = gameObject.Components;
            for (int i = 0; i < comps.Count; i++)
                comps[i].InternalAwake();

            // Promote first enabled camera if none set yet
            var cam = gameObject.GetComponent<Camera>();
            if (ActiveCamera == null && cam != null && cam.Enabled)
                ActiveCamera = cam;

            GameObjectAdded?.Invoke(gameObject);

            return gameObject;
        }

        /// <summary>
        /// Removes a specific <see cref="GameObject"/> from the scene, unregistering its renderers
        /// and destroying it.
        /// </summary>
        public bool Remove(GameObject gameObject)
        {
            if (gameObject == null)
                return false;

            if (!_gameObjects.Remove(gameObject))
                return false;

            // Unregister renderers owned by this GameObject
            var renderers = gameObject.GetComponents<MeshRenderer>();
            for (int i = 0; i < renderers.Count; i++)
                UnregisterRenderer(renderers[i]);

            // Remove its components from started set
            var components = gameObject.Components;
            for (int i = 0; i < components.Count; i++)
                _started.Remove(components[i]);

            gameObject.Destroy();

            GameObjectRemoved?.Invoke(gameObject);

            return true;
        }

        /// <summary>
        /// Registers a mesh renderer with this scene so it can be considered for rendering.
        /// Intended to be called from <see cref="MeshRenderer"/> lifecycle methods.
        /// </summary>
        internal void RegisterRenderer(MeshRenderer renderer)
        {
            if (renderer == null)
                throw new ArgumentNullException(nameof(renderer));

            if (!_renderers.Contains(renderer))
                _renderers.Add(renderer);
        }

        /// <summary>
        /// Unregisters a mesh renderer from this scene.
        /// </summary>
        internal void UnregisterRenderer(MeshRenderer renderer)
        {
            if (renderer == null)
                return;

            _renderers.Remove(renderer);
        }

        /// <summary>
        /// Finds the first GameObject matching the predicate.
        /// </summary>
        public GameObject? Find(Predicate<GameObject> filter)
        {
            for (int i = 0; i < _gameObjects.Count; i++)
            {
                var go = _gameObjects[i];
                if (filter(go))
                    return go;
            }

            return null;
        }

        /// <summary>
        /// Finds all GameObjects matching the predicate. Returns an empty list if none found.
        /// </summary>
        public List<GameObject> FindAll(Predicate<GameObject> filter)
        {
            var found = new List<GameObject>(_gameObjects.Count);

            for (int i = 0; i < _gameObjects.Count; i++)
            {
                var go = _gameObjects[i];
                if (filter(go))
                    found.Add(go);
            }

            return found;
        }

        /// <summary>
        /// Returns all components of type <typeparamref name="T"/> across the entire scene.
        /// </summary>
        public List<T> FindComponentsInScene<T>() where T : Component
        {
            var results = new List<T>();

            for (int i = 0; i < _gameObjects.Count; i++)
            {
                var go = _gameObjects[i];
                var components = go.Components;

                for (int j = 0; j < components.Count; j++)
                {
                    if (components[j] is T t)
                        results.Add(t);
                }
            }

            return results;
        }

        /// <summary>
        /// Finds all GameObjects whose <see cref="GameObject.Layer"/> overlaps the given layer mask.
        /// </summary>
        public List<GameObject> FindByLayer(LayerMask layer)
        {
            var results = new List<GameObject>();

            for (int i = 0; i < _gameObjects.Count; i++)
            {
                var go = _gameObjects[i];
                if (layer.Overlaps(go.Layer))
                    results.Add(go);
            }

            return results;
        }

        /// <summary>
        /// Returns a specific system if one is already added; otherwise returns null.
        /// </summary>
        public T? GetSystem<T>() where T : class
        {
            for (int i = 0; i < _systemsAll.Count; i++)
            {
                if (_systemsAll[i] is T t)
                    return t;
            }
            return null;
        }

        /// <summary>
        /// Attempts to get a specific system; returns true if found.
        /// </summary>
        public bool TryGetSystem<T>(out T? system) where T : class
        {
            system = GetSystem<T>();
            return system != null;
        }

        /// <summary>
        /// Returns the currently active camera.
        /// Prefers the scene's local active camera, then falls back to <see cref="CameraSystem"/> if attached.
        /// </summary>
        public Camera? GetActiveCamera()
        {
            if (_activeCamera != null)
                return _activeCamera;

            var cameraSystem = GetSystem<CameraSystem>();
            if (cameraSystem != null)
                return cameraSystem.ActiveCamera;

            return null;
        }

        /// <summary>
        /// Sets the active camera for this scene.
        /// If a <see cref="CameraSystem"/> is present, its ActiveCamera is updated too.
        /// If not, the scene simply remembers this camera.
        /// </summary>
        public void SetActiveCamera(Camera? camera)
        {
            _activeCamera = camera;

            var cameraSystem = GetSystem<CameraSystem>();
            if (cameraSystem != null)
                cameraSystem.ActiveCamera = camera;
        }

        public void SetActiveCamera(string? targetName)
        {
            var go = Find(go => go.Name.Equals(targetName));
            var camera = go?.GetComponent<Camera>();
            SetActiveCamera(camera);
        }

        /// <summary>
        /// Advances non-render lifecycles and drives component lifecycle. Call once per frame.
        /// </summary>
        public void Update(float deltaTime)
        {
            // The last two lifecycles are Render and PostRender; skip them here.
            var nonRenderCount = _systemsByLifecycle.Length - 2;

            // Run all non-render system lifecycles in index order
            for (int li = 0; li < nonRenderCount; li++)
            {
                var systemBucket = _systemsByLifecycle[li];
                for (int i = 0; i < systemBucket.Count; i++)
                {
                    var s = systemBucket[i];
                    if (!s.Enabled)
                        continue;
                    s.Update(deltaTime);
                }
            }

            // Ensure Start() runs once per component
            for (int i = 0; i < _gameObjects.Count; i++)
            {
                var go = _gameObjects[i];
                if (!go.Enabled)
                    continue;

                var components = go.Components;

                for (int j = 0; j < components.Count; j++)
                {
                    var component = components[j];

                    if (!component.Enabled)
                        continue;

                    if (!_started.Contains(component))
                    {
                        component.InternalStart();
                        _started.Add(component);
                    }
                }
            }

            // Per-frame component Update/LateUpdate
            for (int i = 0; i < _gameObjects.Count; i++)
            {
                var go = _gameObjects[i];
                if (!go.Enabled)
                    continue;

                var components = go.Components;

                for (int j = 0; j < components.Count; j++)
                {
                    var component = components[j];
                    if (!component.Enabled)
                        continue;

                    component.InternalUpdate(deltaTime);
                }
            }

            for (int i = 0; i < _gameObjects.Count; i++)
            {
                var go = _gameObjects[i];
                if (!go.Enabled)
                    continue;

                var components = go.Components;

                for (int j = 0; j < components.Count; j++)
                {
                    var component = components[j];
                    if (!component.Enabled)
                        continue;

                    component.InternalLateUpdate(deltaTime);
                }
            }
        }

        /// <summary>
        /// Dispatches Render and PostRender lifecycles in order.
        /// </summary>
        public void Draw(float deltaTime)
        {
            var renderSystems = _systemsByLifecycle[(int)FrameLifecycle.Render];
            for (int i = 0; i < renderSystems.Count; i++)
            {
                var system = renderSystems[i];
                if (!system.Enabled)
                    continue;
                system.Draw(deltaTime);
            }

            var postRenderSystems = _systemsByLifecycle[(int)FrameLifecycle.PostRender];
            for (int i = 0; i < postRenderSystems.Count; i++)
            {
                var system = postRenderSystems[i];
                if (!system.Enabled)
                    continue;
                system.Draw(deltaTime);
            }
        }

        #endregion

        #region Lifecycle Methods
        // None
        #endregion

        #region Housekeeping Methods

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                Clear();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Scene()
        {
            Dispose(false);
        }

        /// <summary>
        /// Removes all objects and systems from the scene and clears lifecycle state.
        /// </summary>
        public void Clear()
        {
            ClearGameObject();
            ClearSystems();
            ActiveCamera = null;
        }

        /// <summary>
        /// Removes all game objects from the scene, disposing components and unregistering renderers.
        /// </summary>
        public void ClearGameObject()
        {
            for (int i = 0; i < _gameObjects.Count; i++)
            {
                var go = _gameObjects[i];

                // Unregister any renderers
                var renderers = go.GetComponents<MeshRenderer>();
                for (int r = 0; r < renderers.Count; r++)
                    UnregisterRenderer(renderers[r]);

                // Dispose components that implement IDisposable and remove from started set
                var components = go.Components;
                for (int c = 0; c < components.Count; c++)
                {
                    var component = components[c];

                    _started.Remove(component);

                    if (component is IDisposable disposableComponent)
                        disposableComponent.Dispose();
                }

                go.Destroy();
            }

            _gameObjects.Clear();
            _renderers.Clear();
            _started.Clear();
        }

        /// <summary>
        /// Removes all systems from the scene, calling OnRemovedFromScene and disposing where applicable.
        /// </summary>
        public void ClearSystems()
        {
            for (int i = 0; i < _systemsAll.Count; i++)
            {
                var system = _systemsAll[i];
                system.OnRemovedFromScene(this);

                if (system is IDisposable disposable)
                    disposable.Dispose();
            }

            _systemsAll.Clear();

            for (int i = 0; i < _systemsByLifecycle.Length; i++)
                _systemsByLifecycle[i].Clear();
        }

        /// <summary>
        /// String for diagnostics.
        /// </summary>
        public override string ToString()
        {
            return $"Scene(Name={Name}, GameObjects={_gameObjects.Count}, Systems={_systemsAll.Count})";
        }

        #endregion
    }
}
