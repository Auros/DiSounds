using System;
using Zenject;
using System.IO;
using System.Linq;
using DiSounds.UI;
using UnityEngine;
using SiraUtil.Tools;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using DiSounds.Components;
using Random = System.Random;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
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
        private readonly Config _config;
        private readonly SiraLog _siraLog;
        private readonly Texture2D _questionMark;
        private readonly DisoPlayerPanel _disoPlayerPanel;
        private readonly Stack<AudioContainer> _playHistory;
        private readonly DisoPreviewPlayer _disoPreviewPlayer;
        private readonly BeatmapLevelsModel _beatmapLevelsModel;
        private readonly CachedMediaAsyncLoader _cachedMediaAsyncLoader;
        private readonly GameplaySetupViewController _gameplaySetupViewController;
        private readonly StandardLevelDetailViewController _standardLevelDetailViewController;
        private const string _content = "<clickable-text id=\"root\" on-click=\"toggle\" text=\"💿\" align=\"Center\" anchor-pos-x=\"40\" anchor-pos-y=\"0.25\" size-delta-x=\"8\" default-color=\"#ffd630\" />";

        private Texture2D _activeTexture;
        private string _activeName = "Unknown";
        private readonly List<AudioContainer> _loadedCustoms = new List<AudioContainer>();

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public DisoMusicPlayer(Config config,
                               SiraLog siraLog,
                               DisoPlayerPanel disoPlayerPanel,
                               DisoPreviewPlayer disoPreviewPlayer,
                               BeatmapLevelsModel beatmapLevelsModel,
                               CachedMediaAsyncLoader cachedMediaAsyncLoader,
                               GameplaySetupViewController gameplaySetupViewController,
                               StandardLevelDetailViewController standardLevelDetailViewController)
        {
            _config = config;
            _siraLog = siraLog;
            _random = new Random();
            _disoPlayerPanel = disoPlayerPanel;
            _disoPreviewPlayer = disoPreviewPlayer;
            _beatmapLevelsModel = beatmapLevelsModel;
            _playHistory = new Stack<AudioContainer>();
            _cachedMediaAsyncLoader = cachedMediaAsyncLoader;
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

        private async Task Init()
        {
            _siraLog.Debug("Loading music.");
            var badMusic = new List<FileInfo>();
            Stopwatch stopwatch = Stopwatch.StartNew();
            foreach (var musicPath in _config.EnabledMusicFiles)
            {
                if (!musicPath.Exists || musicPath.Extension != ".ogg")
                {
                    badMusic.Add(musicPath);
                    continue;
                }
                try
                {
                    _siraLog.Debug($"Loading {musicPath.FullName}");
                    AudioClip audioClip = await _cachedMediaAsyncLoader.LoadAudioClipAsync(musicPath.FullName, _cts.Token);
                    var name = musicPath.Name.Remove(musicPath.Name.IndexOf(musicPath.Extension));
                    var maybeImage = musicPath.Directory.EnumerateFiles().FirstOrDefault(ef => ef.Name.Remove(ef.Name.IndexOf(ef.Extension)) == name && (ef.Extension == ".png" || ef.Extension == ".jpg"));
                    Texture2D texHolder = _questionMark;
                    if (maybeImage != null && maybeImage.Exists)
                    {
                        texHolder = (await _cachedMediaAsyncLoader.LoadSpriteAsync(maybeImage!.FullName, _cts.Token)).texture;
                    }
                    _loadedCustoms.Add(new AudioContainer(name, audioClip, texHolder));
                }
                catch (Exception e)
                {
                    _siraLog.Error(e.Message);
                    badMusic.Add(musicPath);
                }
            }
            foreach (var bad in badMusic)
            {
                // If any of the music files failed to load or do not exist, remove them.
                _config.EnabledMusicFiles.Remove(bad);
            }
            stopwatch.Stop();
            _siraLog.Debug($"Finished Loading Music in {stopwatch.Elapsed} seconds");

            if (_config.MusicSource == Config.PlaybackType.CustomFiles)
            {
                _didInit = true;
                PlayRandomContainer();
            }
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
            _ = Init();
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
            if (!_didInit)
            {
                if (_config.MusicSource == Config.PlaybackType.CustomSongs)
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
            if (_didInit && _config.MusicPlayerEnabled)
            {
                if (_config.MusicSource == Config.PlaybackType.CustomSongs)
                {
                    var level = GetRandomLevel(_beatmapLevelsModel.customLevelPackCollection);
                    if (level != null)
                    {
                        await PlayThroughLevel(level);
                    }
                }
                else if (_config.MusicSource == Config.PlaybackType.CustomFiles)
                {
                    PlayRandomContainer();
                }
            }
        }

        private void MovePrevious()
        {
            if (_config.MusicPlayerEnabled)
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

        private async Task PlayThroughLevel(IPreviewBeatmapLevel level)
        {
            if (!_config.MusicPlayerEnabled) return;

            var clip = await level.GetPreviewAudioClipAsync(_cts.Token);
            var img = await level.GetCoverImageAsync(_cts.Token);

            _disoPreviewPlayer.SetDefault(clip);
            _activeTexture = img.texture;
            _activeName = level.songName;
            _disoPlayerPanel.SetPlayer(_activeName, _disoPreviewPlayer.DefaultAudioLength, _activeTexture, !_paused);
            _playHistory.Push(new AudioContainer(_activeName, clip, _activeTexture));
        }

        private void PlayRandomContainer()
        {
            if (!_config.MusicPlayerEnabled) return;

            if (_loadedCustoms.Count > 0)
            {
                var container = _loadedCustoms[_random.Next(0, _loadedCustoms.Count)];
                _activeTexture = container.texture ?? _questionMark;
                _activeName = container.name;

                _disoPreviewPlayer.SetDefault(container.clip);
                _disoPlayerPanel.SetPlayer(_activeName, _disoPreviewPlayer.DefaultAudioLength, _activeTexture, !_paused);
                _playHistory.Push(container);
            }
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