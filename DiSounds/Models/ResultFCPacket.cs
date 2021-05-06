using Zenject;
using System.IO;

namespace DiSounds.Models
{
    internal class ResultFCPacket : MutePreviewPacket
    {
        public FileInfo File { get; }

        [Inject]
        protected readonly Config _config = null!;

        public ResultFCPacket(FileInfo file, bool enabled) : base(file.Name, file.FullName)
        {
            File = file;
            _enabled = enabled;
        }

        public override void Activated()
        {
            _config.EnabledResultFCSounds.Add(File);
        }

        public override void Deactivated()
        {
            _config.EnabledResultFCSounds.Remove(File);
        }
    }
}