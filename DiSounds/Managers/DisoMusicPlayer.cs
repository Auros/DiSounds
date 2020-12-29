using System;
using Zenject;
using DiSounds.UI;
using UnityEngine;
using SiraUtil.Tools;
using DiSounds.Models;
using DiSounds.Components;
using DiSounds.Interfaces;
using BeatSaberMarkupLanguage;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using BeatSaberMarkupLanguage.Attributes;

namespace DiSounds.Managers
{
    internal class DisoMusicPlayer : IInitializable, ITickable, IDisposable
    {
        private bool _initialized;
        private bool _paused = false;
        private float _cycleTime = 0f;
        private readonly float _cycleLength = 0.5f;

        private readonly Config _config;
        private readonly SiraLog _siraLog;
        private readonly DisoPlayerPanel _disoPlayerPanel;
        private readonly Stack<AudioContainer> _playHistory;
        private readonly DisoPreviewPlayer _disoPreviewPlayer;
        private readonly IAudioContainerService _audioContainerService;
        private readonly GameplaySetupViewController _gameplaySetupViewController;
        private readonly StandardLevelDetailViewController _standardLevelDetailViewController;
        private const string _content = "<clickable-text id=\"root\" on-click=\"toggle\" text=\"💿\" align=\"Center\" anchor-pos-x=\"40\" anchor-pos-y=\"0.25\" size-delta-x=\"8\" default-color=\"#ffd630\" />";

        private Texture2D? _activeTexture;
        private string _activeName = "Unknown";

        public DisoMusicPlayer(Config config, SiraLog siraLog, DisoPlayerPanel disoPlayerPanel, DisoPreviewPlayer disoPreviewPlayer, IAudioContainerService audioContainerService,
                               GameplaySetupViewController gameplaySetupViewController, StandardLevelDetailViewController standardLevelDetailViewController)
        {
            _config = config;
            _siraLog = siraLog;
            _disoPlayerPanel = disoPlayerPanel;
            _disoPreviewPlayer = disoPreviewPlayer;
            _playHistory = new Stack<AudioContainer>();
            _audioContainerService = audioContainerService;
            _gameplaySetupViewController = gameplaySetupViewController;
            _standardLevelDetailViewController = standardLevelDetailViewController;
            BSMLParser.instance.Parse(_content, _gameplaySetupViewController.transform.Find("HeaderPanel").gameObject, this);
            root.name = "DisoPlayerToggle";
        }

        #region Kernel State

        public void Initialize()
        {
            _disoPlayerPanel.Paused += Paused;
            _disoPlayerPanel.Resumed += Resumed;
            _disoPlayerPanel.MoveNext += MoveNext;
            _audioContainerService.Loaded += LoadedAudio;
            _disoPlayerPanel.MovePrevious += MovePrevious;
            _disoPlayerPanel.VolumeChanged += VolumeChanged;
            SceneManager.activeSceneChanged += SceneChanged;
            _standardLevelDetailViewController.didPressActionButtonEvent += DidPressActionButton;

            _disoPlayerPanel.Volume = _disoPreviewPlayer.Volume;
        }

        private void LoadedAudio()
        {
            _initialized = true;
            MoveNext();
        }

        public void Tick()
        {
            if (!_config.MusicPlayerEnabled) return;

            _cycleTime += Time.deltaTime;
            if (_cycleTime >= _cycleLength)
            {
                _cycleTime = 0f;
                if (_disoPreviewPlayer.PlayingDefault)
                {
                    if (_disoPreviewPlayer.CurrentAudioTime + 0.5f > _disoPreviewPlayer.DefaultClip.length)
                    {
                        MoveNext();
                    }
                    _disoPlayerPanel.SetTime(_disoPreviewPlayer.CurrentAudioTime);
                }
            }
        }

        public void Dispose()
        {
            _disoPlayerPanel.Paused -= Paused;
            _disoPlayerPanel.Resumed -= Resumed;
            _disoPlayerPanel.MoveNext -= MoveNext;
            _audioContainerService.Loaded -= LoadedAudio;
            _disoPlayerPanel.MovePrevious -= MovePrevious;
            _disoPlayerPanel.VolumeChanged -= VolumeChanged;
            SceneManager.activeSceneChanged -= SceneChanged;
            _standardLevelDetailViewController.didPressActionButtonEvent -= DidPressActionButton;
        }

        #endregion

        #region Callbacks

        private void Paused()
        {
            _paused = true;
            _disoPreviewPlayer.Active = false;
        }

        private void Resumed()
        {
            _paused = false;
            _disoPreviewPlayer.Active = true;
        }

        private async void MoveNext()
        {
            if (_config.MusicPlayerEnabled && _initialized)
            {
                var container = await _audioContainerService.GetRandomContainer();
                _activeName = container.name;
                _activeTexture = container.texture;
                _disoPreviewPlayer.SetDefault(container.clip);
                _disoPlayerPanel.SetPlayer(_activeName, _disoPreviewPlayer.DefaultAudioLength, _activeTexture, !_paused);
                _playHistory.Push(container);
            }
        }

        private void MovePrevious()
        {
            if (_config.MusicPlayerEnabled)
            {
                if (_playHistory.Count > 0)
                {
                    var record = _playHistory.Pop();
                    _activeName = record.name;
                    _activeTexture = record.texture;
                    _disoPreviewPlayer.SetDefault(record.clip);
                    _disoPlayerPanel.SetPlayer(_activeName, _disoPreviewPlayer.DefaultAudioLength, _activeTexture, !_paused);
                }
                else
                {
                    MoveNext();
                }
            }
        }

        private void VolumeChanged(float newVolume)
        {
            _disoPreviewPlayer.Volume = newVolume;
        }

        private async void SceneChanged(Scene oldScene, Scene newScene)
        {
            if (newScene.name == "MenuCore" && !_disoPreviewPlayer.Active)
            {
                await SiraUtil.Utilities.AwaitSleep(500);
                if (!_config.SaveTime)
                {
                    MoveNext();
                }
                _disoPreviewPlayer.Active = !_paused;
            }
        }

        private void DidPressActionButton(StandardLevelDetailViewController _)
        {
            _disoPreviewPlayer.Active = false;
        }

        #endregion

        #region BSML

        [UIComponent("root")]
        protected RectTransform root = null!;

        [UIAction("toggle")]
        protected void Toggle()
        {
            if (_disoPlayerPanel.Enabled)
            {
                _disoPlayerPanel.Hide();
                return;
            }
            _disoPlayerPanel.Show();
            _disoPlayerPanel.SetPlayer(_activeName, _disoPreviewPlayer.DefaultAudioLength, _activeTexture, !_paused);
        }

        #endregion
    }
}