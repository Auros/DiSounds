using System;
using Zenject;
using System.Linq;
using System.Text;
using static DiSounds.Config;
using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;

namespace DiSounds.UI
{
    [ViewDefinition("DiSounds.Views.music-view.bsml")]
    [HotReload(RelativePathToLayout = @"..\Views\music-view.bsml")]
    internal class DisoMusicView : BSMLAutomaticViewController
    {
        [Inject]
        protected readonly Config _config = null!;

        [UIValue("save-time")]
        protected bool SaveTime
        {
            get => _config.SaveTime;
            set => _config.SaveTime = value;
        }

        [UIValue("playback-types")]
        protected List<object> Sensitivities => ((PlaybackType[])Enum.GetValues(typeof(PlaybackType))).Cast<object>().ToList();

        [UIValue("playback-mode")]
        protected PlaybackType PlaybackMode
        {
            get => _config.MusicSource;
            set => _config.MusicSource = value;
        }

        [UIAction("format")]
        protected string Format(PlaybackType type)
        {
            char[] chars = ((PlaybackType)Enum.Parse(typeof(PlaybackType), type.ToString())).ToString().ToArray<char>();
            StringBuilder builtFormat = new StringBuilder();
            for (int i = 0; i < chars.Length; i++)
            {
                if (i == 0)
                {
                    builtFormat.Append(chars[i].ToString().ToUpper());
                    continue;
                }
                if (chars[i].ToString().ToUpper() == chars[i].ToString())
                {
                    builtFormat.Append(" ");
                }
                builtFormat.Append(chars[i]);
            }
            return builtFormat.ToString();
        }
    }
}