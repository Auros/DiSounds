using Zenject;
using System.Linq;
using UnityEngine;
using SiraUtil.Tools;

namespace DiSounds.Components
{
    public class DisoPreviewPlayer : SongPreviewPlayer
    {
        private bool _active = true;
        private bool _transitionDidEnd;
        private SiraLog _siraLog = null!;
        private float _lastTrueAudioLength = 0f;
        private AudioClip _realDefaultAudioClip = null!;

        public bool Active
        {
            get => _active;
            set
            {
                if (_active)
                {
                    _lastTrueAudioLength = CurrentAudioTime;
                }
                _active = value;
                if (_active)
                {
                    if (_audioSources != null)
                    {
                        CrossfadeTo(_defaultAudioClip, _lastTrueAudioLength, -1f, _ambientVolumeScale);
                    }
                }
                else CrossfadeTo(null!, 0f, -1f, _volumeScale);
            }
        }

        public float Volume
        {
            get => _volume;
            set => _volume = value;
        }
        
        public float CurrentAudioTime
        {
            get
            {
                if (_audioSources == null) return 0;
                if (_audioSources.ElementAtOrDefault(_activeChannel) == null) return 0;
                var audioSource = _audioSources[_activeChannel];
                return audioSource.time;
            }
        }

        public float AmbientVolume => _ambientVolumeScale;

        public AudioClip DefaultClip => _defaultAudioClip;

        public float DefaultAudioLength => _defaultAudioClip.length;

        public bool PlayingDefault => Initialized && _audioSources[_activeChannel].isPlaying && _defaultAudioClip == _audioSources[_activeChannel].clip;

        public bool Initialized => _audioSources != null && _audioSources.Length != 0 && _audioSources.ElementAtOrDefault(_activeChannel) != null && _audioSources[_activeChannel] != null;

        [Inject]
        public void Construct(SiraLog siraLog)
        {
            _siraLog = siraLog;
            _realDefaultAudioClip = _defaultAudioClip;
        }

        public void SetDefault(AudioClip clip)
        {
            _defaultAudioClip = clip;
            _lastTrueAudioLength = 0;
            if (_active)
            {
                CrossfadeTo(clip, 0f, -1f, _ambientVolumeScale);
            }
        }

        #region Overrides

        public override void OnEnable()
        {
            _fadeSpeed = _defaultCrossfadeSpeed;
            _audioSources = new AudioSource[_channelsCount];
            for (int i = 0; i < this._channelsCount; i++)
            {
                _audioSources[i] = Instantiate(_audioSourcePrefab, transform);
                _audioSources[i].volume = 0f;
                _audioSources[i].loop = false;
                _audioSources[i].reverbZoneMix = 0f;
                _audioSources[i].playOnAwake = false;
            }
            if (Active)
            {
                CrossfadeTo(_defaultAudioClip, _lastTrueAudioLength, -1, _ambientVolumeScale);
            }
        }
        public override void CrossfadeToDefault()
        {
            if (!_active)
            {
                base.CrossfadeTo(null!, _lastTrueAudioLength, -1f, _ambientVolumeScale);
                return;
            }
            if (_audioSources == null) return;
            if (!_transitionAfterDelay && _activeChannel > 0 && _audioSources[_activeChannel].clip == _defaultAudioClip) return;
            CrossfadeTo(_defaultAudioClip, _lastTrueAudioLength, -1f, _ambientVolumeScale);
        }

        public override void CrossfadeTo(AudioClip audioClip, float startTime, float duration, float volumeScale = 1)
        {
            if (_transitionDidEnd)
            {
                _transitionDidEnd = false;
                startTime = _lastTrueAudioLength;
                audioClip = _active ? _defaultAudioClip : null!;
            }
            base.CrossfadeTo(audioClip, startTime, duration, volumeScale);
        }

        public override void Update()
        {
            var fut = _timeToDefaultAudioTransition - Time.deltaTime;
            if (_transitionAfterDelay)
            {
                if (fut <= 0f)
                {
                    _transitionDidEnd = true;
                    CrossfadeTo(_defaultAudioClip, 0f, -1f, _ambientVolumeScale);
                }
            }
            if (Active && PlayingDefault)
            {
                _lastTrueAudioLength = CurrentAudioTime;
            }
            base.Update();
        }

        #endregion
    }
}