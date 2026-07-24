using Microsoft.Xna.Framework;

namespace GDEngine.Core.Events
{
    /// <summary>
    /// Pointer input type used by the UI event system.
    /// </summary>
    public enum UIPointerButton
    {
        Left,
        Right,
        Middle
    }

    /// <summary>
    /// Pointer event data passed to UI pointer handlers.
    /// </summary>
    public sealed class UIPointerEvent
    {
        #region Fields
        public Vector2 Position;
        public UIPointerButton Button;
        #endregion

        #region Constructors
        public UIPointerEvent(Vector2 position, UIPointerButton button)
        {
            Position = position;
            Button = button;
        }
        #endregion
    }

    public interface IUIPointerEnterHandler
    {
        void OnPointerEnter(UIPointerEvent eventData);
    }

    public interface IUIPointerExitHandler
    {
        void OnPointerExit(UIPointerEvent eventData);
    }

    public interface IUIPointerDownHandler
    {
        void OnPointerDown(UIPointerEvent eventData);
    }

    public interface IUIPointerUpHandler
    {
        void OnPointerUp(UIPointerEvent eventData);
    }

    public interface IUIPointerClickHandler
    {
        void OnPointerClick(UIPointerEvent eventData);
    }

    public interface UISubmitHandler
    {
        void OnSubmit();
    }
}
