namespace GDEngine.Core.Input.Devices
{
    /// <summary>
    /// Contract for a pollable input device (keyboard, gamepad, mouse, etc.).
    /// </summary>
    public interface IInputDevice
    {
        /// <summary>Friendly device name (e.g., "Keyboard", "Gamepad P1").</summary>
        string Name { get; }

        /// <summary>True if the device is currently present/connected.</summary>
        bool Connected { get; }

        /// <summary>Poll current device state and compute edges/axes.</summary>
        void Update(float deltaTime);

        /// <summary>Push this frame's state into a receiver (axes + button edges).</summary>
        void Feed(IInputReceiver receiver);

        /// <summary>Clear any per-frame transient state.</summary>
        void ResetTransient();
    }
}
