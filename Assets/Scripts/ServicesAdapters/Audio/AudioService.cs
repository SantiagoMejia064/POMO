using System;

namespace Pomo.ServicesAdapters.Audio
{
    /// <summary>
    /// Audio integration point. Playback is requested only when sound is
    /// enabled; the presentation controller handles the optional Unity clip
    /// without coupling timer logic to audio playback.
    /// </summary>
    public class AudioService
    {
        public bool IsEnabled { get; private set; } = true;

        public event Action PlaybackRequested;

        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
        }

        public void RequestCycleCompletedPlayback()
        {
            if (IsEnabled)
            {
                PlaybackRequested?.Invoke();
            }
        }
    }
}
