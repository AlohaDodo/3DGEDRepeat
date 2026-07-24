using GDEngine.Core.Components;
using GDEngine.Core.Rendering.Base;

namespace GDEngine.Core.Events
{
    /// <summary>
    /// Raised when two non-trigger rigidbodies generate contacts.
    /// Includes layer information so listeners can filter by LayerMask.
    /// </summary>
    public readonly struct CollisionEvent
    {
        public RigidBody BodyA { get; }
        public RigidBody BodyB { get; }

        public LayerMask LayerA { get; }
        public LayerMask LayerB { get; }

        /// <summary>
        /// Combined mask (bitwise OR) of the two layers.
        /// Useful for quick mask tests.
        /// </summary>
        public LayerMask CombinedMask { get; }

        public CollisionEvent(RigidBody bodyA, RigidBody bodyB)
        {
            BodyA = bodyA;
            BodyB = bodyB;

            // Safely pull layers from the owning GameObjects
            var goA = bodyA?.GameObject;
            var goB = bodyB?.GameObject;

            LayerA = goA != null ? goA.Layer : LayerMask.All;
            LayerB = goB != null ? goB.Layer : LayerMask.All;
            CombinedMask = LayerA | LayerB;
        }

        /// <summary>
        /// Returns true if either body’s layer is included in the given mask.
        /// </summary>
        public bool Matches(LayerMask mask)
        {
            // Assumes LayerMask implements bitwise operators (&, |) like in Unity.
            return (LayerA & mask) != 0 || (LayerB & mask) != 0;
        }

        public override string ToString()
        {
            var nameA = BodyA?.GameObject?.Name ?? "<null>";
            var nameB = BodyB?.GameObject?.Name ?? "<null>";
            return $"CollisionEvent(A={nameA}, B={nameB}, Layers={LayerA}|{LayerB})";
        }
    }


    /// <summary>
    /// Raised when at least one collider in a pair is marked as IsTrigger.
    /// </summary>
    public readonly struct TriggerEvent
    {
        public RigidBody TriggerBody { get; }
        public RigidBody OtherBody { get; }

        public TriggerEvent(RigidBody triggerBody, RigidBody otherBody)
        {
            TriggerBody = triggerBody;
            OtherBody = otherBody;
        }

        public override string ToString()
        {
            return $"TriggerEvent(Trigger={TriggerBody?.GameObject?.Name}, Other={OtherBody?.GameObject?.Name})";
        }
    }

}
