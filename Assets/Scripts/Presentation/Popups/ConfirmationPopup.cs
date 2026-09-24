using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Pomo.Presentation.Popups
{
    [DisallowMultipleComponent]
    public class ConfirmationPopup : PopupController
    {
        [Header("Confirmation")]
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private UnityEvent onConfirmed = new UnityEvent();
        [SerializeField] private UnityEvent onCancelled = new UnityEvent();

        private UnityAction confirmationAction;
        private UnityAction cancellationAction;

        public UnityEvent OnConfirmed => onConfirmed;
        public UnityEvent OnCancelled => onCancelled;

        private void OnEnable()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(Confirm);
                confirmButton.onClick.AddListener(Confirm);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(Cancel);
                cancelButton.onClick.AddListener(Cancel);
            }
        }

        private void OnDisable()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(Confirm);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(Cancel);
            }
        }

        public void Show(string title, string message, UnityAction onConfirm, UnityAction onCancel = null)
        {
            confirmationAction = onConfirm;
            cancellationAction = onCancel;
            SetContent(title, message);
            OpenPopup();
        }

        public void Confirm()
        {
            try
            {
                confirmationAction?.Invoke();
                onConfirmed.Invoke();
            }
            finally
            {
                ClearRuntimeActions();
                ClosePopup();
            }
        }

        public void Cancel()
        {
            try
            {
                cancellationAction?.Invoke();
                onCancelled.Invoke();
            }
            finally
            {
                ClearRuntimeActions();
                ClosePopup();
            }
        }

        public override void ClosePopup()
        {
            ClearRuntimeActions();
            base.ClosePopup();
        }

        private void ClearRuntimeActions()
        {
            confirmationAction = null;
            cancellationAction = null;
        }
    }
}
