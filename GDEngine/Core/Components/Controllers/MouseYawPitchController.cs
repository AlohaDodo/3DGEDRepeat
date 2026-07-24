using GDEngine.Core.Timing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace GDEngine.Core.Components
{
    /// <summary>
    /// Simple mouse-look controller that applies yaw (around world Y)
    /// and pitch (around local right) without introducing roll.
    /// </summary>
    /// <see cref="Component"/>
    /// <see cref="Transform"/>
    public class MouseYawPitchController : Component
    {
        #region Fields
        private MouseState _newMouseState;
        private MouseState _oldMouseState;

        private float _mouseSensitivity = 0.4f;

        // Accumulated yaw/pitch in radians.
        // Yaw: rotation around world Y axis.
        // Pitch: rotation around local right axis (clamped).
        private float _yaw;
        private float _pitch;

        private bool _initializedAngles;
        #endregion

        #region Lifecycle Methods
        protected override void Awake()
        {
            _oldMouseState = Mouse.GetState();
            InitializeAnglesFromCurrentTransform();
        }

        protected override void Update(float deltaTime)
        {
            if (!GameObject.Name.Equals(GameObject.Scene.ActiveCamera.GameObject.Name))
                return;

            if (Transform == null)
                return;

            // Get mouse state & deltas
            _newMouseState = Mouse.GetState();

            float dX = _newMouseState.X - _oldMouseState.X;
            float dY = _newMouseState.Y - _oldMouseState.Y;

            // Scale to radians: sensitivity is "radians per pixel".
            // (You can drop Time.DeltaTimeSecs if you prefer non–frame-rate-scaled input.)
            float yawDelta = dX * _mouseSensitivity * Time.DeltaTimeSecs;
            float pitchDelta = dY * _mouseSensitivity * Time.DeltaTimeSecs;

            // Typical FPS scheme: move mouse right → yaw right (negative in right-handed system),
            // move mouse up → look up (decrease pitch).
            _yaw -= yawDelta;
            _pitch -= pitchDelta;

            // Clamp pitch to avoid flipping over (no looking straight up/down).
            const float maxPitchDeg = 89f;
            float maxPitchRad = MathHelper.ToRadians(maxPitchDeg);
            _pitch = MathHelper.Clamp(_pitch, -maxPitchRad, maxPitchRad);

            // Build the desired world rotation as pure yaw + pitch (no roll).
            Quaternion desiredWorldRotation = Quaternion.CreateFromYawPitchRoll(_yaw, _pitch, 0f);

            // Compute the world-space delta from current to desired:
            // delta = desired * inverse(current)
            Quaternion currentWorldRotation = Transform.Rotation;
            Quaternion invCurrent = Quaternion.Inverse(currentWorldRotation);
            Quaternion delta = Quaternion.Normalize(Quaternion.Concatenate(desiredWorldRotation, invCurrent));

            // Apply delta in world space so the underlying Transform stays in sync.
            Transform.RotateBy(delta, worldSpace: true);

            // Store old state for next frame delta
            _oldMouseState = _newMouseState;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Initialize yaw/pitch from the current Transform.Forward, so that
        /// mouse-look starts from the existing camera orientation.
        /// </summary>
        private void InitializeAnglesFromCurrentTransform()
        {
            if (Transform == null || _initializedAngles)
                return;

            // MonoGame forward is (0,0,-1). Extract yaw/pitch from the current forward vector.
            Vector3 f = Vector3.Normalize(Transform.Forward);

            // Pitch is rotation around X: asin(y).
            _pitch = (float)System.Math.Asin(f.Y);

            // Yaw is rotation around Y: atan2(x, -z) because forward is -Z when yaw=0.
            _yaw = (float)System.Math.Atan2(f.X, -f.Z);

            _initializedAngles = true;
        }
        #endregion
    }
}
