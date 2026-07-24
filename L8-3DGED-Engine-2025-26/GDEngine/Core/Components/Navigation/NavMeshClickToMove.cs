using GDEngine.Core.Entities;
using GDEngine.Core.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace GDEngine.Core.Components.Navigation
{
    /// <summary>
    /// Simple click-to-move controller for a <see cref="NavMeshAgent"/>.
    /// Casts a ray from the active camera through the mouse cursor and
    /// intersects it with the ground plane (Y = 0) to set the agent destination.
    /// </summary>
    /// <see cref="NavMeshAgent"/>
    public sealed class NavMeshClickToMove : Component
    {
        #region Static Fields
        private const float _minRayY = 0.0001f;
        #endregion

        #region Fields
        private NavMeshAgent? _agent;
        private Scene _scene = null!;
        private EngineContext _context = null!;
        private bool _wasLeftDown;
        #endregion

        #region Properties
        #endregion

        #region Constructors
        public NavMeshClickToMove()
        {
        }
        #endregion

        #region Methods
        private bool TryBuildRay(out Vector3 origin, out Vector3 direction)
        {
            origin = Vector3.Zero;
            direction = Vector3.UnitZ;

            if (_scene.ActiveCamera == null)
                return false;

            var cam = _scene.ActiveCamera;
            Matrix view = cam.View;
            Matrix projection = cam.Projection;

            GraphicsDevice device = _context.GraphicsDevice;
            Viewport viewport = device.Viewport;

            MouseState mouse = Mouse.GetState();
            Vector3 nearPoint = new Vector3(mouse.X, mouse.Y, 0f);
            Vector3 farPoint = new Vector3(mouse.X, mouse.Y, 1f);

            Vector3 nearWorld = viewport.Unproject(nearPoint, projection, view, Matrix.Identity);
            Vector3 farWorld = viewport.Unproject(farPoint, projection, view, Matrix.Identity);

            Vector3 dir = farWorld - nearWorld;
            if (dir.LengthSquared() < 1e-6f)
                return false;

            dir.Normalize();

            origin = nearWorld;
            direction = dir;
            return true;
        }

        private bool TryGetGroundHit(out Vector3 hit)
        {
            hit = Vector3.Zero;

            Vector3 origin;
            Vector3 dir;
            if (!TryBuildRay(out origin, out dir))
                return false;

            if (Math.Abs(dir.Y) < _minRayY)
                return false;

            // Ground plane at Y = 0
            float t = -origin.Y / dir.Y;
            if (t <= 0f)
                return false;

            hit = origin + dir * t;
            return true;
        }

        private bool IsLeftClickPressed()
        {
            MouseState mouse = Mouse.GetState();
            bool isDown = mouse.LeftButton == ButtonState.Pressed;
            bool pressed = isDown && !_wasLeftDown;
            _wasLeftDown = isDown;
            return pressed;
        }
        #endregion

        #region Lifecycle Methods
        protected override void Awake()
        {
            if (GameObject == null)
                throw new InvalidOperationException("NavMeshClickToMove requires a GameObject.");

            if (GameObject.Scene == null)
                throw new InvalidOperationException("NavMeshClickToMove requires the GameObject to be added to a Scene first.");

            _scene = GameObject.Scene;
            _context = _scene.Context;

            _agent = GameObject.GetComponent<NavMeshAgent>();
            if (_agent == null)
                throw new InvalidOperationException("NavMeshClickToMove requires a NavMeshAgent on the same GameObject.");
        }

        protected override void Update(float deltaTime)
        {
            if (_agent == null)
                return;

            if (!IsLeftClickPressed())
                return;

            Vector3 hit;
            if (!TryGetGroundHit(out hit))
                return;

            _agent.SetDestination(hit);
        }
        #endregion

        #region Housekeeping Methods
        public override string ToString()
        {
            return "NavMeshClickToMove()";
        }
        #endregion
    }
}
