using Zenject;
using UnityEngine;
using DiSounds.Managers;
using SiraUtil.Interfaces;

namespace DiSounds.Installers
{
    internal class SoundMenuInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.Bind<IRegistrar<AudioClip>>().WithId(nameof(DiClickManager)).To<DiClickManager>().AsSingle();
        }
    }
}