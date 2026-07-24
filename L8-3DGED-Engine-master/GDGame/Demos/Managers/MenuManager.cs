#nullable enable
using System;
using GDEngine.Core.Entities;
using GDEngine.Core.Rendering;
using GDEngine.Core.Rendering.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GDGame.Demos.Managers
{
    /// <summary>
    /// High-level menu controller that creates and manages three UI panels:
    /// 1) Main menu (Play, Audio, Controls, Exit).
    /// 2) Audio menu (Music + SFX sliders, Back).
    /// 3) Controls menu (controls layout texture + Back).
    /// 
    /// This class is a MonoGame <see cref="GameComponent"/> so it
    /// can live alongside a <c>SceneManager : DrawableGameComponent</c>
    /// and configure a dedicated "menu scene" that is separate from
    /// gameplay scenes.
    /// </summary>
    /// <see cref="Scene"/>
    /// <see cref="UIMenuPanel"/>
    /// <see cref="UIButton"/>
    /// <see cref="UISlider"/>
    /// <see cref="UITexture"/>
    public sealed class MenuManager : GameComponent
    {
        #region Static Fields
        #endregion

        #region Fields
        private Scene? _menuScene;

        // Panels
        private UIMenuPanel? _mainMenuPanel;
        private UIMenuPanel? _audioMenuPanel;
        private UIMenuPanel? _controlsMenuPanel;

        // Main menu buttons
        private UIButton? _playButton;
        private UIButton? _audioButton;
        private UIButton? _controlsButton;
        private UIButton? _exitButton;

        // Audio menu controls
        private UIButton? _audioBackButton;
        private UISlider? _musicSlider;
        private UISlider? _sfxSlider;

        // Controls menu controls
        private UIButton? _controlsBackButton;
        private UITexture? _controlsLayoutTexture;

        // Assets
        private Texture2D? _buttonTexture;
        private Texture2D? _sliderTrackTexture;
        private Texture2D? _sliderHandleTexture;
        private Texture2D? _controlsLayout;
        private SpriteFont? _font;

        private bool _configured;
        private bool _built;
        #endregion

        #region Properties
        /// <summary>
        /// Raised when the user presses the Play button on the main menu.
        /// The game should subscribe and start gameplay / unpause.
        /// </summary>
        public event Action? PlayRequested;

        /// <summary>
        /// Raised when the user presses the Exit button on the main menu.
        /// The game (or a higher-level system) should subscribe and call Game.Exit().
        /// </summary>
        public event Action? ExitRequested;

        /// <summary>
        /// Raised when the Music slider value changes (0-1 by default).
        /// </summary>
        public event Action<float>? MusicVolumeChanged;

        /// <summary>
        /// Raised when the SFX slider value changes (0-1 by default).
        /// </summary>
        public event Action<float>? SfxVolumeChanged;
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a <see cref="MenuManager"/> as a MonoGame <see cref="GameComponent"/>.
        /// Add this to <c>Game.Components</c> in your Game subclass.
        /// </summary>
        public MenuManager(Game game)
            : base(game)
        {
        }
        #endregion

        #region Methods
        /// <summary>
        /// Configure the manager with:
        /// - The dedicated menu <see cref="Scene"/> (separate from gameplay scenes).
        /// - Textures and font for the UI.
        /// 
        /// Once called, the manager builds the three menu panels into the menu scene.
        /// </summary>
        public void Initialize(
            Scene menuScene,
            Texture2D buttonTexture,
            Texture2D sliderTrackTexture,
            Texture2D sliderHandleTexture,
            Texture2D controlsLayoutTexture,
            SpriteFont font)
        {
            if (menuScene == null)
                throw new ArgumentNullException(nameof(menuScene));
            if (buttonTexture == null)
                throw new ArgumentNullException(nameof(buttonTexture));
            if (sliderTrackTexture == null)
                throw new ArgumentNullException(nameof(sliderTrackTexture));
            if (sliderHandleTexture == null)
                throw new ArgumentNullException(nameof(sliderHandleTexture));
            if (controlsLayoutTexture == null)
                throw new ArgumentNullException(nameof(controlsLayoutTexture));
            if (font == null)
                throw new ArgumentNullException(nameof(font));

            _menuScene = menuScene;
            _buttonTexture = buttonTexture;
            _sliderTrackTexture = sliderTrackTexture;
            _sliderHandleTexture = sliderHandleTexture;
            _controlsLayout = controlsLayoutTexture;
            _font = font;

            _configured = true;

            TryBuildMenus();
        }

        /// <summary>
        /// Show the main menu and hide the other panels.
        /// This assumes the menu scene is currently active in your SceneManager.
        /// </summary>
        public void ShowMainMenu()
        {
            if (_mainMenuPanel == null ||
                _audioMenuPanel == null ||
                _controlsMenuPanel == null)
                return;

            SetActivePanel(_mainMenuPanel, _audioMenuPanel, _controlsMenuPanel);
        }

        /// <summary>
        /// Show the audio menu and hide the other panels.
        /// </summary>
        public void ShowAudioMenu()
        {
            if (_mainMenuPanel == null ||
                _audioMenuPanel == null ||
                _controlsMenuPanel == null)
                return;

            SetActivePanel(_audioMenuPanel, _mainMenuPanel, _controlsMenuPanel);
        }

        /// <summary>
        /// Show the controls menu and hide the other panels.
        /// </summary>
        public void ShowControlsMenu()
        {
            if (_mainMenuPanel == null ||
                _audioMenuPanel == null ||
                _controlsMenuPanel == null)
                return;

            SetActivePanel(_controlsMenuPanel, _mainMenuPanel, _audioMenuPanel);
        }

        private void TryBuildMenus()
        {
            if (_built)
                return;

            if (!_configured)
                return;

            if (_menuScene == null)
                return;

            if (_buttonTexture == null ||
                _sliderTrackTexture == null ||
                _sliderHandleTexture == null ||
                _controlsLayout == null ||
                _font == null)
                return;

            BuildPanels(_menuScene);
            _built = true;

            ShowMainMenu();
        }

        private void BuildPanels(Scene scene)
        {
            // Basic layout: top-left-ish anchor + consistent item size
            Vector2 panelPosition = new Vector2(100f, 100f);
            Vector2 itemSize = new Vector2(260f, 64f);
            float spacing = 12f;

            // Main menu panel
            GameObject mainRoot = new GameObject("UI_MainMenuPanel");
            scene.Add(mainRoot);

            _mainMenuPanel = mainRoot.AddComponent<UIMenuPanel>();
            _mainMenuPanel.PanelPosition = panelPosition;
            _mainMenuPanel.ItemSize = itemSize;
            _mainMenuPanel.VerticalSpacing = spacing;
            _mainMenuPanel.IsVisible = true;

            _playButton = _mainMenuPanel.AddButton(
                "Play",
                _buttonTexture!,
                _font!,
                OnPlayClicked);

            _audioButton = _mainMenuPanel.AddButton(
                "Audio",
                _buttonTexture!,
                _font!,
                OnAudioClicked);

            _controlsButton = _mainMenuPanel.AddButton(
                "Controls",
                _buttonTexture!,
                _font!,
                OnControlsClicked);

            _exitButton = _mainMenuPanel.AddButton(
                "Exit",
                _buttonTexture!,
                _font!,
                OnExitClicked);

            // Audio menu panel
            GameObject audioRoot = new GameObject("UI_AudioMenuPanel");
            scene.Add(audioRoot);

            _audioMenuPanel = audioRoot.AddComponent<UIMenuPanel>();
            _audioMenuPanel.PanelPosition = panelPosition;
            _audioMenuPanel.ItemSize = itemSize;
            _audioMenuPanel.VerticalSpacing = spacing;
            _audioMenuPanel.IsVisible = false;

            _musicSlider = _audioMenuPanel.AddSlider(
                "Music",
                _sliderTrackTexture!,
                _sliderHandleTexture!,
                _font!,
                0f,
                1f,
                0.8f,
                OnMusicSliderChanged);

            _sfxSlider = _audioMenuPanel.AddSlider(
                "SFX",
                _sliderTrackTexture!,
                _sliderHandleTexture!,
                _font!,
                0f,
                1f,
                0.8f,
                OnSfxSliderChanged);

            _audioBackButton = _audioMenuPanel.AddButton(
                "Back",
                _buttonTexture!,
                _font!,
                OnBackToMainFromAudio);

            // -----------------------------------------------------------------
            // Controls menu panel
            // -----------------------------------------------------------------
            GameObject controlsRoot = new GameObject("UI_ControlsMenuPanel");
            scene.Add(controlsRoot);

            _controlsMenuPanel = controlsRoot.AddComponent<UIMenuPanel>();
            _controlsMenuPanel.PanelPosition = panelPosition;
            _controlsMenuPanel.ItemSize = itemSize;
            _controlsMenuPanel.VerticalSpacing = spacing;
            _controlsMenuPanel.IsVisible = false;

            // Controls layout image (full row, slightly taller)
            GameObject controlsImageGO = new GameObject("ControlsLayout");
            scene.Add(controlsImageGO);
            controlsImageGO.Transform.SetParent(_controlsMenuPanel.Transform);

            _controlsLayoutTexture = controlsImageGO.AddComponent<UITexture>();
            _controlsLayoutTexture.Texture = _controlsLayout!;
            _controlsLayoutTexture.Size = new Vector2(itemSize.X * 1.5f, itemSize.Y * 2.0f);
            _controlsLayoutTexture.Position = panelPosition + new Vector2(0f, 0f);
            _controlsLayoutTexture.Tint = Color.White;
            _controlsLayoutTexture.LayerDepth = UILayer.Menu;

            _controlsBackButton = _controlsMenuPanel.AddButton(
                "Back",
                _buttonTexture!,
                _font!,
                OnBackToMainFromControls);

            _controlsMenuPanel.RefreshChildren();
        }

        private static void SetActivePanel(
            UIMenuPanel toShow,
            UIMenuPanel toHideA,
            UIMenuPanel toHideB)
        {
            toShow.IsVisible = true;
            toHideA.IsVisible = false;
            toHideB.IsVisible = false;
        }

        private void OnPlayClicked()
        {
            PlayRequested?.Invoke();
        }

        private void OnAudioClicked()
        {
            ShowAudioMenu();
        }

        private void OnControlsClicked()
        {
            ShowControlsMenu();
        }

        private void OnExitClicked()
        {
            ExitRequested?.Invoke();
        }

        private void OnBackToMainFromAudio()
        {
            ShowMainMenu();
        }

        private void OnBackToMainFromControls()
        {
            ShowMainMenu();
        }

        private void OnMusicSliderChanged(float value)
        {
            MusicVolumeChanged?.Invoke(value);
        }

        private void OnSfxSliderChanged(float value)
        {
            SfxVolumeChanged?.Invoke(value);
        }
        #endregion

        #region Lifecycle Methods
        public override void Update(GameTime gameTime)
        {
            // As a manager, this does not drive any Scene updates itself.
            // The DrawableGameComponent SceneManager should be responsible
            // for calling menuScene.Update/Draw and choosing which scene
            // is currently active.
            if (!_built && _configured)
                TryBuildMenus();

            base.Update(gameTime);
        }
        #endregion

        #region Housekeeping Methods
        public override string ToString()
        {
            return "UIManager(MenuScene=" + (_menuScene?.Name ?? "null") + ", Built=" + (_built ? "true" : "false") + ")";
        }
        #endregion
    }
}
