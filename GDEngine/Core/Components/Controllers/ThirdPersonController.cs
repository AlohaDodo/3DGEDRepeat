using System;
using GDEngine.Core.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace GDEngine.Core.Components
{
    /// <summary>
    /// Simple third-person follow controller for a camera.
    /// 
    /// Attach this component to the same GameObject as a Camera.
    /// Set TargetName to the name of the player GameObject.
    /// The controller will:
    /// - Resolve the target using Scene.Find(Predicate{GameObject}) and cache it.
    /// - Keep the camera behind and above the target at a configurable distance/height.
    /// - Smoothly interpolate position and orientation each frame.
    /// - Support mouse-wheel zoom (scroll up = closer, scroll down = further away).
    /// - Support an over-the-shoulder lateral offset.
    /// </summary>
    /// <see cref="Transform"/>
    /// <see cref="Camera"/>
    public sealed class ThirdPersonController : Component
    {
        #region Static Fields
        #endregion

        #region Fields
        private string _targetName = string.Empty;
        private GameObject? _target;
        private Transform? _targetTransform;

        private float _followDistance = 10f;       // Horizontal distance behind target (world units)
        private float _height = 2.5f;             // Vertical offset above target
        private float _positionDamping = 8f;     // Higher = snappier position
        private float _rotationDamping = 5f;     // Higher = snappier rotation

        private Vector3 _targetOffset = new Vector3(0f, 2.0f, 0f); // Look-at offset from target

        // Over-the-shoulder lateral offset (relative to target's Right vector).
        private float _shoulderOffset = 0f;

        // Mouse-wheel zoom
        private int _previousScrollValue;
        private float _zoomSensitivity = 0.01f;   // Units per raw scroll step (120 units per notch in MonoGame)
        private float _minFollowDistance = 2f;
        private float _maxFollowDistance = 30f;
        #endregion

        #region Properties
        public string TargetName
        {
            get { return _targetName; }
            set
            {
                if (string.Equals(_targetName, value, StringComparison.Ordinal))
                    return;

                _targetName = value ?? string.Empty;
                _target = null;
                _targetTransform = null;
            }
        }

        public float FollowDistance
        {
            get { return _followDistance; }
            set
            {
                if (value < 0.1f)
                    value = 0.1f;

                _followDistance = value;
            }
        }

        public float Height
        {
            get { return _height; }
            set
            {
                if (value < 0f)
                    value = 0f;

                _height = value;
            }
        }

        public float PositionDamping
        {
            get { return _positionDamping; }
            set
            {
                if (value < 0f)
                    value = 0f;

                _positionDamping = value;
            }
        }

        public float RotationDamping
        {
            get { return _rotationDamping; }
            set
            {
                if (value < 0f)
                    value = 0f;

                _rotationDamping = value;
            }
        }

        public Vector3 TargetOffset
        {
            get { return _targetOffset; }
            set { _targetOffset = value; }
        }

        /// <summary>
        /// Lateral offset from the target, relative to the target's Right vector.
        /// Positive = move camera to the right (right shoulder).
        /// Negative = move camera to the left (left shoulder).
        /// </summary>
        public float ShoulderOffset
        {
            get { return _shoulderOffset; }
            set { _shoulderOffset = value; }
        }

        public float ZoomSensitivity
        {
            get { return _zoomSensitivity; }
            set
            {
                if (value < 0f)
                    value = 0f;

                _zoomSensitivity = value;
            }
        }

        public float MinFollowDistance
        {
            get { return _minFollowDistance; }
            set
            {
                if (value < 0.1f)
                    value = 0.1f;
                _minFollowDistance = value;
            }
        }

        public float MaxFollowDistance
        {
            get { return _maxFollowDistance; }
            set
            {
                if (value < _minFollowDistance)
                    value = _minFollowDistance;
                _maxFollowDistance = value;
            }
        }
        #endregion

        #region Constructors
        #endregion

        #region Methods
        public void SetTarget(GameObject target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            _target = target;
            _targetTransform = target.Transform;
            _targetName = target.Name ?? string.Empty;
        }

        private void ResolveTargetByName()
        {
            if (GameObject == null)
                return;

            var scene = GameObject.Scene;
            if (scene == null)
                return;

            if (string.IsNullOrWhiteSpace(_targetName))
                return;

            GameObject? found = scene.Find(go => go.Name == _targetName);
            if (found == null)
                return;

            SetTarget(found);
        }

        private void UpdateZoom()
        {
            MouseState mouse = Mouse.GetState();
            int scrollValue = mouse.ScrollWheelValue;

            int delta = scrollValue - _previousScrollValue;
            if (delta != 0)
            {
                float newDistance = _followDistance - (delta * _zoomSensitivity);

                if (newDistance < _minFollowDistance)
                    newDistance = _minFollowDistance;
                if (newDistance > _maxFollowDistance)
                    newDistance = _maxFollowDistance;

                _followDistance = newDistance;
            }

            _previousScrollValue = scrollValue;
        }

        private void UpdateFollow(float deltaTime)
        {
            if (Transform == null)
                return;

            if (_targetTransform == null)
                return;

            // Position follow 
            Vector3 targetLookAt = _targetTransform.Position + _targetOffset;

            Vector3 targetForward = _targetTransform.Forward;
            if (targetForward.LengthSquared() < 1e-6f)
                targetForward = Vector3.Forward;

            Vector3 targetRight = _targetTransform.Right;
            if (targetRight.LengthSquared() < 1e-6f)
                targetRight = Vector3.Right;

            // Over-the-shoulder offset relative to the target's Right axis.
            Vector3 shoulderOffset = targetRight * _shoulderOffset;

            Vector3 desiredPos =
                targetLookAt
                - targetForward * _followDistance
                + Vector3.Up * _height
                + shoulderOffset;

            Vector3 currentPos = Transform.Position;
            Vector3 newPos;

            if (_positionDamping <= 0f)
            {
                newPos = desiredPos;
            }
            else
            {
                float t = _positionDamping * deltaTime;
                if (t < 0f)
                    t = 0f;
                if (t > 1f)
                    t = 1f;

                newPos = Vector3.Lerp(currentPos, desiredPos, t);
            }

            Vector3 delta = newPos - currentPos;
            if (delta.LengthSquared() > 0f)
                Transform.TranslateBy(delta, true);

            // Rotation follow (look at target, no roll) 
            Vector3 eye = Transform.Position;
            Vector3 toTarget = targetLookAt - eye;
            if (toTarget.LengthSquared() < 1e-6f)
                return;

            Matrix view = Matrix.CreateLookAt(eye, targetLookAt, Vector3.Up);
            Matrix invView = Matrix.Invert(view);
            Quaternion desiredWorld = Quaternion.CreateFromRotationMatrix(invView);
            desiredWorld = Quaternion.Normalize(desiredWorld);

            Quaternion currentWorld = Transform.Rotation;
            Quaternion targetWorld;

            if (_rotationDamping <= 0f)
            {
                targetWorld = desiredWorld;
            }
            else
            {
                float tRot = _rotationDamping * deltaTime;
                if (tRot < 0f)
                    tRot = 0f;
                if (tRot > 1f)
                    tRot = 1f;

                targetWorld = Quaternion.Slerp(currentWorld, desiredWorld, tRot);
                targetWorld = Quaternion.Normalize(targetWorld);
            }

            Transform.RotateToWorld(targetWorld);
        }
        #endregion

        #region Lifecycle Methods
        protected override void Awake()
        {
            if (GameObject == null)
                throw new InvalidOperationException("ThirdPersonController requires a GameObject.");

            var camera = GameObject.GetComponent<Camera>();
            if (camera == null)
                throw new InvalidOperationException("ThirdPersonController requires a Camera on the same GameObject.");

            _previousScrollValue = Mouse.GetState().ScrollWheelValue;

            if (_target == null && !string.IsNullOrWhiteSpace(_targetName))
                ResolveTargetByName();
        }

        protected override void Update(float deltaTime)
        {
            if (!Enabled)
                return;

            UpdateZoom();

            if (_targetTransform == null && !string.IsNullOrWhiteSpace(_targetName))
                ResolveTargetByName();

            if (_targetTransform == null)
                return;

            UpdateFollow(deltaTime);
        }
        #endregion

        #region Housekeeping Methods
        public override string ToString()
        {
            string targetLabel = _target != null
                ? (_target.Name ?? "<unnamed>")
                : (string.IsNullOrWhiteSpace(_targetName) ? "<none>" : _targetName);

            return "ThirdPersonController(Target=" + targetLabel + ")";
        }
        #endregion
    }
}
