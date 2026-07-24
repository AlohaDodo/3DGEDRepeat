using GDEngine.Core.Timing;
using Microsoft.Xna.Framework;

namespace GDEngine.Core.Impulses
{
    /// <summary>
    /// Generic eased 3D impulse event that can be routed via EventBus.
    /// Carries a channel name, direction, amplitude, time, duration, and easing function.
    /// </summary>
    /// <see cref="EventBus"/>
    public readonly struct Eased3DImpulse
    {
        #region Static Fields
        #endregion

        #region Fields
        private readonly string _channel;
        private readonly Vector3 _direction;
        private readonly float _amplitude;
        private readonly float _time;
        private readonly float _duration;
        private readonly Func<float, float> _ease;
        #endregion

        #region Properties
        public string Channel
        {
            get { return _channel; }
        }

        public float NormalizedTime
        {
            get
            {
                if (_duration <= 0f)
                    return 1f;

                float t = _time / _duration;
                if (t < 0f)
                    t = 0f;
                if (t > 1f)
                    t = 1f;

                return t;
            }
        }
        #endregion

        #region Constructors
        public Eased3DImpulse(
            string channel,
            Vector3 direction,
            float amplitude,
            float time,
            float duration,
            Func<float, float>? ease = null)
        {
            if (string.IsNullOrWhiteSpace(channel))
                channel = string.Empty;

            if (direction == Vector3.Zero)
                direction = Vector3.UnitY;

            _channel = channel;
            _direction = Vector3.Normalize(direction);
            _amplitude = amplitude;
            _time = time;
            _duration = duration <= 0f ? 0.0001f : duration;
            _ease = ease ?? Ease.Linear;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Gets the scalar strength (amplitude * easing) at the current time.
        /// </summary>
        public float GetStrength()
        {
            float t = NormalizedTime;
            float curve = _ease(t);
            return _amplitude * curve;
        }

        /// <summary>
        /// Gets the world-space offset vector for this impulse at the current time.
        /// </summary>
        public Vector3 GetOffset()
        {
            float strength = GetStrength();
            if (strength == 0f)
                return Vector3.Zero;

            return _direction * strength;
        }
        #endregion
    }
}
