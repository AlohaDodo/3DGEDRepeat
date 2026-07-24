using GDEngine.Core.Components;
using GDEngine.Core.Rendering.Base;

namespace GDEngine.Core.Entities
{
    /// <summary>
    /// A container for behaviours/components in the scene.
    /// </summary>
    /// <see cref="Transform"/>
    /// <see cref="Component"/>
    public sealed class GameObject : IDisposable
    {
        #region Fields
        private readonly Transform _transform;
        private readonly List<Component> _components = new();
        private Scene? _scene;
        private bool _disposed = false;
        private LayerMask _layer = LayerMask.World;
        private bool _isStatic;
        #endregion

        #region Properties
        public string Name { get; set; }
        public bool Enabled { get; set; } = true;
        public Transform Transform => _transform;
        public IReadOnlyList<Component> Components => _components;
        public Scene? Scene { get => _scene; set => _scene = value; }

        /// <summary>
        /// Logical layer for render and query filtering.
        /// </summary>
        public LayerMask Layer
        {
            get => _layer;
            set => _layer = value;
        }

        /// <summary>
        /// Marks this object as immovable (eligible for static octree/bakes).
        /// </summary>
        public bool IsStatic
        {
            get => _isStatic;
            set => _isStatic = value;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new <see cref="GameObject"/>.
        /// </summary>
        public GameObject(string name = "GameObject")
        {
            Name = name;

            _transform = new Transform();
            _transform.Attach(this);
            _components.Add(_transform);
            _transform.InternalAwake();
        }
        #endregion

        #region Core Methods
        /// <summary>
        /// Adds a new component of type <typeparamref name="T"/>.
        /// </summary>
        public T AddComponent<T>() where T : Component, new()
        {
            return (T)AddComponent(new T());
        }

        /// <summary>
        /// Adds an existing component instance to this object.
        /// </summary>
        public Component AddComponent(Component component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            if (_components.Contains(component))
                return component;

            component.Attach(this);
            _components.Add(component);

            if (_scene != null)
                component.InternalAwake();

            return component;
        }

        /// <summary>
        /// Returns the first component of type <typeparamref name="T"/>, or null.
        /// </summary>
        public T? GetComponent<T>() where T : Component
        {
            for (int i = 0; i < _components.Count; i++)
                if (_components[i] is T t) return t;

            return null;
        }

        /// <summary>
        /// Attempts to get a component of type <typeparamref name="T"/>.
        /// </summary>
        public bool TryGetComponent<T>(out T? component) where T : Component
        {
            component = GetComponent<T>();
            return component != null;
        }

        /// <summary>
        /// Returns all components of type <typeparamref name="T"/>.
        /// </summary>
        public List<T> GetComponents<T>() where T : Component
        {
            var list = new List<T>();
            for (int i = 0; i < _components.Count; i++)
                if (_components[i] is T t) list.Add(t);
            return list;
        }

        /// <summary>
        /// Removes the first component of type <typeparamref name="T"/> (no-op for <see cref="Transform"/>).
        /// </summary>
        public bool RemoveComponent<T>() where T : Component
        {
            for (int i = 0; i < _components.Count; i++)
            {
                if (_components[i] is T t)
                {
                    if (t is Transform)
                        return false;

                    t.InternalDestroy();
                    _components.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes a specific component instance (no-op for <see cref="Transform"/>).
        /// </summary>
        public bool RemoveComponent(Component component)
        {
            if (component == null)
                return false;

            if (component is Transform)
                return false;

            var index = _components.IndexOf(component);
            if (index < 0)
                return false;

            component.InternalDestroy();
            _components.RemoveAt(index);
            return true;
        }
        #endregion

        #region Housekeeping Methods
        /// <summary>
        /// Destroys this object and all its components.
        /// </summary>
        public void Destroy()
        {
            if (_disposed)
                return;

            _disposed = true;

            for (int i = _components.Count - 1; i >= 0; i--)
            {
                var component = _components[i];
                component.InternalDestroy();
            }

            _components.Clear();
            _scene = null;
        }

        public void Dispose()
        {
            Destroy();
            GC.SuppressFinalize(this);
        }

        ~GameObject()
        {
            Dispose();
        }
        #endregion
    }
}
