using System;
using Zenject;
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

        #region BSML

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
    }
}