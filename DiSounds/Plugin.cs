using IPA;
using Zenject;
using SiraUtil;
using IPA.Loader;
using System.Linq;
using IPA.Utilities;
using SiraUtil.Zenject;
using IPA.Config.Stores;
using DiSounds.Installers;
using DiSounds.Components;
using SiraUtil.Attributes;
using Conf = IPA.Config.Config;
using IPALogger = IPA.Logging.Logger;
using UnityEngine;

namespace DiSounds
{
    [Plugin(RuntimeOptions.DynamicInit), Slog]
    public class Plugin
    {
        [Init]
        public Plugin(Conf conf, IPALogger log, Zenjector zenjector, PluginMetadata metadata)
        {
            var config = conf.Generated<Config>();
            config.Version = metadata.Version;

            zenjector
                .On<PCAppInit>()
                .Pseudo(Container =>
                {
                    log?.Debug("Initializing Core Bindings");
                    Container.BindLoggerAsSiraLogger(log);
                    Container.BindInstance(config).AsSingle();
                });

            zenjector.OnMenu<DisoMenuInstaller>();
            zenjector
                .On<MenuInstaller>()
                .Register<CommonSoundInstaller>()
                .Pseudo((ctx, Container) =>
                {
                    log?.Debug("Upgrading to our DiPlayer");
                    var binding = ctx.GetComponent<ZenjectBinding>();
                    var original = (binding.Components.FirstOrDefault(x => x is SongPreviewPlayer) as SongPreviewPlayer)!;
                    var fader = original.GetComponent<FadeOutSongPreviewPlayerOnSceneTransitionStart>();
                    var newPlayer = original.Upgrade<SongPreviewPlayer, DisoPreviewPlayer>();

                    Container.QueueForInject(newPlayer);
                    Container.Unbind<SongPreviewPlayer>();
                    Container.Bind(typeof(SongPreviewPlayer), typeof(DisoPreviewPlayer)).To<DisoPreviewPlayer>().FromInstance(newPlayer).AsSingle();
                    fader.SetField<FadeOutSongPreviewPlayerOnSceneTransitionStart, SongPreviewPlayer>("_songPreviewPlayer", newPlayer);

                    log?.Debug("Exposing UI Audio Manager");
                    var am = ctx.GetRootGameObjects().ElementAt(0).GetComponentInChildren<BasicUIAudioManager>();
                    Container.Bind<BasicUIAudioManager>().FromInstance(am).AsSingle();

                    var gameObject = new GameObject("Audio Sourcer");
                    var mixer = am.GetField<AudioSource, BasicUIAudioManager>("_audioSource").outputAudioMixerGroup;
                    var clone = gameObject.AddComponent<AudioSource>();
                    clone.outputAudioMixerGroup = mixer;
                    Container.BindInstance(clone).WithId("audio.sourcer").AsSingle();
                })
                .Initialized((ctx, Container) =>
                {
                    var manager = Container.ResolveId<SiraUtil.Interfaces.IRegistrar<UnityEngine.AudioClip>>(nameof(Managers.DiClickManager));
                });

        }

        [OnEnable, OnDisable]
        public void OnState() { /* On State */ }
    }
}