using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Memory;
using GDEngine.Core.Components;
using GDEngine.Core.Entities;
using GDEngine.Core.Enums;
using GDEngine.Core.Events;
using GDEngine.Core.Rendering.Base;
using GDEngine.Core.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MathHelper = Microsoft.Xna.Framework.MathHelper;
using Matrix = Microsoft.Xna.Framework.Matrix;

namespace GDEngine.Core.Systems
{
    /// <summary>
    /// The main physics management system using BepuPhysics v2.
    /// Handles simulation stepping, body registration, syncing Transforms,
    /// collision callbacks, gravity, and runtime body-type switching.
    /// </summary>
    public sealed class PhysicsSystem : PausableSystemBase, IDisposable
    {
        #region Fields

        public Simulation _simulation = null!;

        private BufferPool _bufferPool = null!;
        private ThreadDispatcher _threadDispatcher = null!;

        private readonly List<RigidBody> _dynamicBodies = new List<RigidBody>(256);
        private readonly List<RigidBody> _kinematicBodies = new List<RigidBody>(64);
        private readonly List<RigidBody> _staticBodies = new List<RigidBody>(512);

        internal readonly Dictionary<BodyHandle, RigidBody> _handleToComponent =
            new Dictionary<BodyHandle, RigidBody>(512);

        // Map statics back to RigidBody for raycasts / triggers.
        internal readonly Dictionary<StaticHandle, RigidBody> _staticHandleToComponent =
            new Dictionary<StaticHandle, RigidBody>(512);

        // Remember the last physics step dt so bodies don’t depend on render dt.
        private float _lastStepDt = 0f;

        private Vector3 _gravity = new Vector3(0, -9.81f, 0);

        private int _velocityIterations = 8;
        private int _substepCount = 1;

        private float _fixedTimestep = -1f; // -1 = variable timestep
        private float _accumulator = 0f;

        private bool _disposed = false;

        #endregion


        #region Properties

        /// <summary>
        /// Global gravity. Runtime changes correctly update the simulation.
        /// </summary>
        public Vector3 Gravity
        {
            get => _gravity;
            set => _gravity = value;
        }

        /// <summary>
        /// The timestep (in seconds) used in the most recent physics step.
        /// </summary>
        public float LastStepDt
        {
            get => _lastStepDt;
        }

        public int VelocityIterations
        {
            get => _velocityIterations;
            set => _velocityIterations = Math.Max(1, value);
        }

        public int SubstepCount
        {
            get => _substepCount;
            set => _substepCount = Math.Max(1, value);
        }

        public float FixedTimestep
        {
            get => _fixedTimestep;
            set => _fixedTimestep = value;
        }

        public Simulation Simulation => _simulation;

        #endregion


        #region Constructor

        public PhysicsSystem(int order = 1000)
            : base(FrameLifecycle.LateUpdate, order)
        {
            // Physics should not step when the game is paused.
            PauseMode = PauseMode.Update;
        }

        #endregion


        #region Registration Methods

        internal void RegisterBody(RigidBody rb)
        {
            switch (rb.BodyType)
            {
                case BodyType.Dynamic:
                    _dynamicBodies.Add(rb);
                    break;

                case BodyType.Kinematic:
                    _kinematicBodies.Add(rb);
                    break;

                case BodyType.Static:
                    _staticBodies.Add(rb);
                    break;
            }
        }

        internal void UnregisterBody(RigidBody rb)
        {
            _dynamicBodies.Remove(rb);
            _kinematicBodies.Remove(rb);
            _staticBodies.Remove(rb);

            if (rb.BodyHandle.HasValue)
                _handleToComponent.Remove(rb.BodyHandle.Value);
        }

        /// <summary>
        /// Called when a RigidBody switches Static/Dynamic/Kinematic at runtime.
        /// Ensures that bookkeeping in PhysicsSystem stays consistent.
        ///</summary>
        internal void NotifyBodyTypeChanged(RigidBody rb, BodyType oldType, BodyType newType)
        {
            _dynamicBodies.Remove(rb);
            _kinematicBodies.Remove(rb);
            _staticBodies.Remove(rb);

            switch (newType)
            {
                case BodyType.Dynamic:
                    _dynamicBodies.Add(rb);
                    break;

                case BodyType.Kinematic:
                    _kinematicBodies.Add(rb);
                    break;

                case BodyType.Static:
                    _staticBodies.Add(rb);
                    break;
            }
        }

        #endregion


        #region Simulation Add/Remove

        internal BodyHandle AddBodyToSimulation(RigidBody rb, BodyDescription desc)
        {
            var handle = _simulation.Bodies.Add(desc);
            _handleToComponent[handle] = rb;
            return handle;
        }

        internal StaticHandle AddStaticToSimulation(RigidBody rb, StaticDescription desc)
        {
            var handle = _simulation.Statics.Add(desc);
            _staticHandleToComponent[handle] = rb;
            return handle;
        }

        internal void RemoveBodyFromSimulation(BodyHandle handle)
        {
            if (_simulation.Bodies.BodyExists(handle))
            {
                _simulation.Bodies.Remove(handle);
                _handleToComponent.Remove(handle);
            }
        }

        internal void RemoveStaticFromSimulation(StaticHandle handle)
        {
            if (_simulation.Statics.StaticExists(handle))
                _simulation.Statics.Remove(handle);

            _staticHandleToComponent.Remove(handle);
        }

        #endregion


        #region Lifecycle: OnAdded / Update / OnRemoved

        protected override void OnAdded()
        {
            _bufferPool = new BufferPool();
            _threadDispatcher = new ThreadDispatcher(Environment.ProcessorCount);

            var narrow = new NarrowPhaseCallbacks(this);
            var integrator = new PoseIntegratorCallbacks(this);   // <— pass reference to system
            var solve = new SolveDescription(_velocityIterations, _substepCount);

            _simulation = Simulation.Create(
                _bufferPool,
                narrow,
                integrator,
                solve
            );
        }

        protected override void OnUpdate(float dt)
        {
            if (!Enabled)
                return;

            if (dt <= 0f)
                return;

            // CRITICAL FIX: Force all transforms to recalculate BEFORE physics step
            // This ensures physics sees the latest transform state
            if (Scene != null)
            {
                foreach (var go in Scene.GameObjects)
                {
                    if (go.Enabled && go.Transform != null)
                        _ = go.Transform.WorldMatrix;
                }
            }

            if (_fixedTimestep > 0f)
            {
                _accumulator += dt;

                while (_accumulator >= _fixedTimestep)
                {
                    Step(_fixedTimestep);
                    _accumulator -= _fixedTimestep;
                }
            }
            else
            {
                Step(dt);
            }
        }

        //public override void Update(float dt)
        //{
        //    if (!Enabled)
        //        return;

        //    if (dt <= 0f)
        //        return;

        //    // CRITICAL FIX: Force all transforms to recalculate BEFORE physics step
        //    // This ensures physics sees the latest transform state
        //    if (Scene != null)
        //    {
        //        foreach (var go in Scene.GameObjects)
        //        {
        //            if (go.Enabled && go.Transform != null)
        //            {
        //                // Simply accessing WorldMatrix triggers recalculation if dirty
        //                _ = go.Transform.WorldMatrix;
        //            }
        //        }
        //    }

        //    if (_fixedTimestep > 0f)
        //    {
        //        _accumulator += dt;

        //        while (_accumulator >= _fixedTimestep)
        //        {
        //            Step(_fixedTimestep);
        //            _accumulator -= _fixedTimestep;
        //        }
        //    }
        //    else
        //    {
        //        Step(dt);
        //    }
        //}


        protected override void OnRemoved()
        {
            Dispose();
        }

        #endregion


        #region Simulation Step

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private void Step(float dt)
        //{
        //    // 1. Sync TRANSFORM → PHYSICS for kinematics
        //    SyncKinematics();

        //    // 2. Run substeps
        //    float sdt = dt / _substepCount;
        //    for (int i = 0; i < _substepCount; i++)
        //        _simulation.Timestep(sdt, _threadDispatcher);

        //    // 3. Sync PHYSICS → TRANSFORM for dynamics
        //    SyncDynamics();
        //}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Step(float dt)
        {
            _lastStepDt = dt;

            // 1. Sync TRANSFORM → PHYSICS for kinematics
            SyncKinematics();

            // 2. Step the simulation
            _simulation.Timestep(dt, _threadDispatcher);

            // 3. Sync PHYSICS → TRANSFORM for dynamics
            SyncDynamics();
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SyncKinematics()
        {
            foreach (var rb in _kinematicBodies)
            {
                if (rb.Enabled)
                    rb.SyncToPhysics();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SyncDynamics()
        {
            foreach (var rb in _dynamicBodies)
            {
                if (rb.Enabled)
                    rb.SyncFromPhysics();
            }
        }

        #endregion


        #region Raycast

        /// <summary>
        /// Convenience overload that raycasts without including trigger colliders
        /// and without any explicit layer filtering.
        /// </summary>
        public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit hit)
        {
            // No layer filter => LayerMask.All
            return Raycast(origin, direction, maxDistance, LayerMask.All, out hit, false);
        }

        /// <summary>
        /// Convenience overload that raycasts with a layer filter but ignores triggers.
        /// </summary>
        public bool Raycast(
            Vector3 origin,
            Vector3 direction,
            float maxDistance,
            LayerMask layerMask,
            out RaycastHit hit)
        {
            return Raycast(origin, direction, maxDistance, layerMask, out hit, false);
        }

        /// <summary>
        /// Casts a ray through the physics simulation and returns the first hit.
        /// </summary>
        /// <param name="origin">World space origin of the ray.</param>
        /// <param name="direction">World space direction (need not be normalized).</param>
        /// <param name="maxDistance">Maximum distance to check.</param>
        /// <param name="layerMask">Layer mask to filter potential hits.</param>
        /// <param name="hit">Output hit information.</param>
        /// <param name="hitTriggers">If false, trigger colliders are ignored.</param>
        public bool Raycast(
            Vector3 origin,
            Vector3 direction,
            float maxDistance,
            LayerMask layerMask,
            out RaycastHit hit,
            bool hitTriggers = false)
        {
            hit = default;

            if (_simulation == null)
                return false;

            if (direction.LengthSquared() <= float.Epsilon)
                return false;

            direction.Normalize();

            var rayOrigin = origin.ToBepu();
            var rayDirection = direction.ToBepu();

            var handler = new RayHitHandler(this, hitTriggers, layerMask);

            _simulation.RayCast(rayOrigin, rayDirection, maxDistance, ref handler);

            if (handler.ClosestHit.HasValue)
            {
                hit = handler.ClosestHit.Value;
                return true;
            }

            return false;
        }

        #endregion



        #region Disposal

        public void Dispose()
        {
            if (_disposed)
                return;

            _dynamicBodies.Clear();
            _kinematicBodies.Clear();
            _staticBodies.Clear();
            _handleToComponent.Clear();

            _simulation?.Dispose();
            _threadDispatcher?.Dispose();
            _bufferPool?.Clear();

            _disposed = true;
        }

        #endregion


        #region NarrowPhase & PoseIntegrator Structs

        //private struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
        //{
        //    private PhysicsSystem _system;

        //    public NarrowPhaseCallbacks(PhysicsSystem sys)
        //    {
        //        _system = sys;
        //    }

        //    public void Initialize(Simulation simulation) { }

        //    public bool AllowContactGeneration(
        //        int workerIndex,
        //        CollidableReference a,
        //        CollidableReference b,
        //        ref float speculativeMargin)
        //    {
        //        speculativeMargin = 0.1f;
        //        return true;
        //    }

        //    public bool AllowContactGeneration(
        //        int workerIndex,
        //        CollidablePair pair,
        //        int childA,
        //        int childB) => true;

        //    public bool ConfigureContactManifold<TManifold>(
        //        int workerIndex,
        //        CollidablePair pair,
        //        ref TManifold manifold,
        //        out PairMaterialProperties props)
        //        where TManifold : unmanaged, IContactManifold<TManifold>
        //    {
        //        props = new PairMaterialProperties
        //        {
        //            FrictionCoefficient = 0.5f,
        //            MaximumRecoveryVelocity = 2f,
        //            SpringSettings = new SpringSettings(240, 1)
        //        };
        //        return true;
        //    }

        //    public bool ConfigureContactManifold(
        //        int workerIndex,
        //        CollidablePair pair,
        //        int childA,
        //        int childB,
        //        ref ConvexContactManifold manifold)
        //    {
        //        // This is for compound/mesh contacts with children
        //        // Still need to return true to allow the contact
        //        return true;
        //    }

        //    public void Dispose() { }
        //}

        private struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
        {
            private PhysicsSystem _system;

            public NarrowPhaseCallbacks(PhysicsSystem sys)
            {
                _system = sys;
            }

            public void Initialize(Simulation simulation) { }

            public bool AllowContactGeneration(
                int workerIndex,
                CollidableReference a,
                CollidableReference b,
                ref float speculativeMargin)
            {
                // DIAGNOSTIC: Log when contacts are attempted
                Console.WriteLine($"[Physics] Contact attempted between {a.Mobility} and {b.Mobility}");
                return true;
            }

            public bool AllowContactGeneration(
                int workerIndex,
                CollidablePair pair,
                int childA,
                int childB)
            {
                return true;
            }

            //public bool ConfigureContactManifold<TManifold>(
            //    int workerIndex,
            //    CollidablePair pair,
            //    ref TManifold manifold,
            //    out PairMaterialProperties props)
            //    where TManifold : unmanaged, IContactManifold<TManifold>
            //{

            //    props = new PairMaterialProperties
            //    {
            //        FrictionCoefficient = 0.5f,
            //        MaximumRecoveryVelocity = 2f,
            //        SpringSettings = new SpringSettings(30, 1)  // Use stiff springs
            //    };

            //    return true;
            //}

            public bool ConfigureContactManifold<TManifold>(
                int workerIndex,
                CollidablePair pair,
                ref TManifold manifold,
                out PairMaterialProperties props)
                where TManifold : unmanaged, IContactManifold<TManifold>
            {
                // Default material settings
                props = new PairMaterialProperties
                {
                    FrictionCoefficient = 0.5f,
                    MaximumRecoveryVelocity = 2f,
                    SpringSettings = new SpringSettings(30, 1)
                };

                // Look up RigidBody instances for both collidables (static or dynamic/kinematic).
                RigidBody? rbA = null;
                RigidBody? rbB = null;

                var a = pair.A;
                var b = pair.B;

                if (a.Mobility == CollidableMobility.Static)
                    _system._staticHandleToComponent.TryGetValue(a.StaticHandle, out rbA);
                else
                    _system._handleToComponent.TryGetValue(a.BodyHandle, out rbA);

                if (b.Mobility == CollidableMobility.Static)
                    _system._staticHandleToComponent.TryGetValue(b.StaticHandle, out rbB);
                else
                    _system._handleToComponent.TryGetValue(b.BodyHandle, out rbB);

                if (rbA == null || rbB == null)
                    return true;

                var colliderA = rbA.GameObject?.GetComponent<Collider>();
                var colliderB = rbB.GameObject?.GetComponent<Collider>();

                bool aIsTrigger = colliderA?.IsTrigger == true;
                bool bIsTrigger = colliderB?.IsTrigger == true;
                bool isTriggerPair = aIsTrigger || bIsTrigger;

                // Publish via EventBus (if available)
                var scene = _system.Scene;
                var context = scene?.Context;
                var bus = context?.Events;

                if (bus != null)
                {
                    if (isTriggerPair)
                    {
                        var trigger = aIsTrigger ? rbA : rbB;
                        var other = trigger == rbA ? rbB! : rbA!;
                        bus.Post(new TriggerEvent(trigger!, other));
                    }
                    else
                    {
                        bus.Post(new CollisionEvent(rbA, rbB));
                    }
                }

                // For trigger pairs, disable physical response by zeroing material.
                if (isTriggerPair)
                {
                    props.FrictionCoefficient = 0f;
                    props.MaximumRecoveryVelocity = 0f;
                    props.SpringSettings = new SpringSettings(0f, 1f);
                }

                return true;
            }


            public bool ConfigureContactManifold(
                int workerIndex,
                CollidablePair pair,
                int childA,
                int childB,
                ref ConvexContactManifold manifold) => true;

            public void Dispose() { }
        }


        private struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
        {
            private readonly PhysicsSystem _system;

            public PoseIntegratorCallbacks(PhysicsSystem system)
            {
                _system = system;
            }

            public readonly AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;
            public readonly bool AllowSubstepsForUnconstrainedBodies => false;
            public readonly bool IntegrateVelocityForKinematics => false;

            public void Initialize(Simulation simulation) { }

            public void PrepareForIntegration(float dt) { }

            // Gravity is read directly from PhysicsSystem every frame
            public void IntegrateVelocity(
                System.Numerics.Vector<int> bodyIndices,
                Vector3Wide position,
                QuaternionWide orientation,
                BodyInertiaWide localInertia,
                System.Numerics.Vector<int> integrationMask,
                int workerIndex,
                System.Numerics.Vector<float> dt,
                ref BodyVelocityWide velocity)
            {
                var g = _system.Gravity.ToBepu();

                Vector3Wide gravityWide;
                Vector3Wide.Broadcast(g, out gravityWide);

                velocity.Linear.X += gravityWide.X * dt;
                velocity.Linear.Y += gravityWide.Y * dt;
                velocity.Linear.Z += gravityWide.Z * dt;
            }
        }

        /// <summary>
        /// Casts a ray from the camera through a screen position.
        /// </summary>
        public bool RaycastFromScreen(
            Camera camera,
            float screenX,
            float screenY,
            float maxDistance,
            out RaycastHit hit,
            bool hitTriggers = false)
        {
            return RaycastFromScreen(camera, screenX, screenY, maxDistance, LayerMask.All, out hit, hitTriggers);
        }

        /// <summary>
        /// Casts a ray from the camera through a screen position, filtered by layer mask.
        /// </summary>
        public bool RaycastFromScreen(
            Camera camera,
            float screenX,
            float screenY,
            float maxDistance,
            LayerMask layerMask,
            out RaycastHit hit,
            bool hitTriggers = false)
        {
            hit = default;

            if (camera == null || Scene == null || Scene.Context == null)
                return false;

            var viewport = Scene.Context.GraphicsDevice.Viewport;

            // Unproject screen position to world-space near/far points
            var nearWorld = viewport.Unproject(
                new Vector3(screenX, screenY, 0f),
                camera.Projection,
                camera.View,
                Matrix.Identity);

            var farWorld = viewport.Unproject(
                new Vector3(screenX, screenY, 1f),
                camera.Projection,
                camera.View,
                Matrix.Identity);

            var origin = nearWorld;
            var direction = farWorld - nearWorld;

            if (direction.LengthSquared() <= float.Epsilon)
                return false;

            direction.Normalize();

            return Raycast(origin, direction, maxDistance, layerMask, out hit, hitTriggers);
        }

        /// <summary>
        /// Casts a ray from the camera through the current mouse cursor position.
        /// </summary>
        public bool RaycastFromMouse(
            Camera camera,
            Microsoft.Xna.Framework.Input.MouseState mouseState,
            float maxDistance,
            out RaycastHit hit,
            bool hitTriggers = false)
        {
            return RaycastFromScreen(camera, mouseState.X, mouseState.Y, maxDistance, LayerMask.All, out hit, hitTriggers);
        }

        /// <summary>
        /// Casts a ray from the camera through the current mouse cursor position, filtered by layer mask.
        /// </summary>
        public bool RaycastFromMouse(
            Camera camera,
            Microsoft.Xna.Framework.Input.MouseState mouseState,
            float maxDistance,
            LayerMask layerMask,
            out RaycastHit hit,
            bool hitTriggers = false)
        {
            return RaycastFromScreen(camera, mouseState.X, mouseState.Y, maxDistance, layerMask, out hit, hitTriggers);
        }

        internal bool RaycastFromScreen(Camera camera, float x, float y, object maxDistance, object hitMask, out RaycastHit hitInfo, object hitTriggers)
        {
            throw new NotImplementedException();
        }

        #endregion
    }


    #region Raycast Support

    /// <summary>
    /// Ray hit handler for BepuPhysics v2 raycasts.
    /// Handles trigger filtering and LayerMask filtering.
    /// </summary>
    public struct RayHitHandler : IRayHitHandler
    {
        public RaycastHit? ClosestHit;
        public float ClosestT;

        private readonly PhysicsSystem _system;
        private readonly bool _hitTriggers;
        private readonly LayerMask _layerMask;

        public RayHitHandler(PhysicsSystem system, bool hitTriggers, LayerMask layerMask)
        {
            _system = system;
            _hitTriggers = hitTriggers;
            _layerMask = layerMask;

            ClosestHit = null;
            ClosestT = float.MaxValue;
        }

        public bool AllowTest(CollidableReference collidable)
        {
            return AllowTest(collidable, 0);
        }

        public bool AllowTest(CollidableReference collidable, int childIndex)
        {
            // Look up RigidBody (if any) for trigger and layer filtering.
            RigidBody? rb = null;

            if (collidable.Mobility == CollidableMobility.Static)
            {
                _system._staticHandleToComponent.TryGetValue(collidable.StaticHandle, out rb);
            }
            else
            {
                _system._handleToComponent.TryGetValue(collidable.BodyHandle, out rb);
            }

            if (rb != null)
            {
                var go = rb.GameObject;
                var collider = go?.GetComponent<Collider>();

                // Trigger filter
                if (collider != null && collider.IsTrigger && !_hitTriggers)
                    return false;

                // Layer filter: if a specific mask is given, skip bodies whose
                // layer does not intersect with it.
                if (_layerMask != LayerMask.All && go != null)
                {
                    if ((go.Layer & _layerMask) == 0)
                        return false;
                }
            }

            return true;
        }

        public void OnRayHit(
            in RayData ray,
            ref float maximumT,
            float t,
            in System.Numerics.Vector3 normal,
            CollidableReference collidable,
            int childIndex)
        {
            if (t >= maximumT)
                return;

            // Compute hit point in world space
            var hitPoint = ray.Origin + ray.Direction * t;

            RigidBody? rb = null;
            if (collidable.Mobility == CollidableMobility.Static)
            {
                _system._staticHandleToComponent.TryGetValue(collidable.StaticHandle, out rb);
            }
            else
            {
                _system._handleToComponent.TryGetValue(collidable.BodyHandle, out rb);
            }

            var hit = new RaycastHit
            {
                Body = rb,
                Point = hitPoint.ToXNA(),
                Normal = normal.ToXNA(),
                Distance = t
            };

            ClosestHit = hit;
            ClosestT = t;
            maximumT = t;
        }
    }

    /// <summary>
    /// Result of a raycast query.
    /// </summary>
    public struct RaycastHit
    {
        public RigidBody? Body;
        public Vector3 Point;
        public Vector3 Normal;
        public float Distance;

        public override string ToString()
        {
            var bodyName = Body?.GameObject?.Name ?? "null";
            return $"RaycastHit(Body={bodyName}, Point={Point}, Distance={Distance:F2})";
        }
    }

    #endregion

    /// <summary>
    /// Renders wireframe debug visualization for physics colliders in PostRender.
    /// Shows boxes, spheres, and capsules as colored wireframes to help debug physics issues.
    /// </summary>
    /// <remarks>
    /// Color coding:
    /// - Green: Static bodies (immovable)
    /// - Blue: Kinematic bodies (animated)
    /// - Yellow: Dynamic bodies (physics-driven)
    /// - Red: Triggers (no collision response)
    /// </remarks>
    public sealed class PhysicsDebugSystem : PausableSystemBase
    {
        #region Fields
        private Scene _scene = null!;
        private GraphicsDevice _device = null!;
        private BasicEffect _effect = null!;

        // Wireframe primitives cache
        private VertexPositionColor[] _boxVertices = null!;
        private short[] _boxIndices = null!;
        private VertexPositionColor[] _sphereVertices = null!;
        private short[] _sphereIndices = null!;

        // Settings
        private bool _enabled = true;
        private Color _staticColor = Color.Green;
        private Color _kinematicColor = Color.Blue;
        private Color _dynamicColor = Color.Yellow;
        private Color _triggerColor = Color.Red;

        // Sphere resolution
        private const int SphereSegments = 16;
        private const int SphereRings = 8;
        private bool _useTransformScaleForDebug = false;
        #endregion

        #region Properties
        /// <summary>
        /// When true, multiplies collider dimensions by Transform.Scale when drawing.
        /// This is purely visual and does NOT change physics.
        /// </summary>
        public bool UseTransformScaleForDebug
        {
            get => _useTransformScaleForDebug;
            set => _useTransformScaleForDebug = value;
        }

        /// <summary>
        /// Color for static bodies (default: Green).
        /// </summary>
        public Color StaticColor
        {
            get => _staticColor;
            set => _staticColor = value;
        }

        /// <summary>
        /// Color for kinematic bodies (default: Blue).
        /// </summary>
        public Color KinematicColor
        {
            get => _kinematicColor;
            set => _kinematicColor = value;
        }

        /// <summary>
        /// Color for dynamic bodies (default: Yellow).
        /// </summary>
        public Color DynamicColor
        {
            get => _dynamicColor;
            set => _dynamicColor = value;
        }

        /// <summary>
        /// Color for trigger colliders (default: Red).
        /// </summary>
        public Color TriggerColor
        {
            get => _triggerColor;
            set => _triggerColor = value;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a PhysicsDebugRenderer in PostRender lifecycle.
        /// </summary>
        public PhysicsDebugSystem(int order = 100)
            : base(FrameLifecycle.PostRender, order)
        {
            // Physics should not step when the game is paused.
            PauseMode = PauseMode.Update;

        }
        #endregion

        #region Lifecycle Methods
        protected override void OnAdded()
        {
            if (Scene == null)
                throw new InvalidOperationException("PhysicsDebugRenderer requires a Scene.");

            _scene = Scene;
            _device = _scene.Context.GraphicsDevice;

            // Create BasicEffect for wireframe rendering
            _effect = new BasicEffect(_device)
            {
                VertexColorEnabled = true,
                LightingEnabled = false
            };

            // Initialize primitive geometry
            InitializeBoxWireframe();
            InitializeSphereWireframe();
        }

        protected override void OnDraw(float deltaTime)
        {
            if (!_enabled)
                return;

            // Get active camera
            var camera = _scene.ActiveCamera;
            if (camera == null)
                return;

            // Set up effect matrices
            _effect.View = camera.View;
            _effect.Projection = camera.Projection;

            // Disable depth write but enable depth test for wireframe overlay
            var oldDepthStencilState = _device.DepthStencilState;
            _device.DepthStencilState = new DepthStencilState
            {
                DepthBufferEnable = true,
                DepthBufferWriteEnable = false
            };

            // Render all rigidbodies in the scene
            foreach (var gameObject in _scene.GameObjects)
            {
                var rigidBody = gameObject.GetComponent<RigidBody>();
                if (rigidBody == null || !rigidBody.Enabled)
                    continue;

                var collider = gameObject.GetComponent<Collider>();
                if (collider == null)
                    continue;

                // Determine color based on body type and trigger status
                Color color = GetDebugColor(rigidBody, collider);

                // Render based on collider type
                if (collider is BoxCollider boxCollider)
                {
                    DrawBoxCollider(gameObject.Transform, boxCollider, color);
                }
                else if (collider is SphereCollider sphereCollider)
                {
                    DrawSphereCollider(gameObject.Transform, sphereCollider, color);
                }
                else if (collider is CapsuleCollider capsuleCollider)
                {
                    DrawCapsuleCollider(gameObject.Transform, capsuleCollider, color);
                }
            }

            // Restore depth state
            _device.DepthStencilState = oldDepthStencilState;
        }

        protected override void OnRemoved()
        {
            _effect?.Dispose();
        }
        #endregion

        #region Drawing Methods
        private void DrawBoxCollider(Transform transform, BoxCollider box, Color color)
        {
            // Extract position and rotation from transform (without scale)
            var position = transform.Position;
            var rotation = transform.Rotation;

            // Apply collider center offset in world space
            var rotatedCenter = Vector3.Transform(box.Center, rotation);
            var worldPosition = position + rotatedCenter;

            // Build world matrix: Scale by collider size, then rotate and translate
            // NOTE: box.Size is already in world units, so we don't multiply by transform scale
            //Matrix world = Matrix.CreateScale(box.Size) *
            //              Matrix.CreateFromQuaternion(rotation) *
            //              Matrix.CreateTranslation(worldPosition);

            var scale = _useTransformScaleForDebug ? transform.LocalScale : Vector3.One;
            Matrix world = Matrix.CreateScale(box.Size * scale) *
                           Matrix.CreateFromQuaternion(rotation) *
                           Matrix.CreateTranslation(worldPosition);


            _effect.World = world;
            _effect.DiffuseColor = color.ToVector3();

            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _device.DrawUserIndexedPrimitives(
                    PrimitiveType.LineList,
                    _boxVertices,
                    0,
                    _boxVertices.Length,
                    _boxIndices,
                    0,
                    _boxIndices.Length / 2
                );
            }
        }

        private void DrawSphereCollider(Transform transform, SphereCollider sphere, Color color)
        {
            // Extract position and rotation from transform (without scale)
            var position = transform.Position;
            var rotation = transform.Rotation;

            // Apply collider center offset in world space
            var rotatedCenter = Vector3.Transform(sphere.Center, rotation);
            var worldPosition = position + rotatedCenter;

            // Build world matrix: Scale by sphere diameter, then rotate and translate
            // NOTE: sphere.Radius is already in world units, so we don't multiply by transform scale
            Matrix world = Matrix.CreateScale(sphere.Radius * 2f) *
                          Matrix.CreateFromQuaternion(rotation) *
                          Matrix.CreateTranslation(worldPosition);

            _effect.World = world;
            _effect.DiffuseColor = color.ToVector3();

            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _device.DrawUserIndexedPrimitives(
                    PrimitiveType.LineList,
                    _sphereVertices,
                    0,
                    _sphereVertices.Length,
                    _sphereIndices,
                    0,
                    _sphereIndices.Length / 2
                );
            }
        }


        private void DrawCapsuleCollider(Transform transform, CapsuleCollider capsule, Color color)
        {
            // For simplicity, draw capsule as a combination of cylinder + 2 spheres
            // More accurate would be to build actual capsule geometry

            // Extract position and rotation from transform (without scale)
            var position = transform.Position;
            var rotation = transform.Rotation;

            // Apply collider center offset in world space
            var rotatedCenter = Vector3.Transform(capsule.Center, rotation);
            var worldPosition = position + rotatedCenter;

            float radius = capsule.Radius;
            float height = capsule.Height;

            // Draw cylinder body (simplified as box for now)
            // NOTE: capsule dimensions are already in world units
            Matrix world = Matrix.CreateScale(radius * 2f, height, radius * 2f) *
                          Matrix.CreateFromQuaternion(rotation) *
                          Matrix.CreateTranslation(worldPosition);

            _effect.World = world;
            _effect.DiffuseColor = color.ToVector3();

            foreach (var pass in _effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _device.DrawUserIndexedPrimitives(
                    PrimitiveType.LineList,
                    _boxVertices,
                    0,
                    _boxVertices.Length,
                    _boxIndices,
                    0,
                    _boxIndices.Length / 2
                );
            }

            // TODO: Draw hemispherical caps at top and bottom
        }


        private Color GetDebugColor(RigidBody rigidBody, Collider collider)
        {
            // Triggers override everything
            if (collider.IsTrigger)
                return _triggerColor;

            // Body type colors
            switch (rigidBody.BodyType)
            {
                case BodyType.Static:
                    return _staticColor;
                case BodyType.Kinematic:
                    return _kinematicColor;
                case BodyType.Dynamic:
                    return _dynamicColor;
                default:
                    return Color.White;
            }
        }
        #endregion

        #region Initialization Methods
        private void InitializeBoxWireframe()
        {
            // Unit cube centered at origin (will be scaled by collider size)
            float half = 0.5f;

            _boxVertices = new VertexPositionColor[8]
            {
                new VertexPositionColor(new Vector3(-half, -half, -half), Color.White), // 0: LBB (left-bottom-back)
                new VertexPositionColor(new Vector3( half, -half, -half), Color.White), // 1: RBB
                new VertexPositionColor(new Vector3(-half,  half, -half), Color.White), // 2: LTB (left-top-back)
                new VertexPositionColor(new Vector3( half,  half, -half), Color.White), // 3: RTB
                new VertexPositionColor(new Vector3(-half, -half,  half), Color.White), // 4: LBF (left-bottom-front)
                new VertexPositionColor(new Vector3( half, -half,  half), Color.White), // 5: RBF
                new VertexPositionColor(new Vector3(-half,  half,  half), Color.White), // 6: LTF
                new VertexPositionColor(new Vector3( half,  half,  half), Color.White)  // 7: RTF
            };

            // 12 edges (24 indices as line list)
            _boxIndices = new short[]
            {
                // Back face
                0, 1,  1, 3,  3, 2,  2, 0,
                // Front face
                4, 5,  5, 7,  7, 6,  6, 4,
                // Connecting edges
                0, 4,  1, 5,  2, 6,  3, 7
            };
        }

        private void InitializeSphereWireframe()
        {
            var vertices = new List<VertexPositionColor>();
            var indices = new List<short>();

            // Generate sphere vertices (unit sphere, will be scaled)
            for (int ring = 0; ring <= SphereRings; ring++)
            {
                float phi = ring * MathHelper.Pi / SphereRings;
                float y = (float)Math.Cos(phi);
                float ringRadius = (float)Math.Sin(phi);

                for (int seg = 0; seg <= SphereSegments; seg++)
                {
                    float theta = seg * MathHelper.TwoPi / SphereSegments;
                    float x = ringRadius * (float)Math.Cos(theta);
                    float z = ringRadius * (float)Math.Sin(theta);

                    vertices.Add(new VertexPositionColor(
                        new Vector3(x, y, z) * 0.5f, // Scale to unit radius (will scale by diameter)
                        Color.White
                    ));
                }
            }

            // Generate line indices for latitude rings
            for (int ring = 0; ring < SphereRings; ring++)
            {
                for (int seg = 0; seg < SphereSegments; seg++)
                {
                    int current = ring * (SphereSegments + 1) + seg;
                    int next = current + 1;

                    // Horizontal line
                    indices.Add((short)current);
                    indices.Add((short)next);
                }
            }

            // Generate line indices for longitude lines
            for (int seg = 0; seg <= SphereSegments; seg++)
            {
                for (int ring = 0; ring < SphereRings; ring++)
                {
                    int current = ring * (SphereSegments + 1) + seg;
                    int below = (ring + 1) * (SphereSegments + 1) + seg;

                    // Vertical line
                    indices.Add((short)current);
                    indices.Add((short)below);
                }
            }

            _sphereVertices = vertices.ToArray();
            _sphereIndices = indices.ToArray();
        }
        #endregion

        #region Housekeeping Methods
        public override string ToString()
        {
            return $"PhysicsDebugRenderer(Enabled={_enabled})";
        }
        #endregion
    }


}