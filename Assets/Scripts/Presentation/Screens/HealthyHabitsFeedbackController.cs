using System.Collections.Generic;
using System.Linq;
using Pomo.Data.Persistence;
using Pomo.Presentation.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Presentation.Screens
{
    /// <summary>Persists the healthy-habit checklist and turns unchecked habits into gentle feedback.</summary>
    [DisallowMultipleComponent]
    public sealed class HealthyHabitsFeedbackController : MonoBehaviour
    {
        [SerializeField] private TMP_Text excellentFeedback;
        [SerializeField] private bool scrollFeedback;

        private Toggle[] habits;
        private string[] labels;

        private void Awake()
        {
            habits = GetComponentsInChildren<Toggle>(true);
            labels = new string[habits.Length];
            if (scrollFeedback && excellentFeedback != null) PrepareFeedbackScroll();

            for (int index = 0; index < habits.Length; index++)
            {
                int capturedIndex = index;
                labels[index] = GetHabitLabel(habits[index], index);
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
                    pending.Add(labels[index]);
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

        private void PrepareFeedbackScroll()
        {
            RectTransform text = excellentFeedback.rectTransform;
            RectTransform viewport = PomoVisualStyle.Rect("Retroalimentación de hábitos", text.parent,
                new Vector2(.12f, .33f), new Vector2(.88f, .66f));
            viewport.gameObject.AddComponent<Image>().color = Color.clear;
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 90f;
            scroll.viewport = viewport;
            scroll.content = text;

            text.SetParent(viewport, false);
            text.localScale = Vector3.one;
            text.pivot = new Vector2(.5f, 1f);
            PomoVisualStyle.Bounds(text, Vector2.up, Vector2.one);
            excellentFeedback.alignment = TextAlignmentOptions.TopLeft;
            excellentFeedback.fontSize = 39;
            excellentFeedback.enableAutoSizing = false;
            excellentFeedback.textWrappingMode = TextWrappingModes.Normal;
            excellentFeedback.overflowMode = TextOverflowModes.Overflow;
            excellentFeedback.raycastTarget = false;
            text.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private static string GetHabitLabel(Toggle habit, int index)
        {
            var rowLabels = habit.transform.parent.GetComponentsInChildren<TMP_Text>(true)
                .Select(label => label.text.Replace('\n', ' ').Trim()).Where(label => label.Length > 0).ToArray();
            return rowLabels.FirstOrDefault(IsHabitTitle) ?? rowLabels.FirstOrDefault() ?? "hábito " + (index + 1);
        }

        private static bool IsHabitTitle(string text)
        {
            string normalized = text.Replace('\n', ' ').Trim();
            return normalized.Length > 0 && normalized.Any(char.IsLetter) &&
                string.Equals(normalized, normalized.ToUpperInvariant(), System.StringComparison.Ordinal);
        }

        private static string Key(int index) => LocalUserProfile.HealthyHabitKey(index);
    }
}
