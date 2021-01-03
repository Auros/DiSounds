using System;
using Zenject;
using DiSounds.Models;
using System.Threading;
using DiSounds.Interfaces;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DiSounds.Managers
{
    internal class CustomSongPicker : ITickable, IAudioContainerService
    {
        public Action? Loaded { get; set; }

        private bool _didInit;
        private readonly Random _random = new Random();
        private readonly BeatmapLevelsModel _beatmapLevelsModel;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private RandomObjectPicker<IPreviewBeatmapLevel>? _randomObjectPicker;
        private readonly Dictionary<IPreviewBeatmapLevel, AudioContainer> _containerCache;

        public CustomSongPicker(BeatmapLevelsModel beatmapLevelsModel)
        {
            _beatmapLevelsModel = beatmapLevelsModel;
            _cancellationTokenSource = new CancellationTokenSource();
            _containerCache = new Dictionary<IPreviewBeatmapLevel, AudioContainer>();
        }

        public void Tick()
        {
            if (!_didInit)
            {
                if (_beatmapLevelsModel.customLevelPackCollection != null)
                {
                    _didInit = true;
                    List<IPreviewBeatmapLevel> levels = new List<IPreviewBeatmapLevel>();
                    var packs = _beatmapLevelsModel.customLevelPackCollection.beatmapLevelPacks;
                    foreach (var pack in packs)
                    {
                        foreach (var level in pack.beatmapLevelCollection.beatmapLevels)
                        {
                            levels.Add(level);
                        }
                    }
                    _randomObjectPicker = new RandomObjectPicker<IPreviewBeatmapLevel>(levels.ToArray(), 2.5f);
                    Loaded?.Invoke();
                }
            }
        }

        public async Task<AudioContainer> GetRandomContainer()
        {
            UnityEngine.Random.InitState(_random.Next(0, 100000));
            var randomBeatmap = _randomObjectPicker!.PickRandomObject();
            if (randomBeatmap == null)
            {
                return new AudioContainer("^", null!, null!);
            }
            if (!_containerCache.TryGetValue(randomBeatmap, out AudioContainer container))
            {
                var audioClip = await randomBeatmap.GetPreviewAudioClipAsync(_cancellationTokenSource.Token);
                var sprite = await randomBeatmap.GetCoverImageAsync(_cancellationTokenSource.Token);
                container = new AudioContainer(randomBeatmap.songName, audioClip, sprite.texture);
                _containerCache.Add(randomBeatmap, container);
            }
            return container;
        }
    }
}