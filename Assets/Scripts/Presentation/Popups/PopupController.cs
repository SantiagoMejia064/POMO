using TMPro;
using UnityEngine;

namespace Pomo.Presentation.Popups
{
    [DisallowMultipleComponent]
    public class PopupController : MonoBehaviour
    {
        [Header("Popup")]
        [SerializeField] private GameObject popupPanel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text messageText;

        public bool IsOpen => TargetPanel.activeSelf;

        protected GameObject TargetPanel => popupPanel != null ? popupPanel : gameObject;

        public virtual void OpenPopup()
        {
            TargetPanel.SetActive(true);
        }

        public virtual void OpenPopup(string message)
        {
            SetMessage(message);
            OpenPopup();
        }

        public virtual void OpenPopup(string title, string message)
        {
            SetContent(title, message);
            OpenPopup();
        }

        public virtual void ClosePopup()
        {
            TargetPanel.SetActive(false);
        }

        public void SetContent(string title, string message)
        {
            SetTitle(title);
            SetMessage(message);
        }

        public void SetTitle(string title)
        {
            if (titleText != null)
            {
                titleText.text = title ?? string.Empty;
            }
        }

        public void SetMessage(string message)
        {
            if (messageText != null)
            {
                messageText.text = message ?? string.Empty;
            }
        }
    }
}
