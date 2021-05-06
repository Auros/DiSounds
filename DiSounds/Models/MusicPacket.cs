using Zenject;
using System.IO;
using DiSounds.Components;
using System.Threading.Tasks;

namespace DiSounds.Models
{
    internal class MusicPacket : MutePreviewPacket
    {
        public FileInfo File { get; }

        [Inject]
        protected readonly Config _config = null!;

        public MusicPacket(FileInfo file, bool enabled) : base(file.Name, file.FullName)
        {
            File = file;
            _enabled = enabled;
        }

        public override void Activated()
        {
            _config.EnabledMusicFiles.Add(File);
        }

        /*public override async Task Preview()
        {
            if (_disoPreviewPlayer != null)
            {
                AssociatedClip = await _cachedMediaAsyncLoader.LoadAudioClipAsync(Source, _cancellationTokenSource.Token);
                if (AssociatedClip == _disoPreviewPlayer.DefaultAudioClip)
                {
                    return;
                }
                _songPreviewPlayer.PauseCurrentChannel();
            }
            await base.Preview();
            if (_audioSourcer.clip == AssociatedClip)
                _songPreviewPlayer.UnPauseCurrentChannel();
        }*/

        public override void Deactivated()
        {
            _config.EnabledMusicFiles.Remove(File);
        }
    }
}