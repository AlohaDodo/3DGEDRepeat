using System.Numerics;
using XNA = Microsoft.Xna.Framework;

namespace GDEngine.Core.Utilities
{
    /// <summary>
    /// Conversion helpers between BepuPhysics (System.Numerics) and MonoGame/XNA (Microsoft.Xna.Framework).
    /// Both use right-handed Y-up coordinate systems, so conversions are straightforward.
    /// </summary>
    /// <remarks>
    /// BepuPhysics uses System.Numerics types for SIMD performance.
    /// MonoGame uses Microsoft.Xna.Framework types for consistency with XNA.
    /// These extensions provide zero-overhead conversions between the two.
    /// </remarks>
    public static class PhysicsConversions
    {
        #region Vector3 Conversions
        /// <summary>
        /// Converts MonoGame Vector3 to BepuPhysics System.Numerics Vector3.
        /// </summary>
        public static Vector3 ToBepu(this XNA.Vector3 v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        /// <summary>
        /// Converts BepuPhysics System.Numerics Vector3 to MonoGame Vector3.
        /// </summary>
        public static XNA.Vector3 ToXNA(this Vector3 v)
        {
            return new XNA.Vector3(v.X, v.Y, v.Z);
        }
        #endregion

        #region Quaternion Conversions
        /// <summary>
        /// Converts MonoGame Quaternion to BepuPhysics System.Numerics Quaternion.
        /// </summary>
        public static Quaternion ToBepu(this XNA.Quaternion q)
        {
            return new Quaternion(q.X, q.Y, q.Z, q.W);
        }

        /// <summary>
        /// Converts BepuPhysics System.Numerics Quaternion to MonoGame Quaternion.
        /// </summary>
        public static XNA.Quaternion ToXNA(this Quaternion q)
        {
            return new XNA.Quaternion(q.X, q.Y, q.Z, q.W);
        }
        #endregion

        #region RigidPose Conversions
        /// <summary>
        /// Converts Transform position and rotation to BepuPhysics RigidPose.
        /// </summary>
        public static BepuPhysics.RigidPose ToRigidPose(XNA.Vector3 position, XNA.Quaternion rotation)
        {
            return new BepuPhysics.RigidPose(
                position.ToBepu(),
                rotation.ToBepu()
            );
        }

        /// <summary>
        /// Extracts position and rotation from BepuPhysics RigidPose.
        /// </summary>
        public static void FromRigidPose(BepuPhysics.RigidPose pose, out XNA.Vector3 position, out XNA.Quaternion rotation)
        {
            position = pose.Position.ToXNA();
            rotation = pose.Orientation.ToXNA();
        }
        #endregion

        #region BodyVelocity Conversions
        /// <summary>
        /// Creates a BepuPhysics BodyVelocity from linear and angular velocities.
        /// </summary>
        public static BepuPhysics.BodyVelocity ToBodyVelocity(XNA.Vector3 linear, XNA.Vector3 angular)
        {
            return new BepuPhysics.BodyVelocity
            {
                Linear = linear.ToBepu(),
                Angular = angular.ToBepu()
            };
        }

        /// <summary>
        /// Extracts linear and angular velocities from BepuPhysics BodyVelocity.
        /// </summary>
        public static void FromBodyVelocity(BepuPhysics.BodyVelocity velocity, out XNA.Vector3 linear, out XNA.Vector3 angular)
        {
            linear = velocity.Linear.ToXNA();
            angular = velocity.Angular.ToXNA();
        }
        #endregion
    }
}
