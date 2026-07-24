using BepuPhysics;
using BepuPhysics.Collidables;
using GDEngine.Core.Entities;
using GDEngine.Core.Systems;
using GDEngine.Core.Timing;
using GDEngine.Core.Utilities;
using Microsoft.Xna.Framework;
using System.Runtime.CompilerServices;

namespace GDEngine.Core.Components
{
    /// <summary>
    /// Determines how a RigidBody interacts with physics simulation.
    /// </summary>
    public enum BodyType : byte
    {
        /// <summary>
        /// Immovable object. Does not respond to forces or collisions.
        /// Ideal for level geometry, walls, floors. Most efficient.
        /// </summary>
        Static = 0,

        /// <summary>
        /// Animated object. Position/rotation set by code, not physics.
        /// Affects dynamic objects but is not affected by them.
        /// Use for moving platforms, doors, elevators.
        /// </summary>
        Kinematic = 1,

        /// <summary>
        /// Fully simulated object. Responds to forces, gravity, and collisions.
        /// Use for physics-driven objects like projectiles, ragdolls, debris.
        /// </summary>
        Dynamic = 2
    }

    /// <summary>
    /// Connects a GameObject to the physics simulation, enabling collision detection and response.
    /// Requires at least one Collider component on the same GameObject.
    /// </summary>
    /// <remarks>
    /// <para>
    /// RigidBody acts as the bridge between the Transform component and BepuPhysics simulation:
    /// - Static: Transform → Physics (one-way, only on creation)
    /// - Kinematic: Transform → Physics (one-way, every frame)
    /// - Dynamic: Physics → Transform (one-way, every frame after simulation)
    /// </para>
    /// <para>
    /// Performance tips:
    /// - Prefer Static over Kinematic when objects don't move
    /// - Use simple colliders (Box, Sphere, Capsule) over Mesh colliders
    /// - Disable UseGravity for objects that shouldn't fall
    /// - Set objects to sleep when inactive (automatic in BepuPhysics)
    /// </para>
    /// </remarks>
    /// <see cref="Collider"/>
    /// <see cref="PhysicsSystem"/>
    /// <see cref="Transform"/>
    public sealed class RigidBody : Component, IDisposable
    {
        #region Fields
        private PhysicsSystem? _physicsSystem;
        private Collider? _collider;

        // Body configuration
        private BodyType _bodyType = BodyType.Dynamic;
        private float _mass = 1.0f;
        private bool _useGravity = true;

        // Damping
        private float _linearDamping = 0.03f;
        private float _angularDamping = 0.03f;

        // Velocities (only meaningful for Dynamic bodies)
        private Vector3 _linearVelocity = Vector3.Zero;
        private Vector3 _angularVelocity = Vector3.Zero;

        // BepuPhysics handles
        private BodyHandle? _bodyHandle;
        private StaticHandle? _staticHandle;

        // Synchronization control
        private bool _suppressTransformSync = false;

        private bool _disposed = false;
        private float maximumSpeculativeMargin = 0.005f;
        #endregion

        #region Properties
        /// <summary>
        /// Type of physics body (Static, Kinematic, or Dynamic).
        /// Changing this at runtime will recreate the physics body and update tracking lists.
        /// </summary>
        public BodyType BodyType
        {
            get => _bodyType;
            set
            {
                if (_bodyType == value)
                    return;

                var oldType = _bodyType;
                _bodyType = value;

                if (_physicsSystem != null)
                    _physicsSystem.NotifyBodyTypeChanged(this, oldType, _bodyType);

                // If already in simulation, recreate the body to match new type
                if (_physicsSystem != null && (_bodyHandle.HasValue || _staticHandle.HasValue))
                {
                    RemoveFromSimulation();
                    AddToSimulation();
                }
            }
        }

        /// <summary>
        /// Mass of the body in kilograms. Only affects Dynamic bodies.
        /// </summary>
        public float Mass
        {
            get => _mass;
            set
            {
                _mass = Math.Max(0.001f, value); // Prevent zero/negative mass

                // Update inertia if already in simulation as dynamic body
                if (_bodyType == BodyType.Dynamic && _bodyHandle.HasValue && _physicsSystem != null)
                    UpdateInertia();
            }
        }

        /// <summary>
        /// Whether this body is affected by global gravity. Only applies to Dynamic bodies.
        /// </summary>
        public bool UseGravity
        {
            get => _useGravity;
            set => _useGravity = value;
        }

        /// <summary>
        /// Linear velocity damping factor (0 = no damping, 1 = immediate stop).
        /// Simulates air resistance. Only affects Dynamic bodies.
        /// </summary>
        public float LinearDamping
        {
            get => _linearDamping;
            set => _linearDamping = Math.Clamp(value, 0f, 1f);
        }

        /// <summary>
        /// Angular velocity damping factor (0 = no damping, 1 = immediate stop).
        /// Simulates rotational drag. Only affects Dynamic bodies.
        /// </summary>
        public float AngularDamping
        {
            get => _angularDamping;
            set => _angularDamping = Math.Clamp(value, 0f, 1f);
        }

        /// <summary>
        /// Current linear velocity of the body in world space (meters/second).
        /// </summary>
        public Vector3 LinearVelocity
        {
            get => _linearVelocity;
            set
            {
                _linearVelocity = value;

                if (_bodyType == BodyType.Dynamic && _bodyHandle.HasValue && _physicsSystem != null)
                {
                    var bodyRef = _physicsSystem.Simulation.Bodies.GetBodyReference(_bodyHandle.Value);
                    bodyRef.Velocity.Linear = _linearVelocity.ToBepu();
                }
            }
        }

        /// <summary>
        /// Current angular velocity of the body in world space (radians/second).
        /// </summary>
        public Vector3 AngularVelocity
        {
            get => _angularVelocity;
            set
            {
                _angularVelocity = value;

                if (_bodyType == BodyType.Dynamic && _bodyHandle.HasValue && _physicsSystem != null)
                {
                    var bodyRef = _physicsSystem.Simulation.Bodies.GetBodyReference(_bodyHandle.Value);
                    bodyRef.Velocity.Angular = _angularVelocity.ToBepu();
                }
            }
        }

        /// <summary>
        /// Handle of the dynamic/kinematic body in the simulation, if present.
        /// </summary>
        public BodyHandle? BodyHandle => _bodyHandle;

        /// <summary>
        /// Handle of the static body in the simulation, if present.
        /// </summary>
        public StaticHandle? StaticHandle => _staticHandle;
        #endregion

        #region Methods
        /// <summary>
        /// Applies a force to the body over the current frame.
        /// Only affects Dynamic bodies.
        /// </summary>
        /// <param name="force">Force vector in world space (Newtons).</param>
        public void AddForce(Vector3 force)
        {
            if (_bodyType != BodyType.Dynamic || !_bodyHandle.HasValue || _physicsSystem == null)
                return;

            var bodyRef = _physicsSystem.Simulation.Bodies.GetBodyReference(_bodyHandle.Value);

            float dt = Time.DeltaTimeSecs;
            if (dt <= 0f)
                return;

            // F = m * a → a = F / m; Δv = a * dt
            Vector3 acceleration = force / _mass;
            Vector3 deltaV = acceleration * dt;

            bodyRef.Velocity.Linear += deltaV.ToBepu();
            _linearVelocity = bodyRef.Velocity.Linear.ToXNA();
        }

        /// <summary>
        /// Applies an impulse to the body. Only affects Dynamic bodies.
        /// </summary>
        /// <param name="impulse">Impulse vector in world space (N·s).</param>
        public void AddImpulse(Vector3 impulse)
        {
            if (_bodyType != BodyType.Dynamic || !_bodyHandle.HasValue || _physicsSystem == null)
                return;

            var bodyRef = _physicsSystem.Simulation.Bodies.GetBodyReference(_bodyHandle.Value);

            Vector3 deltaV = impulse / _mass;
            bodyRef.Velocity.Linear += deltaV.ToBepu();
            _linearVelocity = bodyRef.Velocity.Linear.ToXNA();
        }

        /// <summary>
        /// Applies torque to the body. Only affects Dynamic bodies.
        /// </summary>
        /// <param name="torque">Torque vector in world space (Newton-meters).</param>
        public void AddTorque(Vector3 torque)
        {
            if (_bodyType != BodyType.Dynamic || !_bodyHandle.HasValue || _physicsSystem == null)
                return;

            // Proper torque application would use inertia tensor.
            // For now, treat torque as a direct change in angular velocity.
            var bodyRef = _physicsSystem.Simulation.Bodies.GetBodyReference(_bodyHandle.Value);
            bodyRef.Velocity.Angular += torque.ToBepu() * 0.1f;
            _angularVelocity = bodyRef.Velocity.Angular.ToXNA();
        }

        /// <summary>
        /// Synchronizes Transform to Physics (used for Kinematic bodies).
        /// Called by PhysicsSystem before simulation step.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SyncToPhysics()
        {
            if (_suppressTransformSync || Transform == null || _collider == null)
                return;

            if (_bodyType == BodyType.Kinematic && _bodyHandle.HasValue && _physicsSystem != null)
            {
                var bodyRef = _physicsSystem.Simulation.Bodies.GetBodyReference(_bodyHandle.Value);

                var rotation = Transform.Rotation;
                var position = Transform.Position;

                // Apply collider center offset in world space
                var localCenter = _collider.Center;
                var rotatedCenter = Vector3.Transform(localCenter, rotation);
                var physicsPosition = position + rotatedCenter;

                bodyRef.Pose.Position = physicsPosition.ToBepu();
                bodyRef.Pose.Orientation = rotation.ToBepu();
            }
        }

        /// <summary>
        /// Synchronizes Physics to Transform (used for Dynamic bodies).
        /// Called by PhysicsSystem after simulation step.
        /// </summary>
        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //internal void SyncFromPhysics()
        //{
        //    if (_suppressTransformSync || Transform == null || _collider == null)
        //        return;

        //    if (_bodyType == BodyType.Dynamic && _bodyHandle.HasValue && _physicsSystem != null)
        //    {
        //        var bodyRef = _physicsSystem.Simulation.Bodies.GetBodyReference(_bodyHandle.Value);

        //        float dt = Time.DeltaTimeSecs;
        //        if (dt > 0f)
        //        {
        //            // Per-body gravity toggle: cancel out global gravity when disabled.
        //            if (!_useGravity)
        //            {
        //                var g = _physicsSystem.Gravity.ToBepu();
        //                bodyRef.Velocity.Linear -= g * dt;
        //            }

        //            // Simple velocity damping
        //            if (_linearDamping > 0f || _angularDamping > 0f)
        //            {
        //                float linFactor = MathF.Max(0f, 1f - _linearDamping * dt);
        //                float angFactor = MathF.Max(0f, 1f - _angularDamping * dt);

        //                bodyRef.Velocity.Linear *= linFactor;
        //                bodyRef.Velocity.Angular *= angFactor;
        //            }
        //        }

        //        // Cache velocities for external access
        //        _linearVelocity = bodyRef.Velocity.Linear.ToXNA();
        //        _angularVelocity = bodyRef.Velocity.Angular.ToXNA();

        //        // Update Transform using collider center offset so visuals stay aligned
        //        var pose = bodyRef.Pose;
        //        var targetRotation = pose.Orientation.ToXNA();
        //        var targetPosition = pose.Position.ToXNA();

        //        var localCenter = _collider.Center;
        //        var rotatedCenter = Vector3.Transform(localCenter, targetRotation);
        //        var visualPosition = targetPosition - rotatedCenter;

        //        _suppressTransformSync = true;
        //        Transform.RotateToWorld(targetRotation);
        //        Transform.TranslateTo(visualPosition);
        //        _suppressTransformSync = false;
        //    }
        //}
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SyncFromPhysics()
        {
            if (_suppressTransformSync || Transform == null || _collider == null)
                return;

            if (_bodyType == BodyType.Dynamic && _bodyHandle.HasValue && _physicsSystem != null)
            {
                var bodyRef = _physicsSystem.Simulation.Bodies.GetBodyReference(_bodyHandle.Value);

                // Use the physics step dt, not the render dt, so behaviour is independent of frame rate.
                float dt = _physicsSystem.LastStepDt;

                if (dt > 0f)
                {
                    // Per-body gravity toggle: cancel out global gravity when disabled.
                    if (!_useGravity)
                    {
                        var g = _physicsSystem.Gravity.ToBepu();
                        bodyRef.Velocity.Linear -= g * dt;
                    }

                    // Simple velocity damping
                    if (_linearDamping > 0f || _angularDamping > 0f)
                    {
                        float linFactor = MathF.Max(0f, 1f - _linearDamping * dt);
                        float angFactor = MathF.Max(0f, 1f - _angularDamping * dt);

                        bodyRef.Velocity.Linear *= linFactor;
                        bodyRef.Velocity.Angular *= angFactor;
                    }
                }

                // Cache velocities for external access
                _linearVelocity = bodyRef.Velocity.Linear.ToXNA();
                _angularVelocity = bodyRef.Velocity.Angular.ToXNA();

                // Update Transform using collider center offset so visuals stay aligned
                var pose = bodyRef.Pose;
                var targetRotation = pose.Orientation.ToXNA();
                var physicsPositionWorld = pose.Position.ToXNA();

                // Remove collider offset to get visual origin in world space
                var localCenter = _collider.Center;
                var rotatedCenter = Vector3.Transform(localCenter, targetRotation);
                var visualWorldPosition = physicsPositionWorld - rotatedCenter;

                // Convert world position to local position if we have a parent
                Vector3 visualLocalPosition;
                if (Transform.Parent == null)
                {
                    visualLocalPosition = visualWorldPosition;
                }
                else
                {
                    var invParent = Matrix.Invert(Transform.Parent.WorldMatrix);
                    visualLocalPosition = Vector3.Transform(visualWorldPosition, invParent);
                }

                _suppressTransformSync = true;
                Transform.RotateToWorld(targetRotation);  // This handles world-to-local conversion
                Transform.TranslateTo(visualLocalPosition);  // Now correctly in local space
                _suppressTransformSync = false;
            }
        }
        #endregion

        #region Lifecycle Methods
        protected override void Awake()
        {
            if (GameObject?.Scene == null)
                return;

            _physicsSystem = GameObject.Scene.GetSystem<PhysicsSystem>();
            if (_physicsSystem == null)
                throw new InvalidOperationException(
                    "PhysicsSystem not found in scene. Add PhysicsSystem to the scene before creating RigidBody components.");

            _collider = GameObject.GetComponent<Collider>();
            if (_collider == null)
                throw new InvalidOperationException(
                    "RigidBody requires a Collider component on the same GameObject. Add a BoxCollider, SphereCollider, or other collider type.");

            _physicsSystem.RegisterBody(this);
        }

        protected override void Start()
        {
            // Add to simulation after all components are awake
            AddToSimulation();
        }

        protected override void OnDestroy()
        {
            RemoveFromSimulation();

            if (_physicsSystem != null)
            {
                _physicsSystem.UnregisterBody(this);
                _physicsSystem = null;
            }
        }
        #endregion

        #region Housekeeping Methods
        private void AddToSimulation()
        {
            if (_physicsSystem == null || _collider == null || Transform == null)
                return;

            // 1. Build shape
            TypedIndex shapeIndex = _collider.CreateShape(_physicsSystem.Simulation);

            var rotation = Transform.Rotation;
            var position = Transform.Position;

            // 2. Apply collider center offset
            var localCenter = _collider.Center;
            var rotatedCenter = Vector3.Transform(localCenter, rotation);
            var physicsPosition = position + rotatedCenter;

            var pose = new RigidPose(
                physicsPosition.ToBepu(),
                rotation.ToBepu()
            );

            // 3. Static body
            if (_bodyType == BodyType.Static)
            {
                var staticDescription = new StaticDescription(
                    pose.Position,
                    pose.Orientation,
                    shapeIndex
                );

                _staticHandle = _physicsSystem.AddStaticToSimulation(this, staticDescription);
                return;
            }

            // 4. Dynamic body
            if (_bodyType == BodyType.Dynamic)
            {
                var inertia = _collider.CalculateInertia(_mass);

                var bodyDescription = BodyDescription.CreateDynamic(
                    pose,
                    inertia,
                    new CollidableDescription(shapeIndex, maximumSpeculativeMargin),
                    new BodyActivityDescription(0.01f)
                );

                bodyDescription.Velocity = PhysicsConversions.ToBodyVelocity(_linearVelocity, _angularVelocity);

                _bodyHandle = _physicsSystem.AddBodyToSimulation(this, bodyDescription);
                return;
            }

            // 5. Kinematic body
            {
                var bodyDescription = BodyDescription.CreateKinematic(
                    pose,
                    PhysicsConversions.ToBodyVelocity(_linearVelocity, _angularVelocity),
                    new CollidableDescription(shapeIndex, maximumSpeculativeMargin),
                    new BodyActivityDescription(0.01f)
                );

                _bodyHandle = _physicsSystem.AddBodyToSimulation(this, bodyDescription);
            }
        }


        private void RemoveFromSimulation()
        {
            if (_physicsSystem == null)
                return;

            if (_bodyHandle.HasValue)
            {
                _physicsSystem.RemoveBodyFromSimulation(_bodyHandle.Value);
                _bodyHandle = null;
            }

            if (_staticHandle.HasValue)
            {
                _physicsSystem.RemoveStaticFromSimulation(_staticHandle.Value);
                _staticHandle = null;
            }
        }

        private void UpdateInertia()
        {
            if (_bodyType != BodyType.Dynamic || !_bodyHandle.HasValue || _physicsSystem == null || _collider == null)
                return;

            var bodyRef = _physicsSystem.Simulation.Bodies.GetBodyReference(_bodyHandle.Value);
            bodyRef.LocalInertia = _collider.CalculateInertia(_mass);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            RemoveFromSimulation();
            _disposed = true;
        }

        public override string ToString()
        {
            return $"RigidBody(Type={_bodyType}, Mass={_mass:F2}, HasHandle={_bodyHandle.HasValue || _staticHandle.HasValue})";
        }
        #endregion
    }
}
