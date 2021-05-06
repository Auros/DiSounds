using HMUI;
using System;
using Zenject;
using System.Linq;
using UnityEngine;
using IPA.Utilities;
using SiraUtil.Tools;
using UnityEngine.UI;
using System.Threading;
using UnityEngine.Events;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DiSounds.Managers
{
    internal class OutroSoundManager : IInitializable, IDisposable
    {
        private bool _didQuit;
        private readonly Config _config;
        private readonly SiraLog _siraLog;
        private readonly System.Random _random;
        private readonly AudioSource _audioSourcer;
        private readonly SongPreviewPlayer _songPreviewPlayer;
        private readonly FadeInOutController _fadeInOutController;
        private readonly IAudioClipAsyncLoader _audioClipAsyncLoader;
        private readonly MainMenuViewController _mainMenuViewController;
        private static readonly PropertyAccessor<ViewController, ButtonBinder>.Getter Bind = PropertyAccessor<ViewController, ButtonBinder>.GetGetter("buttonBinder");
        private static readonly FieldAccessor<MainMenuViewController, Button>.Accessor Quit = FieldAccessor<MainMenuViewController, Button>.GetAccessor("_quitButton");
        private static readonly FieldAccessor<ButtonBinder, List<Tuple<Button, UnityAction>>>.Accessor BindingList = FieldAccessor<ButtonBinder, List<Tuple<Button, UnityAction>>>.GetAccessor("_bindings");

        public OutroSoundManager(Config config, SiraLog siraLog, [Inject(Id = "audio.sourcer")] AudioSource audioSourcer, SongPreviewPlayer songPreviewPlayer, FadeInOutController fadeInOutController, CachedMediaAsyncLoader cachedMediaAsyncLoader, MainMenuViewController mainMenuViewController)
        {
            _config = config;
            _siraLog = siraLog;
            _audioSourcer = audioSourcer;
            _random = new System.Random();
            _songPreviewPlayer = songPreviewPlayer;
            _fadeInOutController = fadeInOutController;
            _audioClipAsyncLoader = cachedMediaAsyncLoader;
            _mainMenuViewController = mainMenuViewController;
        }

        public void Initialize()
        {
            _mainMenuViewController.didActivateEvent += MainMenu_Activated;
        }

        private void MainMenu_Activated(bool firstActivation, bool _, bool __)
        {
            if (firstActivation)
            {
                _siraLog.Debug("Menu Activating");
                var mainView = _mainMenuViewController;
                var view = mainView as ViewController;
                var quitButton = Quit(ref mainView);
                var binder = Bind(ref view);

                _siraLog.Debug("Replacing Binder");
                quitButton.onClick.RemoveAllListeners();
                var list = BindingList(ref binder).ToList();
                var quitBinding = list.First(b => b.Item1 == quitButton);

                list.Remove(quitBinding);
                binder.AddBinding(quitButton, LoadThenPlayOutroThenQuit);
                BindingList(ref binder) = list;
            }
        }

        private void LoadThenPlayOutroThenQuit()
        {
            if (_didQuit)
                return;
            _didQuit = true;
            _ = LoadThenPlayOutroThenQuitAsync();
        }

        private async Task LoadThenPlayOutroThenQuitAsync()
        {
            if (!_config.OutroSoundsEnabled || _config.EnabledOutroSounds.Count == 0)
            {
                _siraLog.Debug("No Outro Audio Set. Quitting Immediately");
                _fadeInOutController.FadeOutInstant();
                Application.Quit();
                return;
            }

            try
            {
                var outro = _config.EnabledOutroSounds[_random.Next(0, _config.EnabledOutroSounds.Count)];
                _siraLog.Debug($"Loading {outro.FullName}");
                var audioClip = await _audioClipAsyncLoader.LoadAudioClipAsync(outro.FullName, CancellationToken.None);
                _songPreviewPlayer.FadeOut(0.5f);
                _audioSourcer.clip = audioClip;
                _audioSourcer.Play();
                _fadeInOutController.FadeOut(audioClip.length);
                await SiraUtil.Utilities.AwaitSleep((int)(audioClip.length * 1000));
                if (_audioSourcer.clip == audioClip)
                {
                    _audioSourcer.clip = null;
                }
                Application.Quit();
            }
            catch (Exception e)
            {
                _siraLog.Error(e.Message);
                // If anything goes wrong, we do not want an SAO incident.
                Application.Quit();
            }
        }

        public void Dispose()
        {
            _mainMenuViewController.didActivateEvent -= MainMenu_Activated;
        }
    }
}
