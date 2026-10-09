using System;

namespace Pomo.ServicesAdapters.Audio
{
    /// <summary>
    /// Audio integration point. Playback is requested only when sound is
    /// enabled; a future Unity/Android adapter can subscribe without changing
    /// timer logic.
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
