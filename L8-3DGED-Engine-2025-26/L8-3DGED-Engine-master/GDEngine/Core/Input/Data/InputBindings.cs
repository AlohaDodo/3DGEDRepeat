using Microsoft.Xna.Framework.Input;

namespace GDEngine.Core.Input.Data
{
    /// <summary>
    /// Simple, rebindable bindings for keyboard and mouse across logical actions.
    /// </summary>
    public sealed class InputBindings
    {
        #region Static Fields
        public static InputBindings Default => CreateDefault();
        #endregion

        #region Fields
        private readonly Dictionary<InputAction, (Keys neg, Keys pos)> _keyboardAxes = new();
        private readonly Dictionary<InputAction, Keys> _keyboardButtons = new();
        private readonly Dictionary<InputAction, MouseButton> _mouseButtons = new();
        #endregion

        #region Properties
        public IReadOnlyDictionary<InputAction, (Keys neg, Keys pos)> KeyboardAxes => _keyboardAxes;
        public IReadOnlyDictionary<InputAction, Keys> KeyboardButtons => _keyboardButtons;
        public IReadOnlyDictionary<InputAction, MouseButton> MouseButtons => _mouseButtons;

        /// <summary>Milliseconds required between presses to treat as distinct presses (debounce).</summary>
        public int DebounceMs { get; set; } = 40;

        /// <summary>Mouse delta sensitivity applied to LookX/LookY.</summary>
        public float MouseSensitivity { get; set; } = 0.1f;

        /// <summary>If true, holding a key generates repeated "press" events after initial debounce.</summary>
        public bool EnableKeyRepeat { get; set; } = false;

        /// <summary>Milliseconds between repeats when EnableKeyRepeat is true.</summary>
        public int KeyRepeatMs { get; set; } = 300;

        /// <summary>
        /// MonoGame reports the wheel in "ticks" (commonly 120 per notch).
        /// Delta and value reported to receivers are divided by this.
        /// </summary>
        public float ScrollTickDivisor { get; set; } = 120f;
        #endregion

        #region Constructors
        public InputBindings() { }
        #endregion

        #region Methods
        public InputBindings BindKeyboardAxis(InputAction action, Keys negative, Keys positive)
        {
            _keyboardAxes[action] = (negative, positive);
            return this;
        }

        public InputBindings BindKeyboardButton(InputAction action, Keys key)
        {
            _keyboardButtons[action] = key;
            return this;
        }

        public InputBindings BindMouseButton(InputAction action, MouseButton button)
        {
            _mouseButtons[action] = button;
            return this;
        }

        public bool TryGetKeyboardAxis(InputAction a, out (Keys neg, Keys pos) pair)
        {
            return _keyboardAxes.TryGetValue(a, out pair);
        }

        public bool TryGetKeyboardButton(InputAction a, out Keys k)
        {
            return _keyboardButtons.TryGetValue(a, out k);
        }

        public bool TryGetMouseButton(InputAction a, out MouseButton b)
        {
            return _mouseButtons.TryGetValue(a, out b);
        }

        private static InputBindings CreateDefault()
        {
            var b = new InputBindings();
            // Axes
            b.BindKeyboardAxis(InputAction.MoveX, Keys.A, Keys.D)
             .BindKeyboardAxis(InputAction.MoveY, Keys.S, Keys.W)
             .BindKeyboardAxis(InputAction.LookX, Keys.Left, Keys.Right)
             .BindKeyboardAxis(InputAction.LookY, Keys.Down, Keys.Up);

            // Keyboard buttons
            b.BindKeyboardButton(InputAction.Jump, Keys.Space)
             .BindKeyboardButton(InputAction.Fire, Keys.LeftControl)
             .BindKeyboardButton(InputAction.Boost, Keys.LeftShift);

            // Mouse buttons (typical FPS mapping)
            b.BindMouseButton(InputAction.Fire, MouseButton.Left)
             .BindMouseButton(InputAction.Boost, MouseButton.Right);

            // Add middle-click mapping if you want (example: Jump):
            b.BindMouseButton(InputAction.Jump, MouseButton.Middle);

            return b;
        }
        #endregion
    }

    /// <summary>
    /// Mouse button names for bindings.
    /// </summary>
    public enum MouseButton : byte
    {
        Left = 0,
        Right = 1,
        Middle = 2,
        XButton1 = 3,
        XButton2 = 4
    }
}
