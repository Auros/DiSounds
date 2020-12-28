using Zenject;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;

namespace DiSounds.UI
{
    [ViewDefinition("DiSounds.Views.click-view.bsml")]
    [HotReload(RelativePathToLayout = @"..\Views\click-view.bsml")]
    internal class DisoClickView : BSMLAutomaticViewController
    {
        [Inject]
        protected readonly Config _config = null!;

        [UIValue("default-click")]
        protected bool DefaultClick
        {
            get => _config.UseMenuDefault;
            set => _config.UseMenuDefault = value;
        }

        [UIValue("random-pitch")]
        protected bool RandomPitch
        {
            get => _config.DoMenuPitch;
            set
            {
                _config.DoMenuPitch = value;
                NotifyPropertyChanged(nameof(ShowPitches));
            }
        }

        [UIValue("show-pitches")]
        protected bool ShowPitches => RandomPitch;

        [UIValue("min-pitch")]
        protected float MinPitch
        {
            get => _config.MinPitch;
            set => _config.MinPitch = value;
        }

        [UIValue("max-pitch")]
        protected float MaxPitch
        {
            get => _config.MaxPitch;
            set => _config.MaxPitch = value;
        }

        [UIAction("percent-formatter")]
        protected string PercentFormatter(float val)
        {
            return val.ToString("P2");
        }
    }
}