using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Presentation.Screens
{
    [DisallowMultipleComponent]
    public class BlockTaskSelectionItemView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Button selectionButton;
        [SerializeField] private Image backgroundImage;

        private static readonly Color SelectedColor = new Color(0.15f, 0.72f, 0.57f, 1f);
        private Color normalColor;
        private bool hasNormalColor;

        public void Bind(string title, Action onSelected)
        {
            if (titleText != null)
            {
                titleText.text = title;
            }

            if (selectionButton == null)
            {
                return;
            }

            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }

            if (backgroundImage != null && !hasNormalColor)
            {
                normalColor = backgroundImage.color;
                hasNormalColor = true;
            }

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

        private void EnsureCompactHeight()
        {
            LayoutElement layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.minHeight = 100f;
            layoutElement.preferredHeight = 100f;
        }
    }
}
