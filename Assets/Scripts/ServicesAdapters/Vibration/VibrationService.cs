using Pomo.Shared.Constants;
using UnityEngine;

namespace Pomo.ServicesAdapters.Vibration
{
    public static class VibrationService
    {
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt(AppConstants.VibrationPreferenceKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(AppConstants.VibrationPreferenceKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static void Pulse()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (Enabled && Application.isFocused) Handheld.Vibrate();
#endif
        }
    }
}
