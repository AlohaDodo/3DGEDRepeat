using GDEngine.Core.Components;
using GDEngine.Core.Timing;
using Microsoft.Xna.Framework;
using System;

namespace GDGame.Demos.Controllers
{
    /// <summary>
    /// Oscillates the owning GameObject along a single direction using a sine wave.
    /// The object moves about its cached starting position with amplitude <c>_maxDistance</c>.
    /// </summary>
    /// <see cref="Entities.GameObject"/>
    /// <see cref="Transform"/>
    public class SineTranslationController : Component
    {
        #region Fields
        // Angular speed in radians per second. Example: 2PI = one full oscillation per second.
        public float _angularSpeed = 1f;

        // Maximum displacement from the starting position along _direction.
        // Effective range of motion is [-_maxDistance, +_maxDistance] along that axis.
        public float _maxDistance = 1f;

        // Local-space direction the object will move along. Normalized on Awake if non-zero.
        // Use Vector3.UnitX / UnitY / UnitZ (or any vector) to choose the axis of motion.
        public Vector3 _direction = Vector3.UnitY;

        // Cached starting position taken on Awake; oscillation is centered about this point.
        private Vector3 _originalLocalPosition;
        #endregion

        #region Lifecycle Methods
        /// <summary>
        /// Advance the oscillator and apply the new offset.
        /// </summary>
        /// <param name="deltaTime">
        /// Frame delta in seconds (not used directly here because we use realtime for consistent phasing).
        /// </param>
        protected override void Update(float deltaTime)
        {
            // MathF.Sin returns [-1, +1], giving a centered oscillation about the origin.
            float phase = (float)(Time.RealtimeSinceStartupSecs * _angularSpeed);
            float distance = MathF.Sin(phase); // [1, +1]

            // Offset from the cached start position along the normalized direction, scaled by amplitude.
            Transform?.TranslateTo(_originalLocalPosition + _direction * distance * _maxDistance);    
        
        }

        /// <summary>
        /// Cache the starting position and normalize the direction (if non-zero).
        /// </summary>
        protected override void Awake()
        {
            // Ensure Transform exists; rotation has no meaning without it.
            if (Transform == null)
                throw new ArgumentNullException(nameof(Transform));

            // Remember where we start so the motion is centered about this point.
            _originalLocalPosition = Transform.LocalPosition;

            // Ensure direction is a unit vector; ignore if the user intentionally set zero (no movement).
            if (_direction != Vector3.Zero)
                _direction.Normalize();
        }
        #endregion
    }
}
