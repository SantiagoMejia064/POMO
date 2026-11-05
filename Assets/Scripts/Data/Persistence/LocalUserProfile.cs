using System;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace Pomo.Data.Persistence
{
    public static class LocalUserProfile
    {
        public const string NameKey = "pomo.profile.name";
        public const int MaximumNameLength = 60;
        public static string Name => NormalizeName(PlayerPrefs.GetString(NameKey, string.Empty));

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
            string name = NormalizeName(value);
            if (name.Length == 0) return false;
            PlayerPrefs.SetString(NameKey, name);
            PlayerPrefs.Save();
            return true;
        }
    }
}
