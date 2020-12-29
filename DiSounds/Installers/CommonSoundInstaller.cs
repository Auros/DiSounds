using Zenject;
using UnityEngine;
using DiSounds.Managers;
using SiraUtil.Interfaces;

namespace DiSounds.Installers
{
    internal class CommonSoundInstaller : Installer
    {
        public override void InstallBindings()
        {
            var config = Container.Resolve<Config>();

            Container.Bind<IRegistrar<AudioClip>>().WithId(nameof(DiClickManager)).To<DiClickManager>().AsSingle();
        }
    }
}