using GDEngine.Core.Input.Data;

namespace GDEngine.Core.Input.Devices
{
    /// <summary>
    /// Receives device-agnostic input events each frame.
    /// Implement on Components or Systems to consume input.
    /// </summary>
    public interface IInputReceiver
    {
        /// <summary>Axis value in [-1, +1]. Called every frame per bound action.</summary>
        void OnAxis(InputAction action, float value);

        /// <summary>
        /// Called on button press. 
        /// isFirstPress = true on the rising edge; false if emitted as a debounced repeat (when key repeat is enabled).
        /// </summary>
        void OnButtonPressed(InputAction action, bool isFirstPress);

        /// <summary>Called once on button release (falling edge).</summary>
        void OnButtonReleased(InputAction action);
    }
}
