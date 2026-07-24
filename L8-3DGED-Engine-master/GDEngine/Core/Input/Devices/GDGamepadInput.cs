using GDEngine.Core.Input.Data;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace GDEngine.Core.Input.Devices
{
    /// <summary>
    /// XInput gamepad mapper: LeftStick=Move, RightStick=Look, A=Jump, RT=Fire, LB=Boost.
    /// </summary>
    public sealed class GDGamepadInput : IInputDevice
    {
        #region Fields
        private readonly PlayerIndex _player;
        private GamePadState _prev;
        private GamePadState _curr;

        // Axis cache
        private readonly Dictionary<InputAction, float> _axes = new();

        private readonly string _name;
        private bool _connected;
        #endregion

        #region Properties
        public string Name => _name;
        public bool Connected => _connected;
        #endregion

        #region Constructors
        public GDGamepadInput(PlayerIndex player = PlayerIndex.One, string? name = null)
        {
            _player = player;
            _name = name ?? $"Gamepad {_player}";
        }
        #endregion

        #region Methods
        /// <summary>
        /// Polls underlying XInput device and computes axes.
        /// </summary>
        public void Update(float deltaTime)
        {
            _prev = _curr;
            _curr = GamePad.GetState(_player);

            _connected = _curr.IsConnected;

            _axes.Clear();
            if (!_connected)
                return;

            // Left stick: Move (Y is inverted so +1 = forward)
            _axes[InputAction.MoveX] = _curr.ThumbSticks.Left.X;
            _axes[InputAction.MoveY] = _curr.ThumbSticks.Left.Y;

            // Right stick: Look
            _axes[InputAction.LookX] = _curr.ThumbSticks.Right.X;
            _axes[InputAction.LookY] = _curr.ThumbSticks.Right.Y;
        }

        /// <summary>
        /// Emits axes every frame and button edges when connected.
        /// </summary>
        public void Feed(IInputReceiver receiver)
        {
            if (!_connected)
                return;

            // Axes
            foreach (var kv in _axes)
                receiver.OnAxis(kv.Key, kv.Value);

            // Buttons (edges)
            FireEdges(receiver, InputAction.Jump, _curr.Buttons.A == ButtonState.Pressed, _prev.Buttons.A == ButtonState.Pressed);
            // Treat RT as Fire when pressed beyond a threshold
            FireEdges(receiver, InputAction.Fire, _curr.Triggers.Right > 0.5f, _prev.Triggers.Right > 0.5f);
            FireEdges(receiver, InputAction.Boost, _curr.Buttons.LeftShoulder == ButtonState.Pressed, _prev.Buttons.LeftShoulder == ButtonState.Pressed);
        }

        /// <summary>
        /// Clears per-frame transients (none beyond cached state).
        /// </summary>
        public void ResetTransient()
        {
            // no-op
        }

        // helper
        private static void FireEdges(IInputReceiver r, InputAction action, bool now, bool was)
        {
            if (now && !was)
                r.OnButtonPressed(action, true);
            else if (!now && was)
                r.OnButtonReleased(action);
        }
        #endregion
    }
}
