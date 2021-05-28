using IPA;
using Zenject;
using SiraUtil;
using IPA.Loader;
using HarmonyLib;
using System.Linq;
using UnityEngine;
using IPA.Utilities;
using SiraUtil.Zenject;
using IPA.Config.Stores;
using DiSounds.Installers;
using DiSounds.Components;
using SiraUtil.Attributes;
using Conf = IPA.Config.Config;
using IPALogger = IPA.Logging.Logger;

namespace DiSounds
{
    [Plugin(RuntimeOptions.DynamicInit), Slog]
    public class Plugin
    {
        private readonly Harmony _harmony;
        private const string _harmonyID = "dev.auros.disounds";

        [Init]
        public Plugin(Conf conf, IPALogger log, Zenjector zenjector, PluginMetadata metadata)
        {
            var config = conf.Generated<Config>();
            _harmony = new Harmony(_harmonyID);
            config.Version = metadata.Version;

            zenjector
                .On<PCAppInit>()
                .Pseudo(Container =>
                {
                    log?.Debug("Initializing Core Bindings");
                    Container.BindLoggerAsSiraLogger(log);
                    Container.BindInstance(config).AsSingle();
                });

            zenjector.OnMenu<DisoMenuInstaller>()
                .Pseudo((ctx, Container) =>
                {
                    if (config.MusicPlayerEnabled)
                    {
                        log?.Debug("Upgrading to our DiPlayer");
                        SongPreviewPlayer original = null!;
                        foreach (var go in ctx.gameObject.scene.GetRootGameObjects())
                        {
                            original = go.GetComponent<SongPreviewPlayer>();
                            if (original != null)
                                break;
                        }

                        var binding = original.GetComponent<ZenjectBinding>();
                        var fader = original.GetComponent<FadeOutSongPreviewPlayerOnSceneTransitionStart>();
                        var focus = original.GetComponent<SongPreviewPlayerPauseOnInputFocusLost>();
                        var newPlayer = original.Upgrade<SongPreviewPlayer, DisoPreviewPlayer>();

                        Container.QueueForInject(newPlayer);
                        Container.Unbind<SongPreviewPlayer>();
                        Container.Bind(typeof(SongPreviewPlayer), typeof(DisoPreviewPlayer)).To<DisoPreviewPlayer>().FromInstance(newPlayer).AsSingle();
                        fader.SetField<FadeOutSongPreviewPlayerOnSceneTransitionStart, AudioPlayerBase>("_songPreviewPlayer", newPlayer);
                        focus.SetField<SongPreviewPlayerPauseOnInputFocusLost, AudioPlayerBase>("_songPreviewPlayer", newPlayer);
                    }
                });
            zenjector
                .On<ColorManagerInstaller>()
                .Register<CommonSoundInstaller>()
                .Pseudo((ctx, Container) =>
                {
                    log?.Debug("Exposing UI Audio Manager");
                    foreach (var rootObject in ctx.GetRootGameObjects())
                    {
                        var audioManager = rootObject.GetComponentInChildren<BasicUIAudioManager>();
                        if (audioManager != null)
                        {
                            Container.Bind<BasicUIAudioManager>().FromInstance(audioManager).AsSingle();
                            break;
                        }
                    }
                })
                .Initialized((ctx, Container) =>
                {
                    var manager = Container.ResolveId<SiraUtil.Interfaces.IRegistrar<AudioClip>>(nameof(Managers.DiClickManager));
                });
        }

        [OnEnable]
        public void OnEnable()
        {
            _harmony.PatchAll();
        }

        [OnDisable]
        public void OnDisable()
        {
            _harmony.UnpatchAll(_harmonyID);
        }
    }
}