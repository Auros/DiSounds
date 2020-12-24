using IPA;
using Zenject;
using SiraUtil;
using System.Linq;
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
        [Init]
        public Plugin(Conf conf, IPALogger log, Zenjector zenjector)
        {
            var config = conf.Generated<Config>();

            zenjector
                .On<PCAppInit>()
                .Pseudo(Container =>
                {
                    Container.BindLoggerAsSiraLogger(log);
                    Container.BindInstance(config).AsSingle();
                });

            zenjector
                .On<MenuInstaller>()
                .Register<SoundMenuInstaller>()
                .Pseudo((ctx, Container) =>
                {
                    // Wrapper
                    var binding = ctx.GetComponent<ZenjectBinding>();
                    var original = (binding.Components.FirstOrDefault(x => x is SongPreviewPlayer) as SongPreviewPlayer)!;
                    var fader = original.GetComponent<FadeOutSongPreviewPlayerOnSceneTransitionStart>();
                    var newPlayer = original.Upgrade<SongPreviewPlayer, DiSongPreviewPlayer>();

                    Container.QueueForInject(newPlayer);
                    Container.Unbind<SongPreviewPlayer>();
                    Container.Bind(typeof(SongPreviewPlayer), typeof(DiSongPreviewPlayer)).To<DiSongPreviewPlayer>().FromInstance(newPlayer).AsSingle();
                    fader.SetField<FadeOutSongPreviewPlayerOnSceneTransitionStart, SongPreviewPlayer>("_songPreviewPlayer", newPlayer);

                    Container.Bind<BasicUIAudioManager>().FromInstance(ctx.GetRootGameObjects().ElementAt(0).GetComponent<BasicUIAudioManager>()).AsSingle();
                });
        }

        [OnEnable, OnDisable]
        public void OnState() { /* On State */ }
    }
}