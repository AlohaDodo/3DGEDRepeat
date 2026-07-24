using GDEngine.Core.Input.Data;
using GDEngine.Core.Timing;
using Microsoft.Xna.Framework.Input;

namespace GDEngine.Core.Input.Devices
{
    /// <summary>
    /// Keyboard device using rebindable <see cref="InputBindings"/> with debouncing + first-press detection.
    /// </summary>
    public sealed class GDKeyboardInput : IInputDevice
    {
        #region Fields
        private readonly string _name;
        private readonly InputBindings _bindings;

        private KeyboardState _prev;
        private KeyboardState _curr;

        private readonly Dictionary<InputAction, float> _axes = new();
        private readonly Dictionary<Keys, double> _lastPressTime = new();
        private readonly Dictionary<Keys, double> _lastRepeatTime = new();
        private readonly HashSet<Keys> _heldNow = new(8);
        private readonly HashSet<Keys> _heldPrev = new(8);
        #endregion

        #region Properties
        public string Name => _name;
        public bool Connected => true; // desktop keyboard assumed present
        #endregion

        #region Constructors
        public GDKeyboardInput(InputBindings? bindings = null, string name = "Keyboard")
        {
            _name = name;
            _bindings = bindings ?? InputBindings.Default;
        }
        #endregion

        #region Methods
        public void Update(float deltaTime)
        {
            _prev = _curr;
            _curr = Keyboard.GetState();

            _axes.Clear();
            _heldPrev.Clear();
            _heldNow.Clear();

            // Build axes
            foreach (var kv in _bindings.KeyboardAxes)
            {
                var negDown = _curr.IsKeyDown(kv.Value.neg) ? 1f : 0f;
                var posDown = _curr.IsKeyDown(kv.Value.pos) ? 1f : 0f;
                _axes[kv.Key] = posDown - negDown;

                if (negDown > 0) _heldNow.Add(kv.Value.neg);
                if (posDown > 0) _heldNow.Add(kv.Value.pos);
            }

            // Track held keys for buttons too
            foreach (var kv in _bindings.KeyboardButtons)
                if (_curr.IsKeyDown(kv.Value))
                    _heldNow.Add(kv.Value);

            // prev held set (from previous frame state)
            foreach (var kv in _bindings.KeyboardAxes)
            {
                if (_prev.IsKeyDown(kv.Value.neg)) _heldPrev.Add(kv.Value.neg);
                if (_prev.IsKeyDown(kv.Value.pos)) _heldPrev.Add(kv.Value.pos);
            }
            foreach (var kv in _bindings.KeyboardButtons)
                if (_prev.IsKeyDown(kv.Value))
                    _heldPrev.Add(kv.Value);
        }

        public void Feed(IInputReceiver receiver)
        {
            // Axes every frame
            foreach (var kv in _axes)
                receiver.OnAxis(kv.Key, kv.Value);

            // Buttons: rising/falling edges + debounce + optional repeats
            foreach (var kv in _bindings.KeyboardButtons)
                EmitButton(receiver, kv.Key, kv.Value);

            // Optional: treat axis keys as buttons too (so UI can react to first key press)
            foreach (var kv in _bindings.KeyboardAxes)
            {
                EmitButton(receiver, kv.Key, kv.Value.neg);
                EmitButton(receiver, kv.Key, kv.Value.pos);
            }
        }

        public void ResetTransient()
        {
            // no-op
        }

        // Internal button emission with debounce + first-press / repeat
        private void EmitButton(IInputReceiver receiver, InputAction action, Keys key)
        {
            bool now = _curr.IsKeyDown(key);
            bool was = _prev.IsKeyDown(key);
            if (!now && !was)
                return;

            double tNow = Time.RealtimeSinceStartupSecs * 1000.0;
            _lastPressTime.TryGetValue(key, out double last);
            _lastRepeatTime.TryGetValue(key, out double lastRep);

            if (now && !was)
            {
                // Rising edge: always allowed (first press), but respect debounce if the previous release/press was too recent
                bool pass = tNow - last >= _bindings.DebounceMs;
                if (pass)
                {
                    FirePressed(receiver, action, isFirstPress: true);
                    _lastPressTime[key] = tNow;
                    _lastRepeatTime[key] = tNow;
                }
            }
            else if (!now && was)
            {
                receiver.OnButtonReleased(action);
            }
            else if (now && was && _bindings.EnableKeyRepeat)
            {
                // Hold repeat after initial delay (KeyRepeatMs)
                if (tNow - lastRep >= _bindings.KeyRepeatMs)
                {
                    FirePressed(receiver, action, isFirstPress: false);
                    _lastRepeatTime[key] = tNow;
                }
            }
        }

        private static void FirePressed(IInputReceiver receiver, InputAction action, bool isFirstPress)
        {
            // Legacy callback
            receiver.OnButtonPressed(action, isFirstPress);
        }
        #endregion
    }
}
