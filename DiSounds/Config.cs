﻿using System;
using System.IO;
using IPA.Config.Stores;
using SiraUtil.Converters;
using Version = SemVer.Version;
using System.Collections.Generic;
using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace DiSounds
{
    internal class Config
    {
        public Action<Config>? Updated;

        [NonNullable, UseConverter(typeof(VersionConverter))]
        public Version Version { get; set; } = new Version("0.0.0");
        

        [NonNullable, UseConverter(typeof(ListConverter<FileInfo, FileInfoConverter>))]
        public virtual List<FileInfo> EnabledMusicFiles { get; set; } = new List<FileInfo>();
        [UseConverter(typeof(EnumConverter<PlaybackType>))]
        public virtual PlaybackType MusicSource { get; set; } = PlaybackType.CustomSongs;
        public virtual bool MusicPlayerEnabled { get; set; } = false;
        public virtual bool SaveTime { get; set; } = false;


        [NonNullable, UseConverter(typeof(ListConverter<FileInfo, FileInfoConverter>))]
        public virtual List<FileInfo> EnabledMenuClicks { get; set; } = new List<FileInfo>();
        public virtual bool MenuClicksEnabled { get; set; } = false;
        public virtual bool UseMenuDefault { get; set; } = true;
        public virtual bool DoMenuPitch { get; set; } = true;
        public virtual float MinPitch { get; set; } = 0.95f;
        public virtual float MaxPitch { get; set; } = 1.05f;

        public enum PlaybackType
        {
            CustomSongs,
            CustomFiles
        }

        public virtual void Changed()
        {
            Updated?.Invoke(this);
        }
    }
}