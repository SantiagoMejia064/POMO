using System;
using Pomo.Data.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Presentation.Screens
{
    [DisallowMultipleComponent]
    public class TaskItemView : MonoBehaviour
    {
        private const float DefaultPreferredHeight = 360f;

        private TMP_Text titleText;
        private TMP_Text subjectText;
        private TMP_Text dueDateText;
        private TMP_Text statusText;
        private Button startButton;
        private Button deleteButton;
        private Button itemButton;

        public void Bind(
            TaskModel task,
            string formattedDueDate,
            string status,
            bool showStartButton,
            string startButtonLabel,
            Action onStartOrComplete,
            Action onEdit,
            Action onDelete)
        {
            CacheReferences();
            EnsurePreferredHeight();

            if (titleText != null) titleText.text = task.title;
            if (subjectText != null) subjectText.text = task.subject;
            if (dueDateText != null) dueDateText.text = formattedDueDate;
            if (statusText != null) statusText.text = status;

            ConfigureButton(itemButton, true, null, onEdit);
            ConfigureButton(startButton, showStartButton, startButtonLabel, onStartOrComplete);
            ConfigureButton(deleteButton, true, null, onDelete);
        }

        private void CacheReferences()
        {
            titleText = FindComponent<TMP_Text>("TitleText");
            subjectText = FindComponent<TMP_Text>("SubjectText");
            dueDateText = FindComponent<TMP_Text>("DueDateText");
            statusText = FindComponent<TMP_Text>("StatusText");
            startButton = FindComponent<Button>("Comenzar");
            deleteButton = FindComponent<Button>("Eliminar");
            itemButton = GetComponent<Button>();
        }

        private T FindComponent<T>(string objectName) where T : Component
        {
            Transform[] descendants = GetComponentsInChildren<Transform>(true);

            foreach (Transform descendant in descendants)
            {
                if (descendant.name == objectName)
                {
                    return descendant.GetComponent<T>();
                }
            }

            return null;
        }

        private void EnsurePreferredHeight()
        {
            LayoutElement layoutElement = GetComponent<LayoutElement>();

            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.preferredHeight = DefaultPreferredHeight;
            layoutElement.flexibleHeight = 0f;
        }

        private static void ConfigureButton(Button button, bool isVisible, string label, Action onClick)
        {
            if (button == null)
            {
                return;
            }

            button.gameObject.SetActive(isVisible);
            button.onClick.RemoveAllListeners();

            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick());
            }

            if (!string.IsNullOrEmpty(label))
            {
                TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>(true);

                if (buttonText != null)
                {
                    buttonText.text = label;
                }
            }
        }
    }
}
