using GDEngine.Core.Input.Data;
using GDEngine.Core.Timing;
using Microsoft.Xna.Framework.Input;

namespace GDEngine.Core.Input.Devices
{
    /// <summary>
    /// Mouse device providing LookX/LookY deltas, mouse buttons, and scroll wheel data via <see cref="InputBindings"/>.
    /// </summary>
    public sealed class GDMouseInput : IInputDevice
    {
        #region Fields
        private readonly string _name;
        private readonly InputBindings _bindings;

        private MouseState _prev;
        private MouseState _curr;

        private float _dx;
        private float _dy;

        private int _wheelPrev;
        private int _wheelCurr;

        private readonly Dictionary<MouseButton, double> _lastPressMs = new();
        private readonly Dictionary<MouseButton, double> _lastRepeatMs = new();
        #endregion

        #region Properties
        public string Name => _name;
        public bool Connected => true; // assume mouse present
        #endregion

        #region Constructors
        public GDMouseInput(InputBindings? bindings = null, string name = "Mouse")
        {
            _name = name;
            _bindings = bindings ?? InputBindings.Default;
        }
        #endregion

        #region Methods
        public void Update(float deltaTime)
        {
            _prev = _curr;
            _curr = Mouse.GetState();

            // Movement deltas (receivers can smooth if desired)
            _dx = (_curr.X - _prev.X) * _bindings.MouseSensitivity;
            _dy = (_curr.Y - _prev.Y) * _bindings.MouseSensitivity;

            // Wheel absolute (raw ticks)
            _wheelPrev = _prev.ScrollWheelValue;
            _wheelCurr = _curr.ScrollWheelValue;
        }

        public void Feed(IInputReceiver receiver)
        {
            // Look axes
            if (_dx != 0f)
                receiver.OnAxis(InputAction.LookX, _dx);
            if (_dy != 0f)
                receiver.OnAxis(InputAction.LookY, _dy);

            // Scroll wheel support 
            int rawDelta = _wheelCurr - _wheelPrev;
            if (rawDelta != 0)
            {
                float delta = rawDelta / _bindings.ScrollTickDivisor;
                float value = _wheelCurr / _bindings.ScrollTickDivisor;

                // Per-frame delta (emit only when non-zero)
                receiver.OnAxis(InputAction.ScrollWheelDelta, delta);
                // Absolute value (emit alongside delta so listeners can track either)
                receiver.OnAxis(InputAction.ScrollWheelValue, value);
            }

            // Buttons (includes middle-click if bound in InputBindings)
            foreach (var kv in _bindings.MouseButtons)
                EmitMouseButton(receiver, kv.Key, kv.Value);
        }

        private void EmitMouseButton(IInputReceiver r, InputAction action, MouseButton b)
        {
            bool now = IsDown(_curr, b);
            bool was = IsDown(_prev, b);
            if (!now && !was)
                return;

            double tNowMs = Time.RealtimeSinceStartupSecs * 1000.0;
            _lastPressMs.TryGetValue(b, out double last);
            _lastRepeatMs.TryGetValue(b, out double lastRep);

            if (now && !was)
            {
                if (tNowMs - last >= _bindings.DebounceMs)
                {
                    r.OnButtonPressed(action, true);   // rising edge (first press)
                    _lastPressMs[b] = tNowMs;
                    _lastRepeatMs[b] = tNowMs;
                }
            }
            else if (!now && was)
            {
                r.OnButtonReleased(action);
            }
            else if (now && was && _bindings.EnableKeyRepeat)
            {
                if (tNowMs - lastRep >= _bindings.KeyRepeatMs)
                {
                    r.OnButtonPressed(action, false);  // debounced repeat
                    _lastRepeatMs[b] = tNowMs;
                }
            }
        }

        private static bool IsDown(in MouseState s, MouseButton b)
        {
            return b switch
            {
                MouseButton.Left => s.LeftButton == ButtonState.Pressed,
                MouseButton.Right => s.RightButton == ButtonState.Pressed,
                MouseButton.Middle => s.MiddleButton == ButtonState.Pressed,
                MouseButton.XButton1 => s.XButton1 == ButtonState.Pressed,
                MouseButton.XButton2 => s.XButton2 == ButtonState.Pressed,
                _ => false
            };
        }

        public void ResetTransient()
        {
            //NO-OP
        }
        #endregion
    }
}
