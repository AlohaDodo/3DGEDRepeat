using GDEngine.Core.Entities;
using GDEngine.Core.Navigation;
using Microsoft.Xna.Framework;

namespace GDEngine.Core.Components.Navigation
{
    /// <summary>
    /// Unity-style navigation agent that consumes an <see cref="INavigationService"/>
    /// and an <see cref="IAgentMovement"/> to follow paths on a scene navmesh.
    /// Raises events when a destination is reached or when a path cannot be computed.
    /// </summary>
    /// <see cref="INavigationService"/>
    /// <see cref="IAgentMovement"/>
    public sealed class NavMeshAgent : Component
    {
        #region Static Fields
        #endregion

        #region Fields
        private readonly List<Vector3> _path = new List<Vector3>(64);
        private int _currentIndex;
        private float _speed;
        private float _stoppingDistance;
        private bool _isStopped;
        private bool _hasPath;
        private INavigationService? _navigation;
        private IAgentMovement _movement;
        #endregion

        #region Properties
        public float Speed
        {
            get
            {
                return _speed;
            }
            set
            {
                if (value < 0f)
                    _speed = 0f;
                else
                    _speed = value;
            }
        }

        public float StoppingDistance
        {
            get
            {
                return _stoppingDistance;
            }
            set
            {
                if (value < 0f)
                    _stoppingDistance = 0f;
                else
                    _stoppingDistance = value;
            }
        }

        public bool IsStopped
        {
            get
            {
                return _isStopped;
            }
            set
            {
                _isStopped = value;
            }
        }

        public bool HasPath
        {
            get
            {
                return _hasPath;
            }
        }

        public bool ReachedDestination
        {
            get
            {
                if (!_hasPath)
                    return true;

                if (Transform == null)
                    return false;

                if (_path.Count == 0)
                    return true;

                Vector3 pos = Transform.Position;
                Vector3 target = _path[_path.Count - 1];

                float distSq = Vector3.DistanceSquared(pos, target);
                float stopSq = _stoppingDistance * _stoppingDistance;
                return distSq <= stopSq;
            }
        }

        public IAgentMovement MovementStrategy
        {
            get
            {
                return _movement;
            }
            set
            {
                if (value != null)
                    _movement = value;
            }
        }

        public IReadOnlyList<Vector3> DebugPath
        {
            get
            {
                return _path;
            }
        }

        public int DebugCurrentIndex
        {
            get
            {
                return _currentIndex;
            }
        }
        #endregion

        #region Events
        public event Action<NavMeshAgent>? DestinationReached;
        public event Action<NavMeshAgent>? PathFailed;
        #endregion

        #region Constructors
        public NavMeshAgent()
        {
            _speed = 3.0f;
            _stoppingDistance = 0.1f;
            _isStopped = false;
            _hasPath = false;
            _currentIndex = 0;
            _movement = new SimpleNavMovement();
        }
        #endregion

        #region Methods
        public bool SetDestination(Vector3 worldTarget)
        {
            if (GameObject == null)
                return false;

            if (Transform == null)
                return false;

            Scene? scene = GameObject.Scene;
            if (scene == null)
                return false;

            if (_navigation == null)
            {
                _navigation = scene.GetSystem<INavigationService>();
            }

            if (_navigation == null)
                return false;

            _path.Clear();
            _currentIndex = 0;

            Vector3 start = Transform.Position;

            if (!_navigation.TryFindPath(start, worldTarget, _path))
            {
                _hasPath = false;
                PathFailed?.Invoke(this);
                return false;
            }

            if (_path.Count == 0)
            {
                _hasPath = false;
                PathFailed?.Invoke(this);
                return false;
            }

            _hasPath = true;
            _isStopped = false;
            return true;
        }

        public void ResetPath()
        {
            _path.Clear();
            _hasPath = false;
            _currentIndex = 0;
        }
        #endregion

        #region Lifecycle Methods
        protected override void Awake()
        {
            if (GameObject == null)
                return;

            if (GameObject.Scene == null)
                return;

            _navigation = GameObject.Scene.GetSystem<INavigationService>();
        }

        protected override void Update(float deltaTime)
        {
            if (_isStopped)
                return;

            if (!_hasPath)
                return;

            if (Transform == null)
                return;

            if (_movement == null)
                return;

            _movement.TickMovement(
                Transform,
                _path,
                ref _currentIndex,
                _speed,
                _stoppingDistance,
                deltaTime);

            if (_currentIndex >= _path.Count)
            {
                _hasPath = false;
                DestinationReached?.Invoke(this);
            }
        }
        #endregion

        #region Housekeeping Methods
        public override string ToString()
        {
            return $"NavMeshAgent(Speed={_speed}, HasPath={_hasPath})";
        }
        #endregion
    }
}
