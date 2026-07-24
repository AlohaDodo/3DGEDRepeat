using GDEngine.Core.Entities;
using GDEngine.Core.Rendering.Base;
using GDEngine.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace GDEngine.Core.Components.Controllers.Physics
{
    /// <summary>
    /// First-person controller using a Dynamic RigidBody for full physics integration.
    /// Provides force-based WASD movement, jumping, and automatic collision response.
    /// Also supports a non-physics mode that moves the Transform directly (fly camera).
    /// This controller handles movement only. For mouse look functionality,
    /// add a <see cref="MouseYawPitchController"/> to the same GameObject.
    /// </summary>
    /// <b>Usage Example:</b>
    /// <code>
    /// var player = new GameObject("Player");
    /// player.AddComponent&lt;Camera&gt;();
    /// player.AddComponent&lt;MouseYawPitchController&gt;();
    /// var controller = player.AddComponent&lt;CollidableFirstPersonController&gt;();
    /// controller.MoveSpeed = 5f;
    /// controller.JumpImpulse = 8f;
    /// controller.UsePhysicsMovement = true; // or false for fly camera
    /// scene.Add(player);
    /// </code>
    /// <see cref="RigidBody"/>
    /// <see cref="CapsuleCollider"/>
    /// <see cref="Camera"/>
    /// <see cref="MouseYawPitchController"/>
    /// <see cref="PhysicsSystem"/>
    public sealed class CollidableFirstPersonController : Component
    {
        #region Constants

        private const float DEFAULT_CAPSULE_RADIUS = 0.4f;
        private const float DEFAULT_CAPSULE_HEIGHT = 1.8f;
        private const float DEFAULT_EYE_HEIGHT_RATIO = 0.9f;
        private const float GROUND_CHECK_SKIN_WIDTH = 0.05f;
        private const float DEFAULT_MASS = 80f;

        #endregion

        #region Fields

        // References
        private RigidBody _rigidBody;
        private CapsuleCollider _capsuleCollider;
        private Camera _camera;
        private PhysicsSystem _physicsSystem;

        // Movement settings
        private float _moveSpeed = 5f;
        private float _sprintMultiplier = 1.8f;
        private float _jumpImpulse = 7f;
        private float _mass = DEFAULT_MASS;

        // Capsule configuration
        private float _capsuleRadius = DEFAULT_CAPSULE_RADIUS;
        private float _capsuleHeight = DEFAULT_CAPSULE_HEIGHT;
        private float _eyeHeightRatio = DEFAULT_EYE_HEIGHT_RATIO;

        // Ground detection
        private float _groundCheckDistance = 0.15f;
        private bool _isGrounded;
        private LayerMask _groundLayers = LayerMask.All;

        // Air control / damping
        private float _airControlFactor = 0.3f;
        private float _groundFriction = 8f;

        // Rotation control
        private bool _freezeRotation = true;

        // Movement mode
        private bool _usePhysicsMovement = true;

        // Input state
        private KeyboardState _currentKeyboard;
        private KeyboardState _previousKeyboard;

        // Input control
        private bool _isInputEnabled = true;

        // Key bindings
        private Keys _forwardKey = Keys.W;
        private Keys _backwardKey = Keys.S;
        private Keys _leftKey = Keys.A;
        private Keys _rightKey = Keys.D;
        private Keys _jumpKey = Keys.Space;
        private Keys _sprintKey = Keys.LeftShift;

        #endregion

        #region Properties

        public float MoveSpeed
        {
            get => _moveSpeed;
            set => _moveSpeed = MathHelper.Max(0f, value);
        }

        public float SprintMultiplier
        {
            get => _sprintMultiplier;
            set => _sprintMultiplier = MathHelper.Max(1f, value);
        }

        public float JumpImpulse
        {
            get => _jumpImpulse;
            set => _jumpImpulse = MathHelper.Max(0f, value);
        }

        public float Mass
        {
            get => _mass;
            set
            {
                _mass = MathHelper.Max(1f, value);
                if (_rigidBody != null)
                    _rigidBody.Mass = _mass;
            }
        }

        public float CapsuleRadius
        {
            get => _capsuleRadius;
            set
            {
                _capsuleRadius = MathHelper.Max(0.1f, value);

                if (_capsuleHeight < _capsuleRadius * 2f)
                    _capsuleHeight = _capsuleRadius * 2f;

                UpdateCapsuleGeometry();
            }
        }

        public float CapsuleHeight
        {
            get => _capsuleHeight;
            set
            {
                _capsuleHeight = MathHelper.Max(_capsuleRadius * 2f, value);
                UpdateCapsuleGeometry();
            }
        }

        public float EyeHeightRatio
        {
            get => _eyeHeightRatio;
            set => _eyeHeightRatio = MathHelper.Clamp(value, 0f, 1f);
        }

        public float GroundCheckDistance
        {
            get => _groundCheckDistance;
            set => _groundCheckDistance = MathHelper.Max(0.01f, value);
        }

        public LayerMask GroundLayers
        {
            get => _groundLayers;
            set => _groundLayers = value;
        }

        public float AirControlFactor
        {
            get => _airControlFactor;
            set => _airControlFactor = MathHelper.Clamp(value, 0f, 1f);
        }

        public float GroundFriction
        {
            get => _groundFriction;
            set => _groundFriction = MathHelper.Max(0f, value);
        }

        public bool FreezeRotation
        {
            get => _freezeRotation;
            set => _freezeRotation = value;
        }

        /// <summary>
        /// When true, uses RigidBody/Physics for movement (collidable character).
        /// When false, movement is applied directly to the Transform (fly camera).
        /// </summary>
        public bool UsePhysicsMovement
        {
            get => _usePhysicsMovement;
            set => _usePhysicsMovement = value;
        }

        /// <summary>
        /// When false, the controller will not read input or move.
        /// Useful for menus, cutscenes, or pause screens.
        /// </summary>
        public bool IsInputEnabled
        {
            get => _isInputEnabled;
            set => _isInputEnabled = value;
        }

        public Vector3 Velocity => _rigidBody?.LinearVelocity ?? Vector3.Zero;

        public float VerticalVelocity => _rigidBody?.LinearVelocity.Y ?? 0f;

        public Vector3 EyePosition
        {
            get
            {
                if (Transform == null)
                    return Vector3.Zero;

                float eyeHeight = _capsuleHeight * _eyeHeightRatio;
                return Transform.Position + Vector3.Up * eyeHeight;
            }
        }

        #region Key Bindings

        public Keys ForwardKey
        {
            get => _forwardKey;
            set => _forwardKey = value;
        }

        public Keys BackwardKey
        {
            get => _backwardKey;
            set => _backwardKey = value;
        }

        public Keys LeftKey
        {
            get => _leftKey;
            set => _leftKey = value;
        }

        public Keys RightKey
        {
            get => _rightKey;
            set => _rightKey = value;
        }

        public Keys JumpKey
        {
            get => _jumpKey;
            set => _jumpKey = value;
        }

        public Keys SprintKey
        {
            get => _sprintKey;
            set => _sprintKey = value;
        }

        #endregion

        #endregion

        #region Lifecycle Methods

        protected override void Awake()
        {
            if (GameObject == null)
                throw new InvalidOperationException(
                    "CollidableFirstPersonController requires a GameObject.");

            var scene = GameObject.Scene;
            if (scene == null)
                throw new InvalidOperationException(
                    "CollidableFirstPersonController requires the GameObject to be in a Scene.");

            _physicsSystem = scene.GetSystem<PhysicsSystem>();
            if (_physicsSystem == null)
                throw new InvalidOperationException(
                    "CollidableFirstPersonController requires a PhysicsSystem in the Scene.");

            _camera = GameObject.GetComponent<Camera>();
            if (_camera == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    "CollidableFirstPersonController: No Camera found, creating one.");
                _camera = GameObject.AddComponent<Camera>();
            }

            _capsuleCollider = GameObject.GetComponent<CapsuleCollider>();
            if (_capsuleCollider == null)
            {
                _capsuleCollider = new CapsuleCollider(_capsuleRadius, _capsuleHeight);
                GameObject.AddComponent(_capsuleCollider);
            }

            UpdateCapsuleGeometry();

            _rigidBody = GameObject.GetComponent<RigidBody>();
            if (_rigidBody == null)
            {
                _rigidBody = new RigidBody
                {
                    BodyType = BodyType.Dynamic,
                    Mass = _mass,
                    UseGravity = true,
                    LinearDamping = 0f,
                    AngularDamping = 1f
                };
                GameObject.AddComponent(_rigidBody);
            }

            _rigidBody.BodyType = BodyType.Dynamic;
            _rigidBody.Mass = _mass;
            _rigidBody.UseGravity = true;
            _rigidBody.LinearDamping = 0f;
            _rigidBody.AngularDamping = 1f;

            UpdateRotationFreeze();
        }

        protected override void Start()
        {
            _previousKeyboard = Keyboard.GetState();
        }

        protected override void Update(float deltaTime)
        {
            if (Transform == null)
                return;

            _currentKeyboard = Keyboard.GetState();

            if (_isInputEnabled == false)
            {
                _previousKeyboard = _currentKeyboard;
                return;
            }

             if (_usePhysicsMovement && _rigidBody != null && _physicsSystem != null && _capsuleCollider != null)
            {
                UpdateGroundCheck();
                UpdateMovementPhysics(deltaTime);
                UpdateRotationFreeze();
            }
            else
            {
                UpdateMovementTransform(deltaTime);
            }

            _previousKeyboard = _currentKeyboard;
        }

        #endregion

        /// <summary>
        /// Keeps the capsule collider in sync with radius/height and
        /// positions it so Transform.Position is at the feet.
        /// </summary>
        private void UpdateCapsuleGeometry()
        {
            if (_capsuleCollider == null)
                return;

            _capsuleCollider.Radius = _capsuleRadius;
            _capsuleCollider.Height = _capsuleHeight;

            // Center so that the bottom of the lower sphere is at y = 0 (feet)
            float centerY = _capsuleHeight * 0.5f - _capsuleRadius;
            _capsuleCollider.Center = new Vector3(0f, centerY, 0f);
        }

        #region Movement Methods

        private void UpdateGroundCheck()
        {
            if (_physicsSystem == null || _capsuleCollider == null || Transform == null)
            {
                _isGrounded = false;
                return;
            }

            Vector3 capsuleBottom = Transform.Position + _capsuleCollider.Center
                                  - Vector3.Up * (_capsuleHeight * 0.5f - _capsuleRadius);

            float rayLength = _capsuleRadius + _groundCheckDistance + GROUND_CHECK_SKIN_WIDTH;

            _isGrounded = _physicsSystem.Raycast(
                capsuleBottom,
                Vector3.Down,
                rayLength,
                _groundLayers,
                out RaycastHit hit);

            // hit.Normal available if needed
        }

        /// <summary>
        /// Physics-based character movement using the RigidBody.
        /// </summary>
        private void UpdateMovementPhysics(float deltaTime)
        {
            if (Transform == null || _rigidBody == null)
                return;

            GetMovementBasis(out Vector3 forward, out Vector3 right);

            Vector3 inputDirection = Vector3.Zero;

            if (_currentKeyboard.IsKeyDown(_forwardKey))
                inputDirection += forward;
            if (_currentKeyboard.IsKeyDown(_backwardKey))
                inputDirection -= forward;
            if (_currentKeyboard.IsKeyDown(_rightKey))
                inputDirection += right;
            if (_currentKeyboard.IsKeyDown(_leftKey))
                inputDirection -= right;

            float targetSpeed = _moveSpeed;
            if (_currentKeyboard.IsKeyDown(_sprintKey))
                targetSpeed *= _sprintMultiplier;

            Vector3 currentVelocity = _rigidBody.LinearVelocity;
            Vector3 horizontalVelocity = new Vector3(currentVelocity.X, 0f, currentVelocity.Z);

            Vector3 desiredHorizontalVelocity = Vector3.Zero;
            bool hasInput = inputDirection.LengthSquared() > 0f;

            if (hasInput)
            {
                inputDirection.Normalize();
                desiredHorizontalVelocity = inputDirection * targetSpeed;
            }

            Vector3 newHorizontalVelocity;

            if (_isGrounded)
            {
                if (hasInput)
                {
                    newHorizontalVelocity = desiredHorizontalVelocity;
                }
                else
                {
                    float frictionFactor = 1f - _groundFriction * deltaTime;
                    frictionFactor = MathHelper.Clamp(frictionFactor, 0f, 1f);
                    newHorizontalVelocity = horizontalVelocity * frictionFactor;

                    if (newHorizontalVelocity.LengthSquared() < 0.0001f)
                        newHorizontalVelocity = Vector3.Zero;
                }
            }
            else
            {
                if (hasInput)
                {
                    Vector3 velocityDelta = desiredHorizontalVelocity - horizontalVelocity;
                    Vector3 airControlVelocity = horizontalVelocity
                        + velocityDelta * _airControlFactor * deltaTime * 10f;

                    if (airControlVelocity.LengthSquared() > (targetSpeed * targetSpeed))
                    {
                        airControlVelocity.Normalize();
                        airControlVelocity *= targetSpeed;
                    }

                    newHorizontalVelocity = airControlVelocity;
                }
                else
                {
                    newHorizontalVelocity = horizontalVelocity;
                }
            }

            Vector3 finalVelocity = new Vector3(
                newHorizontalVelocity.X,
                currentVelocity.Y,
                newHorizontalVelocity.Z);

            _rigidBody.LinearVelocity = finalVelocity;

            if (_isGrounded && IsKeyPressed(_jumpKey))
            {
                Vector3 jumpVelocity = _rigidBody.LinearVelocity;
                jumpVelocity.Y = 0f;
                jumpVelocity.Y += _jumpImpulse;
                _rigidBody.LinearVelocity = jumpVelocity;
                _isGrounded = false;
            }
        }

        /// <summary>
        /// Direct transform movement (fly camera mode) using WASD.
        /// Ignores physics and simply translates the Transform.
        /// </summary>
        private void UpdateMovementTransform(float deltaTime)
        {
            if (Transform == null)
                return;

            GetMovementBasis(out Vector3 forward, out Vector3 right);

            Vector3 inputDirection = Vector3.Zero;

            if (_currentKeyboard.IsKeyDown(_forwardKey))
                inputDirection += forward;
            if (_currentKeyboard.IsKeyDown(_backwardKey))
                inputDirection -= forward;
            if (_currentKeyboard.IsKeyDown(_rightKey))
                inputDirection += right;
            if (_currentKeyboard.IsKeyDown(_leftKey))
                inputDirection -= right;

            if (inputDirection.LengthSquared() <= 0f)
                return;

            inputDirection.Normalize();

            float speed = _moveSpeed;
            if (_currentKeyboard.IsKeyDown(_sprintKey))
                speed *= _sprintMultiplier;

            Vector3 displacement = inputDirection * speed * deltaTime;

            // World-space translation so you can fly freely
            Transform.TranslateBy(displacement, true);
        }

        private void UpdateRotationFreeze()
        {
            if (_rigidBody == null)
                return;

            if (_freezeRotation)
                _rigidBody.AngularVelocity = Vector3.Zero;
        }

        private void GetMovementBasis(out Vector3 forward, out Vector3 right)
        {
            forward = Vector3.Forward;
            right = Vector3.Right;

            if (Transform == null)
                return;

            forward = Transform.Forward;
            right = Transform.Right;

            forward.Y = 0f;
            right.Y = 0f;

            if (forward.LengthSquared() > 1e-6f)
                forward.Normalize();
            else
                forward = Vector3.Forward;

            if (right.LengthSquared() > 1e-6f)
                right.Normalize();
            else
                right = Vector3.Right;
        }

        private bool IsKeyPressed(Keys key)
        {
            return _currentKeyboard.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
        }

        #endregion

        #region Public Methods

        public void Teleport(Vector3 position)
        {
            if (Transform == null)
                return;

            Vector3 delta = position - Transform.Position;
            Transform.TranslateBy(delta, true);
        }

        public void AddImpulse(Vector3 impulse)
        {
            if (_rigidBody == null)
                return;

            _rigidBody.AddImpulse(impulse);
        }

        public void AddForce(Vector3 force)
        {
            if (_rigidBody == null)
                return;

            _rigidBody.AddForce(force);
        }

        public void SetVerticalVelocity(float verticalVelocity)
        {
            if (_rigidBody == null)
                return;

            Vector3 velocity = _rigidBody.LinearVelocity;
            velocity.Y = verticalVelocity;
            _rigidBody.LinearVelocity = velocity;
        }

        public void AddVerticalImpulse(float verticalImpulse)
        {
            if (_rigidBody == null)
                return;

            Vector3 velocity = _rigidBody.LinearVelocity;
            velocity.Y += verticalImpulse;
            _rigidBody.LinearVelocity = velocity;
        }

        #endregion

        #region Housekeeping Methods

        public override string ToString()
        {
            return $"CollidableFirstPersonController(Grounded={_isGrounded}, Speed={_moveSpeed:F1}, Mass={_mass:F0}kg, Physics={_usePhysicsMovement})";
        }

        #endregion
    }
}
