using System;
using Pomo.Shared.Constants;
using UnityEngine;

namespace Pomo.ServicesAdapters.Audio
{
    public enum SoundCue
    {
        MemoryMatch,
        MemoryMismatch,
        MemoryRoundCompleted,
        MemoryTimeUp,
        MemoryCardFlip,
        MemoryButtonClick
    }

    /// <summary>
    /// Audio integration point. Playback is requested only when sound is
    /// enabled; the presentation controller handles the optional Unity clip
    /// without coupling timer logic to audio playback.
    /// </summary>
    public class AudioService
    {
        public bool IsEnabled => PlayerPrefs.GetInt(AppConstants.AudioPreferenceKey, 1) == 1;

        public event Action PlaybackRequested;
        public event Action<SoundCue> CuePlaybackRequested;

        public void SetEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(AppConstants.AudioPreferenceKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
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
