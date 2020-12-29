using UnityEngine;

namespace DiSounds.Models
{
    public struct AudioContainer
    {
        public string name;
        public AudioClip clip;
        public Texture2D? texture;

        public AudioContainer(string name, AudioClip clip, Texture2D? texture = null)
        {
            this.name = name;
            this.clip = clip;
            this.texture = texture;
        }
    }
}