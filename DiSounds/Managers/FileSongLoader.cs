using System;
using Zenject;
using System.IO;
using System.Linq;
using UnityEngine;
using SiraUtil.Tools;
using DiSounds.Models;
using System.Threading;
using System.Diagnostics;
using DiSounds.Interfaces;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DiSounds.Managers
{
    internal class FileSongLoader : IInitializable, IDisposable, IAudioContainerService
    {
        public Action? Loaded { get; set; }

        private readonly Config _config;
        private readonly SiraLog _siraLog;
        private readonly System.Random _random = new System.Random();
        private RandomObjectPicker<AudioContainer>? _randomObjectPicker;
        private readonly CachedMediaAsyncLoader _cachedMediaAsyncLoader;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public FileSongLoader(Config config, SiraLog siraLog, CachedMediaAsyncLoader cachedMediaAsyncLoader)
        {
            _config = config;
            _siraLog = siraLog;
            _cachedMediaAsyncLoader = cachedMediaAsyncLoader;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public void Initialize()
        {
            _ = Init();
        }

        private async Task Init()
        {
            _siraLog.Debug("Loading music.");
            var badMusic = new List<FileInfo>();
            var stopwatch = Stopwatch.StartNew();
            var loadedCustoms = new List<AudioContainer>();
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
                    AudioClip audioClip = await _cachedMediaAsyncLoader.LoadAudioClipAsync(musicPath.FullName, _cancellationTokenSource.Token);
                    var name = musicPath.Name.Remove(musicPath.Name.IndexOf(musicPath.Extension));
                    var maybeImage = musicPath.Directory.EnumerateFiles().FirstOrDefault(ef => ef.Name.Remove(ef.Name.IndexOf(ef.Extension)) == name && (ef.Extension == ".png" || ef.Extension == ".jpg"));
                    Texture2D? texHolder = null;
                    if (maybeImage != null && maybeImage.Exists)
                    {
                        texHolder = (await _cachedMediaAsyncLoader.LoadSpriteAsync(maybeImage!.FullName, _cancellationTokenSource.Token)).texture;
                    }
                    loadedCustoms.Add(new AudioContainer(name, audioClip, texHolder));
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
            _randomObjectPicker = new RandomObjectPicker<AudioContainer>(loadedCustoms.ToArray(), 0.07f);
            Loaded?.Invoke();
        }

        public async Task<AudioContainer> GetRandomContainer()
        {
            await Task.CompletedTask;
            UnityEngine.Random.InitState(_random.Next(0, 100000));
            return _randomObjectPicker!.PickRandomObject();
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}