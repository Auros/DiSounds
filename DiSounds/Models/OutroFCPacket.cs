using Zenject;
using System.IO;

namespace DiSounds.Models
{
    internal class OutroFCPacket : DisoAudioPacket
    {
        public FileInfo File { get; }

        [Inject]
        protected readonly Config _config = null!;

        public OutroFCPacket(FileInfo file, bool enabled) : base(file.Name, file.FullName)
        {
            File = file;
            _enabled = enabled;
        }

        public override void Activated()
        {
            _config.EnabledOutroFCSounds.Add(File);
        }

        public override void Deactivated()
        {
            _config.EnabledOutroFCSounds.Remove(File);
        }
    }
}