using BepuPhysics;
using BepuPhysics.Collidables;
using Microsoft.Xna.Framework;

namespace GDEngine.Core.Components
{
    #region PhysicsMaterial
    /// <summary>
    /// Defines physical properties of a surface.
    /// </summary>
    public sealed class PhysicsMaterial
    {
        #region Static Fields
        /// <summary>Default material with moderate friction and no bounce.</summary>
        public static readonly PhysicsMaterial Default = new PhysicsMaterial(0.5f, 0.0f);

        /// <summary>Very slippery surface.</summary>
        public static readonly PhysicsMaterial Ice = new PhysicsMaterial(0.05f, 0.0f);

        /// <summary>Very grippy surface.</summary>
        public static readonly PhysicsMaterial Rubber = new PhysicsMaterial(0.9f, 0.7f);

        /// <summary>Bouncy surface.</summary>
        public static readonly PhysicsMaterial Bouncy = new PhysicsMaterial(0.4f, 0.9f);

        /// <summary>No friction, no bounce.</summary>
        public static readonly PhysicsMaterial Frictionless = new PhysicsMaterial(0.0f, 0.0f);
        #endregion

        #region Properties
        /// <summary>
        /// Friction coefficient (0 = frictionless, 1 = high friction).
        /// Typical values: Ice ~0.05, Wood ~0.4, Rubber ~0.9
        /// </summary>
        public float Friction { get; set; }

        /// <summary>
        /// Restitution/bounciness coefficient (0 = no bounce, 1 = perfect bounce).
        /// Values above 1 add energy to the system (useful for gameplay but not realistic).
        /// </summary>
        public float Restitution { get; set; }
        #endregion

        #region Constructors
        public PhysicsMaterial(float friction = 0.5f, float restitution = 0.0f)
        {
            Friction = Math.Clamp(friction, 0f, 1f);
            Restitution = Math.Clamp(restitution, 0f, 1f);
        }
        #endregion

        #region Methods
        public override string ToString()
        {
            return $"PhysicsMaterial(Friction={Friction:F2}, Restitution={Restitution:F2})";
        }
        #endregion
    }
    #endregion

    #region Collider Base Class
    /// <summary>
    /// Base class for all physics collider shapes.
    /// Colliders define the shape used for collision detection and response.
    /// Must be paired with a RigidBody component on the same GameObject.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Colliders come in several varieties:
    /// - Primitive shapes (Box, Sphere, Capsule): Fast and efficient
    /// - Mesh colliders: Complex but expensive, prefer for static geometry
    /// - Compound colliders: Multiple shapes combined
    /// </para>
    /// <para>
    /// Performance tips:
    /// - Use the simplest shape that fits the needs
    /// - Sphere and Capsule are fastest for moving objects
    /// - Box is good for static geometry
    /// - Avoid Mesh colliders for dynamic objects
    /// </para>
    /// </remarks>
    /// <see cref="RigidBody"/>
    /// <see cref="BoxCollider"/>
    /// <see cref="SphereCollider"/>
    public abstract class Collider : Component
    {
        #region Fields
        private PhysicsMaterial _material = PhysicsMaterial.Default;
        private bool _isTrigger = false;
        private Vector3 _center = Vector3.Zero;
        #endregion

        #region Properties
        /// <summary>
        /// Material defining friction and bounciness properties.
        /// </summary>
        public PhysicsMaterial Material
        {
            get => _material;
            set => _material = value ?? PhysicsMaterial.Default;
        }

        /// <summary>
        /// If true, this collider acts as a trigger (no physical response, only events).
        /// Triggers detect overlaps but don't block movement.
        /// </summary>
        public bool IsTrigger
        {
            get => _isTrigger;
            set => _isTrigger = value;
        }

        /// <summary>
        /// Local offset from the GameObject's Transform position.
        /// Allows positioning the collision shape independently of the visual representation.
        /// </summary>
        public Vector3 Center
        {
            get => _center;
            set => _center = value;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Creates the BepuPhysics shape for this collider and adds it to the simulation.
        /// </summary>
        /// <param name="simulation">The physics simulation to add the shape to.</param>
        /// <returns>TypedIndex handle to the created shape.</returns>
        internal abstract TypedIndex CreateShape(Simulation simulation);

        /// <summary>
        /// Calculates body inertia for the given mass.
        /// Used by RigidBody to configure dynamic physics properties.
        /// </summary>
        /// <param name="mass">Mass in kilograms.</param>
        /// <returns>Inertia tensor for this shape.</returns>
        internal abstract BodyInertia CalculateInertia(float mass);
        #endregion
    }
    #endregion

    #region BoxCollider
    /// <summary>
    /// Box-shaped collider primitive.
    /// Efficient for cubes, walls, floors, and rectangular objects.
    /// </summary>
    /// <remarks>
    /// Box colliders are one of the fastest collision shapes and work well for:
    /// - Static geometry (walls, floors, platforms)
    /// - Rectangular objects (crates, buildings, vehicles)
    /// - Approximate collision bounds for complex shapes
    /// 
    /// The box is centered at the GameObject's position plus the Center offset,
    /// and oriented according to the GameObject's rotation.
    /// </remarks>
    /// <see cref="Collider"/>
    /// <see cref="RigidBody"/>
    public sealed class BoxCollider : Collider
    {
        private static readonly float MIN_BOX_SIDE_LENGTH = 1E-3F;
        #region Fields
        private Vector3 _size = Vector3.One;
        #endregion

        #region Properties
        /// <summary>
        /// Full extents of the box in local space (width, height, depth).
        /// Default is (1, 1, 1) representing a 1-meter cube.
        /// </summary>
        public Vector3 Size
        {
            get => _size;
            set
            {
                // Ensure positive dimensions
                _size = new Vector3(
                    Math.Max(MIN_BOX_SIDE_LENGTH, value.X),
                    Math.Max(MIN_BOX_SIDE_LENGTH, value.Y),
                    Math.Max(MIN_BOX_SIDE_LENGTH, value.Z)
                );
            }
        }

        /// <summary>
        /// Half-extents of the box (Size / 2). Often more convenient for calculations.
        /// </summary>
        public Vector3 HalfSize
        {
            get => _size * 0.5f;
            set => Size = value * 2f;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a box collider with default 1-meter cube dimensions.
        /// </summary>
        public BoxCollider()
        {
        }

        /// <summary>
        /// Creates a box collider with specified size.
        /// </summary>
        /// <param name="size">Full dimensions of the box.</param>
        public BoxCollider(Vector3 size)
        {
            Size = size;
        }

        /// <summary>
        /// Creates a cube collider with uniform size.
        /// </summary>
        /// <param name="size">Uniform dimension (cube edge length).</param>
        public BoxCollider(float size)
        {
            Size = new Vector3(size, size, size);
        }
        #endregion

        #region Methods
        internal override TypedIndex CreateShape(Simulation simulation)
        {
            // BepuPhysics Box uses half-extents
            var halfExtents = HalfSize;

            var box = new Box(
                halfExtents.X,
                halfExtents.Y,
                halfExtents.Z
            );

            return simulation.Shapes.Add(box);
        }

        internal override BodyInertia CalculateInertia(float mass)
        {
            // BepuPhysics Box uses half-extents
            var halfExtents = HalfSize;

            var box = new Box(
                halfExtents.X,
                halfExtents.Y,
                halfExtents.Z
            );

            // Calculate inertia tensor for a box
            return box.ComputeInertia(mass);
        }

        public override string ToString()
        {
            return $"BoxCollider(Size={_size}, Center={Center})";
        }
        #endregion
    }
    #endregion

    #region SphereCollider
    /// <summary>
    /// Sphere-shaped collider primitive.
    /// Most efficient collision shape for moving objects.
    /// </summary>
    /// <remarks>
    /// Sphere colliders are the fastest collision shape in BepuPhysics and ideal for:
    /// - Projectiles (bullets, cannonballs, grenades)
    /// - Rolling objects (balls, boulders, wheels)
    /// - Character controllers (simplified capsule alternative)
    /// - Approximate collision bounds for round objects
    /// 
    /// The sphere is centered at the GameObject's position plus the Center offset.
    /// Rotation has no effect on spheres since they are rotationally symmetric.
    /// </remarks>
    /// <see cref="Collider"/>
    /// <see cref="RigidBody"/>
    public sealed class SphereCollider : Collider
    {
        #region Fields
        private float _radius = 0.5f;
        #endregion

        #region Properties
        /// <summary>
        /// Radius of the sphere in meters.
        /// Default is 0.5 (diameter of 1 meter).
        /// </summary>
        public float Radius
        {
            get => _radius;
            set => _radius = Math.Max(0.001f, value); // Prevent zero/negative radius
        }

        /// <summary>
        /// Diameter of the sphere (convenience property, equals Radius * 2).
        /// </summary>
        public float Diameter
        {
            get => _radius * 2f;
            set => Radius = value * 0.5f;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a sphere collider with default 0.5-meter radius.
        /// </summary>
        public SphereCollider()
        {
        }

        /// <summary>
        /// Creates a sphere collider with specified radius.
        /// </summary>
        /// <param name="radius">Radius in meters.</param>
        public SphereCollider(float radius)
        {
            Radius = radius;
        }
        #endregion

        #region Methods
        internal override TypedIndex CreateShape(Simulation simulation)
        {
            var sphere = new Sphere(_radius);
            return simulation.Shapes.Add(sphere);
        }

        internal override BodyInertia CalculateInertia(float mass)
        {
            return new Sphere(_radius).ComputeInertia(mass);
        }

        public override string ToString()
        {
            return $"SphereCollider(Radius={_radius:F2}, Center={Center})";
        }
        #endregion
    }
    #endregion

    #region CapsuleCollider
    /// <summary>
    /// Capsule-shaped collider primitive (cylinder with hemispherical caps).
    /// Excellent for character controllers and standing objects.
    /// </summary>
    /// <remarks>
    /// Capsule colliders are ideal for:
    /// - Character controllers (smooth movement over obstacles)
    /// - Humanoid/animal characters
    /// - Pills, bottles, cylindrical projectiles
    /// - Any tall object that needs to handle slopes/steps well
    /// 
    /// A capsule is defined by a radius and height (including the caps).
    /// The capsule is aligned along the Y-axis by default, with hemispherical
    /// caps on top and bottom. The cylinder portion has length (height - 2*radius).
    /// 
    /// The capsule is centered at the GameObject's position plus the Center offset,
    /// and oriented according to the GameObject's rotation.
    /// </remarks>
    /// <see cref="Collider"/>
    /// <see cref="RigidBody"/>
    public sealed class CapsuleCollider : Collider
    {
        #region Fields
        private float _radius = 0.5f;
        private float _height = 2.0f;
        #endregion

        #region Properties
        /// <summary>
        /// Radius of the capsule in meters.
        /// Default is 0.5 (diameter of 1 meter).
        /// </summary>
        public float Radius
        {
            get => _radius;
            set
            {
                _radius = Math.Max(0.001f, value);

                // Ensure height is at least 2*radius (minimum valid capsule)
                if (_height < _radius * 2f)
                    _height = _radius * 2f;
            }
        }

        /// <summary>
        /// Total height of the capsule including both hemispherical caps.
        /// Must be at least 2*Radius. Default is 2.0 meters.
        /// </summary>
        public float Height
        {
            get => _height;
            set => _height = Math.Max(_radius * 2f, value); // Height must be >= diameter
        }

        /// <summary>
        /// Length of the cylindrical portion (Height - 2*Radius).
        /// Read-only property for convenience.
        /// </summary>
        public float CylinderLength => Math.Max(0f, _height - _radius * 2f);
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a capsule collider with default dimensions (radius 0.5, height 2.0).
        /// </summary>
        public CapsuleCollider()
        {
        }

        /// <summary>
        /// Creates a capsule collider with specified dimensions.
        /// </summary>
        /// <param name="radius">Radius in meters.</param>
        /// <param name="height">Total height including caps.</param>
        public CapsuleCollider(float radius, float height)
        {
            _radius = Math.Max(0.001f, radius);
            _height = Math.Max(_radius * 2f, height);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Creates the underlying Bepu capsule shape.
        /// Bepu's Capsule length parameter is the length of the inner line segment (excluding the end caps),
        /// so we use (Height - 2 * Radius) as the cylinder segment length.
        /// </summary>
        internal override TypedIndex CreateShape(Simulation simulation)
        {
            float cylinderLength = Math.Max(0f, _height - 2f * _radius);
            var capsule = new Capsule(_radius, cylinderLength);
            return simulation.Shapes.Add(capsule);
        }

        /// <summary>
        /// Calculates the inertia tensor for this capsule, given a mass.
        /// </summary>
        internal override BodyInertia CalculateInertia(float mass)
        {
            float cylinderLength = Math.Max(0f, _height - 2f * _radius);
            return new Capsule(_radius, cylinderLength).ComputeInertia(mass);
        }

        public override string ToString()
        {
            return $"CapsuleCollider(Radius={_radius:F2}, Height={_height:F2}, Center={Center})";
        }
        #endregion
    }
    #endregion

    }