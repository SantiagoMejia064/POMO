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

            layoutElement.minHeight = 140f;
            layoutElement.preferredHeight = 140f;
            layoutElement.flexibleHeight = 0f;
            layoutElement.minWidth = 0f;
            layoutElement.flexibleWidth = 1f;
            if (titleText == null) return;
            // The original prefab label was taller than the compact row and
            // intercepted taps intended for the neighbouring row.
            titleText.raycastTarget = false;
            titleText.rectTransform.anchorMin = Vector2.zero;
            titleText.rectTransform.anchorMax = Vector2.one;
            titleText.rectTransform.offsetMin = new Vector2(24f, 12f);
            titleText.rectTransform.offsetMax = new Vector2(-24f, -12f);
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 28f;
            titleText.fontSizeMax = 48f;
            titleText.textWrappingMode = TextWrappingModes.Normal;
            titleText.overflowMode = TextOverflowModes.Ellipsis;
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
        }

        public void ApplyTypography(TMP_FontAsset font)
        {
            if (titleText == null || font == null) return;
            titleText.font = font;
            titleText.fontSharedMaterial = font.material;
            titleText.color = Color.white;
        }
    }
}
