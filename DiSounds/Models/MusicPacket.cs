using Zenject;
using System.IO;
using DiSounds.Components;
using System.Threading.Tasks;

namespace DiSounds.Models
{
    internal class MusicPacket : DisoAudioPacket
    {
        public FileInfo File { get; }

        [Inject]
        protected readonly Config _config = null!;

        [Inject]
        private readonly DisoPreviewPlayer _disoPreviewPlayer = null!;

        public MusicPacket(FileInfo file, bool enabled) : base(file.Name, file.FullName)
        {
            File = file;
            _enabled = enabled;
        }

        public override void Activated()
        {
            _config.EnabledMusicFiles.Add(File);
        }

        public override async Task Preview()
        {
            AssociatedClip = await _cachedMediaAsyncLoader.LoadAudioClipAsync(Source, _cancellationTokenSource.Token);
            if (AssociatedClip == _disoPreviewPlayer.DefaultClip)
            {
                return;
            }
            await base.Preview();
        }

        public override void Deactivated()
        {
            _config.EnabledMenuClicks.Remove(File);
        }
    }
}