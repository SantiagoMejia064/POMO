using System;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Pomo.Data.Persistence
{
    public static class LocalUserProfile
    {
        public const string NameKey = "pomo.profile.name";
        public const string OnboardingKey = "pomo.profile.onboarding-completed";
        public const int HealthyHabitCount = 13;
        public const int MaximumNameLength = 60;
        public static string Name => NormalizeName(PlayerPrefs.GetString(NameKey, string.Empty));
        // Older versions saved the name but had no completion flag.
        public static bool HasCompletedOnboarding => Name.Length > 0 && PlayerPrefs.GetInt(OnboardingKey, 1) == 1;

        public static string HealthyHabitKey(int index) => "pomo.healthy-habits.v2." + index;

        public static string NormalizeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            string name;
            try { name = value.Trim().Normalize(NormalizationForm.FormC); }
            catch (ArgumentException) { return string.Empty; }
            if (name.Length > MaximumNameLength) return string.Empty;
            foreach (char character in name)
                if (char.IsControl(character) || char.GetUnicodeCategory(character) == UnicodeCategory.Format)
                    return string.Empty;
            return name;
        }

        public static bool MatchesName(string written, string expected)
        {
            string name = NormalizeName(expected);
            return name.Length > 0 && string.Equals(NormalizeName(written), name, StringComparison.OrdinalIgnoreCase);
        }

        public static bool TrySaveName(string value)
        {
            return SaveName(value, false);
        }

        public static bool TryStartOnboarding(string value)
        {
            return SaveName(value, true);
        }

        private static bool SaveName(string value, bool startOnboarding)
        {
            string name = NormalizeName(value);
            if (name.Length == 0) return false;
            PlayerPrefs.SetString(NameKey, name);
            if (startOnboarding) PlayerPrefs.SetInt(OnboardingKey, 0);
            PlayerPrefs.Save();
            return true;
        }

        public static void CompleteOnboarding()
        {
            PlayerPrefs.SetInt(OnboardingKey, 1);
            PlayerPrefs.Save();
        }
    }
}
