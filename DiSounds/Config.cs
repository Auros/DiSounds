using SemVer;
using System.IO;
using IPA.Config.Stores;
using SiraUtil.Converters;
using System.Collections.Generic;
using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace DiSounds
{
    internal class Config
    {
        [NonNullable, UseConverter(typeof(VersionConverter))]
        public virtual Version Version { get; set; } = new Version("0.0.0");

        [NonNullable, UseConverter(typeof(ListConverter<FileInfo, FileInfoConverter>))]
        public virtual List<FileInfo> EnabledMenuClicks { get; set; } = new List<FileInfo>();

        public virtual bool MenuClicksEnabled { get; set; } = false;
        public virtual bool UseMenuDefault { get; set; } = true;
        public virtual bool DoMenuPitch { get; set; } = true;
        public virtual float MinPitch { get; set; } = 0.95f;
        public virtual float MaxPitch { get; set; } = 1.05f;
    }
}