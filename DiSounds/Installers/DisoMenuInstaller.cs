using Zenject;
using SiraUtil;
using DiSounds.UI;
using DiSounds.Managers;

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
            Container.Bind<DisoFlowCoordinator>().FromNewComponentOnNewGameObject(nameof(DisoFlowCoordinator)).AsSingle();
        }
    }
}