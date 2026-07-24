using GDEngine.Core.Input.Devices;
using Microsoft.Xna.Framework.Input;

namespace GDEngine.Core.Extensions
{
    /// <summary>
    /// Extension to GDMouseInput to support UI button and slider interactions.
    /// Automatically feeds MouseState updates to IInputReceiver components.
    /// </summary>
    public static class GDMouseInputUIExtensions
    {
        /// <summary>
        /// Feed current mouse state to UI components that need it.
        /// </summary>
        public static void FeedMouseStateToUI(this GDMouseInput mouseInput, MouseState currentState, IEnumerable<IInputReceiver> receivers)
        {
            if (receivers == null)
                return;

            // Update each receiver that needs mouse state
            foreach (var receiver in receivers)
            {
                // Use reflection to update mouse state on UI components
                // This allows buttons and sliders to track mouse movement and clicks
                UpdateMouseStateOnReceiver(receiver, currentState);
            }
        }

        private static void UpdateMouseStateOnReceiver(IInputReceiver receiver, MouseState currentState)
        {
            var receiverType = receiver.GetType();

            // Try to find _currentMouseState field
            var mouseStateField = receiverType.GetField("_currentMouseState",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (mouseStateField != null)
            {
                mouseStateField.SetValue(receiver, currentState);
            }
        }
    }
}
