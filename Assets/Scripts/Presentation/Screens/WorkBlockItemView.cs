using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Presentation.Screens
{
    [DisallowMultipleComponent]
    public class WorkBlockItemView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text taskText;
        [SerializeField] private TMP_Text durationText;
        [SerializeField] private TMP_Text favoriteText;
        [SerializeField] private Button selectionButton;

        [SerializeField] private Image backgroundImage;

        private static readonly Color SelectedColor = new Color(0.15f, 0.72f, 0.57f, 1f);
        private Color normalColor;
        private bool hasNormalColor;

        public void Bind(
            string blockName,
            string taskName,
            int focusMinutes,
            int breakMinutes,
            bool isFavorite,
            Action onSelected)
        {
            if (nameText != null)
            {
                nameText.text = blockName;
                nameText.fontSize = 32f;
            }

            if (taskText != null)
            {
                taskText.text = taskName;
                taskText.fontSize = 20f;
            }

            if (durationText != null)
            {
                durationText.text = $"{focusMinutes} min trabajo · {breakMinutes} min descanso";
                durationText.fontSize = 20f;
            }
            if (favoriteText != null) favoriteText.text = isFavorite ? "★" : "☆";

            if (selectionButton == null)
            {
                return;
            }

            CacheBackground();
            EnsureCompactHeight();
            SetSelected(false);
            selectionButton.onClick.RemoveAllListeners();
            selectionButton.onClick.AddListener(() => onSelected?.Invoke());
        }

        public void BindTitleOnly(string title, Action onSelected)
        {
            if (nameText != null)
            {
                nameText.text = title;
                nameText.fontSize = 36f;
            }
            if (taskText != null) taskText.text = string.Empty;
            if (durationText != null) durationText.text = string.Empty;
            if (favoriteText != null) favoriteText.text = string.Empty;

            if (selectionButton == null)
            {
                return;
            }

            CacheBackground();
            EnsureCompactHeight();
            SetSelected(false);
            selectionButton.onClick.RemoveAllListeners();
            selectionButton.onClick.AddListener(() => onSelected?.Invoke());
        }

        public void SetSelected(bool isSelected)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = isSelected ? SelectedColor : normalColor;
            }
        }

        private void CacheBackground()
        {
            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }

            if (backgroundImage != null && !hasNormalColor)
            {
                normalColor = backgroundImage.color;
                hasNormalColor = true;
            }
        }

        private void EnsureCompactHeight()
        {
            LayoutElement layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.minHeight = 128f;
            layoutElement.preferredHeight = 128f;
        }
    }
}
