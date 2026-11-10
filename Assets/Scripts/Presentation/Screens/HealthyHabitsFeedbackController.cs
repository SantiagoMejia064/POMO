using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Presentation.Screens
{
    /// <summary>Persists the healthy-habit checklist and turns unchecked habits into gentle feedback.</summary>
    [DisallowMultipleComponent]
    public sealed class HealthyHabitsFeedbackController : MonoBehaviour
    {
        private const string PreferencePrefix = "pomo.healthy-habits.v2.";

        [SerializeField] private TMP_Text excellentFeedback;

        private Toggle[] habits;
        private TMP_Text[] labels;

        private void Awake()
        {
            habits = GetComponentsInChildren<Toggle>(true);
            labels = GetComponentsInChildren<TMP_Text>(true);

            for (int index = 0; index < habits.Length; index++)
            {
                int capturedIndex = index;
                habits[index].SetIsOnWithoutNotify(PlayerPrefs.GetInt(Key(capturedIndex), 0) == 1);
                habits[index].onValueChanged.AddListener(value => SaveHabit(capturedIndex, value));
            }

            UpdateFeedback();
        }

        private void OnEnable()
        {
            UpdateFeedback();
        }

        private void OnDisable()
        {
            UpdateFeedback();
        }

        private void SaveHabit(int index, bool completed)
        {
            PlayerPrefs.SetInt(Key(index), completed ? 1 : 0);
            PlayerPrefs.Save();
            UpdateFeedback();
        }

        private void UpdateFeedback()
        {
            if (excellentFeedback == null || habits == null) return;

            List<string> pending = new List<string>();
            for (int index = 0; index < habits.Length; index++)
            {
                if (habits[index] != null && !habits[index].isOn)
                {
                    pending.Add(GetHabitLabel(habits[index], index));
                }
            }

            if (pending.Count == 0)
            {
                excellentFeedback.text = "• Todos los hábitos están marcados.";
                return;
            }

            string habitList = string.Join("\n", pending.Select(habit => "• " + habit));
            excellentFeedback.text = habitList;
        }

        private string GetHabitLabel(Toggle habit, int index)
        {
            TMP_Text closest = null;
            TMP_Text closestTitle = null;
            float shortestDistance = float.MaxValue;
            float shortestTitleDistance = float.MaxValue;
            Vector3 habitPosition = habit.transform.position;

            foreach (TMP_Text label in labels)
            {
                if (label == null || label == excellentFeedback || !IsHabitLabel(label.text)) continue;
                float distance = Vector3.SqrMagnitude(label.transform.position - habitPosition);
                if (distance < shortestDistance)
                {
                    shortestDistance = distance;
                    closest = label;
                }
                if (IsHabitTitle(label.text) && distance < shortestTitleDistance)
                {
                    shortestTitleDistance = distance;
                    closestTitle = label;
                }
            }

            TMP_Text selectedLabel = closestTitle ?? closest;
            if (selectedLabel == null) return "hábito " + (index + 1);
            return selectedLabel.text.Replace('\n', ' ').Trim();
        }

        private static bool IsHabitLabel(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string normalized = text.Replace('\n', ' ').Trim();
            return !string.Equals(normalized, "Siguiente", System.StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(normalized, "Hábitos saludables", System.StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(normalized, "Habitos saludables", System.StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsHabitTitle(string text)
        {
            string normalized = text.Replace('\n', ' ').Trim();
            return normalized.Length > 0 && normalized.Any(char.IsLetter) &&
                string.Equals(normalized, normalized.ToUpperInvariant(), System.StringComparison.Ordinal);
        }

        private static string Key(int index) => PreferencePrefix + index;
    }
}
