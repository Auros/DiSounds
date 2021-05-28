using Zenject;
using SiraUtil;
using System.Linq;
using DiSounds.UI;
using DiSounds.Managers;
using UnityEngine;

namespace DiSounds.Installers
{
    internal class DisoMenuInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesTo<MenuButtonManager>().AsSingle();
            Container.Bind<DisoInfoView>().FromNewComponentAsViewController().AsSingle();
            Container.Bind<DisoAudioView>().FromNewComponentAsViewController().AsSingle();
            Container.Bind<DisoClickView>().FromNewComponentAsViewController().AsSingle();
            Container.Bind<DisoMusicView>().FromNewComponentAsViewController().AsSingle();
            Container.Bind<HighwayTutorialSystem>().FromNewComponentAsViewController().AsSingle();
            Container.Bind<DisoFlowCoordinator>().FromNewComponentOnNewGameObject(nameof(DisoFlowCoordinator)).AsSingle();

            var config = Container.Resolve<Config>();
            Container.Bind<ResultsSoundManager>().AsSingle();
            Container.BindInterfacesTo<OutroSoundManager>().AsSingle();
            config.EnabledMusicFiles = config.EnabledMusicFiles.Where(f => f.FullName != null).GroupBy(f => f.FullName).Select(g => g.FirstOrDefault()).ToList();
            config.EnabledMenuClicks = config.EnabledMenuClicks.Where(f => f.FullName != null).GroupBy(f => f.FullName).Select(g => g.FirstOrDefault()).ToList();
            config.EnabledIntroSounds = config.EnabledIntroSounds.Where(f => f.FullName != null).GroupBy(f => f.FullName).Select(g => g.FirstOrDefault()).ToList();

            if (config.MusicPlayerEnabled && (config.MusicSource == Config.PlaybackType.CustomSongs || config.EnabledMusicFiles.Count > 0))
            {
                Container.BindInterfacesTo<DisoMusicPlayer>().AsSingle();
                Container.Bind<DisoPlayerPanel>().FromNewComponentAsViewController().AsSingle();
                switch (config.MusicSource)
                {
                    case Config.PlaybackType.CustomSongs:
                        Container.BindInterfacesTo<CustomSongPicker>().AsSingle();
                        break;
                    case Config.PlaybackType.CustomFiles:
                        Container.BindInterfacesTo<FileSongLoader>().AsSingle();
                        break;
                }
            }
            if (config.IntroSoundsEnabled)
            {
                Container.BindInterfacesAndSelfTo<IntroSoundManager>().AsSingle();
            }

            var manager = Container.Resolve<AudioManagerSO>();
            var gameObject = new GameObject("Audio Sourcer");
            var clone = gameObject.AddComponent<AudioSource>();
            clone.outputAudioMixerGroup = manager.masterMixerGroup;
            Container.BindInstance(clone).WithId("audio.sourcer").AsSingle();
        }
    }
}