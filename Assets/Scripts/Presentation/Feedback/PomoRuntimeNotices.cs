using Pomo.Data.Persistence;
using Pomo.Presentation.UI;
using Pomo.ServicesAdapters.Vibration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Presentation.Feedback
{
    [DisallowMultipleComponent, RequireComponent(typeof(Canvas))]
    public sealed class PomoRuntimeNotices : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset font;
        private static PomoRuntimeNotices instance;
        private GameObject panel;
        private TMP_Text messageText;
        private Button dismiss;
        private float cueExpiresAt;
        private bool storageNoticeVisible;
        private string pendingCue;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            instance = null;
            JsonStorageService.ClearNotices();
        }

        private void OnEnable() => instance = this;

        private void OnDisable()
        {
            if (instance == this) instance = null;
        }

        public static void ShowMilestone(string message, bool showBanner = true, bool announce = true)
        {
            VibrationService.Pulse();
            if (instance != null && showBanner) instance.pendingCue = message;
            else if (announce) PomoScreenReader.Announce(message);
        }

        private void LateUpdate()
        {
            if (storageNoticeVisible) return;

            if (JsonStorageService.TryTakeNotice(out string message))
            {
                storageNoticeVisible = true;
                Show(message, true);
            }
            else if (pendingCue != null)
            {
                Show(pendingCue, false);
                pendingCue = null;
                cueExpiresAt = Time.unscaledTime + 5f;
            }
            else if (panel != null && panel.activeSelf && Time.unscaledTime >= cueExpiresAt)
            {
                panel.SetActive(false);
            }
        }

        private void Show(string message, bool needsDismissal)
        {
            if (panel == null) CreatePanel();
            panel.transform.SetAsLastSibling();
            messageText.text = message;
            dismiss.gameObject.SetActive(needsDismissal);
            messageText.rectTransform.anchorMin = new Vector2(0.04f, needsDismissal ? 0.36f : 0.12f);
            var root = (RectTransform)panel.transform;
            root.sizeDelta = new Vector2(0f, needsDismissal ? 325f : 164f);
            root.anchoredPosition = new Vector2(0f, -18f);
            panel.SetActive(true);
            PomoScreenReader.Announce(message);
        }

        private void CreatePanel()
        {
            panel = new GameObject("Aviso POMO", typeof(RectTransform), typeof(Image));
            panel.layer = gameObject.layer;
            var rect = (RectTransform)panel.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = new Vector2(0.045f, 1f);
            rect.anchorMax = new Vector2(0.955f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            var background = panel.GetComponent<Image>();
            background.color = new Color32(19, 52, 92, 255);
            background.raycastTarget = false;

            messageText = CreateLabel("Mensaje", panel.transform, new Vector2(.04f, .12f), new Vector2(.96f, .94f));
            messageText.fontSizeMax = 45f;
            messageText.fontSizeMin = 30f;
            messageText.enableAutoSizing = true;

            var buttonObject = new GameObject("Entendido", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.layer = gameObject.layer;
            var buttonRect = (RectTransform)buttonObject.transform;
            buttonRect.SetParent(panel.transform, false);
            Anchor(buttonRect, new Vector2(.27f, .035f), new Vector2(.73f, .33f));
            var buttonImage = buttonObject.GetComponent<Image>();
            buttonImage.color = new Color32(218, 239, 255, 255);
            dismiss = buttonObject.GetComponent<Button>();
            dismiss.targetGraphic = buttonImage;
            dismiss.onClick.AddListener(() =>
            {
                storageNoticeVisible = false;
                panel.SetActive(false);
            });
            var label = CreateLabel("Etiqueta", buttonRect, Vector2.zero, Vector2.one);
            label.text = "Entendido";
            label.color = new Color32(19, 52, 92, 255);
        }

        private TextMeshProUGUI CreateLabel(string label, Transform parent, Vector2 min, Vector2 max)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.layer = gameObject.layer;
            var text = go.GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(parent, false);
            Anchor(text.rectTransform, min, max);
            if (font != null) text.font = font;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 38f;
            text.color = Color.white;
            text.richText = false;
            text.raycastTarget = false;
            return text;
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
