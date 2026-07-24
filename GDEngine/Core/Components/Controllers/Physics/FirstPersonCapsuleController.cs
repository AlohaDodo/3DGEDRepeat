using GDEngine.Core.Entities;
using GDEngine.Core.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace GDEngine.Core.Components.Controllers.Physics
{
    /// <summary>
    /// Simple first-person capsule controller that uses a Dynamic <see cref="RigidBody"/>
    /// and <see cref="CapsuleCollider"/> for collisions and gravity.
    /// Movement is driven by WASD on the XZ-plane; jumping uses Space.
    /// Looking is handled separately by attaching a <see cref="MouseYawPitchController"/>
    /// to the camera (typically a child of this GameObject).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Recommended setup:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// Create a root "Player" <see cref="GameObject"/>. Add this controller,
    /// a <see cref="RigidBody"/> and a <see cref="CapsuleCollider"/> to it.
    /// The capsule will be kept upright (aligned with world Y).
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Create a child GameObject with a <c>Camera</c> component and
    /// <see cref="MouseYawPitchController"/>. This child handles yaw + pitch
    /// for view only; physics remains on the upright capsule root.
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <see cref="Component"/>
    /// <see cref="RigidBody"/>
    /// <see cref="CapsuleCollider"/>
    /// <see cref="MouseYawPitchController"/>
    public sealed class FirstPersonCapsuleController : Component
    {
        #region Static Fields
        #endregion

        #region Fields
        private PhysicsSystem? _physicsSystem;
        private RigidBody? _rigidBody;
        private CapsuleCollider? _capsule;

        private KeyboardState _previousKeyboard;

        // Movement configuration
        private float _moveSpeed = 8.0f;           // units per second
        private float _acceleration = 50.0f;       // units per second^2
        private float _groundFriction = 10.0f;     // higher = stop quicker when no input
        private float _jumpImpulse = 7.0f;         // vertical velocity when jumping

        // Capsule dimensions (in world units/metres)
        private float _capsuleRadius = 0.5f;
        private float _capsuleHeight = 1.8f;

        // Ground check
        private float _groundCheckDistance = 0.2f;
        private bool _isGrounded;

        // Input mapping
        private Keys _forwardKey = Keys.W;
        private Keys _backwardKey = Keys.S;
        private Keys _leftKey = Keys.A;
        private Keys _rightKey = Keys.D;
        private Keys _jumpKey = Keys.Space;

        private bool _isInputEnabled = true;
        #endregion

        #region Properties
        /// <summary>Movement speed in units per second.</summary>
        public float MoveSpeed
        {
            get
            {
                return _moveSpeed;
            }
            set
            {
                _moveSpeed = MathHelper.Max(0.0f, value);
            }
        }

        /// <summary>Horizontal acceleration in units per second squared.</summary>
        public float Acceleration
        {
            get
            {
                return _acceleration;
            }
            set
            {
                _acceleration = MathHelper.Max(0.0f, value);
            }
        }

        /// <summary>Ground friction applied when no input is pressed.</summary>
        public float GroundFriction
        {
            get
            {
                return _groundFriction;
            }
            set
            {
                _groundFriction = MathHelper.Max(0.0f, value);
            }
        }

        /// <summary>Vertical jump impulse applied when jumping.</summary>
        public float JumpImpulse
        {
            get
            {
                return _jumpImpulse;
            }
            set
            {
                _jumpImpulse = MathHelper.Max(0.0f, value);
            }
        }

        /// <summary>Capsule radius in world units.</summary>
        public float CapsuleRadius
        {
            get
            {
                return _capsuleRadius;
            }
            set
            {
                _capsuleRadius = MathHelper.Max(0.1f, value);
                UpdateCapsuleShape();
            }
        }

        /// <summary>Capsule height in world units.</summary>
        public float CapsuleHeight
        {
            get
            {
                return _capsuleHeight;
            }
            set
            {
                // Ensure height is always at least 2 * radius to remain a valid capsule.
                _capsuleHeight = MathHelper.Max(2.0f * _capsuleRadius, value);
                UpdateCapsuleShape();
            }
        }

        /// <summary>
        /// Distance below the feet used when checking if the character is grounded.
        /// </summary>
        public float GroundCheckDistance
        {
            get
            {
                return _groundCheckDistance;
            }
            set
            {
                _groundCheckDistance = MathHelper.Max(0.01f, value);
            }
        }

        /// <summary>
        /// When false, the controller ignores input and does not move.
        /// </summary>
        public bool IsInputEnabled
        {
            get
            {
                return _isInputEnabled;
            }
            set
            {
                _isInputEnabled = value;
            }
        }

        /// <summary>
        /// True when the capsule is considered on the ground (based on a raycast).
        /// </summary>
        public bool IsGrounded
        {
            get
            {
                return _isGrounded;
            }
        }
        #endregion

        #region Constructors
        #endregion

        #region Methods
        private void EnsurePhysicsComponents()
        {
            if (GameObject == null)
            {
                throw new InvalidOperationException("FirstPersonCapsuleController requires a GameObject.");
            }

            if (GameObject.Scene == null)
            {
                throw new InvalidOperationException("FirstPersonCapsuleController requires the GameObject to be in a Scene.");
            }

            _physicsSystem = GameObject.Scene.GetSystem<PhysicsSystem>();
            if (_physicsSystem == null)
            {
                throw new InvalidOperationException("FirstPersonCapsuleController requires a PhysicsSystem in the Scene.");
            }

            // IMPORTANT: Ensure CapsuleCollider exists BEFORE adding RigidBody,
            // because RigidBody.Awake() expects a Collider to already be present.

            _capsule = GameObject.GetComponent<CapsuleCollider>();
            if (_capsule == null)
            {
                _capsule = GameObject.AddComponent<CapsuleCollider>();
            }

            _capsule.IsTrigger = false;
            _capsule.Material = PhysicsMaterial.Default;
            UpdateCapsuleShape();

            _rigidBody = GameObject.GetComponent<RigidBody>();
            if (_rigidBody == null)
            {
                _rigidBody = GameObject.AddComponent<RigidBody>();
            }

            _rigidBody.BodyType = BodyType.Dynamic;
            _rigidBody.UseGravity = true;
            _rigidBody.LinearDamping = 0.0f;
            _rigidBody.AngularDamping = 0.0f;
        }


        /// <summary>
        /// Keeps the capsule collider in sync with current radius/height and
        /// positions it so that Transform.Position is at the feet.
        /// </summary>
        private void UpdateCapsuleShape()
        {
            if (_capsule == null)
            {
                return;
            }

            _capsule.Radius = _capsuleRadius;
            _capsule.Height = _capsuleHeight;

            // Position the capsule so that the bottom of the lower sphere is at y = 0
            // relative to the player origin (feet at Transform.Position).
            float centerY = _capsuleHeight * 0.5f - _capsuleRadius;
            _capsule.Center = new Vector3(0.0f, centerY, 0.0f);
        }

        /// <summary>
        /// Calculates horizontal movement basis vectors from the Transform:
        /// Forward and Right projected onto the XZ plane.
        /// </summary>
        private void GetMovementBasis(out Vector3 forward, out Vector3 right)
        {
            if (Transform == null)
            {
                forward = Vector3.Forward;
                right = Vector3.Right;
                return;
            }

            forward = Transform.Forward;
            forward.Y = 0.0f;

            if (forward.LengthSquared() < 0.0001f)
            {
                forward = Vector3.Forward;
            }

            forward.Normalize();

            right = Transform.Right;
            right.Y = 0.0f;

            if (right.LengthSquared() < 0.0001f)
            {
                right = Vector3.Right;
            }

            right.Normalize();
        }

        /// <summary>
        /// Updates the grounded state by raycasting slightly below the feet.
        /// </summary>
        private void UpdateGrounded()
        {
            _isGrounded = false;

            if (_physicsSystem == null || Transform == null)
            {
                return;
            }

            // Cast a short ray straight down from just above the feet.
            Vector3 origin = Transform.Position + Vector3.Up * 0.05f;
            Vector3 direction = -Vector3.Up;

            if (_physicsSystem.Raycast(origin, direction, _groundCheckDistance, out RaycastHit hit))
            {
                _isGrounded = true;
            }
        }

        /// <summary>
        /// Applies WASD movement and jumping by manipulating the RigidBody's
        /// horizontal linear velocity. Vertical velocity (gravity/jump) is preserved.
        /// </summary>
        private void UpdateMovement(float deltaTime, KeyboardState currentKeyboard)
        {
            if (Transform == null || _rigidBody == null)
            {
                return;
            }

            GetMovementBasis(out Vector3 forward, out Vector3 right);

            Vector3 inputDirection = Vector3.Zero;

            if (currentKeyboard.IsKeyDown(_forwardKey))
            {
                inputDirection += forward;
            }

            if (currentKeyboard.IsKeyDown(_backwardKey))
            {
                inputDirection -= forward;
            }

            if (currentKeyboard.IsKeyDown(_rightKey))
            {
                inputDirection += right;
            }

            if (currentKeyboard.IsKeyDown(_leftKey))
            {
                inputDirection -= right;
            }

            bool hasInput = inputDirection.LengthSquared() > 0.0001f;
            if (hasInput)
            {
                inputDirection.Normalize();
            }

            Vector3 velocity = _rigidBody.LinearVelocity;
            Vector3 horizontalVelocity = new Vector3(velocity.X, 0.0f, velocity.Z);
            Vector3 desiredHorizontalVelocity = hasInput
                ? inputDirection * _moveSpeed
                : Vector3.Zero;

            if (hasInput)
            {
                // Accelerate towards the desired velocity.
                Vector3 velocityDelta = desiredHorizontalVelocity - horizontalVelocity;

                float maxChange = _acceleration * deltaTime;
                float deltaLength = velocityDelta.Length();

                if (deltaLength > maxChange && deltaLength > 0.0f)
                {
                    velocityDelta *= maxChange / deltaLength;
                }

                horizontalVelocity += velocityDelta;
            }
            else
            {
                // Apply simple friction when no input is pressed.
                if (_groundFriction > 0.0f && _isGrounded)
                {
                    float frictionFactor = 1.0f - _groundFriction * deltaTime;
                    frictionFactor = MathHelper.Clamp(frictionFactor, 0.0f, 1.0f);
                    horizontalVelocity *= frictionFactor;

                    if (horizontalVelocity.LengthSquared() < 0.0001f)
                    {
                        horizontalVelocity = Vector3.Zero;
                    }
                }
            }

            // Write back horizontal velocity, preserving vertical (gravity/jump).
            velocity.X = horizontalVelocity.X;
            velocity.Z = horizontalVelocity.Z;

            // Jump (only on key press edge and when grounded).
            bool jumpPressed = currentKeyboard.IsKeyDown(_jumpKey) &&
                               _previousKeyboard.IsKeyUp(_jumpKey);

            if (jumpPressed && _isGrounded)
            {
                velocity.Y = _jumpImpulse;
            }

            _rigidBody.LinearVelocity = velocity;
        }

        /// <summary>
        /// Constrains the capsule to stay upright by removing any X/Z angular velocity.
        /// The capsule's long axis will remain aligned with world Y.
        /// </summary>
        private void ConstrainUpright()
        {
            if (_rigidBody == null)
            {
                return;
            }

            Vector3 angularVelocity = _rigidBody.AngularVelocity;

            // Only allow spin around Y if any; remove pitch/roll.
            angularVelocity.X = 0.0f;
            angularVelocity.Z = 0.0f;

            _rigidBody.AngularVelocity = angularVelocity;
        }
        #endregion

        #region Lifecycle Methods
        /// <inheritdoc/>
        protected override void Awake()
        {
            EnsurePhysicsComponents();
        }

        /// <inheritdoc/>
        protected override void Start()
        {
            _previousKeyboard = Keyboard.GetState();
        }

        /// <inheritdoc/>
        protected override void Update(float deltaTime)
        {
            if (_isInputEnabled == false)
            {
                return;
            }

            KeyboardState currentKeyboard = Keyboard.GetState();

            UpdateGrounded();
            UpdateMovement(deltaTime, currentKeyboard);
            ConstrainUpright();

            _previousKeyboard = currentKeyboard;
        }
        #endregion

        #region Housekeeping Methods
        #endregion
    }
}
