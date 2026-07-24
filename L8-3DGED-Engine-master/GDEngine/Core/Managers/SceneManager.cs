using GDEngine.Core.Entities;
using GDEngine.Core.Events;
using GDEngine.Core.Rendering.UI;
using GDEngine.Core.Timing;
using Microsoft.Xna.Framework;

namespace GDEngine.Core.Managers
{
    /// <summary>
    /// Manages a collection of <see cref="Scene"/> instances and forwards
    /// MonoGame update/draw calls to the currently active scene.
    /// Also exposes summary information via <see cref="IShowDebugInfo"/> for UI overlays
    /// and disposes owned scenes when removed, cleared, or when the manager is disposed.
    /// </summary>
    public sealed class SceneManager : DrawableGameComponent, IShowDebugInfo
    {
        #region Static Fields
        #endregion

        #region Fields
        private readonly Dictionary<string, Scene> _scenes = new Dictionary<string, Scene>(8);
        private string? _activeSceneName;
        private Scene? _activeScene;

        private bool _disposed;
        private bool _paused;
        private EventBus _eventBus;
        #endregion

        #region Properties
        /// <summary>
        /// Name of the currently active scene, or null if none is active.
        /// </summary>
        public string? ActiveSceneName
        {
            get { return _activeSceneName; }
        }

        /// <summary>
        /// Currently active scene instance, or null if none is active.
        /// </summary>
        public Scene? ActiveScene
        {
            get { return _activeScene; }
        }

        /// <summary>
        /// Read-only view of all registered scenes.
        /// </summary>
        public IReadOnlyDictionary<string, Scene> Scenes
        {
            get { return _scenes; }
        }

        /// <summary>
        /// True if a valid active scene is currently set.
        /// </summary>
        public bool HasActiveScene
        {
            get { return _activeScene != null; }
        }

        /// <summary>
        /// True when the game is globally paused. This does NOT stop the scene
        /// from updating; instead it broadcasts a pause-changed event that
        /// a PausableSystem (e.g. PhysicsSystem) can react to.
        /// </summary>
        public bool Paused
        {
            get { return _paused; }
            set
            {
                if (_paused == value)
                    return;

                _paused = value;

                // Broadcast to the whole game
                _eventBus?.Publish(new GamePauseChangedEvent(_paused));
            }
        }

        public EventBus EventBus { set => _eventBus = value; }


        #endregion

        #region Constructors
        public SceneManager(Game game)
            : base(game)
        {

        }
        #endregion

        #region Methods
        /// <summary>
        /// Register a new scene with the manager.
        /// </summary>
        /// <param name="name">Unique key for the scene.</param>
        /// <param name="scene">Scene instance to register.</param>
        /// <exception cref="ArgumentNullException">If name or scene is null.</exception>
        /// <exception cref="ArgumentException">If a scene already exists with the same name.</exception>
        public void AddScene(string name, Scene scene)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (scene == null)
                throw new ArgumentNullException(nameof(scene));

            if (_scenes.ContainsKey(name))
                throw new ArgumentException("A scene with the same name is already registered: " + name, nameof(name));

            _scenes.Add(name, scene);
        }

        /// <summary>
        /// Remove a scene by name and dispose it.
        /// If the removed scene is active, the active scene is cleared.
        /// </summary>
        public bool RemoveScene(string name)
        {
            return RemoveScene(name, true);
        }

        /// <summary>
        /// Remove a scene by name.
        /// If the removed scene is active, the active scene is cleared.
        /// </summary>
        /// <param name="name">Scene name to remove.</param>
        /// <param name="disposeScene">If true, calls Dispose() on the scene if it was found.</param>
        public bool RemoveScene(string name, bool disposeScene)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (!_scenes.TryGetValue(name, out Scene? scene))
                return false;

            _scenes.Remove(name);

            if (string.Equals(_activeSceneName, name, StringComparison.Ordinal))
            {
                _activeSceneName = null;
                _activeScene = null;
            }

            if (disposeScene && scene != null)
                scene.Dispose();

            return true;
        }

        /// <summary>
        /// Attempt to get a scene by name.
        /// </summary>
        public bool TryGetScene(string name, out Scene scene)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            return _scenes.TryGetValue(name, out scene!);
        }

        /// <summary>
        /// Set the currently active scene by name.
        /// </summary>
        /// <param name="name">Name of the scene to activate.</param>
        /// <param name="throwIfMissing">If true, throws if the scene is not found.</param>
        /// <returns>True if the scene was found and activated, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">If name is null.</exception>
        /// <exception cref="ArgumentException">If throwIfMissing is true and scene is not found.</exception>
        public bool SetActiveScene(string name, bool throwIfMissing = true)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            if (!_scenes.TryGetValue(name, out Scene? scene))
            {
                if (throwIfMissing)
                    throw new ArgumentException("No scene registered with name: " + name, nameof(name));

                return false;
            }

            _activeSceneName = name;
            _activeScene = scene;
            return true;
        }

        /// <summary>
        /// Clear all scenes and dispose them.
        /// </summary>
        public void ClearScenes()
        {
            ClearScenes(true);
        }

        /// <summary>
        /// Clear all scenes and reset the active scene.
        /// </summary>
        /// <param name="disposeScenes">If true, calls Dispose() on all registered scenes.</param>
        public void ClearScenes(bool disposeScenes)
        {
            if (disposeScenes)
            {
                foreach (KeyValuePair<string, Scene> kvp in _scenes)
                {
                    Scene scene = kvp.Value;
                    if (scene != null)
                        scene.Dispose();
                }
            }

            _scenes.Clear();
            _activeSceneName = null;
            _activeScene = null;
        }

        /// <summary>
        /// Provide scene manager debug lines for on-screen display via <see cref="UIDebugInfo"/>.
        /// </summary>
        public IEnumerable<string> GetDebugLines()
        {
            string activeName = _activeSceneName ?? "none";
            yield return "SceneManager  Active=" + activeName;
            yield return "Active Camera=" + ActiveScene.ActiveCamera.GameObject.Name;
            yield return "  Paused=" + _paused.ToString();
            yield return "Scenes=" + _scenes.Count.ToString();
          
            if (_scenes.Count == 0)
                yield break;

            int shown = 0;
            foreach (KeyValuePair<string, Scene> kvp in _scenes)
            {
                string marker = string.Equals(kvp.Key, _activeSceneName, StringComparison.Ordinal)
                    ? "*>"
                    : " -";

                yield return "  " + marker + " " + kvp.Key;

                shown++;
                if (shown >= 6)
                {
                    if (_scenes.Count > shown)
                        yield return "  ...";
                    break;
                }
            }
        }
        #endregion

        #region Lifecycle Methods
        public override void Update(GameTime gameTime)
        {
            if (!Enabled)
            {
                base.Update(gameTime);
                return;
            }

            if (_activeScene != null)
                _activeScene.Update(Time.DeltaTimeSecs);

            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime)
        {
            if (!Visible)
            {
                base.Draw(gameTime);
                return;
            }

            if (_activeScene != null)
                _activeScene.Draw(Time.DeltaTimeSecs);

            base.Draw(gameTime);
        }

        /// <summary>
        /// Dispose the SceneManager and (by default) all registered scenes.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                base.Dispose(disposing);
                return;
            }

            if (disposing)
            {
                // Dispose managed resources (owned scenes)
                foreach (KeyValuePair<string, Scene> kvp in _scenes)
                {
                    Scene scene = kvp.Value;
                    if (scene != null)
                        scene.Dispose();
                }

                _scenes.Clear();
                _activeScene = null;
                _activeSceneName = null;
            }

            _disposed = true;

            base.Dispose(disposing);
        }
        #endregion

        #region Housekeeping Methods
        public override string ToString()
        {
            return "SceneManager(Active=" + (_activeSceneName ?? "none") + ", Count=" + _scenes.Count + ")";
        }
        #endregion
    }
}
