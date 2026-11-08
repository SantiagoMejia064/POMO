using Pomo.ServicesAdapters.Audio;
using UnityEngine;

namespace Pomo.ServicesAdapters.Android
{
    [DisallowMultipleComponent, RequireComponent(typeof(AudioSource))]
    public sealed class AudioBridge : MonoBehaviour
    {
        private readonly AudioService audioService = new AudioService();
        private AudioSource source;

        private void Awake() => source = GetComponent<AudioSource>();

        public void PlayButtonSound()
        {
            if (audioService.IsEnabled) source.Play();
        }
    }
}
