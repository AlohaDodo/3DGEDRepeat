using GDEngine.Core.Components;
using GDEngine.Core.Input.Data;
using GDEngine.Core.Input.Devices;
using GDEngine.Core.Systems;
using System.Diagnostics;
using System.Linq;

namespace GDGame.Demos
{
    /// <summary>
    /// Demo receiver that prints axis movements and button presses/releases
    /// from any input device routed through the InputSystem.
    /// Attach to any GameObject in the scene.
    /// </summary>
    /// <see cref="InputSystem"/>
    /// <see cref="IInputReceiver"/>
    public class InputReceiverComponent : Component, IInputReceiver
    {
        #region Static Fields
        //7 - ignore noise from analogue sticks/mouse by adding a tiny deadzone
        static readonly float INPUT_DEADZONE = 0.001f; 
        #endregion

        #region Fields
        private InputSystem _input; //  //3 - cache the InputSystem reference for unsubscribe on destroy
        #endregion

        #region Constructors
        // (none)
        #endregion

        #region Methods
        //1 - ensure you add IInputReceiver to the class to register for input callbacks
        //    (this class: DemoInputReceiverComponent : Component, IInputReceiver)

        //2 - in Start() locate the scene's InputSystem and subscribe
        protected override void Start()
        {
            var scene = GameObject?.Scene;
            if (scene == null)
            {
                Debug.WriteLine("[DemoInputReceiverComponent] No scene found. Add one before using this demo.");
                return;
            }

            //4 - find the InputSystem in the scene's systems list and add this as a receiver
            //    (InputSystem can be created earlier with InputSystem.CreateDefault() and scene.Add(sys))
            _input = scene.Systems.FirstOrDefault(s => s is InputSystem) as InputSystem;
            if (_input == null)
            {
                Debug.WriteLine("[DemoInputReceiverComponent] No InputSystem found in Scene. Add one before using this demo.");
                return;
            }

            _input.Add(this);
            Debug.WriteLine("[DemoInputReceiverComponent] Subscribed to InputSystem.");
        }

        //5 - always unsubscribe in OnDestroy() to avoid dangling references
        protected override void OnDestroy()
        {
            if (_input != null)
            {
                _input.Remove(this);
                Debug.WriteLine("[DemoInputReceiverComponent] Unsubscribed from InputSystem.");
            }
        }

        //6 - OnAxis(action, value) is called every frame when an axis has input
        //    Examples: MoveX/MoveY from WASD or LeftStick; LookX/LookY from arrows or mouse delta
        public void OnAxis(InputAction action, float value)
        {
            if (value > -INPUT_DEADZONE && value < INPUT_DEADZONE)
                return;

            Debug.WriteLine($"[Input AXIS] {action} = {value:0.###}");

            switch (action)
            {
                case InputAction.ScrollWheelDelta:
                    // zoom by delta per frame
                    Debug.WriteLine($"[Input AXIS] {action} = {value:0.###}");
                    break;

                case InputAction.ScrollWheelValue:
                    // or map absolute value to a parameter (e.g., UI scroll position)
                    Debug.WriteLine($"[Input AXIS] {action} = {value:0.###}");
                    break;

                    // existing MoveX/MoveY/LookX/LookY...
            }
        }

        //8 - OnButtonPressed(action, isFirstPress) is called on rising edge; repeats pass isFirstPress=false (if enabled)
        public void OnButtonPressed(InputAction action, bool isFirstPress)
        {
            //9 - if you only want first-press behavior (no repeats), early-out on isFirstPress=false
            //if (!isFirstPress) return;

            Debug.WriteLine($"[Input DOWN] {action} (first:{isFirstPress})");
        }

        //10 - OnButtonReleased(action) is called once on the falling edge
        public void OnButtonReleased(InputAction action)
        {
            Debug.WriteLine($"[Input UP]   {action}");
        }
        #endregion

        #region Lifecycle Methods
        // None
        #endregion

        #region Housekeeping Methods
        public override string ToString()
        {
            return "DemoInputReceiverComponent (logs device-agnostic input via InputSystem)";
        }
        #endregion
    }
}
