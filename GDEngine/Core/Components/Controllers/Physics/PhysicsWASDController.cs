using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace GDEngine.Core.Components.Controllers.Physics
{
    /// <summary>
    /// Physics-based WASD controller: drives a dynamic <see cref="RigidBody"/> by
    /// setting its horizontal linear velocity (XZ) based on keyboard input.
    /// Uses the active camera's yaw (flattened forward/right) as the movement basis.
    /// </summary>
    /// <see cref="RigidBody"/>
    /// <see cref="Camera"/>
    public sealed class PhysicsWASDController : Component
    {
        #region Static Fields
        #endregion

        #region Fields

        private RigidBody _rigidBody = null!;

        private float _moveSpeed = 6f;
        private float _boostMultiplier = 2f;

        private Keys _forwardKey = Keys.W;
        private Keys _backwardKey = Keys.S;
        private Keys _leftKey = Keys.A;
        private Keys _rightKey = Keys.D;
        private Keys _boostKey = Keys.LeftShift;

        private KeyboardState _keyboardState;

        #endregion

        #region Properties

        /// <summary>
        /// Base movement speed in world units per second (applied as velocity magnitude).
        /// </summary>
        public float MoveSpeed
        {
            get => _moveSpeed;
            set => _moveSpeed = value > 0f ? value : 0f;
        }

        /// <summary>
        /// Multiplier applied to <see cref="MoveSpeed"/> when the boost key is held.
        /// </summary>
        public float BoostMultiplier
        {
            get => _boostMultiplier;
            set => _boostMultiplier = value > 0f ? value : 1f;
        }

        /// <summary>
        /// Key used to move forward.
        /// </summary>
        public Keys ForwardKey
        {
            get => _forwardKey;
            set => _forwardKey = value;
        }

        /// <summary>
        /// Key used to move backward.
        /// </summary>
        public Keys BackwardKey
        {
            get => _backwardKey;
            set => _backwardKey = value;
        }

        /// <summary>
        /// Key used to move left (strafe).
        /// </summary>
        public Keys LeftKey
        {
            get => _leftKey;
            set => _leftKey = value;
        }

        /// <summary>
        /// Key used to move right (strafe).
        /// </summary>
        public Keys RightKey
        {
            get => _rightKey;
            set => _rightKey = value;
        }

        /// <summary>
        /// Key used to apply a speed boost (sprint).
        /// </summary>
        public Keys BoostKey
        {
            get => _boostKey;
            set => _boostKey = value;
        }

        #endregion

        #region Constructors
        #endregion

        #region Methods

        /// <summary>
        /// Computes a flattened (XZ) forward/right basis from the active camera
        /// or, if no active camera is available, from this component's Transform.
        /// </summary>
        private void GetMovementBasis(out Vector3 forward, out Vector3 right)
        {
            forward = Vector3.Forward;
            right = Vector3.Right;

            var scene = GameObject?.Scene;
            var camera = scene?.ActiveCamera;

            if (camera != null)
            {
                // Use camera's orientation
                forward = camera.Transform.Forward;
                right = camera.Transform.Right;
            }
            else if (Transform != null)
            {
                // Fallback: use this object's orientation
                forward = Transform.Forward;
                right = Transform.Right;
            }

            // Flatten to XZ plane
            forward.Y = 0f;
            right.Y = 0f;

            if (forward.LengthSquared() > 0f)
                forward.Normalize();
            else
                forward = Vector3.Forward;

            if (right.LengthSquared() > 0f)
                right.Normalize();
            else
                right = Vector3.Right;
        }

        #endregion

        #region Lifecycle Methods

        protected override void Start()
        {
            if (GameObject == null)
                throw new NullReferenceException(nameof(GameObject));

            _rigidBody = GameObject.GetComponent<RigidBody>()
                         ?? throw new InvalidOperationException(
                             "PhysicsWASDController requires a RigidBody on the same GameObject.");

            if (_rigidBody.BodyType != BodyType.Dynamic)
                System.Diagnostics.Debug.WriteLine(
                    "PhysicsWASDController: RigidBody is not Dynamic; movement may not behave as expected.");
        }

        protected override void Update(float deltaTime)
        {
            _keyboardState = Keyboard.GetState();

            // Determine movement basis from camera (yaw) or our own Transform.
            GetMovementBasis(out var forward, out var right);

            Vector3 moveDir = Vector3.Zero;

            if (_keyboardState.IsKeyDown(_forwardKey))
                moveDir += forward;
            if (_keyboardState.IsKeyDown(_backwardKey))
                moveDir -= forward;
            if (_keyboardState.IsKeyDown(_rightKey))
                moveDir += right;
            if (_keyboardState.IsKeyDown(_leftKey))
                moveDir -= right;

            float speed = _moveSpeed;
            if (_keyboardState.IsKeyDown(_boostKey))
                speed *= _boostMultiplier;

            // Read current velocity so we preserve vertical motion (gravity/jumps).
            Vector3 velocity = _rigidBody.LinearVelocity;

            if (moveDir.LengthSquared() > 0f && speed > 0f)
            {
                moveDir.Normalize();

                // Set horizontal velocity; keep Y as-is.
                velocity.X = moveDir.X * speed;
                velocity.Z = moveDir.Z * speed;
            }
            else
            {
                // No movement input: stop horizontal motion, let damping & gravity handle the rest.
                velocity.X = 0f;
                velocity.Z = 0f;
            }

            _rigidBody.LinearVelocity = velocity;
        }

        #endregion

        #region Housekeeping Methods
        #endregion
    }
}
