using GDEngine.Core.Enums;
using GDEngine.Core.Input.Devices;

namespace GDEngine.Core.Systems
{
    /// <summary>
    /// EarlyUpdate system that polls all registered input devices and forwards
    /// axis/button events to registered receivers (components/systems).
    /// </summary>
    /// <see cref="IInputDevice"/>
    /// <see cref="IInputReceiver"/>
    public sealed class InputSystem : SystemBase
    {
        #region Fields
        private readonly List<IInputDevice> _devices = new(4);
        private readonly List<IInputReceiver> _receivers = new(32);
        #endregion

        #region Properties
        /// <summary>Number of devices being polled.</summary>
        public int DeviceCount => _devices.Count;

        /// <summary>Number of receivers subscribed to events.</summary>
        public int ReceiverCount => _receivers.Count;
        #endregion

        #region Constructors
        /// <summary>
        /// Place the InputSystem in EarlyUpdate so input is ready for gameplay Update().
        /// </summary>
        public InputSystem() : base(FrameLifecycle.EarlyUpdate, order: 0)
        {
        }
        #endregion

        #region Methods
        /// <summary>Add an input device (keyboard, gamepad, mouse...).</summary>
        public void Add(IInputDevice device)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            if (_devices.Contains(device))
                return;
            _devices.Add(device);
        }

        /// <summary>Remove an input device.</summary>
        public bool Remove(IInputDevice device)
        {
            if (device == null)
                return false;
            return _devices.Remove(device);
        }

        /// <summary>Subscribe a receiver to get input each frame.</summary>
        public void Add(IInputReceiver receiver)
        {
            if (receiver == null)
                throw new ArgumentNullException(nameof(receiver));
            if (_receivers.Contains(receiver))
                return;
            _receivers.Add(receiver);
        }

        /// <summary>Unsubscribe a receiver.</summary>
        public bool Remove(IInputReceiver receiver)
        {
            if (receiver == null)
                return false;
            return _receivers.Remove(receiver);
        }

        /// <summary>
        /// Convenience: create a default keyboard+gamepad setup.
        /// </summary>
        public static InputSystem CreateDefault()
        {
            var sys = new InputSystem();
            sys.Add(new GDKeyboardInput());
            sys.Add(new GDGamepadInput());
            return sys;
        }
        #endregion

        #region Lifecycle Methods
        /// <summary>
        /// Poll devices, then feed all receivers with this frame's axes and edges.
        /// </summary>
        public override void Update(float deltaTime)
        {
            var deviceCount = _devices.Count;
    
            if (deviceCount == 0)
                throw new ArgumentException("Ensure you register a device in the game Main class");

            // poll all devices
            for (int i = 0; i < deviceCount; i++)
                _devices[i].Update(deltaTime);

            var receiverCount = _receivers.Count;

            // deliver to receivers
            for (int r = 0; r < receiverCount; r++)
            {
                var receiver = _receivers[r];
                for (int d = 0; d < deviceCount; d++)
                    _devices[d].Feed(receiver);
            }

            // clear device transients (if any)
            for (int i = 0; i < deviceCount; i++)
                _devices[i].ResetTransient();
        }
        #endregion

        #region Housekeeping Methods
        public override string ToString()
        {
            return $"InputSystem(Devices={_devices.Count}, Receivers={_receivers.Count})";
        }
        #endregion
    }
}
