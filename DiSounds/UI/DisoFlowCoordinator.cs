using HMUI;
using Zenject;
using System.IO;
using System.Linq;
using IPA.Utilities;
using SiraUtil.Tools;
using DiSounds.Models;
using BeatSaberMarkupLanguage;
using System.Collections.Generic;

namespace DiSounds.UI
{
    internal class DisoFlowCoordinator : FlowCoordinator
    {
        private bool _configUpdated;
        private Config _config = null!;
        private SiraLog _siraLog = null!;
        private DisoInfoView _disoInfoView = null!;
        private DisoAudioView _disoAudioView = null!;
        private FadeInOutController _fadeInOutController = null!;
        private MainFlowCoordinator _mainFlowCoordinator = null!;
        private MenuTransitionsHelper _menuTransitionHelper = null!;

        private DisoClickView _disoClickView = null!;
        private DisoMusicView _disoMusicView = null!;

        private static readonly DirectoryInfo _musicDir = new DirectoryInfo(Path.Combine(UnityGame.UserDataPath, "Di", "Sounds", "Music"));
        private static readonly DirectoryInfo _clicksDir = new DirectoryInfo(Path.Combine(UnityGame.UserDataPath, "Di", "Sounds", "Clicks"));

        [Inject]
        protected void Construct(Config config, SiraLog siraLog, DisoInfoView disoInfoView, DisoAudioView disoAudioView, MainFlowCoordinator mainFlowCoordinator,
                                 FadeInOutController fadeInOutController, MenuTransitionsHelper menuTransitionsHelper,
                                 DisoClickView disoClickView, DisoMusicView disoMusicView)
        {
            _config = config;
            _siraLog = siraLog;
            _disoInfoView = disoInfoView;
            _disoAudioView = disoAudioView;
            _mainFlowCoordinator = mainFlowCoordinator;
            _fadeInOutController = fadeInOutController;
            _menuTransitionHelper = menuTransitionsHelper;

            _disoClickView = disoClickView;
            _disoMusicView = disoMusicView;
        }

        protected override async void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            if (firstActivation)
            {
                SetTitle("DiSounds");
                showBackButton = true;
                ProvideInitialViewControllers(_disoInfoView);
            }
            _disoInfoView.ActionClicked += NavigationActionRequested;
            await SiraUtil.Utilities.PauseChamp;
            _config.Updated += ConfigUpdated;
        }

        private void ConfigUpdated(Config _)
        {
            _configUpdated = true;
        }

        private void NavigationActionRequested(Action action)
        {
            if (action == Action.MenuClicks)
            {
                ShowMenuClicksMenu();
                return;
            }
            if (action == Action.MusicPlayer)
            {
                ShowMusicPlayer();
                return;
            }
            SetLeftScreenViewController(null, ViewController.AnimationType.Out);
            SetRightScreenViewController(null, ViewController.AnimationType.Out);
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            _config.Updated -= ConfigUpdated;
            _disoInfoView.ActionClicked -= NavigationActionRequested;
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
        }

        private void ShowMusicPlayer()
        {
            List<MusicPacket> packets = new List<MusicPacket>();
            foreach (var proj in _config.EnabledMusicFiles)
            {
                packets.Add(new MusicPacket(proj, true));
            }
            if (!_musicDir.Exists) _musicDir.Create();
            foreach (var file in _musicDir.EnumerateFiles())
            {
                if (!packets.Any(p => p.File.FullName == file.FullName))
                {
                    if (file.Extension == ".ogg")
                        packets.Add(new MusicPacket(file, false));
                }
            }
            SetLeftScreenViewController(_disoAudioView, ViewController.AnimationType.In);
            SetRightScreenViewController(_disoMusicView, ViewController.AnimationType.Out);
            _disoAudioView.For = "Music Player";
            _disoAudioView.Present(packets);
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
                    if (file.Extension == ".ogg")
                        packets.Add(new ClickPacket(file, false));
                }
            }
            SetLeftScreenViewController(_disoAudioView, ViewController.AnimationType.In);
            SetRightScreenViewController(_disoClickView, ViewController.AnimationType.In);
            _disoAudioView.For = "Menu Clicks";
            _disoAudioView.Present(packets);
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            if (_configUpdated)
            {
                _fadeInOutController.FadeOut(0.25f, delegate () { _menuTransitionHelper.RestartGame(); });
                return;
            }
            _mainFlowCoordinator.DismissFlowCoordinator(this);
        }

        public enum Action
        {
            None,
            MusicPlayer,
            MenuClicks,
            Intro,

            Reset,
            Tutorial
        }
    }
}