using GDEngine.Core.Components.Navigation;
using GDEngine.Core.Entities;
using GDEngine.Core.Enums;
using GDEngine.Core.Navigation;
using GDEngine.Core.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GDEngine.Core.Systems
{
    /// <summary>
    /// Scene-level navigation system.
    /// Owns the <see cref="NavMesh"/> and an <see cref="IPathfinder"/> strategy and
    /// exposes a high-level <see cref="INavigationService"/> for agents.
    /// </summary>
    /// <see cref="NavMesh"/>
    /// <see cref="IPathfinder"/>
    /// <see cref="INavigationService"/>
    public sealed class NavMeshSystem : PausableSystemBase, INavigationService
    {
        #region Static Fields
        #endregion

        #region Fields
        private Scene _scene = null!;
        private readonly NavMesh _navMesh;
        private readonly IPathfinder _pathfinder;
        private readonly List<Vector3> _scratchPath = new List<Vector3>(128);
        #endregion

        #region Properties
        public NavMesh NavMesh
        {
            get
            {
                return _navMesh;
            }
        }
        #endregion

        #region Constructors
        public NavMeshSystem(int order = 0)
            : base(FrameLifecycle.Update, order)
        {
            PauseMode = PauseMode.Update;

            _navMesh = new NavMesh();

            _pathfinder = new AStarPathfinder(
                (a, b) =>
                {
                    Vector3 d = b - a;
                    d.Y = 0f;
                    return d.Length();
                });
        }
        #endregion

        #region Methods
        public void BuildWith(INavMeshBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            builder.Build(_navMesh);
        }

        public void BuildFlatGrid(
            int width,
            int height,
            float cellSize,
            Vector3 origin,
            Func<Vector3, bool>? isWalkable = null)
        {
            var builder = new GridNavMeshBuilder(width, height, cellSize, origin, isWalkable);
            BuildWith(builder);
        }

        public bool TryFindPath(Vector3 start, Vector3 end, List<Vector3> outPath)
        {
            if (outPath == null)
                throw new ArgumentNullException(nameof(outPath));

            if (_navMesh.NodeCount == 0)
                return false;

            return _pathfinder.TryFindPath(_navMesh, start, end, outPath);
        }

        public IReadOnlyList<Vector3>? TryFindPath(Vector3 start, Vector3 end)
        {
            _scratchPath.Clear();

            if (!TryFindPath(start, end, _scratchPath))
                return null;

            return _scratchPath;
        }
        #endregion

        #region Lifecycle Methods
        protected override void OnAdded()
        {
            if (Scene == null)
                throw new NullReferenceException(nameof(Scene));

            _scene = Scene;
        }

        protected override void OnUpdate(float deltaTime)
        {
            // Future: dynamic obstacles, local re-bakes, etc.
        }
        #endregion

        #region Housekeeping Methods
        public override string ToString()
        {
            return $"NavMeshSystem(Nodes={_navMesh.NodeCount})";
        }
        #endregion
    }

    /// <summary>
    /// Draws debug visualisations for the navigation mesh:
    /// - Nodes (walkable / blocked)
    /// - Neighbour links
    /// - Paths of all active NavMeshAgents
    /// 
    /// Uses a top-down 2D overlay (world XZ mapped to screen XY) via SpriteBatch.
    /// Includes a show/hide toggle on a keyboard key (F2 by default).
    /// </summary>
    /// <see cref="NavMeshSystem"/>
    /// <see cref="NavMeshAgent"/>
    public sealed class NavMeshDebugSystem : SystemBase
    {
        #region Static Fields
        private static readonly Color _walkableColor = Color.White;
        private static readonly Color _blockedColor = Color.Red;
        private static readonly Color _linkColor = Color.Gray;
        private static readonly Color _pathColor = Color.Yellow;

        private static readonly SpriteSortMode _sort = SpriteSortMode.Deferred;
        private static readonly RasterizerState _raster = RasterizerState.CullCounterClockwise;
        private static readonly DepthStencilState _depth = DepthStencilState.None;
        private static readonly BlendState _blend = BlendState.AlphaBlend;
        private static readonly SamplerState _sampler = SamplerState.PointClamp;
        #endregion

        #region Fields
        private Scene _scene = null!;
        private EngineContext _context = null!;
        private GraphicsDevice _device = null!;
        private SpriteBatch _spriteBatch = null!;
        private Texture2D _pixel = null!;

        private NavMeshSystem? _navMeshSystem;
        private readonly List<NavMeshAgent> _agents = new List<NavMeshAgent>(32);

        private Vector2 _offset = new Vector2(64f, 64f);
        private float _scale = 16f;

        private bool _isVisible;
        private Keys _toggleKey;
        private bool _toggleKeyWasDown;
        #endregion

        #region Properties
        public Vector2 DebugOffset
        {
            get
            {
                return _offset;
            }
            set
            {
                _offset = value;
            }
        }

        public float DebugScale
        {
            get
            {
                return _scale;
            }
            set
            {
                if (value > 0f)
                    _scale = value;
            }
        }

        public bool IsVisible
        {
            get
            {
                return _isVisible;
            }
            set
            {
                _isVisible = value;
            }
        }

        public Keys ToggleKey
        {
            get
            {
                return _toggleKey;
            }
            set
            {
                _toggleKey = value;
            }
        }
        #endregion

        #region Constructors
        public NavMeshDebugSystem(int order = 8900)
            : base(FrameLifecycle.Render, order)
        {
            _isVisible = true;
            _toggleKey = Keys.F2;
            _toggleKeyWasDown = false;
        }
        #endregion

        #region Methods
        private void CacheAgents()
        {
            _agents.Clear();

            var all = _scene.GameObjects;
            for (int i = 0; i < all.Count; i++)
            {
                var agent = all[i].GetComponent<NavMeshAgent>();
                if (agent != null)
                    _agents.Add(agent);
            }
        }

        private Vector2 WorldToScreen(in Vector3 world)
        {
            return new Vector2(
                _offset.X + world.X * _scale,
                _offset.Y + world.Z * _scale);
        }

        private void DrawLine(Vector2 a, Vector2 b, Color color)
        {
            Vector2 delta = b - a;
            float length = delta.Length();

            if (length <= 0.0001f)
                return;

            float angle = (float)Math.Atan2(delta.Y, delta.X);

            _spriteBatch.Draw(
                _pixel,
                a,
                null,
                color,
                angle,
                Vector2.Zero,
                new Vector2(length, 1f),
                SpriteEffects.None,
                0f);
        }

        private void DrawNavMesh()
        {
            if (_navMeshSystem == null)
                return;

            var mesh = _navMeshSystem.NavMesh;
            if (mesh.NodeCount == 0)
                return;

            float half = 0.3f;

            for (int i = 0; i < mesh.NodeCount; i++)
            {
                Vector3 p3 = mesh.GetNodePosition(i);
                bool walkable = mesh.IsWalkable(i);

                Color c = walkable ? _walkableColor : _blockedColor;

                Vector2 center = WorldToScreen(p3);
                Vector2 a = center + new Vector2(-half * _scale, -half * _scale);
                Vector2 b = center + new Vector2(half * _scale, -half * _scale);
                Vector2 d = center + new Vector2(half * _scale, half * _scale);
                Vector2 e = center + new Vector2(-half * _scale, half * _scale);

                DrawLine(a, b, c);
                DrawLine(b, d, c);
                DrawLine(d, e, c);
                DrawLine(e, a, c);

                int neighbourCount = mesh.GetNeighbourCount(i);
                for (int n = 0; n < neighbourCount; n++)
                {
                    int neighbourId = mesh.GetNeighbourId(i, n);
                    Vector3 pNeighbour = mesh.GetNodePosition(neighbourId);

                    Vector2 s = WorldToScreen(p3);
                    Vector2 t = WorldToScreen(pNeighbour);

                    DrawLine(s, t, _linkColor);
                }
            }
        }

        private void DrawAgentPaths()
        {
            for (int i = 0; i < _agents.Count; i++)
            {
                NavMeshAgent agent = _agents[i];

                var path = agent.DebugPath;
                if (path == null)
                    continue;

                if (path.Count < 2)
                    continue;

                Vector2 prev = WorldToScreen(path[0]);

                for (int p = 1; p < path.Count; p++)
                {
                    Vector2 next = WorldToScreen(path[p]);
                    DrawLine(prev, next, _pathColor);
                    prev = next;
                }

                if (agent.Transform != null)
                {
                    Vector2 pos = WorldToScreen(agent.Transform.Position);
                    Vector2 markerA = pos + new Vector2(-3f, -3f);
                    Vector2 markerB = pos + new Vector2(3f, -3f);
                    Vector2 markerD = pos + new Vector2(3f, 3f);
                    Vector2 markerE = pos + new Vector2(-3f, 3f);

                    DrawLine(markerA, markerB, _pathColor);
                    DrawLine(markerB, markerD, _pathColor);
                    DrawLine(markerD, markerE, _pathColor);
                    DrawLine(markerE, markerA, _pathColor);
                }
            }
        }

        private void UpdateToggleKey()
        {
            KeyboardState state = Keyboard.GetState();
            bool isDown = state.IsKeyDown(_toggleKey);

            if (isDown && !_toggleKeyWasDown)
                _isVisible = !_isVisible;

            _toggleKeyWasDown = isDown;
        }
        #endregion

        #region Lifecycle Methods
        protected override void OnAdded()
        {
            if (Scene == null)
                throw new NullReferenceException(nameof(Scene));

            _scene = Scene;
            _context = _scene.Context;
            _device = _context.GraphicsDevice;
            _spriteBatch = _context.SpriteBatch;

            _pixel = new Texture2D(_device, 1, 1);
            _pixel.SetData(new[] { Color.White });

            _navMeshSystem = _scene.GetSystem<NavMeshSystem>();
        }

        public override void Draw(float deltaTime)
        {
            UpdateToggleKey();

            if (!_isVisible)
                return;

            if (_scene.ActiveCamera == null)
                return;

            if (_navMeshSystem == null)
                _navMeshSystem = _scene.GetSystem<NavMeshSystem>();

            if (_navMeshSystem == null)
                return;

            CacheAgents();

            if (_navMeshSystem.NavMesh.NodeCount == 0)
                return;

            _spriteBatch.Begin(_sort, _blend, _sampler, _depth, _raster);

            DrawNavMesh();
            DrawAgentPaths();

            _spriteBatch.End();
        }
        #endregion

        #region Housekeeping Methods
        public override string ToString()
        {
            return $"NavMeshDebugSystem(Scale={_scale}, Offset={_offset}, Visible={_isVisible})";
        }
        #endregion
    }
}
