using System;
using Zenject;
using UnityEngine;
using System.Threading;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using BeatSaberMarkupLanguage.Attributes;

namespace DiSounds.Models
{
    internal abstract class DisoAudioPacket : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected readonly CancellationTokenSource _cancellationTokenSource;

        [Inject(Id = "audio.sourcer")]
        protected readonly AudioSource _audioSourcer = null!;

        [Inject]
        protected readonly CachedMediaAsyncLoader _cachedMediaAsyncLoader = null!;

        [UIValue("name")]
        public string Name { get; }

        [UIValue("source")]
        public string Source { get; }

        protected bool _enabled;
        [UIValue("enabled")]
        public bool Enabled
        {
            get => _enabled;
            set
            {
                _enabled = value;
                NotifyPropertyChanged(nameof(Enabled));

                NotifyPropertyChanged(nameof(Status));
                NotifyPropertyChanged(nameof(StatusColor));
                NotifyPropertyChanged(nameof(ToggleString));
            }
        }

        [UIValue("status")]
        public string Status => Enabled ? "✅" : "❌";

        [UIValue("status-color")]
        public string StatusColor => Enabled ? "lime" : "red";

        [UIValue("toggle-string")]
        public string ToggleString => Enabled ? "-" : "+";

        public AudioClip? AssociatedClip { get; set; }

        public DisoAudioPacket(string name, string source)
        {
            Name = name;
            Source = source;
            _cancellationTokenSource = new CancellationTokenSource();
        }

        [UIAction("preview")]
        public virtual async Task Preview()
        {
            if (AssociatedClip == null)
            {
                try
                {
                    AssociatedClip = await _cachedMediaAsyncLoader.LoadAudioClipAsync(Source, _cancellationTokenSource.Token);
                } catch { }
            }
            if (AssociatedClip != null)
            {
                _audioSourcer.clip = AssociatedClip;
                _audioSourcer.Play();
                await SiraUtil.Utilities.AwaitSleep(10000);
                if (_audioSourcer.clip == AssociatedClip)
                {
                    _audioSourcer.Stop();
                }
            }
        }

        public abstract void Activated();

        public abstract void Deactivated();

        [UIAction("change-state")]
        protected void ChangeState()
        {
            Enabled = !Enabled;

            if (Enabled) Activated();
            else Deactivated();
        }

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            try
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            catch { }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}