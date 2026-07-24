using GDEngine.Core.Components;
using GDEngine.Core.Events;
using GDEngine.Core.Systems;
using Microsoft.Xna.Framework;

namespace GDEngine.Core.Rendering.UI
{
    /// <summary>
    /// Base class for selectable UI controls that respond to pointer and submit events.
    /// Manages interaction state and visual transitions on a target <see cref="UIRenderer"/>.
    /// </summary>
    /// <see cref="UIRenderer"/>
    public enum UISelectionState
    {
        Normal,
        Highlighted,
        Pressed,
        Disabled
    }

    /// <summary>
    /// Transition mode used by <see cref="UISelectable"/> to update its target graphic.
    /// </summary>
    public enum UITransitionMode
    {
        None,
        ColorTint
        // SpriteSwap can be added later if you want full Unity parity.
    }

    /// <summary>
    /// Unity-style selectable base class for UI controls (buttons, sliders, etc.).
    /// Handles interaction state and color-tint transitions on a target graphic.
    /// </summary>
    /// <see cref="UIRenderer"/>
    public class UISelectable : Component,
        IUIPointerEnterHandler,
        IUIPointerExitHandler,
        IUIPointerDownHandler,
        IUIPointerUpHandler
    {
        #region Static Fields
        // None for now.
        #endregion

        #region Fields
        private UIRenderer? _targetGraphic;

        private UITransitionMode _transitionMode;
        private UISelectionState _currentState;
        private bool _interactable;

        private Color _normalColor;
        private Color _highlightedColor;
        private Color _pressedColor;
        private Color _disabledColor;

        private Vector2 _position;
        private Vector2 _size;
        private Rectangle _bounds;

        private bool _isPointerInside;
        private bool _isPointerDown;
        private bool _hasFocus;

        private bool _autoSizeFromTargetGraphic;
        #endregion

        #region Properties
        public UIRenderer? TargetGraphic
        {
            get
            {
                return _targetGraphic;
            }
            set
            {
                _targetGraphic = value;
                if (_autoSizeFromTargetGraphic)
                    RefreshSizeFromTargetGraphic();
            }
        }

        public UITransitionMode TransitionMode
        {
            get
            {
                return _transitionMode;
            }
            set
            {
                _transitionMode = value;
                ApplyState(_currentState);
            }
        }

        public bool Interactable
        {
            get
            {
                return _interactable;
            }
            set
            {
                if (_interactable == value)
                    return;

                _interactable = value;

                if (_interactable)
                    TransitionToState(_isPointerInside ? UISelectionState.Highlighted : UISelectionState.Normal);
                else
                    TransitionToState(UISelectionState.Disabled);
            }
        }

        public UISelectionState CurrentState
        {
            get
            {
                return _currentState;
            }
        }

        /// <summary>
        /// Top-left position of the control in UI space.
        /// </summary>
        public Vector2 Position
        {
            get
            {
                return _position;
            }
            set
            {
                _position = value;
                UpdateBounds();
            }
        }

        /// <summary>
        /// Size of the control in UI space.
        /// </summary>
        public Vector2 Size
        {
            get
            {
                return _size;
            }
            set
            {
                _size = value;
                UpdateBounds();
            }
        }

        /// <summary>
        /// Rectangle used for hit testing in UI space.
        /// </summary>
        public Rectangle Bounds
        {
            get
            {
                return _bounds;
            }
        }

        public Color NormalColor
        {
            get
            {
                return _normalColor;
            }
            set
            {
                _normalColor = value;
                if (_currentState == UISelectionState.Normal)
                    ApplyState(_currentState);
            }
        }

        public Color HighlightedColor
        {
            get
            {
                return _highlightedColor;
            }
            set
            {
                _highlightedColor = value;
                if (_currentState == UISelectionState.Highlighted)
                    ApplyState(_currentState);
            }
        }

        public Color PressedColor
        {
            get
            {
                return _pressedColor;
            }
            set
            {
                _pressedColor = value;
                if (_currentState == UISelectionState.Pressed)
                    ApplyState(_currentState);
            }
        }

        public Color DisabledColor
        {
            get
            {
                return _disabledColor;
            }
            set
            {
                _disabledColor = value;
                if (_currentState == UISelectionState.Disabled)
                    ApplyState(_currentState);
            }
        }

        /// <summary>
        /// Update the bounding box for the ui element directly from target texture size
        /// </summary>
        public bool AutoSizeFromTargetGraphic
        {
            get
            {
                return _autoSizeFromTargetGraphic;
            }
            set
            {
                _autoSizeFromTargetGraphic = value;
                if (_autoSizeFromTargetGraphic)
                    RefreshSizeFromTargetGraphic();
            }
        }
        #endregion

        #region Constructors
        public UISelectable()
        {
            _transitionMode = UITransitionMode.ColorTint;
            _currentState = UISelectionState.Normal;
            _interactable = true;

            _normalColor = Color.White;
            _highlightedColor = new Color(230, 230, 230);
            _pressedColor = new Color(200, 200, 200);
            _disabledColor = new Color(128, 128, 128);

            _position = Vector2.Zero;
            _size = Vector2.Zero;
            _bounds = Rectangle.Empty;

            _autoSizeFromTargetGraphic = true;
        }
        #endregion

        #region Methods
        public virtual void OnPointerEnter(UIPointerEvent eventData)
        {
            _isPointerInside = true;

            if (!_interactable)
                return;

            if (_isPointerDown)
            {
                TransitionToState(UISelectionState.Pressed);
            }
            else
            {
                TransitionToState(UISelectionState.Highlighted);
            }
        }

        public virtual void OnPointerExit(UIPointerEvent eventData)
        {
            _isPointerInside = false;

            if (!_interactable)
                return;

            if (_isPointerDown)
            {
                TransitionToState(UISelectionState.Pressed);
            }
            else
            {
                TransitionToState(UISelectionState.Normal);
            }
        }

        public virtual void OnPointerDown(UIPointerEvent eventData)
        {
            if (!_interactable)
                return;

            _isPointerDown = true;
            TransitionToState(UISelectionState.Pressed);
        }

        public virtual void OnPointerUp(UIPointerEvent eventData)
        {
            if (!_interactable)
                return;

            _isPointerDown = false;
            TransitionToState(_isPointerInside ? UISelectionState.Highlighted : UISelectionState.Normal);
        }

        public virtual void OnSelect()
        {
            _hasFocus = true;

            if (!_interactable)
                return;

            if (!_isPointerInside)
                TransitionToState(UISelectionState.Highlighted);
        }

        public virtual void OnDeselect()
        {
            _hasFocus = false;

            if (!_interactable)
                return;

            TransitionToState(UISelectionState.Normal);
        }

        protected void TransitionToState(UISelectionState newState)
        {
            if (_currentState == newState)
                return;

            _currentState = newState;
            ApplyState(_currentState);
        }

        protected void ApplyState(UISelectionState state)
        {
            if (_transitionMode == UITransitionMode.None)
                return;

            if (_targetGraphic == null)
                return;

            Color tint = _normalColor;

            if (!_interactable || state == UISelectionState.Disabled)
            {
                tint = _disabledColor;
            }
            else
            {
                if (state == UISelectionState.Highlighted)
                    tint = _highlightedColor;
                else if (state == UISelectionState.Pressed)
                    tint = _pressedColor;
                else
                    tint = _normalColor;
            }

            // Basic ColorTint support:
            // If the target is a UITexture, use Tint. If it's UIText, use FallbackColor.
            var texture = _targetGraphic as UITexture;
            if (texture != null)
            {
                texture.Tint = tint;
                return;
            }

            var text = _targetGraphic as UIText;
            if (text != null)
            {
                text.FallbackColor = tint;
            }
        }

        protected void RefreshSizeFromTargetGraphic()
        {
            if (_targetGraphic == null)
                return;

            var texture = _targetGraphic as UITexture;
            if (texture != null && texture.Texture != null)
            {
                _size = new Vector2(texture.Texture.Width, texture.Texture.Height);
                UpdateBounds();
                return;
            }

            var text = _targetGraphic as UIText;
            if (text != null && text.Font != null && text.TextProvider != null)
            {
                string content = text.TextProvider();
                Vector2 measured = text.Font.MeasureString(content);
                _size = measured;
                UpdateBounds();
            }
        }

        protected void UpdateBounds()
        {
            _bounds = new Rectangle(
                (int)_position.X,
                (int)_position.Y,
                (int)_size.X,
                (int)_size.Y);
        }
        #endregion

        #region Lifecycle Methods
        protected override void Awake()
        {
            if (_targetGraphic == null)
                _targetGraphic = GameObject?.GetComponent<UIRenderer>();

            if (_autoSizeFromTargetGraphic)
                RefreshSizeFromTargetGraphic();

            ApplyState(_currentState);

            var scene = GameObject?.Scene;
            if (scene == null)
                return;

            var uiEventSystem = scene.GetSystem<UIEventSystem>();
            if (uiEventSystem != null)
                uiEventSystem.Add(this);
        }


        protected override void OnEnabled()
        {
            // Ensure we are registered with the UIEventSystem whenever we are enabled.
            var scene = GameObject?.Scene;
            if (scene != null)
            {
                var uiEventSystem = scene.GetSystem<UIEventSystem>();
                if (uiEventSystem != null)
                    uiEventSystem.Add(this);   // safe: Add() checks Contains before adding
            }

            if (_interactable)
                TransitionToState(_isPointerInside ? UISelectionState.Highlighted : UISelectionState.Normal);
            else
                TransitionToState(UISelectionState.Disabled);
        }

        protected override void OnDisabled()
        {
            _isPointerInside = false;
            _isPointerDown = false;

            var scene = GameObject?.Scene;
            if (scene == null)
                return;

            var uiEventSystem = scene.GetSystem<UIEventSystem>();
            if (uiEventSystem != null)
                uiEventSystem.Remove(this);
        }


        #endregion

        #region Housekeeping Methods
        // Add ToString / clone later if you wish.
        #endregion
    }
}
