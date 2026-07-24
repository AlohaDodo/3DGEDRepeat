using GDEngine.Core.Entities;
using GDEngine.Core.Timing;
using Microsoft.Xna.Framework;

namespace GDEngine.Core.Components
{
    /// <summary>
    /// Drives a GameObject along a position curve and orients it towards a target curve.
    /// Both curves are AnimationCurve3D and are evaluated over a normalised time [0,1].
    /// </summary>
    /// <see cref="AnimationCurve3D"/>
    /// <see cref="GameObject"/>
    /// <see cref="Transform"/>
    public sealed class CurveController : Component
    {
        #region Static Fields
        // None
        #endregion

        #region Fields
        private Transform? _transform;

        private AnimationCurve3D? _positionCurve;
        private AnimationCurve3D? _targetCurve;

        private float _time;
        private float _duration = 5f;
        private float _playbackSpeed = 1f;
        #endregion

        #region Properties
        /// <summary>
        /// Curve used to control the drawn gameobject position in world space.
        /// The curve is sampled with a normalised parameter t in [0,1].
        /// </summary>
        public AnimationCurve3D? PositionCurve
        {
            get => _positionCurve;
            set => _positionCurve = value;
        }

        /// <summary>
        /// Curve used to control the drawn gameobject look target in world space.
        /// If null, the drawn gameobject will look along its current forward direction.
        /// </summary>
        public AnimationCurve3D? TargetCurve
        {
            get => _targetCurve;
            set => _targetCurve = value;
        }

        /// <summary>
        /// Total duration, in seconds, for a full playback of the curves.
        /// </summary>
        public float Duration
        {
            get => _duration;
            set
            {
                if (value <= 0f)
                    value = 0.01f;
                _duration = value;
            }
        }

        /// <summary>
        /// Playback speed multiplier. 1 = normal, 2 = twice as fast, 0.5 = half speed.
        /// </summary>
        public float PlaybackSpeed
        {
            get => _playbackSpeed;
            set => _playbackSpeed = value;
        }

        #endregion

        #region Constructors
        // Parameterless constructor required for AddComponent<T>.
        #endregion

        #region Methods
        private double _timeSeconds;

        /// <summary>
        /// Compute the curve-time value we pass into AnimationCurve.
        /// Duration is the time (in seconds) for one 0→1 sweep.
        /// </summary>
        private double ComputeCurveTime(float deltaTime)
        {
            _timeSeconds += deltaTime * PlaybackSpeed;

            if (Duration <= 0f)
                return _timeSeconds; // use controller time directly

            // Map controller seconds onto curve domain (0→1 in Duration seconds)
            double curveTime = _timeSeconds / Duration;

            // If Loop == true we do NOT wrap; we let AnimationCurve’s CurveLoopType
            // decide what to do with times outside [start,end].
            return curveTime;
        }


        private void OrientTowards(in Vector3 position, in Vector3 target)
        {
            if (_transform == null)
                return;

            var toTarget = target - position;
            if (toTarget.LengthSquared() < 0.0001f)
                return;

            var forward = Vector3.Normalize(toTarget);

            // Build a world matrix from position + forward + world up.
            var world = Matrix.CreateWorld(position, forward, Vector3.Up);
            var desired = Quaternion.CreateFromRotationMatrix(world);

            var current = _transform.Rotation;
            var delta = Quaternion.Multiply(desired, Quaternion.Inverse(current));
            delta = Quaternion.Normalize(delta);

            _transform.RotateBy(delta, worldSpace: true);
        }
        #endregion

        #region Lifecycle Methods
        protected override void Awake()
        {
            if (GameObject == null)
                throw new NullReferenceException(nameof(GameObject));
            _transform = GameObject.Transform;

            base.Awake();
        }

        protected override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            if (_transform == null)
                return;

            if (_positionCurve == null || _targetCurve == null)
                throw new NullReferenceException("Ensure you set position and look target curves");

            double curveTime = ComputeCurveTime(deltaTime);

            // Set position
            Vector3 position = _transform.Position;
            if (_positionCurve != null && !_positionCurve.IsEmpty)
                position = _positionCurve.Evaluate(curveTime);

            _transform.TranslateTo(position);

            // Set target 
            if (_targetCurve != null && !_targetCurve.IsEmpty)
            {
                Vector3 target = _targetCurve.Evaluate(curveTime);
                OrientTowards(position, target);
            }
        }


        #endregion

        #region Housekeeping Methods
        // None
        #endregion
    }
}