using System;
using Zenject;
using DiSounds.UI;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.MenuButtons;

namespace DiSounds.Managers
{
    internal class MenuButtonManager : IInitializable, IDisposable
    {
        private readonly MenuButton _menuButton;
        private readonly DisoFlowCoordinator _disoFlowCoordinator;
        private readonly MainFlowCoordinator _mainFlowCoordinator;

        public MenuButtonManager(DisoFlowCoordinator disoFlowCoordinator, MainFlowCoordinator mainFlowCoordinator)
        {
            _disoFlowCoordinator = disoFlowCoordinator;
            _mainFlowCoordinator = mainFlowCoordinator;
            _menuButton = new MenuButton("DiSounds", ShowFlow);
        }

        public void Initialize() => MenuButtons.instance.RegisterButton(_menuButton);

        public void Dispose()
        {
            if (MenuButtons.IsSingletonAvailable && BSMLParser.IsSingletonAvailable)
            {
                MenuButtons.instance.UnregisterButton(_menuButton);
            }
        }

        private void ShowFlow()
        {
            _mainFlowCoordinator.PresentFlowCoordinator(_disoFlowCoordinator);
        }
    }
}