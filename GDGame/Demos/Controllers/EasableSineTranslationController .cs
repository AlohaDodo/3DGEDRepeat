using GDEngine.Core.Components;
using GDEngine.Core.Timing;
using Microsoft.Xna.Framework;
using System;

namespace GDGame.Demos.Controllers
{
    /// <summary>
    /// Oscillates the GameObject along a direction using a sine wave,
    /// but with an optional time-easing curve to shape the speed profile over each cycle.
    /// Useful for adding personality (e.g., EaseInOutSine, EaseOutCubic, Elastic-like feel) to the motion.
    /// </summary>
    public class EasableSineTranslationController : Component
    {
        #region Fields
        // Angular speed in radians per second e.g. 2PI = 1 full cycle per second.
        public float _angularSpeed = MathHelper.TwoPi * 0.5f;

        // Max distance (amplitude) from the original position along _direction.
        public float _maxDistance = 1f;

        // Movement direction (will be normalized on Awake if non-zero).
        public Vector3 _direction = Vector3.UnitY;

        // Optional phase offset (radians) added before wrapping into a cycle.
        public float _phaseRadians = 0f;

        // Time shaping curve e.g., Ease.EaseInOutSine, Ease.EaseOutCubic, Ease.EaseInOutElastic, etc.
        public Func<float, float> _timeCurve = Ease.EaseInOutSine;

        // Cached original position.
        private Vector3 _originalLocalPosition;
        #endregion

        #region Properties
        #endregion

        #region Constructors
        #endregion

        #region Methods
        // Maps a running angle (radians) to an eased angle by:
        private float ComputeEasedAngle(float rawAngleInRadians)
        {
            // Normalize angle to cycles, then take fractional part as t is [0,1)
            float cycles = rawAngleInRadians / MathHelper.TwoPi;
            float t = cycles - MathF.Floor(cycles);

            // Apply time curve if present
            float te = _timeCurve != null ? _timeCurve(t) : t;

            // Convert back to radians
            float angleEased = te * MathHelper.TwoPi;
            return angleEased;
        }
        #endregion

        #region Lifecycle Methods
        /// <summary>
        /// Cache the starting position and normalize the direction (if non-zero).
        /// </summary>
        protected override void Awake()
        {
            // Ensure Transform exists; rotation has no meaning without it.
            if (Transform == null)
                throw new ArgumentNullException(nameof(Transform));

            _originalLocalPosition = Transform.LocalPosition;

            if (_direction != Vector3.Zero)
                _direction.Normalize();
        }

        /// <summary>
        /// Advance time, build an eased phase angle, evaluate sine, and offset along the direction.
        /// </summary>
        protected override void Update(float deltaTime)
        {
            // Build the raw running angle using unscaled realtime 
            float angleRaw = (float)Time.RealtimeSinceStartupSecs * _angularSpeed + _phaseRadians;

            // Ease the time within the cycle before applying sine
            float angle = ComputeEasedAngle(angleRaw);

            // Distance between [-1,1]
            float distance = MathF.Sin(angle);

            // Apply amplitude and direction about the original position
            Transform?.TranslateTo(_originalLocalPosition + _direction * distance * _maxDistance);
        }
        #endregion
    }
}
