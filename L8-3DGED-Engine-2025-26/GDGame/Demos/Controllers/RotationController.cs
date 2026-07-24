using GDEngine.Core.Components;
using GDEngine.Core.Entities;
using Microsoft.Xna.Framework;
using System;

namespace GDGame.Demos.Controllers
{
    /// <summary>
    /// Rotates the owning <see cref="GameObject"/> around a configurable local-space axis
    /// at a configurable angular speed. Rotation is applied incrementally each frame using
    /// an axis–angle quaternion and composed with the current <see cref="Transform.LocalRotation"/>.
    /// </summary>
    /// <see cref="Transform"/>
    public sealed class RotationController : Component
    {
        #region Static Fields
        // Smallest speed we consider "meaningful" (radians/sec). Helps avoid work and floating-point churn.
        private static readonly float ROTATION_THRESHOLD = 1E-8f;
        #endregion

        #region Fields
        // Local-space rotation axis. Will be normalized in Awake() to ensure stable angular motion.
        // Defaults to +Y (Vector3.Up) which yields a spin like a turntable.
        public Vector3 _rotationAxisNormalized = Vector3.Up;

        // Angular speed in radians per second. Positive values rotate according to the right-hand rule
        // about _rotationAxisNormalized; negative values rotate in the opposite direction.
        public float _rotationSpeedInRadiansPerSecond = MathF.PI / 2f; // 90°/s by default
        #endregion

        #region Lifecycle Methods
        /// <summary>
        /// Applies an incremental rotation for this frame.
        /// </summary>
        /// <param name="deltaTime">Elapsed time since last frame (seconds).</param>
        protected override void Update(float deltaTime)
        {
            // Skip tiny angular speeds to avoid unnecessary quaternion work / denorms.
            if (MathF.Abs(_rotationSpeedInRadiansPerSecond) <= ROTATION_THRESHOLD)
                return;

            // θ = ω * Δt (radians). This is the per-frame angle to rotate by.
            float angle = _rotationSpeedInRadiansPerSecond * deltaTime;

            // Build a delta-rotation from axis–angle. Assumes axis is already normalized in Awake().
            Quaternion delta = Quaternion.CreateFromAxisAngle(_rotationAxisNormalized, angle);

            //Apply rotation via delta quaternion
            Transform?.RotateBy(delta);
        }

        /// <summary>
        /// Validates references and normalizes configuration for stable runtime behavior.
        /// </summary>
        protected override void Awake()
        {
            // Ensure Transform exists; rotation has no meaning without it.
            if (Transform == null)
                throw new ArgumentNullException(nameof(Transform));

            // Guarantee unit-length axis so angular speed maps 1:1 to radians/sec about that axis.
            // If the user provided the zero vector, Normalize() will leave it at (0,0,0);
            // in that case, no visible rotation will occur, which is acceptable and safe.
            _rotationAxisNormalized.Normalize();

            // NO-OP: base.Awake() if the base class needs it; otherwise intentionally omitted.
        }
        #endregion
    }
}
