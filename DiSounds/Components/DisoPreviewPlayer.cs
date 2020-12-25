using Zenject;
using UnityEngine;
using SiraUtil.Tools;

namespace DiSounds.Components
{
    public class DisoPreviewPlayer : SongPreviewPlayer
    {
        private SiraLog _siraLog = null!;

        [Inject]
        public void Construct(SiraLog siraLog)
        {
            _siraLog = siraLog;
        }

        public override void CrossfadeTo(AudioClip audioClip, float startTime, float duration, float volumeScale = 1)
        {
            _siraLog?.Info("Crossfading...");
            base.CrossfadeTo(audioClip, startTime, duration, volumeScale);
        }
    }
}