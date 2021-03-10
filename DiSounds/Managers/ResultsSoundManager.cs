using System;
using SiraUtil.Tools;
using System.Threading;
using System.Threading.Tasks;
using DiSounds.Components;

namespace DiSounds.Managers
{
    internal class ResultsSoundManager
    {
        private readonly Config _config;
        private readonly Random _random;
        private readonly SiraLog _siraLog;
        private readonly SongPreviewPlayer _songPreviewPlayer;
        private readonly IAudioClipAsyncLoader _audioClipAsyncLoader;

        public ResultsSoundManager(Config config, SiraLog siraLog, SongPreviewPlayer songPreviewPlayer, CachedMediaAsyncLoader cachedMediaAsyncLoader)
        {
            _config = config;
            _siraLog = siraLog;
            _random = new Random();
            _songPreviewPlayer = songPreviewPlayer;
            _audioClipAsyncLoader = cachedMediaAsyncLoader;
        }

        public async Task PlayRandomAudio(UnityEngine.AudioClip fallback, LevelCompletionResults results, bool highScore)
        {
            await SiraUtil.Utilities.AwaitSleep(250);
            if (results.levelEndStateType == LevelCompletionResults.LevelEndStateType.Failed && _config.ResultFailedSoundsEnabled)
            {
                var hasAudio = _config.EnabledResultFailedSounds.Count != 0;

                if (!hasAudio)
                    return;

                var randomAudioFile = _config.EnabledResultFailedSounds[_random.Next(0, _config.EnabledResultFailedSounds.Count)];
                _siraLog.Info($"Loading Audio File: {randomAudioFile.FullName}");
                var clip = await _audioClipAsyncLoader.LoadAudioClipAsync(randomAudioFile.FullName, CancellationToken.None);
                Play(clip);
            }
            else if (results.levelEndStateType == LevelCompletionResults.LevelEndStateType.Cleared && _config.ResultSoundsEnabled) 
            {
                var info = results.fullCombo ? ( _config.EnabledResultFCSounds.Count == 0 ? _config.EnabledResultSounds : _config.EnabledResultFCSounds  ) : _config.EnabledResultSounds;
                var hasAudio = info.Count != 0;

                if (!hasAudio)
                {
                    if (highScore)
                        _songPreviewPlayer.CrossfadeTo(fallback, 0f, fallback.length);
                    return;
                }

                var randomAudioFile = info[_random.Next(0, info.Count)];
                _siraLog.Info($"Loading Audio File: {randomAudioFile.FullName}");
                var clip = await _audioClipAsyncLoader.LoadAudioClipAsync(randomAudioFile.FullName, CancellationToken.None);
                
                if (highScore)
                {
                    Play(clip);
                }
            }
        }

        private void Play(UnityEngine.AudioClip clip)
        {
            if (_songPreviewPlayer is DisoPreviewPlayer diso)
                diso.CrossfadeTo(clip, 0f, clip.length + 1.5f);
            else
                _songPreviewPlayer.CrossfadeTo(clip, 0f, clip.length + 1.5f);
        }
    }
}