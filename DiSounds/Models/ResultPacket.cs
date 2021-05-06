using Zenject;
using System.IO;

namespace DiSounds.Models
{
    internal class ResultPacket : MutePreviewPacket
    {
        public FileInfo File { get; }

        [Inject]
        protected readonly Config _config = null!;

        public ResultPacket(FileInfo file, bool enabled) : base(file.Name, file.FullName)
        {
            File = file;
            _enabled = enabled;
        }

        public override void Activated()
        {
            _config.EnabledResultSounds.Add(File);
        }

        public override void Deactivated()
        {
            _config.EnabledResultSounds.Remove(File);
        }
    }
}