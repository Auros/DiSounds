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
        private readonly Stack<AudioContainer> _playFuture;
        private readonly Stack<AudioContainer> _playHistory;
        private readonly DisoPreviewPlayer _disoPreviewPlayer;
        private readonly IAudioContainerService _audioContainerService;
        private readonly GameplaySetupViewController _gameplaySetupViewController;
        private const string _content = "<clickable-text id=\"root\" on-click=\"toggle\" text=\"💿\" align=\"Center\" anchor-pos-x=\"54\" anchor-pos-y=\"37.25\" size-delta-x=\"8\" default-color=\"#ffd630\" />";

        private Texture2D? _activeTexture;
        private string _activeName = "Unknown";
        private AudioContainer? _currentContainer;

        public DisoMusicPlayer(Config config, SiraLog siraLog, DisoPlayerPanel disoPlayerPanel, DisoPreviewPlayer disoPreviewPlayer, IAudioContainerService audioContainerService, GameplaySetupViewController gameplaySetupViewController)
        {
            _config = config;
            _siraLog = siraLog;
            _disoPlayerPanel = disoPlayerPanel;
            _disoPreviewPlayer = disoPreviewPlayer;
            _playFuture = new Stack<AudioContainer>();
            _playHistory = new Stack<AudioContainer>();
            _audioContainerService = audioContainerService;
            _gameplaySetupViewController = gameplaySetupViewController;
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

            _disoPlayerPanel.Volume = _disoPreviewPlayer.Volume;
        }

        private void LoadedAudio()
        {
            BSMLParser.instance.Parse(_content, _gameplaySetupViewController.gameObject, this);
            root.name = "DisoPlayerToggle";
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
                    if (_disoPreviewPlayer.CurrentAudioTime + 0.5f > _disoPreviewPlayer.DefaultAudioClip.length)
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
        }

        #endregion

        #region Callbacks

        private void Paused()
        {
            _paused = true;
            _disoPreviewPlayer.ShouldUnpause = false;
            _disoPreviewPlayer.PauseCurrentChannel();
        }

        private void Resumed()
        {
            _paused = false;
            _disoPreviewPlayer.ShouldUnpause = true;
            _disoPreviewPlayer.UnPauseCurrentChannel();
        }

        private async void MoveNext()
        {
            if (_config.MusicPlayerEnabled && _initialized)
            {
                if (_currentContainer != null)
                {
                    _playHistory.Push(_currentContainer.Value);
                }
                AudioContainer container;
                if (_playFuture.Count > 0)
                {
                    container = _playFuture.Pop();
                }
                else
                {
                    container = await _audioContainerService.GetRandomContainer();
                }
                if (container.name == "^")
                {
                    _disoPreviewPlayer.NextDoRandom = false;
                    return;
                }
                _activeName = container.name;
                _currentContainer = container;
                _activeTexture = container.texture;
                _disoPreviewPlayer.CrossfadeToNewDefault(container.clip);
                _disoPreviewPlayer.NextDoRandom = false;
                _disoPlayerPanel.SetPlayer(_activeName, _disoPreviewPlayer.DefaultAudioClip.length, _activeTexture, !_paused);
                if (_paused)
                {
                    _disoPlayerPanel.SetTime(0f);
                }
            }
            _disoPreviewPlayer.NextDoRandom = false;
        }

        private void MovePrevious()
        {
            if (_config.MusicPlayerEnabled)
            {
                if (_disoPreviewPlayer.CurrentAudioTime > 1f)
                {
                    _disoPreviewPlayer.CrossfadeTo(_disoPreviewPlayer.DefaultAudioClip, 0f, _disoPreviewPlayer.DefaultAudioClip.length);
                    _disoPlayerPanel.SetTime(0f);
                    return;
                }
                if (_playHistory.Count > 0)
                {
                    if (_currentContainer != null)
                    {
                        _playFuture.Push(_currentContainer.Value);
                    }
                    var record = _playHistory.Pop();
                    _currentContainer = record;
                    _activeName = record.name;
                    _activeTexture = record.texture;
                    _disoPreviewPlayer.CrossfadeToNewDefault(record.clip);
                    _disoPlayerPanel.SetPlayer(_activeName, _disoPreviewPlayer.DefaultAudioClip.length, _activeTexture, !_paused);
                }
                else
                {
                    MoveNext();
                }
                if (_paused)
                {
                    _disoPlayerPanel.SetTime(0f);
                }
            }
        }

        private void VolumeChanged(float newVolume)
        {
            _disoPreviewPlayer.Volume = newVolume;
        }

        private async void SceneChanged(Scene oldScene, Scene newScene)
        {
            await SiraUtil.Utilities.PauseChamp;
            if (newScene.name == "MenuCore")
            {
                if (!_config.SaveTime)
                {
                    _disoPreviewPlayer.NextDoRandom = true;
                    MoveNext();
                }
                if (!_paused)
                    _disoPreviewPlayer.UnPauseCurrentChannel();
                else
                    _disoPreviewPlayer.PauseCurrentChannel();
            }
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
            _disoPlayerPanel.SetPlayer(_activeName, _disoPreviewPlayer.DefaultAudioClip.length, _activeTexture, !_paused);
        }

        #endregion
    }
}