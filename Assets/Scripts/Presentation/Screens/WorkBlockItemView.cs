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
        [SerializeField] private Button favoriteToggleButton;
        [SerializeField] private Sprite favoriteOnSprite;
        [SerializeField] private Sprite favoriteOffSprite;
        private Image favoriteIcon;

        [SerializeField] private Image backgroundImage;

        private static readonly Color SelectedColor = new Color(0.15f, 0.72f, 0.57f, 1f);
        private Color normalColor;
        private bool hasNormalColor;
        private bool hasReservedFavoriteSpace;

        public void Bind(
            string blockName,
            string taskName,
            int focusMinutes,
            int breakMinutes,
            bool isFavorite,
            Action onSelected,
            Action onFavoriteToggle = null)
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
            if (favoriteText != null) favoriteText.text = string.Empty;

            if (selectionButton == null)
            {
                return;
            }

            CacheBackground();
            EnsureCompactHeight(false, onFavoriteToggle != null);
            ConfigureFavoriteToggle(isFavorite, onFavoriteToggle);
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
            if (favoriteToggleButton != null) favoriteToggleButton.gameObject.SetActive(false);

            if (selectionButton == null)
            {
                return;
            }

            CacheBackground();
            EnsureCompactHeight(true, false);
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

        private void EnsureCompactHeight(bool titleOnly, bool reserveFavorite)
        {
            LayoutElement layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            bool hasTask = taskText != null && !string.IsNullOrWhiteSpace(taskText.text);
            layoutElement.minHeight = hasTask ? 220f : 160f;
            layoutElement.preferredHeight = layoutElement.minHeight;
            layoutElement.flexibleHeight = 0f;
            layoutElement.minWidth = 0f;
            layoutElement.flexibleWidth = 1f;
            float right = reserveFavorite ? 112f : 24f;
            FitLabel(nameText, titleOnly ? 0f : hasTask ? .66f : .46f, 1f, right, 46f);
            FitLabel(taskText, .33f, .66f, right, 30f);
            FitLabel(durationText, 0f, hasTask ? .33f : .46f, right, 30f);
        }

        private static void FitLabel(TMP_Text text, float bottom, float top, float right, float size)
        {
            if (text == null) return;
            text.raycastTarget = false;
            text.rectTransform.anchorMin = new Vector2(0f, bottom);
            text.rectTransform.anchorMax = new Vector2(1f, top);
            text.rectTransform.offsetMin = new Vector2(24f, 6f);
            text.rectTransform.offsetMax = new Vector2(-right, -6f);
            text.enableAutoSizing = true;
            text.fontSizeMin = size * .8f;
            text.fontSizeMax = size;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.alignment = TextAlignmentOptions.MidlineLeft;
        }

        public void ApplyTypography(TMP_FontAsset titleFont, TMP_FontAsset bodyFont)
        {
            ApplyFont(nameText, titleFont);
            ApplyFont(taskText, bodyFont);
            ApplyFont(durationText, bodyFont);
        }

        private static void ApplyFont(TMP_Text text, TMP_FontAsset font)
        {
            if (text == null || font == null) return;
            text.font = font;
            text.fontSharedMaterial = font.material;
            text.color = Color.white;
        }

        private void ConfigureFavoriteToggle(bool isFavorite, Action onFavoriteToggle)
        {
            if (onFavoriteToggle == null)
            {
                if (favoriteToggleButton != null)
                {
                    favoriteToggleButton.gameObject.SetActive(false);
                }

                return;
            }

            EnsureFavoriteToggleButton();
            favoriteToggleButton.gameObject.SetActive(true);
            favoriteText.text = string.Empty;
            favoriteText.raycastTarget = false;
            if (favoriteIcon == null)
            {
                var icon = new GameObject("Estrella de favorito", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                icon.transform.SetParent(favoriteText.transform, false);
                favoriteIcon = icon.GetComponent<Image>();
                favoriteIcon.rectTransform.anchorMin = Vector2.zero;
                favoriteIcon.rectTransform.anchorMax = Vector2.one;
                favoriteIcon.rectTransform.offsetMin = new Vector2(10f, 10f);
                favoriteIcon.rectTransform.offsetMax = new Vector2(-10f, -10f);
                favoriteIcon.preserveAspect = true;
            }
            favoriteIcon.sprite = isFavorite ? favoriteOnSprite : favoriteOffSprite;
            favoriteIcon.color = Color.white;
            favoriteToggleButton.targetGraphic = favoriteIcon;
            favoriteToggleButton.onClick.RemoveAllListeners();
            favoriteToggleButton.onClick.AddListener(() => onFavoriteToggle());
        }

        private void EnsureFavoriteToggleButton()
        {
            if (favoriteText == null)
            {
                GameObject toggleObject = new GameObject(
                    "Favorito",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
                toggleObject.transform.SetParent(transform, false);

                RectTransform toggleRect = toggleObject.GetComponent<RectTransform>();
                toggleRect.anchorMin = Vector2.one;
                toggleRect.anchorMax = Vector2.one;
                toggleRect.pivot = Vector2.one;
                toggleRect.anchoredPosition = new Vector2(-20f, -14f);
                toggleRect.sizeDelta = new Vector2(82f, 82f);

                favoriteText = toggleObject.GetComponent<TextMeshProUGUI>();
                favoriteText.font = TMP_Settings.defaultFontAsset;
                favoriteText.fontSize = 48f;
                favoriteText.alignment = TextAlignmentOptions.Center;
                favoriteText.enableWordWrapping = false;
                favoriteText.raycastTarget = true;
            }

            if (favoriteToggleButton == null)
            {
                favoriteToggleButton = favoriteText.GetComponent<Button>();
                if (favoriteToggleButton == null)
                {
                    favoriteToggleButton = favoriteText.gameObject.AddComponent<Button>();
                }
            }

            favoriteToggleButton.targetGraphic = favoriteText;
            UnityEngine.UI.Navigation navigation = favoriteToggleButton.navigation;
            navigation.mode = UnityEngine.UI.Navigation.Mode.None;
            favoriteToggleButton.navigation = navigation;

            ReserveFavoriteSpace(nameText);
            ReserveFavoriteSpace(durationText);
        }

        private void ReserveFavoriteSpace(TMP_Text text)
        {
            if (hasReservedFavoriteSpace || text == null)
            {
                return;
            }

            RectTransform textRect = text.rectTransform;
            if (textRect.anchorMin.x == 0f && textRect.anchorMax.x == 1f)
            {
                textRect.offsetMin = new Vector2(Mathf.Max(textRect.offsetMin.x, 24f), textRect.offsetMin.y);
                textRect.offsetMax = new Vector2(Mathf.Min(textRect.offsetMax.x, -100f), textRect.offsetMax.y);
            }

            if (text == durationText)
            {
                hasReservedFavoriteSpace = true;
            }
        }
    }
}
