using Zenject;
using System.IO;

namespace DiSounds.Models
{
    internal class IntroPacket : DisoAudioPacket
    {
        public FileInfo File { get; }

        [Inject]
        protected readonly Config _config = null!;

        public IntroPacket(FileInfo file, bool enabled) : base(file.Name, file.FullName)
        {
            File = file;
            _enabled = enabled;
        }

        public override void Activated()
        {
            _config.EnabledIntroSounds.Add(File);
        }

        public override void Deactivated()
        {
            _config.EnabledIntroSounds.Remove(File);
        }
    }
}