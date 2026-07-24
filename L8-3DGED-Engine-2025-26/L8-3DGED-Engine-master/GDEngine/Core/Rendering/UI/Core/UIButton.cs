using GDEngine.Core.Events;
using Microsoft.Xna.Framework;

namespace GDEngine.Core.Rendering.UI
{
    /// <summary>
    /// Unity-style button control built on top of <see cref="UISelectable"/>.
    /// Raises a click event on pointer click or submit.
    /// </summary>
    /// <see cref="UISelectable"/>
    public class UIButton : UISelectable,
        IUIPointerClickHandler,
        UISubmitHandler
    {
        #region Static Fields
        // None.
        #endregion

        #region Fields
        private Action? _onClick;
        private Action? _onPointerEntered;
        private Action? _onPointerExited;
        private Action? _onPointerDown;
        private Action? _onPointerUp;
        #endregion

        #region Properties (events)
        //event = (list of function pointers + flag (1/0)
        public event Action Clicked       //DoSomething(int x)
        {
            add { _onClick += value; }
            remove { _onClick -= value; }
        }

        public event Action PointerEntered
        {
            add { _onPointerEntered += value; }
            remove { _onPointerEntered -= value; }
        }

        public event Action PointerExited
        {
            add { _onPointerExited += value; }
            remove { _onPointerExited -= value; }
        }

        public event Action PointerDown
        {
            add { _onPointerDown += value; }
            remove { _onPointerDown -= value; }
        }

        public event Action PointerUp
        {
            add { _onPointerUp += value; }
            remove { _onPointerUp -= value; }
        }
        #endregion

        #region Constructors
        public UIButton()
        {
        }
        #endregion

        #region Methods
        public override void OnPointerEnter(UIPointerEvent eventData)
        {
            base.OnPointerEnter(eventData);
            _onPointerEntered?.Invoke();
        }

        public override void OnPointerExit(UIPointerEvent eventData)
        {
            base.OnPointerExit(eventData);
            _onPointerExited?.Invoke();
        }

        public override void OnPointerDown(UIPointerEvent eventData)
        {
            base.OnPointerDown(eventData);
            _onPointerDown?.Invoke();
        }

        public override void OnPointerUp(UIPointerEvent eventData)
        {
            base.OnPointerUp(eventData);
            _onPointerUp?.Invoke();
        }

        public void OnPointerClick(UIPointerEvent eventData)
        {
            if (!Interactable)
                return;

            if (eventData.Button != UIPointerButton.Left)
                return;

            Press();
        }

        public void OnSubmit()
        {
            if (!Interactable)
                return;

            Press();
        }

        /// <summary>
        /// Invokes the click event programmatically.
        /// </summary>
        public void Press()
        {
            if (!Interactable)
                return;

            _onClick?.Invoke();
        }

        /// <summary>
        /// Utility to center the button's bounds around a given position.
        /// </summary>
        /// <param name="centerPosition">Center position in UI space.</param>
        public void CenterAt(Vector2 centerPosition)
        {
            Position = centerPosition - (Size * 0.5f);
        }
        #endregion

        #region Lifecycle Methods
        // No additional lifecycle hooks yet.
        #endregion

        #region Housekeeping Methods
        // Add ToString if desired.
        #endregion
    }
}
