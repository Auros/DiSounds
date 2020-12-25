using HMUI;
using Zenject;
using System.IO;
using System.Linq;
using IPA.Utilities;
using DiSounds.Models;
using BeatSaberMarkupLanguage;
using System.Collections.Generic;

namespace DiSounds.UI
{
    internal class DisoFlowCoordinator : FlowCoordinator
    {
        private Config _config = null!;
        private DisoInfoView _disoInfoView = null!;
        private DisoAudioView _disoAudioView = null!;
        private MainFlowCoordinator _mainFlowCoordinator = null!;

        private DisoClickView _disoClickView = null!;

        private static readonly DirectoryInfo _clicksDir = new DirectoryInfo(Path.Combine(UnityGame.UserDataPath, "Di", "Sounds", "Clicks"));

        [Inject]
        protected void Construct(Config config, DisoInfoView disoInfoView, DisoAudioView disoAudioView, MainFlowCoordinator mainFlowCoordinator,
                                 DisoClickView disoClickView)
        {
            _config = config;
            _disoInfoView = disoInfoView;
            _disoAudioView = disoAudioView;
            _mainFlowCoordinator = mainFlowCoordinator;

            _disoClickView = disoClickView;
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            if (firstActivation)
            {
                SetTitle("DiSounds");
                showBackButton = true;
                ProvideInitialViewControllers(_disoInfoView);

            }
            _disoInfoView.ActionClicked += NavigationActionRequested;
        }

        private void NavigationActionRequested(Action action)
        {
            if (action == Action.MenuClicks)
            {
                ShowMenuClicksMenu();
                return;
            }
            SetLeftScreenViewController(null, ViewController.AnimationType.Out);
            SetRightScreenViewController(null, ViewController.AnimationType.Out);
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            _disoInfoView.ActionClicked -= NavigationActionRequested;
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
        }

        private void ShowMenuClicksMenu()
        {
            List<ClickPacket> packets = new List<ClickPacket>();
            foreach (var proj in _config.EnabledMenuClicks)
            {
                packets.Add(new ClickPacket(proj, true));
            }
            if (!_clicksDir.Exists) _clicksDir.Create();
            foreach (var file in _clicksDir.EnumerateFiles())
            {
                if (!packets.Any(p => p.File.FullName == file.FullName))
                {
                    packets.Add(new ClickPacket(file, false));
                }
            }
            SetLeftScreenViewController(_disoAudioView, ViewController.AnimationType.In);
            SetRightScreenViewController(_disoClickView, ViewController.AnimationType.In);
            _disoAudioView.Present(packets);
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            _mainFlowCoordinator.DismissFlowCoordinator(this);
        }

        public enum Action
        {
            None,
            MusicPlayer,
            MenuClicks,
            Intro,
        }
    }
}