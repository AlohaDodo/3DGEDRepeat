using GDEngine.Core.Components;
using GDEngine.Core.Entities;
using GDEngine.Core.Enums;
using GDEngine.Core.Events;
using GDEngine.Core.Rendering.UI;
using GDEngine.Core.Timing;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace GDEngine.Core.Systems
{
    /// <summary>
    /// Centralizes pointer handling and dispatches pointer events to UI elements:
    /// - Tracks hover, press, and click using mouse input.
    /// - Performs hit tests against UISelectable.Bounds.
    /// - Delivers OnPointerEnter/Exit/Down/Up/Click and OnSubmit style events.
    /// 
    /// Intended to be the only place that calls Mouse.GetState for UI.
    /// </summary>
    /// <see cref="UISelectable"/>
    /// <see cref="UIButton"/>
    public sealed class UIEventSystem : SystemBase
    {
        #region Static Fields
        // None at present.
        #endregion

        #region Fields
        private Scene _scene = null!;
        private readonly List<UISelectable> _selectables = new List<UISelectable>();
        private readonly List<UISelectable> _allSelectables = new List<UISelectable>(16);
        private readonly List<UISelectable> _activeSelectables = new List<UISelectable>(16);
        private bool _needsRebuildActiveList;

        private MouseState _previousMouseState;
        private MouseState _currentMouseState;

        private Vector2 _pointerPosition;
        private bool _leftPressedLastFrame;

        private UISelectable? _currentHovered;
        private UISelectable? _currentPressed;
        #endregion

        #region Properties
        public Vector2 PointerPosition
        {
            get
            {
                return _pointerPosition;
            }
        } 
        #endregion


        #region Constructors
        public UIEventSystem(int order = 0)
            : base(FrameLifecycle.Update, order)
        {
        }
        #endregion

        #region Methods
        /// <summary>
        /// Registers a selectable with this event system.
        /// </summary>
        public void Add(UISelectable selectable)
        {
            if (selectable == null)
                throw new ArgumentNullException(nameof(selectable));

            if (_selectables.Contains(selectable))
                return;

            _selectables.Add(selectable);
        }

        /// <summary>
        /// Unregisters a selectable from this event system.
        /// </summary>
        public void Remove(UISelectable selectable)
        {
            if (selectable == null)
                return;

            _selectables.Remove(selectable);

            if (_currentHovered == selectable)
                _currentHovered = null;

            if (_currentPressed == selectable)
                _currentPressed = null;
        }

        private void OnSelectableEnabledChanged(Component component, bool enabled)
        {
            _needsRebuildActiveList = true;
        }

        private void RebuildActiveList()
        {
            _activeSelectables.Clear();

            for (int i = 0; i < _allSelectables.Count; i++)
            {
                if (_allSelectables[i].Enabled)
                    _activeSelectables.Add(_allSelectables[i]);
            }

            _needsRebuildActiveList = false;
        }

        /// <summary>
        /// Returns the top-most UISelectable under the given pointer position, if any.
        /// Uses the target graphic's LayerDepth as a z-sort key (lower = in front).
        /// </summary>
        private UISelectable? HitTest(Vector2 pointerPosition)
        {
            UISelectable? best = null;
            float bestDepth = float.MaxValue;

            for (int i = 0; i < _selectables.Count; i++)
            {
                var selectable = _selectables[i];

                // Ignore disabled controls and non-interactable ones
                if (!selectable.Enabled || !selectable.Interactable)
                    continue;

                Rectangle hitRect;

                if (selectable is UISlider slider)
                {
                    hitRect = slider.GetTrackRectForHitTesting();
                }
                else
                {
                    hitRect = selectable.Bounds;
                }

                if (!hitRect.Contains((int)pointerPosition.X, (int)pointerPosition.Y))
                    continue;

                float depth = 0.5f;
                if (selectable.TargetGraphic != null)
                    depth = selectable.TargetGraphic.LayerDepth;

                if (best == null || depth < bestDepth)
                {
                    best = selectable;
                    bestDepth = depth;
                }
            }

            return best;
        }


        private static UIPointerEvent CreatePointerEvent(Vector2 pointerPosition)
        {
            return new UIPointerEvent(pointerPosition, UIPointerButton.Left);
        }
        private static void SendPointerEnter(UISelectable selectable, UIPointerEvent data)
        {
            if (selectable is IUIPointerEnterHandler enterHandler)
                enterHandler.OnPointerEnter(data);
        }
        private static void SendPointerExit(UISelectable selectable, UIPointerEvent data)
        {
            if (selectable is IUIPointerExitHandler exitHandler)
                exitHandler.OnPointerExit(data);
        }
        private static void SendPointerDown(UISelectable selectable, UIPointerEvent data)
        {
            if (selectable is IUIPointerDownHandler downHandler)
                downHandler.OnPointerDown(data);
        }
        private static void SendPointerUp(UISelectable selectable, UIPointerEvent data)
        {
            if (selectable is IUIPointerUpHandler upHandler)
                upHandler.OnPointerUp(data);
        }
        private static void SendPointerClick(UISelectable selectable, UIPointerEvent data)
        {
            if (selectable is IUIPointerClickHandler clickHandler)
                clickHandler.OnPointerClick(data);
        }
        #endregion

        #region Lifecycle Methods
        protected override void OnAdded()
        {
            if (Scene == null)
                throw new NullReferenceException(nameof(Scene));

            _scene = Scene;

            _previousMouseState = Mouse.GetState();
            _currentMouseState = _previousMouseState;
            _pointerPosition = new Vector2(_currentMouseState.X, _currentMouseState.Y);
            _leftPressedLastFrame = _currentMouseState.LeftButton == ButtonState.Pressed;
        }

        public override void Update(float deltaTime)
        {
           // System.Diagnostics.Debug.WriteLine($"UIEventSystem::Update at {Time.RealtimeSinceStartupSecs}");

          //  if (_needsRebuildActiveList)
                RebuildActiveList();

            _previousMouseState = _currentMouseState;
            _currentMouseState = Mouse.GetState();

            _pointerPosition.X = _currentMouseState.X;
            _pointerPosition.Y = _currentMouseState.Y;

            bool leftDown = _currentMouseState.LeftButton == ButtonState.Pressed;
            bool leftPressedThisFrame = leftDown && !_leftPressedLastFrame;
            bool leftReleasedThisFrame = !leftDown && _leftPressedLastFrame;

            _leftPressedLastFrame = leftDown;

            UISelectable? hit = HitTest(_pointerPosition);
            var eventData = CreatePointerEvent(_pointerPosition);

            // Pointer enter / exit
            if (hit != _currentHovered)
            {
                if (_currentHovered != null)
                    SendPointerExit(_currentHovered, eventData);

                if (hit != null)
                    SendPointerEnter(hit, eventData);

                _currentHovered = hit;
            }

            // Pointer down
            if (leftPressedThisFrame)
            {
                if (hit != null)
                {
                    _currentPressed = hit;
                    SendPointerDown(hit, eventData);
                }
                else
                {
                    _currentPressed = null;
                }
            }

            // Pointer up + click
            if (leftReleasedThisFrame)
            {
                if (_currentPressed != null)
                {
                    SendPointerUp(_currentPressed, eventData);

                    // Only fire click if we released over the same selectable we pressed.
                    if (hit == _currentPressed)
                        SendPointerClick(_currentPressed, eventData);
                }

                _currentPressed = null;
            }
        }
        #endregion
    }
}
