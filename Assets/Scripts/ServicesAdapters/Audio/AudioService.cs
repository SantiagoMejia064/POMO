using System;

namespace Pomo.ServicesAdapters.Audio
{
    public enum SoundCue
    {
        MemoryMatch,
        MemoryMismatch,
        MemoryRoundCompleted,
        MemoryTimeUp
    }

    /// <summary>
    /// Audio integration point. Playback is requested only when sound is
    /// enabled; the presentation controller handles the optional Unity clip
    /// without coupling timer logic to audio playback.
    /// </summary>
    public class AudioService
    {
        // La preferencia se comparte entre Trabajo y Minijuegos durante esta sesión.
        private static bool sessionSoundEnabled = true;

        public bool IsEnabled => sessionSoundEnabled;

        public event Action PlaybackRequested;
        public event Action<SoundCue> CuePlaybackRequested;

        public void SetEnabled(bool enabled)
        {
            sessionSoundEnabled = enabled;
        }

        public void RequestCycleCompletedPlayback()
        {
            if (IsEnabled)
            {
                PlaybackRequested?.Invoke();
            }
        }

        public void RequestCuePlayback(SoundCue cue)
        {
            if (IsEnabled)
            {
                CuePlaybackRequested?.Invoke(cue);
            }
        }
    }
}
