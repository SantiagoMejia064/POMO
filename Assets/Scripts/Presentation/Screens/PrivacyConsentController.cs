using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pomo.Presentation.Screens
{
    /// <summary>Controls the saved consent overlay without changing the welcome flow.</summary>
    [DisallowMultipleComponent]
    public sealed class PrivacyConsentController : MonoBehaviour
    {
        public const string AcceptedVersionKey = "pomo.privacy.accepted-version";
        public const int NoticeVersion = 1;

        [SerializeField, InspectorName("Contenido de bienvenida")] private CanvasGroup welcomeContent;
        [SerializeField, InspectorName("Panel de consentimiento")] private GameObject consentPanel;
        [SerializeField, InspectorName("Cuadro de consentimiento")] private CanvasGroup consentCard;
        [SerializeField, InspectorName("Aviso completo")] private GameObject noticePanel;
        [SerializeField, InspectorName("Casilla de aceptación")] private Toggle acceptanceToggle;
        [SerializeField, InspectorName("Botón aceptar")] private Button acceptButton;
        [SerializeField, InspectorName("Botón leer términos")] private Button termsButton;
        [SerializeField, InspectorName("Botón volver")] private Button closeNoticeButton;
        [SerializeField, InspectorName("Texto del aviso")] private TMP_Text noticeText;
        [SerializeField, InspectorName("Área de lectura")] private ScrollRect noticeScroll;
        [SerializeField, InspectorName("Contenido del aviso")] private TextAsset privacyNotice;

        private bool initialized;
        private bool configured;
        private bool acceptedInCurrentOpening;
        private bool readingFromSettings;
        private Transform consentOriginalParent;
        private int consentOriginalSiblingIndex;
        private Action returnFromReading;

        [SerializeField] private NombreBienvenidaController nombreBienvenida;

        public bool HasAccepted => PlayerPrefs.GetInt(AcceptedVersionKey, 0) == NoticeVersion;
        public bool IsBlockingWelcome => consentPanel != null && consentPanel.activeInHierarchy;

        private void OnEnable()
        {
            Initialize();
            if (!configured) return;
            acceptanceToggle.SetIsOnWithoutNotify(false);
            acceptButton.interactable = false;
            noticePanel.SetActive(false);
            consentCard.interactable = true;
            consentCard.blocksRaycasts = true;
            // The saved scene decides which panel starts visible. Never skip it using an old acceptance.
            acceptedInCurrentOpening = !consentPanel.activeSelf;
            welcomeContent.interactable = acceptedInCurrentOpening;
            welcomeContent.blocksRaycasts = acceptedInCurrentOpening;
        }

        private void Initialize()
        {
            if (initialized) return;
            initialized = true;
            configured = welcomeContent != null && consentPanel != null && consentCard != null &&
                noticePanel != null && acceptanceToggle != null && acceptButton != null &&
                termsButton != null && closeNoticeButton != null && noticeText != null &&
                noticeScroll != null && privacyNotice != null && !string.IsNullOrWhiteSpace(privacyNotice.text);
            if (!configured)
            {
                if (welcomeContent != null) welcomeContent.interactable = false;
                if (acceptButton != null) acceptButton.interactable = false;
                Debug.LogError("POMO: faltan referencias del aviso de privacidad.", this);
                return;
            }
            noticeText.richText = false;
            noticeText.text = privacyNotice.text;
            acceptanceToggle.onValueChanged.AddListener(OnAcceptanceChanged);
            acceptButton.onClick.AddListener(Accept);
            termsButton.onClick.AddListener(OpenTerms);
            closeNoticeButton.onClick.AddListener(CloseTerms);
        }

        private void OnAcceptanceChanged(bool accepted)
        {
            acceptButton.interactable = accepted;
        }

        public void Accept()
        {
            // Also guard direct calls and keyboard submission, not only pointer clicks.
            if (!configured || !acceptanceToggle.isOn || noticePanel.activeSelf) return;
            PlayerPrefs.SetInt(AcceptedVersionKey, NoticeVersion);
            PlayerPrefs.Save();
            acceptedInCurrentOpening = true;
            if (nombreBienvenida != null) nombreBienvenida.PrepararEntrada();
            ApplyGate(false);
        }

        public void OpenTerms()
        {
            Initialize();
            if (!configured) return;
            ApplyGate(true);
            consentCard.interactable = false;
            consentCard.blocksRaycasts = false;
            noticePanel.SetActive(true);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(noticeScroll.content);
            noticeScroll.StopMovement();
            noticeScroll.verticalNormalizedPosition = 1f;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(closeNoticeButton.gameObject);
        }

        public void OpenTermsForReading(Action onClose)
        {
            Initialize();
            if (!configured || readingFromSettings) return;
            Canvas canvas = consentPanel.GetComponentInParent<Canvas>(true);
            if (canvas == null) return;
            consentOriginalParent = consentPanel.transform.parent;
            consentOriginalSiblingIndex = consentPanel.transform.GetSiblingIndex();
            returnFromReading = onClose;
            readingFromSettings = true;
            consentPanel.transform.SetParent(canvas.rootCanvas.transform, true);
            consentPanel.transform.SetAsLastSibling();
            consentCard.gameObject.SetActive(false);
            consentPanel.SetActive(true);
            noticePanel.SetActive(true);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(noticeScroll.content);
            noticeScroll.StopMovement();
            noticeScroll.verticalNormalizedPosition = 1f;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(closeNoticeButton.gameObject);
        }

        public void CloseTerms()
        {
            if (!configured) return;
            if (readingFromSettings)
            {
                noticePanel.SetActive(false);
                consentPanel.SetActive(false);
                consentPanel.transform.SetParent(consentOriginalParent, true);
                consentPanel.transform.SetSiblingIndex(consentOriginalSiblingIndex);
                consentCard.gameObject.SetActive(true);
                readingFromSettings = false;
                Action onClose = returnFromReading;
                returnFromReading = null;
                onClose?.Invoke();
                return;
            }
            noticePanel.SetActive(false);
            consentCard.interactable = true;
            consentCard.blocksRaycasts = true;
            ApplyGate(!acceptedInCurrentOpening);
            if (!acceptedInCurrentOpening && EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(termsButton.gameObject);
        }

        private void ApplyGate(bool blocked)
        {
            welcomeContent.interactable = !blocked;
            welcomeContent.blocksRaycasts = !blocked;
            consentPanel.SetActive(blocked);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }

        private void OnDestroy()
        {
            if (!configured) return;
            acceptanceToggle.onValueChanged.RemoveListener(OnAcceptanceChanged);
            acceptButton.onClick.RemoveListener(Accept);
            termsButton.onClick.RemoveListener(OpenTerms);
            closeNoticeButton.onClick.RemoveListener(CloseTerms);
        }
    }
}
