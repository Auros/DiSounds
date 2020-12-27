using System;
using Zenject;
using DiSounds.UI;
using UnityEngine;
using SiraUtil.Tools;
using System.Threading;
using System.Reflection;
using DiSounds.Components;
using Random = System.Random;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using BeatSaberMarkupLanguage.Attributes;

namespace DiSounds.Managers
{
    internal class DisoMusicPlayer : IInitializable, ITickable, IDisposable
    {
        private bool _didInit;
        private bool _paused = false;
        private float _cycleTime = 0f;
        private readonly float _cycleLength = 0.5f;

        private readonly Random _random;
        private readonly SiraLog _siraLog; 
        private readonly Texture2D _questionMark;
        private readonly DisoPlayerPanel _disoPlayerPanel;
        private readonly Stack<AudioContainer> _playHistory;
        private readonly DisoPreviewPlayer _disoPreviewPlayer;
        private readonly BeatmapLevelsModel _beatmapLevelsModel;
        private readonly GameplaySetupViewController _gameplaySetupViewController;
        private readonly StandardLevelDetailViewController _standardLevelDetailViewController;
        private const string _content = "<clickable-text id=\"root\" on-click=\"toggle\" text=\"💿\" align=\"Center\" anchor-pos-x=\"40\" anchor-pos-y=\"0.25\" size-delta-x=\"8\" default-color=\"#ffd630\" />";

        private Texture2D _activeTexture;
        private string _activeName = "Unknown";

        private CancellationTokenSource _cts = new CancellationTokenSource();

        public DisoMusicPlayer(SiraLog siraLog, DisoPlayerPanel disoPlayerPanel, DisoPreviewPlayer disoPreviewPlayer, BeatmapLevelsModel beatmapLevelsModel, GameplaySetupViewController gameplaySetupViewController, StandardLevelDetailViewController standardLevelDetailViewController)
        {
            _siraLog = siraLog;
            _random = new Random();
            _disoPlayerPanel = disoPlayerPanel;
            _disoPreviewPlayer = disoPreviewPlayer;
            _beatmapLevelsModel = beatmapLevelsModel;
            _playHistory = new Stack<AudioContainer>();
            _gameplaySetupViewController = gameplaySetupViewController;
            _standardLevelDetailViewController = standardLevelDetailViewController;
            _questionMark = Utilities.LoadTextureRaw(Utilities.GetResource(Assembly.GetExecutingAssembly(), "DiSounds.Resources.question.png"));
            _activeTexture = _questionMark;
            Setup();
        }

        private void Setup()
        {
            BSMLParser.instance.Parse(_content, _gameplaySetupViewController.transform.Find("HeaderPanel").gameObject, this);
            root.name = "DisoPlayerToggle";
        }

        #region Kernel State

        public void Initialize()
        {
            _disoPlayerPanel.Paused += Paused;
            _disoPlayerPanel.Resumed += Resumed;
            _disoPlayerPanel.MoveNext += MoveNext;
            _disoPlayerPanel.MovePrevious += MovePrevious;
            _disoPlayerPanel.VolumeChanged += VolumeChanged;
            SceneManager.activeSceneChanged += SceneChanged;
            _standardLevelDetailViewController.didPressActionButtonEvent += DidPressActionButton;

            _disoPlayerPanel.Volume = _disoPreviewPlayer.Volume;
        }

        public void Tick()
        {
            _cycleTime += Time.deltaTime;
            if (_cycleTime >= _cycleLength)
            {
                _cycleTime = 0f;
                if (_disoPreviewPlayer.PlayingDefault)
                {
                    _disoPlayerPanel.SetTime(_disoPreviewPlayer.CurrentAudioTime);
                }
            }
            if (!_didInit)
            {
                if (_beatmapLevelsModel.customLevelPackCollection != null)
                {
                    _didInit = true;
                    var level = GetRandomLevel(_beatmapLevelsModel.customLevelPackCollection);
                    if (level != null)
                    {
                        _ = PlayThroughLevel(level);
                    }
                }
                return;
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _disoPlayerPanel.Paused -= Paused;
            _disoPlayerPanel.Resumed -= Resumed;
            _disoPlayerPanel.MoveNext -= MoveNext;
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
            if (_didInit)
            {
                var level = GetRandomLevel(_beatmapLevelsModel.customLevelPackCollection);
                if (level != null)
                {
                    await PlayThroughLevel(level);
                }
            }
        }

        private void MovePrevious()
        {
            if (_playHistory.Count > 0)
            {
                var record = _playHistory.Pop();
                _activeTexture = record.texture!;
                if (_activeTexture == null)
                {
                    _activeTexture = _questionMark;
                }
                _activeName = record.name;
                _disoPreviewPlayer.SetDefault(record.clip);
                _disoPlayerPanel.SetPlayer(_activeName, _disoPreviewPlayer.DefaultAudioLength, _activeTexture, true);
            }
            else
            {
                MoveNext();
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
                await SiraUtil.Utilities.PauseChamp;
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
            _disoPlayerPanel.SetPlayer(_activeName, _disoPreviewPlayer.DefaultAudioLength, _activeTexture, true);
        }

        #endregion

        private async Task PlayThroughLevel(IPreviewBeatmapLevel level)
        {
            var clip = await level.GetPreviewAudioClipAsync(_cts.Token);
            var img = await level.GetCoverImageAsync(_cts.Token);

            _disoPreviewPlayer.SetDefault(clip);
            _activeTexture = img.texture;
            _activeName = level.songName;
            _disoPlayerPanel.SetPlayer(_activeName, _disoPreviewPlayer.DefaultAudioLength, _activeTexture, true);

            _playHistory.Push(new AudioContainer(_activeName, clip, _activeTexture));
        }

        private IPreviewBeatmapLevel? GetRandomLevel(IBeatmapLevelPackCollection collection)
        {
            List<IPreviewBeatmapLevel> levels = new List<IPreviewBeatmapLevel>();
            var packs = collection.beatmapLevelPacks;
            foreach (var pack in packs)
            {
                foreach (var level in pack.beatmapLevelCollection.beatmapLevels)
                {
                    levels.Add(level);
                }
            }
            return levels.Count == 0 ? null : levels[_random.Next(0, levels.Count)];
        }
    
        private struct AudioContainer
        {
            public string name;
            public AudioClip clip;
            public Texture2D? texture;

            public AudioContainer(string name, AudioClip clip, Texture2D? texture = null)
            {
                this.name = name;
                this.clip = clip;
                this.texture = texture;
            }
        }
    }
}