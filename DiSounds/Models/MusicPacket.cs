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

        [InjectOptional]
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
            if (_disoPreviewPlayer != null)
            {
                AssociatedClip = await _cachedMediaAsyncLoader.LoadAudioClipAsync(Source, _cancellationTokenSource.Token);
                if (AssociatedClip == _disoPreviewPlayer.DefaultAudioClip)
                {
                    return;
                }
            }
            await base.Preview();
        }

        public override void Deactivated()
        {
            _config.EnabledMusicFiles.Remove(File);
        }
    }
}