using GDEngine.Core.Components;
using GDEngine.Core.Events;
using GDEngine.Core.Systems;
using Microsoft.Xna.Framework;

namespace GDEngine.Core.Rendering.UI
{
    /// <summary>
    /// Unity-style horizontal slider control built on <see cref="UISelectable"/>.
    /// Uses a track (TargetGraphic) and an optional handle <see cref="UITexture"/> to represent its value.
    /// </summary>
    /// <see cref="UISelectable"/>
    public class UISlider : UISelectable, IUIPointerClickHandler
    {
        #region Fields
        private float _minValue;
        private float _maxValue;
        private float _value;
        private bool _wholeNumbers;
        private bool _isDragging;

        private UITexture? _handleGraphic;

        private Action<float>? _onValueChanged;
        #endregion

        #region Properties
        /// <summary>
        /// Minimum slider value.
        /// </summary>
        public float MinValue
        {
            get
            {
                return _minValue;
            }
            set
            {
                _minValue = value;
                if (_maxValue < _minValue)
                    _maxValue = _minValue;

                SetValueInternal(_value, true, false);
            }
        }

        /// <summary>
        /// Maximum slider value.
        /// </summary>
        public float MaxValue
        {
            get
            {
                return _maxValue;
            }
            set
            {
                _maxValue = value;
                if (_maxValue < _minValue)
                    _minValue = _maxValue;

                SetValueInternal(_value, true, false);
            }
        }

        /// <summary>
        /// Current slider value in [MinValue, MaxValue].
        /// </summary>
        public float Value
        {
            get
            {
                return _value;
            }
            set
            {
                SetValueInternal(value, true, true);
            }
        }

        /// <summary>
        /// Whether the slider snaps to whole-number values.
        /// </summary>
        public bool WholeNumbers
        {
            get
            {
                return _wholeNumbers;
            }
            set
            {
                _wholeNumbers = value;
                SetValueInternal(_value, true, false);
            }
        }

        /// <summary>
        /// Normalized slider value in [0,1].
        /// </summary>
        public float NormalizedValue
        {
            get
            {
                if (Math.Abs(_maxValue - _minValue) < float.Epsilon)
                    return 0f;

                return (_value - _minValue) / (_maxValue - _minValue);
            }
            set
            {
                float clamped = MathHelper.Clamp(value, 0f, 1f);
                float v = MathHelper.Lerp(_minValue, _maxValue, clamped);
                SetValueInternal(v, true, true);
            }
        }

        /// <summary>
        /// Optional handle graphic. If assigned, its position will be updated to reflect the current value.
        /// Must be a <see cref="UITexture"/>.
        /// </summary>
        public UITexture? HandleGraphic
        {
            get
            {
                return _handleGraphic;
            }
            set
            {
                _handleGraphic = value;
                UpdateHandlePosition();
            }
        }

        /// <summary>
        /// Raised whenever <see cref="Value"/> changes.
        /// </summary>
        public event Action<float> ValueChanged
        {
            add
            {
                _onValueChanged += value;
            }
            remove
            {
                _onValueChanged -= value;
            }
        }
        #endregion

        #region Constructors
        public UISlider()
        {
            _minValue = 0f;
            _maxValue = 1f;
            _value = 0.5f;
            _wholeNumbers = false;
            _isDragging = false;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Sets the slider value without invoking <see cref="ValueChanged"/>.
        /// </summary>
        public void SetValueWithoutNotify(float value)
        {
            SetValueInternal(value, true, false);
        }

        /// <summary>
        /// Handles pointer click on the slider track.
        /// </summary>
        public void OnPointerClick(UIPointerEvent eventData)
        {
            if (!Interactable)
                return;

            UpdateValueFromPointer(eventData.Position.X, true);
        }

        /// <summary>
        /// Override pointer down to start dragging and update value immediately.
        /// </summary>
        public override void OnPointerDown(UIPointerEvent eventData)
        {
            base.OnPointerDown(eventData);

            if (!Interactable)
                return;

            _isDragging = true;
            UpdateValueFromPointer(eventData.Position.X, true);
        }

        /// <summary>
        /// Override pointer up to stop dragging.
        /// </summary>
        public override void OnPointerUp(UIPointerEvent eventData)
        {
            base.OnPointerUp(eventData);

            _isDragging = false;
        }

        private void SetValueInternal(float input, bool clamp, bool sendCallback)
        {
            float v = input;

            if (clamp)
            {
                if (_minValue <= _maxValue)
                    v = MathHelper.Clamp(v, _minValue, _maxValue);
                else
                    v = MathHelper.Clamp(v, _maxValue, _minValue);
            }

            if (_wholeNumbers)
                v = (float)Math.Round(v);

            if (Math.Abs(_value - v) < float.Epsilon)
                return;

            _value = v;
            UpdateHandlePosition();

            if (sendCallback)
                _onValueChanged?.Invoke(_value);
        }

        private void UpdateValueFromPointer(float pointerX, bool sendCallback)
        {
            Rectangle track = GetTrackRectForHitTesting();
            if (track.Width <= 1)
                return;

            float t = (pointerX - track.Left) / (float)track.Width;
            t = MathHelper.Clamp(t, 0f, 1f);

            float v = MathHelper.Lerp(_minValue, _maxValue, t);
            SetValueInternal(v, true, sendCallback);
        }

        private void UpdateHandlePosition()
        {
            if (_handleGraphic == null)
                return;

            Rectangle track = GetTrackRectForHitTesting();
            if (track.Width <= 0 || track.Height <= 0)
                return;

            Vector2 handleSize = _handleGraphic.Size;
            if (handleSize.X <= 0f || handleSize.Y <= 0f)
                return;

            float t = NormalizedValue;

            float trackLeft = track.Left;
            float usableWidth = track.Width - handleSize.X;
            if (usableWidth < 0f)
                usableWidth = 0f;

            float handleX = trackLeft + (t * usableWidth);
            float handleY = track.Top + (track.Height - handleSize.Y) * 0.5f;

            _handleGraphic.Position = new Vector2(handleX, handleY);
        }


        public Rectangle GetTrackRectForHitTesting()
        {
            var uiTexture = TargetGraphic as UITexture;
            if (uiTexture != null && uiTexture.Size.X > 0f && uiTexture.Size.Y > 0f)
                return uiTexture.DestinationRectangle;

            return Bounds;
        }

        #endregion

        #region Lifecycle Methods
        protected override void Awake()
        {
            base.Awake();

            // Try to infer handle from children if not explicitly assigned
            if (_handleGraphic == null && Transform != null)
            {
                for (int i = 0; i < Transform.ChildCount; i++)
                {
                    var child = Transform.Children[i];
                    var handle = child.GameObject?.GetComponent<UITexture>();
                    if (handle != null)
                    {
                        _handleGraphic = handle;
                        break;
                    }
                }
            }

            UpdateHandlePosition();
        }

        protected override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            if (!_isDragging)
                return;

            var scene = GameObject?.Scene;
            if (scene == null)
                return;

            var uiEventSystem = scene.GetSystem<UIEventSystem>();
            if (uiEventSystem == null)
                return;

            Vector2 pointer = uiEventSystem.PointerPosition;
            UpdateValueFromPointer(pointer.X, true);
        }
        #endregion

        #region Housekeeping Methods
        // Add ToString or cloning later if needed.
        #endregion
    }
}
