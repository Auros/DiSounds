using TMPro;
using System;
using Zenject;
using UnityEngine;
using BeatSaberMarkupLanguage.Parser;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;

namespace DiSounds.UI
{
    [ViewDefinition("DiSounds.Views.info-view.bsml")]
    [HotReload(RelativePathToLayout = @"..\Views\info-view.bsml")]
    internal class DisoInfoView : BSMLAutomaticViewController
    {
        public event Action<DisoFlowCoordinator.Action>? ActionClicked;

        [Inject]
        protected readonly Config _config = null!;

        [Inject]
        protected readonly FadeInOutController fadeInOutController = null!;

        [Inject]
        protected readonly MenuTransitionsHelper menuTransitionsHelper = null!;

        #region BSML

        [UIParams]
        protected BSMLParserParams parserParams = null!;

        [UIComponent("info-window")]
        protected Backgroundable infoWindowBackground = null!;

        [UIAction("#post-parse")]
        protected void Parsed()
        {
            infoWindowBackground.background.material = BeatSaberMarkupLanguage.Utilities.ImageResources.NoGlowMat;
        }

        #endregion

        #region Info Window

        [UIValue("version")]
        protected string Version => $"Mod Version: {_config.Version}";

        #endregion

        #region Help Window

        [UIAction("faq")]
        protected void FAQ()
        {
            SetModal("" +
                "<b><u>How do I add new sounds?</u></b>\n" +
                "- Add the files into UserData/Di/Sounds/ and place them into the respective folder\n" +
                "\n" +
                "<b><u>What file types are supported?</u></b>\n" +
                "- Only .ogg files are supported for all audio files.\n"
                , delegate () { parserParams.EmitEvent("hide-yn"); } , TextAlignmentOptions.TopLeft);
            parserParams.EmitEvent("show-yn");
        }

        [UIAction("reset")]
        protected void Reset()
        {
            SetModal("Are you sure you want to reset your config?", delegate ()
            {
                var version = _config.Version;
                var config = new Config { Version = version };
                _config.CopyFrom(config);
                parserParams.EmitEvent("hide-yn");
                fadeInOutController.FadeOut(0.5f, delegate ()
                {
                    menuTransitionsHelper.RestartGame();
                });
            }, TextAlignmentOptions.Center);
            parserParams.EmitEvent("show-yn");
        }

        #endregion

        #region Extras Window

        [UIAction("github")]
        protected void GitHub() => URL("https://github.com/Auros/DiSounds");

        [UIAction("donate")]
        protected void Donate() => URL("https://ko-fi.com/aurosnex");

        protected void URL(string url)
        {
            SetModal($"This will open <color=#4e68de>{url}</color> in your default browser.", delegate ()
            {
                Application.OpenURL(url); 
                parserParams.EmitEvent("hide-yn"); 
            }, TextAlignmentOptions.Center);
            parserParams.EmitEvent("show-yn");
        }

        #endregion

        #region Navigation BSML

        [UIAction("clicked-player")]
        protected void ClickedPlayer() => ActionClicked?.Invoke(DisoFlowCoordinator.Action.MusicPlayer);

        [UIAction("clicked-clicks")]
        protected void ClickedClicks() => ActionClicked?.Invoke(DisoFlowCoordinator.Action.MenuClicks);

        [UIAction("clicked-intro")]
        protected void ClickedIntro() => ActionClicked?.Invoke(DisoFlowCoordinator.Action.Intro);

        #endregion

        #region Dashboard Window

        [UIValue("music-player")]
        protected bool MusicPlayerEnabled
        {
            get => _config.MusicPlayerEnabled;
            set => _config.MusicPlayerEnabled = value;
        }

        [UIValue("menu-clicks")]
        protected bool MenuClicksEnabled
        {
            get => _config.MenuClicksEnabled;
            set => _config.MenuClicksEnabled = value;
        }

        [UIValue("intro")]
        protected bool Intro
        {
            get => true;
            set => _ = value;
        }

        #endregion

        #region YN Modal

        private string _text = "No Content Set";
        [UIValue("yn-text")]
        protected string Text
        {
            get => _text;
            set
            {
                _text = value;
                NotifyPropertyChanged();
            }
        }

        private TextAlignmentOptions _alignment;
        [UIValue("alignment")]
        protected TextAlignmentOptions Alignment
        {
            get => _alignment;
            set
            {
                _alignment = value;
                NotifyPropertyChanged();
            }
        }

        protected Action? didOK;

        [UIAction("did-ok")]
        protected void DidOK()
        {
            didOK?.Invoke();
        }

        public void SetModal(string text, Action? onOK = null, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            Text = text;
            didOK = onOK;
            Alignment = alignment;
        }

        #endregion
    }
}