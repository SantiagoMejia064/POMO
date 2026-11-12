
using System;
using System.Collections.Generic;
using System.Linq;
using Pomo.Data.Persistence;
using Pomo.Presentation.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Presentation.Screens
{
    [DisallowMultipleComponent]
    public sealed class HealthyHabitsFeedbackController : MonoBehaviour
    {
        [Serializable]
        public class HabitEntry
        {
            [Header("Elementos de la casilla")]
            public Toggle toggle;

            public TMP_Text title;
            public TMP_Text description;
        }

        [Header("Los 12 hábitos saludables")]
        [SerializeField]
        private HabitEntry[] habits = new HabitEntry[12];

        [Header("Retroalimentación")]
        [SerializeField] private TMP_Text excellentFeedback;
        [SerializeField] private bool scrollFeedback;

        private const string TitleSuffix = "_title";
        private const string DescriptionSuffix = "_description";

        private void Awake()
        {
            if (habits == null)
                habits = new HabitEntry[12];

            for (int i = 0; i < habits.Length; i++)
            {
                HabitEntry habit = habits[i];

                if (habit == null || habit.toggle == null)
                    continue;

                string key = Key(i);

                // Recuperar título y descripción guardados.
                if (habit.title != null)
                {
                    string savedTitle = PlayerPrefs.GetString(
                        key + TitleSuffix,
                        habit.title.text
                    );

                    habit.title.text = savedTitle;
                }

                if (habit.description != null)
                {
                    string savedDescription = PlayerPrefs.GetString(
                        key + DescriptionSuffix,
                        habit.description.text
                    );

                    habit.description.text = savedDescription;
                }

                // Recuperar si el usuario ya practica el hábito.
                bool practiced = PlayerPrefs.GetInt(key, 0) == 1;

                habit.toggle.SetIsOnWithoutNotify(practiced);

                // Guardar cambios automáticamente.
                int capturedIndex = i;

                habit.toggle.onValueChanged.AddListener(
                    value => SaveHabit(capturedIndex, value)
                );

                // Guardar los textos iniciales.
                SaveHabitTexts(i);
            }

            if (scrollFeedback && excellentFeedback != null)
                PrepareFeedbackScroll();

            UpdateFeedback();
        }

        private void OnEnable()
        {
            UpdateFeedback();
        }

        private void OnDestroy()
        {
            if (habits == null) return;

            for (int i = 0; i < habits.Length; i++)
            {
                if (habits[i] == null || habits[i].toggle == null)
                    continue;

                habits[i].toggle.onValueChanged.RemoveAllListeners();
            }
        }

        // =====================================================
        // GUARDAR SELECCIÓN DEL USUARIO
        // =====================================================

        private void SaveHabit(int index, bool completed)
        {
            PlayerPrefs.SetInt(
                Key(index),
                completed ? 1 : 0
            );

            SaveHabitTexts(index);

            PlayerPrefs.Save();

            UpdateFeedback();
        }

        // =====================================================
        // GUARDAR TÍTULO Y DESCRIPCIÓN
        // =====================================================

        private void SaveHabitTexts(int index)
        {
            if (index < 0 || index >= habits.Length)
                return;

            HabitEntry habit = habits[index];

            if (habit == null) return;

            string key = Key(index);

            if (habit.title != null)
            {
                PlayerPrefs.SetString(
                    key + TitleSuffix,
                    habit.title.text
                );
            }

            if (habit.description != null)
            {
                PlayerPrefs.SetString(
                    key + DescriptionSuffix,
                    habit.description.text
                );
            }
        }

        // =====================================================
        // CONSULTAR HÁBITOS PARA LOS CONSEJOS DE POMO
        // =====================================================

        public bool IsHabitAlreadyPracticed(int index)
        {
            if (index < 0 || index >= habits.Length)
                return false;

            if (habits[index] == null ||
                habits[index].toggle == null)
                return false;

            return habits[index].toggle.isOn;
        }

        public string GetHabitTitle(int index)
        {
            if (index < 0 || index >= habits.Length)
                return string.Empty;

            return habits[index]?.title != null
                ? habits[index].title.text
                : string.Empty;
        }

        public string GetHabitDescription(int index)
        {
            if (index < 0 || index >= habits.Length)
                return string.Empty;

            return habits[index]?.description != null
                ? habits[index].description.text
                : string.Empty;
        }

        public int HabitCount => habits != null ? habits.Length : 0;

        // Devuelve los índices de los hábitos que necesita mejorar.
        public List<int> GetHabitsToImprove()
        {
            List<int> pending = new List<int>();

            for (int i = 0; i < habits.Length; i++)
            {
                if (habits[i] != null &&
                    habits[i].toggle != null &&
                    !habits[i].toggle.isOn)
                {
                    pending.Add(i);
                }
            }

            return pending;
        }

        // Devuelve los índices de los hábitos que ya practica.
        public List<int> GetPracticedHabits()
        {
            List<int> practiced = new List<int>();

            for (int i = 0; i < habits.Length; i++)
            {
                if (IsHabitAlreadyPracticed(i))
                    practiced.Add(i);
            }

            return practiced;
        }

        // =====================================================
        // RETROALIMENTACIÓN DE HÁBITOS PENDIENTES
        // =====================================================

        private void UpdateFeedback()
        {
            if (excellentFeedback == null || habits == null)
                return;

            List<string> pending = GetHabitsToImprove()
                .Select(index => GetHabitTitle(index))
                .Where(title => !string.IsNullOrWhiteSpace(title))
                .ToList();

            if (pending.Count == 0)
            {
                excellentFeedback.text =
                    "¡Excelente! Ya tienes todos estos hábitos incorporados.";
                return;
            }

            excellentFeedback.text = string.Join(
                "\n",
                pending.Select(title => "• " + title)
            );
        }

        // =====================================================
        // SCROLL DE RETROALIMENTACIÓN
        // =====================================================

        private void PrepareFeedbackScroll()
        {
            RectTransform text = excellentFeedback.rectTransform;

            RectTransform viewport = PomoVisualStyle.Rect(
                "Retroalimentación de hábitos",
                text.parent,
                new Vector2(.12f, .33f),
                new Vector2(.88f, .66f)
            );

            viewport.gameObject.AddComponent<Image>().color =
                Color.clear;

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

            PomoVisualStyle.Bounds(
                text,
                Vector2.up,
                Vector2.one
            );

            excellentFeedback.alignment =
                TextAlignmentOptions.TopLeft;

            excellentFeedback.fontSize = 39;
            excellentFeedback.enableAutoSizing = false;
            excellentFeedback.textWrappingMode =
                TextWrappingModes.Normal;

            excellentFeedback.overflowMode =
                TextOverflowModes.Overflow;

            excellentFeedback.raycastTarget = false;

            text.gameObject
                .AddComponent<ContentSizeFitter>()
                .verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        // =====================================================
        // CLAVES DE PERSISTENCIA
        // =====================================================

        private static string Key(int index)
        {
            return LocalUserProfile.HealthyHabitKey(index);
        }
    }
}
