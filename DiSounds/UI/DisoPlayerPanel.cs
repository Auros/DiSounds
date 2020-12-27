using HMUI;
using System;
using Zenject;
using UnityEngine;
using VRUIControls;
using IPA.Utilities;
using BeatSaberMarkupLanguage.Attributes;
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
        internal static readonly FieldAccessor<ImageView, float>.Accessor ImageSkew = FieldAccessor<ImageView, float>.GetAccessor("_skew");

        public bool Enabled { get; private set; }

        public event Action? Paused;
        public event Action? Resumed;
        public event Action? MoveNext;
        public event Action? MovePrevious;
        public event Action<float>? VolumeChanged;

        private Sprite _playSprite = null!;
        private Sprite _pauseSprite = null!;

        private float _songLength = 0f;
        private float _currentSongTime = 0f;

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
            _playSprite = BeatSaberMarkupLanguage.Utilities.FindSpriteInAssembly("DiSounds.Resources.play.png");
            _pauseSprite = BeatSaberMarkupLanguage.Utilities.FindSpriteInAssembly("DiSounds.Resources.pause.png");
            _kawaseBlurRenderer = levelPackDetailViewController.GetField<KawaseBlurRendererSO, LevelPackDetailViewController>("_kawaseBlurRenderer");
            _floatingScreen = FloatingScreen.CreateFloatingScreen(new Vector2(100f, 15f), false, Vector3.zero, Quaternion.identity);
            _floatingScreen.GetComponent<VRGraphicRaycaster>().SetField("_physicsRaycaster", raycaster);
            _floatingScreen.transform.SetParent(gameplaySetupViewController.transform, false);
            _floatingScreen.transform.localPosition = new Vector3(3f, 50f);
            _floatingScreen.transform.localScale = Vector3.one;
            _floatingScreen.gameObject.SetActive(false);
            _floatingScreen.gameObject.SetActive(true);
            _floatingScreen.name = "DisoPlayer";
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

        public void SetPlayer(string name, float clipLength, Texture texture, bool playing = true)
        {
            CoverHint = name;
            _songLength = clipLength;
            var blurTex = _kawaseBlurRenderer.Blur(texture, KawaseBlurRendererSO.KernelSize.Kernel63, 2);
            coverArt.sprite = Sprite.Create(blurTex, new Rect(0f, 0f, blurTex.width, blurTex.height), new Vector2(0.5f, 0.5f), 1024 >> 2, 0U, SpriteMeshType.FullRect, Vector4.zero, false);
            playPauseImage.sprite = playing ? _pauseSprite : _playSprite;
        }

        public void SetTime(float time)
        {
            CurrentSongTime = time;

        }

        #region Events

        [UIAction("next-click")] protected void Next() => MoveNext?.Invoke();
        [UIAction("prev-click")] protected void Prev() => MovePrevious?.Invoke();

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
                Paused?.Invoke();
            }
            else
            {
                playPauseImage.sprite = _playSprite;
                Resumed?.Invoke();
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
        protected int Scrub => _songLength == 0 ? -30 : (int)Mathf.Lerp(-30f, 30f, _currentSongTime / _songLength);

        [UIValue("time-text")]
        protected string TimeText => $"{string.Format("{0}:{1:00}", CurrentSongTime / 60, CurrentSongTime % 60)} / {string.Format("{0}:{1:00}", _songLength / 60, _songLength % 60)}";

        #endregion

        #region Components

        [UIComponent("cover-art")]
        protected ImageView coverArt = null!;

        [UIComponent("slider")]
        protected SliderSetting slider = null!;

        [UIComponent("play-pause")]
        protected ImageView playPauseImage = null!;
        
        [UIComponent("passive-bar")]
        protected ImageView passiveBar = null!;

        [UIComponent("active-bar")]
        protected ImageView activeBar = null!;

        #endregion
    }
}