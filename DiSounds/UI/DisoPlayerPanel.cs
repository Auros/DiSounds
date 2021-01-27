using HMUI;
using System;
using Zenject;
using UnityEngine;
using VRUIControls;
using IPA.Utilities;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.FloatingScreen;
using BeatSaberMarkupLanguage.ViewControllers;
using BeatSaberMarkupLanguage.Components.Settings;

namespace DiSounds.UI
{
    [ViewDefinition("DiSounds.Views.player-panel.bsml")]
    [HotReload(RelativePathToLayout = @"..\Views\player-panel.bsml")]
    internal class DisoPlayerPanel : BSMLAutomaticViewController
    {
        private FloatingScreen _floatingScreen = null!;
        private KawaseBlurRendererSO _kawaseBlurRenderer = null!;
        private GameplaySetupViewController _gameplaySetupViewController = null!;
        internal static readonly FieldAccessor<ImageView, float>.Accessor ImageSkew = FieldAccessor<ImageView, float>.GetAccessor("_skew");
        internal static readonly FieldAccessor<ClickableImage, bool>.Accessor Highlight = FieldAccessor<ClickableImage, bool>.GetAccessor("_isHighlighted");
        private static readonly FieldAccessor<ModalView, bool>.Accessor AnimateTheFuckingCanvasHolyShitWhatTheFuckHyperbolicMagnetismUserInterfaceLibrary = FieldAccessor<ModalView, bool>.GetAccessor("_animateParentCanvas");

        public bool Enabled { get; private set; }
        public bool Initialized => coverArt != null;

        public event Action? Paused;
        public event Action? Resumed;
        public event Action? MoveNext;
        public event Action? MovePrevious;
        public event Action<float>? VolumeChanged;

        private Sprite? _blurred;
        private Sprite? _original;
        private Sprite _playSprite = null!;
        private Sprite _pauseSprite = null!;

        private float _songLength = 0f;
        private float _currentSongTime = 0f;

        private Sprite _default = null!;

        protected float CurrentSongTime
        {
            get => _currentSongTime;
            set
            {
                _currentSongTime = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(Scrub));
                NotifyPropertyChanged(nameof(TimeText));
            }
        }

        [Inject]
        protected void Construct(PhysicsRaycasterWithCache raycaster, GameplaySetupViewController gameplaySetupViewController, LevelPackDetailViewController levelPackDetailViewController)
        {
            _gameplaySetupViewController = gameplaySetupViewController;
            _playSprite = BeatSaberMarkupLanguage.Utilities.FindSpriteInAssembly("DiSounds.Resources.play.png");
            _default = BeatSaberMarkupLanguage.Utilities.FindSpriteInAssembly("DiSounds.Resources.question.png");
            _pauseSprite = BeatSaberMarkupLanguage.Utilities.FindSpriteInAssembly("DiSounds.Resources.pause.png");
            _kawaseBlurRenderer = levelPackDetailViewController.GetField<KawaseBlurRendererSO, LevelPackDetailViewController>("_kawaseBlurRenderer");
            _floatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(100f, 15f), false, Vector3.zero, Quaternion.identity);
            _floatingScreen.GetComponent<VRGraphicRaycaster>().SetField("_physicsRaycaster", raycaster);
            _floatingScreen.transform.SetParent(_gameplaySetupViewController.transform, false);
            _floatingScreen.transform.localPosition = new Vector3(3f, 50f);
            _floatingScreen.transform.localScale = Vector3.one;
            _floatingScreen.gameObject.SetActive(false);
            _floatingScreen.gameObject.SetActive(true);
            _floatingScreen.name = "DisoPlayer";
        }

        protected void Start()
        {
            _gameplaySetupViewController.didActivateEvent += GameplaySetupActivated;
        }

        private void GameplaySetupActivated(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            if (_floatingScreen.transform.parent != _gameplaySetupViewController.transform)
            {
                _floatingScreen.transform.SetParent(_gameplaySetupViewController.transform, false);
                _floatingScreen.transform.localPosition = new Vector3(3f, 50f);
                _floatingScreen.transform.localScale = Vector3.one;
                _floatingScreen.gameObject.SetActive(false);
                _floatingScreen.gameObject.SetActive(true);
            }
        }

        protected override void OnDestroy()
        {
            _gameplaySetupViewController.didActivateEvent -= GameplaySetupActivated;
            base.OnDestroy();
        }

        public void Show()
        {
            _floatingScreen.SetRootViewController(this, AnimationType.In);
            Enabled = true;
        }

        public void Hide()
        {
            _floatingScreen.SetRootViewController(null, AnimationType.Out);
            Enabled = false;
        }

        public void SetPlayer(string name, float clipLength, Texture2D? texture, bool playing = true)
        {
            CoverHint = name;
            _songLength = clipLength;
            if (texture != null && coverArt != null)
            {
                texture.wrapMode = TextureWrapMode.Clamp;
                var blurTex = _kawaseBlurRenderer.Blur(texture, KawaseBlurRendererSO.KernelSize.Kernel35, 2);
                _original = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 1024 >> 2, 0U, SpriteMeshType.FullRect, Vector4.zero, false);
                _blurred = Sprite.Create(blurTex, new Rect(0f, 0f, blurTex.width, blurTex.height), new Vector2(0.5f, 0.5f), 1024 >> 2, 0U, SpriteMeshType.FullRect, Vector4.zero, false);
                coverArt.sprite = playing ? _original : _blurred;
            }
            else
            {
                _original = _blurred = _default;
                if (coverArt != null)
                {
                    coverArt.sprite = _original;
                }
            }
            if (playPauseImage != null)
            {
                playPauseImage.sprite = playing ? _pauseSprite : _playSprite;
            }
        }

        public void SetTime(float time)
        {
            CurrentSongTime = time;
        }

        #region Events

        [UIAction("next-click")]
        protected void Next()
        {
            Highlight(ref nextImage) = true;
            nextImage.InvokeMethod<object, ClickableImage>("UpdateHighlight");
            MoveNext?.Invoke();
        }

        [UIAction("prev-click")]
        protected void Prev()
        {
            Highlight(ref prevImage) = true;
            prevImage.InvokeMethod<object, ClickableImage>("UpdateHighlight");
            MovePrevious?.Invoke();
        }

        [UIAction("#post-parse")]
        protected void Parsed()
        {
            passiveBar.color = Color.gray;
            playPauseImage.sprite = _playSprite;

            var clo = slider.slider;
            var bg = clo.transform.GetChild(0).GetComponent<ImageView>();
            var area = clo.transform.GetChild(1);

            var handle = area.transform.GetChild(0).GetComponent<ImageView>();
            var value = area.transform.GetChild(1).GetComponent<CurvedTextMeshPro>();
            value.fontStyle = TMPro.FontStyles.Normal;
            ImageSkew(ref handle) = 0f;
            ImageSkew(ref bg) = 0f;

        }

        [UIAction("play-pause-click")]
        protected void PlayPause()
        {
            if (playPauseImage.sprite == _playSprite)
            {
                playPauseImage.sprite = _pauseSprite;
                coverArt.sprite = _original;
                Resumed?.Invoke();
            }
            else
            {
                playPauseImage.sprite = _playSprite;
                coverArt.sprite = _blurred;
                Paused?.Invoke();
            }
        }

        [UIAction("volume-update")]
        protected void VolumeUpdate(float val)
        {
            VolumeChanged?.Invoke(val);
        }

        [UIAction("percent-formatter")]
        protected string PercentFormatter(float val)
        {
            return val.ToString("P2");
        }

        protected void OnEnable()
        {
            canvasGroup.alpha = 1;
        }

        protected void OnDisable()
        {
            if (volumeModalRoot != null)
            {
                volumeModalRoot.gameObject.SetActive(false);
                AnimateTheFuckingCanvasHolyShitWhatTheFuckHyperbolicMagnetismUserInterfaceLibrary(ref volumeModalRoot) = true;
            }
        }

        #endregion

        #region Bindings

        private string _coverHint = "Nothing.";
        [UIValue("cover-hint")]
        protected string CoverHint
        {
            get => _coverHint;
            set
            {
                _coverHint = value;
                NotifyPropertyChanged();
            }
        }

        private float _volume;
        [UIValue("volume")]
        public float Volume
        {
            get => _volume;
            set
            {
                _volume = value;
                NotifyPropertyChanged(nameof(Volume));
            }
        }

        [UIValue("scrub-pos")]
        protected float Scrub => _songLength == 0 ? -30 : Mathf.Lerp(-30f, 30f, _currentSongTime / _songLength);

        [UIValue("time-text")]
        protected string TimeText => $"{string.Format("{0}:{1:00}", (int)(CurrentSongTime / 60), CurrentSongTime % 60)} / {string.Format("{0}:{1:00}", (int)(_songLength / 60), _songLength % 60)}";

        #endregion

        #region Components

        [UIComponent("volume-modal-root")]
        protected ModalView volumeModalRoot = null!;

        [UIComponent("cover-art")]
        protected ImageView coverArt = null!;

        [UIComponent("slider")]
        protected SliderSetting slider = null!;

        [UIComponent("play-pause")]
        protected ImageView playPauseImage = null!;

        [UIComponent("prev-image")]
        protected ClickableImage prevImage = null!;

        [UIComponent("next-image")]
        protected ClickableImage nextImage = null!;
        
        [UIComponent("passive-bar")]
        protected ImageView passiveBar = null!;

        [UIComponent("active-bar")]
        protected ImageView activeBar = null!;

        #endregion
    }
}