using SiraUtil.Tools;
using UnityEngine;
using Zenject;

namespace DiSounds.Components
{
    public class DisoPreviewPlayer : SongPreviewPlayer
    {
        private SiraLog _siraLog = null!;
        private AudioClip _realDefault = null!;
        private bool _transitionDidEnd;
        private float _lastTime = 0f;

        public override void Awake()
        {
            base.Awake();
            _realDefault = _defaultAudioClip;
        }

        public override void OnEnable()
        {
            foreach (var audioSourceVolumeController in _audioSourceControllers)
                if (audioSourceVolumeController != null && audioSourceVolumeController.audioSource != null)
                    audioSourceVolumeController.audioSource.enabled = true;
            _fadeSpeed = _fadeInSpeed;
        }

        public override void Start()
        {
            base.Start();
            CrossfadeToDefault();
        }

        public bool NextDoRandom { private get; set; }

        public DiContainer Container { get; set; } = null!;

        public AudioClip DefaultAudioClip => _defaultAudioClip;

        public bool ShouldUnpause { get; set; } = true;

        public float Volume
        {
            get => _volumeScale;
            set => _volumeScale = value;
        }
        
        public float CurrentAudioTime
        {
            get
            {
                if (_activeChannel < 0) return 0;
                AudioSource audioSource = _audioSourceControllers[_activeChannel].audioSource;
                return audioSource.time;
            }
        }

        public bool PlayingDefault
        {
            get
            {
                if (_activeChannel < 0) return false;
                AudioSource audioSource = _audioSourceControllers[_activeChannel].audioSource;
                return audioSource.clip == _defaultAudioClip;
            }
        }

        public override void Update()
        {
            if (!ShouldUnpause && PlayingDefault)
                return;

            var fut = _timeToDefaultAudioTransition - Time.deltaTime;
            if (_transitionAfterDelay)
            {
                if (fut <= 0f)
                {
                    _transitionDidEnd = true;
                    CrossfadeTo(_defaultAudioClip, Volume, _lastTime, -1f);
                }
            }
            if (PlayingDefault)
            {
                _lastTime = CurrentAudioTime;
            }
            base.Update();
            if (PlayingDefault)
                _lastTime = CurrentAudioTime;
        }

        [Inject]
        public void Construct(SiraLog siraLog, DiContainer container)
        {
            _siraLog = siraLog;
            Container = container;
            CrossfadeToDefault();
        }

        public override void CrossfadeToNewDefault(AudioClip audioClip)
        {
            if (audioClip == _realDefault || audioClip.name == "MultiplayerLobbyAmbience")
                return;
            _lastTime = 0f;
            _defaultAudioClip = audioClip;
            var time = NextDoRandom ? Mathf.Max(Random.Range(0f, _defaultAudioClip.length - 0.1f)) : 0f;
            NextDoRandom = false;
            CrossfadeTo(audioClip, -4f, time, -1f, true);
        }

        public override void CrossfadeToDefault()
        {
            if (PlayingDefault)
                return;
            if (_audioSourceControllers == null || (!_transitionAfterDelay && _activeChannel > 0 && PlayingDefault))
                return;
            CrossfadeTo(_defaultAudioClip, -4f, _lastTime, -1f, true);
        }

        public override void CrossfadeTo(AudioClip audioClip, float volume, float startTime, float duration, bool isDefault)
        {
            if (_transitionDidEnd)
            {
                _transitionDidEnd = false;
                startTime = _lastTime;
                base.CrossfadeToDefault();
            }
            if (isDefault && ShouldUnpause)
                base.CrossfadeTo(audioClip, volume, startTime, duration, isDefault);
        }

        public override void UnPauseCurrentChannel()
        {
            if (ShouldUnpause)
                base.UnPauseCurrentChannel();
        }
    }
}