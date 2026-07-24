using System;
using System.Collections.Generic;
using GDEngine.Core.Components;
using GDEngine.Core.Entities;
using GDEngine.Core.Rendering.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GDEngine.Core.Rendering.UI
{
    /// <summary>
    /// Simple vertical menu panel that can create and manage Unity-style
    /// buttons and sliders as child UI controls.
    /// </summary>
    /// <see cref="UIButton"/>
    /// <see cref="UISlider"/>
    public sealed class UIMenuPanel : Component
    {
        #region Static Fields
        // None.
        #endregion

        #region Fields
        private readonly List<UIRenderer> _renderers = new List<UIRenderer>();
        private readonly List<UISelectable> _selectables = new List<UISelectable>();

        private Vector2 _panelPosition;
        private Vector2 _itemSize;
        private float _verticalSpacing;
        private int _itemCount;
        private bool _isVisible = true;
        #endregion

        #region Properties
        /// <summary>
        /// Top-left position of the first menu item in UI space.
        /// </summary>
        public Vector2 PanelPosition
        {
            get
            {
                return _panelPosition;
            }
            set
            {
                _panelPosition = value;
            }
        }

        /// <summary>
        /// Size of a single menu item (button or slider track) in UI space.
        /// </summary>
        public Vector2 ItemSize
        {
            get
            {
                return _itemSize;
            }
            set
            {
                _itemSize = value;
            }
        }

        /// <summary>
        /// Vertical spacing between menu items.
        /// </summary>
        public float VerticalSpacing
        {
            get
            {
                return _verticalSpacing;
            }
            set
            {
                _verticalSpacing = value;
            }
        }

        /// <summary>
        /// Whether this panel is currently visible. When disabled, all child
        /// renderers/selectables are disabled and do not receive input.
        /// </summary>
        public bool IsVisible
        {
            get
            {
                return _isVisible;
            }
            set
            {
                if (_isVisible == value)
                    return;

                _isVisible = value;
                UpdateChildrenEnabled();
            }
        }
        #endregion

        #region Constructors
        public UIMenuPanel()
        {
            _panelPosition = new Vector2(100f, 100f);
            _itemSize = new Vector2(200f, 64f);
            _verticalSpacing = 10f;
            _itemCount = 0;
        }
        #endregion

        #region Methods
        private Vector2 GetRowPosition(int index)
        {
            float x = _panelPosition.X;
            float y = _panelPosition.Y + index * (_itemSize.Y + _verticalSpacing);
            return new Vector2(x, y);
        }

        private void TrackCreatedControl(GameObject go)
        {
            if (go == null)
                return;

            var renderers = go.GetComponents<UIRenderer>();
            if (renderers != null)
                _renderers.AddRange(renderers);

            var selectables = go.GetComponents<UISelectable>();
            if (selectables != null)
                _selectables.AddRange(selectables);

            UpdateChildrenEnabled();
        }

        private void UpdateChildrenEnabled()
        {
            for (int i = 0; i < _renderers.Count; i++)
                _renderers[i].Enabled = _isVisible;

            for (int i = 0; i < _selectables.Count; i++)
                _selectables[i].Enabled = _isVisible;
        }

        /// <summary>
        /// Creates a Unity-style menu button (background + label + UIButton)
        /// as a child of this panel.
        /// </summary>
        public UIButton AddButton(
            string text,
            Texture2D buttonTexture,
            SpriteFont font,
            Action onClick,
            Color? textColor = null)
        {
            var scene = GameObject?.Scene;
            if (scene == null)
                throw new InvalidOperationException("UIMenuPanel must be attached to a GameObject that is added to a Scene.");

            int rowIndex = _itemCount;
            _itemCount++;

            Vector2 rowPos = GetRowPosition(rowIndex);

            GameObject go = new GameObject($"Button_{text}");
            scene.Add(go);
            go.Transform.SetParent(Transform);

            // Background graphic
            UITexture graphic = go.AddComponent<UITexture>();
            graphic.Texture = buttonTexture;
            graphic.Position = rowPos;
            graphic.Size = _itemSize;
            graphic.Tint = Color.White;
            graphic.LayerDepth = UILayer.Menu;

            // Button logic
            UIButton button = go.AddComponent<UIButton>();
            button.TargetGraphic = graphic;
            button.AutoSizeFromTargetGraphic = false;
            button.Position = rowPos;
            button.Size = _itemSize;

            button.NormalColor = Color.White;
            button.HighlightedColor = Color.LightGray;
            button.PressedColor = Color.Gray;
            button.DisabledColor = Color.DarkGray;

            button.Clicked += onClick;

            // Label
            UIText label = go.AddComponent<UIText>();
            label.Font = font;
            label.FallbackColor = textColor ?? Color.White;
            label.DropShadow = true;
            label.LayerDepth = UILayer.MenuFront;

            label.TextProvider = () => text;
            label.PositionProvider = () =>
            {
                return button.Position + (button.Size * 0.5f);
            };
            label.Anchor = TextAnchor.Center;
            label.UniformScale = 1f;
            label.Offset = Vector2.Zero;

            TrackCreatedControl(go);

            return button;
        }

        /// <summary>
        /// Creates a unity-style horizontal slider (track + handle + label)
        /// as a child of this panel.
        /// </summary>
        public UISlider AddSlider(
            string labelText,
            Texture2D trackTexture,
            Texture2D handleTexture,
            SpriteFont font,
            float minValue,
            float maxValue,
            float initialValue,
            Action<float> onValueChanged,
            Color? textColor = null)
        {
            var scene = GameObject?.Scene;
            if (scene == null)
                throw new InvalidOperationException("UIMenuPanel must be attached to a GameObject that is added to a Scene.");

            int rowIndex = _itemCount;
            _itemCount++;

            Vector2 rowPos = GetRowPosition(rowIndex);

            // Root object for slider row
            GameObject sliderRoot = new GameObject($"Slider_{labelText}");
            scene.Add(sliderRoot);
            sliderRoot.Transform.SetParent(Transform);

            // Track
            UITexture trackGraphic = sliderRoot.AddComponent<UITexture>();
            trackGraphic.Texture = trackTexture;
            trackGraphic.Position = rowPos + new Vector2(100f, 0f); // leave room on the left for label
            trackGraphic.Size = new Vector2(_itemSize.X - 100f, _itemSize.Y * 0.4f);
            trackGraphic.Tint = Color.White;
            trackGraphic.LayerDepth = UILayer.Menu;

            // Slider logic
            UISlider slider = sliderRoot.AddComponent<UISlider>();
            slider.TargetGraphic = trackGraphic;
            slider.AutoSizeFromTargetGraphic = false;
            slider.Position = trackGraphic.Position;
            slider.Size = trackGraphic.Size;
            slider.MinValue = minValue;
            slider.MaxValue = maxValue;
            slider.WholeNumbers = false;
            slider.Value = initialValue;
            slider.ValueChanged += onValueChanged;

            // Handle object
            GameObject handleObject = new GameObject($"SliderHandle_{labelText}");
            scene.Add(handleObject);
            handleObject.Transform.SetParent(sliderRoot.Transform);

            UITexture handleGraphic = handleObject.AddComponent<UITexture>();
            handleGraphic.Texture = handleTexture;
            handleGraphic.Size = new Vector2(24f, 24f);
            handleGraphic.Tint = Color.White;
            handleGraphic.LayerDepth = UILayer.MenuFront;

            slider.HandleGraphic = handleGraphic;

            // Label, to the left, vertically centered
            UIText label = sliderRoot.AddComponent<UIText>();
            label.Font = font;
            label.FallbackColor = textColor ?? Color.White;
            label.DropShadow = true;
            label.LayerDepth = UILayer.MenuFront;

            label.TextProvider = () => $"{labelText}: {slider.Value:0.00}";
            label.PositionProvider = () =>
            {
                Vector2 center = rowPos + new Vector2(50f, _itemSize.Y * 0.5f);
                return center;
            };
            label.Anchor = TextAnchor.Center;
            label.UniformScale = 1f;
            label.Offset = Vector2.Zero;

            TrackCreatedControl(sliderRoot);
            TrackCreatedControl(handleObject);

            return slider;
        }

        /// <summary>
        /// Clears internal lists and repopulates them from existing child UI components.
        /// Call this if you add UI controls via other code.
        /// </summary>
        public void RefreshChildren()
        {
            _renderers.Clear();
            _selectables.Clear();

            if (Transform == null)
                return;

            CollectRecursive(Transform);
            UpdateChildrenEnabled();
        }

        private void CollectRecursive(Transform root)
        {
            var renderers = root.GameObject?.GetComponents<UIRenderer>();
            if (renderers != null)
                _renderers.AddRange(renderers);

            var selectables = root.GameObject.GetComponents<UISelectable>();
            if (selectables != null)
                _selectables.AddRange(selectables);

            foreach (var child in root.Children)
                CollectRecursive(child);
        }
        #endregion

        #region Lifecycle Methods
        protected override void Awake()
        {
            base.Awake();
            RefreshChildren();
        }
        #endregion

        #region Housekeeping Methods
        // Add ToString, clone, etc. here later if needed.
        #endregion
    }
}
