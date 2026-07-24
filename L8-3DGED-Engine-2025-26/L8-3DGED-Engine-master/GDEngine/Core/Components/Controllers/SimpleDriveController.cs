using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace GDEngine.Core.Components
{
    /// <summary>
    /// Simple drive controller:
    /// U/J = forward/back along current facing; H/K = yaw left/right (world-up).
    /// Forward is taken from the world matrix so it matches visual rotation
    /// regardless of row/column axis extraction.
    /// </summary>
    public sealed class SimpleDriveController : Component
    {
        #region Fields
        private float _moveSpeed = 15f;  // units/sec
        private float _turnSpeed = 2.5f;   // radians/sec
        #endregion

        #region Lifecycle Methods
        protected override void Update(float deltaTime)
        {
            if (Transform == null)
                return;

            var k = Keyboard.GetState();

            // --- Rotation (H left, K right) around WORLD UP to avoid roll coupling ---
            float yawInput = 0f;
            if (k.IsKeyDown(Keys.H)) yawInput += 1f;
            if (k.IsKeyDown(Keys.K)) yawInput -= 1f;

            if (yawInput != 0f)
            {
                // worldSpace:true guarantees pure yaw about +Y, even if the object has tilt
                Transform.RotateEulerBy(new Vector3(0f, yawInput * _turnSpeed * deltaTime, 0f), worldSpace: true);
            }

            // --- Translation (U forward, J back) along actual current facing ---
            float moveInput = 0f;
            if (k.IsKeyDown(Keys.U)) moveInput -= 1f;
            if (k.IsKeyDown(Keys.J)) moveInput += 1f;

            if (moveInput != 0f)
            {
                // Derive forward from the world matrix (works regardless of row/column basis)
                Vector3 dir = Vector3.Normalize(Vector3.TransformNormal(Vector3.Forward, Transform.WorldMatrix));

                // Keep motion planar (comment out next line if you want vertical movement)
                dir.Y = 0f;
                if (dir.LengthSquared() > 1e-8f) dir.Normalize();

                Vector3 worldDelta = -dir * (moveInput * _moveSpeed * deltaTime);
                Transform.TranslateBy(worldDelta, worldSpace: true);
            }
        }
        #endregion
    }
}
