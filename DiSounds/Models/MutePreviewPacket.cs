using System.Threading.Tasks;
using Zenject;

namespace DiSounds.Models
{
    internal abstract class MutePreviewPacket : DisoAudioPacket
    {
        [Inject]
        protected readonly SongPreviewPlayer _songPreviewPlayer = null!;

        protected MutePreviewPacket(string name, string source) : base(name, source)
        {

        }

        public async override Task Preview()
        {
            _songPreviewPlayer.PauseCurrentChannel();
            await base.Preview();
            if (_audioSourcer.clip == AssociatedClip)
                _songPreviewPlayer.UnPauseCurrentChannel();
        }
    }
}