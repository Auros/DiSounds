using System;
using DiSounds.Models;
using System.Threading.Tasks;

namespace DiSounds.Interfaces
{
    public interface IAudioContainerService
    {
        Action? Loaded { get; set; }
        Task<AudioContainer> GetRandomContainer();
    }
}