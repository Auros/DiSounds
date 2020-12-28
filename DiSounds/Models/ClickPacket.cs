using Zenject;
using System.IO;
using UnityEngine;
using DiSounds.Managers;
using SiraUtil.Interfaces;
using System.Threading.Tasks;

namespace DiSounds.Models
{
    internal class ClickPacket : DisoAudioPacket
    {
        public FileInfo File { get; }

        [Inject]
        protected readonly Config _config = null!;

        [Inject(Id = nameof(DiClickManager))]
        protected readonly IRegistrar<AudioClip> _registrar = null!;

        public ClickPacket(FileInfo file, bool enabled) : base(file.Name, file.FullName)
        {
            File = file;
            _enabled = enabled;
        }

        public override async Task Preview()
        {
            // <[ w h a t   t h e   f u c k ]>
            IToggleable? toggleable = _registrar as IToggleable;
            if (!(toggleable is null)) toggleable.Status = false;
            await SiraUtil.Utilities.PauseChamp;
            await base.Preview();
            await SiraUtil.Utilities.PauseChamp;
            if (!(toggleable is null)) toggleable.Status = true;
        }

        public override void Activated()
        {
            _config.EnabledMenuClicks.Add(File);
        }

        public override void Deactivated()
        {
            _config.EnabledMenuClicks.Remove(File);
        }
    }
}