using System;
using Zenject;
using UnityEngine;
using SiraUtil.Tools;
using System.Threading;
using System.Diagnostics;
using Random = System.Random;

namespace DiSounds.Managers
{
    internal class IntroSoundManager : IInitializable
    {
        private bool _didPlay;
        private readonly Config _config;
        private readonly Random _random;
        private readonly SiraLog _siraLog;
        private readonly AudioSource _audioSourcer;
        private readonly IAudioClipAsyncLoader _audioClipAsyncLoader;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public IntroSoundManager(Config config, SiraLog siraLog, [Inject(Id = "audio.sourcer")] AudioSource audioSourcer, CachedMediaAsyncLoader cachedMediaAsyncLoader)
        {
            _config = config;
            _siraLog = siraLog;
            _random = new Random();
            _audioSourcer = audioSourcer;
            _audioClipAsyncLoader = cachedMediaAsyncLoader;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async void Initialize()
        {

            if (_config.EnabledIntroSounds.Count > 0 && !_didPlay)
            {
                _didPlay = true;
                var ourLuckyIntro = _config.EnabledIntroSounds[_random.Next(0, _config.EnabledIntroSounds.Count)];

                try
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    _siraLog.Debug($"Loading {ourLuckyIntro.FullName}");
                    AudioClip audioClip = await _audioClipAsyncLoader.LoadAudioClipAsync(ourLuckyIntro.FullName, _cancellationTokenSource.Token);
                    stopwatch.Stop();
                    _siraLog.Debug($"Finished Loading Intro in {stopwatch.Elapsed} seconds");
                    await SiraUtil.Utilities.AwaitSleep(1000);
                    _audioSourcer.clip = audioClip;
                    _audioSourcer.Play();
                    await SiraUtil.Utilities.AwaitSleep((int)(audioClip.length * 1000));
                    if (_audioSourcer.clip == audioClip)
                    {
                        _audioSourcer.clip = null;
                    } 
                }
                catch (Exception e)
                {
                    _siraLog.Error(e.Message);
                    _config.EnabledIntroSounds.Remove(ourLuckyIntro);
                }
            }
        }
    }
}