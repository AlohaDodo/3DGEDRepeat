using GDEngine.Core.Timing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace GDEngine.Core.Components
{
    /// <summary>
    /// Keyboard mover (default: WASD): W/S forward/back along camera Forward; A/D strafe along camera Right.
    /// Uses only Transform.TranslateBy(...); frame-rate independent via Time.DeltaTime.
    /// </summary>
    /// <see cref="Transform"/>
    /// <see cref="Camera"/>
    public sealed class KeyboardWASDController : Component
    {
        #region Fields
        private float _moveSpeed = 15f;        // base units/second
        private float _boostMultiplier = 4f;

        private Keys _forward = Keys.W;
        private Keys _backward = Keys.S;
        private Keys _left = Keys.A;
        private Keys _right = Keys.D;
        private Keys _boost = Keys.LeftShift;
        #endregion

        #region Properties
        public float MoveSpeed
        {
            get => _moveSpeed;
            set => _moveSpeed = value < 0f ? 0f : value;
        }

        public float BoostMultiplier
        {
            get => _boostMultiplier;
            set => _boostMultiplier = value < 1f ? 1f : value;
        }

        public Keys ForwardKey { get => _forward; set => _forward = value; }
        public Keys BackwardKey { get => _backward; set => _backward = value; }
        public Keys LeftKey { get => _left; set => _left = value; }
        public Keys RightKey { get => _right; set => _right = value; }
        public Keys BoostKey { get => _boost; set => _boost = value; }
        #endregion

        #region Constructors
        #endregion

        #region Methods
        // Move by world-space direction * speed * dt using Transform.TranslateBy.
        private void Move(Vector3 direction, float speed)
        {
            if (Transform == null)
                return;

            //remove any incidental magnitude on the direction
            direction.Normalize();

            //add on magnitude here
            Vector3 delta = direction * (speed * Time.DeltaTimeSecs);

            // we treat delta as a world vector.
            // Because worldDir is built from the camera basis, it still "feels" camera-relative.
            Transform.TranslateBy(delta, worldSpace: true);
        }
        #endregion

        #region Lifecycle Methods

        // Build direction from camera basis each frame; apply boost; translate.
        protected override void Update(float deltaTime)
        {
            if (Transform == null)
                return;

            var kb = Keyboard.GetState();

            float speed = _moveSpeed;
            if (kb.IsKeyDown(_boost))
                speed *= _boostMultiplier;

            Vector3 dir = Vector3.Zero;

            if (kb.IsKeyDown(_forward))
                dir += Transform.Forward;

            if (kb.IsKeyDown(_backward))
                dir -= Transform.Forward;

            if (kb.IsKeyDown(_right))
                dir += Transform.Right;

            if (kb.IsKeyDown(_left))
                dir -= Transform.Right;

            // TODO - Add QE left/drop

            if (dir.LengthSquared() != 0)
                Move(dir, speed);
        }

        #endregion

        #region Housekeeping Methods
        // none
        #endregion
    }
}

/*
using GDEngine.Core.Timing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace GDEngine.Core.Components
{
   
    public class KeyboardWASDController : Component
    {
        private KeyboardState _newKBState;

        protected override void Update(float deltaTime)
        {
            //get kb state
            _newKBState = Keyboard.GetState();
            Vector3 dir = Vector3.Zero;
            //read some keys
            if (_newKBState.IsKeyDown(Keys.W))
                dir += Transform.Forward;
            else if (_newKBState.IsKeyDown(Keys.S))
                dir -= Transform.Forward;

            if (_newKBState.IsKeyDown(Keys.A))
                dir += Transform.Right;
            else if (_newKBState.IsKeyDown(Keys.D))
                dir -= Transform.Right;
            //test if not zero
            if (dir.LengthSquared() > 0)
                HandleMove(dir);
        }
        
        private void HandleMove(Vector3 direction)
        {
            //make direction length = 1
            direction.Normalize();

            //scale by elapsed time (and speed later)
            direction *= Time.DeltaTime * 20;

            //apply delta vector
            Transform.TranslateBy(direction, true);
        }

        //TODO - support speed, boost key, key re-mapping
    }
}
    */
