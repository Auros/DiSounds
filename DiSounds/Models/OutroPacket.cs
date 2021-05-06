using Zenject;
using System.IO;
using System.Threading.Tasks;

namespace DiSounds.Models
{
    internal class OutroPacket : MutePreviewPacket
    {
        public FileInfo File { get; }

        [Inject]
        protected readonly Config _config = null!;

        public OutroPacket(FileInfo file, bool enabled) : base(file.Name, file.FullName)
        {
            File = file;
            _enabled = enabled;
        }

        public override void Activated()
        {
            _config.EnabledOutroSounds.Add(File);
        }

        public override void Deactivated()
        {
            _config.EnabledOutroSounds.Remove(File);
        }
    }
}