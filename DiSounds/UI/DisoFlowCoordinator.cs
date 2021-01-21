using HMUI;
using Zenject;
using System.IO;
using System.Linq;
using UnityEngine;
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
        private HighwayTutorialSystem _highwayTutorialSystem = null!;

        private DisoClickView _disoClickView = null!;
        private DisoMusicView _disoMusicView = null!;

        private static readonly DirectoryInfo _musicDir = new DirectoryInfo(Path.Combine(UnityGame.UserDataPath, "Di", "Sounds", "Music"));
        private static readonly DirectoryInfo _introDir = new DirectoryInfo(Path.Combine(UnityGame.UserDataPath, "Di", "Sounds", "Intro"));
        private static readonly DirectoryInfo _outroDir = new DirectoryInfo(Path.Combine(UnityGame.UserDataPath, "Di", "Sounds", "Outro"));
        private static readonly DirectoryInfo _clicksDir = new DirectoryInfo(Path.Combine(UnityGame.UserDataPath, "Di", "Sounds", "Clicks"));

        [Inject]
        protected void Construct(Config config, SiraLog siraLog, DisoInfoView disoInfoView, DisoAudioView disoAudioView, MainFlowCoordinator mainFlowCoordinator,
                                 FadeInOutController fadeInOutController, MenuTransitionsHelper menuTransitionsHelper, HighwayTutorialSystem highwayTutorialSystem,
                                 DisoClickView disoClickView, DisoMusicView disoMusicView)
        {
            _config = config;
            _siraLog = siraLog;
            _disoInfoView = disoInfoView;
            _disoAudioView = disoAudioView;
            _mainFlowCoordinator = mainFlowCoordinator;
            _fadeInOutController = fadeInOutController;
            _menuTransitionHelper = menuTransitionsHelper;
            _highwayTutorialSystem = highwayTutorialSystem;

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
            _highwayTutorialSystem.BlossomHappened += Instruction;
            await SiraUtil.Utilities.PauseChamp;
            if (_config.FirstTime)
            {
                await SiraUtil.Utilities.AwaitSleep(1000);
                _config.FirstTime = false;
                _disoInfoView.Tutorial();
            }
            _disoInfoView.MenuValue = Action.None;
            _config.Updated += ConfigUpdated;

        }

        private void Instruction(HighwayTutorialSystem.Blossom instruction)
        {
            // oversight
            if (instruction.Text.Contains("music player"))
            {
                ShowTutorialAudioMenu();
                return;
            }
            if (instruction.Text.StartsWith("That's all!"))
            {
                NavigationActionRequested(Action.None);
            }
        }

        private void ConfigUpdated(Config _)
        {
            _configUpdated = true;
        }

        private void NavigationActionRequested(Action action)
        {
            if (action != Action.None && _highwayTutorialSystem.Active)
            {
                ShowTutorialAudioMenu();
                return;
            }
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
            if (action == Action.Intro)
            {
                ShowIntroSoundsMenu();
                return;
            }
            if (action == Action.Outro)
            {
                ShowOutroSoundsMenu();
                return;
            }
            if (action == Action.OutroFC)
            {
                ShowOutroFCSoundsMenu();
                return;
            }
            if (action == Action.Tutorial)
            {
                if (!_highwayTutorialSystem.Active)
                {
                    _highwayTutorialSystem.Add(new HighwayTutorialSystem.Blossom("Welcome To DiSounds! Hit the next arrow below to continue.", new Vector3(0f, 1.5f, 2f), Quaternion.identity));
                    _highwayTutorialSystem.Add(new HighwayTutorialSystem.Blossom("This is the dashboard. Use it to enable/disable different modes.", new Vector3(1.4f, 1.9f, 2.2f), EulY(30), true));
                    _highwayTutorialSystem.Add(new HighwayTutorialSystem.Blossom("This is the settings window. Use it to navigate to different settings.", new Vector3(1.4f, 0.55f, 2.2f), EulY(30), true));
                    _highwayTutorialSystem.Add(new HighwayTutorialSystem.Blossom("This is the music player. It shows all the loaded clips for the active mode.", new Vector3(-2f, 2.45f, 1.1f), EulY(300)));
                    _highwayTutorialSystem.Add(new HighwayTutorialSystem.Blossom("This is the enabled status of a clip.", new Vector3(-2.3f, 1.6f, 0.875f), EulY(300), true));
                    _highwayTutorialSystem.Add(new HighwayTutorialSystem.Blossom("This is the file name of a clip.", new Vector3(-2f, 1.60f, 1.5f), EulY(300), true));
                    _highwayTutorialSystem.Add(new HighwayTutorialSystem.Blossom("This is the action button. Use it to enable/disable a clip.", new Vector3(-1.75f, 1.6f, 1.92f), EulY(310), true));
                    _highwayTutorialSystem.Add(new HighwayTutorialSystem.Blossom("This is the preview button. Use it to preview a clip.", new Vector3(-1.7f, 1.6f, 2.1f), EulY(310), true));
                    _highwayTutorialSystem.Add(new HighwayTutorialSystem.Blossom("Over here, any mode specific settings will appear.", new Vector3(2f, 2.45f, 1.1f), EulY(60)));
                    _highwayTutorialSystem.Add(new HighwayTutorialSystem.Blossom("You can learn where to add new audio clips here.", new Vector3(-0.4f, 0.95f, 2.5f), Quaternion.identity, true));
                    _highwayTutorialSystem.Add(new HighwayTutorialSystem.Blossom("If you want to start clean, reset everything here.", new Vector3(0.3f, 0.95f, 2.5f), Quaternion.identity, true));
                    _highwayTutorialSystem.Add(new HighwayTutorialSystem.Blossom("That's all! Enjoy the mod!", new Vector3(0f, 1.5f, 2f), Quaternion.identity));
                    _highwayTutorialSystem.Enable();
                }
                return;
            }
            SetLeftScreenViewController(null, ViewController.AnimationType.Out);
            SetRightScreenViewController(null, ViewController.AnimationType.Out);
        }

        private Quaternion EulY(float euly) => Quaternion.Euler(new Vector3(0f, euly, 0f));

        private void ShowTutorialAudioMenu()
        {
            SetLeftScreenViewController(_disoAudioView, ViewController.AnimationType.In);
            SetRightScreenViewController(null, ViewController.AnimationType.Out);
            _disoAudioView.For = "Tutorial";
            List<TutorialPacket> packets = new List<TutorialPacket>
            {
                new TutorialPacket("Example.ogg", true),
                new TutorialPacket("Disabled Audio.ogg", false)
            };
            _disoAudioView.Present(packets);
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            _config.Updated -= ConfigUpdated;
            _highwayTutorialSystem.BlossomHappened -= Instruction;
            _disoInfoView.ActionClicked -= NavigationActionRequested;
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            _highwayTutorialSystem.Disable();
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

        private void ShowIntroSoundsMenu()
        {
            List<IntroPacket> packets = new List<IntroPacket>();
            foreach (var proj in _config.EnabledIntroSounds)
            {
                packets.Add(new IntroPacket(proj, true));
            }
            if (!_introDir.Exists) _introDir.Create();
            foreach (var file in _introDir.EnumerateFiles())
            {
                if (!packets.Any(p => p.File.FullName == file.FullName))
                {
                    if (file.Extension == ".ogg")
                        packets.Add(new IntroPacket(file, false));
                }
            }
            SetLeftScreenViewController(_disoAudioView, ViewController.AnimationType.In);
            SetRightScreenViewController(null, ViewController.AnimationType.Out);
            _disoAudioView.For = "Intro Sounds";
            _disoAudioView.Present(packets);
        }

        private void ShowOutroSoundsMenu()
        {
            List<OutroPacket> packets = new List<OutroPacket>();
            foreach (var proj in _config.EnabledOutroSounds)
            {
                packets.Add(new OutroPacket(proj, true));
            }
            if (!_outroDir.Exists) _outroDir.Create();
            foreach (var file in _outroDir.EnumerateFiles())
            {
                if (!packets.Any(p => p.File.FullName == file.FullName))
                {
                    if (file.Extension == ".ogg")
                        packets.Add(new OutroPacket(file, false));
                }
            }
            SetLeftScreenViewController(_disoAudioView, ViewController.AnimationType.In);
            SetRightScreenViewController(null, ViewController.AnimationType.Out);
            _disoAudioView.For = "Outro";
            _disoAudioView.Present(packets);
        }

        private void ShowOutroFCSoundsMenu()
        {
            List<OutroPacket> packets = new List<OutroPacket>();
            foreach (var proj in _config.EnabledOutroFCSounds)
            {
                packets.Add(new OutroPacket(proj, true));
            }
            if (!_outroDir.Exists) _outroDir.Create();
            foreach (var file in _outroDir.EnumerateFiles())
            {
                if (!packets.Any(p => p.File.FullName == file.FullName))
                {
                    if (file.Extension == ".ogg")
                        packets.Add(new OutroPacket(file, false));
                }
            }
            SetLeftScreenViewController(_disoAudioView, ViewController.AnimationType.In);
            SetRightScreenViewController(null, ViewController.AnimationType.Out);
            _disoAudioView.For = "Outro (Full Combo)";
            _disoAudioView.Present(packets);
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            if (_configUpdated)
            {
                _fadeInOutController.FadeOut(0.35f, delegate () { _menuTransitionHelper.RestartGame(); });
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
            Outro,
            OutroFC,

            Reset,
            Tutorial,
            NoTutorial
        }
    }
}