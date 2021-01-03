using System.Threading.Tasks;

namespace DiSounds.Models
{
    internal class TutorialPacket : DisoAudioPacket
    {
        public TutorialPacket(string name, bool enabled) : base(name, "")
        {
            _enabled = enabled;
        }

        public override void Activated()
        {

        }

        public override async Task Preview()
        {
            await SiraUtil.Utilities.PauseChamp;
        }

        public override void Deactivated()
        {

        }
    }
}