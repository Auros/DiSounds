using System;
using Zenject;
using System.IO;
using UnityEngine;
using System.Linq;
using IPA.Utilities;
using SiraUtil.Tools;
using System.Threading;
using System.Diagnostics;
using SiraUtil.Interfaces;
using System.Collections.Generic;

namespace DiSounds.Managers
{
    internal class DiClickManager : IRegistrar<AudioClip>, IInitializable, IDisposable, IRefreshable, IToggleable
    {
        private readonly Config _config;
        private readonly SiraLog _siraLog;
        private readonly AudioClip _defaultClip;
        private readonly BasicUIAudioManager _basicUIAudioManager;
        private readonly CachedMediaAsyncLoader _cachedMediaAsyncLoader;
        private readonly CancellationTokenSource _cancellationTokenSource;

        private static readonly FieldAccessor<BasicUIAudioManager, float>.Accessor Min = FieldAccessor<BasicUIAudioManager, float>.GetAccessor("_minPitch");
        private static readonly FieldAccessor<BasicUIAudioManager, float>.Accessor Max = FieldAccessor<BasicUIAudioManager, float>.GetAccessor("_maxPitch");
        private static readonly FieldAccessor<BasicUIAudioManager, AudioClip[]>.Accessor AudioClips = FieldAccessor<BasicUIAudioManager, AudioClip[]>.GetAccessor("_clickSounds");
        private static readonly FieldAccessor<BasicUIAudioManager, RandomObjectPicker<AudioClip>>.Accessor RandomAudioPicker = FieldAccessor<BasicUIAudioManager, RandomObjectPicker<AudioClip>>.GetAccessor("_randomSoundPicker");

        public bool Status
        {
            get => _basicUIAudioManager.enabled;
            set => _basicUIAudioManager.enabled = value;
        }

        public DiClickManager(Config config, SiraLog siraLog, BasicUIAudioManager basicUIAudioManager, CachedMediaAsyncLoader cachedMediaAsyncLoader, InitializableManager initManager, DisposableManager disposeManager)
        {
            _config = config;
            _siraLog = siraLog;
            _basicUIAudioManager = basicUIAudioManager;
            _cachedMediaAsyncLoader = cachedMediaAsyncLoader;
            _cancellationTokenSource = new CancellationTokenSource();

            initManager.Add(this);
            disposeManager.Add(this);
            _defaultClip = AudioClips(ref basicUIAudioManager)[0];
        }

        public async void Initialize()
        {
            _siraLog.Debug("Loading audio clips.");
            var badClips = new List<FileInfo>();
            Stopwatch stopwatch = Stopwatch.StartNew();
            foreach (var clipPath in _config.EnabledMenuClicks)
            {
                if (!clipPath.Exists)
                {
                    badClips.Add(clipPath);
                    continue;
                }
                try
                {
                    _siraLog.Debug($"Loading {clipPath.FullName}");
                    AudioClip audioClip = await _cachedMediaAsyncLoader.LoadAudioClipAsync(clipPath.FullName, _cancellationTokenSource.Token);
                    Add(audioClip);
                }
                catch (Exception e)
                {
                    _siraLog.Error(e.Message);
                    badClips.Add(clipPath);
                }
            }
            foreach (var bad in badClips)
            {
                // If any of the clips failed to load or do not exist, remove them.
                _config.EnabledMenuClicks.Remove(bad);
            }
            stopwatch.Stop();
            _siraLog.Debug($"Finished Loading Audio Clips in {stopwatch.Elapsed} seconds");

            Refresh();
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
        }

        public virtual void Add(AudioClip value)
        {
            var manager = _basicUIAudioManager;
            var clips = AudioClips(ref manager).ToList();
            if (!clips.Contains(value))
            {
                clips.Add(value);
            }

            Set(clips.ToArray());
        }

        public virtual void Remove(AudioClip value)
        {
            var manager = _basicUIAudioManager;
            var clips = AudioClips(ref manager).ToList();
            int size = clips.Count;
            clips.Remove(value);
            
            if (clips.Count != size)
            {
                Set(clips.ToArray());
            }
        }

        public void Refresh()
        {
            _siraLog.Debug("Refreshing...");

            var manager = _basicUIAudioManager;
            if (!_config.MenuClicksEnabled)
            {
                var def = new AudioClip[] { _defaultClip };
                AudioClips(ref manager) = def;
                Min(ref manager) = 0.95f;
                Max(ref manager) = 1.05f;
                Set(def);
                return;
            }

            RecalculatePitch();
            if (_config.UseMenuDefault)
            {
                Add(_defaultClip);
            }
            else
            {
                Remove(_defaultClip);
            }
        }

        private void RecalculatePitch()
        {
            var manager = _basicUIAudioManager;
            Min(ref manager) = _config.DoMenuPitch ? _config.MinPitch : 1f;
            Max(ref manager) = _config.DoMenuPitch ? _config.MaxPitch : 1f;
        }

        private void Set(AudioClip[] newClips)
        {
            var manager = _basicUIAudioManager;
            AudioClips(ref manager) = newClips;
            RandomAudioPicker(ref manager) = new RandomObjectPicker<AudioClip>(newClips, 0.07f);
        }
    }
}