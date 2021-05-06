using Zenject;
using System.IO;

namespace DiSounds.Models
{
    internal class ResultFailedPacket : MutePreviewPacket
    {
        public FileInfo File { get; }

        [Inject]
        protected readonly Config _config = null!;

        public ResultFailedPacket(FileInfo file, bool enabled) : base(file.Name, file.FullName)
        {
            File = file;
            _enabled = enabled;
        }

        public override void Activated()
        {
            _config.EnabledResultFailedSounds.Add(File);
        }

        public override void Deactivated()
        {
            _config.EnabledResultFailedSounds.Remove(File);
        }
    }
}